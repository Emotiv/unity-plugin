using System;
using System.Collections.Generic;

namespace EmotivUnityPlugin
{
    /// <summary>
    /// Reponsible for handling sessions and records.
    /// </summary>
    public class SessionHandler
    {
        static readonly object _locker = new object();
        private static string _sessionId = "";
        private CortexClient _ctxClient = CortexClient.Instance;

        //event
        public event EventHandler<SessionEventArgs> SessionActived;
        public event EventHandler<string> SessionClosedOK;
        public event EventHandler<Record> CreateRecordOK;
        public event EventHandler<Record> StopRecordOK;
        public event EventHandler<string> SessionClosedNotify;

        public static SessionHandler Instance { get; } = new SessionHandler();

        /// <summary>
        /// Gets current SessionId.
        /// </summary>
        /// <value>The current SessionId.</value>
        public string SessionId
        {
            get {
                lock (_locker)
                {
                    return _sessionId;
                }
            }
        }

        //Constructor
        public SessionHandler()
        {
            _ctxClient.CreateSessionOK  += CreateSessionOk;
            _ctxClient.UpdateSessionOK  += UpdateSessionOk;
            _ctxClient.CreateRecordOK   += OnCreateRecordOK;
            _ctxClient.UpdateRecordOK   += OnUpdateRecordOK;
            _ctxClient.StopRecordOK     += OnStopRecordOK;
            _ctxClient.SessionClosedNotify += OnSessionClosedNotify;
        }

        private void OnSessionClosedNotify(object sender, string sessionId)
        {
            UnityEngine.Debug.Log("SessionHandler: OnSessionClosedNotify " + sessionId);
            lock (_locker)
            {
                if (_sessionId == sessionId) {
                    // clear session data
                    _sessionId = "";
                    SessionClosedNotify(this, sessionId);
                }
            }
            
        }

        private void OnStopRecordOK(object sender, Record record)
        {
            UnityEngine.Debug.Log("OnStopRecordOK: recordId " + record.Uuid);
            StopRecordOK(this, record);
        }

        private void OnUpdateRecordOK(object sender, Record record)
        {
            UnityEngine.Debug.Log("OnUpdateRecordOK: recordId " + record.Uuid);
            // TODO: emit signal
        }

        private void OnCreateRecordOK(object sender, Record record)
        {
            UnityEngine.Debug.Log("SessionCreator: OnCreateRecordOK recordid " + record.Uuid);
            CreateRecordOK(this, record);
        }

        private void CreateSessionOk(object sender, SessionEventArgs sessionInfo)
        {
            lock(_locker) _sessionId = sessionInfo.SessionId;

            if (sessionInfo.Status == SessionStatus.Activated) {
                UnityEngine.Debug.Log("Session " + sessionInfo.SessionId + " is activated successfully.");
                SessionActived(this, sessionInfo);
            }
            else {
                UnityEngine.Debug.Log("Session " + sessionInfo.SessionId + " is opened successfully.");
            }
            
        }
        private void UpdateSessionOk(object sender, SessionEventArgs sessionInfo)
        {
            
            if (sessionInfo.Status == SessionStatus.Closed)
            {
                lock(_locker) _sessionId = "";
                SessionClosedOK(this, sessionInfo.SessionId);
                
            }
            else if (sessionInfo.Status == SessionStatus.Activated)
            {
                lock(_locker) _sessionId = sessionInfo.SessionId;
                SessionActived(this, sessionInfo);
            }
        }

        /// <summary>
        /// Open a session with an EMOTIV headset.
        /// A application can open only one session at a time with a given headset.
        /// </summary>
        public void Create(string cortexToken, string headsetId, bool activeSession = false)
        {
            if (!String.IsNullOrEmpty(cortexToken) &&
                !String.IsNullOrEmpty(headsetId))
            {
                string status = activeSession ? "active" : "open";
                _ctxClient.CreateSession(cortexToken, headsetId, status);
            }
            else {
                UnityEngine.Debug.Log("CreateSession: Invalid parameters");
            }
            
        }

        /// <summary>
        /// Close the current session.
        /// </summary>
        public void CloseSession(string cortexToken)
        {
            lock(_locker)
            {
                if (!String.IsNullOrEmpty(_sessionId)) {
                    _ctxClient.UpdateSession(cortexToken, _sessionId, "close");
                }
            }
            
        }

        /// <summary>
        /// Create a new record.
        /// </summary>
        public void StartRecord(string cortexToken, string title, 
                                string description = null, string subjectName = null, List<string> tags= null)
        {
            lock(_locker)
            {
                if (!String.IsNullOrEmpty(_sessionId)) {
                    _ctxClient.CreateRecord(cortexToken, _sessionId, title, description, subjectName, tags);
                }
                else
                {
                    UnityEngine.Debug.Log("StartRecord: invalid sessionId.");
                }
            }
            
        }
        
        /// <summary>
        /// Stop a record that was previously started by createRecord
        /// </summary>
        public void StopRecord(string cortexToken)
        {
            lock(_locker)
            {
                if (!String.IsNullOrEmpty(_sessionId)) {
                    _ctxClient.StopRecord(cortexToken, _sessionId);
                }
                else
                {
                    UnityEngine.Debug.Log("StopRecord: invalid sessionId.");
                }
            }
        }
        
        /// <summary>
        /// Update a record.
        /// </summary>
        public void UpdateRecord(string cortexToken, string recordId, string title = null, 
                                string description = null, List<string> tags = null)
        {
            lock(_locker)
            {
                if (!String.IsNullOrEmpty(_sessionId)) {
                    _ctxClient.UpdateRecord(cortexToken, recordId, title, description, tags);
                }
                else
                {
                    UnityEngine.Debug.Log("StartRecord: invalid sessionId.");
                }
            }
        }
        // inject marker


        /// <summary>
        /// Clear current session Data.
        /// </summary>
        public void ClearSessionData() {
            lock(_locker)
            {
                _sessionId      = "";
            } 
        }
    }
}
