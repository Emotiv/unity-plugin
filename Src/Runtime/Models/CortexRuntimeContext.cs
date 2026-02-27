using EmotivUnityPlugin; // TODO (Tung Nguyen): remove this line when move all old code to new SDK   
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

    }
}