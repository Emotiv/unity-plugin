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

namespace Emotiv.Cortex.Service
{
    public class AuthService : IAuthService
    {
        private readonly CortexRuntimeContext _context;
        private readonly CortexClient _client;

#if USE_EMBEDDED_LIB || UNITY_ANDROID || UNITY_IOS
        private CrossPlatformBrowser _crossPlatformBrowser;
        private AuthenticationSession _authenticationSession;
        private CancellationTokenSource _cancellationTokenSource;
        private static readonly char[] HEX_ARRAY = "0123456789abcdef".ToCharArray();
#endif

        public AuthService(CortexRuntimeContext context, CortexClient client)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _client = client ?? throw new ArgumentNullException(nameof(client));

#if UNITY_ANDROID || UNITY_IOS || USE_EMBEDDED_LIB
            InitAuthentication(AppConfig.ClientId, AppConfig.ClientSecret);
#endif
        }

        public async Task<CortexResult<UserDataInfo>> GetApiInfoAsync()
        {       
            while (!_context.IsConnected)
            {
                // Wait until Cortex connection is established before proceeding with API calls
                await Task.Delay(1000);
            }
            
            UnityEngine.Debug.Log("AuthService: GetApiInfoAsync(): Cortex connection established, proceeding with GetUserLogin.");
            var result = await WaitForGetUserLoginAsync();
            _context.SetUser(result);
            return CortexResult<UserDataInfo>.Success(result);
        }

        public async Task<CortexResult<UserDataInfo>> InitAsync()
        {
            return await CompleteAuthorizationAsync();
        }

        public async Task<CortexResult<UserDataInfo>> LoginAsync()
        {
            UnityEngine.Debug.Log("AuthService: LoginAsync(): Start login flow");
#if UNITY_ANDROID || UNITY_IOS
            var tcs = new TaskCompletionSource<CortexResult<UserDataInfo>>();
            UniWebViewManager.Instance.StartAuthorization(
                onSuccess: async (authCode) => {
                    Debug.Log("UniWebView Authorization succeeded! Starting login with auth code");
                    var result = await LoginWithAuthenticationCodeAsync(authCode);
                    tcs.TrySetResult(result);
                },
                onError: (errorCode, errorMessage) => {
                    Debug.LogError($"Authorization failed! Error {errorCode}: {errorMessage}");
                    tcs.TrySetResult(CortexResult<UserDataInfo>.Fail(
                        CortexErrorMapper.FromErrorCode(CortexErrorCode.AuthorizationFailed)));
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
            return CortexResult<UserDataInfo>.Fail(
                CortexErrorMapper.FromErrorCode(CortexErrorCode.UnknownError));
#else
            await Task.Yield();
            return CortexResult<UserDataInfo>.Fail(
                CortexErrorMapper.FromErrorCode(CortexErrorCode.UnknownError));
#endif
        }

        public void Logout()
        {
            if (string.IsNullOrEmpty(_context.User.EmotivId))
            {
                Debug.LogWarning("Logout requested but no user is logged in.");
                return;
            }
            _client.Logout(_context.User.EmotivId);
            _context.SetUser(new UserDataInfo());
        }

        private async Task<CortexResult<UserDataInfo>> LoginWithAuthenticationCodeAsync(string code)
        {
            UnityEngine.Debug.Log("AuthService: LoginWithAuthenticationCodeAsync(): code: " + code);
            UserDataInfo loginData = await WaitForLoginAsync(code);
            _context.SetUser(loginData);
            return await CompleteAuthorizationAsync();
        }

        private async Task<CortexResult<UserDataInfo>> CompleteAuthorizationAsync()
        {
            var loginData = _context.User;
            
            if (string.IsNullOrEmpty(loginData.EmotivId))
            {
                return CortexResult<UserDataInfo>.Fail(
                    CortexErrorMapper.FromErrorCode(CortexErrorCode.NoUserLogin));
            }
            // User is already logged in, proceed with authorization
            var authorizeResult = await AuthorizeAsync();
            if (!authorizeResult.IsSuccess)
            {
                return authorizeResult;
            }

            UnityEngine.Debug.Log("AuthService: CompleteAuthorizationAsync(): Authorized.");

            License license = await GetLicenseInfoAsync();
            UnityEngine.Debug.Log("AuthService: CompleteAuthorizationAsync(): License info retrieved." +
                (license != null ? $" expired: {license.expired}" : " license is null"));
            if (license == null || license.expired)
            {
                return CortexResult<UserDataInfo>.Fail(
                    CortexErrorMapper.FromErrorCode(CortexErrorCode.LicenseError));
            }

            var resultUser = new UserDataInfo(loginData.LastLoginTime, authorizeResult.Data.CortexToken, loginData.EmotivId)
            {
                EULAAccepted = authorizeResult.Data.EULAAccepted
            };
            _context.SetUser(resultUser);
            return CortexResult<UserDataInfo>.Success(resultUser);
        }


        private Task<UserDataInfo> WaitForGetUserLoginAsync()
        {
            var tcs = new TaskCompletionSource<UserDataInfo>();
            EventHandler<UserDataInfo> handler = null;
            handler = (sender, data) =>
            {
                _client.GetUserLoginDone -= handler;
                if (!string.IsNullOrEmpty(data.EmotivId))
                {
                    _context.SetUser(data);
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
                    _context.SetUser(data);
                }
                tcs.TrySetResult(data);
            };
            _client.LoginDone += handler;
            _client.LoginWithAuthenticationCode(authCode);
            return tcs.Task;
        }

        private Task<CortexResult<UserDataInfo>> AuthorizeAsync()
        {
            var tcs = new TaskCompletionSource<CortexResult<UserDataInfo>>();

            EventHandler<string> okHandler = null;
            EventHandler<ErrorMsgEventArgs> errorHandler = null;
            EventHandler<string> eulaHandler = null;

            okHandler = (sender, token) =>
            {
                Cleanup();
                var authorizedUser = new UserDataInfo(_context.User.LastLoginTime, token, _context.User.EmotivId)
                {
                    EULAAccepted = true
                };
                tcs.TrySetResult(CortexResult<UserDataInfo>.Success(authorizedUser));
            };

            eulaHandler = (sender, token) =>
            {
                Cleanup();
                UnityEngine.Debug.LogWarning("AuthService: AuthorizeAsync(): EULA not accepted.");
                _context.User.EULAAccepted = false;
                tcs.TrySetResult(CortexResult<UserDataInfo>.Fail(
                    CortexErrorMapper.FromErrorCode(CortexErrorCode.AuthorizationFailed)));
            };

            errorHandler = (sender, error) =>
            {
                if (error.MethodName != "authorize")
                {
                    return;
                }
                Cleanup();
                var mappedError = new CortexError(
                    CortexErrorCode.AuthorizationFailed,
                    error.MessageError,
                    error.Code);
                tcs.TrySetResult(CortexResult<UserDataInfo>.Fail(mappedError));
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
