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

        private string _currMarkerId;

        public static RecordManager Instance { get; } = new RecordManager();

        // Event
        public event EventHandler<Record> informStartRecordResult;
        public event EventHandler<Record> informStopRecordResult;

        public event EventHandler<JObject> informMarkerResult;

        public event EventHandler<string> DataPostProcessingFinished
        {
            add { _ctxClient.DataPostProcessingFinished += value; }
            remove { _ctxClient.DataPostProcessingFinished -= value; }
        }

        public event EventHandler<MultipleResultEventArgs> ExportRecordsFinished
        {
            add { _ctxClient.ExportRecordsFinished += value; }
            remove { _ctxClient.ExportRecordsFinished -= value; }
        }

        // Constructor
        public RecordManager ()
        {
            _sessionHandler.CreateRecordOK  += OnCreateRecordOK;
            _sessionHandler.StopRecordOK    += OnStopRecordOK;
            _ctxClient.InjectMarkerOK += OnInjectMarkerOK;
            _ctxClient.UpdateMarkerOK += OnUpdateMarkerOK;
        }

        private void OnStopRecordOK(object sender, Record record)
        {
            UnityEngine.Debug.Log("RecordManager: OnStopRecordOK recordId: " + record.Uuid +
                                   " at: " + record.EndDateTime);
            informStopRecordResult(this, record);
        }

        private void OnCreateRecordOK(object sender, Record record)
        {
            informStopRecordResult(this, record);
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
        
        public void ExportRecord(List<string> records, string folderPath,
                                 List<string> streamTypes, string format, string version = null,
                                 List<string> licenseIds = null, bool includeDemographics = false,
                                 bool includeMarkerExtraInfos = false, bool includeSurvey = false,
                                 bool includeDeprecatedPM = false)
        {
            _ctxClient.ExportRecord(_authorizer.CortexToken, records, folderPath,
                                    streamTypes, format, version, licenseIds,
                                    includeDemographics, includeMarkerExtraInfos,
                                    includeSurvey, includeDeprecatedPM);
        }

    }
}
