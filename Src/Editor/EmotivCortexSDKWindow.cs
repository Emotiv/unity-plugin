using UnityEditor;
using UnityEngine;

namespace EmotivUnityPlugin
{
    /// <summary>
    /// Emotiv Cortex SDK Editor Window for managing plugin configuration
    /// </summary>
    public class EmotivCortexSDKWindow : EditorWindow
    {
        private const string DEFAULT_APP_URL_PRODUCTION = "wss://localhost:6868";
        private const string DEFAULT_APP_URL_DEVELOPMENT = "wss://localhost:7070";
        private string GetAppName()
        {
            return Application.productName;
        }
        private int selectedTab = 0;
        private readonly string[] tabs = { "Config", "Help", "About" };
        
        private Vector2 scrollPosition;
        
        // Configuration fields - Only show essential fields in UI
        private string clientId = "";
        private string clientSecret = "";
        private bool useEmbeddedLib = false;
        
        // Hidden fields with defaults (not shown in UI, but can be set via command line)
        // App name is now always Application.productName
        private string providerName = "";
         // Default provider is empty, can be set via command line or UI
        private bool allowSaveLogToFile = true;
        private bool useDefaultEmotivLogHandler = false; // Default is false (custom log handler)
        private string appUrl = DEFAULT_APP_URL_PRODUCTION; // Default production
        private bool isDataBufferUsing = true; // Default true, can be changed in EmotivUnityItf
        private bool isProductionEnvironment = true; // Default production
        
        private const string PREFS_CLIENT_ID = "EmotivCortex_ClientId";
        private const string PREFS_CLIENT_SECRET = "EmotivCortex_ClientSecret";
        private const string PREFS_APP_NAME = "EmotivCortex_AppName";
        private const string PREFS_PROVIDER_NAME = "EmotivCortex_ProviderName";
        private const string PREFS_ALLOW_SAVE_LOG = "EmotivCortex_AllowSaveLogToFile";
        private const string PREFS_APP_URL = "EmotivCortex_AppUrl";
        private const string PREFS_IS_DATA_BUFFER_USING = "EmotivCortex_IsDataBufferUsing";
        private const string PREFS_USE_EMBEDDED_LIB = "EmotivCortex_UseEmbeddedLib";
        private const string PREFS_IS_PRODUCTION_ENV = "EmotivCortex_IsProductionEnvironment";

