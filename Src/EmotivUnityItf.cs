using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmotivUnityPlugin
{
    /// <summary>
    /// EmotivUnityItf as interface for 3rd parties application work with Emotiv Unity Plugin
    /// </summary>
    public class EmotivUnityItf
    {
        private DataStreamManager _dsManager = DataStreamManager.Instance;
        private BCITraining _bciTraining = BCITraining.Instance;
        private RecordManager _recordMgr = RecordManager.Instance;
        private CortexClient   _ctxClient  = CortexClient.Instance;

        bool _isAuthorizedOK = false;
        bool _isRecording = false;

        bool _isProfileLoaded = false;
        private string _workingHeadsetId = "";
        private string _dataSubLog = ""; // data subscribing log
        private string _trainingLog = ""; // training log

        private string _messageLog = "";

        public static EmotivUnityItf Instance { get; } = new EmotivUnityItf();

        public bool IsAuthorizedOK => _isAuthorizedOK;

        public bool IsSessionCreated => _dsManager.IsSessionCreated;

        public bool IsProfileLoaded => _isProfileLoaded;

        public bool IsRecording { get => _isRecording; set => _isRecording = value; }
        public string DataSubLog { get => _dataSubLog; set => _dataSubLog = value; }
        public string TrainingLog { get => _trainingLog; set => _trainingLog = value; }
        public string MessageLog { get => _messageLog; set => _messageLog = value; }



        /// <summary>
        /// Set up App configuration.
        /// </summary>
        /// <param name="clientId">A clientId of Application.</param>
        /// <param name="clientSecret">A clientSecret of Application.</param>
        /// <param name="appVersion">Application version.</param>
        /// <param name="appName">Application name.</param>
        public void SetAppConfig(string clientId, string clientSecret,
                                 string appVersion, string appName,
                                 string appUrl = "", string emotivAppsPath = "")
        {
            _dsManager.SetAppConfig(clientId, clientSecret, appVersion, appName);
        }

        // Init
        public void Init(string clientId, string clientSecret, string appName, 
                         string appVersion = "", bool isDataBufferUsing = true)
        {
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                UnityEngine.Debug.LogError("The clientId or clientSecret is empty. Please fill them before starting.");
                return;
            }
            _dsManager.SetAppConfig(clientId, clientSecret, appVersion, appName);
            _dsManager.IsDataBufferUsing = isDataBufferUsing;
            // init bcitraining
            _bciTraining.Init();

            // binding
            _dsManager.LicenseValidTo += OnLicenseValidTo;
            _dsManager.SessionActivatedOK += OnSessionActiveOK;

            // if do not use data buffer to store data, we need to handle data stream signal
            if (!isDataBufferUsing)
            {
                _dsManager.EEGDataReceived += OnEEGDataReceived;
                _dsManager.MotionDataReceived += OnMotionDataReceived;
                _dsManager.DevDataReceived += OnDevDataReceived;
                _dsManager.PerfDataReceived += OnPerfDataReceived;
                _dsManager.BandPowerDataReceived += OnBandPowerDataReceived;
                _dsManager.InformSuccessSubscribedData += OnInformSuccessSubscribedData;

            }

            _dsManager.FacialExpReceived += OnFacialExpReceived;
            _dsManager.MentalCommandReceived += OnMentalCommandReceived;
            _dsManager.SysEventsReceived += OnSysEventsReceived;

            // bind to record manager 
            _recordMgr.informMarkerResult += OnInformMarkerResult;
            _recordMgr.informStartRecordResult += OnInformStartRecordResult;
            _recordMgr.informStopRecordResult += OnInformStopRecordResult;

            // bci training
            _bciTraining.InformLoadUnLoadProfileDone += OnInformLoadUnLoadProfileDone;
            // get error message
            _ctxClient.ErrorMsgReceived             += MessageErrorRecieved;
        }

        /// <summary>
        /// Start program: open websocket, authorize process
        /// </summary>
        public void Start()
        {
            _dsManager.StartAuthorize();
        }

        /// <summary>
        /// Stop program to clear data, stop queryHeadset
        /// </summary>
        public void Stop()
        {
            _dsManager.Stop();
            _isAuthorizedOK = false;
            _isProfileLoaded = false;
            _workingHeadsetId = "";
        }

        /// <summary>
        /// Create session with headset
        /// </summary>
        public void CreateSessionWithHeadset(string headsetId)
        {
            // start data stream without streams -> create session with the headset
            if (_isAuthorizedOK)
                _dsManager.StartDataStream(new List<string>(), headsetId);
            else
                UnityEngine.Debug.LogWarning("Please wait authorize successfully before creating session with headset " + headsetId);
        }

        /// <summary>
        /// Subscribe data
        /// </summary>
        public void SubscribeData(List<string> streamNameList)
        {
            if (_dsManager.IsSessionCreated)
                _dsManager.SubscribeMoreData(streamNameList);
            else
                UnityEngine.Debug.LogWarning("Please wait session created successfully before subscribe data ");
        }

        /// <summary>
        /// Unsubscribe data
        /// </summary>
        public void UnSubscribeData(List<string> streamNameList)
        {
            _dsManager.UnSubscribeData(streamNameList);
        }

        
        // --------Get subscribed data from buffer---------
        /// <summary>
        /// Get EEG channels lists.
        /// </summary>
        public List<Channel_t> GetEEGChannels()
        {
            return _dsManager.GetEEGChannels();
        }
        /// <summary>
        /// Get EEG data by channel.
        /// </summary>
        public double[] GetEEGData(Channel_t chan)
        {
            return _dsManager.GetEEGData(chan);
        }

        /// <summary>
        /// Get the current number of samples of a channel in eeg buffer.
        /// </summary>
        public int GetNumberEEGSamples()
        {
            return _dsManager.GetNumberEEGSamples();
        }

        // get motion data
        /// <summary>
        /// Get motion data by channel.
        /// </summary>
        public double[] GetMotionData(Channel_t chan)
        {
            return _dsManager.GetMotionData(chan);
        }

        /// <summary>
        /// Get Motion channel lists.
        /// </summary>
        public List<Channel_t> GetMotionChannels()
        {
            return _dsManager.GetMotionChannels();
        }

        /// <summary>
        /// Get the current number of samples of a channel in motion buffer.
        /// </summary>
        public int GetNumberMotionSamples()
        {
            return _dsManager.GetNumberMotionSamples();
        }
        // get band power data
        /// <summary>
        /// Get band power label lists.
        /// </summary>
        public List<string> GetBandPowerLists()
        {
            return _dsManager.GetBandPowerLists();
        }

        /// <summary>
        /// Get the current number of samples of a channel in band power buffer.
        /// </summary>
        public int GetNumberPowerBandSamples()
        {
            return _dsManager.GetNumberPowerBandSamples();
        }

        public double GetBandPower(Channel_t chan, BandPowerType _band)
        {
            switch (_band)
            {
                case BandPowerType.Thetal:
                    return _dsManager.GetThetaData(chan);
                case BandPowerType.Alpha:
                    return _dsManager.GetAlphaData(chan);
                case BandPowerType.BetalL:
                    return _dsManager.GetLowBetaData(chan);
                case BandPowerType.BetalH:
                    return _dsManager.GetHighBetaData(chan);
                case BandPowerType.Gamma:
                    return _dsManager.GetGammaData(chan);
                default:
                    return -1;
            }
        }

        //=== Peformance metric data ===
        /// <summary>
        /// Get Performance metrics label lists.
        /// </summary>
        public List<string> GetPMLists()
        {
            return _dsManager.GetPMLists();
        }

        /// <summary>
        /// Get the current number of samples of a channel in performance metric buffer.
        /// </summary>
        public int GetNumberPMSamples()
        {
            return _dsManager.GetNumberPMSamples();
        }

        /// <summary>
        /// Get peformance metric data by label.
        /// </summary>
        public double GetPMData(string label)
        {
            return _dsManager.GetPMData(label);
        }

        /// <summary>
        /// Get contact quality by channel.
        /// </summary>
        public double GetContactQuality(Channel_t channel)
        {
            return _dsManager.GetContactQuality(channel);
        }

        /// <summary>
        /// Get contact quality by channelId.
        /// </summary>
        public double GetContactQuality(int channelId)
        {
            return _dsManager.GetContactQuality(channelId);
        }

        /// <summary>
        /// Get the current number of samples of a channel in contact quality or dev buffer.
        /// </summary>
        public int GetNumberCQSamples()
        {
            return _dsManager.GetNumberCQSamples();
        }

        //--------End functions which get data from buffer--------------
        
        /// <summary>
        /// Create a record
        /// </summary>
        public void StartRecord(string title, string description = null,
                                 string subjectName = null, List<string> tags = null)
        {
            _recordMgr.StartRecord(title, description, subjectName, tags);
        }

        /// <summary>
        /// Stop current record
        /// </summary>
        public void StopRecord()
        {
            _recordMgr.StopRecord();
        }

        /// <summary>
        /// Add an instance marker
        /// </summary>
        public void InjectMarker(string markerLabel, string markerValue)
        {
            _recordMgr.InjectMarker(markerLabel, markerValue);
        }

        /// <summary>
        /// Update current instance marker
        /// </summary>
        public void UpdateMarker()
        {
            _recordMgr.UpdateMarker(); // TODO: add tags as parameter for update marker
        }

        /// <summary>
        /// Load a profile if is existed or create and load profile if it is not existed
        /// </summary>
        public void LoadProfile(string profileName)
        {
            if (!string.IsNullOrEmpty(_workingHeadsetId))
                _bciTraining.LoadProfileWithHeadset(profileName, _workingHeadsetId);
            else
                UnityEngine.Debug.LogError("LoadProfile: Please create a session with a headset first.");
        }

        /// <summary>
        /// Unload a profile
        /// </summary>
        public void UnLoadProfile(string profileName)
        {
            if (!string.IsNullOrEmpty(_workingHeadsetId))
                _bciTraining.UnLoadProfile(profileName, _workingHeadsetId);
            else
                UnityEngine.Debug.LogError("UnLoadProfile: Please create a session with a headset first.");
        }

        /// <summary>
        /// Save a profile
        /// </summary>
        public void SaveProfile(string profileName)
        {
            if (!string.IsNullOrEmpty(_workingHeadsetId))
                _bciTraining.SaveProfile(profileName, _workingHeadsetId);
            else
                UnityEngine.Debug.LogError("SaveProfile: Please create a session with a headset first.");
        }

        /// <summary>
        /// Start a Mental command Training
        /// </summary>
        public void StartMCTraining(string action)
        {
            if (_isProfileLoaded)
            {
                _bciTraining.StartTraining(action, "mentalCommand");
            }
            else
            {
                UnityEngine.Debug.LogError("Please load a profile before starting training");
            }
        }

        /// <summary>
        /// Accept a Mental command Training
        /// </summary>
        public void AcceptMCTraining()
        {
            _bciTraining.AcceptTraining("mentalCommand");
        }

        /// <summary>
        /// Reject a Mental command Training
        /// </summary>
        public void RejectMCTraining()
        {
            _bciTraining.RejectTraining("mentalCommand");
        }

        /// <summary>
        /// Erase a Mental command Training
        /// </summary>
        public void EraseMCTraining(string action)
        {
            _bciTraining.EraseTraining(action, "mentalCommand");
        }

        /// <summary>
        /// Reset Mental command Training
        /// </summary>
        public void ResetMCTraining(string action)
        {
            _bciTraining.ResetTraining(action, "mentalCommand");
        }

        // training fe
        /// <summary>
        /// Start a Facial Expression Training
        /// </summary>
        public void StartFETraining(string action)
        {
            if (_isProfileLoaded)
            {
                _bciTraining.StartTraining(action, "facialExpression");
            }
            else
            {
                UnityEngine.Debug.LogError("Please load a profile before starting training");
            }
        }

        /// <summary>
        /// Accept a Facial Expression Training
        /// </summary>
        public void AcceptFETraining()
        {
            _bciTraining.AcceptTraining("facialExpression");
        }

        /// <summary>
        /// Reject a Facial Expression Training
        /// </summary>
        public void RejectFETraining()
        {
            _bciTraining.RejectTraining("facialExpression");
        }

        /// <summary>
        /// Erase a Facial Expression Training
        /// </summary>
        public void EraseFETraining(string action)
        {
            _bciTraining.EraseTraining(action, "facialExpression");
        }

        /// <summary>
        /// Reset Facial Expression Training
        /// </summary>
        public void ResetFETraining(string action)
        {
            _bciTraining.ResetTraining(action, "facialExpression");
        }

        // other BCI APIs


        // Event handlers
        private void OnLicenseValidTo(object sender, DateTime validTo)
        {
            UnityEngine.Debug.Log("OnLicenseValidTo: the license valid to " + Utils.ISODateTimeToString(validTo));
            _isAuthorizedOK = true;
            _messageLog = "Authorizing process done.";
        }

        private void OnSessionActiveOK(object sender, string headsetId)
        {
            _workingHeadsetId = headsetId;
            _messageLog = "A session working with " + headsetId + " is activated successfully.";
        }

        private void OnInformLoadUnLoadProfileDone(object sender, bool isProfileLoaded)
        {
            _isProfileLoaded = isProfileLoaded;
            if (isProfileLoaded)
            {
                _messageLog = "The profile is loaded successfully.";
            }
            else {
                _messageLog = "The profile is unloaded successfully.";
            }
        }

        private void OnInformStartRecordResult(object sender, Record record)
        {
            UnityEngine.Debug.Log("OnInformStartRecordResult recordId: " + record.Uuid + ", title: "
                + record.Title + ", startDateTime: " + record.StartDateTime);
            _isRecording = true;
            _messageLog = "The record " + record.Title + " is created at " + record.StartDateTime;

        }

        private void OnInformStopRecordResult(object sender, Record record)
        {
            UnityEngine.Debug.Log("OnInformStopRecordResult recordId: " + record.Uuid + ", title: "
                + record.Title + ", startDateTime: " + record.StartDateTime + ", endDateTime: " + record.EndDateTime);
            _isRecording = false;
            _messageLog = "The record " + record.Title + " is ended at " + record.EndDateTime;

        }

        private void OnInformMarkerResult(object sender, JObject markerObj)
        {
            UnityEngine.Debug.Log("OnInformMarkerResult");
            _messageLog = "The marker " + markerObj["uuid"].ToString() + ", label: " 
                + markerObj["label"].ToString() + ", value: " + markerObj["value"].ToString()
                + ", type: " + markerObj["type"].ToString() + ", started at: " + markerObj["startDatetime"].ToString();
        }

        private void OnInformSuccessSubscribedData(object sender, List<string> successStreams)
        {
            string tmpText = "The streams: ";
            foreach (var item in successStreams)
            {
                tmpText = tmpText + item + "; ";
            }
            tmpText = tmpText + " are subscribed successfully. The output data will be shown on the console log.";
            _messageLog = tmpText;
        }

        // Handle events  if we do not use data buffer of Emotiv Unity Plugin
        private void OnBandPowerDataReceived(object sender, ArrayList e)
        {
            string dataText = "pow data: ";
            foreach (var item in e) {
                dataText += item.ToString() + ",";
            }
            // print out data to console
            UnityEngine.Debug.Log(dataText);
        }

        private void OnPerfDataReceived(object sender, ArrayList e)
        {
            string dataText = "met data: ";
            foreach (var item in e) {
                dataText += item.ToString() + ",";
            }
            // print out data to console
            UnityEngine.Debug.Log(dataText);
        }

        private void OnDevDataReceived(object sender, ArrayList e)
        {
            string dataText = "dev data: ";
            foreach (var item in e) {
                dataText += item.ToString() + ",";
            }
            // print out data to console
            UnityEngine.Debug.Log(dataText);
        }

        private void OnMotionDataReceived(object sender, ArrayList e)
        {
            string dataText = "mot data: ";
            foreach (var item in e) {
                dataText += item.ToString() + ",";
            }
            // print out data to console
            UnityEngine.Debug.Log(dataText);
        }

        private void OnEEGDataReceived(object sender, ArrayList e)
        {
            string dataText = "eeg data: ";
            foreach (var item in e) {
                dataText += item.ToString() + ",";
            }
            // print out data to console
            UnityEngine.Debug.Log(dataText);
        }

        private void OnSysEventsReceived(object sender, SysEventArgs data)
        {
            string dataText = "sys data: " + data.Detection + ", event: " + data.EventMessage + ", time " + data.Time.ToString();
            // print out data to console
            UnityEngine.Debug.Log(dataText);
            // show the system event to message log
            _messageLog = dataText;
        }

        private void OnMentalCommandReceived(object sender, MentalCommandEventArgs data)
        {
            string dataText = "com data: " + data.Act + ", power: " + data.Pow.ToString() + ", time " + data.Time.ToString();
            // print out data to console
            UnityEngine.Debug.Log(dataText);
        }

        private void OnFacialExpReceived(object sender, FacEventArgs data)
        {
            string dataText = "fac data: eye act " + data.EyeAct+ ", upper act: " +
                                data.UAct + ", upper act power " + data.UPow.ToString() + ", lower act: " +
                                data.LAct + ", lower act power " + data.LPow.ToString() + ", time: " + data.Time.ToString();
            // print out data to console
            UnityEngine.Debug.Log(dataText);
        }

        private void MessageErrorRecieved(object sender, ErrorMsgEventArgs errorInfo)
        {
            string message  = errorInfo.MessageError;
            string method   = errorInfo.MethodName;
            int errorCode   = errorInfo.Code;

            _messageLog = "Get Error: errorCode " + errorCode.ToString() + ", message: " + message + ", API: " + method;  
        }

    }
}
