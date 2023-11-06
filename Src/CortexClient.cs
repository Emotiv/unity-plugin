#region License
// Copyright (c) 2007 James Newton-King
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Threading;
using WebSocket4Net;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Timers;

namespace EmotivUnityPlugin
{
    /// <summary>
    /// Represents a simple client for the Cortex service.
    /// </summary>
    public class CortexClient
    {
        const string Url = "wss://localhost:6868";
        static readonly object _locker = new object();
        private Dictionary<int, string> _methodForRequestId;

        /// <summary>
        /// Websocket Client.
        /// </summary>
        private WebSocket _wSC;

        /// <summary>
        /// Unique id for each request
        /// </summary>
        /// <remarks>The id will be reset to 0 when reach to 100</remarks>
        private int _nextRequestId;
        
        /// <summary>
        /// Timer for connecting to Emotiv Cortex Service
        /// </summary>
        private System.Timers.Timer _wscTimer = null;
        
        private AutoResetEvent m_MessageReceiveEvent = new AutoResetEvent(false);
        private AutoResetEvent m_OpenedEvent = new AutoResetEvent(false);

        public event EventHandler<bool> WSConnectDone;
        public event EventHandler<ErrorMsgEventArgs> ErrorMsgReceived;
        public event EventHandler<StreamDataEventArgs> StreamDataReceived;
        public event EventHandler<List<Headset>> QueryHeadsetOK;
        public event EventHandler<HeadsetConnectEventArgs> HeadsetConnectNotify;
        public event EventHandler HeadsetDisconnected;
        public event EventHandler<bool> HeadsetDisConnectedOK;
        public event EventHandler<bool> HasAccessRightOK;
        public event EventHandler<bool> ORequestAccessDone;
        public event EventHandler<bool> AccessRightGrantedDone;
        public event EventHandler<string> AuthorizeOK;
        public event EventHandler<UserDataInfo> GetUserLoginDone;
        public event EventHandler<string> EULAAccepted;
        public event EventHandler<string> EULANotAccepted;
        public event EventHandler<string> UserLoginNotify;
        public event EventHandler<string> UserLogoutNotify;
        public event EventHandler<License> GetLicenseInfoDone;
        public event EventHandler<SessionEventArgs> CreateSessionOK;
        public event EventHandler<SessionEventArgs> UpdateSessionOK;
        public event EventHandler<MultipleResultEventArgs> SubscribeDataDone;
        public event EventHandler<MultipleResultEventArgs> UnSubscribeDataDone;
        public event EventHandler<Record> CreateRecordOK;
        public event EventHandler<Record> StopRecordOK;
        public event EventHandler<Record> UpdateRecordOK;
        public event EventHandler<List<Record>> QueryRecordsDone;
        public event EventHandler<MultipleResultEventArgs> DeleteRecordsDone;
        public event EventHandler<JObject> InjectMarkerOK;
        public event EventHandler<JObject> UpdateMarkerOK;
        public event EventHandler<JObject> GetDetectionInfoDone;
        public event EventHandler<JObject> GetCurrentProfileDone;
        public event EventHandler<string> CreateProfileOK;
        public event EventHandler<string> LoadProfileOK;
        public event EventHandler<string> SaveProfileOK;
        public event EventHandler<bool> UnloadProfileDone;
        public event EventHandler<string> DeleteProfileOK;
        public event EventHandler<string> RenameProfileOK;
        public event EventHandler<JArray> QueryProfileOK;
        public event EventHandler<double> GetTrainingTimeDone;
        public event EventHandler<JObject> TrainingOK;
        public event EventHandler<string> StreamStopNotify;
        public event EventHandler<string> SessionClosedNotify;
        public event EventHandler<string> RefreshTokenOK;
        public event EventHandler<string> HeadsetScanFinished;

        public event EventHandler<bool> BTLEPermissionGrantedNotify; // notify btle permision grant status

        private CortexClient()
        {
            
        }
        public void InitWebSocketClient()
        {
            _nextRequestId = 1;
            _wSC = new WebSocket(Config.AppUrl);
            // Since Emotiv Cortex 3.7.0, the supported SSL Protocol will be TLS1.2 or later
            _wSC.Security.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;

            _methodForRequestId = new Dictionary<int, string>();

            _wSC.Opened += new EventHandler(WebSocketClient_Opened);
            _wSC.Error  += new EventHandler<SuperSocket.ClientEngine.ErrorEventArgs>(WebSocketClient_Error);
            _wSC.Closed += WebSocketClient_Closed;
            _wSC.MessageReceived += WebSocketClient_MessageReceived;
            _wSC.DataReceived += WebSocketClient_DataReceived;
        }

