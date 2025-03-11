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
using System.Threading;
using Newtonsoft.Json.Linq;

using System.Collections.Generic;
using System.Collections;
using System.Timers;
using UnityEngine;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace EmotivUnityPlugin
{
    // for android
    #if UNITY_ANDROID
    public class CortexLibInterfaceProxy : AndroidJavaProxy
    {
        public CortexLibInterfaceProxy() : base("com.emotiv.unityplugin.CortexConnectionInterface") { }

        void onReceivedMessage(String msg) {
            // Handle the callback in Unity
            EmbeddedCortexClient.Instance.OnMessageReceived(msg);
        }

        void onCortexStarted() {
            Debug.Log("Cortex Lib Started");
            // Start your timer or perform other initialization
            EmbeddedCortexClient.Instance.OnWSConnected(true);
        }
    }

    // implement CortexLogHandler java class
    public class CortexLogHandler : AndroidJavaProxy
    {
        public CortexLogHandler() : base("com.emotiv.unityplugin.JavaLogInterface") { }

        public void onReceivedLog(String msg) {
            MyLogger.Instance.Log(LogType.Log, "CortexLog", msg, null);
        }
    }

    
    #elif USE_EMBEDDED_LIB
    public class CortexReponseHandler : ResponseHandlerCpp
    {
        public CortexReponseHandler() : base()
        {}

        public override void processResponse(string responseMessage)
        {
            if(OnCortexResponse != null)
                OnCortexResponse(this, responseMessage);
        }

        public event EventHandler<string>? OnCortexResponse;
    }

    public class CortexStarted : CortexStartedEventHandler
    {
        public CortexStarted() : base()
        {}

        public override void onCortexStarted()
        {
            UnityEngine.Debug.Log("Cortex Started");
            OnCortexStarted?.Invoke(this, true);
        }

        public event EventHandler<bool>? OnCortexStarted;
    }

    public class CortexLog : CortexLogEventHandler
    {
        public CortexLog() : base()
        {}

        public override void onLogMessage(string message)
        {
            UnityEngine.Debug.Log(message);
        }
    }

    #endif

    #if UNITY_IOS
    public class CortexIOSHandler 
    {
        [DllImport("__Internal")]
        public static extern bool InitCortexLib();
        
        [DllImport("__Internal")]
        public static extern void SendRequest(string request);
        
        [DllImport("__Internal")]
        public static extern void StopCortexLib();

        public delegate void MessageCallback(string message);
        public delegate void StartedCallback();
        
        [DllImport("__Internal")]
        private static extern void RegisterUnityResponseCallback(MessageCallback callback);
        [DllImport("__Internal")]
        private static extern void RegisterUnityStartedCallback(StartedCallback callback);
        
        [AOT.MonoPInvokeCallback(typeof(MessageCallback))]
        private static void OnMessageReceived(string message)
        {
            EmbeddedCortexClient.Instance.OnMessageReceived(message);
        }

        [AOT.MonoPInvokeCallback(typeof(StartedCallback))]
        private static void OnCortexLibIosStarted()
        {
            Debug.Log("OnCortexLibIosStarted");
            EmbeddedCortexClient.Instance.OnWSConnected(true);
        }

        public static void RegisterCallback()
        {
            RegisterUnityResponseCallback(OnMessageReceived);
            RegisterUnityStartedCallback(OnCortexLibIosStarted);
        }
    }
    #endif

    public class EmbeddedCortexClient : CortexClient
    {
        #if UNITY_ANDROID
        private AndroidJavaObject _cortexLibManager;
        private CortexLibInterfaceProxy cortexLibInterfaceProxy;
        private  CortexLogHandler _cortexLogHandler;
        #elif USE_EMBEDDED_LIB
        private CortexReponseHandler _responseHandler;
        #if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private EmbeddedCortexClientWin _cortexClient; // Cortex client for windows
        #endif
        #endif

        // Private constructor to prevent direct instantiation
        public EmbeddedCortexClient() { }

        // Implementation of the abstract method

        // override the init method
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

        private static void OnMessageReceivedStatic(string message)
        {
            EmbeddedCortexClient.Instance.OnMessageReceived(message);
        }

        private static void OnWSConnectedStatic()
        {
            EmbeddedCortexClient.Instance.OnWSConnected(true);
        }

        private void CortexStarted(object? sender, bool e)
        {
            #if USE_EMBEDDED_LIB
            _cortexClient = new EmbeddedCortexClientWin();
            _responseHandler = new CortexReponseHandler();
            _responseHandler.OnCortexResponse += OnCortexResponse;
            _cortexClient.registerResponseHandler(_responseHandler);
            #endif
        }

        // override the close method
        public override void Close()
        {
            // stop the cortex lib
            #if UNITY_ANDROID
            if (_cortexLibManager != null)
            {
                _cortexLibManager.Call("stop");
            }
            #elif USE_EMBEDDED_LIB
            _cortexClient.close();
            #elif UNITY_IOS
            CortexIOSHandler.StopCortexLib();
            #endif
        }

        #if UNITY_ANDROID
        private void LoadCortexLibAndroid(AndroidJavaObject application)
        {
            cortexLibInterfaceProxy = new CortexLibInterfaceProxy();
            AndroidJavaClass cortexLibActivityClass = new AndroidJavaClass("com.emotiv.unityplugin.CortexLibActivity");
            // Get the instance of CortexLibActivity
            _cortexLibManager = cortexLibActivityClass.CallStatic<AndroidJavaObject>("getInstance");
            
            bool isCortexLibManagerNotNull = _cortexLibManager != null;
            if (isCortexLibManagerNotNull) {
                _cortexLibManager.Call("load", application);
                // set log handler
                _cortexLogHandler = new CortexLogHandler();
                _cortexLibManager.Call("setJavaLogInterface", _cortexLogHandler);
                // start the cortex lib
                _cortexLibManager.Call("start", cortexLibInterfaceProxy);
            }
            else
                UnityEngine.Debug.LogError("CortexLibManager is null. Cannot load cortex lib.");

        }
        #elif USE_EMBEDDED_LIB
        private void OnCortexResponse(object? sender, string message) {
            // Handle the callback in Unity
            EmbeddedCortexClient.Instance.OnMessageReceived(message);;
        }
        #endif
        /// <summary>
        /// Build a json rpc request and send message via websocket
        /// </summary>
        public override void SendTextMessage(JObject param, string method, bool hasParam = true)
        {
            string request = PrepareRequest(method, param, hasParam);
            // UnityEngine.Debug.Log("SendTextMessage: " + request);
            #if UNITY_ANDROID
            _cortexLibManager.Call("sendRequest", request);
            #elif USE_EMBEDDED_LIB
            _cortexClient.sendRequest(request);
            #elif UNITY_IOS
            CortexIOSHandler.SendRequest(request);
            #endif
        }
    }
}
