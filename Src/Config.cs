using System.IO;

namespace EmotivUnityPlugin
{
    /// <summary>
    /// Contain common Config of a Unity App.
    /// </summary>
    public static class Config
    {
        public static string AppClientId            = "";
        public static string AppClientSecret        = "";
        public static string AppUrl = "";
        public static string AppName = "";
        public static string ProviderName = "";
        public static string LogDirectory = "";
        public static string DataDirectory = "";

        public static string EmotivAppsPath         = ""; // location of emotiv Apps . Eg: C:\Program Files\EmotivApps
        public static string TmpDataFileName        = "data.dat";
        public static string ProfilesDir            = "Profiles";
        public static string LogsDir                = "UnityLogs";
        public static int QUERY_HEADSET_TIME        = 1000;
        public static int TIME_CLOSE_STREAMS        = 1000;
        public static int RETRY_CORTEXSERVICE_TIME  = 5000;
        public static int WAIT_USERLOGIN_TIME       = 5000;
        
        // If you use an Epoc Flex headset, then you must put your configuration here
        // TODO: need detail here
        public static string FlexMapping = @"{
                                  'CMS':'TP8', 'DRL':'P6',
                                  'RM':'TP10','RN':'P4','RO':'P8'}";

        public static void Init(
            string clientId,
            string clientSecret,
            string appName,
            bool allowSaveLogAndDataToFile,
            string appUrl,
            string providerName,
            string emotivAppsPath
        )
        {
            AppClientId = clientId;
            AppClientSecret = clientSecret;
            AppName = appName;
            AppUrl = appUrl;
            ProviderName = providerName;
            EmotivAppsPath = emotivAppsPath;

            if (allowSaveLogAndDataToFile)
            {
                // create tmp directory for unity app
                string tmpPath = Utils.GetAppTmpPath(providerName, appName);
                LogDirectory = Path.Combine(tmpPath, LogsDir);
                DataDirectory = Path.Combine(tmpPath, ProfilesDir);

                // Ensure the directories exist
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }

                if (!Directory.Exists(DataDirectory))
                {
                    Directory.CreateDirectory(DataDirectory);
                }
            }
            else
            {
                LogDirectory = "";
                DataDirectory = "";
            }

        }
    }

    public static class DataStreamName
    {
        public const string DevInfos           = "dev";
        public const string EEG                = "eeg";
        public const string Motion             = "mot";
        public const string PerformanceMetrics = "met";
        public const string BandPower          = "pow";
        public const string MentalCommands     = "com";
        public const string FacialExpressions  = "fac";
        public const string SysEvents          = "sys";   // System events of the mental commands and facial expressions
        public const string EQ                 = "eq"; // EEG quality
    }

    public static class WarningCode
    {
        public const int StreamStop               = 0;
        public const int SessionAutoClosed        = 1;
        public const int UserLogin                = 2;
        public const int UserLogout               = 3;
        public const int ExtenderExportSuccess    = 4;
        public const int ExtenderExportFailed     = 5;
        public const int UserNotAcceptLicense     = 6;
        public const int UserNotHaveAccessRight   = 7;
        public const int UserRequestAccessRight   = 8;
        public const int AccessRightGranted       = 9;
        public const int AccessRightRejected      = 10;
        public const int CannotDetectOSUSerInfo   = 11;
        public const int CannotDetectOSUSername   = 12;
        public const int ProfileLoaded            = 13;
        public const int ProfileUnloaded          = 14;
        public const int CortexAutoUnloadProfile  = 15;
        public const int UserLoginOnAnotherOsUser = 16;
        public const int EULAAccepted             = 17;
        public const int StreamWritingClosed      = 18;
        public const int CortexIsReady            = 23;
        public const int UserNotAcceptPrivateEULA = 28;
        public const int DataPostProcessingFinished = 30; // Data post processing finished, this event is used to notify the app that the data has been processed and is ready for use, for example exporting
        public const int HeadsetWrongInformation = 100;
        public const int HeadsetCannotConnected   = 101;
        public const int HeadsetConnectingTimeout = 102;
        public const int HeadsetDataTimeOut       = 103;
        public const int HeadsetConnected         = 104;
        public const int BTLEPermissionNotGranted = 31;
        public const int HeadsetScanFinished      = 142;
    }

    // error code
    public static class ErrorCode {
        public const int NoAppInfoOrAccessRightError = -32102; // app do not have access right to or no app info in the Cortex database
        public const int LoginTokenError = -32108;
        public const int AuthorizeTokenError = -32109;
        public const int CloudTokenIsRefreshing = -32130;
        public const int NotReAuthorizedError = -32170;
        public const int CortexTokenCompareErrorAppInfo = -32135;
        public const int CortexTokenNotFit = -32034;

    }

    public static class DevStreamParams
    {
        public const string battery = "Battery";
        public const string signal = "Signal";
    }
}