        public void ForceCloseWSC()
        {
            UnityEngine.Debug.Log("Force close websocket client.");
            if (_wscTimer != null) {
                _wscTimer = null;
            }
            // stop websocket client
            if (_wSC != null)
                _wSC.Close();
        }

        /// <summary>
        /// Singleton Instance of Cortex Client
        /// </summary>
        public static CortexClient Instance { get; } = new CortexClient();


        /// <summary>
        /// Set up timer for connecting to Emotiv Cortex service
        /// </summary>
        private void SetWSCTimer() {
            if (_wscTimer != null)
                return;
            _wscTimer = new System.Timers.Timer(Config.RETRY_CORTEXSERVICE_TIME);
            // Hook up the Elapsed event for the timer.
            _wscTimer.Elapsed       += OnTimerEvent;
            _wscTimer.AutoReset     = false; // do not auto reset
            _wscTimer.Enabled       = true; 
        }

        /// <summary>
        /// Handle for _wscTimer timer timeout
        //  Retry Connect when time out 
        /// </summary>
        private void OnTimerEvent(object sender, ElapsedEventArgs e)
        {
            UnityEngine.Debug.Log("OnTimerEvent: Retry connect to CortexService....");
            RetryConnect();
        }

        private void RetryConnect() {
           m_OpenedEvent.Reset();
            if (_wSC == null || (_wSC.State != WebSocketState.None && _wSC.State != WebSocketState.Closed))
                return;
            
            _wSC.Open();
        }

        private void WebSocketClient_DataReceived(object sender, DataReceivedEventArgs e)
        {
            // TODO
            UnityEngine.Debug.Log("WebSocketClient_DataReceived");
        }

        /// <summary>
        /// Build a json rpc request and send message via websocket
        /// </summary>
        private void SendTextMessage(JObject param, string method, bool hasParam = true)
        {
            lock(_locker)
            {
                JObject request = new JObject(
                new JProperty("jsonrpc", "2.0"),
                new JProperty("id", _nextRequestId),
                new JProperty("method", method));

                if (hasParam) {
                    request.Add("params", param);
                }
                // UnityEngine.Debug.Log("Send " + method);
                // UnityEngine.Debug.Log(request.ToString());

                // send the json message
                _wSC.Send(request.ToString());

                // add to dictionary, replace if a key is existed
                _methodForRequestId[_nextRequestId] = method;

                if (_nextRequestId > 100) {
                    _nextRequestId = 1;
                }
                else
                    _nextRequestId++;
            }
        }

        /// <summary>
        /// Handle message received return from Emotiv Cortex Service
        /// </summary> 
        private void WebSocketClient_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            string receievedMsg = e.Message;
            //UnityEngine.Debug.Log("WebSocketClient_MessageReceived " + receievedMsg);

            JObject response = JObject.Parse(e.Message);

