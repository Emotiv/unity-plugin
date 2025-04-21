using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EmotivUnityPlugin
{
    /// <summary>
    /// Represent as controller to process data stream scribing.
    /// </summary>
    public class DataStreamProcess
    {
        static readonly object _locker = new object();
        private CortexClient   _ctxClient  = CortexClient.Instance;
        private List<string>   _streams;
        private HeadsetFinder  _headsetFinder   = HeadsetFinder.Instance;
        private Authorizer     _authorizer      = Authorizer.Instance;
        private SessionHandler _sessionHandler  = SessionHandler.Instance;
        private MN8EEGInterpolator _mn8Inter = new MN8EEGInterpolator(true);

        ConnectToCortexStates _connectCortexState = ConnectToCortexStates.Service_connecting;
        string _connectCortexWarningMessage = "";

        // Event
        public event EventHandler<ArrayList> MotionDataReceived;      // motion data
        public event EventHandler<ArrayList> EEGDataReceived;         // eeg data
        public event EventHandler<ArrayList> DevDataReceived;         // contact quality
        public event EventHandler<ArrayList> PerfDataReceived;        // performance metric
        public event EventHandler<ArrayList> BandPowerDataReceived;   // band power
        public event EventHandler<ArrayList> FacialExpReceived;       // Facial expressions
        public event EventHandler<ArrayList> MentalCommandReceived;   // mental command
        public event EventHandler<ArrayList> SysEventsReceived;       // Training events of the mental commands and facial expressions
        public event EventHandler<Dictionary<string, JArray>> SubscribedOK;
        public event EventHandler<DateTime> LicenseExpired;           // inform license expired
        public event EventHandler<DateTime> LicenseValidTo;           // inform license valid to date

        // notify headset connecting status
        public event EventHandler<HeadsetConnectEventArgs> HeadsetConnectNotify
        {
            add { _ctxClient.HeadsetConnectNotify += value; }
            remove { _ctxClient.HeadsetConnectNotify -= value; }
        }
        public event EventHandler<List<string>> StreamStopNotify;
        public event EventHandler<string> SessionClosedNotify;

        public event EventHandler<SessionEventArgs> SessionActivedOK
        {
            add { _sessionHandler.SessionActived += value; }
            remove { _sessionHandler.SessionActived -= value; }
        }
        public event EventHandler<string> CreateSessionFail;

        public event EventHandler<List<Headset>> QueryHeadsetOK
        {
            add { _headsetFinder.QueryHeadsetOK += value; }
            remove { _headsetFinder.QueryHeadsetOK -= value; }
        }
        public event EventHandler<string> UserLogoutNotify;             // inform license valid to date

        // For test
        public event EventHandler<string> ErrorNotify;

        public event EventHandler<bool> BTLEPermissionGrantedNotify
        {
            add { _ctxClient.BTLEPermissionGrantedNotify += value; }
            remove { _ctxClient.BTLEPermissionGrantedNotify -= value; }
        }
        public event EventHandler<string> HeadsetScanFinished
        {
            add { _ctxClient.HeadsetScanFinished += value; }
            remove { _ctxClient.HeadsetScanFinished -= value; }
        }

        /// <summary>
        /// Gets states when work with cortex.
        /// Currently, the states relate to authorizing.
        /// </summary>
        /// <value>The current _connectCortexState.</value>

        public ConnectToCortexStates GetConnectToCortexState() => _connectCortexState;

        public string GetConnectToCortexWarningMessage() => _connectCortexWarningMessage;

        public DataStreamProcess() 
        {
        }

        public void ProcessInit() 
        {
            _streams = new List<string>();
            // Event register
            _ctxClient.ErrorMsgReceived             += MessageErrorRecieved;
            _ctxClient.StreamDataReceived           += OnStreamDataReceived;
            _ctxClient.SubscribeDataDone            += OnSubscribeDataDone;
            _ctxClient.UnSubscribeDataDone          += OnUnSubscribeDataDone;
            _ctxClient.StreamStopNotify             += OnStreamStopNotify;

            _authorizer.UserLogoutNotify            += OnUserLogoutNotify;
            _authorizer.AuthorizedFailed            += OnAuthorizedFailed;
            _authorizer.GetLicenseInfoDone          += OnGetLicenseInfoDone;
            _authorizer.LicenseExpired              += OnLicenseExpired;
            _authorizer.ConnectServiceStateChanged  += OnConnectServiceStateChanged;
            _sessionHandler.SessionClosedOK         += OnSessionClosedOK;
            _sessionHandler.SessionClosedNotify     += OnSessionClosedNotify;
        }

        private void OnUserLogoutNotify(object sender, string message)
        {
            // UnityEngine.Debug.Log("OnUserLogoutNotify: " + message);
            StopQueryHeadset();
            // Clear session data
            Clear();
            UserLogoutNotify(this, message);
        }

        private void OnConnectServiceStateChanged(object sender, ConnectToCortexStates state)
        {
            UnityEngine.Debug.Log("OnConnectServiceStateChanged: " + state);
            if (state == ConnectToCortexStates.Service_connecting) {
                StopQueryHeadset();
                // TODO: should check change state at Connect headset controllers
            }

            _connectCortexState = state;
        }

        private void OnLicenseExpired(object sender, License lic)
        {
            LicenseExpired(this, lic.hardLimitTime); // inform hard limit
        }

        private void OnGetLicenseInfoDone(object sender, License lic)
        {
            LicenseValidTo(this, lic.validTo);
            // auto scan headset
            _headsetFinder.FinderInit();
        }

        private void OnStreamStopNotify(object sender, string sessionId)
        {
            string currSessionId = _sessionHandler.SessionId;
            lock (_locker)
            {
                if (currSessionId == sessionId && (_streams.Count > 0)) {
                    StreamStopNotify(this, _streams);
                    _streams.Clear();
                }
            }
        }

        private void OnSessionClosedNotify(object sender, string sessionId)
        {
            // clear data streams
            _streams.Clear();
            SessionClosedNotify(this, sessionId);
        }

        private void OnSessionClosedOK(object sender, string sessionId)
        {
            // clear data streams
            _streams.Clear();
            SessionClosedNotify(this, sessionId);
            UnityEngine.Debug.Log("The Session " + sessionId + " has closed successfully.");
        }

        private void OnUnSubscribeDataDone(object sender, MultipleResultEventArgs e)
        {
            foreach (JObject ele in e.SuccessList)
            {
                lock (_locker)
                {
                    string streamName = (string)ele["streamName"];
                    List<string> unSubList = new List<string>();
                    if (_streams.Contains(streamName)) {
                        _streams.Remove(streamName);
                        unSubList.Add(streamName);
                    }
                    if (unSubList.Count > 0) {
                        StreamStopNotify(this, unSubList);
                    }
                }
            }
            foreach (JObject ele in e.FailList)
            {
                string streamName = (string)ele["streamName"];
                int code = (int)ele["code"];
                string errorMessage = (string)ele["message"];
                UnityEngine.Debug.Log("UnSubscribe stream " + streamName + " unsuccessfully." + " code: " + code + " message: " + errorMessage);
            }
        }

        private void OnSubscribeDataDone(object sender, MultipleResultEventArgs e)
        {
            UnityEngine.Debug.Log("DataStreamProcess: SubscribeDataOK ");
            List<string> failStreams = new List<string>();
            foreach (JObject ele in e.FailList)
            {
                lock (_locker)
                {
                    string streamName = (string)ele["streamName"];
                    int code = (int)ele["code"];
                    string errorMessage = (string)ele["message"];
                    UnityEngine.Debug.Log("Subscribe stream " + streamName + " unsuccessfully." + " code: " + code + " message: " + errorMessage);
                    if (_streams.Contains(streamName)) {
                        failStreams.Add(streamName);
                        _streams.Remove(streamName);
                    }
                }
            }
            
            if (failStreams.Count > 0) {
                // notify stream stop
                StreamStopNotify(this, failStreams);
            }
            Dictionary<string, JArray> headers = new Dictionary<string, JArray>();
            foreach (JObject ele in e.SuccessList)
            {
                string streamName = (string)ele["streamName"];
                JArray header = (JArray)ele["cols"];
                UnityEngine.Debug.Log("SubscribeDataOK header size: " + header.Count + ", stream name " + streamName);

                if (streamName == DataStreamName.DevInfos) {
                    JArray devCols = new JArray();
                    devCols.Add(header[0].ToString());
                    devCols.Add(header[1].ToString());

                    JArray tmp = (JArray)header[2];
                    if(tmp.Count == 3) {
                        devCols.Add(ChannelStringList.ChannelToString(Channel_t.CHAN_AF3));
                        devCols.Add(ChannelStringList.ChannelToString(Channel_t.CHAN_T7));
                        devCols.Add(ChannelStringList.ChannelToString(Channel_t.CHAN_Pz));
                        devCols.Add(ChannelStringList.ChannelToString(Channel_t.CHAN_T8));
                        devCols.Add(ChannelStringList.ChannelToString(Channel_t.CHAN_AF4));
                        devCols.Add(ChannelStringList.ChannelToString(Channel_t.CHAN_CQ_OVERALL));
                    } else {
                        for (int i = 0; i < tmp.Count; i++) {
                            devCols.Add(tmp[i].ToString());
                        }
                    }

                    if (header.Count == 4) {
                        UnityEngine.Debug.Log("The dev stream contain BatteryPercent channel.");
                        devCols.Add(header[3].ToString());
                    }
                    header.RemoveAll();
                    header = devCols;
                } else if(streamName == DataStreamName.EEG && header.Count == 7) {

                    UnityEngine.Debug.Log("Add More channel");

                    JArray devCols = new JArray();
                    devCols.Add(header[0].ToString());
                    devCols.Add(header[1].ToString());

                    devCols.Add(ChannelStringList.ChannelToString(Channel_t.CHAN_AF3));
                    devCols.Add(ChannelStringList.ChannelToString(Channel_t.CHAN_T7));
                    devCols.Add(ChannelStringList.ChannelToString(Channel_t.CHAN_Pz));
                    devCols.Add(ChannelStringList.ChannelToString(Channel_t.CHAN_T8));
                    devCols.Add(ChannelStringList.ChannelToString(Channel_t.CHAN_AF4));

                    devCols.Add(ChannelStringList.ChannelToString(Channel_t.CHAN_RAW_CQ));
                    devCols.Add(ChannelStringList.ChannelToString(Channel_t.CHAN_MARKER_HARDWARE));
                    devCols.Add(ChannelStringList.ChannelToString(Channel_t.CHAN_MARKER));

                    header.RemoveAll();
                    header = devCols;
                }

                headers.Add(streamName, header);
            }
            if (headers.Count > 0) {
                SubscribedOK(this, headers);
            } else {
                UnityEngine.Debug.Log("No data streams are subsribed successfully.");
            }
        }

        private void OnAuthorizedFailed(object sender, string cortexToken)
        {
            UnityEngine.Debug.Log("DataStreamProcess: OnAuthorizedFailed: Do not authorize again.");
            // retry authorize 
            //StartAuthorize();
        }
        private void Clear() {
            lock(_locker)
            {
                // clear session creator
                _sessionHandler.ClearSessionData();
                _streams.Clear();
            }
        }

        private void OnStreamDataReceived(object sender, StreamDataEventArgs e)
        {
            // UnityEngine.Debug.Log("OnStreamDataReceived " + e.StreamName);
            if (e.StreamName == DataStreamName.EEG)
            {
                ArrayList dataList = e.Data as ArrayList;
                if(dataList.Count != 7) {
                    EEGDataReceived(this, dataList);

                    string outStr = string.Join(", ", dataList.ToArray());
                    Debug.Log("Data = [" + outStr + "]");

                    return;
                }

                double T7  = Convert.ToDouble(dataList[3]);
                double T8  = Convert.ToDouble(dataList[4]);
                double AF3 = _mn8Inter.CalculateAF3(T7);
                double AF4 = _mn8Inter.CalculateAF4(T8);
                double Pz  = _mn8Inter.CalculatePz(T7, T8);
                var newValues = new double[] {AF3, T7, Pz, T8, AF4};

                dataList.RemoveAt(4);
                dataList.RemoveAt(3);
                for (int i = newValues.Length - 1; i >= 0; i--) {
                    dataList.Insert(3, newValues[i]);
                }

                EEGDataReceived(this, dataList);
            }
            else if (e.StreamName == DataStreamName.Motion)
            {
                MotionDataReceived(this, e.Data);
            }
            else if (e.StreamName == DataStreamName.PerformanceMetrics)
            {
                PerfDataReceived(this, e.Data);
            }
            else if (e.StreamName == DataStreamName.BandPower)
            {
                BandPowerDataReceived(this, e.Data);
            } 
            else if (e.StreamName == DataStreamName.DevInfos)
            {
                ArrayList dataList = e.Data as ArrayList;
                if(dataList.Count != 7) {
                    DevDataReceived(this, e.Data);
                    return;
                }

                double T7  = Convert.ToDouble(dataList[3]);
                double T8  = Convert.ToDouble(dataList[4]);
                double AF3 = T7;
                double AF4 = T8;
                double Pz  = T7;
                var newValues = new double[] {AF3, T7, Pz, T8, AF4};

                dataList.RemoveAt(4);
                dataList.RemoveAt(3);
                for (int i = newValues.Length - 1; i >= 0; i--) {
                    dataList.Insert(3, newValues[i]);
                }

                string joined = string.Join(", ", dataList.ToArray());
                Debug.Log("Dev Data = [" + joined + "]");

                DevDataReceived(this, dataList);
            } 
            else if (e.StreamName == DataStreamName.FacialExpressions) 
            {
                FacialExpReceived(this, e.Data);
            }
            else if (e.StreamName == DataStreamName.MentalCommands) 
            {
                MentalCommandReceived(this, e.Data);
            }
            else if (e.StreamName == DataStreamName.SysEvents)
            {
                SysEventsReceived(this, e.Data);
            }
        }
        private void MessageErrorRecieved(object sender, ErrorMsgEventArgs errorInfo)
        {
            
            string message  = errorInfo.MessageError;
            string method   = errorInfo.MethodName;
            int errorCode   = errorInfo.Code;
            UnityEngine.Debug.Log("MessageErrorRecieved :code " + errorCode
                                   + " message " + message 
                                   + "method name " + method);
            if (errorInfo.MethodName == "createSession") {
                _sessionHandler.ClearSessionData();
                CreateSessionFail(this, message);
            }
            _connectCortexWarningMessage = message;
            // ErrorNotify(this, message);
        }

        public void SubscribeData(List<string> streams = null) {

            lock (_locker)
            {
                string cortexToken = _authorizer.CortexToken; 
                string currSessionId = _sessionHandler.SessionId;
                // subscribe data
                if (streams == null && _streams.Count > 0) {
                    // subscribe current streams
                    _ctxClient.Subscribe(cortexToken, currSessionId, _streams);
                }
                else if (streams != null) {
                    // subscribe data streams 
                    _ctxClient.Subscribe(cortexToken, currSessionId, streams);
                }
                else {
                    UnityEngine.Debug.Log("SubscribeData: No Stream to subscribe.");
                }
            }
        }

        /// <summary>
        /// Create a session with a headset.
        /// </summary>
        public void CreateSession(string headsetId, bool isActiveSession)
        {
            // Wait a moment before creating session
            System.Threading.Thread.Sleep(1000);
            // CreateSession
            string cortexToken = _authorizer.CortexToken;
             UnityEngine.Debug.Log("Create Session with headset " + headsetId);
             _sessionHandler.Create(cortexToken, headsetId, isActiveSession);
        }

        /// <summary>
        /// Add wanted data streams. 
        /// </summary>
        public void AddStreams(string stream)
        {
            lock (_locker) 
            {
                if (!_streams.Contains(stream)) {
                    _streams.Add(stream);
                }
            }
            
        }

        public void StartConnectToDevice(string headsetId)
        {
            // connect to headset then createSession
            UnityEngine.Debug.Log("Start connecting to headset " + headsetId);
            ConnectToDevice(headsetId); // TODO support mappings for EpocFlex
        }
        
        /// <summary>
        /// Connect to a headset. 
        /// </summary>
        public void ConnectToDevice(string headsetId, string ConnectBy = "", JObject flexMappings= null) {
            _ctxClient.ControlDevice("connect", headsetId, flexMappings);
        }

        /// <summary>
        /// Start authorizing process. 
        /// </summary>
        public void StartAuthorize(string licenseID = "")
        {
            UnityEngine.Debug.Log("DataStreamProcess: Start...");
            // Init websocket client
            _ctxClient.InitWebSocketClient();

            // Start connecting to cortex service
            _authorizer.StartAction(licenseID);
        }

        /// <summary>
        /// Unsubscribe current data streams. 
        /// </summary>
        public void UnSubscribe(List<string> streams = null)
        {
            lock (_locker)
            {
                string cortexToken = _authorizer.CortexToken;
                string currSessionId = _sessionHandler.SessionId;
                if (streams == null) {
                    // unsubscribe all data 
                    _ctxClient.UnSubscribe(cortexToken, currSessionId, _streams);
                }
                else {
                    _ctxClient.UnSubscribe(cortexToken, currSessionId, streams);
                }
            }
            
        }
        
        /// <summary>
        /// Close the current session. 
        /// </summary>
        public void CloseSession()
        {
            string cortexToken = _authorizer.CortexToken;
            _sessionHandler.CloseSession(cortexToken);
        }

        /// <summary>
        /// Start query headsets to get headsets information. 
        /// </summary>
        public void QueryHeadsets(string headsetId = "") 
        {
            _ctxClient.QueryHeadsets(headsetId);
        }

        /// <summary>
        /// Stop query headsets. 
        /// </summary>
        public void StopQueryHeadset()
        {
            _headsetFinder.StopQueryHeadset();
        }

        /// <summary>
        /// Force close websocket client. 
        /// </summary>
        public void ForceCloseWebsocket()
        {
            _ctxClient.ForceCloseWSC();
        }

        /// <summary>
        /// Refresh headset to trigger scan btle devices from Cortex
        /// </summary>
        public void RefreshHeadset() {
            _headsetFinder.RefreshHeadset();
        }
    }
}
