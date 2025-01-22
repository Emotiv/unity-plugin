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
            // Handle the callback in Unity
            EmbeddedCortexClient.Instance.OnMessageReceived(msg);
        }

        void onCortexStarted() {
            Debug.Log("Cortex Lib Started");
            // Start your timer or perform other initialization
            EmbeddedCortexClient.Instance.OnWSConnected(true);
        }
    }
    #endif

    /// <summary>
    /// Represents a Client that connects with embedded CortexLib
    /// </summary>
    public class EmbeddedCortexClient : CortexClient
    {
        #if UNITY_ANDROID
        private AndroidJavaObject _cortexLibManager;
        private CortexLibInterfaceProxy cortexLibInterfaceProxy;
        // activity
        private AndroidJavaObject _activity;
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
                _activity = activity;
                AndroidJavaObject application = activity.Call<AndroidJavaObject>("getApplication");
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
            // stop the cortex lib
            #if UNITY_ANDROID
            if (_activity != null)
            {
                _activity.Call("stop");
            }
            #endif
        }

        #if UNITY_ANDROID
        private void LoadCortexLibAndroid(AndroidJavaObject application)
        {
            cortexLibInterfaceProxy = new CortexLibInterfaceProxy();
            // AndroidJavaClass cortexLibActivityClass = new AndroidJavaClass("com.emotiv.unityplugin.CortexLibActivity");
            // // Get the instance of CortexLibActivity
            // _cortexLibManager = cortexLibActivityClass.CallStatic<AndroidJavaObject>("getInstance");
            
            bool isCortexLibManagerNotNull = _activity != null;
            if (isCortexLibManagerNotNull) {
                _activity.Call("load", application);
                // start the cortex lib 
                _activity.Call("start", cortexLibInterfaceProxy);
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
            // UnityEngine.Debug.Log("SendTextMessage: " + request);

            _activity.Call("sendRequest", request);
        }

        // authenticate the user
        public override void Authenticate()
        {
            // call authenticate method in cortex lib
            _activity.Call("authenticate", Config.AppClientId, 100);
        }
    }
}
