using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Timers;

namespace EmotivUnityPlugin
{
    /// <summary>
    /// Reponsible for authorizing process.
    /// </summary>
    public class Authorizer
    {
        static readonly object _locker = new object();
        private CortexClient _ctxClient = CortexClient.Instance;
        private static string _cortexToken = "";
        private static string _emotivId = "";
        private string _licenseID = "";
        private static int _debitNo = 5000; // default value

        /// <summary>
        /// Timer for waiting a user login
        /// </summary>
        private System.Timers.Timer _waitUserLoginTimer = null;

        /// <summary>
        /// Gets cortexToken.
        /// </summary>
        /// <value>The current current cortex token.</value>
        public string CortexToken
        {
            get {
                lock (_locker)
                {
                    return _cortexToken;
                }
            }
        }

        // Events
        public event EventHandler<string>  AuthorizedFailed;
        public event EventHandler<ConnectToCortexStates> ConnectServiceStateChanged;
        public event EventHandler<License> GetLicenseInfoDone;
        public event EventHandler<License> LicenseExpired;
        public event EventHandler<string>  UserLogoutNotify;

        public static Authorizer Instance { get; } = new Authorizer();
        public string LicenseID { get => _licenseID; set => _licenseID = value; }

        public Authorizer()
        {
            _ctxClient.WSConnectDone            += OnWSConnectDone;
            _ctxClient.GetUserLoginDone         += OnGetUserLoginDone;
            _ctxClient.UserLoginNotify          += OnUserLoginNotify;          // inform user loggin 
            _ctxClient.UserLogoutNotify         += OnUserLogoutNotify;         // inform user log out
            _ctxClient.HasAccessRightOK         += OnHasAccessRightOK;
            _ctxClient.ORequestAccessDone       += OnRequestAccessDone;
            _ctxClient.AccessRightGrantedDone   += OnAccessRightGrantedOK; // inform user have granted or rejected access right for the App
            _ctxClient.AuthorizeOK              += OnAuthorizedOK;
            _ctxClient.EULAAccepted             += OnEULAAccepted;
            _ctxClient.EULANotAccepted          += OnEULANotAccepted;
            _ctxClient.RefreshTokenOK           += OnRefreshTokenOK;
            _ctxClient.GetLicenseInfoDone       += OnGetLicenseInfoDone;
            _ctxClient.ErrorMsgReceived        += OnErrorMsgReceived;
        }

                // login with authorization code
        public void LoginWithAuthenticationCode(string code) {
            _ctxClient.LoginWithAuthenticationCode(code);
        }

        private void OnEULANotAccepted(object sender, string cortexToken)
        {
            UnityEngine.Debug.Log("OnEULANotAccepted: token " + cortexToken);
            ConnectServiceStateChanged(this, ConnectToCortexStates.EULA_Not_Accepted);
            
            if (String.IsNullOrEmpty(cortexToken))
                return;
            // save cortexToken 
            _cortexToken = cortexToken;
        }

        private void OnErrorMsgReceived(object sender, ErrorMsgEventArgs errorInfo) {
            if (errorInfo.Code == ErrorCode.AuthorizeTokenError || errorInfo.Code == ErrorCode.LoginTokenError)
            {
                UnityEngine.Debug.LogError("OnErrorMsgReceived error: " + errorInfo.MessageError  + ". Need to re-login for user " + Config.UserName + " emotivId: " + _emotivId);
                if (_emotivId == "")
                    return;
                // logout user
                _ctxClient.Logout(_emotivId);
            }
            else if (errorInfo.Code == ErrorCode.CloudTokenIsRefreshing || errorInfo.Code == ErrorCode.NotReAuthorizedError || errorInfo.Code == ErrorCode.CortexTokenCompareErrorAppInfo) {
                // load cortexToken
                UserDataInfo tokenInfo  = Authorizer.LoadToken();

                if (string.IsNullOrEmpty(tokenInfo.CortexToken)) {
                    UnityEngine.Debug.Log("OnErrorMsgReceived: No token found. Need to logout user " + _emotivId);
                    if (_emotivId == "")
                        return;
                    // logout user
                    _ctxClient.Logout(_emotivId);
                }
                else {
                    UnityEngine.Debug.Log("OnErrorMsgReceived: " + errorInfo.MessageError  +  " Re-authorize again until it is done");
                    _ctxClient.Authorize(_licenseID, _debitNo);
                } 
            }
        }

