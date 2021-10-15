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
        private static string _licenseID = "";
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
        }

        private void OnEULANotAccepted(object sender, string message)
        {
            UnityEngine.Debug.Log("OnEULANotAccepted: " + message);
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
                UnityEngine.Debug.Log("Websocket is opened.");
                // get user login
                _ctxClient.GetUserLogin();
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
                // save token
                SaveToken(tokenInfo);

                // Save App version
                Utils.SaveAppVersion(Config.AppVersion);
                
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
        private UserDataInfo LoadToken() {
            string rootPath     = Utils.GetAppTmpPath();
            string targetDir    = Path.Combine(rootPath, Config.ProfilesDir);

            if (!Directory.Exists(targetDir)){
                UnityEngine.Debug.Log("LoadToken: not exists directory " + targetDir);
                return new UserDataInfo();
            }
            string fileDir = Path.Combine(targetDir, Config.TmpDataFileName);
            if (!File.Exists(fileDir)) {
                UnityEngine.Debug.Log("LoadToken: not exists file " + fileDir);
                return new UserDataInfo();
            }
            try
            {
                Stream stream = File.Open(fileDir, FileMode.Open);
                BinaryFormatter bformater = new BinaryFormatter();
                UserDataInfo tokenInfo   = (UserDataInfo)bformater.Deserialize(stream);
                stream.Close();
                return tokenInfo;
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
        private void SaveToken(UserDataInfo tokenSavedInfo) {
            string rootPath = Utils.GetAppTmpPath();
            string targetDir = Path.Combine(rootPath, Config.ProfilesDir);
            if (!Directory.Exists(targetDir)) {
                try
                {
                    // create directory
                    Directory.CreateDirectory(targetDir);
                    UnityEngine.Debug.Log("SaveCortexToken: create directory " + targetDir);
                }
                catch (Exception e)
                {      
                    UnityEngine.Debug.Log("Can not create directory: " + targetDir + " : failed: " + e.ToString());
                    return;
                }
                finally {}
            }
            string fileDir = Path.Combine(targetDir, Config.TmpDataFileName);

            try
            {
                using(var fileStream = new FileStream(fileDir, FileMode.Create)) {
                BinaryFormatter bformatter = new BinaryFormatter();
                bformatter.Serialize(fileStream, tokenSavedInfo);
                // var data = JsonConvert.SerializeObject(tokenSavedInfo);
                // byte[] dataByte = new UTF8Encoding(true).GetBytes(data);
                // fileStream.Write(dataByte, 0, dataByte.Length);
                }
                UnityEngine.Debug.Log("Save token successfully.");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log("Can not save token failed: " + e.ToString());
                return;
            }
            
        }

        /// <summary>
        /// Remove token when user logout 
        /// </summary>
        private void RemoveToken(string path = "") {
            string rootPath = "";
            if (string.IsNullOrEmpty(path)){
                //get tmp Path of App 
                rootPath = Utils.GetAppTmpPath();
            }
            else {
                rootPath = path;
            }
            string targetDir = Path.Combine(rootPath, Config.ProfilesDir);
            if (!Directory.Exists(targetDir)){
                UnityEngine.Debug.Log("RemoveCortexToken: not exists directory " + targetDir);
                return;
            }
            string fileDir = Path.Combine(targetDir, Config.TmpDataFileName);
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
            if (_waitUserLoginTimer!= null &&  _waitUserLoginTimer.Enabled)
                _waitUserLoginTimer.Stop();
            // retry get user login
            _ctxClient.GetUserLogin();
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
               
                
                double lastLoginTime    = loginData.LastLoginTime;
                // notify change sate
                ConnectServiceStateChanged(this, ConnectToCortexStates.Authorizing);

                // If app version different saved app version
                if (!Utils.IsSameAppVersion(Config.AppVersion)) {
                    // re authorize again
                    UnityEngine.Debug.Log("There are new version of App. Need to re-authorize.");
                    _ctxClient.HasAccessRights();
                    return;
                }
                // load cortexToken
                UserDataInfo tokenInfo  = LoadToken();
                string savedEmotivId    = tokenInfo.EmotivId;
                double savedTime        = tokenInfo.LastLoginTime;

                // Re-Authorize if saved emotivId different logged in emotivId
                if (string.IsNullOrEmpty(savedEmotivId) ||
                    savedEmotivId != loginData.EmotivId) {
                    // re authorize again
                    UnityEngine.Debug.Log("There are new logging user. Need to re-authorize.");
                    _ctxClient.HasAccessRights();
                    return;
                }
                if (lastLoginTime >= savedTime) {
                    UnityEngine.Debug.Log("User has just re-logined. Need to re-authorize.");
                    _ctxClient.HasAccessRights();
                    return;
                }

                UnityEngine.Debug.Log("Refresh token for next using.");
                // genereate new token
                _ctxClient.GenerateNewToken(tokenInfo.CortexToken);
            } else {

                
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
            }            
        }

        /// <summary>
        /// Start opening a websocket client to work with Emotiv cortex service. 
        /// </summary>
        public void StartAction(string licenseID ="")
        {
            if (!string.IsNullOrEmpty(licenseID))
                _licenseID = licenseID;
            _ctxClient.Open();
        }
    }
}
