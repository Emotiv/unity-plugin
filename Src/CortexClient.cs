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
using Newtonsoft.Json.Linq;

using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Threading.Tasks;

namespace EmotivUnityPlugin
{
    public abstract class CortexClient
    {
        protected Dictionary<int, string> _methodForRequestId = new Dictionary<int, string>();

        static readonly object _locker = new object();

        /// <summary>
        /// Unique id for each request
        /// </summary>
        /// <remarks>The id will be reset to 0 when reach to 100</remarks>
        protected int _nextRequestId = 1;
        
        public AutoResetEvent m_MessageReceiveEvent = new AutoResetEvent(false);
        public AutoResetEvent m_OpenedEvent = new AutoResetEvent(false);

        public event EventHandler<bool>  WSConnectDone;
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
        public event EventHandler<string> EULANotAccepted; // return cortexToken if user has not accept eula to proceed next step
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
        public event EventHandler<List<int>> GetMentalCommandActionSensitivityOK;
        public event EventHandler<bool> SetMentalCommandActionSensitivityOK;
        public event EventHandler<Dictionary<string, int>> InformTrainedSignatureActions;

        public event EventHandler<bool> BTLEPermissionGrantedNotify; // notify btle permision grant status
        
        //list of having consumer data
        public event EventHandler<List<DateTime>> QueryDatesHavingConsumerDataDone;
        public event EventHandler<List<MentalStateModel>> QueryDayDetailOfConsumerDataDone;
        public event EventHandler<MultipleResultEventArgs> ExportRecordsFinished;
        public event EventHandler<string> DataPostProcessingFinished;

        public virtual void Init(object context = null) {}

        public virtual void Open() {}
        
        public virtual void Close() {}

        public virtual void SendTextMessage(JObject param, string method, bool hasParam = true) {}

        protected static CortexClient instance;
    
        public static CortexClient Instance
        {
            get
            {
                if (instance == null)
                {
                    #if UNITY_ANDROID || UNITY_IOS || USE_EMBEDDED_LIB
                        instance = new EmbeddedCortexClient();
                    #else
                        instance = new WebsocketCortexClient();
                    #endif
                }
                return instance;
            }
        }

        // prepare json rpc request
        protected string PrepareRequest(string method, JObject param, bool hasParam = true) {
            JObject request = new JObject(
            new JProperty("jsonrpc", "2.0"),
            new JProperty("id", _nextRequestId),
            new JProperty("method", method));

            if (hasParam) {
                request.Add("params", param);
            }
            // add to dictionary, replace if a key is existed
            _methodForRequestId[_nextRequestId] = method;

            _nextRequestId++;

            return request.ToString();
        }

        public void OnWSConnected(bool isConnected)
        {
            WSConnectDone(this, isConnected);
        }

