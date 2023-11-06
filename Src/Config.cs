namespace EmotivUnityPlugin
{
    /// <summary>
    /// Contain common Config of a Unity App.
    /// </summary>
    public static class Config
    {
        
        /// <summary>ClientId of your application.
        /// <para>To get a client id and a client secret, you must connect to your Emotiv
        /// account on emotiv.com and create a Cortex app.
        /// https://www.emotiv.com/my-account/cortex-apps/.</para></summary>
        public static string AppClientId            = "";
        public static string AppClientSecret        = "";

         public static string AppUrl                 = "wss://localhost:6868"; // default
        public static string AppVersion             = "1.0.0"; // default
        public static string AppName                = "UnityApp"; // default app name
        
        /// <summary>
        /// Name of directory where contain tmp data and logs file.
        /// </summary>
        public static string TmpAppDataDir          = "UnityApp";
        public static string EmotivAppsPath         = ""; // location of emotiv Apps . Eg: C:\Program Files\EmotivApps
        public static string TmpVersionFileName     = "version.ini";
        public static string TmpDataFileName        = "data.dat";
        public static string ProfilesDir            = "Profiles";
        public static string LogsDir                = "logs";
        public static int QUERY_HEADSET_TIME        = 1000;
        public static int TIME_CLOSE_STREAMS        = 1000;
        public static int RETRY_CORTEXSERVICE_TIME  = 5000;
        public static int WAIT_USERLOGIN_TIME       = 5000;
        
        // If you use an Epoc Flex headset, then you must put your configuration here
        // TODO: need detail here
        public static string FlexMapping = @"{
                                  'CMS':'TP8', 'DRL':'P6',
                                  'RM':'TP10','RN':'P4','RO':'P8'}";
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
        public const int HeadsetWrongInformation  = 100;
        public const int HeadsetCannotConnected   = 101;
        public const int HeadsetConnectingTimeout = 102;
        public const int HeadsetDataTimeOut       = 103;
        public const int HeadsetConnected         = 104;
        public const int BTLEPermissionNotGranted = 31;
        public const int HeadsetScanFinished      = 142;
    }

    public static class DevStreamParams
    {
        public const string battery = "Battery";
        public const string signal = "Signal";
    }
}
