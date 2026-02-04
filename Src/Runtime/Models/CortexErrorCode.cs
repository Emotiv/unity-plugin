namespace Emotiv.Cortex.Models
{
    public enum CortexErrorCode
    {
        OK = 0,
        PermissionsDenied = 2, // Indicates that the requested operation failed because the required permissions were not granted.
        CortexConnectionError = 3, // Indicates that the connection to Cortex started unsuccessfully.
        NoEULAAccepted = 4, // Indicates that the user has not accepted the End User License Agreement (EULA). Only for Cortex v3. Must be removed at Cortex v5
        AuthorizationFailed = 5, // Indicates that the authorization process failed overall.
        LicenseError = 6, // Indicates that there is an issue with the license (e.g., invalid or expired license).
        UnknownError
    }
}
