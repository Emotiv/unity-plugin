using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;

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

        private TaskCompletionSource<List<Record>> _queryRecordsTcs; // pending QueryRecords request
        private TaskCompletionSource<ExportRecordResult> _exportRecordTcs; // pending ExportRecord request
        private bool _isExportRecordAsyncPending; // true while an ExportRecordAsync request is awaiting its response

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

        public event EventHandler<MultipleResultEventArgs> ExportRecordsFinished;
        // Constructor
        public RecordManager ()
        {
            _sessionHandler.CreateRecordOK  += OnCreateRecordOK;
            _sessionHandler.StopRecordOK    += OnStopRecordOK;
            _ctxClient.InjectMarkerOK += OnInjectMarkerOK;
            _ctxClient.UpdateMarkerOK += OnUpdateMarkerOK;
            _ctxClient.QueryRecordsDone += OnQueryRecordsDone;
            _ctxClient.ExportRecordsFinished += OnExportRecordsFinished;
            _ctxClient.ErrorMsgReceived += OnErrorMsgReceived;
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
        private void OnQueryRecordsDone(object sender, List<Record> records)
        {
            _queryRecordsTcs?.TrySetResult(records);
        }
        private void OnExportRecordsFinished(object sender, MultipleResultEventArgs e)
        {
            List<string> successRecordIds = new List<string>();
            foreach (JObject item in e.SuccessList ?? new JArray())
            {
                successRecordIds.Add((string)item["recordId"]);
            }

            List<ExportRecordFailure> failedRecords = new List<ExportRecordFailure>();
            foreach (JObject item in e.FailList ?? new JArray())
            {
                failedRecords.Add(new ExportRecordFailure((string)item["recordId"], (int)item["code"], (string)item["message"]));
            }

            if (_isExportRecordAsyncPending)
            {
                _isExportRecordAsyncPending = false;
                _exportRecordTcs?.TrySetResult(new ExportRecordResult(successRecordIds, failedRecords));
            }
            else
            {
                // only raised for the non-async ExportRecord fire-and-forget call
                ExportRecordsFinished?.Invoke(this, e);
            }
        }
        private void OnErrorMsgReceived(object sender, ErrorMsgEventArgs errorInfo)
        {
            if (errorInfo.MethodName == "queryRecords")
            {
                _queryRecordsTcs?.TrySetException(new Exception(errorInfo.MessageError));
                _queryRecordsTcs = null;
            }
            else if (errorInfo.MethodName == "exportRecord")
            {
                _isExportRecordAsyncPending = false;
                _exportRecordTcs?.TrySetException(new Exception(errorInfo.MessageError));
                _exportRecordTcs = null;
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
        
        /// <summary>
        /// Exports one or more records to a folder. Fire-and-forget; kept for backward compatibility.
        /// This call does not return the export result. Use <see cref="ExportRecordAsync"/> if you need to await the result.
        /// See https://emotiv.gitbook.io/cortex-api/records/exportrecord for details.
        /// </summary>
        /// <param name="records">List of record UUIDs to export</param>
        /// <param name="folderPath">Absolute path to the folder for exported files</param>
        /// <param name="streamTypes">List of stream types to include (e.g., "EEG", "MOTION")</param>
        /// <param name="format">Export file format ("EDF", "EDFPLUS", "BDFPLUS", "CSV")</param>
        /// <param name="version">Optional. For "CSV" format, use "V1" or "V2"</param>
        /// <param name="licenseIds">Optional. License IDs for exporting records from other apps</param>
        /// <param name="includeDemographics">Include demographic info</param>
        /// <param name="includeMarkerExtraInfos">Include extra marker info</param>
        /// <param name="includeSurvey">Include survey data</param>
        /// <param name="includeDeprecatedPM">Include deprecated performance metrics</param>
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

        /// <summary>
        /// Exports one or more records to a folder, and waits for the response.
        /// Unlike <see cref="ExportRecord"/>, this returns the ids of records that were exported successfully, and the failed ones with error details.
        /// See https://emotiv.gitbook.io/cortex-api/records/exportrecord for details.
        /// </summary>
        /// <param name="records">List of record UUIDs to export</param>
        /// <param name="folderPath">Absolute path to the folder for exported files</param>
        /// <param name="streamTypes">List of stream types to include (e.g., "EEG", "MOTION")</param>
        /// <param name="format">Export file format ("EDF", "EDFPLUS", "BDFPLUS", "CSV")</param>
        /// <param name="version">Optional. For "CSV" format, use "V1" or "V2"</param>
        /// <param name="licenseIds">Optional. License IDs for exporting records from other apps</param>
        /// <param name="includeDemographics">Include demographic info</param>
        /// <param name="includeMarkerExtraInfos">Include extra marker info</param>
        /// <param name="includeSurvey">Include survey data</param>
        /// <param name="includeDeprecatedPM">Include deprecated performance metrics</param>
        /// <returns>The ids of records that were exported successfully, and the failed ones with error details.</returns>
        public async Task<ExportRecordResult> ExportRecordAsync(List<string> records, string folderPath,
                                 List<string> streamTypes, string format, string version = null,
                                 List<string> licenseIds = null, bool includeDemographics = false,
                                 bool includeMarkerExtraInfos = false, bool includeSurvey = false,
                                 bool includeDeprecatedPM = false)
        {
            if (_exportRecordTcs != null && !_exportRecordTcs.Task.IsCompleted)
                throw new InvalidOperationException("An ExportRecord request is already in progress.");

            _exportRecordTcs = new TaskCompletionSource<ExportRecordResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            _isExportRecordAsyncPending = true;
            _ctxClient.ExportRecord(_authorizer.CortexToken, records, folderPath,
                                    streamTypes, format, version, licenseIds,
                                    includeDemographics, includeMarkerExtraInfos,
                                    includeSurvey, includeDeprecatedPM);
            return await _exportRecordTcs.Task;
        }

        /// <summary>
        /// Query records owned by the current user, and waits for the response.
        /// See https://emotiv.gitbook.io/cortex-api/records/queryrecords for query/orderBy field details.
        /// </summary>
        /// <param name="query">Filter fields (e.g. licenseId, applicationId, keyword, startDatetime, modifiedDatetime, duration). Defaults to no filter.</param>
        /// <param name="orderBy">Sort fields, e.g. [{ "startDatetime": "DESC" }]. Defaults to newest first.</param>
        /// <param name="limit">Maximum number of records to return. Defaults to 10.</param>
        /// <param name="offset">Number of records to skip, for pagination. Defaults to 0.</param>
        /// <param name="includeMarkers">Include the markers linked to each record. Defaults to true.</param>
        /// <param name="includeSyncStatusInfo">Include the "syncStatus" field of each record. Defaults to true.</param>
        /// <returns>The list of records matching the query.</returns>
        public async Task<List<Record>> QueryRecords(JObject query = null, JArray orderBy = null, int limit = 10, int offset = 0,
                                                      bool includeMarkers = true, bool includeSyncStatusInfo = true)
        {
            if (_queryRecordsTcs != null && !_queryRecordsTcs.Task.IsCompleted)
                throw new InvalidOperationException("A QueryRecords request is already in progress.");

            query ??= new JObject();
            orderBy ??= new JArray(new JObject(new JProperty("startDatetime", "DESC")));

            _queryRecordsTcs = new TaskCompletionSource<List<Record>>(TaskCreationOptions.RunContinuationsAsynchronously);
            _ctxClient.QueryRecord(_authorizer.CortexToken, query, orderBy, offset, limit, includeMarkers, includeSyncStatusInfo);
            return await _queryRecordsTcs.Task;
        }

    }
}
