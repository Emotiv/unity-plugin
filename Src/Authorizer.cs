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
        private static double _currentLoginTime = 0; // store current login time

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
        public event EventHandler<ErrorMsgEventArgs> ErrorMsgReceived;
        public event EventHandler<string>  NoAccessRightNotify;

        public static Authorizer Instance { get; } = new Authorizer();
        public string LicenseID { get => _licenseID; set => _licenseID = value; }
        
        /// <summary>
        /// Gets the current Emotiv ID of the logged-in user.
        /// </summary>
        /// <value>The current Emotiv ID.</value>
        public string CurrentEmotivId
        {
            get {
                lock (_locker)
                {
                    return _emotivId;
                }
            }
        }

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

        private void OnErrorMsgReceived(object sender, ErrorMsgEventArgs errorInfo)
        {

            UnityEngine.Debug.Log($"OnErrorMsgReceived: Code={errorInfo.Code}, Message={errorInfo.MessageError}, Method={errorInfo.MethodName}");

#if UNITY_ANDROID || UNITY_IOS || USE_EMBEDDED_LIB
            // For mobile and embedded lib platforms
            bool shouldLogout = false;
            bool shouldAuthorize = false;
            switch (errorInfo.Code)
            {
                case ErrorCode.LoginTokenError:
                case ErrorCode.NoAppInfoOrAccessRightError:
                case ErrorCode.AuthorizeTokenError:
                    shouldLogout = !string.IsNullOrEmpty(_emotivId);
                    break;
                case ErrorCode.NotReAuthorizedError:
                case ErrorCode.CortexTokenCompareErrorAppInfo:
                case ErrorCode.CortexTokenNotFit:
                case ErrorCode.CloudTokenIsRefreshing:
                    shouldAuthorize = true;
                    break;
            }
            if (shouldLogout)
            {
                _ctxClient.Logout(_emotivId);
                return;
            }

            if (shouldAuthorize)
            {
                _ctxClient.Authorize(_licenseID, _debitNo);
                return;
            }
#else
            // For desktop without embedded lib
            switch (errorInfo.Code)
            {
                case ErrorCode.NoAppInfoOrAccessRightError:
                case ErrorCode.AuthorizeTokenError:
                case ErrorCode.NotReAuthorizedError:
                case ErrorCode.CortexTokenCompareErrorAppInfo:
                case ErrorCode.CortexTokenNotFit:
                    ConnectServiceStateChanged?.Invoke(this, ConnectToCortexStates.Authorize_failed);
                    return;
            }
#endif
            // send other error messages to EmotivUnityItf
            ErrorMsgReceived?.Invoke(this, errorInfo);
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

        /// <summary>
        /// Retry authorization process
        /// </summary>
        public void RetryAuthorize() {
            // back to authorizing state
            ConnectServiceStateChanged(this, ConnectToCortexStates.Authorizing);
            _ctxClient.Authorize(_licenseID, _debitNo);
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
            tokenInfo.LastLoginTime = _currentLoginTime;
            
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
                    _emotivId           = "";
                    _cortexToken        = "";
                    _currentLoginTime   = 0;
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
                tokenInfo.LastLoginTime = _currentLoginTime;
                lock (_locker)
                {
                    tokenInfo.EmotivId  = _emotivId;
                    _cortexToken        = cortexToken;
                }

                // save token for next using
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
            string fileDir = GetSavedTokenFilePath();
            if (String.IsNullOrEmpty(fileDir)) {
                UnityEngine.Debug.Log("LoadToken: no saved token file found.");
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
            if (String.IsNullOrEmpty(Config.DataDirectory)) {
                UnityEngine.Debug.Log("SaveToken: no data directory found.");
                return;
            }

            string fileDir = Path.Combine(Config.DataDirectory, Config.TmpDataFileName);
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
            string fileDir = GetSavedTokenFilePath();
            if (String.IsNullOrEmpty(fileDir)) {
                UnityEngine.Debug.Log("RemoveCortexToken: no saved token file found.");
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

        private static string GetSavedTokenFilePath()
        {
            if (String.IsNullOrEmpty(Config.DataDirectory)) {
                return "";
            }
            string fileDir = Path.Combine(Config.DataDirectory, Config.TmpDataFileName);
            if (!File.Exists(fileDir)) {
                UnityEngine.Debug.Log("GetSavedTokenFilePath: not exists file " + fileDir);
                return "";
            }
            return fileDir;
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
            if (hasAccessRight)
            {
                UnityEngine.Debug.Log("The User has access right to this application.");
            }
            else
            {
                NoAccessRightNotify?.Invoke(this, "The Application has not granted access right. Please use EMOTIV Launcher to proceed.");
            }
        }

        private void OnHasAccessRightOK(object sender, bool hasAccessRight)
        {
            UnityEngine.Debug.Log("HasAccessRightOK: " + hasAccessRight);
            if (hasAccessRight) {
                CheckTokenAndAuthorize(_emotivId, _currentLoginTime);
            } else {
                // clear token and remove token file
                _cortexToken    = "";
                RemoveToken();
                // inform user to login
                _ctxClient.RequestAccess();
            }
        }

        private void OnUserLogoutNotify(object sender, string message)
        {
            UnityEngine.Debug.Log("UserLogoutOK :" + message);
            lock(_locker)
            {
                _cortexToken        = "";
                _emotivId           = "";
                _licenseID          = "";
                _currentLoginTime   = 0;
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
            // if emotivId is not empty -> has login user
            if (!String.IsNullOrEmpty(loginData.EmotivId))
            {
                // stop timer if it is running
                if (_waitUserLoginTimer != null && _waitUserLoginTimer.Enabled)
                    _waitUserLoginTimer.Stop();

                // save emotivId and lastLoginTime
                lock (_locker) 
                {
                    _emotivId = loginData.EmotivId;
                    _currentLoginTime = loginData.LastLoginTime;
                }

                // notify change sate
                ConnectServiceStateChanged(this, ConnectToCortexStates.Authorizing);
                // if embedded cortex lib or mobile platform, do not check access right
#if UNITY_ANDROID || UNITY_IOS || USE_EMBEDDED_LIB
                CheckTokenAndAuthorize(loginData.EmotivId, loginData.LastLoginTime);
#else
                // check access right to the application working with Emotiv Cortex Service
                _ctxClient.HasAccessRights();
#endif
            }
            else
            {
#if UNITY_ANDROID || UNITY_IOS || USE_EMBEDDED_LIB
                // for mobile platform or embedded cortex lib
                // check if user has logged in or not. return state Login_notYet 
                ConnectServiceStateChanged(this, ConnectToCortexStates.Login_notYet);
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

        private void CheckTokenAndAuthorize(string emotivId, double currentLoginTime)
        {
            UserDataInfo tokenInfo = Authorizer.LoadToken();
            string cortexToken = tokenInfo.CortexToken;
            string savedEmotivId = tokenInfo.EmotivId;
            double savedLoginTime = tokenInfo.LastLoginTime;
            
            if (!string.IsNullOrEmpty(savedEmotivId) &&
                !string.IsNullOrEmpty(cortexToken) &&
                savedEmotivId == emotivId &&
                currentLoginTime <= savedLoginTime)
            {
                UnityEngine.Debug.Log("CheckTokenAndAuthorize: has cortex token, generate new token");
                _ctxClient.GenerateNewToken(cortexToken);
            }
            else
            {
                if (currentLoginTime > savedLoginTime)
                {
                    UnityEngine.Debug.Log("CheckTokenAndAuthorize: current login time is newer than saved token, authorize again");
                }
                else
                {
                    UnityEngine.Debug.Log("CheckTokenAndAuthorize: no cortex token or invalid conditions, authorize again");
                }
                _ctxClient.Authorize(_licenseID, _debitNo);
            }
        }
    }
}
