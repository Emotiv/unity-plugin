using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Emotiv.Cortex.Models;
using EmotivUnityPlugin;
using Newtonsoft.Json.Linq;

namespace Emotiv.Cortex.Service
{
    public class HeadsetService : IHeadsetService
    {
        private readonly CortexRuntimeContext _context;
        private readonly CortexClient _client;
        private volatile bool _refreshAndQueryInProgress;
        private readonly object _sampleLock = new object();
        private readonly Dictionary<string, IDataSample> _latestSamples = new Dictionary<string, IDataSample>(StringComparer.Ordinal);
        private readonly Dictionary<string, IReadOnlyList<string>> _streamHeaders = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        private bool _streamDataHooked;

        public HeadsetService(CortexRuntimeContext context, CortexClient client)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _client.HeadsetScanFinished += OnHeadsetScanFinished;
            HookStreamData();
        }

        public async Task<CortexResult> ScanHeadsetAsync()
        {
            var queryCode = await RefreshAndQueryAsync();
            if (queryCode != CortexErrorCode.OK)
            {
                return CortexResult.Fail(CortexErrorMapper.FromErrorCode(queryCode));
            }

            return CortexResult.Success();
        }

        public List<Headset> GetHeadsets()
        {
            return _context.Headsets;
        }

        public bool TakeLatestSample(string stream, out IDataSample sample)
        {
            sample = null;
            if (string.IsNullOrWhiteSpace(stream))
            {
                return false;
            }

            lock (_sampleLock)
            {
                return _latestSamples.TryGetValue(stream, out sample) && sample != null;
            }
        }
        
        private void OnHeadsetScanFinished(object sender, string message)
        {
            UnityEngine.Debug.Log("Headset scan finished with message: " + message);
            _ = RefreshAndQueryAsync();
        }

        private async Task<CortexErrorCode> RefreshAndQueryAsync()
        {
            if (_refreshAndQueryInProgress)
            {
                return CortexErrorCode.UnknownError;
            }

            _refreshAndQueryInProgress = true;
            try
            {
                var refreshTcs = new TaskCompletionSource<CortexErrorCode>();
                EventHandler<(CortexErrorCode error, string command)> refreshResultHandler = null;

                refreshResultHandler = (sender, result) =>
                {
                    if (result.command != "refresh")
                    {
                        return;
                    }
                    CleanupRefresh();
                    refreshTcs.TrySetResult(result.error);
                };

                void CleanupRefresh()
                {
                    _client.ControlDeviceResult -= refreshResultHandler;
                }

                _client.ControlDeviceResult += refreshResultHandler;
                RefreshHeadset();

                var refreshCode = await refreshTcs.Task;

                if (refreshCode != CortexErrorCode.OK)
                {
                    return refreshCode;
                }

                var queryTcs = new TaskCompletionSource<CortexErrorCode>();
                EventHandler<(CortexErrorCode error, List<Headset> data)> queryResultHandler = null;

                queryResultHandler = (sender, result) =>
                {                    
                    CleanupQuery();
                    if (result.error != CortexErrorCode.OK)
                    {
                        queryTcs.TrySetResult(result.error);
                        return;
                    }

                    if (result.data != null)
                    {
                        _context.SetHeadsets(result.data);
                    }
                    else
                    {
                        _context.SetHeadsets(null);
                    }
                    queryTcs.TrySetResult(CortexErrorCode.OK);
                };

                void CleanupQuery()
                {
                    _client.QueryHeadsetResult -= queryResultHandler;
                }

                _client.QueryHeadsetResult += queryResultHandler;
                QueryHeadsets();

                return await queryTcs.Task;
            }
            finally
            {
                _refreshAndQueryInProgress = false;
            }
        }


        public async Task<CortexResult<SessionInfo>> ConnectHeadsetAsync(
            string headsetId,
            Dictionary<string, string> mappings = null,
            IReadOnlyList<string> streams = null)
        {
            // check headset exists in the list
            var headset = _context.Headsets.FirstOrDefault(h => string.Equals(h.HeadsetID, headsetId, StringComparison.Ordinal));
            if (headset == null)
            {
                return CortexResult<SessionInfo>.Fail(CortexErrorMapper.FromErrorCode(CortexErrorCode.HeadsetNotFound));
            }

            var connectCode = await ConnectHeadsetAsync(headsetId, mappings);
            if (connectCode != CortexErrorCode.OK)
            {
                return CortexResult<SessionInfo>.Fail(CortexErrorMapper.FromErrorCode(connectCode));
            }

            var sessionResult = await CreateSessionAsync(headsetId);
            if (sessionResult.Code != CortexErrorCode.OK)
            {
                return CortexResult<SessionInfo>.Fail(CortexErrorMapper.FromErrorCode(sessionResult.Code));
            }

            if (streams != null && streams.Count > 0)
            {
                var subscribeResult = await SubscribeAsync(sessionResult.Data.SessionId, streams);
                if (subscribeResult.IsSuccess)
                {
                    return CortexResult<SessionInfo>.Success(sessionResult.Data);
                }
                else {
                    return CortexResult<SessionInfo>.Fail(subscribeResult.Error);
                }

            }

            return CortexResult<SessionInfo>.Success(sessionResult.Data);
        }

