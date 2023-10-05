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

        private string _wantedProfileName = "";

        private string _workingHeadsetId = "";
        private string _currAction = "";


        // event
        public event EventHandler<bool> InformLoadUnLoadProfileDone;

        /// <summary>
        /// all profiles of user.
        /// </summary>
        private List<string> _profileLists = null;

        public List<string> ProfileLists { get => _profileLists; set => _profileLists = value; }


        public BCITraining()
        {
        }

        public static BCITraining Instance { get; } = new BCITraining();
        public string WantedProfileName { get => _wantedProfileName; set => _wantedProfileName = value; }

        /// <summary>
        /// Initial mental command training.
        /// </summary>
        public void Init()
        {
            _trainingHandler.QueryProfileOK     += OnQueryProfileOK;
            _trainingHandler.CreateProfileOK    += OnCreateProfileOK;
            _trainingHandler.ProfileSavedOK     += OnProfileSavedOK;
            _trainingHandler.TrainingOK         += OnTrainingOK;
            _trainingHandler.GetDetectionInfoOK += OnGetDetectionInfoOK;
            _trainingHandler.ProfileLoaded      += OnProfileLoaded;
            _trainingHandler.ProfileUnLoaded += OnProfileUnLoaded;
            _trainingHandler.GetCurrentProfileDone += OnGetCurrentProfileDone;

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
            _trainingHandler.CreateProfile(profileName, _workingHeadsetId);
        }

        /// <summary>
        /// Load a profile with a specific headset.
        /// If the profile is not exited -> create new profile then load profile
        /// </summary>
        public void LoadProfileWithHeadset(string profileName, string headsetId)
        {
            _wantedProfileName = profileName;
            _workingHeadsetId = headsetId;

            // query profile after that the profile will be created and loaded
            QueryProfile();
        }

        /// <summary>
        /// UnLoad a profile
        /// </summary>
        public void UnLoadProfile(string profileName, string headsetId)
        {
            _trainingHandler.UnLoadProfile(profileName, headsetId);
        }

        /// <summary>
        /// Save a profile
        /// </summary>
        public void SaveProfile(string profileName, string headsetId)
        {
            _trainingHandler.SaveProfile(profileName, headsetId);
        }

        /// <summary>
        /// Start a new training for the specified action.
        /// </summary>
        public void StartTraining(string action, string detection)
        {
            _currAction = action;
            _trainingHandler.DoTraining(action, "start", detection);
        }

        /// <summary>
        /// Accept for current successful training and add it to the profile.
        /// </summary>
        public void AcceptTraining(string detection)
        {
            if (string.IsNullOrEmpty(_currAction))
            {
                UnityEngine.Debug.LogError("AcceptTraining: Invalid action training.");
            }
            else
            {
                _trainingHandler.DoTraining(_currAction, "accept", detection);
            }
            
        }

        /// <summary>
        /// Reject for current successful training. It is not added to the profile.
        /// </summary>
        public void RejectTraining(string detection)
        {
            if (string.IsNullOrEmpty(_currAction))
            {
                UnityEngine.Debug.LogError("RejectTraining: Invalid action training.");
            }
            else
            {
                _trainingHandler.DoTraining(_currAction, "reject", detection);
            }
        }

        /// <summary>
        /// Erase all the training data for the specified action.
        /// </summary>
        public void EraseTraining(string action, string detection)
        {
            _trainingHandler.DoTraining(action, "erase", detection);
        }

        /// <summary>
        /// Cancel the current training.
        /// </summary>
        public void ResetTraining(string action, string detection)
        {
            _trainingHandler.DoTraining(action, "reset", detection);
        }

        // Event handers
        private void OnProfileUnLoaded(object sender, bool e)
        {
            UnityEngine.Debug.Log("OnProfileUnLoaded");
            // TODO: verify unload is for current profile
            _workingHeadsetId = "";
            _wantedProfileName = "";
            InformLoadUnLoadProfileDone(this, false);
        }
        private void OnGetCurrentProfileDone(object sender, JObject data)
        {
            if (data["name"].Type == JTokenType.Null)
            {
                // no profile loaded with the headset. Load profile
                UnityEngine.Debug.Log("OnGetCurrentProfileDone: no profile loaded with the headset");
                _trainingHandler.LoadProfile(_wantedProfileName, _workingHeadsetId);
            }
            else
            {
                string name = data["name"].ToString();
                bool loadByThisApp = (bool)data["loadedByThisApp"];

                if (name != _wantedProfileName)
                {
                    UnityEngine.Debug.LogError("There is profile " + name + " is loaded for headset " + _workingHeadsetId);
                }
                else if (loadByThisApp)
                {
                    InformLoadUnLoadProfileDone(this, true);
                }
                else
                {
                    // the profile is loaded by other apps -> unload
                    _trainingHandler.UnLoadProfile(_wantedProfileName, _workingHeadsetId);
                }
            }
        }
        private void OnProfileLoaded(object sender, string profileName)
        {
            UnityEngine.Debug.Log("BCITraining: OnProfileLoaded profile " + profileName);

            if (profileName == _wantedProfileName)
            {
                InformLoadUnLoadProfileDone(this, true);
            }
            else
            {
                UnityEngine.Debug.LogError("OnProfileLoaded: mismatch profilename");
            }
        }
        private void OnGetDetectionInfoOK(object sender, DetectionInfo detectionInfo)
        {
            UnityEngine.Debug.Log("OnGetDetectionInfoOK: " + detectionInfo.DetectionName);
        }

        private void OnTrainingOK(object sender, JObject result)
        {
            UnityEngine.Debug.Log("OnTrainingOK: " + result);
        }
        private void OnCreateProfileOK(object sender, string profileName)
        {
            UnityEngine.Debug.Log("BCITraining: OnCreateProfileOK profilename " + profileName);
            if (profileName == _wantedProfileName)
            {
                // auto load profile
                _trainingHandler.LoadProfile(_wantedProfileName, _workingHeadsetId);
            }
            else
            {
                UnityEngine.Debug.LogError("BCITraining: OnCreateProfileOK: mismatch profilename ");
            }


        }

        private void OnQueryProfileOK(object sender, List<string> profiles)
        {
            if (profiles.Count == 0)
            {
                // check wanted profile name
                if (!string.IsNullOrEmpty(_wantedProfileName))
                {
                    // create new profile
                    _trainingHandler.CreateProfile(_wantedProfileName, _workingHeadsetId);
                }
            }
            else
            {
                _profileLists = new List<string>(profiles);
                if (string.IsNullOrEmpty(_wantedProfileName))
                {
                    return;
                }
                bool foundProfile = false;

                UnityEngine.Debug.Log("OnQueryProfileOK: number of profiles " +_profileLists.Count.ToString());

                foreach (var profileName in _profileLists)
                {
                    if (_wantedProfileName == profileName)
                    {
                        UnityEngine.Debug.Log("OnQueryProfileOK: the profile" + _wantedProfileName + " is existed.");
                        foundProfile = true;
                        // get current profile
                        _trainingHandler.GetCurrentProfile(_workingHeadsetId);
                        return;
                    }
                }
                if (!foundProfile)
                {
                    // create new profile
                    _trainingHandler.CreateProfile(_wantedProfileName, _workingHeadsetId);
                }
            }
        }

        private void OnProfileSavedOK(object sender, string profileName)
        {
            UnityEngine.Debug.Log("The profile " + profileName + " is saved successfully.");
        }

    }
}
