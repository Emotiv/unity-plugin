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
        public static HeadsetService Instance { get; } = new HeadsetService();

        private readonly CortexClient _client;
        private readonly List<Headset> _headsets = new List<Headset>();
        private bool _refreshAndQueryInProgress;
        private bool _sessionCreated;
        private readonly object _sampleLock = new object();
        private readonly Dictionary<string, IDataSample> _latestSamples = new Dictionary<string, IDataSample>(StringComparer.Ordinal);
        private readonly Dictionary<string, IReadOnlyList<string>> _streamHeaders = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        private bool _streamDataHooked;

        public HeadsetService()
        {
            _client = CortexClient.Instance;
            _client.HeadsetScanFinished += OnHeadsetScanFinished;
            HookStreamData();
        }

        internal HeadsetService(CortexClient client)
        {
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
            return new List<Headset>(_headsets);
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
            if (!_sessionCreated) {
                _ = RefreshAndQueryAsync();
            }
            
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
                EventHandler<(CortexErrorCode error, string message)> refreshResultHandler = null;

                refreshResultHandler = (sender, result) =>
                {
                    CleanupRefresh();
                    refreshTcs.TrySetResult(result.error);
                };

                void CleanupRefresh()
                {
                    _client.RefreshHeadsetResult -= refreshResultHandler;
                }

                _client.RefreshHeadsetResult += refreshResultHandler;
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

                    _headsets.Clear();
                    if (result.data != null)
                    {
                        _headsets.AddRange(result.data);
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
            var headset = _headsets.FirstOrDefault(h => string.Equals(h.HeadsetID, headsetId, StringComparison.Ordinal));
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
            _sessionCreated = true;

            if (streams != null && streams.Count > 0)
            {
                var subscribeCode = await SubscribeAsync(sessionResult.Data.SessionId, streams);
                if (subscribeCode != CortexErrorCode.OK)
                {
                    return CortexResult<SessionInfo>.Success(sessionResult.Data);
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
            EventHandler<bool> okHandler = null;
            EventHandler<ErrorMsgEventArgs> errorHandler = null;

            okHandler = (sender, result) =>
            {
                Cleanup();
                tcs.TrySetResult((result ? CortexErrorCode.OK : CortexErrorCode.UnknownError));
            };

            errorHandler = (sender, error) =>
            {
                if (error.MethodName != "controlDevice")
                {
                    return;
                }
                Cleanup();
                tcs.TrySetResult((CortexErrorCode.UnknownError));
            };

            void Cleanup()
            {
                _client.HeadsetDisConnectedOK -= okHandler;
                _client.ErrorMsgReceived -= errorHandler;
            }

            _client.HeadsetDisConnectedOK += okHandler;
            _client.ErrorMsgReceived += errorHandler;
            _client.ControlDevice("disconnect", headsetId, null);

            var code = await tcs.Task;
            return code == CortexErrorCode.OK 
                ? CortexResult.Success() 
                : CortexResult.Fail(CortexErrorMapper.FromErrorCode(code));
        }

        private async Task<CortexErrorCode> ConnectHeadsetAsync(string headsetId, Dictionary<string, string> mappings)
        {
            var tcs = new TaskCompletionSource<CortexErrorCode>();
            EventHandler<HeadsetConnectEventArgs> connectHandler = null;
            EventHandler<ErrorMsgEventArgs> errorHandler = null;

            connectHandler = (sender, info) =>
            {
                if (!string.Equals(info.HeadsetId, headsetId, StringComparison.Ordinal))
                {
                    return;
                }
                Cleanup();
                tcs.TrySetResult(info.IsSuccess ? CortexErrorCode.OK : CortexErrorCode.UnknownError);
            };

            errorHandler = (sender, error) =>
            {
                if (error.MethodName != "controlDevice")
                {
                    return;
                }
                Cleanup();
                tcs.TrySetResult(CortexErrorCode.UnknownError);
            };

            void Cleanup()
            {
                _client.HeadsetConnectNotify -= connectHandler;
                _client.ErrorMsgReceived -= errorHandler;
            }

            _client.HeadsetConnectNotify += connectHandler;
            _client.ErrorMsgReceived += errorHandler;

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

        private async Task<CortexErrorCode> SubscribeAsync(string sessionId, IReadOnlyList<string> streams)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                return CortexErrorCode.UnknownError;
            }

            var tcs = new TaskCompletionSource<CortexErrorCode>();
            EventHandler<MultipleResultEventArgs> okHandler = null;
            EventHandler<ErrorMsgEventArgs> errorHandler = null;

            okHandler = (sender, result) =>
            {
                CacheStreamHeaders(result);
                Cleanup();
                var hasSuccess = result.SuccessList != null && result.SuccessList.Count > 0;
                tcs.TrySetResult(hasSuccess ? CortexErrorCode.OK : CortexErrorCode.UnknownError);
            };

            errorHandler = (sender, error) =>
            {
                if (error.MethodName != "subscribe")
                {
                    return;
                }
                Cleanup();
                tcs.TrySetResult(CortexErrorCode.UnknownError);
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
                for (var i = 0; i < headers.Count && (i + 1) < e.Data.Count; i++)
                {
                    var key = headers[i];
                    if (IsNonChannelHeader(key))
                    {
                        continue;
                    }
                    var value = Convert.ToSingle(e.Data[i + 1]);
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
            if (streamName == DataStreamName.DevInfos && header.Count > 0)
            {
                var devCols = new List<string>();
                devCols.Add(header[0].ToString());
                devCols.Add(header[1].ToString());

                if (header.Count > 2 && header[2] is JArray channelList)
                {
                    for (var i = 0; i < channelList.Count; i++)
                    {
                        devCols.Add(channelList[i].ToString());
                    }
                }

                if (header.Count > 3)
                {
                    for (var id = 3; id < header.Count; id++)
                    {
                        devCols.Add(header[id].ToString());
                    }
                }

                return devCols;
            }

            var cols = new List<string>(header.Count);
            foreach (var token in header)
            {
                cols.Add(token.ToString());
            }
            return cols;
        }

        private static bool IsNonChannelHeader(string header)
        {
            if (string.IsNullOrWhiteSpace(header))
            {
                return true;
            }

            return header.Equals("interpolated", StringComparison.OrdinalIgnoreCase)
                   || header.Equals("counter", StringComparison.OrdinalIgnoreCase)
                   || header.Equals("timestamp", StringComparison.OrdinalIgnoreCase);
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
