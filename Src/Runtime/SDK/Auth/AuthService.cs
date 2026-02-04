using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Emotiv.Cortex.Models;
using EmotivUnityPlugin;
using UnityEngine;
#if USE_EMBEDDED_LIB || UNITY_ANDROID || UNITY_IOS
using Cdm.Authentication.Browser;
using Cdm.Authentication.OAuth2;
using System.Threading;
using System.Security.Cryptography;
using System.Text;
using Cdm.Authentication.Clients;
#endif
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace Emotiv.Cortex.Service
{
    public class AuthService : IAuthService
    {
        public static AuthService Instance { get; } = new AuthService();

        private readonly CortexClient _client;
        private UserDataInfo _loggedInUser = new UserDataInfo();
        private bool _isInitialized;

#if USE_EMBEDDED_LIB || UNITY_ANDROID || UNITY_IOS
        private CrossPlatformBrowser _crossPlatformBrowser;
        private AuthenticationSession _authenticationSession;
        private CancellationTokenSource _cancellationTokenSource;
        private static readonly char[] HEX_ARRAY = "0123456789abcdef".ToCharArray();
#endif

        public AuthService()
        {
            _client = CortexClient.Instance;
        }

        internal AuthService(CortexClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<(CortexErrorCode Code, UserDataInfo User)> InitAndAuthorizeAsync()
        {
            if (!await EnsureAndroidPermissionsAsync())
            {
                return (CortexErrorCode.PermissionsDenied, new UserDataInfo());
            }

            InitConfigFromAppConfig();

#if UNITY_ANDROID || UNITY_IOS || USE_EMBEDDED_LIB
            InitAuthentication(AppConfig.ClientId, AppConfig.ClientSecret);
#endif

            object context = GetAndroidContext();
            _client.Init(context);
            _client.Open();

            bool isConnected = await WaitForCortexConnectionStaredAsync();
            if (!isConnected)
            {
                return (CortexErrorCode.CortexConnectionError, new UserDataInfo());
            }
            _isInitialized = true;
            UnityEngine.Debug.Log("AuthService: InitAndAuthorizeAsync(): WS connected.");

            UserDataInfo loginData = await WaitForGetUserLoginAsync();
            return await CompleteAuthorizationAsync(loginData);
        }

        public async Task<(CortexErrorCode Code, UserDataInfo User)> LoginAndAuthorizeAsync()
        {
            UnityEngine.Debug.Log("AuthService: LoginAndAuthorizeAsync(): Start login flow");
#if UNITY_ANDROID || UNITY_IOS
            var tcs = new TaskCompletionSource<(CortexErrorCode Code, UserDataInfo User)>();
            UniWebViewManager.Instance.StartAuthorization(
                onSuccess: async (authCode) => {
                    Debug.Log("UniWebView Authorization succeeded! Starting login with auth code");
                    var result = await LoginWithAuthenticationCodeAsync(authCode);
                    tcs.TrySetResult(result);
                },
                onError: (errorCode, errorMessage) => {
                    Debug.LogError($"Authorization failed! Error {errorCode}: {errorMessage}");
                    tcs.TrySetResult((CortexErrorCode.AuthorizationFailed, new UserDataInfo()));
                }
            );
            return await tcs.Task;
#elif USE_EMBEDDED_LIB
            if (_authenticationSession != null)
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = new CancellationTokenSource();
                try
                {
                    var accessTokenResponse =
                        await _authenticationSession.AuthenticateAsync(_cancellationTokenSource.Token);

                    return await LoginWithAuthenticationCodeAsync(accessTokenResponse.accessToken);
                }
                catch (AuthorizationCodeRequestException ex)
                {
                    Debug.LogError($"{nameof(AuthorizationCodeRequestException)} " +
                                $"error: {ex.error.code}, description: {ex.error.description}, uri: {ex.error.uri}");
                }
                catch (AccessTokenRequestException ex)
                {
                    Debug.LogError($"{nameof(AccessTokenRequestException)} " +
                                $"error: {ex.error.code}, description: {ex.error.description}, uri: {ex.error.uri}");
                }
                catch (Exception ex)
                {
                    Debug.LogError("Exception " + ex.Message);
                }
            }
            return (CortexErrorCode.UnknownError, new UserDataInfo());
#else
            await Task.Yield();
            return (CortexErrorCode.UnknownError, new UserDataInfo());
#endif
        }

        public void Logout()
        {
            if (string.IsNullOrEmpty(_loggedInUser.EmotivId))
            {
                Debug.LogWarning("Logout requested but no user is logged in.");
                return;
            }
            _client.Logout(_loggedInUser.EmotivId);
            _loggedInUser = new UserDataInfo();
        }

        private async Task<(CortexErrorCode Code, UserDataInfo User)> LoginWithAuthenticationCodeAsync(string code)
        {
            UnityEngine.Debug.Log("AuthService: LoginWithAuthenticationCodeAsync(): code: " + code);
            UserDataInfo loginData = await WaitForLoginAsync(code);
            return await CompleteAuthorizationAsync(loginData);
        }

        private async Task<(CortexErrorCode Code, UserDataInfo User)> CompleteAuthorizationAsync(UserDataInfo loginData)
        {
            if (string.IsNullOrEmpty(loginData.EmotivId))
            {
                return (CortexErrorCode.OK, loginData);
            }

            _loggedInUser = loginData;

            UnityEngine.Debug.Log("AuthService: CompleteAuthorizationAsync(): User logged in: " + loginData.EmotivId);

#if UNITY_ANDROID || UNITY_IOS || USE_EMBEDDED_LIB
            bool hasAccessRight = true;
#else
            bool hasAccessRight = await CheckAccessRightsAsync();
            if (!hasAccessRight)
            {
                _client.RequestAccess();
                return (CortexErrorCode.NoEULAAccepted, loginData);
            }
#endif
            UnityEngine.Debug.Log("AuthService: CompleteAuthorizationAsync(): Access rights granted.");
            var authorizeResult = await AuthorizeAsync();
            if (!authorizeResult.Success)
            {
                return (CortexErrorCode.AuthorizationFailed, loginData);
            }

            UnityEngine.Debug.Log("AuthService: CompleteAuthorizationAsync(): Authorized.");

            License license = await GetLicenseInfoAsync();
            UnityEngine.Debug.Log("AuthService: CompleteAuthorizationAsync(): License info retrieved." +
                (license != null ? $" expired: {license.expired}" : " license is null"));
            if (license == null || license.expired)
            {
                return (CortexErrorCode.LicenseError, loginData);
            }

            var resultUser = new UserDataInfo(loginData.LastLoginTime, authorizeResult.CortexToken, loginData.EmotivId);
            _loggedInUser = resultUser;
            return (CortexErrorCode.OK, resultUser);
        }

        private void InitConfigFromAppConfig()
        {
            string appUrl = "wss://localhost:6868";
#if !USE_EMBEDDED_LIB && !UNITY_ANDROID && !UNITY_IOS
            if (!string.IsNullOrEmpty(AppConfig.AppUrl))
            {
                appUrl = AppConfig.AppUrl;
            }
#endif

            Config.Init(
                AppConfig.ClientId,
                AppConfig.ClientSecret,
                AppConfig.AppName,
                AppConfig.AllowSaveLogToFile,
                appUrl,
                "",
                ""
            );

            MyLogger.Instance.Init(AppConfig.AppName, AppConfig.AllowSaveLogToFile);
        }

        private object GetAndroidContext()
        {
#if UNITY_ANDROID
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            return currentActivity;
#else
            return null;
#endif
        }

        private Task<bool> WaitForCortexConnectionStaredAsync()
        {
            var tcs = new TaskCompletionSource<bool>();
            EventHandler<bool> handler = null;
            handler = (sender, isConnected) =>
            {
                _client.CortexConnectionStared -= handler;
                tcs.TrySetResult(isConnected);
            };
            _client.CortexConnectionStared += handler;
            return tcs.Task;
        }

        private Task<UserDataInfo> WaitForGetUserLoginAsync()
        {
            var tcs = new TaskCompletionSource<UserDataInfo>();
            EventHandler<UserDataInfo> handler = null;
            handler = (sender, data) =>
            {
                _client.GetUserLoginDone -= handler;
                UnityEngine.Debug.Log("AuthService: WaitForGetUserLoginAsync(): User logged in: " + data.EmotivId);
                if (!string.IsNullOrEmpty(data.EmotivId))
                {
                    _loggedInUser = data;
                }
                tcs.TrySetResult(data);
            };
            _client.GetUserLoginDone += handler;
            _client.GetUserLogin();
            return tcs.Task;
        }

        private Task<UserDataInfo> WaitForLoginAsync(string authCode)
        {
            var tcs = new TaskCompletionSource<UserDataInfo>();
            EventHandler<UserDataInfo> handler = null;
            handler = (sender, data) =>
            {
                _client.LoginDone -= handler;
                UnityEngine.Debug.Log("AuthService: WaitForLoginAsync(): User logged in: " + data.EmotivId);
                if (!string.IsNullOrEmpty(data.EmotivId))
                {
                    _loggedInUser = data;
                }
                tcs.TrySetResult(data);
            };
            _client.LoginDone += handler;
            _client.LoginWithAuthenticationCode(authCode);
            return tcs.Task;
        }

        private Task<bool> CheckAccessRightsAsync()
        {
            var tcs = new TaskCompletionSource<bool>();

            EventHandler<bool> okHandler = null;
            EventHandler<ErrorMsgEventArgs> errorHandler = null;

            okHandler = (sender, hasAccessRight) =>
            {
                _client.HasAccessRightOK -= okHandler;
                _client.ErrorMsgReceived -= errorHandler;
                tcs.TrySetResult(hasAccessRight);
            };

            errorHandler = (sender, error) =>
            {
                if (error.MethodName != "hasAccessRight")
                {
                    return;
                }
                _client.HasAccessRightOK -= okHandler;
                _client.ErrorMsgReceived -= errorHandler;
                tcs.TrySetResult(false);
            };

            _client.HasAccessRightOK += okHandler;
            _client.ErrorMsgReceived += errorHandler;
            _client.HasAccessRights();
            return tcs.Task;
        }

        private Task<(bool Success, string CortexToken)> AuthorizeAsync()
        {
            var tcs = new TaskCompletionSource<(bool Success, string CortexToken)>();

            EventHandler<string> okHandler = null;
            EventHandler<ErrorMsgEventArgs> errorHandler = null;
            EventHandler<string> eulaHandler = null;

            okHandler = (sender, token) =>
            {
                Cleanup();
                tcs.TrySetResult((true, token));
            };

            eulaHandler = (sender, token) =>
            {
                Cleanup();
                tcs.TrySetResult((false, token));
            };

            errorHandler = (sender, error) =>
            {
                if (error.MethodName != "authorize")
                {
                    return;
                }
                Cleanup();
                tcs.TrySetResult((false, ""));
            };

            void Cleanup()
            {
                _client.AuthorizeOK -= okHandler;
                _client.EULANotAccepted -= eulaHandler;
                _client.ErrorMsgReceived -= errorHandler;
            }

            _client.AuthorizeOK += okHandler;
            _client.EULANotAccepted += eulaHandler;
            _client.ErrorMsgReceived += errorHandler;

            _client.Authorize(string.Empty, 5000);
            return tcs.Task;
        }

        private Task<License> GetLicenseInfoAsync()
        {
            var tcs = new TaskCompletionSource<License>();
            EventHandler<(CortexErrorCode error, License data)> handler = null;

            handler = (sender, result) =>
            {
                _client.GetLicenseInfoResult -= handler;
                UnityEngine.Debug.Log("AuthService: GetLicenseInfoAsync(): result code: " + result.error);
                if (result.error == CortexErrorCode.OK)
                {
                    tcs.TrySetResult(result.data);
                }
                else
                {
                    tcs.TrySetResult(null);
                }
            };

            _client.GetLicenseInfoResult += handler;

            _client.GetLicenseInfo();
            return tcs.Task;
        }

        private async Task<bool> EnsureAndroidPermissionsAsync()
        {
#if UNITY_ANDROID
            if (HasAllPermissions())
            {
                return true;
            }

            foreach (var permission in GetRequiredPermissions())
            {
                if (!Permission.HasUserAuthorizedPermission(permission))
                {
                    Permission.RequestUserPermission(permission);
                    while (!Permission.HasUserAuthorizedPermission(permission))
                    {
                        await Task.Delay(100);
                    }
                }
            }

            return HasAllPermissions();
#else
            await Task.Yield();
            return true;
#endif
        }

#if UNITY_ANDROID
        private static string[] GetRequiredPermissions()
        {
            if (GetAndroidVersion() >= 31)
            {
                return new[] {
                    "android.permission.ACCESS_FINE_LOCATION",
                    "android.permission.BLUETOOTH_SCAN",
                    "android.permission.BLUETOOTH_CONNECT"
                };
            }

            return new[] {
                "android.permission.ACCESS_FINE_LOCATION",
                "android.permission.BLUETOOTH"
            };
        }

        private static bool HasAllPermissions()
        {
            foreach (var permission in GetRequiredPermissions())
            {
                if (!Permission.HasUserAuthorizedPermission(permission))
                {
                    return false;
                }
            }
            return true;
        }

        private static int GetAndroidVersion()
        {
            string osInfo = SystemInfo.operatingSystem;
            if (osInfo.Contains("Android"))
            {
                int apiIndex = osInfo.IndexOf("API-");
                if (apiIndex != -1)
                {
                    string apiLevel = osInfo.Substring(apiIndex + 4).Split(' ')[0];
                    if (int.TryParse(apiLevel, out int androidVersion))
                    {
                        return androidVersion;
                    }
                }
            }
            return 0;
        }
#endif

        private void InitAuthentication(string clientId, string clientSecret)
        {
#if USE_EMBEDDED_LIB || UNITY_ANDROID || UNITY_IOS
            string server = "";
#if DEV_SERVER
            Debug.Log("Development build detected. Using development server.");
            server = "cerebrum-dev.emotivcloud.com";
#else
            Debug.Log("Production build detected. Using production server.");
            server = "cerebrum.emotivcloud.com";
#endif
            string hash = Md5(clientId);
            string prefixRedirectUrl = "emotiv-" + hash;
            string redirectUrl = prefixRedirectUrl + "://authorize";
            string serverUrl = $"https://{server}";
#if UNITY_ANDROID || UNITY_IOS
            string authorizationUrl = $"https://{server}/api/oauth/authorize/?response_type=code" +
                        $"&client_id={Uri.EscapeDataString(clientId)}" +
                        $"&redirect_uri={redirectUrl}" + "&hide_signup=1&hide_social_signin=1";
            UniWebViewManager.Instance.Init(
                authorizationUrl,
                prefixRedirectUrl
            );
#else
            _crossPlatformBrowser = new CrossPlatformBrowser();
            _crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.WindowsEditor, new WindowsSystemBrowser());
            _crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.WindowsPlayer, new WindowsSystemBrowser());
            _crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.OSXEditor, new DeepLinkBrowser());
            _crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.OSXPlayer, new DeepLinkBrowser());

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            // Deep linking is not supported on Windows (except UWP), so RegistryConfig is used to handle this case.
            new RegistryConfig(prefixRedirectUrl).Configure();
