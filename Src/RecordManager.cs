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

        public event EventHandler<Marker> MarkerInjected;
        public event EventHandler<Marker> MarkerUpdated;

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
            informStopRecordResult?.Invoke(this, record);
        }

        private void OnCreateRecordOK(object sender, Record record)
        {
            informStartRecordResult?.Invoke(this, record);
        }
        private void OnInjectMarkerOK(object sender, JObject markerObj)
        {
            _currMarkerId = markerObj["uuid"].ToString();
            MarkerInjected?.Invoke(this, new Marker(markerObj));
        }
        private void OnUpdateMarkerOK(object sender, JObject markerObj)
        {
            MarkerUpdated?.Invoke(this, new Marker(markerObj));
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
        public void InjectMarker(string markerLabel, string markerValue, string port = null, JObject extras = null)
        {
            lock(_locker)
            {
                string cortexToken  = _authorizer.CortexToken;
                string sessionId = _sessionHandler.SessionId;

                // inject marker
                _ctxClient.InjectMarker(cortexToken, sessionId, markerLabel, markerValue, Utils.GetEpochTimeNow(), port, extras);
            }
        }

        /// <summary>
        /// update marker to set the end date time of a marker, turning an "instance" marker into an "interval" marker
        /// </summary>
        /// <param name="markerId">The ID of the marker to update. If null, the most recent marker will be updated.</param>
        /// <param name="extras">Additional information for the marker (optional).</param>
        public void UpdateMarker(string markerId = null, JObject extras = null)
        {
            lock(_locker)
            {
                string cortexToken  = _authorizer.CortexToken;
                string sessionId = _sessionHandler.SessionId;

                if (string.IsNullOrEmpty(markerId) && string.IsNullOrEmpty(_currMarkerId))
                {
                    Debug.LogError("UpdateMarker was called without a valid markerId, and no current marker is available.");
                }
                else if (!string.IsNullOrEmpty(markerId))
                {
                    // update marker
                    _ctxClient.UpdateMarker(cortexToken, sessionId, markerId, Utils.GetEpochTimeNow(), extras);
                }
                else
                {
                    // update the most recent marker if markerId is not provided
                    _ctxClient.UpdateMarker(cortexToken, sessionId, _currMarkerId, Utils.GetEpochTimeNow(), extras);
                }
                
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
