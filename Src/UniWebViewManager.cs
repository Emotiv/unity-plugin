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

    public void Init(string authUrl, string urlScheme)
    {
        GameObject webViewGameObject = new GameObject("UniWebView");
        webView = webViewGameObject.AddComponent<UniWebView>();

        webView.AddUrlScheme(urlScheme);
        webView.Load(authUrl);
        webView.Frame = new Rect(0, 0, Screen.width, Screen.height);   
    }
    public void StartAuthorization(Action<string> onSuccess, Action<long, string> onError)
    {
        Debug.Log("UniWebViewManager Starting authorization process...");
        if (webView == null)
        {
            Debug.LogError("UniWebViewManager is not initialized. Call Init() first.");
            return;
        }

        webView.Show();


        webView.OnMessageReceived += (view, message) => {
            if (message.RawMessage.Contains("?code="))
            {
                string code = ExtractCodeFromUri(message.RawMessage);
                webView.Hide();
                Cleanup();

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
        }
    }
}
