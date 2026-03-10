using System;
using Emotiv.Cortex.Models;
using EmotivUnityPlugin; // TODO (Tung Nguyen): remove this line when move all old code to new SDK
using UnityEngine;

namespace Emotiv.Cortex.Service
{
    public class EmotivCortexRuntime : IEmotivCortexRuntime
    {
        private readonly CortexRuntimeContext _context;
        private CortexClient _client;
        private IAuthService _auth;
        private IHeadsetService _headset;
        private ISimpleBCIService _simpleBCI;
        
        public EmotivCortexRuntime()
        {
            _context = new CortexRuntimeContext();
            _client = CortexClient.Instance;

            InitConfigFromAppConfig();

            object context = null;
        #if UNITY_ANDROID
            using (var unityPlayer = new UnityEngine.AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                context = unityPlayer.GetStatic<UnityEngine.AndroidJavaObject>("currentActivity");
            }
        #endif
            _client.Init(context);
            _client.CortexConnectionStared += OnCortexConnectionStared;
            _client.Open();
        }
        
        private void OnCortexConnectionStared(object sender, bool isConnected)
        {
            _client.CortexConnectionStared -= OnCortexConnectionStared;
            _context.SetConnectionStatus(isConnected);
            
            if (!isConnected)
            {
                throw new InvalidOperationException("Cortex connection failed.");
            }
            UnityEngine.Debug.Log("EmotivCortexRuntime: Cortex connection established.");
        }
        public IAuthService Auth
            => _auth ??= new AuthService(_context, _client);
        
        public IHeadsetService Headset
            => _headset ??= new HeadsetService(_context, _client);

        public ISimpleBCIService SimpleBCI
            => _simpleBCI ??= new SimpleBCIService(_context, _client);
            
        public void Dispose()
        {
            _client.CortexConnectionStared -= OnCortexConnectionStared;
        }

        private void InitConfigFromAppConfig()
        {
            string appUrl = "wss://localhost:6868";
#if !USE_EMBEDDED_LIB && !UNITY_ANDROID && !UNITY_IOS
            if (!string.IsNullOrEmpty(AppConfig.AppUrl))
            {
                appUrl = AppConfig.AppUrl;
            }
#endif

            Config.Init(
                AppConfig.ClientId,
                AppConfig.ClientSecret,
                AppConfig.AppName,
                AppConfig.AllowSaveLogToFile,
                appUrl,
                "",
                ""
            );
            MyLogger.Instance.Init(AppConfig.AppName, AppConfig.AllowSaveLogToFile);

        }
    }
}