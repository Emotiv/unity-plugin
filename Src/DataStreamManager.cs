using System;
using System.Threading;
using System.Collections;
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
        /// A wanted headset which user want to create a session and subscribe data.
        /// </summary>
        string _wantedHeadsetId  = "";
        bool _isSessActivated    = false;
        bool _readyCreateSession = false;
        bool _isDataBufferUsing = true; // use data buffer to store subscribed data for eeg, pm, cq, eq, pow, motion
        List<Headset>  _detectedHeadsets = new List<Headset>(); // list of detected headsets

        private bool _isHeadsetScanning = false;

        public event EventHandler<string> SessionActivatedOK;
        public event EventHandler<string> HeadsetConnectFail;
        public event EventHandler<DateTime> LicenseValidTo;
        public event EventHandler<bool> BTLEPermissionGrantedNotify
        {
            add { _dsProcess.BTLEPermissionGrantedNotify += value; }
            remove { _dsProcess.BTLEPermissionGrantedNotify -= value; }
        }

        public event EventHandler<List<string>> InformSuccessSubscribedData;

        // list signal if do not store data to buffer
        public event EventHandler<ArrayList> MotionDataReceived;      // motion data
        public event EventHandler<ArrayList> EEGDataReceived;         // eeg data
        public event EventHandler<ArrayList> DevDataReceived;         // contact quality
        public event EventHandler<ArrayList> PerfDataReceived;        // performance metric
        public event EventHandler<ArrayList> BandPowerDataReceived;   // band power
        public event EventHandler<FacEventArgs> FacialExpReceived;         // Facial expressions
        public event EventHandler<MentalCommandEventArgs> MentalCommandReceived;     // mental command
        public event EventHandler<SysEventArgs> SysEventsReceived;  // System events for training

        public event EventHandler<string> HeadsetScanFinished;

        private DataStreamManager()
        {
            Init();
        }

        ~DataStreamManager()
        {
        }
        public static DataStreamManager Instance { get; } = new DataStreamManager();
        public bool IsDataBufferUsing { get => _isDataBufferUsing; set => _isDataBufferUsing = value; }
        public bool IsSessionCreated {get => _isSessActivated;}

        public bool IsHeadsetScanning { get => _isHeadsetScanning;}

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
            _dsProcess.HeadsetScanFinished      += OnHeadsetScanFinished;
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
            
            UnityEngine.Debug.Log("DataStreamManager:ResetDataBuffers Done");
        }

        //====Handle events
        private void OnSessionClosedNotify(object sender, string sessionId)
        {
            lock (_locker)
            {
                UnityEngine.Debug.Log("DataStreamManager: OnSessionClosedNotify.");

                _isSessActivated    = false;
                _readyCreateSession = true;
                _wantedHeadsetId    = "";
                _isHeadsetScanning = false;
                _detectedHeadsets.Clear();
                ResetDataBuffers();
            }
        }

        private void OnUserLogoutNotify(object sender, string message)
        {
            lock (_locker)
            {
                UnityEngine.Debug.Log("DataStreamManager: OnUserLogoutNotify.");

                // reset data
                _isSessActivated    = false;
                _readyCreateSession = true;
                _wantedHeadsetId    = "";
                _isHeadsetScanning = false;
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
                //UnityEngine.Debug.Log("DataStreamManager-OnQueryHeadsetOK: " + strOut);
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
                UnityEngine.Debug.Log("DataStreamManager: OnSessionActivedOK " + sessionInfo.HeadsetId);
                if (sessionInfo.HeadsetId == _wantedHeadsetId) {
                    _isSessActivated    = true;
                    _readyCreateSession = false;
                    SessionActivatedOK(this, _wantedHeadsetId);
                    // subscribe data
                    _dsProcess.SubscribeData();
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
                        _dsProcess.FacialExpReceived -= OnFacialExpressionReceived;
                        
                    }
                    else if (streamName == DataStreamName.SysEvents) {
                        _dsProcess.SysEventsReceived -= OnSysEventReceived;

                    }
                    else if (streamName == DataStreamName.MentalCommands) {
                        _dsProcess.MentalCommandReceived -= OnMentalCommandReceived;
                    }
                    else {
                        UnityEngine.Debug.Log("DataStreamManager-OnStreamStopNotify: stream name:" + streamName);
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
                UnityEngine.Debug.Log("DataStreamManager: SubscribedOK");
                List<string> successfulStreams = new List<string>();;
                
                foreach (string key in e.Keys)
                {
                    int headerCount = e[key].Count;
                    if (key == DataStreamName.EEG) {
                        if (_isDataBufferUsing)
                        {
                            _eegBuff = new EegMotionDataBuffer();
                            _eegBuff.SetDataType(EegMotionDataBuffer.DataType.EEG);
                            _eegBuff.SettingBuffer(4, 4, headerCount);
                            _eegBuff.SetChannels(e[key]);
                            _dsProcess.EEGDataReceived += _eegBuff.OnDataReceived;
                            UnityEngine.Debug.Log("Subscribed done EEG Data Stream");                           
                        }
                        else if (_eegBuff == null) {
                            _dsProcess.EEGDataReceived += this.EEGDataReceived;
                            successfulStreams.Add(DataStreamName.EEG);
                        }
                    }
                    else if (key == DataStreamName.Motion) {
                        if (_isDataBufferUsing)
                        {
                            _motionBuff = new EegMotionDataBuffer();
                            _motionBuff.SetDataType(EegMotionDataBuffer.DataType.MOTION);
                            _motionBuff.SettingBuffer(4, 4, headerCount);
                            _motionBuff.SetChannels(e[key]);
                            _dsProcess.MotionDataReceived += _motionBuff.OnDataReceived;
                            UnityEngine.Debug.Log("Subscribed done Motion Data Stream");
                        }
                        else if (_motionBuff == null) {
                            _dsProcess.MotionDataReceived += this.MotionDataReceived;
                            successfulStreams.Add(DataStreamName.Motion);
                        }
                    }
                    else if (key == DataStreamName.BandPower) {
                        if (_isDataBufferUsing)
                        {
                            _bandpowerBuff = new BandPowerDataBuffer();
                            _bandpowerBuff.SettingBuffer(4, 4, headerCount);
                            _bandpowerBuff.SetChannels(e[key]);
                            _dsProcess.BandPowerDataReceived += _bandpowerBuff.OnBandPowerReceived;
                            UnityEngine.Debug.Log("Subscribed done: POW Stream");
                        }
                        else if (_bandpowerBuff == null){
                            _dsProcess.BandPowerDataReceived += this.BandPowerDataReceived;
                            successfulStreams.Add(DataStreamName.BandPower);
                        }
                    }
                    else if (key == DataStreamName.DevInfos) {
                        if (_isDataBufferUsing)
                        {
                            _devBuff = new DevDataBuffer();
                            _devBuff.SettingBuffer(1, 1, headerCount);
                            _devBuff.SetChannels(e[key]);
                            _dsProcess.DevDataReceived += _devBuff.OnDevDataReceived;
                            UnityEngine.Debug.Log("Subscribed done: DEV Data Stream");
                            
                        }
                        else if (_devBuff == null){
                            _dsProcess.DevDataReceived += this.DevDataReceived;
                            successfulStreams.Add(DataStreamName.DevInfos);
                        }
                    }
                    else if (key == DataStreamName.PerformanceMetrics) {
                        if (_isDataBufferUsing)
                        {
                            _pmBuff = new PMDataBuffer();
                            int count = _pmBuff.SetChannels(e[key]);
                            _pmBuff.SettingBuffer(1, 1, count);
                            _dsProcess.PerfDataReceived += _pmBuff.OnPMDataReceived;
                            UnityEngine.Debug.Log("Subscribed done: Peformance metrics Data Stream");
                            
                        }
                        else if (_pmBuff == null){
                            _dsProcess.PerfDataReceived += this.PerfDataReceived;
                            successfulStreams.Add(DataStreamName.PerformanceMetrics);
                        }
                    }
                    else if (key == DataStreamName.FacialExpressions) {
                        _dsProcess.FacialExpReceived += OnFacialExpressionReceived;
                        UnityEngine.Debug.Log("Subscribed done: Facial Expression Data Stream");
                        successfulStreams.Add(DataStreamName.FacialExpressions);
                    }
                    else if (key == DataStreamName.MentalCommands) {
                        _dsProcess.MentalCommandReceived += OnMentalCommandReceived;
                        UnityEngine.Debug.Log("Subscribed done: Mental command Data Stream");
                        successfulStreams.Add(DataStreamName.MentalCommands);
                    }
                    else if (key == DataStreamName.SysEvents) {
                        _dsProcess.SysEventsReceived += OnSysEventReceived;
                        UnityEngine.Debug.Log("Subscribed done: Sys event Stream");
                        successfulStreams.Add(DataStreamName.SysEvents);
                    }
                    else {
                        UnityEngine.Debug.Log("SubscribedOK(): stream " + key);
                    }
                }

                if (!_isDataBufferUsing) {
                    // inform data success subscribed data to EmotivUnityItf only for case not use data buffer
                    InformSuccessSubscribedData(this, successfulStreams);
                }
            }
        }

        private void OnFacialExpressionReceived(object sender, ArrayList data)
        {
            double time     = Convert.ToDouble(data[0]);
            string eyeAct   = Convert.ToString(data[1]);
            string uAct     = Convert.ToString(data[2]);
            double uPow     = Convert.ToDouble(data[3]);
            string lAct     = Convert.ToString(data[4]);
            double lPow     = Convert.ToDouble(data[5]);
            FacEventArgs facEvent = new FacEventArgs(time, eyeAct,
                                                    uAct, uPow, lAct, lPow);
            
             FacialExpReceived(this, facEvent);
        }

        private void OnMentalCommandReceived(object sender, ArrayList data)
        {

            double time     = Convert.ToDouble(data[0]);
            string act      = Convert.ToString(data[1]);
            double pow      = Convert.ToDouble(data[2]);
            MentalCommandEventArgs comEvent = new MentalCommandEventArgs(time, act, pow);

            MentalCommandReceived(this, comEvent);
        }

        private void OnSysEventReceived(object sender, ArrayList data)
        {
            double time         = Convert.ToDouble(data[0]);
            string detection    = Convert.ToString(data[1]);
            string eventMsg     = Convert.ToString(data[2]);
            SysEventArgs sysEvent = new SysEventArgs(time, detection, eventMsg);
            
             SysEventsReceived(this, sysEvent);
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

        private void OnHeadsetScanFinished(object sender, string message)
        {
            UnityEngine.Debug.Log(message);
            // Reset _isHeadsetScanning when get headset scan finished
            _isHeadsetScanning = false;
        }

        /// <summary>
        /// Start authorizing process.
        /// </summary>
        public void StartAuthorize(string licenseKey = "")
        {
            if (string.IsNullOrEmpty(Config.AppClientId) || 
                string.IsNullOrEmpty(Config.AppClientSecret) || 
                string.IsNullOrEmpty(Config.AppVersion)) {
                UnityEngine.Debug.Log(" Can not start App because invalid application configuration.");
                return;
            }
            _dsProcess.StartAuthorize(licenseKey);
        }

        /// <summary>
        /// Set up App configuration.
        /// </summary>
        /// <param name="clientId">A clientId of Application.</param>
        /// <param name="clientSecret">A clientSecret of Application.</param>
        /// <param name="appVersion">Application version.</param>
        /// <param name="appName">Application name.</param>
        /// <param name="tmpAppDataDir">Name of temp application data directory where will keep logs and user data .</param>
        public void SetAppConfig(string clientId, string clientSecret, 
                                 string appVersion="", string appName="", string tmpAppDataDir="",
                                 string appUrl = "", string emotivAppsPath = "") 
        {
            if (string.IsNullOrEmpty(clientId)) {
                UnityEngine.Debug.LogError("Empty clientId. Please fill in the clientId before running.");
                return;
            }
            if (string.IsNullOrEmpty(clientSecret))
            {
                UnityEngine.Debug.LogError("Empty clientSecret. Please fill in the clientId before running.");
                return;
            }

            Config.AppClientId      = clientId;
            Config.AppClientSecret  = clientSecret;

            if (!string.IsNullOrEmpty(appVersion))
                Config.AppVersion       = appVersion;
            if (!string.IsNullOrEmpty(appUrl))
                Config.AppUrl       = appUrl;
            if (!string.IsNullOrEmpty(emotivAppsPath))
                Config.EmotivAppsPath = emotivAppsPath;
            if (!string.IsNullOrEmpty(appName))
                Config.AppName = appName;
            if (!string.IsNullOrEmpty(tmpAppDataDir))
                Config.TmpAppDataDir = tmpAppDataDir;
        }

        /// <summary>
        /// Start data stream with a given headset.
        /// </summary>
        /// <param name="streamNameList">Lists of data streams you want to subscribe.</param>
        /// <param name="headsetId">the id of headset you want to retrieve data.</param>
        public void StartDataStream(List<string> streamNameList, string headsetId)
        {
            lock (_locker)
            {
                if (!string.IsNullOrEmpty(_wantedHeadsetId)) {
                    UnityEngine.Debug.Log("The data streams has already started for headset "
                                        + _wantedHeadsetId + ". Please wait...");
                    return;
                }

                if (string.IsNullOrEmpty(headsetId)) {
                    if (_detectedHeadsets.Count > 0) {
                        // get the first headset in the list
                        _wantedHeadsetId = _detectedHeadsets[0].HeadsetID;
                    }
                    else {
                        UnityEngine.Debug.LogError("No headset available.");
                        return;
                    }

                }
                else {
                    bool _foundHeadset = false;
                    foreach (var item in _detectedHeadsets) {
                        if (item.HeadsetID == headsetId){
                            _foundHeadset = true;
                        }
                    }
                    if (_foundHeadset) {
                        _wantedHeadsetId = headsetId;
                    }
                    else {
                        UnityEngine.Debug.LogError("The headset " + headsetId + " is not found. Please check the headset Id.");
                        return;
                    }
                }

                UnityEngine.Debug.Log("DataStreamManager-StartDataStream: " + _wantedHeadsetId);

                foreach(var curStream in streamNameList) {
                    _dsProcess.AddStreams(curStream);
                }
                // check headset connected
                if (isConnectedHeadset(_wantedHeadsetId)) {
                    UnityEngine.Debug.Log("The headset " + _wantedHeadsetId + " has already connected. Start creating session.");
                    _readyCreateSession = false;
                    _dsProcess.CreateSession(_wantedHeadsetId, true);
                }
                else {
                    _readyCreateSession = true;
                    _dsProcess.StartConnectToDevice(_wantedHeadsetId);
                }
            }
        }

        /// <summary>
        /// Subscribe data stream in lists.
        /// </summary>
        /// <param name="streamNameList">Lists of data streams you want to subscribe.</param>
        public void SubscribeMoreData(List<string> streamNameList)
        {
            if (_isSessActivated) {
                foreach(var ele in streamNameList) {
                    _dsProcess.AddStreams(ele);
                }
                // Subscribe data
                _dsProcess.SubscribeData();
            }
            else {
                UnityEngine.Debug.Log("SubscribeMoreData: A Session has not been activated.");
            }
        }

        /// <summary>
        /// UnSubscribe data stream in lists.
        /// </summary>
        /// <param name="streamNameList">Lists of data streams you want to unsubscribe.</param>
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

        /// <summary>
        /// Get Connect To Cortex State which show the state when connect Cortex and authorize Application
        /// </summary>
        public ConnectToCortexStates GetConnectToCortexState()
        {
            return _dsProcess.GetConnectToCortexState();
        }

        public string GetConnectToCortexWarningMessage()
        {
            return _dsProcess.GetConnectToCortexWarningMessage();
        }

        //=== Device data ===
        /// <summary>
        /// Get battery level.
        /// </summary>
        public double Battery()
        {
            if (_devBuff == null)
                return -1;
            else 
                return _devBuff.Battery;
        }

        /// <summary>
        /// Get battery maximum level.
        /// </summary>
        public double BatteryMax()
        {
            if (_devBuff == null)
                return 0;
            else 
                return _devBuff.BatteryMax;
        }

        /// <summary>
        /// Get wireless signal strength.
        /// </summary>
        public double SignalStrength()
        {
            if (_devBuff == null)
                return 0;
            else 
                return _devBuff.SignalStrength;
        }

        /// <summary>
        /// Get contact quality by channel.
        /// </summary>
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

        /// <summary>
        /// Get contact quality by channelId.
        /// </summary>
        public double GetContactQuality(int channelId) {
            if (_devBuff == null)
                return 0;
            else
                return _devBuff.GetContactQuality(channelId);
        }

        /// <summary>
        /// Get the current number of samples of a channel in contact quality or dev buffer.
        /// </summary>
        public int GetNumberCQSamples()
        {
            if (_devBuff == null)
                return 0;

            return _devBuff.GetBufferSize();
        }

        //=== EEG data ===

        /// <summary>
        /// Get EEG channels lists.
        /// </summary>
        public List<Channel_t> GetEEGChannels()
        {
            if (_eegBuff == null) {
                return new List<Channel_t>();
            }
            else {
                return _eegBuff.DataChannels;
            }
        }
        
        /// <summary>
        /// Get EEG data by channel.
        /// </summary>
        public double[] GetEEGData(Channel_t chan)
        {
            if (_eegBuff == null) {
                return null;
            }
            else 
                return _eegBuff.GetData(chan);
        }

        /// <summary>
        /// Get the current number of samples of a channel in eeg buffer.
        /// </summary>
        public int GetNumberEEGSamples()
        {
            if (_eegBuff == null)
                return 0;

            return _eegBuff.GetBufferSize();
        }

        //=== Motion data ===
        /// <summary>
        /// Get motion data by channel.
        /// </summary>
        public double[] GetMotionData(Channel_t chan)
        {
            if (_motionBuff == null) {
                return null;
            }
            else 
                return _motionBuff.GetData(chan);
        }

        /// <summary>
        /// Get Motion channel lists.
        /// </summary>
        public List<Channel_t> GetMotionChannels()
        {
            if (_motionBuff == null) {
                return new List<Channel_t>();
            }
            else {
                return _motionBuff.DataChannels;
            }
        }

        /// <summary>
        /// Get the current number of samples of a channel in motion buffer.
        /// </summary>
        public int GetNumberMotionSamples()
        {
            if (_motionBuff == null)
                return 0;

            return _motionBuff.GetBufferSize();
        }

        //=== Band power data ===

        /// <summary>
        /// Get band power label lists.
        /// </summary>
        public List<string> GetBandPowerLists()
        {
            if (_bandpowerBuff == null)
                return new List<string>();
            else 
                return _bandpowerBuff.BandPowerList;
        }

        /// <summary>
        /// Get the current number of samples of a channel in band power buffer.
        /// </summary>
        public int GetNumberPowerBandSamples()
        {
            if (_bandpowerBuff == null)
                return 0;

            return _bandpowerBuff.GetBufferSize();
        }

        /// <summary>
        /// Get Theta data by channel.
        /// </summary>
        public double GetThetaData(Channel_t channel)
        {
            if (_bandpowerBuff == null)
                return 0;
            else 
                return _bandpowerBuff.ThetalPower(channel);
        }

        /// <summary>
        /// Get alpha data by channel.
        /// </summary>
        public double GetAlphaData(Channel_t channel)
        {
            if (_bandpowerBuff == null)
                return 0;
            else 
                return _bandpowerBuff.AlphaPower(channel);
        }

        /// <summary>
        /// Get low beta data by channel.
        /// </summary>
        public double GetLowBetaData(Channel_t channel)
        {
            if (_bandpowerBuff == null)
                return 0;
            else 
                return _bandpowerBuff.BetalLPower(channel);
        }

        /// <summary>
        /// Get high beta data by channel.
        /// </summary>
        public double GetHighBetaData(Channel_t channel)
        {
            if (_bandpowerBuff == null)
                return 0;
            else 
                return _bandpowerBuff.BetalHPower(channel);
        }

        /// <summary>
        /// Get gama data by channel.
        /// </summary>
        public double GetGammaData(Channel_t channel)
        {
            if (_bandpowerBuff == null)
                return 0;
            else 
                return _bandpowerBuff.GamaPower(channel);
        }

        //=== Peformance metric data ===
        /// <summary>
        /// Get Performance metrics label lists.
        /// </summary>
        public List<string> GetPMLists()
        {
            if (_pmBuff == null)
                return new List<string>();
            else 
                return _pmBuff.PmList;
        }

        /// <summary>
        /// Get the current number of samples of a channel in performance metric buffer.
        /// </summary>
        public int GetNumberPMSamples()
        {
            if (_pmBuff == null)
                return 0;

            return _pmBuff.GetBufferSize();
        }

        /// <summary>
        /// Get peformance metric data by label.
        /// </summary>
        public double GetPMData(string label)
        {
            if (_pmBuff == null)
                return 0;
            else 
                return _pmBuff.GetData(label);
        }

        /// <summary>
        /// Query headsets.
        /// </summary>
        public void QueryHeadsets(string headsetId = "") {
            _dsProcess.QueryHeadsets(headsetId);
        }

        /// <summary>
        /// Get detected headsets.
        /// </summary>
        public List<Headset> GetDetectedHeadsets() {
            lock (_locker)
            {
                return _detectedHeadsets;
            }
        }

        /// <summary>
        /// Close session and stop websocket client.
        /// </summary>
        public void Stop() {
            // close data stream
            CloseSession();
            // stop query headset
            _dsProcess.StopQueryHeadset();
            _dsProcess.ForceCloseWebsocket();
        }

        /// <summary>
        /// Get current wanted headset.
        /// </summary>
        public string GetWantedHeadset() {
            lock (_locker)
            {
                return _wantedHeadsetId;
            }
        }

        /// <summary>
        /// ScanHeadsets to trigger scan headsets from Cortex
        /// </summary>
        public void ScanHeadsets() {
            UnityEngine.Debug.Log("Start scanning headset.");
            _isHeadsetScanning = true;
            _dsProcess.RefreshHeadset();
        }
    }
}