            if (response["id"] != null)
            {
                string method = ""; // method name
                lock (_locker)
                {
                    int id = (int)response["id"];
                    method = _methodForRequestId[id];
                    _methodForRequestId.Remove(id);
                }
                
                
                if (response["error"] != null)
                {
                    JObject error = (JObject)response["error"];
                    int code = (int)error["code"];
                    string messageError = (string)error["message"];
                    UnityEngine.Debug.Log("An error received: " + messageError);
                    //Send Error message event
                    ErrorMsgReceived(this, new ErrorMsgEventArgs(code, messageError, method));
                    
                } else {
                    // handle response
                    JToken data = response["result"];
                    HandleResponse(method, data);
                    // check bluetooth permisison granted
                    if (method == "queryHeadsets" && response.ContainsKey("attention")) {
                        JObject attentionObj =  (JObject)response["attention"];
                        bool btlePermisionGranted = true;
                        if ((int)attentionObj["code"] == WarningCode.BTLEPermissionNotGranted) {
                            btlePermisionGranted = false;
                        }
                        BTLEPermissionGrantedNotify(this, btlePermisionGranted);
                    }
                }
            }
            else if (response["sid"] != null)
            {
                string sid = (string)response["sid"];
                double time = 0;
                if (response["time"] != null)
                    time = (double)response["time"];

                foreach (JProperty property in response.Properties()) {
                    if (property.Name != "sid" && property.Name != "time") {
                        ArrayList data = new ArrayList();
                        data.Add(time); // insert timestamp to datastream
                        // spread to one array intead of a array included in a array
                        foreach( var ele in property.Value){
                            if (ele.Type == JTokenType.Array){
                                foreach (var item in ele){
                                    if (item.Type == JTokenType.Object)
                                    {
                                        // Ignore marker data 
                                        UnityEngine.Debug.Log("marker object " + item); 
                                    }
                                    else
                                        data.Add(Convert.ToDouble(item));
                                }
                            }
                            else if (ele.Type == JTokenType.String){
                                data.Add(Convert.ToString(ele));
                            }
                            else if (ele.Type == JTokenType.Boolean){
                                data.Add(Convert.ToBoolean(ele));
                            }
                            else if (ele.Type == JTokenType.Null){
                                data.Add(-1); // use -1 for null value
                            }
                            else {
                                data.Add(Convert.ToDouble(ele));
                            }
                        }
                        // UnityEngine.Debug.Log("WebSocketClient_MessageReceived: name " + property.Name + " count " + data.Count);
                        StreamDataReceived(this, new StreamDataEventArgs(sid, data, property.Name));
                    }
                }
            }
            else if (response["warning"] != null)
            {
                JObject warning = (JObject)response["warning"];
                int code = -1;
                if (warning["code"] != null) {
                    code = (int)warning["code"];
                }
                JToken messageData = warning["message"];
                HandleWarning(code, messageData);
            }
        }

