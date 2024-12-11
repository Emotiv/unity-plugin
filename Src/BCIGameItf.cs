using System.Collections.Generic;

namespace EmotivUnityPlugin
{
    public class BCIGameItf
    {
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

        /// <summary>
        /// Check if profile is loaded and ready for training.
        /// </summary>
        public bool IsReadyForTraining()
        {
            return emotivUnityItf.IsProfileLoaded;
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

        /// <summary>
        /// Initialize and start the application. It should be called when the app has granted permissions: bluetooth, location, write external storage.
        /// It will auto login if the user has not logged in before otherwise it will authorize to get cortex token for working with the cortex API.
        /// </summary>
        public void Start(string clientId, string clientSecret, string username, string password, object context = null)
        {
            // Init
            emotivUnityItf.Init(clientId, clientSecret, "HeartBeat", "1.1.0", username, password);
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
        public double GetMentalCommandActionPower(string action = "push")
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
        public bool IsGoodMCAction(string action = "push", double threshold = 0.5)
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
        public void StartTraining(string action = "push", bool isAutoAccept = true, bool isAutoSave = true)
        {
            emotivUnityItf.StartMCTraining(action, isAutoAccept, isAutoSave);
        }

        /// <summary>
        /// Start neutral training.
        /// </summary>
        public void StartNeutralTraining(bool isAutoAccept = true, bool isAutoSave = true)
        {
            emotivUnityItf.StartMCTraining("neutral", isAutoAccept, isAutoSave);
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
        /// Set sensitivity.
        /// </summary>
        public void SetSensitivity(int value = 5)
        {
            List<int> levels = new List<int> { value };
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
    }
}