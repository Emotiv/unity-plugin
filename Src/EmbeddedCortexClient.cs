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

namespace EmotivUnityPlugin
{
    // for android
    #if UNITY_ANDROID
    public class CortexLibInterfaceProxy : AndroidJavaProxy
    {
        public CortexLibInterfaceProxy() : base("com.emotiv.unityplugin.CortexConnectionInterface") { }

        void onReceivedMessage(String msg) {
            Debug.Log("onReceivedMessage: " + msg);
            // Handle the callback in Unity
            // convert string to json object
            EmbeddedCortexClient.Instance.OnMessageReceived(msg);
        }

        void onCortexLibStart() {
            Debug.Log("Cortex Lib Started");
            // Start your timer or perform other initialization
            // EmbeddedCortexClient.Instance.OnCortexLibStarted();
        }
    }
    #endif

    /// <summary>
    /// Represents a simple client for the Cortex service.
    /// </summary>
    public class EmbeddedCortexClient : CortexClient
    {
        #if UNITY_ANDROID
        private AndroidJavaObject _cortexLibManager;
        private CortexLibInterfaceProxy cortexLibInterfaceProxy;
        #endif

        // Private constructor to prevent direct instantiation
        public EmbeddedCortexClient() { }

        // Implementation of the abstract method

        // override the init method
        public override void Init(object context = null)
        {
            _nextRequestId = 1;
            _methodForRequestId = new Dictionary<int, string>();

            #if UNITY_ANDROID
            if (context is AndroidJavaObject application)
            {
                LoadCortexLibAndroid(application);
            }
            else
            {
                Debug.LogError("Expected AndroidJavaObject activity for Android initialization");
            }
            #endif

        }

        // override the close method
        public override void Close()
        {
            // TODO
        }

        public void OnCortexLibStarted() {
            OnWSConnected(true);
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
                // start the cortex lib 
                _cortexLibManager.Call("start", cortexLibInterfaceProxy);
            }
            else
                UnityEngine.Debug.LogError("CortexLibManager is null. Cannot load cortex lib.");

        }
        #endif
        /// <summary>
        /// Build a json rpc request and send message via websocket
        /// </summary>
        public override void SendTextMessage(JObject param, string method, bool hasParam = true)
        {
            string request = PrepareRequest(method, param, hasParam);
            _cortexLibManager.Call("sendRequest", request);
        }
    }
}
