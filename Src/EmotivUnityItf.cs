using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

#if USE_EMBEDDED_LIB || UNITY_ANDROID || UNITY_IOS
using Cdm.Authentication.Browser;
using Cdm.Authentication.OAuth2;
using System.Threading;
using System.Security.Cryptography;
using System.Text;
using Cdm.Authentication.Clients;
using Newtonsoft.Json;
#endif

namespace EmotivUnityPlugin
{
    
    public class MentalComm{
        public string act = "NULL";
        public double pow = 0;
    }

    public class BatteryInfo {
        public double batteryLeft = 0;
        public double batteryRight = 0;
        public double overallBattery = 0;
        public bool isTwoSideBatteryType = false;
        public double batteryMaxLevel = 0;
    }

    /// <summary>
    /// <para>
    /// <b>EmotivUnityItf</b> is an interface class for Unity applications to interact with the Emotiv Cortex API. 
    /// It supports both Emotiv Cortex Service (desktop only, without USE_EMBEDDED_LIB) 
    /// and Emotiv embedded library (when USE_EMBEDDED_LIB is defined (desktop) or mobile platform).
    /// The class provides unified access for cortex connection creation, authorization, headset management, 
    /// session creation, data subscription, recording, training, and profile management.
    /// </para>
    /// </summary>
    public class EmotivUnityItf
    {
        private DataStreamManager _dsManager = DataStreamManager.Instance;
        private BCITraining _bciTraining = BCITraining.Instance;
        private RecordManager _recordMgr = RecordManager.Instance;
        private Authorizer     _authorizer = Authorizer.Instance;

        bool _isAuthorizedOK = false;
        bool _isRecording = false;
        Record _recentRecord = null; // the most recent record created and just stopped
        bool _isAutoAcceptTraining = false;

        bool _isAutoSaveProfile = false;

        bool _isMCTrainingCompleted = false;

        bool _isMCTrainingSuccess = false;

        bool _isProfileLoaded = false;
        string _loadedProfileName = "";
        bool _isSupportedDeviceForProfile = true; // true if the current headset is supported for the profile

        private string _workingHeadsetId = "";
        private string _dataSubLog = ""; // data subscribing log
        private string _trainingLog = ""; // training log

        private string _messageLog = "";

        private bool _isWebViewOpened = false;
        private List<int> _mentalCommandActionSensitivity = new List<int>();

        // trained signature actions
        private Dictionary<string, int> _trainedSignatureActions = new Dictionary<string, int>();

        private List<string> _desiredErasingProfiles = new List<string>(); // desired profiles to erase

        // date having consumer data
        private List<DateTime> _datesHavingConsumerData = new List<DateTime>();

        private List<MentalStateModel> _mentalStateDatas = new List<MentalStateModel>();

        public static EmotivUnityItf Instance { get; } = new EmotivUnityItf();

        public bool IsAuthorizedOK => _isAuthorizedOK;

        public bool IsSessionCreated => _dsManager.IsSessionCreated;

        public bool IsProfileLoaded => _isProfileLoaded;

        public bool IsRecording { get => _isRecording; set => _isRecording = value; }
        public string DataSubLog { get => _dataSubLog; set => _dataSubLog = value; }
        public string TrainingLog { get => _trainingLog; set => _trainingLog = value; }
        public string MessageLog { get => _messageLog; set => _messageLog = value; }
        public MentalComm LatestMentalCommand { get; private set; } = new MentalComm();
        public bool IsMCTrainingCompleted { get => _isMCTrainingCompleted; set => _isMCTrainingCompleted = value; }
        public bool IsMCTrainingSuccess { get => _isMCTrainingSuccess; set => _isMCTrainingSuccess = value; }
        public List<string> DesiredErasingProfiles { get => _desiredErasingProfiles; set => _desiredErasingProfiles = value; }
        public List<int> MentalCommandActionSensitivity { get => _mentalCommandActionSensitivity; set => _mentalCommandActionSensitivity = value; }
        public List<DateTime> DatesHavingConsumerData { get => _datesHavingConsumerData; set => _datesHavingConsumerData = value; }
        public List<MentalStateModel> MentalStateDatas { get => _mentalStateDatas; set => _mentalStateDatas = value; }
        public bool IsWebViewOpened { get => _isWebViewOpened; set => _isWebViewOpened = value; }
        public string LoadedProfileName { get => _loadedProfileName; set => _loadedProfileName = value; }
        public string WorkingHeadsetId { get => _workingHeadsetId; set => _workingHeadsetId = value; }
        public Record RecentRecord { get => _recentRecord; set => _recentRecord = value; }
        public bool IsSupportedDeviceForProfile { get => _isSupportedDeviceForProfile; set => _isSupportedDeviceForProfile = value; }

        // Events
        public event EventHandler<ErrorMsgEventArgs> ErrorMsgReceived;

        /// <summary>
        /// Gets the current Emotiv ID of the logged-in user.
        /// </summary>
        /// <returns>The current Emotiv ID, or empty string if not logged in.</returns>
        public string GetCurrentEmotivId()
        {
            return _authorizer.CurrentEmotivId;
        }


#if USE_EMBEDDED_LIB || UNITY_ANDROID || UNITY_IOS
        private CrossPlatformBrowser _crossPlatformBrowser;
        private AuthenticationSession _authenticationSession;
        private CancellationTokenSource _cancellationTokenSource; 
        
        private static readonly char[] HEX_ARRAY = "0123456789abcdef".ToCharArray();

        #endif

        #if USE_EMBEDDED_LIB && UNITY_STANDALONE_WIN && !UNITY_EDITOR
        public  async Task ProcessCallback(string args)
        {
            await WindowsSystemBrowser.ProcessCallback(args);
        }
        #endif

        /// <summary>
        /// Logs in with an authentication code. Which used for unity example with embedded Cortex on Windows.
        /// </summary>
        /// <param name="code">The authentication code.</param>
        public void LoginWithAuthenticationCode(string code) {
            _dsManager.LoginWithAuthenticationCode(code);
        }
        
