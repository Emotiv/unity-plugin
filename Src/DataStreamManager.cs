using System;
using System.Threading;
using System.Collections;
using EmotivUnityPlugin;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace EmotivUnityPlugin
{
    /// <summary>
    /// Reponsible for managing data streams subscribing from connect to cortex to subscribe data.
    /// </summary>
    public class DataStreamManager
    {
        static DataStreamProcess _dsProcess = new DataStreamProcess();
        static readonly object _locker = new object();
        
        /// <summary>
        /// Band power data buffer.
        /// </summary>
        BandPowerDataBuffer _bandpowerBuff;

        /// <summary>
        /// EEG data buffer.
        /// </summary>
        EegMotionDataBuffer _eegBuff;
        
        /// <summary>
        /// Motion data buffer.
        /// </summary>
        EegMotionDataBuffer _motionBuff;
        
        /// <summary>
        /// Device information data buffer.
        /// </summary>
        DevDataBuffer       _devBuff;
        
        /// <summary>
        /// Performance metric data buffer.
        /// </summary>
        PMDataBuffer        _pmBuff;

        /// <summary>
        /// Facial expression data sample object which include timestamp.
        /// </summary>
        List<string>        _facLists = null;
        
        /// <summary>
        /// Mental command data sample object which include timestamp.
        /// </summary>
        List<string>        _mentalCommandLists = null;
        
        /// <summary>
        /// System or Training events data sample object which include timestamp.
        /// </summary>
        List<string>        _sysLists = null;

        /// <summary>
        /// A wanted headset which user want to create a session and subscribe data.
        /// </summary>
        string _wantedHeadsetId  = "";
        bool _isSessActivated    = false;
        bool _readyCreateSession = false;
        List<Headset>  _detectedHeadsets = new List<Headset>(); // list of detected headsets

        public event EventHandler<string> SessionActivatedOK;
        public event EventHandler<string> HeadsetConnectFail;
        public event EventHandler<DateTime> LicenseValidTo;

        public event EventHandler<FacEventArgs> FacialExpReceived;
        public event EventHandler<MentalCommandEventArgs> MentalCommandReceived;
        public event EventHandler<SysEventArgs> SysEventReceived;

        public event EventHandler<string> SysEventSubscribed;
        public event EventHandler<string> SysEventUnSubscribed;

        private DataStreamManager()
        {
            Init();
        }

        ~DataStreamManager()
        {
        }
        public static DataStreamManager Instance { get; } = new DataStreamManager();

        private void Init()
        {
            _dsProcess.ProcessInit();
            _dsProcess.SubscribedOK             += OnSubscribedOK;
            _dsProcess.HeadsetConnectNotify     += OnHeadsetConnectNotify;
            _dsProcess.StreamStopNotify         += OnStreamStopNotify;
            _dsProcess.LicenseValidTo           += OnLicenseValidTo;
            _dsProcess.LicenseExpired           += OnLicenseExpired;
            _dsProcess.SessionActivedOK         += OnSessionActivedOK;
            _dsProcess.CreateSessionFail        += OnCreateSessionFail;
            _dsProcess.QueryHeadsetOK           += OnQueryHeadsetOK;
            _dsProcess.UserLogoutNotify         += OnUserLogoutNotify;
            _dsProcess.SessionClosedNotify      += OnSessionClosedNotify;
        }

        /// <summary>
        /// Reset all data buffers.
        /// </summary>
        private void ResetDataBuffers()
        {
            if (_eegBuff != null) {
                _dsProcess.EEGDataReceived -= _eegBuff.OnDataReceived;
                _eegBuff.Clear();
                _eegBuff = null;
            }
            if (_devBuff != null) {
                _dsProcess.DevDataReceived -= _devBuff.OnDevDataReceived;
                _devBuff.Clear();
                _devBuff = null;
            }
            if (_bandpowerBuff != null) {
                _dsProcess.BandPowerDataReceived -= _bandpowerBuff.OnBandPowerReceived;
                _bandpowerBuff.Clear();
                _bandpowerBuff = null;
            }
            if (_motionBuff != null) {
                _dsProcess.MotionDataReceived -= _motionBuff.OnDataReceived;
                _motionBuff.Clear();
                _motionBuff = null;
            }
            if (_pmBuff != null) {
                _dsProcess.PerfDataReceived -= _pmBuff.OnPMDataReceived;
                _pmBuff.Clear();
                _pmBuff = null;
            }
            if (_facLists != null) {
                _dsProcess.FacialExpReceived -= OnFacialExpressionReceived;
                _facLists.Clear();
                _facLists = null;
            }
            if (_mentalCommandLists != null) {
                _dsProcess.MentalCommandReceived -= OnMentalCommandReceived;
                _mentalCommandLists.Clear();
                _mentalCommandLists = null;
            }
            if (_sysLists != null) {
                _dsProcess.SysEventsReceived -= OnSysEventReceived;
                _sysLists.Clear();
                _sysLists = null;
                SysEventUnSubscribed(this, "");
            }
            
            UnityEngine.Debug.Log("EmotivDataStream:ResetDataBuffers Done");
        }

        //====Handle events
        private void OnSessionClosedNotify(object sender, string sessionId)
        {
            lock (_locker)
            {
                UnityEngine.Debug.Log("EmotivDataStream: OnSessionClosedNotify.");

                _isSessActivated    = false;
                _readyCreateSession = true;
                _wantedHeadsetId    = "";
                _detectedHeadsets.Clear();
                ResetDataBuffers();
            }
        }

        private void OnUserLogoutNotify(object sender, string message)
        {
            lock (_locker)
            {
                UnityEngine.Debug.Log("EmotivDataStream: OnUserLogoutNotify.");

                // reset data
                _isSessActivated    = false;
                _readyCreateSession = true;
                _wantedHeadsetId    = "";
                _detectedHeadsets.Clear();
                ResetDataBuffers();
            }
        }

        private void OnQueryHeadsetOK(object sender, List<Headset> headsets)
        {
            lock(_locker)
            {
                _detectedHeadsets.Clear();
                string strOut = "";
                foreach(var headset in headsets) {
                    _detectedHeadsets.Add(headset);
                    string headsetId = headset.HeadsetID;
                    if (_readyCreateSession && headsetId == _wantedHeadsetId &&
                        (headset.Status == "connected")) {
                        UnityEngine.Debug.Log("The headset " + headsetId + " is connected. Start creating session.");
                        
                        _readyCreateSession = false;
                        // create session
                        _dsProcess.CreateSession(headsetId, true);
                    }
                    strOut += (headset.HeadsetID + "-" + headset.HeadsetConnection + "-" + headset.Status + "; ");
                }
                UnityEngine.Debug.Log("EmotivDataStream-OnQueryHeadsetOK: " + strOut);
            }
        }

        private void OnCreateSessionFail(object sender, string errorMsg)
        {
            lock (_locker)
            {
                _wantedHeadsetId    = ""; // reset headset
                _readyCreateSession = true;
                _detectedHeadsets.Clear();
                UnityEngine.Debug.Log("Create session unsuccessfully. Message " + errorMsg);
            }
        }

        private void OnSessionActivedOK(object sender, SessionEventArgs sessionInfo)
        {
            lock (_locker)
            {
                UnityEngine.Debug.Log("EmotivDAtaStream: OnSessionActivedOK " + sessionInfo.HeadsetId);
                if (sessionInfo.HeadsetId == _wantedHeadsetId) {
                    // subscribe data
                    _isSessActivated    = true;
                    _readyCreateSession = false;

                    SessionActivatedOK(this, _wantedHeadsetId);
                    _dsProcess.SubscribeData(sessionInfo.SessionId);
                }
                else {
                    //TODO:
                    UnityEngine.Debug.Log("Session is activated but for headset " + sessionInfo.HeadsetId);
                }
            }
        }

        private void OnLicenseExpired(object sender, DateTime hardLimitTime)
        {
            UnityEngine.Debug.Log("OnLicenseExpired: Please re-authorize before hard limit time " + Utils.ISODateTimeToString(hardLimitTime));
            // TODO: Inform user buy license and re-authorize before hard limit time.
        }

        private void OnLicenseValidTo(object sender, DateTime validTo)
        {
            LicenseValidTo(this, validTo);
            UnityEngine.Debug.Log("OnLicenseValidTo: the license valid to " + Utils.ISODateTimeToString(validTo));
        }

        private void OnStreamStopNotify(object sender, List<string> streams)
        {
            lock (_locker)
            {
                _wantedHeadsetId    = "" ; // reset headset
                _readyCreateSession = true;
                UnityEngine.Debug.Log("OnStreamStopNotify: Stop data stream from Cortex.");
                foreach (string streamName in streams) {
                    if (streamName == DataStreamName.EEG) {
                        // clear _eegBuffer
                        if (_eegBuff != null) {
                            _dsProcess.EEGDataReceived -= _eegBuff.OnDataReceived;
                            _eegBuff.Clear();
                            _eegBuff = null;
                        }
                    }
                    else if (streamName == DataStreamName.DevInfos) {
                        if (_devBuff != null) {
                            _dsProcess.DevDataReceived -= _devBuff.OnDevDataReceived;
                            _devBuff.Clear();
                            _devBuff = null;
                        }
                    }
                    else if (streamName == DataStreamName.BandPower) {
                        if (_bandpowerBuff != null) {
                            _dsProcess.BandPowerDataReceived -= _bandpowerBuff.OnBandPowerReceived;
                            _bandpowerBuff.Clear();
                            _bandpowerBuff = null;
                        }
                    }
                    else if (streamName == DataStreamName.Motion) {
                        if (_motionBuff != null) {
                            _dsProcess.MotionDataReceived -= _motionBuff.OnDataReceived;
                            _motionBuff.Clear();
                            _motionBuff = null;
                        }
                    }
                    else if (streamName == DataStreamName.PerformanceMetrics) {
                        if (_pmBuff != null) {
                            _dsProcess.PerfDataReceived -= _pmBuff.OnPMDataReceived;
                            _pmBuff.Clear();
                            _pmBuff = null;
                        }
                    }
                    else if (streamName == DataStreamName.FacialExpressions) {
                        if (_facLists != null) {
                            _dsProcess.FacialExpReceived -= OnFacialExpressionReceived;
                            _facLists.Clear();
                            _facLists = null;
                        }
                    }
                    else if (streamName == DataStreamName.SysEvents) {
                        if (_sysLists != null) {
                            _dsProcess.SysEventsReceived -= OnSysEventReceived;
                            _sysLists.Clear();
                            _sysLists = null;
                            SysEventUnSubscribed(this, "");
                        }
                    }
                    else if (streamName == DataStreamName.MentalCommands) {
                        if (_mentalCommandLists != null) {
                            _dsProcess.MentalCommandReceived -= OnMentalCommandReceived;
                            _mentalCommandLists.Clear();
                            _mentalCommandLists = null;
                        }
                    }
                    else {
                        UnityEngine.Debug.Log("EmotivDataStream-OnStreamStopNotify: stream name:" + streamName);
                    }
                }
            }
        }

        private void OnHeadsetConnectNotify(object sender, HeadsetConnectEventArgs e)
        {
            lock (_locker)
            {
                string headsetId = e.HeadsetId;
                UnityEngine.Debug.Log("OnHeadsetConnectNotify for headset " + headsetId + " while wantedHeadset : " + _wantedHeadsetId);
                if (e.IsSuccess && _readyCreateSession &&
                    (headsetId == _wantedHeadsetId)) {
                    UnityEngine.Debug.Log("Connect the headset " + headsetId + " successfully. Start creating session.");
                    _readyCreateSession = false;
                    // create session
                    _dsProcess.CreateSession(headsetId, true);
                }
                else if (headsetId == _wantedHeadsetId) {
                    UnityEngine.Debug.Log("Connect the headset " + headsetId + " unsuccessfully. Message : " + e.Message);
                    HeadsetConnectFail(this, headsetId);
                    _wantedHeadsetId = ""; // reset headsetId
                    _isSessActivated = false;
                }
                else {
                    UnityEngine.Debug.Log("Connect a headset " + headsetId + " unsuccessfully. Message : " + e.Message);
                }
            }
        }

        private void OnSubscribedOK(object sender, Dictionary<string, JArray> e)
        {
            lock (_locker)
            {
                UnityEngine.Debug.Log("EmotivDataStream: SubscribedOK");
                foreach (string key in e.Keys)
                {
                    int headerCount = e[key].Count;
                    if (key == DataStreamName.EEG) {
                        if (_eegBuff == null) {
                            _eegBuff = new EegMotionDataBuffer();
                            _eegBuff.SetDataType(EegMotionDataBuffer.DataType.EEG);
                            _eegBuff.SettingBuffer(4, 4, headerCount);
                            _eegBuff.SetChannels(e[key]);
                            _dsProcess.EEGDataReceived += _eegBuff.OnDataReceived;
                            UnityEngine.Debug.Log("Subscribed done EEG Data Stream");
                        }               
                    }
                    else if (key == DataStreamName.Motion) {
                        if (_motionBuff == null) {
                            _motionBuff = new EegMotionDataBuffer();
                            _motionBuff.SetDataType(EegMotionDataBuffer.DataType.MOTION);
                            _motionBuff.SettingBuffer(4, 4, headerCount);
                            _motionBuff.SetChannels(e[key]);
                            _dsProcess.MotionDataReceived += _motionBuff.OnDataReceived;
                            UnityEngine.Debug.Log("Subscribed done Motion Data Stream");
                        }               
                    }
                    else if (key == DataStreamName.BandPower) { 
                        if (_bandpowerBuff == null){
                            _bandpowerBuff = new BandPowerDataBuffer();
                            _bandpowerBuff.SettingBuffer(4, 4, headerCount);
                            _bandpowerBuff.SetChannels(e[key]);
                            _dsProcess.BandPowerDataReceived += _bandpowerBuff.OnBandPowerReceived;
                            UnityEngine.Debug.Log("Subscribed done: POW Stream");
                        }        
                    }
                    else if (key == DataStreamName.DevInfos) {
                        if (_devBuff == null){
                            _devBuff = new DevDataBuffer();
                            _devBuff.SettingBuffer(4, 4, headerCount);
                            _devBuff.SetChannels(e[key]);
                            _dsProcess.DevDataReceived += _devBuff.OnDevDataReceived;
                            UnityEngine.Debug.Log("Subscribed done: DEV Data Stream");
                        }
                    }
                    else if (key == DataStreamName.PerformanceMetrics) {
                        if (_pmBuff == null){
                            _pmBuff = new PMDataBuffer();
                            int count = _pmBuff.SetChannels(e[key]);
                            _pmBuff.SettingBuffer(4, 4, count);
                            _dsProcess.PerfDataReceived += _pmBuff.OnPMDataReceived;
                            UnityEngine.Debug.Log("Subscribed done: Peformance metrics Data Stream");
                        }
                    }
                    else if (key == DataStreamName.FacialExpressions) {
                        if (_facLists == null){
                            _facLists = new List<string>();
                            _facLists.Add("TimeStamp");
                            foreach (var ele in e[key]) {
                                _facLists.Add(ele.ToString());
                            }
                            _dsProcess.FacialExpReceived += OnFacialExpressionReceived;
                            UnityEngine.Debug.Log("Subscribed done: Facial expression Data Stream");
                        }
                    }
                    else if (key == DataStreamName.MentalCommands) {
                        if (_mentalCommandLists == null){
                            _mentalCommandLists = new List<string>();
                            _mentalCommandLists.Add("TimeStamp");
                            foreach (var ele in e[key]) {
                                _mentalCommandLists.Add(ele.ToString());
                            }
                            _dsProcess.MentalCommandReceived += OnMentalCommandReceived;
                            UnityEngine.Debug.Log("Subscribed done: Mental command Data Stream");
                        }
                    }
                    else if (key == DataStreamName.SysEvents) {
                        if (_sysLists == null){
                            _sysLists = new List<string>();
                            _sysLists.Add("TimeStamp");
                            foreach (var ele in e[key]) {
                                _sysLists.Add(ele.ToString());
                            }
                            _dsProcess.SysEventsReceived += OnSysEventReceived;

                            SysEventSubscribed(this, _wantedHeadsetId);
                            UnityEngine.Debug.Log("Subscribed done: Sys event Stream");
                        }
                    }
                    else {
                        UnityEngine.Debug.Log("SubscribedOK(): stream " + key);
                    }
                }
            }
        }

        private void OnFacialExpressionReceived(object sender, ArrayList data)
        {
            if (_facLists == null || _facLists.Count != data.Count) {
                UnityEngine.Debug.LogAssertion("OnFacialExpressionReceived: Mismatch between data and label");
                return;
            }
            double time     = Convert.ToDouble(data[0]);
            string eyeAct   = Convert.ToString(data[1]);
            string uAct     = Convert.ToString(data[2]);
            double uPow     = Convert.ToDouble(data[3]);
            string lAct     = Convert.ToString(data[4]);
            double lPow     = Convert.ToDouble(data[5]);
            FacEventArgs facEvent = new FacEventArgs(time, eyeAct,
                                                    uAct, uPow, lAct, lPow);
            string facListsStr = "";
            foreach (var ele in _facLists) {
                facListsStr += ele + " , ";
            }
            UnityEngine.Debug.Log("Fac labels: " +facListsStr);
            UnityEngine.Debug.Log("Fac datas : " +facEvent.Time.ToString() + " , " + facEvent.EyeAct+ " , " +
                                facEvent.UAct + " , " + facEvent.UPow.ToString() + " , " +
                                facEvent.LAct + " , " + facEvent.LPow.ToString());
            
            // TODO: emit event to other modules
            // FacialExpReceived(this, facEvent);
        }

        private void OnMentalCommandReceived(object sender, ArrayList data)
        {
            if (_mentalCommandLists == null || _mentalCommandLists.Count != data.Count) {
                UnityEngine.Debug.LogAssertion("OnMentalCommandReceived: Mismatch between data and label");
                return;
            }
            double time     = Convert.ToDouble(data[0]);
            string act      = Convert.ToString(data[1]);
            double pow      = Convert.ToDouble(data[2]);
            MentalCommandEventArgs comEvent = new MentalCommandEventArgs(time, act, pow);
            string comListsStr = "";
            foreach (var ele in _mentalCommandLists) {
                comListsStr += ele + " , ";
            }
            UnityEngine.Debug.Log("MentalCommand labels: " +comListsStr);
            UnityEngine.Debug.Log("MentalCommand datas : " +comEvent.Time.ToString() + " , " 
                                + comEvent.Act+ " , " + comEvent.Pow);
            
            // TODO: emit event to other modules
            //MentalCommandReceived(this, comEvent);
        }

        private void OnSysEventReceived(object sender, ArrayList data)
        {
            if (_sysLists == null || _sysLists.Count != data.Count) {
                UnityEngine.Debug.LogAssertion("OnSysEventReceived: Mismatch between data and label");
                return;
            }
            double time         = Convert.ToDouble(data[0]);
            string detection    = Convert.ToString(data[1]);
            string eventMsg     = Convert.ToString(data[2]);
            SysEventArgs sysEvent = new SysEventArgs(time, detection, eventMsg);
            string SysListsStr = "";
            foreach (var ele in _sysLists) {
                SysListsStr += ele + " , ";
            }
            UnityEngine.Debug.Log("MentalCommand labels: " +SysListsStr);
            UnityEngine.Debug.Log("MentalCommand datas : " +sysEvent.Time.ToString() + " , " 
                                + sysEvent.Detection+ " , " + sysEvent.EventMessage);
            
            // TODO: emit event to other modules
            SysEventReceived(this, sysEvent);
        }
        private bool isConnectedHeadset(string headsetId) 
        {
            lock (_locker)
            {
                foreach (var item in _detectedHeadsets) {
                    if (item.HeadsetID == headsetId &&
                        item.Status == "connected"){
                        return true;
                    }
                }
                return false;
            }
            
        }

        private void CloseSession()
        {
            lock (_locker)
            {
                UnityEngine.Debug.Log("Closing Streams.");
                _wantedHeadsetId = "";
                if (_isSessActivated) {
                    _dsProcess.CloseSession();
                    Thread.Sleep(Config.TIME_CLOSE_STREAMS);
                }
            }
        }

        /// <summary>
        /// Start authorizing process.
        /// </summary>
        public void StartAuthorize(string licenseKey = "")
        {
            if (string.IsNullOrEmpty(Config.AppClientId) || 
                string.IsNullOrEmpty(Config.AppClientSecret) || 
                string.IsNullOrEmpty(Config.AppVersion) ||
                string.IsNullOrEmpty(Config.TmpAppDataDir)) {
                UnityEngine.Debug.Log(" Can not start App because invalid application configuration.");
                return;
            }
            _dsProcess.StartAuthorize(licenseKey);
        }

        /// <summary>
        /// Set up App configuration.
        /// </summary>
        public void SetAppConfig(string clientId, string clientSecret, 
                                 string appVersion, string appName, string tmpAppDataDir,
                                 string appUrl = "", string emotivAppsPath = "") 
        {
            if (string.IsNullOrEmpty(clientId) || 
                string.IsNullOrEmpty(clientSecret)) {
                UnityEngine.Debug.Log(" Invalid App configurations.");
                return;
            }
            Config.AppClientId      = clientId;
            Config.AppClientSecret  = clientSecret;
            Config.AppVersion       = appVersion;
            if (!String.IsNullOrEmpty(appUrl))
                Config.AppUrl       = appUrl;
            Config.EmotivAppsPath   = emotivAppsPath;
            Config.AppName          = appName;
            Config.TmpAppDataDir    = tmpAppDataDir;
        }

        /// <summary>
        /// Start data stream with a given headset.
        /// </summary>
        public void StartDataStream(List<string> streamNameList, string headsetId)
        {
            lock (_locker)
            {
                UnityEngine.Debug.Log("EmotivDataStream-StartDataStream: " + headsetId);
                if (!string.IsNullOrEmpty(_wantedHeadsetId)) {
                    UnityEngine.Debug.Log("The data streams has already started for headset "
                                        + _wantedHeadsetId + ". Please wait...");
                    return;
                }
                _wantedHeadsetId = headsetId;
                foreach(var curStream in streamNameList) {
                    _dsProcess.AddStreams(curStream);
                }
                // check headset connected
                if (isConnectedHeadset(headsetId)) {
                    UnityEngine.Debug.Log("The headset " + headsetId + " has already connected. Start creating session.");
                    _readyCreateSession = false;
                    _dsProcess.CreateSession(headsetId, true);
                }
                else {
                    _readyCreateSession = true;
                    _dsProcess.StartConnectToDevice(headsetId);
                }
            }
        }

        /// <summary>
        /// Subscribe data stream in lists.
        /// </summary>
        public void SubscribeMoreData(List<string> streamNameList)
        {
            if (_isSessActivated) {
                foreach(var ele in streamNameList) {
                    _dsProcess.AddStreams(ele);
                }
                // Subscribe data
                _dsProcess.SubscribeData("");
            }
            else {
                UnityEngine.Debug.Log("SubscribeMoreData: A Session has not been activated.");
            }
        }

        /// <summary>
        /// UnSubscribe data stream in lists.
        /// </summary>
        public void UnSubscribeData(List<string> streamNameList)
        {
            if (_isSessActivated) {
                // UnSubscribe data
                _dsProcess.UnSubscribe(streamNameList);
            }
            else {
                UnityEngine.Debug.Log("UnSubscribeData: A Session has not been activated.");
            }
        }

        public ConnectToCortexStates GetConnectToCortexState()
        {
            return _dsProcess.GetConnectToCortexState();
        }

        //=== Device data ===
        public double Battery()
        {
            if (_devBuff == null)
                return -1;
            else 
                return _devBuff.Battery;
        }

        public double BatteryMax()
        {
            if (_devBuff == null)
                return 0;
            else 
                return _devBuff.BatteryMax;
        }

        public double SignalStrength()
        {
            if (_devBuff == null)
                return 0;
            else 
                return _devBuff.SignalStrength;
        }

        public double GetContactQuality(Channel_t channel) {
            if (_devBuff == null)
                return 0;
            else {
                double result = _devBuff.GetContactQuality(channel);
                if (result < 0)
                    result = 0;
                return result;
            }
        }

        public double GetContactQuality(int channelId) {
            if (_devBuff == null)
                return 0;
            else
                return _devBuff.GetContactQuality(channelId);
        }

        public int GetNumberCQSamples()
        {
            if (_devBuff == null)
                return 0;

            return _devBuff.GetBufferSize();
        }

        //=== EEG data ===
        public double[] GetEEGData(Channel_t chan)
        {
            if (_eegBuff == null) {
                return null;
            }
            else 
                return _eegBuff.GetData(chan);
        }

        public int GetNumberEEGSamples()
        {
            if (_eegBuff == null)
                return 0;

            return _eegBuff.GetBufferSize();
        }

        //=== Motion data ===
        public double[] GetMotionData(Channel_t chan)
        {
            if (_motionBuff == null) {
                return null;
            }
            else 
                return _motionBuff.GetData(chan);
        }

        public List<Channel_t> GetMotionChannels()
        {
            if (_motionBuff == null) {
                return new List<Channel_t>();
            }
            else {
                return _motionBuff.DataChannels;
            }
        }

        public int GetNumberMotionSamples()
        {
            if (_motionBuff == null)
                return 0;

            return _motionBuff.GetBufferSize();
        }

        //=== Band power data ===

        public List<string> GetBandPowerLists()
        {
            if (_bandpowerBuff == null)
                return new List<string>();
            else 
                return _bandpowerBuff.BandPowerList;
        }

        public int GetNumberPowerBandSamples()
        {
            if (_bandpowerBuff == null)
                return 0;

            return _bandpowerBuff.GetBufferSize();
        }

        public double GetThetaData(Channel_t channel)
        {
            if (_bandpowerBuff == null)
                return 0;
            else 
                return _bandpowerBuff.ThetalPower(channel);
        }

        public double GetAlphaData(Channel_t channel)
        {
            if (_bandpowerBuff == null)
                return 0;
            else 
                return _bandpowerBuff.AlphaPower(channel);
        }

        public double GetLowBetaData(Channel_t channel)
        {
            if (_bandpowerBuff == null)
                return 0;
            else 
                return _bandpowerBuff.BetalLPower(channel);
        }

        public double GetHighBetaData(Channel_t channel)
        {
            if (_bandpowerBuff == null)
                return 0;
            else 
                return _bandpowerBuff.BetalHPower(channel);
        }

        public double GetGammaData(Channel_t channel)
        {
            if (_bandpowerBuff == null)
                return 0;
            else 
                return _bandpowerBuff.GamaPower(channel);
        }

        //=== Peformance metric data ===
        public List<string> GetPMLists()
        {
            if (_pmBuff == null)
                return new List<string>();
            else 
                return _pmBuff.PmList;
        }

        public int GetNumberPMSamples()
        {
            if (_pmBuff == null)
                return 0;

            return _pmBuff.GetBufferSize();
        }

        public double GetPMData(string label)
        {
            if (_pmBuff == null)
                return 0;
            else 
                return _pmBuff.GetData(label);
        }


        //=== Query headset ===
        public void QueryHeadsets(string headsetId = "") {
            _dsProcess.QueryHeadsets(headsetId);
        }

        public List<Headset> GetDetectedHeadsets() {
            lock (_locker)
            {
                return _detectedHeadsets;
            }
        }

        public void Stop() {
            // close data stream
            CloseSession();
            // stop query headset
            _dsProcess.StopQueryHeadset();
            _dsProcess.ForceCloseWebsocket();
        }
    }
}
