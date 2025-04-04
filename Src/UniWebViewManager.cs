using UnityEngine;
using System;

public class UniWebViewManager : MonoBehaviour
{
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

    private UniWebView webView;
    private string _authUrl;

    public void Init(string authUrl, string urlScheme)
    {
        _authUrl = authUrl;
        GameObject webViewGameObject = new GameObject("UniWebView");
        webView = webViewGameObject.AddComponent<UniWebView>();

        webView.AddUrlScheme(urlScheme);
        webView.Load(_authUrl);
        webView.Frame = new Rect(0, 0, Screen.width, Screen.height);
    }

    public void StartAuthorization(Action<string> onSuccess, Action<long, string> onError)
    {
        Debug.Log($"UniWebViewManager Starting authorization process...");

        if (webView == null)
        {
            Debug.Log($"UniWebViewManager is not initialized. Call Init() first.");
            return;
        }

        if (!string.IsNullOrEmpty(_authUrl))
        {
            webView.Load(_authUrl);
        }

        webView.Show();

        webView.OnMessageReceived += (view, message) =>
        {
            Debug.Log($"UniWebViewManager Message received: {message.RawMessage}");
            if (message.RawMessage.Contains("?code="))
            {
                string code = ExtractCodeFromUri(message.RawMessage);
                webView.Hide();

                onSuccess?.Invoke(code);
            }
        };

        webView.OnPageErrorReceived += (view, errorCode, message) =>
        {
            Debug.LogError($"Authorization Error: {errorCode} - {message}");
            onError?.Invoke(errorCode, message);
        };
    }

    private string ExtractCodeFromUri(string uri)
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

        return null;
    }

    public void Cleanup()
    {
        if (webView != null)
        {
            Destroy(webView.gameObject);
            webView = null;
            _authUrl = null;
        }
    }
}