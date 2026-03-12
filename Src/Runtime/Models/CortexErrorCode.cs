namespace Emotiv.Cortex.Models
{
    public enum CortexErrorCode
    {
        OK = 0,
        NoUserLogin = 1000, // Indicates that there is no user logged in.
        AuthorizationFailed = 1001, // Indicates that the authorization process failed overall.
        LicenseError = 1002, // Indicates that there is an issue with the license (e.g., invalid or expired license).
        HeadsetNotFound = 1003, // Indicates that the specified headset could not be found.
        CannotConnectToHeadset = 1004, // Indicates that the connection to the headset could not be established.
        SubscriptionFailed = 1005, // Indicates that the subscription process failed.
        NoConnectedHeadset = 1006, // Indicates that there is no connected headset.
        TrainingFailed = 1007, // Indicates that the training process failed.
        UnknownError = 2000
    }
}
