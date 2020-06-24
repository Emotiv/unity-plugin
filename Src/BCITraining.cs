using System.Collections.Generic;
using System;
using Newtonsoft.Json.Linq;

namespace EmotivUnityPlugin
{
    /// <summary>
    /// Reponsible for brain–computer interface (BCI) training included mental command and facial expression
    /// </summary>
    public class BCITraining
    {
        static readonly object _locker = new object();
        
        private static string CurrDetection = "mentalCommand";

        private TrainingHandler _trainingHandler    = TrainingHandler.Instance;
        private DataStreamManager _dsManager        = DataStreamManager.Instance;

        /// <summary>
        /// all profiles of user.
        /// </summary>
        private List<string> _profileLists = null;

        public List<string> ProfileLists { get => _profileLists; set => _profileLists = value; }


        public BCITraining()
        {
        }
        /// <summary>
        /// Initial mental command training.
        /// </summary>
        public void Init()
        {
            _dsManager.SysEventSubscribed       += OnSysEventSubscribed;
            _dsManager.SysEventReceived         += OnSysEventReceived;
            _dsManager.SysEventUnSubscribed     += OnSysEventUnSubscribed;
            _trainingHandler.QueryProfileOK     += OnQueryProfileOK;
            _trainingHandler.CreateProfileOK    += OnCreateProfileOK;
            _trainingHandler.ProfileSavedOK     += OnProfileSavedOK;
            _trainingHandler.TrainingOK         += OnTrainingOK;
            _trainingHandler.GetDetectionInfoOK += OnGetDetectionInfoOK;
            _trainingHandler.ProfileLoaded      += OnProfileLoaded;

        }

        private void OnProfileLoaded(object sender, string profileName)
        {
            UnityEngine.Debug.Log("BCITraining: OnProfileLoaded profile " + profileName);

            // Only for test
            // StartTraining("neutral", CurrDetection);
        }

        private void OnSysEventUnSubscribed(object sender, string e)
        {
            UnityEngine.Debug.Log("BCITraining: OnSysEventUnSubscribed");
            _trainingHandler.Clear();
        }

        private void OnGetDetectionInfoOK(object sender, DetectionInfo detectionInfo)
        {
            if (detectionInfo.DetectionName == CurrDetection)
            {
                UnityEngine.Debug.Log("OnGetDetectionInfoOK");

                // QueryProfile
                QueryProfile();
            }
        }

        private void OnTrainingOK(object sender, JObject result)
        {
            UnityEngine.Debug.Log("TrainingOK: " + result);
        }

        private void OnSysEventReceived(object sender, SysEventArgs e)
        {
            if (e.Detection == "mentalCommand")
            {
                string eventType = e.EventMessage;
                UnityEngine.Debug.Log("OnSysEventReceived: event message " + e.EventMessage + " at " + e.Time);

                
                if (eventType == "MC_Started")
                {
                    UnityEngine.Debug.Log("Start training...");
                }
                else if (eventType == "MC_Succeeded")
                {
                    UnityEngine.Debug.Log("OnSysEventReceived:TrainingSucceeded ");
                    //TrainingSucceeded(this, true);
                    // Only for Test
                    // AcceptTraining("neutral", CurrDetection);
                }
                else if (eventType == "MC_Completed" ||
                         eventType == "MC_Rejected" ||
                         eventType == "MC_DataErased" ||
                         eventType == "MC_Reset")
                {
                    UnityEngine.Debug.Log("OnSysEventReceived:  " + eventType);
                    // save profile
                    _trainingHandler.SaveProfile();
                }
            }
        }

        private void OnCreateProfileOK(object sender, string profileName)
        {
            UnityEngine.Debug.Log("BCITraining: OnCreateProfileOK profilename " + profileName);
        }

        private void OnQueryProfileOK(object sender, List<string> profiles)
        {
            _profileLists = new List<string>(profiles);
            UnityEngine.Debug.Log("BCITraining: OnQueryProfileOK - num of profiles " + _profileLists.Count);

            UnityEngine.Debug.Log("List of profiles: ");
            foreach(var profileName in _profileLists)
            {
                UnityEngine.Debug.Log(profileName);
            }
            
            // For Test only
            // if (_profileLists.Count > 0)
            // {
            //     LoadProfile(_profileLists[0]);
            // }
            // else
            // {
            //     CreateProfile("Demo_BCI_Training ");
            // }
        }

        private void OnSysEventSubscribed(object sender, string headsetId)
        {
            UnityEngine.Debug.Log("BCITraining: OnSysEventSubscribed headsetId " + headsetId);
            _trainingHandler.SetHeadset(headsetId);
            // Get detection
            _trainingHandler.GetDetectionInfo(CurrDetection);
        }

        private void OnProfileSavedOK(object sender, string profileName)
        {
            UnityEngine.Debug.Log("The profile " + profileName + " is saved successfully.");
        }

        /// <summary>
        /// Get information about the detection 
        /// </summary>
        public void GetDetectionInfo(string detection)
        {
            if (string.IsNullOrEmpty(detection))
                _trainingHandler.GetDetectionInfo(CurrDetection);
            else
                _trainingHandler.GetDetectionInfo(detection);
        }

        /// <summary>
        /// Query profile.
        /// </summary>
        public void QueryProfile()
        {
            _trainingHandler.QueryProfile();
        }

        /// <summary>
        /// Create a profile.
        /// </summary>
        public void CreateProfile(string profileName)
        {
            _trainingHandler.CreateProfile(profileName);
        }

        /// <summary>
        /// Load a profile
        /// </summary>
        public void LoadProfile(string profileName)
        {
            if (_profileLists.Contains(profileName)) {
                _trainingHandler.LoadProfile(profileName);
            }
            else
                UnityEngine.Debug.Log("The profile can not be loaded. The name " + profileName + " has not existed.");
        }

        /// <summary>
        /// UnLoad a profile
        /// </summary>
        public void UnLoadProfile(string profileName)
        {
            if (_profileLists.Contains(profileName)) {
                _trainingHandler.UnLoadProfile();
            }
            else
                UnityEngine.Debug.Log("The profile can not be unloaded. The name " + profileName + " has not existed.");
        }

        /// <summary>
        /// Start a Training
        /// </summary>
        public void StartTraining(string action, string detection)
        {
            _trainingHandler.DoTraining(action, "start", detection);
        }

        /// <summary>
        /// Accept a Training
        /// </summary>
        public void AcceptTraining(string action, string detection)
        {
            _trainingHandler.DoTraining(action, "accept", detection);
        }

        /// <summary>
        /// Reject a Training
        /// </summary>
        public void RejectTraining(string action, string detection)
        {
            _trainingHandler.DoTraining(action, "reject", detection);
        }

        /// <summary>
        /// Erase a Training
        /// </summary>
        public void EraseTraining(string action, string detection)
        {
            _trainingHandler.DoTraining(action, "erase", detection);
        }

        /// <summary>
        /// Reset a Training
        /// </summary>
        public void ResetTraining(string action, string detection)
        {
            _trainingHandler.DoTraining(action, "reset", detection);
        }
    }
}