        /// <summary>
        /// Handle response message. 
        /// A response means success message to distinguish from error object
        /// </summary>
        private void HandleResponse(string method, JToken data)
        {
            // UnityEngine.Debug.Log("handleResponse: " + method);
            if (method == "queryHeadsets")
            {
                List<Headset> headsetLists = new List<Headset>();
                foreach (JObject item in data) {
                    headsetLists.Add(new Headset(item));
                }
                QueryHeadsetOK(this, headsetLists);
            }
            else if (method == "controlDevice")
            {
                string command = (string)data["command"];
                if (command == "disconnect")
                {
                    HeadsetDisConnectedOK(this, true);
                }
            }
            else if (method == "getUserLogin")
            {
                JArray users = (JArray)data;
                UserDataInfo loginData = new UserDataInfo();
                if (users.Count > 0)
                {
                    foreach (JObject user in users)
                    {
                        if (user["currentOSUId"].ToString() == user["loggedInOSUId"].ToString()) {
                            loginData.EmotivId      = user["username"].ToString();
                            DateTime lastLoginTime  = user.Value<DateTime>("lastLoginTime");
                            loginData.LastLoginTime = Utils.ISODateTimeToEpocTime(lastLoginTime);
                        }
                    }
                }
                GetUserLoginDone(this, loginData);
            }
            else if (method == "hasAccessRight")
            {
                bool hasAccessRight = (bool)data["accessGranted"];
                HasAccessRightOK(this, hasAccessRight);
            }
            else if (method == "requestAccess")
            {
                bool hasAccessRight = (bool)data["accessGranted"];
                ORequestAccessDone(this, hasAccessRight);
            }
            else if (method == "generateNewToken")
            {
                string cortexToken = data["cortexToken"].ToString();
                RefreshTokenOK(this, cortexToken);
            }
            else if (method == "getLicenseInfo")
            {
                License lic = new License(data["license"]);
                GetLicenseInfoDone(this, lic);
            }
            else if (method == "getUserInformation")
            {
                //TODO
            }
            else if (method == "authorize")
            {
                string token = (string)data["cortexToken"];
                if (data["warning"] != null)
                {
                    JObject warning         = (JObject)data["warning"];
                    string warningMessage   = warning["message"].ToString();
                    UnityEngine.Debug.Log("User has not accepted eula. Please accept EULA on EMOTIV Launcher to proceed.");
                    EULANotAccepted(this, warningMessage);
                }
                AuthorizeOK(this, token);
            }
            else if (method == "createSession")
            {
                string sessionId    = (string)data["id"];
                string status       = (string)data["status"];
                string appId        = (string)data["appId"];
                JObject headset     = (JObject)data["headset"];
                string headsetId    = headset["id"].ToString();
                CreateSessionOK(this, new SessionEventArgs(sessionId, status, appId, headsetId));
            }
            else if (method == "updateSession")
            {
                string sessionId = (string)data["id"];
                string status = (string)data["status"];
                string appId = (string)data["appId"];
                JObject headset     = (JObject)data["headset"];
                string headsetId    = headset["id"].ToString();
                UpdateSessionOK(this, new SessionEventArgs(sessionId, status, appId, headsetId));
            }
            else if (method == "createRecord")
            {
                Record record = new Record((JObject)data["record"]);
                CreateRecordOK(this, record);
            }
            else if (method == "stopRecord")
            {
                Record record = new Record((JObject)data["record"]);
                StopRecordOK(this, record);
            }
            else if (method == "updateRecord")
            {
                Record record = new Record((JObject)data);
                UpdateRecordOK(this, record);
            }
            else if (method == "queryRecords")
            {
                int count = (int)data["count"];
                JArray records = (JArray)data["records"];
                List<Record> recordLists = new List<Record>();
                foreach(JObject ele in records)
                {
                    recordLists.Add(new Record(ele));
                }
                QueryRecordsDone(this, recordLists);
            }
            else if (method == "deleteRecord")
            {
                JArray successList = (JArray)data["success"];
                JArray failList = (JArray)data["failure"];
                DeleteRecordsDone(this, new MultipleResultEventArgs(successList, failList));
            }
            else if (method == "unsubscribe")
            {
                JArray successList = (JArray)data["success"];
                JArray failList = (JArray)data["failure"];
                UnSubscribeDataDone(this, new MultipleResultEventArgs(successList, failList));
            }
            else if (method == "subscribe")
            {
                // UnityEngine.Debug.Log("################subscribe: ");
                JArray successList = (JArray)data["success"];
                JArray failList = (JArray)data["failure"];
                SubscribeDataDone(this, new MultipleResultEventArgs(successList, failList));

            }
            else if (method == "injectMarker")
            {
                JObject marker = (JObject)data["marker"];
                InjectMarkerOK(this, marker);
            }
            else if (method == "updateMarker")
            {
                JObject marker = (JObject)data["marker"];
                UpdateMarkerOK(this, marker);
            }
            else if (method == "getDetectionInfo")
            {
                GetDetectionInfoDone(this, (JObject)data);
            }
            else if (method == "getCurrentProfile")
            {
                GetCurrentProfileDone(this, (JObject)data);
            }
            else if (method == "setupProfile")
            {
                string action = (string)data["action"];
                string profileName = (string)data["name"];
                if (action == "create")
                {
                    CreateProfileOK(this, profileName);
                }
                else if (action == "load")
                {
                    LoadProfileOK(this, profileName);
                }
                else if (action == "save")
                {
                    SaveProfileOK(this, profileName);
                }
                else if (action == "unload")
                {
                    UnloadProfileDone(this, true);
                }
                else if (action == "rename")
                {
                    RenameProfileOK(this, profileName);
                }
                else if (action == "delete")
                {
                    DeleteProfileOK(this, profileName);
                }
            }
            else if (method == "queryProfile")
            {
                QueryProfileOK(this, (JArray)data);
            }
            else if (method == "training")
            {
                TrainingOK(this, (JObject)data);
            }
            else if (method == "getTrainingTime")
            {
                GetTrainingTimeDone(this, (double)data["time"]);
            }
        }

