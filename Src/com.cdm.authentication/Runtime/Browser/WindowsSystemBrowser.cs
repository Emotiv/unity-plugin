using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using IdentityModel.Client;
using UnityEngine;

namespace Cdm.Authentication.Browser
{
    public class WindowsSystemBrowser : IBrowser
    {
        private TaskCompletionSource<BrowserResult> _taskCompletionSource;
        public async Task<BrowserResult> StartAsync(
            string loginUrl, string redirectUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(loginUrl))
                throw new ArgumentNullException(nameof(loginUrl));

            if (string.IsNullOrEmpty(redirectUrl))
                throw new ArgumentNullException(nameof(redirectUrl));

            _taskCompletionSource = new TaskCompletionSource<BrowserResult>();
            cancellationToken.Register(() => { _taskCompletionSource?.TrySetCanceled(); });

            try
            {
                var state = ExtractStateFromUrl(loginUrl);
                Debug.Log("Opening browser for login: " + loginUrl + " with state: " + state);
                var callbackManager = new CallbackManager(state);

                Application.OpenURL(loginUrl);
                var response = await callbackManager.RunServer();
                // check response is not null
                if (response == null)
                {
                    _taskCompletionSource.SetResult(
                        new BrowserResult(BrowserStatus.UnknownError, "Browser could not be started."));
                    return await _taskCompletionSource.Task;
                }
                if (response == "error")
                {
                    _taskCompletionSource.SetResult(
                        new BrowserResult(BrowserStatus.UserCanceled, "User canceled the login."));
                    return await _taskCompletionSource.Task;
                }
                _taskCompletionSource.SetResult(
                    new BrowserResult(BrowserStatus.Success, response));

                return await _taskCompletionSource.Task;
            }
            catch (Exception ex)
            {
                _taskCompletionSource.SetResult(
                    new BrowserResult(BrowserStatus.UnknownError, ex.Message));
                return await _taskCompletionSource.Task;
            }
            
            
        }
        public string ExtractStateFromUrl(string url)
        {
            Uri uri = new Uri(url);
            string query = uri.Query;
            var queryParams = HttpUtility.ParseQueryString(query);
            return queryParams["state"];
        }

        public  static async Task ProcessCallback(string args)
        {
            UnityEngine.Debug.Log("Processing callback" + args);
            var response = new AuthorizeResponse(args);
            if (!String.IsNullOrWhiteSpace(response.State))
            {
                UnityEngine.Debug.Log($"Found state: {response.State}");
                var callbackManager = new CallbackManager(response.State);
                await callbackManager.RunClient(args);
                await Task.Delay(1000);
                Application.Quit();
            }
            else
            {
                UnityEngine.Debug.Log("Error: no state on response");
            }
        }
    }

    
}