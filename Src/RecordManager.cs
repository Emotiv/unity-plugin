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

        private string _currRecordId;
        private string _currMarkerId;

        public static RecordManager Instance { get; } = new RecordManager();
        
        // Event
        public event EventHandler<Record> informStartRecordResult;
        public event EventHandler<Record> informStopRecordResult;

        public event EventHandler<JObject> informMarkerResult;

        // Constructor
        public RecordManager ()
        {
            _sessionHandler.CreateRecordOK  += OnCreateRecordOK;
            _sessionHandler.StopRecordOK    += OnStopRecordOK;
            _ctxClient.InjectMarkerOK += OnInjectMarkerOK;
            _ctxClient.UpdateMarkerOK += OnUpdateMarkerOK;
            _ctxClient.ErrorMsgReceived += MessageErrorRecieved;
        }

        private void OnStopRecordOK(object sender, Record record)
        {
            UnityEngine.Debug.Log("RecordManager: OnStopRecordOK recordId: " + record.Uuid + 
                                   " at: " + record.EndDateTime);
            informStopRecordResult(this, record);
            _currRecordId = "";
        }

        private void OnCreateRecordOK(object sender, Record record)
        {
            informStopRecordResult(this, record);
            _currRecordId = record.Uuid;
            informStartRecordResult(this, record);
        }
        private void OnInjectMarkerOK(object sender, JObject markerObj)
        {
            _currMarkerId = markerObj["uuid"].ToString();
            informMarkerResult(this, markerObj);
        }
        private void OnUpdateMarkerOK(object sender, JObject markerObj)
        {
            informMarkerResult(this, markerObj);
        }

        private void MessageErrorRecieved(object sender, ErrorMsgEventArgs errorInfo)
        {
            
            string message  = errorInfo.MessageError;
            string method   = errorInfo.MethodName;
            int errorCode   = errorInfo.Code;
            UnityEngine.Debug.Log("MessageErrorRecieved :code " + errorCode
                                   + " message " + message 
                                   + "method name " + method);
            //string errorMsg = method +" gets error: "+ message;
            //if (method == "injectMarker" || method == "updateMarker") {
            //    informMarkerResult(this, errorMsg);
            //}
            //else if (method == "createRecord" || method == "stopRecord") {
            //    informRecordResult(this, errorMsg);
            //}
        }


        /// <summary>
        /// Create a new record.
        /// </summary>
        public void StartRecord(string title, string description = null, 
                                 string subjectName = null, List<string> tags= null)
        {
            lock(_locker)
            {
                // start record
                _sessionHandler.StartRecord(_authorizer.CortexToken, title, description, subjectName, tags);
            }
        }

        /// <summary>
        /// Stop a record that was previously started by StartRecord
        /// </summary>
        public void StopRecord()
        {
            lock(_locker)
            {
                _sessionHandler.StopRecord(_authorizer.CortexToken);
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

                // inject marker
                _ctxClient.InjectMarker(cortexToken, sessionId, markerLabel, markerValue, Utils.GetEpochTimeNow());
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

                // update marker
                _ctxClient.UpdateMarker(cortexToken, sessionId, _currMarkerId, Utils.GetEpochTimeNow());
            }
        }

    }
}
