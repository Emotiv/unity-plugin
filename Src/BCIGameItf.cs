using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EmotivUnityPlugin
{
    public class BCIGameItf
    {
        public const string DEFAULT_MC_ACTION = "pull"; // default action for mental command training
        public const string NEUTRAL_ACTION = "neutral"; // neutral action for training
        public const int DEFAULT_SENSITIVITY = 5; // default sensitivity for mental command training

        private static BCIGameItf _instance;
        private EmotivUnityItf emotivUnityItf = EmotivUnityItf.Instance;

        private BCIGameItf()
        {
            // Private constructor to prevent instantiation
        }

        public static BCIGameItf Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new BCIGameItf();
                }
                return _instance;
            }
        }

        public ConnectToCortexStates GetConnectToCortexState()
        {
            return emotivUnityItf.GetConnectToCortexState();
        }

        // LoginWithAuthenticationCode
        public void LoginWithAuthenticationCode(string code)
        {
            emotivUnityItf.LoginWithAuthenticationCode(code);
        }

        public void AcceptEulaAndPrivacyPolicy()
        {
            emotivUnityItf.AcceptEulaAndPrivacyPolicy();
        }

        /// <summary>
        /// Get detected headsets. Returns a list of detected headsets.
        /// </summary>
        public List<Headset> GetDetectedHeadsets()
        {
            return emotivUnityItf.GetDetectedHeadsets();
        }

        // connect headset state
        public ConnectHeadsetStates GetConnectHeadsetState()
        {
            return emotivUnityItf.GetConnectHeadsetState();
        }

        /// <summary>
        /// Check if authorized.
        /// </summary>
        public bool IsAuthorized()
        {
            return emotivUnityItf.IsAuthorizedOK;
        }

        /// <summary>
        /// Check if session is created.
        /// </summary>
        public bool IsSessionCreated()
        {
            return emotivUnityItf.IsSessionCreated;
        }

        /// <summary>
        /// Get log message.
        /// </summary>
        public string GetLogMessage()
        {
            return emotivUnityItf.MessageLog;
        }

        /// <summary>
        /// Get battery information which includes battery level for left and right ear, and overall battery level.
        /// </summary>
        public BatteryInfo GetBattery()
        {
            return emotivUnityItf.GetBattery();
        }

        /// <summary>
        /// Get contact quality and battery level of the headset for a specific channel.
        /// </summary>
        public double GetContactQuality(Channel_t channel)
        {
            return emotivUnityItf.GetContactQuality(channel);
        }

        /// <summary>
        /// Get number of contact quality samples.
        /// </summary>
        public int GetNumberCQSamples()
        {
            return emotivUnityItf.GetNumberCQSamples();
        }

        // eq data
        public double GetEQ(Channel_t channel)
        {
            return emotivUnityItf.GetEQ(channel);
        }

        // numebr of eeg quality samples
        public int GetNumberEQSamples()
        {
            return emotivUnityItf.GetNumberEQSamples();
        }

        /// <summary>
        /// Check if profile is loaded and ready for training.
        /// </summary>
        public bool IsReadyForTraining()
        {
            // Check if profile is loaded and no desired erasing profiles (in erasing process)
            return emotivUnityItf.IsProfileLoaded && (emotivUnityItf.DesiredErasingProfiles.Count == 0);
        }

        /// <summary>
        /// Check if training is completed or not. It will falses when training started then it will be true when training is completed both success and fail.
        /// </summary>
        public bool IsTrainingCompleted()
        {
            return emotivUnityItf.IsMCTrainingCompleted;
        }

        /// <summary>
        /// Check if training is success or fail. But it should check after training is completed.
        /// </summary>
        public bool IsTrainingSuccess()
        {
            return emotivUnityItf.IsMCTrainingSuccess;
        }

        public List<DateTime> DatesHavingConsumerData()
        {
            return emotivUnityItf.DatesHavingConsumerData;
        }

        public List<MentalStateModel> MentalStateDatas()
        {
            return emotivUnityItf.MentalStateDatas;
        }


        /// <summary>
        /// Check if the connected device is supported for the current profile.
        /// </summary>
        public bool IsSupportedDeviceForProfile()
        {
            return emotivUnityItf.IsSupportedDeviceForProfile;
        }

        #if USE_EMBEDDED_LIB && UNITY_STANDALONE_WIN && !UNITY_EDITOR
        /// <summary>
        /// Process callback to handle authorization response from Emotiv Cloud when login. It use for embedded library on Windows and not for editor mode.
        /// </summary>
        public  async Task ProcessCallback(string args) {
            await emotivUnityItf.ProcessCallback(args);
        }
        #endif

        #if USE_EMBEDDED_LIB || UNITY_ANDROID || UNITY_IOS
        /// <summary>
        /// Authenticate with Emotiv. It will open system browser to login and get the authentication code.
        /// </summary>
        public async Task AuthenticateAsync()
        {
            await emotivUnityItf.AuthenticateAsync();
        }
        #endif

        /// <summary>
        /// Initialize and start the application. It should be called when the app has granted permissions: bluetooth, location, write external storage.
        /// It will auto login if the user has not logged in before otherwise it will authorize to get cortex token for working with the cortex API.
        /// </summary>
        public void Start(string clientId, string clientSecret, string appName, bool isSaveLog = true, object context = null)
        {
            // Init
            emotivUnityItf.Init(clientId, clientSecret, appName, isSaveLog);
            // Loads the Cortex library, connects, and authorizes the application.
            emotivUnityItf.Start(context);
        }

        /// <summary>
        /// Query the headset information.
        /// </summary>
        public void QueryHeadsets(string desiredHeadsetId = "")
        {
            emotivUnityItf.QueryHeadsets(desiredHeadsetId);
        }

        /// <summary>
        /// Connect headset, create session and subscribe data: dev for contact quality, com for mental command, sys for system event training.
        /// </summary>
        public void StartStreamData(string desiredHeadsetId = "")
        {
            List<string> streamNameList = new List<string> { "sys", "com", "dev" };
            emotivUnityItf.StartDataStream(streamNameList, desiredHeadsetId);
        }

        /// <summary>
        /// Get mental command action power for a specific action.
        /// </summary>
        public double GetMentalCommandActionPower(string action = DEFAULT_MC_ACTION)
        {
            MentalComm mentalCommand = emotivUnityItf.LatestMentalCommand;
            if (mentalCommand.act == action)
            {
                return mentalCommand.pow;
            }
            else
            {
                UnityEngine.Debug.Log("Detected training action is " + mentalCommand.act + " with power " + mentalCommand.pow);
                return -1;
            }
        }

        /// <summary>
        /// Check if mental command power for a specific action is greater than a threshold.
        /// </summary>
        public bool IsGoodMCAction(string action = DEFAULT_MC_ACTION, double threshold = 0.5)
        {
            MentalComm mentalCommand = emotivUnityItf.LatestMentalCommand;
            if (mentalCommand.act == action)
            {
                return mentalCommand.pow > threshold;
            }
            else
            {
                UnityEngine.Debug.Log("Detected training action is " + mentalCommand.act + " with power " + mentalCommand.pow);
                return false;
            }
        }

        /// <summary>
        /// Create a player profile.
        /// </summary>
        public void CreatePlayer(string playerName)
        {
            emotivUnityItf.LoadProfile(playerName);
        }

        /// <summary>
        /// Get profile list.
        /// </summary>
        public List<string> GetProfileList()
        {
            return emotivUnityItf.GetProfileList();
        }

        /// <summary>
        /// Start training for a specific action.
        /// </summary>
        public void StartTraining(string action = DEFAULT_MC_ACTION, bool isAutoAccept = true, bool isAutoSave = true)
        {
            emotivUnityItf.StartMCTraining(action, isAutoAccept, isAutoSave);
        }

        /// <summary>
        /// Start neutral training.
        /// </summary>
        public void StartNeutralTraining(bool isAutoAccept = true, bool isAutoSave = true)
        {
            emotivUnityItf.StartMCTraining(NEUTRAL_ACTION, isAutoAccept, isAutoSave);
        }

        /// <summary>
        /// Accept training if you don't want to auto accept.
        /// </summary>
        public void AcceptTraining()
        {
            emotivUnityItf.AcceptMCTraining();
        }

        /// <summary>
        /// Reject training.
        /// </summary>
        public void RejectTraining()
        {
            emotivUnityItf.RejectMCTraining();
        }

        /// <summary>
        /// Save profile.
        /// </summary>
        public void SaveProfile(string profileName)
        {
            emotivUnityItf.SaveProfile(profileName);
        }

        /// <summary>
        /// Set sensitivity for first trained action. Should be called after training is completed.
        /// </summary>
        public void SetSensitivity(int value = DEFAULT_SENSITIVITY)
        {
            List<int> levels = new List<int> { value, DEFAULT_SENSITIVITY, DEFAULT_SENSITIVITY, DEFAULT_SENSITIVITY }; // require 4 value for 4 actions (even only 1 trained action)
            emotivUnityItf.SetMentalCommandActionSensitivity(levels);
        }

        /// <summary>
        /// Stop the application.
        /// </summary>
        public void Stop()
        {
            emotivUnityItf.Stop();
        }

        /// <summary>
        /// Unload profile of player to free up resources. Should unload profile when the player is not in use.
        /// </summary>
        public void UnloadProfile()
        {
            emotivUnityItf.UnLoadProfile();
        }

        /// <summary>
        /// Get the number of training times for a specific action. We can use it to display number of training times for each action.
        /// </summary>
        /// <param name="action">The action to get the training times for. Default is pull action.</param>
        /// <returns>The number of training times for the specified action.</returns>
        public int GetNumberTrainingOfAction(string action = DEFAULT_MC_ACTION)
        {
            return emotivUnityItf.GetTrainingTimeForAction(action);
        }


        // add description
        /// <summary>
        /// Gets the current Emotiv ID of the logged-in user.
        /// </summary>
        /// <returns>The current Emotiv ID, or empty string if not logged in.</returns>
        public string GetCurrentEmotivId()
        {
            return emotivUnityItf.GetCurrentEmotivId();
        }

        /// <summary>
        /// Get the mental command action sensitivity for the first trained action except neutral (pull action).
        /// Should be called after training is completed. Default sensitivity is 5
        /// </summary>
        /// <returns>The sensitivity of the first trained mental command action, or -1 if no actions are trained.</returns>
        public int GetFirstMCActionSensitivity()
        {
            if (emotivUnityItf.MentalCommandActionSensitivity.Count > 0)
            {
                return emotivUnityItf.MentalCommandActionSensitivity[0];
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Clear all training data for all mental command actions. It will erase actions one by one. You can check emotivUnityItf.DesiredErasingProfiles.Count to know the number of actions that are being erased.
        /// </summary>
        public void EraseDataAllMCTraining()
        {
            emotivUnityItf.EraseAllMCTraining();
        }

        /// <summary>
        /// Erase training data for the neutral action.
        /// </summary>
        public void EraseDataForNeutralTraining()
        {
            emotivUnityItf.EraseMCTraining(NEUTRAL_ACTION);
        }

        /// <summary>
        /// Erase training data for a specific mental command action.
        /// </summary>
        /// <param name="action">The action to erase the training data for. Default is the default mental command action.</param>
        public void EraseDataForMCTrainingAction(string action = DEFAULT_MC_ACTION)
        {
            emotivUnityItf.EraseMCTraining(action);
        }

        // logout
        public void Logout()
        {
            emotivUnityItf.Logout();
        }

        /// <summary>
        /// Query the dates having consumer data within a specified date range.
        /// </summary>
        /// <param name="from">The start date of the range.</param>
        /// <param name="to">The end date of the range.</param>
        public void QueryDatesHavingConsumerData(DateTime from, DateTime to)
        {
            emotivUnityItf.QueryDatesHavingConsumerData(from, to);
        }

        /// <summary>
        /// Queries the detailed consumer data for a specific day.
        /// </summary>
        /// <param name="date">The date for which to query the detailed consumer data.</param>
        public void QueryDayDetailOfConsumerData(DateTime date) {
            emotivUnityItf.QueryDayDetailOfConsumerData(date);
        }

        public void OpenURL(string url)
        {
            emotivUnityItf.OpenURL(url);
        }

        public bool IsWebViewOpened()
        {
            return emotivUnityItf.IsWebViewOpened;
        }

        public string LoadedProfilePlayer() {
            return emotivUnityItf.LoadedProfileName;
        }

        public string GetWorkingHeadsetId() {
            return emotivUnityItf.WorkingHeadsetId;
        }
    }
}
