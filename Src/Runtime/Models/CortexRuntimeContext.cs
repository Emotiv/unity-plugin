using System;
using EmotivUnityPlugin; // TODO (Tung Nguyen): remove this line when move all old code to new SDK   
using System.Collections.Generic;
namespace Emotiv.Cortex.Models
{
    public sealed class CortexRuntimeContext
    {
        private readonly object _lock = new();

        // ------------------------
        // User
        // ------------------------
        private UserDataInfo _user;
        public UserDataInfo User
        {
            get
            {
                lock (_lock)
                {
                    return _user;
                }
            }
        }

        public void SetUser(UserDataInfo user)
        {
            lock (_lock)
            {
                _user = user;
            }
        }

        // ------------------------
        // Connection Status
        // ------------------------
        private bool _isConnected;
        public bool IsConnected
        {
            get
            {
                lock (_lock)
                {
                    return _isConnected;
                }
            }
        }

        public void SetConnectionStatus(bool isConnected)
        {
            lock (_lock)
            {
                _isConnected = isConnected;
            }
        }

        // ------------------------
        // Headsets
        // ------------------------
        private List<Headset> _headsets = new List<Headset>();
        public List<Headset> Headsets
        {
            get
            {
                lock (_lock)
                {
                    return new List<Headset>(_headsets);
                }
            }
        }

        public void SetHeadsets(List<Headset> headsets)
        {
            lock (_lock)
            {
                _headsets.Clear();
                if (headsets != null)
                {
                    _headsets.AddRange(headsets);
                }
            }
        }

        // connected headset ids
        private HashSet<string> _connectedHeadsetIds = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> ConnectedHeadsetIds
        {
            get
            {
                lock (_lock)
                {
                    return new HashSet<string>(_connectedHeadsetIds);
                }
            }
        }

        public void AddConnectedHeadsetId(string headsetId)
        {
            lock (_lock)
            {
                if (!_connectedHeadsetIds.Add(headsetId))
                {
                    // Handle the case where the headsetId was already present, if needed
                    UnityEngine.Debug.LogWarning($"Headset ID {headsetId} is already in the connected headset list.");
                }
            }
        }
        // clear connected headset id or clear all connected headset
        public void ClearConnectedHeadsetId(string headsetId = null)
        {
            lock (_lock)
            {
                if (headsetId == null)
                {
                    _connectedHeadsetIds.Clear();
                }
                else
                {
                    _connectedHeadsetIds.Remove(headsetId);
                }
            }
        }
    }
}
