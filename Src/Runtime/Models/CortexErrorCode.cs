namespace Emotiv.Cortex.Models
{
    public enum CortexErrorCode
    {
        OK = 0,
        PermissionsDenied = 2,
        WebSocketConnectFailed = 3,
        AccessRightDenied = 4,
        AuthorizationFailed = 5,
        LicenseExpiredOrInvalid = 6,
        UnknownError
    }
}