        /// <summary>
        /// Handle warning  message. 
        /// A warning message is notified from Cortex. The warning messages do not contain request Id
        /// </summary>
        private void HandleWarning(int code, JToken messageData)
        {
            UnityEngine.Debug.Log("handleWarning: " + code);
            if (code == WarningCode.StreamStop ) {
                string sessionId = messageData["sessionId"].ToString();
               StreamStopNotify(this, sessionId);
            }
            else if (code == WarningCode.SessionAutoClosed ) {
                string sessionId = messageData["sessionId"].ToString();
               SessionClosedNotify(this, sessionId);
            }
            else if (code == WarningCode.AccessRightGranted)
            {
                // granted access right
                AccessRightGrantedDone(this, true);
            }
            else if (code == WarningCode.AccessRightRejected)
            {
                AccessRightGrantedDone(this, false);
            }
            else if (code == WarningCode.UserNotAcceptLicense)
            {
                string message = messageData.ToString();
                EULANotAccepted(this, message);
            }
            else if (code == WarningCode.EULAAccepted)
            {
                string message = messageData.ToString();
                EULAAccepted(this, message);
            }
            else if (code == WarningCode.UserLogin)
            {
                string message = messageData.ToString();
                UserLoginNotify(this, message);
            }
            else if (code == WarningCode.UserLogout)
            {
                string message = messageData.ToString();
                UserLogoutNotify(this, message);
            }
            else if (code == WarningCode.HeadsetConnected) {
                string headsetId = messageData["headsetId"].ToString();
                string message = messageData["behavior"].ToString();
                UnityEngine.Debug.Log("handleWarning:" + message);
                HeadsetConnectNotify(this, new HeadsetConnectEventArgs(true, message, headsetId));
            }
            else if (code == WarningCode.HeadsetWrongInformation ||
                     code == WarningCode.HeadsetCannotConnected ||
                     code == WarningCode.HeadsetConnectingTimeout) {
                string headsetId = messageData["headsetId"].ToString();
                string message = messageData["behavior"].ToString();
                HeadsetConnectNotify(this, new HeadsetConnectEventArgs(false, message, headsetId));
            }
            else if (code == WarningCode.CortexAutoUnloadProfile)
            {
                // the current profile is unloaded automatically
                UnloadProfileDone(this, true);
            }
            else if (code == WarningCode.HeadsetScanFinished)
            {
                string message = messageData["behavior"].ToString();
                HeadsetScanFinished(this, message);
            }
        }

        /// <summary>
        /// Handle when socket close
        /// </summary>
        private void WebSocketClient_Closed(object sender, EventArgs e)
        {
            WSConnectDone(this, false);
            // start connecting cortex service again
            if (_wscTimer != null)
                _wscTimer.Start();
        }
        
        /// <summary>
        /// Handle when socket open
        /// </summary>
        private void WebSocketClient_Opened(object sender, EventArgs e)
        {
            m_OpenedEvent.Set();
            if (_wSC.State == WebSocketState.Open) {
                WSConnectDone(this, true);
                // stop timer
                _wscTimer.Stop();

            } else {
                UnityEngine.Debug.Log("Open Websocket unsuccessfully.");
            }
        }

        /// <summary>
        /// Handle error when try to open socket
        /// </summary>
        private void WebSocketClient_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            UnityEngine.Debug.Log(e.Exception.GetType() + ":" + e.Exception.Message + Environment.NewLine + e.Exception.StackTrace);

