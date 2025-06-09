using System;
using System.Reflection;
using System.IO;
#if UNITY_STANDALONE_WIN && NET_4_6
using Microsoft.Win32; // For registry operations but only supported on Windows with .NET Framework version >= 4.6, not for .NET Standard
#endif
using UnityEngine;

namespace EmotivUnityPlugin
{
    public class RegistryConfig
    {
        public RegistryConfig(string uriScheme)
        {
            CustomUriScheme = uriScheme;
        }

        public void Configure()
        {
#if UNITY_STANDALONE_WIN && NET_4_6
            if (NeedToAddKeys()) AddRegKeys();
#else
            Debug.LogWarning("RegistryConfig is only supported on Windows and with .NET Framework version >= 4.6, not for .NET Standard");
#endif  
        }

        private string CustomUriScheme { get; }
#if UNITY_STANDALONE_WIN && NET_4_6
        string CustomUriSchemeKeyPath => RootKeyPath + @"\" + CustomUriScheme;
        string CustomUriSchemeKeyValueValue => "URL:" + CustomUriScheme;
        string CommandKeyPath => CustomUriSchemeKeyPath + @"\shell\open\command";

        const string RootKeyPath = @"Software\Classes";

        const string CustomUriSchemeKeyValueName = "";

        const string ShellKeyName = "shell";
        const string OpenKeyName = "open";
        const string CommandKeyName = "command";

        const string CommandKeyValueName = "";
        const string CommandKeyValueFormat = "\"{0}\\UnityExample.exe\" \"%1\"";
        static string CommandKeyValueValue => String.Format(CommandKeyValueFormat, Path.GetDirectoryName(Application.dataPath));

        const string UrlProtocolValueName = "URL Protocol";
        const string UrlProtocolValueValue = "";

        bool NeedToAddKeys()
        {
            var addKeys = false;
            using (var commandKey = Registry.CurrentUser.OpenSubKey(CommandKeyPath))
            {
                var commandValue = commandKey?.GetValue(CommandKeyValueName);
                addKeys |= !CommandKeyValueValue.Equals(commandValue);
            }

            using (var customUriSchemeKey = Registry.CurrentUser.OpenSubKey(CustomUriSchemeKeyPath))
            {
                var uriValue = customUriSchemeKey?.GetValue(CustomUriSchemeKeyValueName);
                var protocolValue = customUriSchemeKey?.GetValue(UrlProtocolValueName);

                addKeys |= !CustomUriSchemeKeyValueValue.Equals(uriValue);
                addKeys |= !UrlProtocolValueValue.Equals(protocolValue);
            }
            return addKeys;
        }

        void AddRegKeys()
        {
            using (var classesKey = Registry.CurrentUser.OpenSubKey(RootKeyPath, true))
            {
                using (var root = classesKey!.OpenSubKey(CustomUriScheme, true) ??
                    classesKey.CreateSubKey(CustomUriScheme, true))
                {
                    root.SetValue(CustomUriSchemeKeyValueName, CustomUriSchemeKeyValueValue);
                    root.SetValue(UrlProtocolValueName, UrlProtocolValueValue);

                    using (var shell = root.OpenSubKey(ShellKeyName, true) ??
                            root.CreateSubKey(ShellKeyName, true))
                    {
                        using (var open = shell.OpenSubKey(OpenKeyName, true) ??
                                shell.CreateSubKey(OpenKeyName, true))
                        {
                            using (var command = open.OpenSubKey(CommandKeyName, true) ??
                                    open.CreateSubKey(CommandKeyName, true))
                            {
                                command.SetValue(CommandKeyValueName, CommandKeyValueValue);
                            }
                        }
                    }
                }
            }
        }
#endif
    }
}