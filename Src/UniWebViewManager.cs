#if UNITY_ANDROID || UNITY_IOS
using UnityEngine;
using System;

public class UniWebViewManager : MonoBehaviour
{
    private static readonly string LogTag = "[UniWebViewManager]"; // Log tag for consistency

    private static UniWebViewManager _instance;
    public static UniWebViewManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var obj = new GameObject("UniWebViewManager");
                _instance = obj.AddComponent<UniWebViewManager>();
                DontDestroyOnLoad(obj);
            }
            return _instance;
        }
    }

    private UniWebViewAuthenticationSession authSession;
    private UniWebViewSafeBrowsing  safeBrowsing;
    private string _authUrl;
    private string _urlScheme;

    public void Init(string authUrl, string urlScheme)
    {
        _authUrl = authUrl;
        _urlScheme = urlScheme;
    }

    public void StartAuthorization(Action<string> onSuccess, Action<long, string> onError)
    {
        if (string.IsNullOrEmpty(_authUrl) || string.IsNullOrEmpty(_urlScheme))
        {
            Debug.LogError($"{LogTag} Init must be called with valid authUrl and urlScheme before starting authorization.");
            return;
        }

        Debug.Log($"{LogTag} Starting authorization using UniWebViewAuthenticationSession...");

        authSession = UniWebViewAuthenticationSession.Create(_authUrl, _urlScheme);
        
        authSession.OnAuthenticationFinished += (session, result) =>
        {
            if (!string.IsNullOrEmpty(result))
            {
                Debug.Log($"{LogTag} Auth finished. Callback URL: {result}");

                string code = ExtractCodeFromUri(result);
                if (!string.IsNullOrEmpty(code))
                {
                    onSuccess?.Invoke(code);
                }
                else
                {
                    onError?.Invoke(-1, $"{LogTag} Authorization code not found in redirect URL.");
                }
            }
            else
            {
                onError?.Invoke(-2, $"{LogTag} Authentication session failed or was cancelled.");
            }
        };

        authSession.OnAuthenticationErrorReceived += (session, errorCode, errorMessage) =>
        {
            Debug.LogError($"{LogTag} Authentication Error: {errorCode} - {errorMessage}");
            onError?.Invoke(errorCode, errorMessage);
        };

        authSession.Start();
    }

    private string ExtractCodeFromUri(string uri)
    {
        try
        {
            var query = new Uri(uri).Query.TrimStart('?');
            foreach (var param in query.Split('&'))
            {
                var keyValue = param.Split('=');
                if (keyValue.Length == 2 && Uri.UnescapeDataString(keyValue[0]) == "code")
                {
                    return Uri.UnescapeDataString(keyValue[1]);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"{LogTag} Failed to extract code from URI: {e.Message}");
        }

        return null;
    }

    public void OpenURL(string url, Action<bool>onClosed)
    {
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogError($"{LogTag} URL cannot be null or empty.");
            return;
        }

        safeBrowsing = UniWebViewSafeBrowsing.Create(url);
        safeBrowsing.OnSafeBrowsingFinished += (browsing) => { 
            Debug.Log("UniWebViewSafeBrowsing closed.");
            onClosed?.Invoke(true);
        };

        safeBrowsing.Show();
    }

    public void Cleanup()
    {
        if (authSession != null)
        {
            authSession = null;
        }

        _authUrl = null;
        _urlScheme = null;
    }
}
#endif