        /// <summary>
        /// Gets the list of detected headsets.
        /// </summary>
        /// <returns>A list of detected headsets.</returns>
        public List<Headset> GetDetectedHeadsets()
        {
            return _dsManager.GetDetectedHeadsets();
        }

        /// <summary>
        /// Gets the number of training times for a specific action.
        /// </summary>
        /// <param name="action">The action to get the training times for.</param>
        /// <returns>The number of training times for the specified action.</returns>
        public int GetTrainingTimeForAction(string action)
        {
            return _trainedSignatureActions.ContainsKey(action) ? _trainedSignatureActions[action] : 0;
        }

        /// <summary>
        /// Gets the list of profiles.
        /// </summary>
        /// <returns>A list of profiles.</returns>
        public List<string> GetProfileList()
        {
            return _bciTraining.ProfileLists;
        }

        /// <summary>
        /// Gets the connection state of the desired connecting headset.
        /// </summary>
        /// <returns>The connection state of the headset.</returns>
        public ConnectHeadsetStates GetConnectHeadsetState()
        {
            return _dsManager.ConnectHeadsetState;
        }

        /// <summary>
        /// Gets the state of application in authorizing process. The Authorized state is true if the application is authorized successfully.
        /// </summary>
        /// <returns>The state of application.</returns>
        public ConnectToCortexStates GetConnectToCortexState()
        {
            return _dsManager.GetConnectToCortexState();
        }

        /// <summary>
        /// Initializes the Emotiv Unity Interface.
        /// </summary>
        /// <param name="clientId">The client ID of the application.</param>
        /// <param name="clientSecret">The client secret of the application.</param>
        /// <param name="appName">The name of the application. the Appname must not be empty</param>
        /// <param name="allowSaveLogToFile">Set to true whether to save log and token to file or not.
        /// <param name="isDataBufferUsing"> Set to true whether to use data buffer to store data before get from Unity script. 
        ///                Otherwise, the subscribing data only are handled on xyDataReceived() and displayed on Message Log   </param>
        /// <param name="appUrl">The URL of the application (optional). Only for desktop version and work with Cortex Service.</param>
        /// <param name="providerName">The provider name (optional). Used to create a subdirectory in the application data directory.</param>
        /// <param name="emotivAppsPath">The path to Emotiv Launcher file path (optional). Only for desktop version and work with Cortex Service. </param>
        public void Init(string clientId, string clientSecret, string appName,
                         bool allowSaveLogToFile = true, bool isDataBufferUsing = true,
                         string appUrl = "", string providerName = "", string emotivAppsPath = "")
        {
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                UnityEngine.Debug.LogError("The clientId or clientSecret is empty. Please fill them before starting.");
                return;
            }

            if (string.IsNullOrEmpty(appName))
            {
                UnityEngine.Debug.LogError("The appName is empty. Please fill it before starting.");
                return;
            }

            // init configuration
            Config.Init(clientId, clientSecret, appName, allowSaveLogToFile, appUrl, providerName, emotivAppsPath);

            // init logger
            MyLogger.Instance.Init(appName, allowSaveLogToFile);

            // init authentication for Android and Embedded Cortex Desktop
            #if UNITY_ANDROID || USE_EMBEDDED_LIB || UNITY_IOS
            InitForAuthentication(clientId, clientSecret);
            #endif

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
                _dsManager.EQDataReceived += OnEQDataReceived;
                _dsManager.PerfDataReceived += OnPerfDataReceived;
                _dsManager.BandPowerDataReceived += OnBandPowerDataReceived;
                _dsManager.InformSuccessSubscribedData += OnInformSuccessSubscribedData;
            }
            _dsManager.MessageQueryHeadsetOK += OnMessageQueryHeadsetOK;
            _dsManager.FacialExpReceived += OnFacialExpReceived;
            _dsManager.MentalCommandReceived += OnMentalCommandReceived;
            _dsManager.SysEventsReceived += OnSysEventsReceived;
            _dsManager.HeadsetConnectFail += OnHeadsetConnectFail;
            _dsManager.UserLogoutNotify += OnUserLogoutNotify;
            _dsManager.StreamStopNotify += OnStreamStopNotify;
            _dsManager.QueryDatesHavingConsumerDataDone += OnQueryDatesHavingConsumerDataDone;
            _dsManager.QueryDayDetailOfConsumerDataDone += OnQueryDayDetailOfConsumerDataDone;
            _dsManager.BTLEPermissionGrantedNotify += onBTLEPermissionGrantedNotify;

            // bind to record manager 
            _recordMgr.informMarkerResult += OnInformMarkerResult;
            _recordMgr.informStartRecordResult += OnInformStartRecordResult;
            _recordMgr.informStopRecordResult += OnInformStopRecordResult;
            _recordMgr.DataPostProcessingFinished += OnDataPostProcessingFinished;
            _recordMgr.ExportRecordsFinished += OnExportRecordsFinished;

