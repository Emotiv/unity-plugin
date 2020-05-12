using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

namespace EmotivUnityPlugin
{
    /// <summary>
    /// Reponsible for managing and handling records and markers.
    /// </summary>
    public class RecordManager
    {
        static readonly object _locker = new object();
        private CortexClient   _ctxClient       = CortexClient.Instance;
        private Authorizer     _authorizer      = Authorizer.Instance;
        private SessionHandler _sessionHandler  = SessionHandler.Instance;

        private bool _isSessionActived;

        public static RecordManager Instance { get; } = new RecordManager();
        
        // Event

        // Constructor
        public RecordManager ()
        {
            _isSessionActived = false;
            _sessionHandler.SessionActived  += OnSessionActived;
            _sessionHandler.CreateRecordOK  += OnCreateRecordOK;
            _sessionHandler.StopRecordOK    += OnStopRecordOK;
            
        }

        private void OnStopRecordOK(object sender, string recordId)
        {
            UnityEngine.Debug.Log("RecordManager: OnStopRecordOK recordId: " + recordId + 
                                   " at: " + Utils.GetEpochTimeNow());
        }

        private void OnCreateRecordOK(object sender, Record record)
        {
            UnityEngine.Debug.Log("RecordManager: OnCreateRecordOK recordId: " + record.Uuid 
                                   + " title: " + record.Title + " at " + Utils.GetEpochTimeNow());
            // Only for test
            // Thread.Sleep(10000); // sleep 10 seconds
            // stop records
            // string cortexToken  = _authorizer.CortexToken;
            // _sessionCreator.StopRecord(cortexToken);

        }

        private void OnSessionActived(object sender, SessionEventArgs e)
        {
            _isSessionActived = true;
            UnityEngine.Debug.Log("RecordManager: OnSessionActived sessionId: " + e.SessionId);
        }


        /// <summary>
        /// Create a new record.
        /// </summary>
        public void StartRecord(string title, string description = null, 
                                 string subjectName = null, List<string> tags= null)
        {
            lock(_locker)
            {
                string cortexToken  = _authorizer.CortexToken;
                if (!String.IsNullOrEmpty(cortexToken) && _isSessionActived) {
                    // start record
                    _sessionHandler.StartRecord(cortexToken, title, description, subjectName, tags);
                }
            }
        }

        /// <summary>
        /// Stop a record that was previously started by StartRecord
        /// </summary>
        public void StopRecord()
        {
            lock(_locker)
            {
                string cortexToken  = _authorizer.CortexToken;
                if (!String.IsNullOrEmpty(cortexToken))
                {
                    _sessionHandler.StopRecord(cortexToken);
                }
                
            }
        }
        // TODO: Update Record

        // TODO: Inject Marker

    }
}