        /// <summary>
        /// Set up timer for checking has a user login
        /// </summary>
        private void SetWaitUserLoginTimer() {
            if (_waitUserLoginTimer != null)
                return;
            _waitUserLoginTimer = new System.Timers.Timer(Config.WAIT_USERLOGIN_TIME);
            // Hook up the Elapsed event for the timer.
            _waitUserLoginTimer.Elapsed      += OnTimerEvent;
            _waitUserLoginTimer.AutoReset     = false; // do not auto reset
        }

        /// <summary>
        /// Handle for _waitUserLoginTimer timer timeout
        //  Retry get user login
        /// </summary>
        private void OnTimerEvent(object sender, ElapsedEventArgs e)
        {
            // retry get user login
            _ctxClient.GetUserLogin();
        }

        // log out user
        public void Logout() {
            if (_emotivId == "")
                return;
            _ctxClient.Logout(_emotivId);
        }

        private void OnGetLicenseInfoDone(object sender, License lic)
        {
            // UnityEngine.Debug.Log(" OnGetLicenseInfoDone:  lic: " + lic.licenseId);
            if (lic.expired) {
                ConnectServiceStateChanged(this, ConnectToCortexStates.LicenseExpried);
                LicenseExpired(this, lic);
            } else {
                lock(_locker) _licenseID = lic.licenseId;
                string cortexToken = CortexToken;
                if (!String.IsNullOrEmpty(cortexToken)) {
                    ConnectServiceStateChanged(this, ConnectToCortexStates.Authorized);
                    GetLicenseInfoDone(this, lic);
                } else {
                    ConnectServiceStateChanged(this, ConnectToCortexStates.Authorize_failed);
                    AuthorizedFailed(this, "");
                }
            }
        }

        private void OnRefreshTokenOK(object sender, string cortexToken)
        {
            UnityEngine.Debug.Log("The cortex token is refreshed successfully.");
            // load cortexToken
            UserDataInfo tokenInfo  = new UserDataInfo();
            tokenInfo.CortexToken   = cortexToken;
            tokenInfo.LastLoginTime = Utils.ISODateTimeToEpocTime(DateTime.Now);
            
            lock(_locker)
            {
                tokenInfo.EmotivId      = _emotivId;
                _cortexToken            = cortexToken;
            }
            
            // save token
            SaveToken(tokenInfo);

            // get license information
            _ctxClient.GetLicenseInfo(cortexToken);
        }

        private void OnWSConnectDone(object sender, bool isConnected)
        {
            if (isConnected) {
                #if UNITY_ANDROID || UNITY_IOS
                    UnityEngine.Debug.Log("Embedded cortex lib is started.");
                #else
                    UnityEngine.Debug.Log("Websocket is opened.");
                    _ctxClient.GetUserLogin();
                #endif
                ConnectServiceStateChanged(this, ConnectToCortexStates.Login_waiting);
            } else {
                lock(_locker)
                {
                    // clear data
                    _emotivId       = "";
                    _cortexToken    = "";
                }
                ConnectServiceStateChanged(this, ConnectToCortexStates.Service_connecting);
            }
        }

        private void OnEULAAccepted(object sender, string message)
        {
            UnityEngine.Debug.Log("EULAAcceptedOK: " + message);
            _ctxClient.Authorize(_licenseID, _debitNo);
        }

