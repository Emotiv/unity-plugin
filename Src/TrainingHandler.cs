using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace EmotivUnityPlugin
{
    /// <summary>
    /// Reponsible for training.
    /// </summary>
    public class TrainingHandler
    {

        static readonly object _locker = new object();
        private CortexClient _ctxClient = CortexClient.Instance;
        private string _profileName = ""; // must not existed

        private string _currDetection = "";
        private bool _isProfileLoaded;
        private string _headsetId; // Id of current headset

        private HeadsetFinder _headsetFinder    = HeadsetFinder.Instance;
        private Authorizer _authorizer          = Authorizer.Instance;
        private SessionHandler _sessionHandler  = SessionHandler.Instance;
        

        // event
        public event EventHandler<List<string>> QueryProfileOK;
        public event EventHandler<string> ProfileLoaded;
        public event EventHandler<bool> ProfileUnLoaded;
        public event EventHandler<bool> TrainingSucceeded;
        public event EventHandler<bool> ReadyForTraning;

        public event EventHandler<string> CreateProfileOK
        {
            add { _ctxClient.CreateProfileOK += value; }
            remove { _ctxClient.CreateProfileOK -= value; }
        }

        public event EventHandler<string> ProfileSavedOK
        {
            add { _ctxClient.SaveProfileOK += value; }
            remove { _ctxClient.SaveProfileOK -= value; }
        }

        public event EventHandler<JObject> TrainingOK
        {
            add { _ctxClient.TrainingOK += value; }
            remove { _ctxClient.TrainingOK -= value; }
        }

        public event EventHandler<DetectionInfo> GetDetectionInfoOK;

        public static TrainingHandler Instance { get; } = new TrainingHandler();

        //Constructor
        public TrainingHandler()
        {
            _isProfileLoaded = false;
            // Event register
            _ctxClient.GetDetectionInfoDone += OnGetDetectionOk;
            _ctxClient.LoadProfileOK        += OnProfileLoadedOK;
            _ctxClient.UnloadProfileDone    += OnUnloadProfileDone;
            _ctxClient.QueryProfileOK       += OnQueryProfileOK;
        }

        private void OnUnloadProfileDone(object sender, bool isSuccess)
        {
            if (isSuccess == true) {
                lock(_locker) _profileName = "";
            }
            ProfileUnLoaded(this, isSuccess);
        }

        private void OnQueryProfileOK(object sender, JArray profiles)
        {
            UnityEngine.Debug.Log("QueryProfileOK" + profiles);
            List<string> profileLists = new List<string>();
            foreach (JObject ele in profiles)
            {
                string name = (string)ele["name"];
                profileLists.Add(name);
            }
            QueryProfileOK(this, profileLists);
        }

        private void OnProfileLoadedOK(object sender, string profileName)
        {
            _profileName = profileName;
            _isProfileLoaded = true;
            ProfileLoaded(this, profileName);
        }

        private void OnGetDetectionOk(object sender, JObject data)
        {
            UnityEngine.Debug.Log("GetDetectionInfoOK: " + data);
            DetectionInfo detectioninfo = new DetectionInfo(_currDetection);

            JArray actions = (JArray)data["actions"];
            foreach (var ele in actions) {
                detectioninfo.Actions.Add(ele.ToString());
            }
            JArray controls = (JArray)data["controls"];
            foreach (var ele in actions) {
                detectioninfo.Controls.Add(ele.ToString());
            }
            JArray events = (JArray)data["events"];
            foreach (var ele in actions) {
                detectioninfo.Events.Add(ele.ToString());
            }
            JArray signature = (JArray)data["signature"];
            foreach (var ele in actions) {
                detectioninfo.Signature.Add(ele.ToString());
            }
            GetDetectionInfoOK(this, detectioninfo);
        }

        /// <summary>
        /// Clear.
        /// </summary>
        public void Clear()
        {
            lock(_locker)
            {
                _headsetId          = "";
                _profileName        = "";
                _isProfileLoaded    = false;
            }
        }

        public void QueryProfile()
        {
            // query profiles
            string cortexToken = _authorizer.CortexToken;
            _ctxClient.QueryProfile(cortexToken);
        }
        
        /// <summary>
        /// Get useful detection information.
        /// </summary>
        public void GetDetectionInfo(string detection)
        {
            lock(_locker) _currDetection = detection;
            string cortexToken = _authorizer.CortexToken;
            _ctxClient.GetDetectionInfo(detection);
        }
        public void SetHeadset(string headsetId)
        {
            lock(_locker) _headsetId = headsetId; 
        }

        public void DoTraining(string action, string status, string detection)
        {
            UnityEngine.Debug.Log(status + " " + action + " training.");
            if (!_isProfileLoaded) {
                UnityEngine.Debug.Log("The profile for training have not still be loaded. Please wait");
                return;
            }
            //Do training
            string cortexToken  = _authorizer.CortexToken;
            string sessionId    = _sessionHandler.SessionId;
            _ctxClient.Training(cortexToken, sessionId, status, detection, action);
        }

        public void CreateProfile(string profileName)
        {
            string cortexToken  = _authorizer.CortexToken;
            _ctxClient.SetupProfile(cortexToken, profileName, "create");
        }

        public void LoadProfile(string profileName)
        {
            
            string cortexToken = _authorizer.CortexToken;
            _ctxClient.SetupProfile(cortexToken, profileName, "load", _headsetId);
        }

        public void UnLoadProfile()
        {
            string cortexToken = _authorizer.CortexToken;
            _ctxClient.SetupProfile(cortexToken, _profileName, "unload", _headsetId);
        }

        public void SaveProfile()
        {
            string cortexToken = _authorizer.CortexToken;
            _ctxClient.SetupProfile(cortexToken, _profileName, "save", _headsetId);
        }
    }
}
