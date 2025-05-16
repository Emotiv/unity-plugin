using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace EmotivUnityPlugin
{
    public static class Utils
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

        public static string GetAppTmpPath(string providerName, string appName)
        {
            string homePath = "";
        #if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            homePath = Environment.GetEnvironmentVariable("LocalAppData");
        #elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            homePath = Environment.GetEnvironmentVariable("HOME");
            string currentTempPath = Path.Combine(homePath, "Library/Application Support");
            homePath = currentTempPath;
        #elif UNITY_STANDALONE_LINUX
            homePath = Environment.GetEnvironmentVariable("HOME");
        #elif UNITY_IOS
            homePath = Application.persistentDataPath;
        #elif UNITY_ANDROID
            // return application data path on android
            return Application.persistentDataPath;
        #else
            homePath = Directory.GetCurrentDirectory();
        #endif
            string targetFolderName = appName;
            if (!string.IsNullOrEmpty(providerName))
                targetFolderName = Path.Combine(providerName, appName);
            
            return Path.Combine(homePath, targetFolderName);
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

        public static HeadsetFamily GetHeadsetGroup(HeadsetTypes headsetType)
        {
            if (headsetType == HeadsetTypes.HEADSET_TYPE_INSIGHT || headsetType == HeadsetTypes.HEADSET_TYPE_INSIGHT2)
                return HeadsetFamily.INSIGHT;
            else if (headsetType == HeadsetTypes.HEADSET_TYPE_MN8 || headsetType == HeadsetTypes.HEADSET_TYPE_MW20)
                return HeadsetFamily.MN8;
            else
                return HeadsetFamily.EPOC;
        }

        public static bool IsInsightType(HeadsetTypes headsetType)
        {
            if (headsetType == HeadsetTypes.HEADSET_TYPE_INSIGHT || headsetType == HeadsetTypes.HEADSET_TYPE_INSIGHT2)
                return true;
            else
                return false;
        }
        
        public static TimeSpan IndexToTime(int index)
        {
            if (index < 0 || index >= 48)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index must be between 0 and 47.");
            }

            int hours = index / 2;
            int minutes = (index % 2) * 30;

            return new TimeSpan(hours, minutes, 0);
        }
    }
}
