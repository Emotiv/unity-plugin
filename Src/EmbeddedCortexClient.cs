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
using Newtonsoft.Json.Linq;
using UnityEngine;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace EmotivUnityPlugin
{
    // Platform-specific helper classes
    #if UNITY_ANDROID
    public class CortexLibInterfaceProxy : AndroidJavaProxy
    {
        public CortexLibInterfaceProxy() : base("com.emotiv.unityplugin.CortexConnectionInterface") { }
        void onReceivedMessage(String msg) => EmbeddedCortexClient.Instance.OnMessageReceived(msg);
        void onCortexStarted() { Debug.Log("Cortex Lib Started"); EmbeddedCortexClient.Instance.OnWSConnected(true); }
    }
    public class CortexLogHandler : AndroidJavaProxy
    {
        public CortexLogHandler() : base("com.emotiv.unityplugin.JavaLogInterface") { }
        public void onReceivedLog(String msg) => MyLogger.Instance.Log(LogType.Log, "CortexLog", msg, null);
    }
    #elif USE_EMBEDDED_LIB
    public class CortexReponseHandler : ResponseHandlerCpp
    {
        public CortexReponseHandler() : base() {}
        public override void processResponse(string responseMessage) => OnCortexResponse?.Invoke(this, responseMessage);
        public event EventHandler<string>? OnCortexResponse;
    }
    public class CortexStarted : CortexStartedEventHandler
    {
        public CortexStarted() : base() {}
        public override void onCortexStarted() { UnityEngine.Debug.Log("Cortex Started"); OnCortexStarted?.Invoke(this, true); }
        public event EventHandler<bool>? OnCortexStarted;
    }
    public class CortexLog : CortexLogEventHandler
    {
        public CortexLog() : base() {}
        public override void onLogMessage(string message) => UnityEngine.Debug.Log(message);
    }
    #elif UNITY_IOS
    public class CortexIOSHandler 
    {
        [DllImport("__Internal")] public static extern bool InitCortexLib();
        [DllImport("__Internal")] public static extern void SendRequest(string request);
        [DllImport("__Internal")] public static extern void StopCortexLib();
        public delegate void MessageCallback(string message);
        public delegate void StartedCallback();
        [DllImport("__Internal")] private static extern void RegisterUnityResponseCallback(MessageCallback callback);
        [DllImport("__Internal")] private static extern void RegisterUnityStartedCallback(StartedCallback callback);
        [AOT.MonoPInvokeCallback(typeof(MessageCallback))]
        private static void OnMessageReceived(string message) => EmbeddedCortexClient.Instance.OnMessageReceived(message);
        [AOT.MonoPInvokeCallback(typeof(StartedCallback))]
        private static void OnCortexLibIosStarted() { Debug.Log("OnCortexLibIosStarted"); EmbeddedCortexClient.Instance.OnWSConnected(true); }
        public static void RegisterCallback() { RegisterUnityResponseCallback(OnMessageReceived); RegisterUnityStartedCallback(OnCortexLibIosStarted); }
    }
    #endif

    // Main client class
    #if USE_EMBEDDED_LIB || UNITY_ANDROID || UNITY_IOS
    public class EmbeddedCortexClient : CortexClient
    {
        // Platform-specific fields
        #if UNITY_ANDROID
        private AndroidJavaObject _cortexLibManager;
        private CortexLibInterfaceProxy cortexLibInterfaceProxy;
        private CortexLogHandler _cortexLogHandler;
        #elif USE_EMBEDDED_LIB
        private CortexReponseHandler _responseHandler;
        private EmbeddedCortexClientNative _cortexClient;
        #endif

        private IntPtr _cortexLibPtr = IntPtr.Zero; // Only used for native P/Invoke

        public EmbeddedCortexClient() { }

        public override void Init(object context = null)
        {
            #if UNITY_ANDROID
            if (context is AndroidJavaObject activity)
            {
                AndroidJavaObject application = activity.Call<AndroidJavaObject>("getApplication");
                LoadCortexLibAndroid(application);
            }
            else
            {
                Debug.LogError("Expected AndroidJavaObject activity for Android initialization");
            }
            #elif USE_EMBEDDED_LIB
            // Enable this line if you want to see the cortex log messages.   
            //CortexLog logger = new();
            //CortexLib.setLogHandler(1, logger);
            CortexStarted startEvent = new();
            startEvent.OnCortexStarted += CortexStarted;
            CortexLib.start(startEvent);
            #elif UNITY_IOS
            CortexIOSHandler.RegisterCallback();
            CortexIOSHandler.InitCortexLib();
            #endif
        }

        private static void OnMessageReceivedStatic(string message) => EmbeddedCortexClient.Instance.OnMessageReceived(message);
        private static void OnWSConnectedStatic() => EmbeddedCortexClient.Instance.OnWSConnected(true);

        #if USE_EMBEDDED_LIB
        private void CortexStarted(object? sender, bool e)
        {
            _cortexClient = new EmbeddedCortexClientNative();
            _responseHandler = new CortexReponseHandler();
            _responseHandler.OnCortexResponse += OnCortexResponse;
            _cortexClient.registerResponseHandler(_responseHandler);
        }
        #endif

        public override void Close()
        {
            #if UNITY_ANDROID
            if (_cortexLibManager != null) _cortexLibManager.Call("stop");
            #elif USE_EMBEDDED_LIB
            _cortexClient?.close();
            #elif UNITY_IOS
            CortexIOSHandler.StopCortexLib();
            #endif
        }

        #if UNITY_ANDROID
        private void LoadCortexLibAndroid(AndroidJavaObject application)
        {
            cortexLibInterfaceProxy = new CortexLibInterfaceProxy();
            AndroidJavaClass cortexLibActivityClass = new AndroidJavaClass("com.emotiv.unityplugin.CortexLibActivity");
            _cortexLibManager = cortexLibActivityClass.CallStatic<AndroidJavaObject>("getInstance");
            if (_cortexLibManager != null)
            {
                _cortexLibManager.Call("load", application);
                _cortexLogHandler = new CortexLogHandler();
                _cortexLibManager.Call("setJavaLogInterface", _cortexLogHandler);
                _cortexLibManager.Call("start", cortexLibInterfaceProxy);
                #if DEV_SERVER
                Debug.Log("Build is Development");
                #else
                Debug.Log("Build is Production");
                #endif
            }
            else Debug.LogError("CortexLibManager is null. Cannot load cortex lib.");
        }
        #elif USE_EMBEDDED_LIB
        private void OnCortexResponse(object? sender, string message) => EmbeddedCortexClient.Instance.OnMessageReceived(message);
        #endif

        public override void SendTextMessage(JObject param, string method, bool hasParam = true)
        {
            string request = PrepareRequest(method, param, hasParam);
            #if UNITY_ANDROID
            _cortexLibManager.Call("sendRequest", request);
            #elif USE_EMBEDDED_LIB
            _cortexClient.sendRequest(request);
            #elif UNITY_IOS
            CortexIOSHandler.SendRequest(request);
            #endif
        }
    }
    #endif
}
