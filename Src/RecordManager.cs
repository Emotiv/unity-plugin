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

        private string _currRecordId;
        private string _currMarkerId;

        public static RecordManager Instance { get; } = new RecordManager();
        
        // Event
        public event EventHandler<string> informRecordResult;

        public event EventHandler<string> informMarkerResult;

        // Constructor
        public RecordManager ()
        {
            _isSessionActived = false;
            _sessionHandler.SessionActived  += OnSessionActived;
            _sessionHandler.CreateRecordOK  += OnCreateRecordOK;
            _sessionHandler.StopRecordOK    += OnStopRecordOK;
            _ctxClient.InjectMarkerOK += OnInjectMarkerOK;
            _ctxClient.UpdateMarkerOK += OnUpdateMarkerOK;
            _ctxClient.ErrorMsgReceived += MessageErrorRecieved;
        }

        private void OnStopRecordOK(object sender, string recordId)
        {
            UnityEngine.Debug.Log("RecordManager: OnStopRecordOK recordId: " + recordId + 
                                   " at: " + Utils.GetEpochTimeNow());
            if (recordId == _currRecordId) {
                string stopRecordResult = "The record is stopped successfully.\n";
                stopRecordResult += "recordId: " + recordId;
                informRecordResult(this, stopRecordResult);
                _currRecordId = "";
            }
        }

        private void OnCreateRecordOK(object sender, Record record)
        {
            UnityEngine.Debug.Log("RecordManager: OnCreateRecordOK recordId: " + record.Uuid 
                                   + " title: " + record.Title + " at " + Utils.GetEpochTimeNow());
            
            string startRecordResult = "The record is created successfully.\n";
            startRecordResult += "recordId: " + record.Uuid +"\n title: " + record.Title;
            informRecordResult(this, startRecordResult);
            _currRecordId = record.Uuid;
        }

        private void OnSessionActived(object sender, SessionEventArgs e)
        {
            _isSessionActived = true;
            UnityEngine.Debug.Log("RecordManager: OnSessionActived sessionId: " + e.SessionId);
        }

        private void OnInjectMarkerOK(object sender, JObject markerObj)
        {
            Debug.Log("OnInjectMarkerOK " + markerObj);
            _currMarkerId = markerObj["uuid"].ToString();
            string markerType = markerObj["type"].ToString();
            string markerStartTime = markerObj["startDatetime"].ToString();
            string markerEndTime = markerObj["endDatetime"].ToString();
            string markerLabel = markerObj["label"].ToString();
            string markerValue = markerObj["value"].ToString();

            string injectMarkerResult = "The marker is injected successfully.\n";
            injectMarkerResult += "markerId: " + _currMarkerId +"\n type: " + markerType
                                 +"\n label: " + markerLabel +"\n value: " + markerValue
                                 +"\n startDatetime: " + markerStartTime +"\n endDatetime: " + markerEndTime;
            informMarkerResult(this, injectMarkerResult);
        }
        private void OnUpdateMarkerOK(object sender, JObject markerObj)
        {
            Debug.Log("OnUpdateMarkerOK " + markerObj);
            string markerType = markerObj["type"].ToString();
            string markerStartTime = markerObj["startDatetime"].ToString();
            string markerEndTime = markerObj["endDatetime"].ToString();
            string markerLabel = markerObj["label"].ToString();
            string markerValue = markerObj["value"].ToString();

            string updateMarkerResult = "The marker is updated successfully.\n";
            updateMarkerResult += "markerId: " + _currMarkerId +"\n type: " + markerType
                                 +"\n label: " + markerLabel +"\n value: " + markerValue
                                 +"\n startDatetime: " + markerStartTime +"\n endDatetime: " + markerEndTime;
            informMarkerResult(this, updateMarkerResult);
        }

        private void MessageErrorRecieved(object sender, ErrorMsgEventArgs errorInfo)
        {
            
            string message  = errorInfo.MessageError;
            string method   = errorInfo.MethodName;
            int errorCode   = errorInfo.Code;
            UnityEngine.Debug.Log("MessageErrorRecieved :code " + errorCode
                                   + " message " + message 
                                   + "method name " + method);
            string errorMsg = method +" gets error: "+ message;
            if (method == "injectMarker" || method == "updateMarker") {
                informMarkerResult(this, errorMsg);
            }
            else if (method == "createRecord" || method == "stopRecord") {
                informRecordResult(this, errorMsg);
            }
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

        /// <summary>
        /// inject marker
        /// </summary>
        public void InjectMarker(string markerLabel, string markerValue)
        {
            lock(_locker)
            {
                string cortexToken  = _authorizer.CortexToken;
                string sessionId = _sessionHandler.SessionId;

                if (!String.IsNullOrEmpty(cortexToken) && !String.IsNullOrEmpty(sessionId)) {
                    // inject marker
                    _ctxClient.InjectMarker(cortexToken, sessionId, markerLabel, markerValue, Utils.GetEpochTimeNow());
                }
            }
        }

        /// <summary>
        /// update marker to set the end date time of a marker, turning an "instance" marker into an "interval" marker
        /// </summary>
        public void UpdateMarker()
        {
            lock(_locker)
            {
                string cortexToken  = _authorizer.CortexToken;
                string sessionId = _sessionHandler.SessionId;

                if (!String.IsNullOrEmpty(cortexToken) && !String.IsNullOrEmpty(sessionId)) {
                    // update marker
                    _ctxClient.UpdateMarker(cortexToken, sessionId, _currMarkerId, Utils.GetEpochTimeNow());
                }
            }
        }

    }
}
