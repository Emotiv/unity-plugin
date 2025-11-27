using UnityEngine;

namespace EmotivUnityPlugin
{
    /// <summary>
    /// ScriptableObject for storing Emotiv Cortex SDK configuration.
    /// This allows configuration to be included in builds via the Resources folder.
    /// </summary>
    [CreateAssetMenu(fileName = "EmotivCortexSettings", menuName = "Emotiv/Cortex Settings")]
    public class EmotivCortexSettings : ScriptableObject
    {
        public string clientId = "";
        public string clientSecret = "";
        public string appName = "";
        public string providerName = "";
        public string appUrl = "wss://localhost:6868";
        public bool allowSaveLogToFile = true;
        public bool isDataBufferUsing = true;
        public bool useEmbeddedLib = false;
        public bool isProductionEnvironment = true;

        /// <summary>
        /// Load settings from Resources folder
        /// </summary>
        public static EmotivCortexSettings Load()
        {
            return Resources.Load<EmotivCortexSettings>("EmotivCortexSettings");
        }

        /// <summary>
        /// Convert settings to EmotivCortexConfig object
        /// </summary>
        public EmotivCortexConfig ToConfig()
        {
            return new EmotivCortexConfig
            {
                ClientId = this.clientId,
                ClientSecret = this.clientSecret,
                AppName = this.appName,
                ProviderName = this.providerName,
                AppUrl = this.appUrl,
                AllowSaveLogToFile = this.allowSaveLogToFile,
                IsDataBufferUsing = this.isDataBufferUsing,
                UseEmbeddedLib = this.useEmbeddedLib,
                IsProductionEnvironment = this.isProductionEnvironment
            };
        }
    }
}