            // bci training
            _bciTraining.InformLoadProfileDone += OnInformLoadProfileDone;
            _bciTraining.InformUnLoadProfileDone += OnInformUnLoadProfileDone;
            _bciTraining.SetMentalCommandActionSensitivityOK += OnSetMentalCommandActionSensitivityOK;
            _bciTraining.InformEraseDone += OnInformEraseDone;
            _bciTraining.InformTrainedSignatureActions += OnInformTrainedSignatureActions;
            _bciTraining.ProfileSavedOK += OnProfileSavedOK;
            _bciTraining.GetMentalCommandActionSensitivityOK += OnGetMentalCommandActionSensitivityOK;
            _bciTraining.InformUnsupportedDeviceForProfile += OnInformUnsupportedDeviceForProfile;
            // get error message
            _authorizer.ErrorMsgReceived += MessageErrorRecieved;
            _authorizer.LicenseExpired += OnLicenseExpired;
            _authorizer.NoAccessRightNotify += OnNoAccessRightNotify;
        }

        private void OnInformUnsupportedDeviceForProfile(object sender, string profileName)
        {
            _isSupportedDeviceForProfile = false;
        }

        /// <summary>
        /// If work with Cortex Service, the function will open socket then start authorizing process
        /// In the case work with Embedded Cortex library, It will load library then start connecting and authorizing process.
        /// </summary>
        /// <param name="context">It should be set the current activity in the case Android platform.</param>
        public void Start(object context = null)
        {
            _dsManager.StartAuthorize("", context);
        }

        /// <summary>
        /// Stops the program to clear data and stop querying headsets.
        /// </summary>
        public void Stop()
        {
            #if USE_EMBEDDED_LIB
            _cancellationTokenSource?.Cancel();
            _authenticationSession?.Dispose();
            #endif
            #if UNITY_ANDROID || UNITY_IOS
            UniWebViewManager.Instance?.Cleanup();
            #endif

            _dsManager.Stop();
            ClearData();
        }

        /// <summary>
        /// Logs out the current user.
        /// </summary>
        public void Logout()
        {
            _dsManager.Logout();
        }

        /// <summary>
        /// Retry authorization process
        /// </summary>
        public void RetryAuthorize()
        {
            _authorizer.RetryAuthorize();
        }

        // accept eula and privacy policy
        public void AcceptEulaAndPrivacyPolicy()
        {
            _dsManager.AcceptEulaAndPrivacyPolicy();
        }

        /// <summary>
        /// Queries the headsets.
        /// </summary>
        /// <param name="headsetId">The headset id of specific headset if want get headset information of a specific headset. 
        ///                         If use empty string it will query all headsets</param>
        public void QueryHeadsets(string headsetId = "") {
            _dsManager.QueryHeadsets(headsetId);
        }

        /// <summary>
        /// Creates a session with a specific headset.
        /// </summary>
        /// <param name="headsetId">The headset ID.</param>
        public void CreateSessionWithHeadset(string headsetId)
        {
            // start data stream without streams -> create session with the headset
            if (_isAuthorizedOK)
                _dsManager.StartDataStream(new List<string>(), headsetId);
            else
                UnityEngine.Debug.LogWarning("Please wait authorize successfully before creating session with headset " + headsetId);
        }

        /// <summary>
        /// A compromise function to create a session with a specific headset then subscribe data
        /// </summary>
        /// <param name="streamNameList">The list of stream names.</param>
        /// <param name="headsetId">The headset ID.</param>
        public void StartDataStream(List<string> streamNameList, string headsetId)
        {
            // check authorized
            if (!_isAuthorizedOK)
            {
                UnityEngine.Debug.LogWarning("Please wait authorize successfully before starting data stream");
                return;
            }
            _dsManager.StartDataStream(streamNameList, headsetId);
        }

        /// <summary>
        /// Subscribes to data streams.
        /// </summary>
        /// <param name="streamNameList">The list of stream names to subscribe to.</param>
        public void SubscribeData(List<string> streamNameList)
        {
            if (_dsManager.IsSessionCreated)
                _dsManager.SubscribeMoreData(streamNameList);
            else
                UnityEngine.Debug.LogWarning("Please wait session created successfully before subscribe data ");
        }

        /// <summary>
        /// Unsubscribes from data streams.
        /// </summary>
        /// <param name="streamNameList">The list of stream names to unsubscribe from.</param>
        public void UnSubscribeData(List<string> streamNameList)
        {
            _dsManager.UnSubscribeData(streamNameList);
        }

        // --------Get subscribed data from buffer---------
        /// <summary>
        /// Gets the list of EEG channels.
        /// </summary>
        /// <returns>A list of EEG channels.</returns>
        public List<Channel_t> GetEEGChannels()
        {
            return _dsManager.GetEEGChannels();
        }

        /// <summary>
        /// Gets the current number of samples of a channel in the EEG buffer.
        /// </summary>
        /// <returns>The number of EEG samples in the data buffer.</returns>
        public int GetNumberEEGSamples()
        {
            return _dsManager.GetNumberEEGSamples();
        }

        /// <summary>
        /// Gets EEG data by channel. Should call after check GetNumberEEGSamples() > 0 to make sure the data is ready.
        /// </summary>
        /// <param name="chan">The EEG channel.</param>
        /// <returns>An array of EEG data.</returns>
        public double[] GetEEGData(Channel_t chan)
        {
            return _dsManager.GetEEGData(chan);
        }

        /// <summary>
        /// Gets the list of motion channels.
        /// </summary>
        /// <returns>A list of motion channels.</returns>
        public List<Channel_t> GetMotionChannels()
        {
            return _dsManager.GetMotionChannels();
        }

        /// <summary>
        /// Gets the current number of samples of a channel in the Motion buffer.
        /// </summary>
        /// <returns>The number of motion samples.</returns>
        public int GetNumberMotionSamples()
        {
            return _dsManager.GetNumberMotionSamples();
        }

        /// <summary>
        /// Gets motion data by channel. Should call after check GetNumberMotionSamples() > 0 to make sure the data is ready.
        /// </summary>
        /// <param name="chan">The motion channel.</param>
        /// <returns>An array of motion data.</returns>
        public double[] GetMotionData(Channel_t chan)
        {
            return _dsManager.GetMotionData(chan);
        }

        /// <summary>
        /// Gets the list of band power labels.
        /// </summary>
        /// <returns>A list of band power labels.</returns>
        public List<string> GetBandPowerLists()
        {
            return _dsManager.GetBandPowerLists();
        }

        /// <summary>
        /// Gets the current number of samples of a channel in the band power buffer.
        /// </summary>
        /// <returns>The number of band power samples.</returns>
        public int GetNumberPowerBandSamples()
        {
            return _dsManager.GetNumberPowerBandSamples();
        }

        /// <summary>
        /// Gets the band power data for a specific channel and band. Should call after check GetNumberPowerBandSamples() > 0 to make sure the data is ready.
        /// </summary>
        /// <param name="chan">The channel.</param>
        /// <param name="band">The band power type.</param>
        /// <returns>The band power data.</returns>
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

        /// <summary>
        /// Gets the list of performance metrics labels.
        /// </summary>
        /// <returns>A list of performance metrics labels.</returns>
        public List<string> GetPMLists()
        {
            return _dsManager.GetPMLists();
        }

        /// <summary>
        /// Gets the current number of samples of a channel in the performance metrics buffer.
        /// </summary>
        /// <returns>The number of performance metrics samples.</returns>
        public int GetNumberPMSamples()
        {
            return _dsManager.GetNumberPMSamples();
        }

        /// <summary>
        /// Gets performance metrics data by label. Should call after check GetNumberPMSamples() > 0 to make sure the data is ready.
        /// </summary>
        /// <param name="label">The performance metrics label.</param>
        /// <returns>The performance metrics data.</returns>
        public double GetPMData(string label)
        {
            return _dsManager.GetPMData(label);
        }

        
        /// <summary>
        /// Gets the current number of samples of a channel in the contact quality or device buffer.
        /// </summary>
        /// <returns>The number of contact quality samples.</returns>
        public int GetNumberCQSamples()
        {
            return _dsManager.GetNumberCQSamples();
        }

        /// <summary>
        /// Gets the contact quality by channel. Should call after check GetNumberCQSamples() > 0 to make sure the data is ready.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <returns>The contact quality data.</returns>
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
        /// Gets the battery level. If the headset is two-side battery type, it will return the minimum battery level of two sides.
        /// </summary>
        /// <returns>The battery information.</returns>
        public BatteryInfo GetBattery()
        {
            BatteryInfo batteryInfo = new BatteryInfo();
            batteryInfo.batteryLeft = _dsManager.BatteryLeft();
            batteryInfo.batteryRight = _dsManager.BatteryRight();
            batteryInfo.isTwoSideBatteryType = (batteryInfo.batteryLeft >= 0)  || (batteryInfo.batteryRight >= 0);
            if  (batteryInfo.isTwoSideBatteryType) {
                batteryInfo.overallBattery = Math.Min(batteryInfo.batteryLeft, batteryInfo.batteryRight);
            }
            else {
                batteryInfo.overallBattery = _dsManager.Battery();
            }
            return batteryInfo;
        }
        
        
        /// <summary>
        /// Gets the number of EEG quality samples.
        /// </summary>
        /// <returns>The number of EEG quality samples.</returns>
        public int GetNumberEQSamples()
        {
            return _dsManager.GetNumberEQSamples();
        }

        /// <summary>
        /// Gets the EEG quality data for a specific channel. Should call after check GetNumberEQSamples() > 0 to make sure the data is ready.
        /// </summary>
        /// <param name="channel">The EEG channel.</param>
        /// <returns>The EEG quality data.</returns>
        public double GetEQ(Channel_t channel)
        {
            return _dsManager.GetEQ(channel);
        }
        
        /// <summary>
        /// Create a record.
        /// </summary>
        /// <param name="title">The title of the record.</param>
        /// <param name="description">The description of the record (optional).</param>
        /// <param name="subjectName">The subject name (optional).</param>
        /// <param name="tags">The tags associated with the record (optional).</param>
        public void StartRecord(string title, string description = null,
                                string subjectName = null, List<string> tags = null)
        {
            _recordMgr.StartRecord(title, description, subjectName, tags);
        }

        /// <summary>
        /// Stops the current recording.
        /// </summary>
        public void StopRecord()
        {
            _recordMgr.StopRecord();
        }


        /// <summary>
        /// Export one or more records to a specified folder with customizable options.
        /// </summary>
        /// <param name="records">List of record UUIDs to export</param>
        /// <param name="folderPath">Absolute path to the folder for exported files</param>
        /// <param name="streamTypes">List of stream types to include (e.g., "EEG", "MOTION")</param>
        /// <param name="format">Export file format ("EDF", "EDFPLUS", "BDFPLUS", "CSV")</param>
        /// <param name="version">Optional. For "CSV" format, use "V1" or "V2"</param>
        /// <param name="licenseIds">Optional. License IDs for exporting records from other apps</param>
        /// <param name="includeDemographics">Include demographic info</param>
        /// <param name="includeMarkerExtraInfos">Include extra marker info</param>
        /// <param name="includeSurvey">Include survey data</param>
        /// <param name="includeDeprecatedPM">Include deprecated performance metrics</param>
        /// <remarks>See https://emotiv.gitbook.io/cortex-api/records/exportrecord for details</remarks>
        public void ExportRecord(List<string> records, string folderPath,
                                 List<string> streamTypes, string format, string version = null,
                                 List<string> licenseIds = null, bool includeDemographics = false,
                                 bool includeMarkerExtraInfos = false, bool includeSurvey = false,
                                 bool includeDeprecatedPM = false)
        {
            _recordMgr.ExportRecord(records, folderPath, streamTypes, format, version,
                                     licenseIds, includeDemographics, includeMarkerExtraInfos,
                                     includeSurvey, includeDeprecatedPM);
        }

        /// <summary>
        /// Injects an instance marker into the current record.
        /// </summary>
        /// <param name="markerLabel">The label of the marker.</param>
        /// <param name="markerValue">The value of the marker.</param>
        public void InjectMarker(string markerLabel, string markerValue)
        {
            _recordMgr.InjectMarker(markerLabel, markerValue);
        }

        /// <summary>
        /// Updates the current marker to make it is interval marker with the end time is current time.
        /// </summary>
        public void UpdateMarker()
        {
            _recordMgr.UpdateMarker(); // TODO: add tags as parameter for update marker
        }

        /// <summary>
        /// Loads a profile if it exists or creates and loads a profile if it does not exist.
        /// </summary>
        /// <param name="profileName">The name of the profile.</param>
        public void LoadProfile(string profileName)
        {
            if (!string.IsNullOrEmpty(_workingHeadsetId))
                _bciTraining.LoadProfileWithHeadset(profileName, _workingHeadsetId);
            else
                UnityEngine.Debug.LogError("LoadProfile: Please create a session with a headset first.");
        }

        /// <summary>
        /// Unloads the current profile.
        /// </summary>
        public void UnLoadProfile()
        {
            if (!string.IsNullOrEmpty(_workingHeadsetId) && _loadedProfileName != "") {
                _bciTraining.UnLoadProfile(_loadedProfileName, _workingHeadsetId);
            }
            else
                UnityEngine.Debug.LogError("UnLoadProfile: Please create a session with a headset first.");
        }

        /// <summary>
        /// Saves the current profile.
        /// </summary>
        /// <param name="profileName">The name of the profile.</param>
        public void SaveProfile(string profileName)
        {
            if (!string.IsNullOrEmpty(_workingHeadsetId))
                _bciTraining.SaveProfile(profileName, _workingHeadsetId);
            else
                UnityEngine.Debug.LogError("SaveProfile: Please create a session with a headset first.");
        }

        /// <summary>
        /// Starts a Mental Command training session.
        /// </summary>
        /// <param name="action">The action to train.</param>
        /// <param name="isAutoAccept">Whether to automatically accept the training.</param>
        /// <param name="isAutoSave">Whether to automatically save the profile after training.</param>
        public void StartMCTraining(string action, bool isAutoAccept = false, bool isAutoSave = false)
        {
            if (_isProfileLoaded)
            {
                _isAutoAcceptTraining = isAutoAccept;
                _isAutoSaveProfile = isAutoSave;
                _isMCTrainingCompleted = false;
                _isMCTrainingSuccess = false;
                _bciTraining.StartTraining(action, "mentalCommand");
            }
            else
            {
                UnityEngine.Debug.LogError("Please load a profile before starting training");
            }
        }

        /// <summary>
        /// Accepts the current Mental Command training session.
        /// </summary>
        public void AcceptMCTraining()
        {
            _bciTraining.AcceptTraining("mentalCommand");
        }

        /// <summary>
        /// Rejects the current Mental Command training session.
        /// </summary>
        public void RejectMCTraining()
        {
            _bciTraining.RejectTraining("mentalCommand");
        }

        /// <summary>
        /// Erases the Mental Command training data for a specific action.
        /// </summary>
        /// <param name="action">The action to erase training data for.</param>
        public void EraseMCTraining(string action)
        {
            UnityEngine.Debug.Log("EraseMCTraining: " + action);
            _bciTraining.EraseTraining(action, "mentalCommand");

            // remove the action from desired erasing list
            if (_desiredErasingProfiles.Contains(action))
            {
            _desiredErasingProfiles.Remove(action);
            }
        }

        /// <summary>
        /// Erases all Mental Command training data.
        /// </summary>
        public void EraseAllMCTraining()
        {
            // erase all training data of signature actions which have training time > 0
            Dictionary<string, int> trainedActions = _trainedSignatureActions;
            foreach (var item in trainedActions)
            {
            if (item.Value > 0)
            {
                _desiredErasingProfiles.Add(item.Key);
            }
            }

            if (_desiredErasingProfiles.Count == 0)
            {
            UnityEngine.Debug.Log("There is no signature action to erase.");
            }
            else
            {
            // Erase training data of action one by one.
            EraseMCTraining(_desiredErasingProfiles[0]);
            }
        }

        /// <summary>
        /// Resets the Mental Command training data for a specific action.
        /// </summary>
        /// <param name="action">The action to reset training data for.</param>
        public void ResetMCTraining(string action)
        {
            _bciTraining.ResetTraining(action, "mentalCommand");
        }

        /// <summary>
        /// Starts a Facial Expression training session.
        /// </summary>
        /// <param name="action">The action to train.</param>
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
        /// Accepts the current Facial Expression training session.
        /// </summary>
        public void AcceptFETraining()
        {
            _bciTraining.AcceptTraining("facialExpression");
        }

        /// <summary>
        /// Rejects the current Facial Expression training session.
        /// </summary>
        public void RejectFETraining()
        {
            _bciTraining.RejectTraining("facialExpression");
        }

        /// <summary>
        /// Erases the Facial Expression training data for a specific action.
        /// </summary>
        /// <param name="action">The action to erase training data for.</param>
        public void EraseFETraining(string action)
        {
            _bciTraining.EraseTraining(action, "facialExpression");
        }

        /// <summary>
        /// Resets the Facial Expression training data for a specific action.
        /// </summary>
        /// <param name="action">The action to reset training data for.</param>
        public void ResetFETraining(string action)
        {
            _bciTraining.ResetTraining(action, "facialExpression");
        }

        /// <summary>
        /// Sets the sensitivity of the 4 active mental command actions even if active actions are less than 4.
        /// </summary>
        /// <param name="levels">The list of sensitivity levels. It is array of 4 numbers in range 1-10 </param>
        public void SetMentalCommandActionSensitivity(List<int> levels)
        {
            if (string.IsNullOrEmpty(_loadedProfileName))
            {
                UnityEngine.Debug.LogError("Please load a profile before setting sensitivity levels.");
                return;
            }

            _bciTraining.SetMentalCommandActionSensitivity(_loadedProfileName, levels);
        }

        /// <summary>
        /// Gets the sensitivity levels for Mental Command actions. The sensitivity data will be got via MentalCommandActionSensitivity
        /// </summary>
        public void GetMentalCommandActionSensitivity()
        {
            _bciTraining.GetMentalCommandActionSensitivity(_loadedProfileName);
        }

        /// <summary>
        /// Gets the trained signature actions for a specific detection type.
        /// </summary>
        /// <param name="detection">The detection type (e.g., "mentalCommand").</param>
        public void GetTrainedSignatureActions(string detection)
        {
            _bciTraining.GetTrainedSignatureActions(detection, _loadedProfileName);
        }

        /// <summary>
        /// Queries the dates having consumer data within a specified date range.
        /// </summary>
        /// <param name="from">The start date of the range.</param>
        /// <param name="to">The end date of the range.</param>
        public void QueryDatesHavingConsumerData(DateTime from, DateTime to) {
            _dsManager.QueryDatesHavingConsumerData(from, to);
        }

        /// <summary>
        /// Queries the detailed consumer data for a specific day.
        /// </summary>
        /// <param name="date">The date for which to query the detailed consumer data.</param>
        public void QueryDayDetailOfConsumerData(DateTime date) {
            _dsManager.QueryDayDetailOfConsumerData(date);
        }

        // Event handlers
        private void OnHeadsetConnectFail(object sender, string headsetId)
        {
            UnityEngine.Debug.Log("OnHeadsetConnectFail: headsetId " + headsetId);
            _messageLog = "The headset " + headsetId + " is failed to connect.";
        }
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
            // reset profile and training data when session is activated
            ResetProfileAndTrainingData();
        }

        private void OnInformLoadProfileDone(object sender, string profileName)
        {
            UnityEngine.Debug.Log("OnInformLoadProfileDone:  The profile " + profileName + " is loaded successfully.");
            _messageLog = "The profile "+ profileName + " is loaded successfully.";
            _isProfileLoaded = true;
            _loadedProfileName = profileName;
            _isSupportedDeviceForProfile = true; // assume the current headset is supported for the profile
            // get trained signature actions
            GetTrainedSignatureActions("mentalCommand");

            // get mental command action sensitivity
            GetMentalCommandActionSensitivity();
        }

        private void OnInformUnLoadProfileDone(object sender, string profileName)
        {
            _messageLog = "The profile " + profileName + " is unloaded successfully.";

            if (_loadedProfileName == profileName)
            {
                _isProfileLoaded = false;
                _loadedProfileName = "";
                _desiredErasingProfiles.Clear();
            }
            
        }

        private void OnInformStartRecordResult(object sender, Record record)
        {
            UnityEngine.Debug.Log("OnInformStartRecordResult recordId: " + record.Uuid + ", title: "
                + record.Title + ", startDateTime: " + record.StartDateTime);
            _isRecording = true;
            _recentRecord = record; // store the recent record
            _messageLog = "The record " + record.Title + " is created at " + record.StartDateTime;

        }

        private void OnInformStopRecordResult(object sender, Record record)
        {
            UnityEngine.Debug.Log("OnInformStopRecordResult recordId: " + record.Uuid + ", title: "
                + record.Title + ", startDateTime: " + record.StartDateTime + ", endDateTime: " + record.EndDateTime);
            _isRecording = false;
            _recentRecord = record; // update the recent record
            _messageLog = "The record " + record.Title + " is ended at " + record.EndDateTime;

        }

        private void OnInformMarkerResult(object sender, JObject markerObj)
        {
            UnityEngine.Debug.Log("OnInformMarkerResult");
            _messageLog = "The marker " + markerObj["uuid"].ToString() + ", label: " 
                + markerObj["label"].ToString() + ", value: " + markerObj["value"].ToString()
                + ", type: " + markerObj["type"].ToString() + ", started at: " + markerObj["startDatetime"].ToString();
        }

        private void OnMessageQueryHeadsetOK(object sender, string headsetsInfo)
        {
            _messageLog = headsetsInfo;
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
            _messageLog = dataText;
            // print out data to console
            UnityEngine.Debug.Log(dataText);
        }

        private void OnPerfDataReceived(object sender, ArrayList e)
        {
            string dataText = "met data: ";
            foreach (var item in e) {
                dataText += item.ToString() + ",";
            }
            _messageLog = dataText;
            // print out data to console
            UnityEngine.Debug.Log(dataText);
        }

        private void OnDevDataReceived(object sender, ArrayList e)
        {
            string dataText = "dev data: ";
            foreach (var item in e) {
                dataText += item.ToString() + ",";
            }
            _messageLog = dataText;
            // print out data to console
            UnityEngine.Debug.Log(dataText);
        }

        private void OnEQDataReceived(object sender, ArrayList e)
        {
            string dataText = "eq data: ";
            foreach (var item in e) {
                dataText += item.ToString() + ",";
            }
            _messageLog = dataText;
            // print out data to console
            UnityEngine.Debug.Log(dataText);
        }

        private void OnMotionDataReceived(object sender, ArrayList e)
        {
            string dataText = "mot data: ";
            foreach (var item in e) {
                dataText += item.ToString() + ",";
            }
            _messageLog = dataText;
            // print out data to console
            UnityEngine.Debug.Log(dataText);
        }

        private void OnEEGDataReceived(object sender, ArrayList e)
        {
            string dataText = "eeg data: ";
            foreach (var item in e) {
                dataText += item.ToString() + ",";
            }
            _messageLog = dataText;
            // print out data to console
            UnityEngine.Debug.Log(dataText);
        }

        private void OnSysEventsReceived(object sender, SysEventArgs data)
        {
            string dataText = "sys data: " + data.Detection + ", event: " + data.EventMessage + ", time " + data.Time.ToString();
            UnityEngine.Debug.Log("OnSysEventsReceived: " + data.EventMessage + ", _isAutoSaveProfile " + _isAutoSaveProfile + ", _loadedProfileName " + _loadedProfileName);
            // show the system event to message log
            _messageLog = dataText;

            if (data.Detection == "mentalCommand")
            {
                if (_isAutoAcceptTraining && data.EventMessage == "MC_Succeeded") {
                    AcceptMCTraining();
                }
                else if (data.EventMessage == "MC_Failed")
                {
                    _isMCTrainingCompleted = true;
                    _isMCTrainingSuccess = false;
                }
                else if (data.EventMessage == "MC_Completed")
                {
                    UnityEngine.Debug.Log("The training is completed.");
                    _isMCTrainingCompleted = true;
                    _isMCTrainingSuccess = true;
                    if (_isAutoSaveProfile && _loadedProfileName != "")
                    {
                        SaveProfile(_loadedProfileName);
                    }
                }
                else if (data.EventMessage == "MC_DataErased" && _loadedProfileName != "")
                {
                    // save profile after erasing the training data
                    SaveProfile(_loadedProfileName);
                }
            }
        }

        private void OnMentalCommandReceived(object sender, MentalCommandEventArgs data)
        {
            string dataText = "com data: " + data.Act + ", power: " + data.Pow.ToString() + ", time " + data.Time.ToString();
            // print out data to console
            UnityEngine.Debug.Log(dataText);
            LatestMentalCommand.act = data.Act;
            LatestMentalCommand.pow = data.Pow;
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
            // Emit the error message event
            ErrorMsgReceived?.Invoke(this, errorInfo);
        }

        private void OnStreamStopNotify(object sender, List<string> streams)
        {
            string tmpText = "The data streams: ";
            foreach (var item in streams)
            {
                tmpText= tmpText + item + "; ";
            }
            tmpText = tmpText + " are stopped.";
            _messageLog = tmpText;
        }

        private void OnUserLogoutNotify(object sender, string message)
        {
            // clear data
            ClearData();
            _messageLog = message;
        }

        private void OnGetMentalCommandActionSensitivityOK(object sender, List<int> e)
        {
            UnityEngine.Debug.Log("OnGetMentalCommandActionSensitivityOK: " + e.Count);
            _mentalCommandActionSensitivity = e;
        }

        private void OnProfileSavedOK(object sender, string profileName)
        {
            if (profileName == _loadedProfileName)
            {
                _messageLog = "The profile " + profileName + " is saved successfully.";

                // check desired erasing profiles. If there is any, erase the one by one in the list
                if (_desiredErasingProfiles.Count > 0)
                {
                    UnityEngine.Debug.Log("OnProfileSavedOK: There are " + _desiredErasingProfiles.Count + " signature actions to erase.");
                    EraseMCTraining(_desiredErasingProfiles[0]);
                }
                else {
                    // get signature actions again
                    GetTrainedSignatureActions("mentalCommand");

                    // get mental command action sensitivity
                    GetMentalCommandActionSensitivity();
                }
            }
        }

        private void OnInformTrainedSignatureActions(object sender, Dictionary<string, int> trainedActions)
        {
            // print out trained actions to message log
            string trainedActionsText = "Trained actions: ";
            foreach (var item in trainedActions)
            {
                trainedActionsText += item.Key + " (" + item.Value + "), ";
            }
            // _messageLog = trainedActionsText;
            UnityEngine.Debug.Log("OnInformTrainedSignatureActions "+ trainedActionsText);

            _trainedSignatureActions = trainedActions;
        }

        private void OnInformEraseDone(object sender, string action)
        {
            UnityEngine.Debug.Log("OnInformEraseDone: " + action);
        }

        private void OnSetMentalCommandActionSensitivityOK(object sender, bool isSuccess)
        {
            UnityEngine.Debug.Log("SetMentalCommandActionSensitivityOK: " + isSuccess);
            if (isSuccess)
            {
                // save profile
                SaveProfile(_loadedProfileName);
            }
        }

        private void OnQueryDatesHavingConsumerDataDone(object sender, List<DateTime> dates)
        {
            string datesText = "Dates having consumer data: ";
            foreach (var item in dates)
            {
                datesText += item.ToString("yyyy-MM-dd") + ", ";
            }
            _messageLog = datesText;
            
            _datesHavingConsumerData = dates;
        }

        private void OnQueryDayDetailOfConsumerDataDone(object sender, List<MentalStateModel> mentalStateList)
        {
            string mentalStateText = "Mental state data: ";
            
            for (int i = 0; i < mentalStateList.Count; i++)
            {
                TimeSpan time = Utils.IndexToTime(i);
                mentalStateText += "At time: " + time.ToString() +  mentalStateList[i].ToString() + "\n";
            }
            UnityEngine.Debug.Log(mentalStateText);
            // _messageLog = mentalStateText;
            _mentalStateDatas = mentalStateList;
        }

        private void onBTLEPermissionGrantedNotify(object sender, bool isBTLEPermissionGranted)
        {
            if (isBTLEPermissionGranted)
            {
                _messageLog = "The Bluetooth permission granted.";
            }
            else
            {
                _messageLog = "Bluetooth permission was denied. Please enable Bluetooth permissions to scan for headsets.";
            }
        }

        private void OnNoAccessRightNotify(object sender, string msg)
        {
            _messageLog = msg;
        }

        private void OnLicenseExpired(object sender, License e)
        {
            _messageLog = "License expired. Please contact Emotiv to renew your license. \n" +
                          "Please relogin to apply new license.";

        }

        private void OnExportRecordsFinished(object sender, MultipleResultEventArgs e)
        {
            // get successful list
            JArray successfulList = e.SuccessList;
            // check _recentRecordId is in the successful list
            bool exportRecentRecordSuccess = false;
            if (successfulList != null && successfulList.Count > 0)
            {
                foreach (var record in successfulList)
                {
                    if (record is JObject recordObj && recordObj["recordId"]?.ToString() == _recentRecord?.Uuid)
                    {
                        exportRecentRecordSuccess = true;
                        break;
                    }
                }
            }

            _messageLog = "Export record  " + _recentRecord?.Title + (exportRecentRecordSuccess ? " successfully. " : " failed. ") +
                          " The recordId: " + _recentRecord?.Uuid;
        }

        private void OnDataPostProcessingFinished(object sender, string recordId)
        {
            if (recordId == _recentRecord?.Uuid)
            {
                _isRecording = false;
                // ready for exporting recent record
                _messageLog = "Data post processing finished for record: " + _recentRecord?.Title;
            }
        }


        // clear data
        private void ClearData()
        {
            _isAuthorizedOK = false;
            _workingHeadsetId = "";
            ResetProfileAndTrainingData();
        }
        

        /// <summary>
        /// Reset the loaded profile and all training-related data.
        /// </summary>
        public void ResetProfileAndTrainingData()
        {
            _isProfileLoaded = false;
            _loadedProfileName = "";
            _isMCTrainingCompleted = false;
            _isMCTrainingSuccess = false;
            _isAutoAcceptTraining = false;
            _isAutoSaveProfile = false;
            _isSupportedDeviceForProfile = true;
            _trainedSignatureActions.Clear();
            _mentalCommandActionSensitivity.Clear();
            _desiredErasingProfiles.Clear();
        }

        public void OpenURL(string url)
        {
            #if UNITY_ANDROID || UNITY_IOS
            _isWebViewOpened = true;
            UniWebViewManager.Instance.OpenURL(
                url, 
                onClosed: (isClosed) => {
                    Debug.Log($"UniWebView closed! isClosed: {isClosed}");
                    _isWebViewOpened = false;
                    
                }
            );
            #else
            Application.OpenURL(url);
            #endif
        }

        #if USE_EMBEDDED_LIB || UNITY_ANDROID || UNITY_IOS
        private string BytesToHex(byte[] bytes)
        {
            char[] hexChars = new char[bytes.Length * 2];
            for (int j = 0; j < bytes.Length; ++j)
            {
                int v = bytes[j] & 0xFF;
                hexChars[j * 2] = HEX_ARRAY[v >> 4];
                hexChars[j * 2 + 1] = HEX_ARRAY[v & 0x0F];
            }
            return new string(hexChars);
        }

        private string Md5(string s)
        {
            try
            {
                using (var md5 = MD5.Create())
                {
                    byte[] inputBytes = Encoding.UTF8.GetBytes(s);
                    byte[] hashBytes = md5.ComputeHash(inputBytes);
                    return BytesToHex(hashBytes);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return string.Empty;
            }
        }

        private void InitForAuthentication(string clientId, string clientSecret)
        {
            string server = "";
 #if DEV_SERVER
            UnityEngine.Debug.Log("Development build detected. Using development server.");
            server = "cerebrum-dev.emotivcloud.com";
#else
            UnityEngine.Debug.Log("Production build detected. Using production server.");
            server = "cerebrum.emotivcloud.com";
#endif
            string hash = Md5(clientId);
            string prefixRedirectUrl = "emotiv-" + hash;
            string redirectUrl = prefixRedirectUrl + "://authorize";
            string serverUrl = $"https://{server}";
            #if UNITY_ANDROID || UNITY_IOS
            string authorizationUrl = $"https://{server}/api/oauth/authorize/?response_type=code" +
                        $"&client_id={Uri.EscapeDataString(clientId)}" +
                        $"&redirect_uri={redirectUrl}" + $"&hide_signup=1&hide_social_signin=1";
            UniWebViewManager.Instance.Init(
                authorizationUrl, 
                prefixRedirectUrl
            );
            #else
            _crossPlatformBrowser = new CrossPlatformBrowser();
            _crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.WindowsEditor, new WindowsSystemBrowser());
            _crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.WindowsPlayer, new WindowsSystemBrowser());
            _crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.OSXEditor, new DeepLinkBrowser());
            _crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.OSXPlayer, new DeepLinkBrowser());

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            // Deep linking is not supported on Windows (except UWP), so RegistryConfig is used to handle this case.
            new RegistryConfig(prefixRedirectUrl).Configure();
