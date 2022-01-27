using System;
using System.IO;
using System.Text;

namespace EmotivUnityPlugin
{
    public class Utils
    {
        
        public static Int64 GetEpochTimeNow()
        {
            TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            Int64 timeSinceEpoch = (Int64)t.TotalMilliseconds;
            return timeSinceEpoch;

        }
        public static string GenerateUuidProfileName(string prefix)
        {
            return prefix + "-" + GetEpochTimeNow();
        }

        // keep some tmp data of Unity App
        public static string GetAppTmpPath() 
        {
            string homePath = "";
        #if UNITY_STANDALONE_WIN
            homePath = Environment.GetEnvironmentVariable("LocalAppData");
        #elif UNITY_STANDALONE_OSX
            homePath = Environment.GetEnvironmentVariable("HOME");
            string currentTempPath = Path.Combine(homePath, "Library/Application Support");
            homePath = currentTempPath;
        #elif UNITY_STANDALONE_LINUX
            homePath = Environment.GetEnvironmentVariable("HOME");
            // TODO
        #elif UNITY_IOS
            // TODO
        #elif UNITY_ANDROID
            // TODO
        #else
            // TODO
            homePath = Directory.GetCurrentDirectory();
        #endif
            string targetPath = Path.Combine(homePath, Config.TmpAppDataDir);
            return targetPath;
        }

        public static string GetLogPath() 
        {
            string tmpPath = GetAppTmpPath();
            string targetDir = Path.Combine(tmpPath, Config.LogsDir);
            if (!Directory.Exists(targetDir)) {
                try
                {
                    // create directory
                    Directory.CreateDirectory(targetDir);
                    UnityEngine.Debug.Log("GetLogPath: create directory " + targetDir);
                }
                catch (Exception e)
                {      
                    UnityEngine.Debug.Log("Can not create directory: " + targetDir + " : failed: " + e.ToString());
                }
                finally {}
            }
            return targetDir;
        }

        public static DateTime StringToIsoDateTime(string time) {
            // UnityEngine.Debug.Log(" StringToIsoDateTime: " + time);
            return DateTime.Parse(time);
        }
        public static string ISODateTimeToString(DateTime isoTime) {
            if (isoTime.CompareTo(new DateTime()) == 0)
                return "";
            return isoTime.ToString("yyyy-MM-ddTHH:mm:ss.ffffzzz");
        }

        public static double ISODateTimeToEpocTime(DateTime isoTime) {
            DateTime dt1970 = new DateTime(1970, 1, 1);
            TimeSpan span = isoTime - dt1970;
            return span.TotalMilliseconds;
        }

        public static bool CheckEmotivAppInstalled(string emotivAppsPath = "", bool isRequired = false) {

            if (!isRequired)
                return true;
            
            if (string.IsNullOrEmpty(emotivAppsPath)) {
                UnityEngine.Debug.Log("The emotivAppsPath is empty. So will not check emotiv installed or not.");
                return true;
            }
            else if (!Directory.Exists(emotivAppsPath)) {
                UnityEngine.Debug.Log("The emotivApps directory is not existed.");
                return false;
            }
            
        
        #if UNITY_STANDALONE_WIN
            string emotivAppName     =   "EMOTIV Launcher.exe";
            string fileDir = Path.Combine(emotivAppsPath, emotivAppName);
            if (File.Exists(fileDir)) {
                return true;
            }
            UnityEngine.Debug.Log("IsEmotivAppInstalled: not exists file: " + fileDir);
            return false;
        #elif UNITY_STANDALONE_OSX
            string emotivAppName     =  "EMOTIV Launcher.app";
            string appDir = Path.Combine(emotivAppsPath, emotivAppName);
            if (Directory.Exists(appDir)) {
                return true;
            }
            UnityEngine.Debug.Log("IsEmotivAppInstalled: not exists bundle file: " + appDir);
            return false;
        #elif UNITY_STANDALONE_LINUX
            string homePath = Environment.GetEnvironmentVariable("HOME");
            return true;
            // TODO
        #elif UNITY_IOS
            // TODO
            return true;
        #elif UNITY_ANDROID
            // TODO
            return true;
        #else
            // TODO
            return true;
        #endif
            
        }

        public static bool IsSameAppVersion(string appVersion){
            string rootPath = Utils.GetAppTmpPath();

            string targetDir = Path.Combine(rootPath, Config.ProfilesDir);
            if (!Directory.Exists(targetDir)){
                UnityEngine.Debug.Log("IsSameAppVersion: not exists directory " + targetDir);
                return false;
            }
            string fileDir = Path.Combine(targetDir, Config.TmpVersionFileName);
            if (!File.Exists(fileDir)) {
                UnityEngine.Debug.Log("IsSameAppVersion: not exists file " + fileDir);
                return false;
            }
            string savedAppVer = File.ReadAllText(fileDir);
            if (savedAppVer == appVersion) {
                return true;
            }
            else {
                return false;
            }
        }
        // save cortexToken to File
        public static void SaveAppVersion(string appVersion) {
            //get tmp Path of App 
            string rootPath = Utils.GetAppTmpPath();

            string targetDir = Path.Combine(rootPath, Config.ProfilesDir);
            if (!Directory.Exists(targetDir)) {
                try
                {
                    // create directory
                    Directory.CreateDirectory(targetDir);
                    UnityEngine.Debug.Log("SaveAppVersion: create directory " + targetDir);
                }
                catch (Exception e)
                {      
                    UnityEngine.Debug.Log("Can not create directory: " + targetDir + " : failed: " + e.ToString());
                    return;
                }
                finally {}
            }
            string fileDir = Path.Combine(targetDir, Config.TmpVersionFileName);

            using(var fileStream = new FileStream(fileDir, FileMode.Create)) {
                byte[] dataByte = new UTF8Encoding(true).GetBytes(appVersion);
                fileStream.Write(dataByte, 0, dataByte.Length);
            }
        }

        public static bool IsNumericType(object o)
        {   
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                return true;
                default:
                return false;
            }
        }

        public static bool IsInsightType(HeadsetTypes headsetType)
        {
            if (headsetType == HeadsetTypes.HEADSET_TYPE_INSIGHT || headsetType == HeadsetTypes.HEADSET_TYPE_INSIGHT2)
                return true;
            else
                return false;
        }
    }
}