        /// <summary>
        /// Handle message received return from Emotiv Cortex
        /// </summary> 
        public void OnMessageReceived(string receievedMsg)
        {
            // UnityEngine.Debug.Log("OnMessageReceived " + receievedMsg);

            JObject response = JObject.Parse(receievedMsg);

            if (response["id"] != null)
            {
                string method = ""; // method name
                lock (_locker)
                {
                    int id = (int)response["id"];
                    method = _methodForRequestId[id];
                    bool remove_res = _methodForRequestId.Remove(id);
                    if (!remove_res)
                    {
                        UnityEngine.Debug.Log("qCannot remove key " + id + " method:" + method);
                    }
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
                        UnityEngine.Debug.Log("BTLEPermissionGrantedNotify: " + btlePermisionGranted);
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
            else if (method == "login" || method == "loginWithAuthenticationCode")
            {
                UserDataInfo loginData = new UserDataInfo();
                loginData.EmotivId = data["username"].ToString();
                String message = data["message"].ToString();
                UnityEngine.Debug.Log("login message: " + message);
                GetUserLoginDone(this, loginData);
            }
            else if (method == "logout")
            {
                String message = data["message"].ToString();
                UserLogoutNotify(this, message);
                // get user login info
                GetUserLogin();
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
                    int code                = (int)warning["code"];
                    if (code == WarningCode.UserNotAcceptLicense || 
                        code == WarningCode.UserNotAcceptPrivateEULA) {
                        UnityEngine.Debug.Log("User has not accepted eula. Please accept EULA on EMOTIV Launcher to proceed.");
                        EULANotAccepted(this, token);
                        return;
                    }
                }
                
                AuthorizeOK(this, token);
            }
            else if (method == "acceptLicense")
            {
                string message = data["message"].ToString();
                EULAAccepted(this, message);
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
            else if (method == "getTrainedSignatureActions")
            {
                JArray trainedActions = (JArray)data["trainedActions"];
                Dictionary<string, int> trainedActionsDict = new Dictionary<string, int>();
                foreach (JObject actionObj in trainedActions)
                {
                    string action = actionObj["action"].ToString();
                    int times = (int)actionObj["times"];
                    trainedActionsDict[action] = times;
                }
                InformTrainedSignatureActions(this, trainedActionsDict);
                
            }
            else if (method == "getTrainingTime")
            {
                GetTrainingTimeDone(this, (double)data["time"]);
            }
            else if (method  == "mentalCommandActionSensitivity") {
                // check data is array or string
                if (data.Type == JTokenType.Array) {
                    JArray dataList = (JArray)data;
                    List<int> sensitivityList = new List<int>();
                    foreach (int val in dataList) {
                        sensitivityList.Add(val);
                    }
                    GetMentalCommandActionSensitivityOK(this, sensitivityList);
                }
                else {
                    SetMentalCommandActionSensitivityOK(this, true);
                }
            }
            else if (method == "queryDatesHavingConsumerData")
            {
                // array of date string
                JArray dateList = (JArray)data;
                List<DateTime> dateListConverted = new List<DateTime>();
                foreach (string dateStr in dateList)
                {
                    dateListConverted.Add(DateTime.ParseExact(dateStr, "yyyy-MM-dd", 
                                          System.Globalization.CultureInfo.InvariantCulture));
                }
                QueryDatesHavingConsumerDataDone(this, dateListConverted);
            }
            else if (method == "queryDayDetailOfConsumerData")
            {
                List<MentalStateModel> mentalStateList = new List<MentalStateModel>();
                JArray dataList = (JArray)data;
                foreach (JToken item in dataList)
                {
                    mentalStateList.Add(new MentalStateModel(item.ToObject<JObject>()));
                }
                QueryDayDetailOfConsumerDataDone(this, mentalStateList);
            }
            else if (method == "exportRecord")
            {
                JArray successList = (JArray)data["success"];
                JArray failList = (JArray)data["failure"];
                ExportRecordsFinished(this, new MultipleResultEventArgs(successList, failList));
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
            else if (code == WarningCode.CortexIsReady ) {
                // get user login info
                GetUserLogin();
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
                UnityEngine.Debug.Log(message);
                EULANotAccepted(this, "");
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
            else if (code == WarningCode.DataPostProcessingFinished)
            {
                DataPostProcessingFinished(this, messageData["recordId"].ToString());
            }
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
            if (debitNumber > 0)
                param.Add("debit", debitNumber);
            SendTextMessage(param, "authorize", true);
        }

        // authorize with authorization code
        public void LoginWithAuthenticationCode(string code)
        {
            JObject param = new JObject();
            param.Add("clientId", Config.AppClientId);
            param.Add("clientSecret", Config.AppClientSecret);
            param.Add("code", code);
            SendTextMessage(param, "loginWithAuthenticationCode", true);
        }

        // accept eula
        public void AcceptEulaAndPrivacyPolicy(string cortexToken)
        {
            JObject param = new JObject(
                    new JProperty("cortexToken", cortexToken)
                );
            SendTextMessage(param, "acceptLicense", true);
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

        // Login
        public void Login (string username, string password)
        {
            JObject param = new JObject(
                    new JProperty("clientId", Config.AppClientId),
                    new JProperty("clientSecret", Config.AppClientSecret),
                    new JProperty("username", username),
                    new JProperty("password", password)
                );
            SendTextMessage(param, "login", true);
        }

        // Logout
        public void Logout(string username)
        {
            JObject param = new JObject(
                    new JProperty("username", username)
                );
            SendTextMessage(param, "logout", true);
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
            SendTextMessage(param, "queryHeadsets", true);
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
        
        // Export Records
        // Required params: cortexToken, records, folderPath, streamTypes, format
        public void ExportRecord(string cortexToken, List<string> records, string folderPath,
                                 List<string> streamTypes, string format, string version = null,
                                 List<string> licenseIds = null, bool includeDemographics = false,
                                 bool includeMarkerExtraInfos = false, bool includeSurvey = false,
                                 bool includeDeprecatedPM = false)
        {
            JObject param = new JObject();
            param.Add("recordIds", JArray.FromObject(records));
            param.Add("cortexToken", cortexToken);

#if !UNITY_IOS
            // On iOS, the parameter folder doesn't exist. 
            // Cortex exports the data to the "Documents" folder of the current application.
            param.Add("folder", folderPath);
#endif
            param.Add("streamTypes", JArray.FromObject(streamTypes));
            param.Add("format", format); // EDF, CSV, EDFPLUS, BDFPLUS

            // If the format is "EDF", then you must omit version parameter. 
            // If the format is "CSV", then version parameter must be "V1" or "V2".
            if (version != null)
            {
                param.Add("version", version);
            }
            if (licenseIds != null)
            {
                param.Add("licenseIds", JArray.FromObject(licenseIds));
            }

            if (includeDemographics)
            {
                param.Add("includeDemographics", includeDemographics);
            }
            if (includeMarkerExtraInfos)
            {
                param.Add("includeMarkerExtraInfos", includeMarkerExtraInfos);
            }
            if (includeSurvey)
            {
                param.Add("includeSurvey", includeSurvey);
            }
            if (includeDeprecatedPM)
            {
                param.Add("includeDeprecatedPM", includeDeprecatedPM);
            }

            SendTextMessage(param, "exportRecord", true);
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

        // getTrainedSignatureActions
        // Required params: cortexToken, detection, sessionId or profileName
        public void GetTrainedSignatureActions(string cortexToken, string detection, string sessionId, string profileName)
        {
            JObject param = new JObject();
            param.Add("cortexToken", cortexToken);
            param.Add("detection", detection);
            // if (sessionId != "")
            //     param.Add("session", sessionId);

            if (profileName != "")
                param.Add("profile", profileName);
            SendTextMessage(param, "getTrainedSignatureActions", true);
        }

        public void MentalCommandActionSensitivity (string cortexToken, string status, string sessionId, string profileName, List<int> values = null)
        {
            JObject param = new JObject();
            param.Add("cortexToken", cortexToken);
            param.Add("status", status);

            // check session id is empty
            if (sessionId != "")
                param.Add("session", sessionId);

            // check profile name is empty
            if (profileName != "")
                param.Add("profile", profileName);

            if (values != null) {
                JArray valuesArr = new JArray();
                // parse values to array and add to param
                foreach (int ele in values){
                    valuesArr.Add(ele);
                }
                param.Add("values", valuesArr);
                
            }
            SendTextMessage(param, "mentalCommandActionSensitivity", true);
        }

        public void QueryDatesHavingConsumerData(string cortexToken, DateTime start, DateTime end)
        {
            JObject param = new JObject();
            param.Add("cortexToken", cortexToken);
            param.Add("startDate", start.Date.ToString("yyyy-MM-dd"));
            param.Add("endDate", end.Date.ToString("yyyy-MM-dd"));
            SendTextMessage(param, "queryDatesHavingConsumerData", true);
        }

        public void QueryDayDetailOfConsumerData(string cortexToken, DateTime date)
        {
            JObject param = new JObject();
            param.Add("cortexToken", cortexToken);
            param.Add("date", date.Date.ToString("yyyy-MM-dd"));
            SendTextMessage(param, "queryDayDetailOfConsumerData", true);
        }
    }
}