        public async Task<CortexResult> DisconnectHeadsetAsync(string headsetId)
        {
            if (string.IsNullOrWhiteSpace(headsetId))
            {
                return CortexResult.Fail(CortexErrorMapper.FromErrorCode(CortexErrorCode.UnknownError));
            }

            var tcs = new TaskCompletionSource<CortexErrorCode>();
            EventHandler<(CortexErrorCode error, string command)> disconnectHandler = null;

            disconnectHandler = (sender, result) =>
            {
                if (result.command != "disconnect")
                {
                    return;
                }
                Cleanup();
                tcs.TrySetResult(result.error);
            };

            void Cleanup()
            {
                _client.ControlDeviceResult -= disconnectHandler;
            }

            _client.ControlDeviceResult += disconnectHandler;
            _client.ControlDevice("disconnect", headsetId, null);

            var code = await tcs.Task;
            return code == CortexErrorCode.OK 
                ? CortexResult.Success() 
                : CortexResult.Fail(CortexErrorMapper.FromErrorCode(code));
        }

        private async Task<CortexErrorCode> ConnectHeadsetAsync(string headsetId, Dictionary<string, string> mappings)
        {
            var tcs = new TaskCompletionSource<CortexErrorCode>();
            EventHandler<(CortexErrorCode error, string command)> connectHandler = null;

            connectHandler = (sender, result) =>
            {
                if (result.command != "connect")
                {
                    return;
                }
                Cleanup();
                tcs.TrySetResult(result.error);
            };

            void Cleanup()
            {
                _client.ControlDeviceResult -= connectHandler;
            }

            _client.ControlDeviceResult += connectHandler;

            _client.ControlDevice("connect", headsetId, ToMappings(mappings));
            return await tcs.Task;
        }

        private async Task<(CortexErrorCode Code, SessionInfo Data)> CreateSessionAsync(string headsetId)
        {
            var tcs = new TaskCompletionSource<(CortexErrorCode Code, SessionInfo Data)>();
            EventHandler<SessionEventArgs> okHandler = null;
            EventHandler<ErrorMsgEventArgs> errorHandler = null;

            okHandler = (sender, sessionInfo) =>
            {
                if (!string.Equals(sessionInfo.HeadsetId, headsetId, StringComparison.Ordinal))
                {
                    return;
                }
                Cleanup();
                tcs.TrySetResult((CortexErrorCode.OK, SessionInfo.FromSessionEventArgs(sessionInfo)));
            };

            errorHandler = (sender, error) =>
            {
                if (error.MethodName != "createSession")
                {
                    return;
                }
                Cleanup();
                tcs.TrySetResult((CortexErrorCode.UnknownError, null));
            };

            void Cleanup()
            {
                _client.CreateSessionOK -= okHandler;
                _client.ErrorMsgReceived -= errorHandler;
            }

            _client.CreateSessionOK += okHandler;
            _client.ErrorMsgReceived += errorHandler;
            _client.CreateSession(headsetId);

            return await tcs.Task;
        }

