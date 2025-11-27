using UnityEngine;

namespace EmotivUnityPlugin
{
    /// <summary>
    /// Configuration data structure for Emotiv Cortex SDK
    /// </summary>
    public class EmotivCortexConfig
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string AppName { get; set; }
        public string ProviderName { get; set; }
        public bool AllowSaveLogToFile { get; set; }
        public string AppUrl { get; set; }
        public bool IsDataBufferUsing { get; set; }
        public bool UseEmbeddedLib { get; set; }
        public bool IsProductionEnvironment { get; set; }
        public bool UseDefaultEmotivLogHandler { get; set; }
    }
    
    /// <summary>
    /// Runtime configuration reader that works in both Editor and standalone builds
    /// </summary>
    public static class EmotivCortexConfigReader
    {
        private const string PREFS_CLIENT_ID = "EmotivCortex_ClientId";
        private const string PREFS_CLIENT_SECRET = "EmotivCortex_ClientSecret";
        private const string PREFS_APP_NAME = "EmotivCortex_AppName";
        private const string PREFS_PROVIDER_NAME = "EmotivCortex_ProviderName";
        private const string PREFS_ALLOW_SAVE_LOG = "EmotivCortex_AllowSaveLogToFile";
        private const string PREFS_APP_URL = "EmotivCortex_AppUrl";
        private const string PREFS_IS_DATA_BUFFER_USING = "EmotivCortex_IsDataBufferUsing";
        private const string PREFS_USE_EMBEDDED_LIB = "EmotivCortex_UseEmbeddedLib";
        private const string PREFS_IS_PRODUCTION_ENV = "EmotivCortex_IsProductionEnvironment";
        
        /// <summary>
        /// Get configuration from Resources (primary) or PlayerPrefs (fallback)
        /// Configuration must be set via EmotivCortexSDKWindow before building
        /// </summary>
        public static EmotivCortexConfig GetConfiguration()
        {
            // Try to load from ScriptableObject in Resources first (works in builds)
            EmotivCortexSettings settings = EmotivCortexSettings.Load();
            
            // If ScriptableObject has valid data, use it
            if (settings != null && !string.IsNullOrEmpty(settings.clientId))
            {
                return settings.ToConfig();
            }
            
            // Fallback to PlayerPrefs (for Editor or if settings not found)
            return new EmotivCortexConfig
            {
                ClientId = PlayerPrefs.GetString(PREFS_CLIENT_ID, ""),
                ClientSecret = PlayerPrefs.GetString(PREFS_CLIENT_SECRET, ""),
                AppName = PlayerPrefs.GetString(PREFS_APP_NAME, ""),
                ProviderName = PlayerPrefs.GetString(PREFS_PROVIDER_NAME, ""),
                AllowSaveLogToFile = PlayerPrefs.GetInt(PREFS_ALLOW_SAVE_LOG, 1) == 1,
                AppUrl = PlayerPrefs.GetString(PREFS_APP_URL, "wss://localhost:6868"),
                IsDataBufferUsing = PlayerPrefs.GetInt(PREFS_IS_DATA_BUFFER_USING, 1) == 1,
                UseEmbeddedLib = PlayerPrefs.GetInt(PREFS_USE_EMBEDDED_LIB, 0) == 1,
                IsProductionEnvironment = PlayerPrefs.GetInt(PREFS_IS_PRODUCTION_ENV, 1) == 1,
                UseDefaultEmotivLogHandler = PlayerPrefs.GetInt("EmotivCortex_UseDefaultEmotivLogHandler", 1) == 1,
            };
        }
    }
}