        private void OnAuthorizedOK(object sender, string cortexToken)
        {
            if (!String.IsNullOrEmpty(cortexToken)) {
                UnityEngine.Debug.Log("Authorize successfully.");
                
                UserDataInfo tokenInfo  = new UserDataInfo();
                tokenInfo.CortexToken   = cortexToken;
                tokenInfo.LastLoginTime = Utils.ISODateTimeToEpocTime(DateTime.Now);
                lock (_locker)
                {
                    tokenInfo.EmotivId  = _emotivId;
                    _cortexToken        = cortexToken;
                }

                // do not save token for mobile platform
                #if !UNITY_ANDROID && !UNITY_IOS && !USE_EMBEDDED_LIB
                    UnityEngine.Debug.Log("Save token for next using.");
                    // Save App version
                    Utils.SaveAppVersion(Config.AppVersion);
                #endif
                Authorizer.SaveToken(tokenInfo);

                // get license information
                _ctxClient.GetLicenseInfo(cortexToken);
            } else {
                AuthorizedFailed(this, cortexToken);
                UnityEngine.Debug.Log("Invalid Token.");
            }
        }

        /// <summary>
        /// Load token from local app data
        /// </summary>
        private static UserDataInfo LoadToken() {
            string fileDir = Path.Combine(Utils.DataDirectory, Config.TmpDataFileName);
            if (!File.Exists(fileDir)) {
                UnityEngine.Debug.Log("LoadToken: not exists token file " + fileDir);
                return new UserDataInfo();
            }
            try
            {
                // get tokenSavedInfo from file
                Stream stream = File.Open(fileDir, FileMode.Open);
                BinaryFormatter bformater = new BinaryFormatter();
                UserDataInfo tokenSavedInfo = (UserDataInfo)bformater.Deserialize(stream);
                stream.Close();
                if (tokenSavedInfo == null) {
                    UnityEngine.Debug.Log("LoadToken: tokenSavedInfo is null");
                    return new UserDataInfo();
                }
                else {
                    return tokenSavedInfo;
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError("LoadToken: can not read user data : " + e.Message);
                return new UserDataInfo();
            }
        }

        /// <summary>
        /// Save token to local app data for next using
        /// </summary>
        private static void SaveToken(UserDataInfo tokenSavedInfo) {
            string fileDir = Path.Combine(Utils.DataDirectory, Config.TmpDataFileName);
            try
            {
                // save tokenSavedInfo to file
                Stream stream = File.Open(fileDir, FileMode.Create);
                BinaryFormatter bformater = new BinaryFormatter();
                bformater.Serialize(stream, tokenSavedInfo);
                stream.Close();
                UnityEngine.Debug.Log("Save token done.");

            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("Can not save token failed: " + e.ToString());
            }
            
        }

        /// <summary>
        /// Remove token when user logout 
        /// </summary>
        private static void RemoveToken() {
            string fileDir = Path.Combine(Utils.DataDirectory, Config.TmpDataFileName);
            if (!File.Exists(fileDir)) {
                UnityEngine.Debug.Log("RemoveCortexToken: not exists file " + fileDir);
                return;
            }
            // Remove token file
            try
            {
                File.Delete(fileDir);
            }
            catch (System.IO.IOException ioExp)
            {
                UnityEngine.Debug.Log("RemoveCortexToken: " + ioExp.Message);
            }
            UnityEngine.Debug.Log("Remove token done.");
        }

        private void OnAccessRightGrantedOK(object sender, bool isGranted)
        {
            UnityEngine.Debug.Log("AccessRightGrantedOK: " + isGranted);
            if (isGranted) {
                _ctxClient.Authorize(_licenseID, _debitNo);
            } else {
                UnityEngine.Debug.Log("The access right to the Application has been rejected");
            }
        }

        private void OnRequestAccessDone(object sender, bool hasAccessRight)
        {
            if (hasAccessRight) {
                UnityEngine.Debug.Log("The User has access right to this application.");
            } else {
                UnityEngine.Debug.Log("The User has not granted access right to this application. Please use EMOTIV Launcher to proceed.");
            }
        }

        private void OnHasAccessRightOK(object sender, bool hasAccessRight)
        {
            UnityEngine.Debug.Log("HasAccessRightOK: " + hasAccessRight);
            if (hasAccessRight) {
                // Authorize
                _ctxClient.Authorize(_licenseID, _debitNo);
            } else {
                _ctxClient.RequestAccess();
            }
        }

        private void OnUserLogoutNotify(object sender, string message)
        {
            UnityEngine.Debug.Log("UserLogoutOK :" + message);
            lock(_locker)
            {
                _cortexToken    = "";
                _emotivId       = "";
                _licenseID      = "";
            }
            
            // Remove token
            RemoveToken();
            // stop wait user login
            if (_waitUserLoginTimer!= null && _waitUserLoginTimer.Enabled)
                _waitUserLoginTimer.Stop();
            // notify to data stream process
            UserLogoutNotify(this, message);
            // retry get user login
            _ctxClient.GetUserLogin();
            
        }

        private void OnUserLoginNotify(object sender, string message)
        {
            // stop wait user login
            if (_waitUserLoginTimer!= null &&  _waitUserLoginTimer.Enabled) {
                UnityEngine.Debug.Log("User has logged in. Stop waiting user login.");
                _waitUserLoginTimer.Stop();
                _ctxClient.GetUserLogin();
            }
        }

        private void OnGetUserLoginDone(object sender, UserDataInfo loginData)
        {
            UnityEngine.Debug.Log("OnGetUserLoginDone.");
            // if emotivId is not empty -> has login user
            if (!String.IsNullOrEmpty(loginData.EmotivId)) {
                // stop timer
                if (_waitUserLoginTimer != null &&  _waitUserLoginTimer.Enabled)
                    _waitUserLoginTimer.Stop();

                // save emotivId
                lock (_locker) _emotivId   = loginData.EmotivId;

                // notify change sate
                ConnectServiceStateChanged(this, ConnectToCortexStates.Authorizing);
                // load cortexToken
                UserDataInfo tokenInfo  = Authorizer.LoadToken();
                string savedEmotivId    = tokenInfo.EmotivId;

                // print saved EmotivId and saved time and token
                UnityEngine.Debug.Log("Saved EmotivId: " + savedEmotivId + " current logged in emotivId " + loginData.EmotivId +
                  " saved token: " + tokenInfo.CortexToken);

                // check cortex token
                if (!string.IsNullOrEmpty(savedEmotivId) && !string.IsNullOrEmpty(tokenInfo.CortexToken) && savedEmotivId == loginData.EmotivId) {
                    // generate new token for next using
                    UnityEngine.Debug.Log("Refresh token for next using.");
                    _ctxClient.GenerateNewToken(tokenInfo.CortexToken);
                }
                else {
                    // need to re-authorize again
                    #if UNITY_ANDROID || UNITY_IOS || USE_EMBEDDED_LIB
                        // for embedded cortex lib need to athorize again
                        _ctxClient.Authorize(_licenseID, _debitNo);
                    #else
                        // check access right to re authorize again
                        _ctxClient.HasAccessRights();
                    #endif
                }  
            } 
            else {

                // for embedded cortex lib need to call login  windows and androids
                #if UNITY_ANDROID || UNITY_IOS || USE_EMBEDDED_LIB
                    UnityEngine.Debug.Log("No emotiv user login. Need to call login for username " + Config.UserName);
                    ConnectServiceStateChanged(this, ConnectToCortexStates.Login_notYet);
                    if (Config.UserName == "")
                        return;
                    
                    // _ctxClient.Login(Config.UserName, Config.Password);

                #else
                    bool checkEmotivAppRequire = true; // require to check emotiv apps installed or not
                    #if UNITY_EDITOR
                        checkEmotivAppRequire = false;
                    #endif
                    // check EmotivApp has installed
                    if (Utils.CheckEmotivAppInstalled(Config.EmotivAppsPath, checkEmotivAppRequire)) {
                        ConnectServiceStateChanged(this, ConnectToCortexStates.Login_notYet);
                    }
                    else {
                        // EMOTIVApp not found
                        ConnectServiceStateChanged(this, ConnectToCortexStates.EmotivApp_NotFound);
                    }
                    // start waiting user login
                    SetWaitUserLoginTimer();
                    _waitUserLoginTimer.Start();
                    
                    UnityEngine.Debug.Log("You must login via EMOTIV Launcher before working with Cortex");
                #endif
            }           
        }
    }
}