            if (e.Exception.InnerException != null) {
                UnityEngine.Debug.Log(e.Exception.InnerException.GetType());
                WSConnectDone(this, false);
                // start connecting cortex service again
                _wscTimer.Start();
            }
        }

        /// <summary>
        /// Open a websocket client.
        /// </summary>
        public void Open()
        {
            // set timer for connect cortex service
            SetWSCTimer();
            //Open websocket
            m_OpenedEvent.Reset();
            if (_wSC == null || (_wSC.State != WebSocketState.None && _wSC.State != WebSocketState.Closed))
                return;
            
            _wSC.Open();
        }

        // Has Access Right
        public void HasAccessRights()
        {
            JObject param = new JObject(
                    new JProperty("clientId", Config.AppClientId),
                    new JProperty("clientSecret", Config.AppClientSecret)
                );
            SendTextMessage(param, "hasAccessRight", true);
        }
        // Request Access
        public void RequestAccess()
        {
            JObject param = new JObject(
                    new JProperty("clientId", Config.AppClientId),
                    new JProperty("clientSecret", Config.AppClientSecret)
                );
            SendTextMessage(param, "requestAccess", true);
        }
        // Authorize
        public void Authorize(string licenseID, int debitNumber)
        {
            JObject param = new JObject();
            param.Add("clientId", Config.AppClientId);
            param.Add("clientSecret", Config.AppClientSecret);
            if (!String.IsNullOrEmpty(licenseID)) {
                param.Add("license", licenseID);
            }
            param.Add("debit", debitNumber);
            SendTextMessage(param, "authorize", true);
        }
        // get license information
        public void GetLicenseInfo(string cortexToken)
        {
            JObject param = new JObject(
                    new JProperty("cortexToken", cortexToken)
                );
            SendTextMessage(param, "getLicenseInfo", true);
        }
        public void GetUserInformation(string cortexToken)
        {
            JObject param = new JObject(
                    new JProperty("cortexToken", cortexToken)
                );
            SendTextMessage(param, "getUserInformation", true);
        }

        // GetUserLogin
        public void GetUserLogin()
        {        
            JObject param = new JObject();
            SendTextMessage(param, "getUserLogin", false);
        }
        // GenerateNewToken
        public void GenerateNewToken(string currentAccessToken)
        {
            JObject param = new JObject(
                    new JProperty("clientId", Config.AppClientId),
                    new JProperty("clientSecret", Config.AppClientSecret),
                    new JProperty("cortexToken", currentAccessToken)
                );
            SendTextMessage(param, "generateNewToken", true);
        }

        // QueryHeadset
        public void QueryHeadsets(string headsetId)
        {
            JObject param = new JObject();
            if (!String.IsNullOrEmpty(headsetId)) {
                param.Add("id", headsetId);
            }
            SendTextMessage(param, "queryHeadsets", false);
        }

        // controlDevice
        // required params: command
        // command = {"connect", "disconnect", "refresh"}
        // mappings is required if connect to epoc flex
        public void ControlDevice(string command, string headsetId, JObject mappings)
        {
            JObject param = new JObject();
            param.Add("command", command);
            if (!String.IsNullOrEmpty(headsetId)) {
                param.Add("headset", headsetId);
            }
            if (mappings != null && mappings.Count > 0) {
                param.Add("mappings", mappings);
            }
            SendTextMessage(param, "controlDevice", true);
        }

        // CreateSession
        // Required params: cortexToken, status
        public void CreateSession(string cortexToken, string headsetId, string status)
        {
            JObject param = new JObject();
            if (!String.IsNullOrEmpty(headsetId)) {
                param.Add("headset", headsetId);
            }
            param.Add("cortexToken", cortexToken);
            param.Add("status", status);
            SendTextMessage(param, "createSession", true);
        }

        // UpdateSession
        // Required params: session, status, cortexToken
        public void UpdateSession(string cortexToken, string sessionId, string status)
        {
            JObject param = new JObject();
            param.Add("session", sessionId);
            param.Add("cortexToken", cortexToken);
            param.Add("status", status);
            SendTextMessage(param, "updateSession", true);
        }

        // CreateRecord
        // Required params: session, title, cortexToken
        public void CreateRecord(string cortexToken, string sessionId, string title,
                                 string description = null, string subjectName = null, List<string> tags= null)
        {
            JObject param = new JObject();
            param.Add("session", sessionId);
            param.Add("cortexToken", cortexToken);
            param.Add("title", title);
            if (description != null) {
                param.Add("description", description);
            }
            if (subjectName != null) {
                param.Add("subjectName", subjectName);
            }
            if (tags != null) {
                param.Add("tags",JArray.FromObject(tags));
            }
            SendTextMessage(param, "createRecord", true);
        }

        // StopRecord
        // Required params: session, cortexToken
        public void StopRecord(string cortexToken, string sessionId)
        {
            JObject param = new JObject();
            param.Add("session", sessionId);
            param.Add("cortexToken", cortexToken);
            SendTextMessage(param, "stopRecord", true);
        }

        // UpdateRecord
        // Required params: session, record
        public void UpdateRecord(string cortexToken, string recordId, string title = null,
                                 string description = null, List<string> tags = null)
        {
            JObject param = new JObject();
            param.Add("record", recordId);
            param.Add("cortexToken", cortexToken);
            if (description != null) {
                param.Add("description", description);
            }
            if (tags != null) {
                param.Add("tags", JArray.FromObject(tags));
            }
            SendTextMessage(param, "updateRecord", true);
        }

        // QueryRecord
        // Required params: cortexToken, query
        public void QueryRecord(string cortexToken, JObject query, JArray orderBy = null, JToken offset = null, JToken limit = null)
        {
            JObject param = new JObject();
            param.Add("query", query);
            param.Add("cortexToken", cortexToken);
            if (orderBy != null) {
                param.Add("orderBy", orderBy);
            }
            if (offset != null) {
                param.Add("offset", (long)offset);
            }
            if (limit != null) {
                param.Add("limit", (long)limit);
            }
            SendTextMessage(param, "queryRecords", true);
        }

        // DeleteRecord
        // Required params: session, records
        public void DeleteRecord(string cortexToken, List<string> records)
        {
            JObject param = new JObject();
            param.Add("records", JArray.FromObject(records));
            param.Add("cortexToken", cortexToken);
            SendTextMessage(param, "deleteRecord", true);
        }

        // InjectMarker
        // Required params: session, cortexToken, label, value, time
        public void InjectMarker(string cortexToken, string sessionId, 
                                 string label, JToken value, double time,
                                 string port = null, JObject extras = null)
        {
            JObject param = new JObject();
            param.Add("session", sessionId);
            param.Add("cortexToken", cortexToken);
            param.Add("label", label);
            param.Add("time", time);
            param.Add("value", value);
            if (port != null)
                param.Add("port", port);
            if (extras != null)
                param.Add("extras", extras);
            SendTextMessage(param, "injectMarker", true);
        }

        // UpdateMarker
        // Required params: session, cortexToken, label, value, time
        public void UpdateMarker(string cortexToken, string sessionId, string markerId, 
                                 double time, JObject extras = null)
        {
            JObject param = new JObject();
            param.Add("session", sessionId);
            param.Add("cortexToken", cortexToken);
            param.Add("markerId", markerId);
            param.Add("time", time);
            if (extras != null)
                param.Add("extras", extras);
            SendTextMessage(param, "updateMarker", true);
        }

        // Subscribe Data
        // Required params: session, cortexToken, streams
        public void Subscribe(string cortexToken, string sessionId, List<string> streams)
        {
            // UnityEngine.Debug.Log("Subscribe " + streams.Count);
            JObject param = new JObject();
            param.Add("session", sessionId);
            param.Add("cortexToken", cortexToken);
            JArray streamArr = new JArray();
            foreach (var ele in streams){
                streamArr.Add(ele);
            }
            param.Add("streams", streamArr);
            SendTextMessage(param, "subscribe", true);
        }

        // UnSubscribe Data
        // Required params: session, cortexToken, streams
        public void UnSubscribe(string cortexToken, string sessionId, List<string> streams)
        {
            JObject param = new JObject();
            param.Add("session", sessionId);
            param.Add("cortexToken", cortexToken);
            JArray streamArr = new JArray();
            foreach (var ele in streams){
                streamArr.Add(ele);
            }
            param.Add("streams", streamArr);
            SendTextMessage(param, "unsubscribe", true);
        }

        // Training - Profile
        // getDetectionInfo
        // Required params: detection
        public void GetDetectionInfo(string detection)
        {
            JObject param = new JObject();
            param.Add("detection", detection);
            SendTextMessage(param, "getDetectionInfo", true);
        }
        // getCurrentProfile
        // Required params: cortexToken, headset
        public void GetCurrentProfile(string cortexToken, string headsetId)
        {
            JObject param = new JObject();
            param.Add("cortexToken", cortexToken);
            param.Add("headset", headsetId);
            SendTextMessage(param, "getCurrentProfile", true);
        }
        // setupProfile
        // Required params: cortexToken, profile, status
        public void SetupProfile(string cortexToken, string profile, string status, string headsetId = null, string newProfileName = null)
        {
            JObject param = new JObject();
            param.Add("profile", profile);
            param.Add("cortexToken", cortexToken);
            param.Add("status", status);
            if (headsetId != null) {
                param.Add("headset", headsetId);
            }
            if (newProfileName != null) {
                param.Add("newProfileName", newProfileName);
            }
            SendTextMessage(param, "setupProfile", true);
        }
        // queryProfile
        // Required params: cortexToken
        public void QueryProfile(string cortexToken)
        {
            JObject param = new JObject();
            param.Add("cortexToken", cortexToken);
            SendTextMessage(param, "queryProfile", true);
        }
        // getTrainingTime
        // Required params: cortexToken
        public void GetTrainingTime(string cortexToken, string detection, string sessionId)
        {
            JObject param = new JObject();
            param.Add("cortexToken", cortexToken);
            param.Add("detection", detection);
            param.Add("session", sessionId);
            SendTextMessage(param, "getTrainingTime", true);
        }
        // training
        // Required params: cortexToken, profile, status
        public void Training(string cortexToken, string sessionId, string status, string detection, string action)
        {
            JObject param = new JObject();
            param.Add("session", sessionId);
            param.Add("cortexToken", cortexToken);
            param.Add("status", status);
            param.Add("detection", detection);
            param.Add("action", action);

            SendTextMessage(param, "training", true);
        }
    }
}
