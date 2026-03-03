using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Timers;
using UnityEngine;

namespace EmotivUnityPlugin
{
    /// <summary>
    /// Reponsible for finding headsets.
    /// </summary>
    public class HeadsetFinder
    {
        private CortexClient _ctxClient = CortexClient.Instance;

        /// <summary>
        /// Timer for querying headsets
        /// </summary>
        private Timer _aTimer = null;

        // Event
        public event EventHandler<bool> HeadsetDisConnectedOK;
        public event EventHandler<List<Headset>> QueryHeadsetOK;

        public HeadsetFinder()
        {
            _ctxClient = CortexClient.Instance;
            _ctxClient.QueryHeadsetOK        += OnQueryHeadsetReceived;
            _ctxClient.HeadsetDisConnectedOK += OnHeadsetDisconnectedOK;
        }

        public static HeadsetFinder Instance { get; } = new HeadsetFinder();

        private void OnHeadsetDisconnectedOK(object sender, bool e)
        {
            HeadsetDisConnectedOK(this, true);
        }

        private void OnQueryHeadsetReceived(object sender, List<Headset> headsets)
        {
            QueryHeadsetOK(this, headsets);
        }

        /// <summary>
        /// Init headset finder
        /// </summary>
        public void FinderInit()
        {
            SetQueryHeadsetTimer();
        }

        public void StopQueryHeadset() {
            if (_aTimer != null && _aTimer.Enabled) {
                UnityEngine.Debug.Log("Stop query headset");
                _aTimer.Stop();
            }
        }
        public void RefreshHeadset() {
            _ctxClient.ControlDevice("refresh", "", null);
        }

        /// <summary>
        /// Setup query headset timer
        /// </summary>
        private void SetQueryHeadsetTimer()
        {
            if (_aTimer != null) {
                 _aTimer.Enabled = true;
                return;
            }

            _aTimer = new Timer(Config.QUERY_HEADSET_TIME);

            // Hook up the Elapsed event for the timer. 
            _aTimer.Elapsed += OnTimedEvent;
            _aTimer.AutoReset = true;
            _aTimer.Enabled = true;
        }

        /// <summary>
        /// Handle timeout. Retry query headsets.
        /// </summary>
        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            _ctxClient.QueryHeadsets("");
        }
    }
}