        [MenuItem("Tools/Emotiv Cortex SDK")]
        public static void ShowWindow()
        {
            EmotivCortexSDKWindow window = GetWindow<EmotivCortexSDKWindow>("Emotiv Cortex SDK");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private void OnEnable()
        {
            LoadConfiguration();
        }

        private void OnGUI()
        {
            // Draw toolbar
            selectedTab = GUILayout.Toolbar(selectedTab, tabs);
            
            EditorGUILayout.Space(10);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            switch (selectedTab)
            {
                case 0:
                    DrawConfigTab();
                    break;
                case 1:
                    DrawHelpTab();
                    break;
                case 2:
                    DrawAboutTab();
                    break;
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawConfigTab()
        {
            EditorGUILayout.LabelField("Emotiv Cortex SDK Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.HelpBox("Configure your Emotiv Cortex SDK settings. These values will be saved and can be accessed at runtime.", MessageType.Info);
            EditorGUILayout.Space(10);
            

            EditorGUILayout.LabelField("Credentials", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "You can obtain these credentials after registering your App ID with the Cortex SDK for development.",
                MessageType.Info);
            if (GUILayout.Button("For instructions, visit: https://emotiv.gitbook.io/cortex-api#create-a-cortex-app", GUILayout.Height(22)))
            {
                Application.OpenURL("https://emotiv.gitbook.io/cortex-api#create-a-cortex-app");
            }
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Client ID", EditorStyles.label);
            clientId = EditorGUILayout.TextField(clientId);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Client Secret", EditorStyles.label);
            clientSecret = EditorGUILayout.PasswordField(clientSecret);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
            

            // Embedded Lib option removed from UI. Set via command line or manually in code only.
            EditorGUILayout.HelpBox("Embedded Library option is only configurable via command line or manually in code. Default: Disabled.", MessageType.Info);
            EditorGUILayout.Space(10);

            // Use Default Emotiv Log Handler
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Use Default Emotiv Log Handler", GUILayout.Width(190)); // Adjust width as needed
            GUILayout.Space(20); // Adds space between label and toggle
            useDefaultEmotivLogHandler = GUILayout.Toggle(useDefaultEmotivLogHandler, "");
            EditorGUILayout.EndHorizontal();
            if (useDefaultEmotivLogHandler)
            {
                EditorGUILayout.HelpBox("Enable this to use the default Emotiv log handler.", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("Disable to customize your own log handler. Reference to DefaultEmotivLogHandler.cs for implementation details.", MessageType.Info);
            }
            
            EditorGUILayout.Space(10);

            // Information about hidden defaults
            EditorGUILayout.LabelField("Default Settings (Hidden)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "• Environment: Production\n" +
                "• Data Buffer: Enabled\n" +
                "To change these settings, use command line arguments or modify EmotivUnityItf.cs directly.\n" +
                "For development environment use -emotivIsProduction false",
                MessageType.Info);
            EditorGUILayout.Space(10);
            
            // Save button
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Save Configuration", GUILayout.Width(150), GUILayout.Height(30)))
            {
                SaveConfiguration();
                EditorUtility.DisplayDialog("Success", "Configuration saved successfully!", "OK");
            }
            
            if (GUILayout.Button("Reset to Default", GUILayout.Width(150), GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Reset Configuration", 
                    "Are you sure you want to reset all configuration values to default?", 
                    "Yes", "No"))
                {
                    ResetConfiguration();
                }
            }
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // Display current scripting define symbols
            EditorGUILayout.LabelField("Scripting Define Symbols", EditorStyles.boldLabel);
            BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            string defines = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
            EditorGUILayout.HelpBox($"Current: {(string.IsNullOrEmpty(defines) ? "None" : defines)}", MessageType.None);
            
            if (useEmbeddedLib && !defines.Contains("USE_EMBEDDED_LIB"))
            {
                EditorGUILayout.HelpBox("USE_EMBEDDED_LIB is not defined in Scripting Define Symbols. It will be added when you save.", MessageType.Warning);
            }
            else if (!useEmbeddedLib && defines.Contains("USE_EMBEDDED_LIB"))
            {
                EditorGUILayout.HelpBox("USE_EMBEDDED_LIB is defined in Scripting Define Symbols. It will be removed when you save.", MessageType.Warning);
            }
            
            // Check server environment defines (mutually exclusive) - production is default
            if (!isProductionEnvironment)
            {
                if (!defines.Contains("DEV_SERVER"))
                {
                    EditorGUILayout.HelpBox("DEV_SERVER is not defined. It will be added when you save.", MessageType.Warning);
                }
                if (defines.Contains("PRODUCT_SERVER"))
                {
                    EditorGUILayout.HelpBox("PRODUCT_SERVER is defined. It will be removed when you save (Development mode).", MessageType.Warning);
                }
            }
            else
            {
                if (!defines.Contains("PRODUCT_SERVER"))
                {
                    EditorGUILayout.HelpBox("PRODUCT_SERVER is not defined. It will be added when you save.", MessageType.Warning);
                }
                if (defines.Contains("DEV_SERVER"))
                {
                    EditorGUILayout.HelpBox("DEV_SERVER is defined. It will be removed when you save (Production mode).", MessageType.Warning);
                }
            }
        }

        private void DrawHelpTab()
        {
            EditorGUILayout.LabelField("Help", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);
            
            EditorGUILayout.HelpBox("Help documentation is under development.", MessageType.Info);
            EditorGUILayout.Space(10);
            

            EditorGUILayout.LabelField("Quick Start Guide", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("1. Configure your Client ID and Client Secret in the Config tab.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("2. Choose appropriate options for your application.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("3. Save the configuration.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Resources", EditorStyles.boldLabel);
            if (GUILayout.Button("Emotiv Developer Portal", GUILayout.Height(30)))
            {
                Application.OpenURL("https://www.emotiv.com/developer/");
            }
            if (GUILayout.Button("Cortex API Documentation", GUILayout.Height(30)))
            {
                Application.OpenURL("https://emotiv.gitbook.io/cortex-api/");
            }
            if (GUILayout.Button("Unity Plugin GitHub", GUILayout.Height(30)))
            {
                Application.OpenURL("https://github.com/Emotiv/unity-plugin");
            }
        }

        private void DrawAboutTab()
        {
            EditorGUILayout.LabelField("About", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);
            
            EditorGUILayout.HelpBox("Emotiv Cortex SDK for Unity\n\nVersion: 1.0.0", MessageType.Info);
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Description", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("The Emotiv Cortex SDK enables Unity applications to work with the Emotiv Cortex for EEG headset integration, data streaming, training, and recording.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Platform Support", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "For the latest compatibility details, visit:",
                MessageType.Info);
            if (GUILayout.Button("https://github.com/Emotiv/unity-plugin?tab=readme-ov-file#overview", GUILayout.Height(22)))
            {
                Application.OpenURL("https://github.com/Emotiv/unity-plugin?tab=readme-ov-file#overview");
            }
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("License", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("MIT License", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Copyright © 2024 Emotiv Inc.", EditorStyles.wordWrappedLabel);
        }

        private void LoadConfiguration()
        {
            clientId = EditorPrefs.GetString(PREFS_CLIENT_ID, "");
            clientSecret = EditorPrefs.GetString(PREFS_CLIENT_SECRET, "");
            // App name is always Application.productName
            providerName = EditorPrefs.GetString(PREFS_PROVIDER_NAME, "");
            allowSaveLogToFile = EditorPrefs.GetBool(PREFS_ALLOW_SAVE_LOG, true);
            appUrl = EditorPrefs.GetString(PREFS_APP_URL, DEFAULT_APP_URL_PRODUCTION);
            isDataBufferUsing = EditorPrefs.GetBool(PREFS_IS_DATA_BUFFER_USING, true);
            useEmbeddedLib = EditorPrefs.GetBool(PREFS_USE_EMBEDDED_LIB, false);
            isProductionEnvironment = EditorPrefs.GetBool(PREFS_IS_PRODUCTION_ENV, true);
            
            // Set appUrl based on environment if not explicitly set
            if (string.IsNullOrEmpty(appUrl) || appUrl == DEFAULT_APP_URL_PRODUCTION || appUrl == DEFAULT_APP_URL_DEVELOPMENT)
            {
                appUrl = isProductionEnvironment ? DEFAULT_APP_URL_PRODUCTION : DEFAULT_APP_URL_DEVELOPMENT;
            }
        }

        private void SaveConfiguration()
        {
            // Set appUrl based on environment
            appUrl = isProductionEnvironment ? DEFAULT_APP_URL_PRODUCTION : DEFAULT_APP_URL_DEVELOPMENT;
            
            // Save to EditorPrefs (for Editor use)
            EditorPrefs.SetString(PREFS_CLIENT_ID, clientId);
            EditorPrefs.SetString(PREFS_CLIENT_SECRET, clientSecret);
            // App name is always Application.productName
            EditorPrefs.SetString(PREFS_PROVIDER_NAME, providerName);
            EditorPrefs.SetBool(PREFS_ALLOW_SAVE_LOG, allowSaveLogToFile);
            EditorPrefs.SetString(PREFS_APP_URL, appUrl);
            EditorPrefs.SetBool(PREFS_IS_DATA_BUFFER_USING, isDataBufferUsing);
            EditorPrefs.SetBool(PREFS_USE_EMBEDDED_LIB, useEmbeddedLib);
            EditorPrefs.SetBool(PREFS_IS_PRODUCTION_ENV, isProductionEnvironment);
            
            // Also save to PlayerPrefs (for runtime/standalone builds)
            PlayerPrefs.SetString(PREFS_CLIENT_ID, clientId);
            PlayerPrefs.SetString(PREFS_CLIENT_SECRET, clientSecret);
            PlayerPrefs.SetString(PREFS_APP_NAME, GetAppName());
            PlayerPrefs.SetString(PREFS_PROVIDER_NAME, providerName);
            PlayerPrefs.SetInt(PREFS_ALLOW_SAVE_LOG, allowSaveLogToFile ? 1 : 0);
            PlayerPrefs.SetString(PREFS_APP_URL, appUrl);
            PlayerPrefs.SetInt(PREFS_IS_DATA_BUFFER_USING, isDataBufferUsing ? 1 : 0);
            PlayerPrefs.SetInt(PREFS_USE_EMBEDDED_LIB, useEmbeddedLib ? 1 : 0);
            PlayerPrefs.SetInt(PREFS_IS_PRODUCTION_ENV, isProductionEnvironment ? 1 : 0);
            PlayerPrefs.Save();
            
            // Save to ScriptableObject (for build inclusion)
            SaveToScriptableObject();
            
            // Update scripting define symbols based on useEmbeddedLib and isProductionEnvironment
            UpdateScriptingDefineSymbols();
            
            Debug.Log("Emotiv Cortex SDK configuration saved to EditorPrefs, PlayerPrefs, and Resources.");
        }

        private void SaveToScriptableObject()
        {
            string resourcesPath = "Assets/Plugins/Emotiv-Unity-Plugin/Resources";
            string assetPath = resourcesPath + "/EmotivCortexSettings.asset";
            
            // Create Resources folder if it doesn't exist
            if (!System.IO.Directory.Exists(resourcesPath))
            {
                System.IO.Directory.CreateDirectory(resourcesPath);
                AssetDatabase.Refresh();
            }
            
            // Load or create the ScriptableObject
            EmotivCortexSettings settings = AssetDatabase.LoadAssetAtPath<EmotivCortexSettings>(assetPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<EmotivCortexSettings>();
                AssetDatabase.CreateAsset(settings, assetPath);
            }
            
            // Update settings
            settings.clientId = clientId;
            settings.clientSecret = clientSecret;
            settings.appName = GetAppName();
            settings.providerName = providerName;
            settings.appUrl = appUrl;
            settings.allowSaveLogToFile = allowSaveLogToFile;
            settings.isDataBufferUsing = isDataBufferUsing;
            settings.useEmbeddedLib = useEmbeddedLib;
            settings.isProductionEnvironment = isProductionEnvironment;
            
            // Mark as dirty and save
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void ResetConfiguration()
        {
            clientId = "";
            clientSecret = "";
            // App name is always Application.productName
            providerName = "";
            allowSaveLogToFile = true;
            appUrl = DEFAULT_APP_URL_PRODUCTION;
            isDataBufferUsing = true;
            useEmbeddedLib = false;
            isProductionEnvironment = true;
            
            SaveConfiguration();
            Debug.Log("Emotiv Cortex SDK configuration reset to default values.");
        }

        private void UpdateScriptingDefineSymbols()
        {
            BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            string defines = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
            
            // Handle USE_EMBEDDED_LIB
            if (useEmbeddedLib)
            {
                // Add USE_EMBEDDED_LIB if not present
                if (!defines.Contains("USE_EMBEDDED_LIB"))
                {
                    if (!string.IsNullOrEmpty(defines))
                    {
                        defines += ";USE_EMBEDDED_LIB";
                    }
                    else
                    {
                        defines = "USE_EMBEDDED_LIB";
                    }
                    Debug.Log("Added USE_EMBEDDED_LIB to Scripting Define Symbols.");
                }
            }
            else
            {
                // Remove USE_EMBEDDED_LIB if present
                if (defines.Contains("USE_EMBEDDED_LIB"))
                {
                    defines = defines.Replace("USE_EMBEDDED_LIB;", "").Replace(";USE_EMBEDDED_LIB", "").Replace("USE_EMBEDDED_LIB", "");
                    Debug.Log("Removed USE_EMBEDDED_LIB from Scripting Define Symbols.");
                }
            }
            
            // Handle DEV_SERVER and PRODUCT_SERVER - only one should be defined
            if (!isProductionEnvironment)
            {
                // Development mode: Add DEV_SERVER, remove PRODUCT_SERVER
                if (!defines.Contains("DEV_SERVER"))
                {
                    if (!string.IsNullOrEmpty(defines))
                    {
                        defines += ";DEV_SERVER";
                    }
                    else
                    {
                        defines = "DEV_SERVER";
                    }
                    Debug.Log("Added DEV_SERVER to Scripting Define Symbols (Development mode).");
                }
                // Remove PRODUCT_SERVER if present
                if (defines.Contains("PRODUCT_SERVER"))
                {
                    defines = defines.Replace("PRODUCT_SERVER;", "").Replace(";PRODUCT_SERVER", "").Replace("PRODUCT_SERVER", "");
                    Debug.Log("Removed PRODUCT_SERVER from Scripting Define Symbols.");
                }
            }
            else
            {
                // Production mode: Add PRODUCT_SERVER, remove DEV_SERVER
                if (!defines.Contains("PRODUCT_SERVER"))
                {
                    if (!string.IsNullOrEmpty(defines))
                    {
                        defines += ";PRODUCT_SERVER";
                    }
                    else
                    {
                        defines = "PRODUCT_SERVER";
                    }
                    Debug.Log("Added PRODUCT_SERVER to Scripting Define Symbols (Production mode).");
                }
                // Remove DEV_SERVER if present
                if (defines.Contains("DEV_SERVER"))
                {
                    defines = defines.Replace("DEV_SERVER;", "").Replace(";DEV_SERVER", "").Replace("DEV_SERVER", "");
                    Debug.Log("Removed DEV_SERVER from Scripting Define Symbols.");
                }
            }
            
            // Apply changes
            PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, defines);
        }

        /// <summary>
        /// Get configuration at runtime (can be called from other scripts)
        /// </summary>
        public static EmotivCortexConfig GetRuntimeConfiguration()
        {
            return new EmotivCortexConfig
            {
                ClientId = EditorPrefs.GetString(PREFS_CLIENT_ID, ""),
                ClientSecret = EditorPrefs.GetString(PREFS_CLIENT_SECRET, ""),
                AppName = EditorPrefs.GetString(PREFS_APP_NAME, ""),
                ProviderName = EditorPrefs.GetString(PREFS_PROVIDER_NAME, ""),
                AllowSaveLogToFile = EditorPrefs.GetBool(PREFS_ALLOW_SAVE_LOG, true),
                AppUrl = EditorPrefs.GetString(PREFS_APP_URL, ""),
                IsDataBufferUsing = EditorPrefs.GetBool(PREFS_IS_DATA_BUFFER_USING, true),
                UseEmbeddedLib = EditorPrefs.GetBool(PREFS_USE_EMBEDDED_LIB, false),
                IsProductionEnvironment = EditorPrefs.GetBool(PREFS_IS_PRODUCTION_ENV, true)
            };
        }

        /// <summary>
        /// Configure SDK from command line arguments (for CI/CD builds)
        /// Call via: -executeMethod EmotivUnityPlugin.Editor.EmotivCortexSDKWindow.ConfigureFromCommandLine
        /// Arguments:
        ///   -emotivClientId <value>
        ///   -emotivClientSecret <value>
        ///   -emotivAppName <value>
        ///   -emotivProviderName <value>
        ///   -emotivAppUrl <value>
        ///   -emotivAppVersion <value>
        ///   -emotivUseEmbedded <true/false>
        ///   -emotivAllowSaveLog <true/false>
        ///   -emotivUseDataBuffer <true/false>
        ///   -emotivIsProduction <true/false> (default: true)
        /// </summary>
        public static void ConfigureFromCommandLine()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            
            string clientId = GetArgValue(args, "-emotivClientId");
            string clientSecret = GetArgValue(args, "-emotivClientSecret");
            string appName = GetArgValue(args, "-emotivAppName");
            string providerName = GetArgValue(args, "-emotivProviderName");
            string appUrl = GetArgValue(args, "-emotivAppUrl");
            string appVersion = GetArgValue(args, "-emotivAppVersion");
            string useEmbeddedStr = GetArgValue(args, "-emotivUseEmbedded");
            string allowSaveLogStr = GetArgValue(args, "-emotivAllowSaveLog");
            string useDataBufferStr = GetArgValue(args, "-emotivUseDataBuffer");
            string isProductionStr = GetArgValue(args, "-emotivIsProduction");
            
            bool configUpdated = false;
            
            if (!string.IsNullOrEmpty(clientId))
            {
                EditorPrefs.SetString(PREFS_CLIENT_ID, clientId);
                Debug.Log($"[Emotiv Config] Set ClientId: {clientId}");
                configUpdated = true;
            }
            
            if (!string.IsNullOrEmpty(clientSecret))
            {
                EditorPrefs.SetString(PREFS_CLIENT_SECRET, clientSecret);
                Debug.Log("[Emotiv Config] Set ClientSecret: [HIDDEN]");
                configUpdated = true;
            }
            
            if (!string.IsNullOrEmpty(appName))
            {
                EditorPrefs.SetString(PREFS_APP_NAME, appName);
                Debug.Log($"[Emotiv Config] Set AppName: {appName}");
                configUpdated = true;
            }
            
            if (!string.IsNullOrEmpty(providerName))
            {
                EditorPrefs.SetString(PREFS_PROVIDER_NAME, providerName);
                Debug.Log($"[Emotiv Config] Set ProviderName: {providerName}");
                configUpdated = true;
            }
            
            if (!string.IsNullOrEmpty(appUrl))
            {
                EditorPrefs.SetString(PREFS_APP_URL, appUrl);
                Debug.Log($"[Emotiv Config] Set AppUrl: {appUrl}");
                configUpdated = true;
            }
            
            if (!string.IsNullOrEmpty(appVersion))
            {
                PlayerSettings.bundleVersion = appVersion;
                Debug.Log($"[Emotiv Config] Set App Version: {appVersion}");
                configUpdated = true;
            }
            
            if (!string.IsNullOrEmpty(allowSaveLogStr))
            {
                bool allowSaveLog = allowSaveLogStr.ToLower() == "true";
                EditorPrefs.SetBool(PREFS_ALLOW_SAVE_LOG, allowSaveLog);
                Debug.Log($"[Emotiv Config] Set AllowSaveLogToFile: {allowSaveLog}");
                configUpdated = true;
            }
            
            if (!string.IsNullOrEmpty(useDataBufferStr))
            {
                bool useDataBuffer = useDataBufferStr.ToLower() == "true";
                EditorPrefs.SetBool(PREFS_IS_DATA_BUFFER_USING, useDataBuffer);
                Debug.Log($"[Emotiv Config] Set UseDataBuffer: {useDataBuffer}");
                configUpdated = true;
            }
            
            if (!string.IsNullOrEmpty(useEmbeddedStr))
            {
                bool useEmbedded = useEmbeddedStr.ToLower() == "true";
                EditorPrefs.SetBool(PREFS_USE_EMBEDDED_LIB, useEmbedded);
                
                // Update scripting define symbols for current build target
                BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
                var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
                string defines = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
                
                if (useEmbedded && !defines.Contains("USE_EMBEDDED_LIB"))
                {
                    defines += string.IsNullOrEmpty(defines) ? "USE_EMBEDDED_LIB" : ";USE_EMBEDDED_LIB";
                    PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, defines);
                    Debug.Log("[Emotiv Config] Added USE_EMBEDDED_LIB to Scripting Define Symbols.");
                }
                else if (!useEmbedded && defines.Contains("USE_EMBEDDED_LIB"))
                {
                    defines = defines.Replace("USE_EMBEDDED_LIB;", "").Replace(";USE_EMBEDDED_LIB", "").Replace("USE_EMBEDDED_LIB", "");
                    PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, defines);
                    Debug.Log("[Emotiv Config] Removed USE_EMBEDDED_LIB from Scripting Define Symbols.");
                }
                
                Debug.Log($"[Emotiv Config] Set UseEmbeddedLib: {useEmbedded}");
                configUpdated = true;
            }
            
            if (!string.IsNullOrEmpty(isProductionStr))
            {
                bool isProduction = isProductionStr.ToLower() == "true";
                EditorPrefs.SetBool(PREFS_IS_PRODUCTION_ENV, isProduction);
                
                // Set appUrl based on environment if not explicitly provided
                if (string.IsNullOrEmpty(appUrl))
                {
                    string autoAppUrl = isProduction ? DEFAULT_APP_URL_PRODUCTION : DEFAULT_APP_URL_DEVELOPMENT;
                    EditorPrefs.SetString(PREFS_APP_URL, autoAppUrl);
                    Debug.Log($"[Emotiv Config] Auto-set AppUrl based on environment: {autoAppUrl}");
                }
                
                // Update scripting define symbols for current build target
                BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
                var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
                string defines = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
                
                if (!isProduction && !defines.Contains("DEV_SERVER"))
                {
                    defines += string.IsNullOrEmpty(defines) ? "DEV_SERVER" : ";DEV_SERVER";
                    PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, defines);
                    Debug.Log("[Emotiv Config] Added DEV_SERVER to Scripting Define Symbols (Development mode).");
                }
                else if (isProduction && defines.Contains("DEV_SERVER"))
                {
                    defines = defines.Replace("DEV_SERVER;", "").Replace(";DEV_SERVER", "").Replace("DEV_SERVER", "");
                    PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, defines);
                    Debug.Log("[Emotiv Config] Removed DEV_SERVER from Scripting Define Symbols (Production mode).");
                }
                
                Debug.Log($"[Emotiv Config] Set IsProductionEnvironment: {isProduction}");
                configUpdated = true;
            }
            
            if (configUpdated)
            {
                Debug.Log("[Emotiv Config] Configuration completed successfully from command line arguments.");
            }
            else
            {
                Debug.LogWarning("[Emotiv Config] No configuration arguments provided. Use -emotivClientId, -emotivClientSecret, etc.");
            }
        }

        /// <summary>
        /// Helper method to get command line argument value
        /// </summary>
        private static string GetArgValue(string[] args, string argName)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i].Equals(argName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }
            return null;
        }
    }
}
