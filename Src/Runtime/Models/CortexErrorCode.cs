namespace Emotiv.Cortex.Models
{
    public enum CortexErrorCode
    {
        OK = 0,
        NoUserLogin, // Indicates that there is no user logged in.
        AuthorizationFailed, // Indicates that the authorization process failed overall.
        LicenseError, // Indicates that there is an issue with the license (e.g., invalid or expired license).
        HeadsetNotFound, // Indicates that the specified headset could not be found.
        SubscriptionFailed, // Indicates that the subscription process failed.
        UnknownError
    }
}