        private async Task<CortexResult> SubscribeAsync(string sessionId, IReadOnlyList<string> streams)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                return CortexResult.Fail(CortexErrorMapper.FromErrorCode(CortexErrorCode.SubscriptionFailed));
            }

            var tcs = new TaskCompletionSource<CortexResult>();
            EventHandler<MultipleResultEventArgs> okHandler = null;
            EventHandler<ErrorMsgEventArgs> errorHandler = null;

            okHandler = (sender, result) =>
            {
                CacheStreamHeaders(result);
                Cleanup();
                var hasSuccess = result.SuccessList != null && result.SuccessList.Count > 0;
                tcs.TrySetResult(hasSuccess ? CortexResult.Success() : CortexResult.Fail(CortexErrorMapper.FromErrorCode(CortexErrorCode.SubscriptionFailed)));
            };

            errorHandler = (sender, error) =>
            {
                if (error.MethodName != "subscribe")
                {
                    return;
                }
                Cleanup();
                var cortexError = new CortexError((CortexErrorCode)error.Code, error.MessageError);
                tcs.TrySetResult(CortexResult.Fail(cortexError));
            };

            void Cleanup()
            {
                _client.SubscribeDataDone -= okHandler;
                _client.ErrorMsgReceived -= errorHandler;
            }

            _client.SubscribeDataDone += okHandler;
            _client.ErrorMsgReceived += errorHandler;
            _client.Subscribe(_client.CurrentCortexToken, sessionId, streams.ToList());

            return await tcs.Task;
        }

        private void HookStreamData()
        {
            if (_streamDataHooked)
            {
                return;
            }

            _client.StreamDataReceived += OnStreamDataReceived;
            _streamDataHooked = true;
        }

        private void OnStreamDataReceived(object sender, StreamDataEventArgs e)
        {
            if (e == null || string.IsNullOrEmpty(e.StreamName))
            {
                return;
            }

            switch (e.StreamName)
            {
                case DataStreamName.DevInfos:
                    if (TryBuildCQSample(e, out var cqSample))
                    {
                        SetLatestSample(DataStreamName.DevInfos, cqSample);
                    }
                    break;
                case DataStreamName.MentalCommands:
                    if (TryBuildMentalCommandSample(e, out var comSample))
                    {
                        SetLatestSample(DataStreamName.MentalCommands, comSample);
                    }
                    break;
            }
        }

        private void SetLatestSample(string stream, IDataSample sample)
        {
            if (sample == null || string.IsNullOrEmpty(stream))
            {
                return;
            }
            lock (_sampleLock)
            {
                _latestSamples[stream] = sample;
            }
        }

        private bool TryBuildCQSample(StreamDataEventArgs e, out IDataSample sample)
        {
            sample = null;
            if (e.Data == null || e.Data.Count == 0)
            {
                return false;
            }

            if (!TryGetTimestamp(e.Data, out var timestamp))
            {
                return false;
            }

            IReadOnlyList<string> headers = null;
            lock (_sampleLock)
            {
                _streamHeaders.TryGetValue(DataStreamName.DevInfos, out headers);
            }

            var values = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            if (headers != null)
            {
                for (var i = 0; i < headers.Count && i < e.Data.Count; i++)
                {
                    var key = headers[i];
                    var value = Convert.ToSingle(e.Data[i]);
                    values[key] = value;
                }
            }
            else
            {
                // throw exception or return false if headers are not available, as we don't know how to parse the data
                return false;
            }

            sample = new CQDataSample(timestamp, values);
            return true;
        }

        private bool TryBuildMentalCommandSample(StreamDataEventArgs e, out IDataSample sample)
        {
            sample = null;
            if (e.Data == null || e.Data.Count < 3)
            {
                return false;
            }

            if (!TryGetTimestamp(e.Data, out var timestamp))
            {
                return false;
            }

            var action = Convert.ToString(e.Data[1]);
            var power = Convert.ToSingle(e.Data[2]);
            sample = new MentalCommandDataSample(timestamp, action, power);
            return true;
        }

        private static bool TryGetTimestamp(ArrayList data, out double timestamp)
        {
            timestamp = 0;
            if (data == null || data.Count == 0)
            {
                return false;
            }

            try
            {
                timestamp = Convert.ToDouble(data[0]);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void CacheStreamHeaders(MultipleResultEventArgs result)
        {
            if (result?.SuccessList == null || result.SuccessList.Count == 0)
            {
                return;
            }

            lock (_sampleLock)
            {
                foreach (JObject ele in result.SuccessList)
                {
                    var streamName = (string)ele["streamName"];
                    if (string.IsNullOrEmpty(streamName))
                    {
                        continue;
                    }

                    var header = (JArray)ele["cols"];
                    if (header == null)
                    {
                        continue;
                    }

                    _streamHeaders[streamName] = NormalizeHeaders(streamName, header);
                }
            }
        }


        private static IReadOnlyList<string> NormalizeHeaders(string streamName, JArray header)
        {
            UnityEngine.Debug.Log("Normalizing headers for stream: " + streamName + " with header: " + header.Count);
            var cols = new List<string>();
            cols.Add("TimeStamp");
            if (streamName == DataStreamName.DevInfos && header.Count >= 2)
            {
                // the dev infos has data format kind of "Battery", "Signal", ["AF3","T7","Pz","T8","AF4","OVERALL"],"BatteryPercent"
                cols.Add(header[0].ToString());
                cols.Add(header[1].ToString());

                if (header.Count > 2 && header[2] is JArray channelList)
                {
                    for (var i = 0; i < channelList.Count; i++)
                    {
                        cols.Add(channelList[i].ToString());
                    }
                }

                if (header.Count > 3)
                {
                    for (var id = 3; id < header.Count; id++)
                    {
                        cols.Add(header[id].ToString());
                    }
                }

                return cols;
            }

            foreach (var token in header)
            {
                cols.Add(token.ToString());
            }
            return cols;
        }

        private static JObject ToMappings(Dictionary<string, string> mappings)
        {
            if (mappings == null || mappings.Count == 0)
            {
                return null;
            }

            var obj = new JObject();
            foreach (var kvp in mappings)
            {
                obj[kvp.Key] = kvp.Value;
            }
            return obj;
        }

        private void RefreshHeadset()
        {
            _client.ControlDevice("refresh", string.Empty, null);
        }

        private void QueryHeadsets()
        {
            _client.QueryHeadsets(string.Empty);
        }
    }
}