#endif

            var configuration = new AuthorizationCodeFlow.Configuration()
            {
                clientId = clientId,
                clientSecret = clientSecret,
                redirectUri = redirectUrl,
                scope = ""
            };
            var auth = new MockServerAuth(configuration, serverUrl);
            _authenticationSession = new AuthenticationSession(auth, _crossPlatformBrowser);
            _authenticationSession.loginTimeout = TimeSpan.FromSeconds(600);
#endif
#endif
        }

#if USE_EMBEDDED_LIB || UNITY_ANDROID || UNITY_IOS
        private string BytesToHex(byte[] bytes)
        {
            char[] hexChars = new char[bytes.Length * 2];
            for (int j = 0; j < bytes.Length; ++j)
            {
                int v = bytes[j] & 0xFF;
                hexChars[j * 2] = HEX_ARRAY[v >> 4];
                hexChars[j * 2 + 1] = HEX_ARRAY[v & 0x0F];
            }
            return new string(hexChars);
        }

        private string Md5(string s)
        {
            try
            {
                using (var md5 = MD5.Create())
                {
                    byte[] inputBytes = Encoding.UTF8.GetBytes(s);
                    byte[] hashBytes = md5.ComputeHash(inputBytes);
                    return BytesToHex(hashBytes);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return string.Empty;
            }
        }
#endif
    }
}