#endif

            var configuration = new AuthorizationCodeFlow.Configuration()
            {
                clientId = clientId,
                clientSecret = clientSecret,
                redirectUri = redirectUrl,
                scope = ""
            };
            var auth = new MockServerAuth(configuration, serverUrl);
            _authenticationSession = new AuthenticationSession(auth, _crossPlatformBrowser);
            _authenticationSession.loginTimeout = TimeSpan.FromSeconds(600);
            #endif
        }
        public async Task AuthenticateAsync()
        {
            #if UNITY_ANDROID || UNITY_IOS
            _isWebViewOpened = true;
            UniWebViewManager.Instance.StartAuthorization(
                onSuccess: (authCode) => {
                    Debug.Log($"UniWebView Authorization succeeded! Starting login with auth code");
                    LoginWithAuthenticationCode(authCode);
                    _isWebViewOpened = false;
                },
                onError: (errorCode, errorMessage) => {
                    Debug.LogError($"Authorization failed! Error {errorCode}: {errorMessage}");
                    _isWebViewOpened = false;
                    
                }
            );
            #else
            if (_authenticationSession != null)
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = new CancellationTokenSource();
                try
                {
                    MessageLog = "Starting authentication...";
                    var accessTokenResponse =
                        await _authenticationSession.AuthenticateAsync(_cancellationTokenSource.Token);

                    LoginWithAuthenticationCode(accessTokenResponse.accessToken);
                }
                catch (AuthorizationCodeRequestException ex)
                {
                    Debug.LogError($"{nameof(AuthorizationCodeRequestException)} " +
                                $"error: {ex.error.code}, description: {ex.error.description}, uri: {ex.error.uri}");
                }
                catch (AccessTokenRequestException ex)
                {
                    Debug.LogError($"{nameof(AccessTokenRequestException)} " +
                                $"error: {ex.error.code}, description: {ex.error.description}, uri: {ex.error.uri}");
                }
                catch (Exception ex)
                {
                    Debug.LogError( "Exception " + ex.Message);
                }
            }
            #endif
        }
        #endif

    }
}
