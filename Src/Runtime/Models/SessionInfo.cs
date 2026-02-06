using EmotivUnityPlugin;

namespace Emotiv.Cortex.Models
{
    public enum SessionStatus
    {
        Opened = 0,
        Activated = 1,
        Closed = 2
    }

    public sealed class SessionInfo
    {
        public string SessionId { get; private set; }
        public SessionStatus Status { get; private set; }
        public string ApplicationId { get; private set; }
        public string HeadsetId { get; private set; }

        public static SessionInfo FromSessionEventArgs(SessionEventArgs sessionInfo)
        {
            return new SessionInfo
            {
                SessionId = sessionInfo.SessionId,
                Status = MapStatus(sessionInfo.Status),
                ApplicationId = sessionInfo.ApplicationId,
                HeadsetId = sessionInfo.HeadsetId
            };
        }

        private static SessionStatus MapStatus(EmotivUnityPlugin.SessionStatus status)
        {
            switch (status)
            {
                case EmotivUnityPlugin.SessionStatus.Opened:
                    return SessionStatus.Opened;
                case EmotivUnityPlugin.SessionStatus.Activated:
                    return SessionStatus.Activated;
                default:
                    return SessionStatus.Closed;
            }
        }
    }
}
