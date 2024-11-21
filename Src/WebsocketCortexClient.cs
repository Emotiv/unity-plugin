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
using WebSocket4Net;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Timers;

namespace EmotivUnityPlugin
{
    /// <summary>
    /// Represents a simple client for the Cortex service.
    /// </summary>
    public class WebsocketCortexClient : CortexClient
    {
        const string Url = "wss://localhost:6868";
        static readonly object _locker = new object();
        private Dictionary<int, string> _methodForRequestId;

        /// <summary>
        /// Websocket Client.
        /// </summary>
        private WebSocket _wSC;
        
        /// <summary>
        /// Timer for connecting to Emotiv Cortex Service
        /// </summary>
        private System.Timers.Timer _wscTimer = null;

        // Private constructor to prevent direct instantiation
        public  WebsocketCortexClient() { }


        // override the init method
        public override void Init(object context = null)
        {
            _wSC = new WebSocket(Config.AppUrl);
            // Since Emotiv Cortex 3.7.0, the supported SSL Protocol will be TLS1.2 or later
            _wSC.Security.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            _wSC.Opened += new EventHandler(WebSocketClient_Opened);
            _wSC.Error  += new EventHandler<SuperSocket.ClientEngine.ErrorEventArgs>(WebSocketClient_Error);
            _wSC.Closed += WebSocketClient_Closed;
            _wSC.MessageReceived += WebSocketClient_MessageReceived;
            _wSC.DataReceived += WebSocketClient_DataReceived;

            // open websocket
            Open();
        }

        // override the close method
        public override void Close()
        {
            UnityEngine.Debug.Log("Force close websocket client.");
            if (_wscTimer != null) {
                _wscTimer = null;
            }
            // stop websocket client
            if (_wSC != null)
                _wSC.Close();
        }



        /// <summary>
        /// Set up timer for connecting to Emotiv Cortex service
        /// </summary>
        private void SetWSCTimer() {
            if (_wscTimer != null)
                return;
            _wscTimer = new System.Timers.Timer(Config.RETRY_CORTEXSERVICE_TIME);
            // Hook up the Elapsed event for the timer.
            _wscTimer.Elapsed       += OnTimerEvent;
            _wscTimer.AutoReset     = false; // do not auto reset
            _wscTimer.Enabled       = true; 
        }

        /// <summary>
        /// Handle for _wscTimer timer timeout
        //  Retry Connect when time out 
        /// </summary>
        private void OnTimerEvent(object sender, ElapsedEventArgs e)
        {
            UnityEngine.Debug.Log("OnTimerEvent: Retry connect to CortexService....");
            RetryConnect();
        }

        private void RetryConnect() {
           m_OpenedEvent.Reset();
            if (_wSC == null || (_wSC.State != WebSocketState.None && _wSC.State != WebSocketState.Closed))
                return;
            
            _wSC.Open();
        }

        private void WebSocketClient_DataReceived(object sender, DataReceivedEventArgs e)
        {
            // TODO
            UnityEngine.Debug.Log("WebSocketClient_DataReceived");
        }

        /// <summary>
        /// Build a json rpc request and send message via websocket
        /// </summary>
        public override void SendTextMessage(JObject param, string method, bool hasParam = true)
        {
            lock(_locker)
            {
                string request = PrepareRequest(method, param, hasParam);
                // UnityEngine.Debug.Log("Send " + method);
                // UnityEngine.Debug.Log(request.ToString());

                // send the json message
                _wSC.Send(request);
            }
        }

        /// <summary>
        /// Handle message received return from Emotiv Cortex Service
        /// </summary> 
        private void WebSocketClient_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            OnMessageReceived(e.Message);
        }

        /// <summary>
        /// Handle when socket close
        /// </summary>
        private void WebSocketClient_Closed(object sender, EventArgs e)
        {
            OnWSConnected(false);
            // start connecting cortex service again
            if (_wscTimer != null)
                _wscTimer.Start();
        }
        
        /// <summary>
        /// Handle when socket open
        /// </summary>
        private void WebSocketClient_Opened(object sender, EventArgs e)
        {
            m_OpenedEvent.Set();
            if (_wSC.State == WebSocketState.Open) {
                OnWSConnected(true);
                // stop timer
                _wscTimer.Stop();

            } else {
                UnityEngine.Debug.Log("Open Websocket unsuccessfully.");
            }
        }

        /// <summary>
        /// Handle error when try to open socket
        /// </summary>
        private void WebSocketClient_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            UnityEngine.Debug.Log(e.Exception.GetType() + ":" + e.Exception.Message + Environment.NewLine + e.Exception.StackTrace);

            if (e.Exception.InnerException != null) {
                UnityEngine.Debug.Log(e.Exception.InnerException.GetType());
                OnWSConnected(false);
                // start connecting cortex service again
                _wscTimer.Start();
            }
        }

        /// <summary>
        /// Open a websocket client.
        /// </summary>
        private void Open()
        {
            // set timer for connect cortex service
            SetWSCTimer();
            //Open websocket
            m_OpenedEvent.Reset();
            if (_wSC == null || (_wSC.State != WebSocketState.None && _wSC.State != WebSocketState.Closed))
                return;
            
            _wSC.Open();
        }
    }
}
