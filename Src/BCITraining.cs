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

        private string _unloadingProfileName = "";

        private string _workingHeadsetId = "";
        private string _currAction = "";


        // event
        public event EventHandler<string> InformLoadProfileDone;
        public event EventHandler<string> InformUnLoadProfileDone;

        public event EventHandler<bool> SetMentalCommandActionSensitivityOK
        {
            add { _trainingHandler.SetMentalCommandActionSensitivityOK += value; }
            remove { _trainingHandler.SetMentalCommandActionSensitivityOK -= value; }
        }

        // forward event InformTrainedSignatureActions
        public event EventHandler<Dictionary<string, int>> InformTrainedSignatureActions
        {
            add { _trainingHandler.InformTrainedSignatureActions += value; }
            remove { _trainingHandler.InformTrainedSignatureActions -= value; }
        }

        // forward event ProfileSavedOK from TrainingHandler
        public event EventHandler<string> ProfileSavedOK
        {
            add { _trainingHandler.ProfileSavedOK += value; }
            remove { _trainingHandler.ProfileSavedOK -= value; }
        }

        // forward event GetMentalCommandActionSensitivityOK
        public event EventHandler<List<int>> GetMentalCommandActionSensitivityOK
        {
            add { _trainingHandler.GetMentalCommandActionSensitivityOK += value; }
            remove { _trainingHandler.GetMentalCommandActionSensitivityOK -= value; }
        }

        public event EventHandler<string> InformEraseDone; // inform erase done for action

        public event EventHandler<string> InformUnsupportedDeviceForProfile; // inform unsupported device for profile

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
            _unloadingProfileName = profileName;    
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


        // Set sensitivity for mental command
        public void SetMentalCommandActionSensitivity(string profileName, List<int> levels)
        {
            _trainingHandler.SetMentalCommandSensitivity( profileName, levels);
        }

        // get sensitivity for mental command
        public void GetMentalCommandActionSensitivity(string profileName)
        {
            _trainingHandler.GetMentalCommandSensitivity(profileName);
        }

        // get trained signature actions
        public void GetTrainedSignatureActions(string detection, string profileName = "")
        {
            _trainingHandler.GetTrainedSignatureActions(detection, profileName);
        }

        // Event handers
        private void OnProfileUnLoaded(object sender, bool e)
        {
            
            if (!string.IsNullOrEmpty(_unloadingProfileName) && _unloadingProfileName == _wantedProfileName)
            {
                UnityEngine.Debug.Log("OnProfileUnLoaded: the profile " + _unloadingProfileName + " is unloaded successfully.");
                InformUnLoadProfileDone(this, _wantedProfileName);
                _workingHeadsetId = "";
                _wantedProfileName = "";
            }
            else if (_unloadingProfileName != _wantedProfileName) {
                _unloadingProfileName = "";
                UnityEngine.Debug.Log("OnProfileUnLoaded: the profile " + _unloadingProfileName + " is unloaded successfully. But it is not the wanted profile " + _wantedProfileName);
                // Load wanted profile after unloading the current profile
                if (!string.IsNullOrEmpty(_wantedProfileName) && !string.IsNullOrEmpty(_workingHeadsetId))
                {
                    // load wanted profile
                    LoadProfileWithHeadset(_wantedProfileName, _workingHeadsetId);
                }
            }
        }
        private void OnGetCurrentProfileDone(object sender, JObject data)
        {
            if (data["name"].Type == JTokenType.Null)
            {
                // no profile loaded with the headset. Load profile
                UnityEngine.Debug.Log("OnGetCurrentProfileDone: no profile loaded for the headset " + _workingHeadsetId);
                _trainingHandler.LoadProfile(_wantedProfileName, _workingHeadsetId);
            }
            else
            {
                string name = data["name"].ToString();
                bool loadByThisApp = (bool)data["loadedByThisApp"];

                if (loadByThisApp) {
                    UnityEngine.Debug.Log("OnGetCurrentProfileDone: the profile " + name + " is loaded by this app.");
                    if (name != _wantedProfileName)
                    {
                        UnityEngine.Debug.LogError("There is profile " + name + " is loaded for headset " + _workingHeadsetId + " but not " + _wantedProfileName);
                        // the profile is loaded by this app -> unload
                        _unloadingProfileName = name;
                        _trainingHandler.UnLoadProfile(name, _workingHeadsetId);
                    }
                    else
                    {
                        InformLoadProfileDone(this, name);
                    }
                }
                else
                {
                    UnityEngine.Debug.LogError("OnGetCurrentProfileDone: the profile " + name + " is loaded by other apps.");
                }
            }
        }
        private void OnProfileLoaded(object sender, string profileName)
        {
            UnityEngine.Debug.Log("BCITraining: OnProfileLoaded profile " + profileName);

            if (profileName == _wantedProfileName)
            {
                InformLoadProfileDone(this, profileName);
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
            string action = result["action"].ToString();
            string status = result["status"].ToString();
            string message = result["message"].ToString();
            if (status == "erase")
            {
                InformEraseDone(this, action);
            }
            UnityEngine.Debug.Log("OnTrainingOK: " + action + " status:" + status + " message:" + message);
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

        private void OnQueryProfileOK(object sender, List<EmoProfile> profiles)
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
                _profileLists = new List<string>();
                bool foundProfile = false;
                bool isDeviceSupported = false;
                foreach (var profile in profiles)
                {
                    _profileLists.Add(profile.ProfileName);
                    if (profile.ProfileName == _wantedProfileName)
                    {
                        foundProfile = true;
                        if (!string.IsNullOrEmpty(_workingHeadsetId))
                        {
                            // Check if the profile supports the current headset
                            isDeviceSupported = profile.IsDeviceSupported(_workingHeadsetId);
                        }
                    }
                }

                if (!foundProfile)
                {
                    // create new profile
                    _trainingHandler.CreateProfile(_wantedProfileName, _workingHeadsetId);
                }
                else if (isDeviceSupported)
                {
                    // load profile
                    _trainingHandler.GetCurrentProfile(_workingHeadsetId);
                }
                else
                {
                    InformUnsupportedDeviceForProfile?.Invoke(this, _wantedProfileName);
                }
            }
        }

        private void OnProfileSavedOK(object sender, string profileName)
        {
            UnityEngine.Debug.Log("The profile " + profileName + " is saved successfully.");
        }

    }
}
