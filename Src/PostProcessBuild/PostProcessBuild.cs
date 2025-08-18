#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
using System.IO;

public class PostProcessBuild
{
    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target == BuildTarget.iOS)
        {
            string pbxProjectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
            PBXProject pbxProject = new PBXProject();
            pbxProject.ReadFromFile(pbxProjectPath);

            string targetGuid = pbxProject.GetUnityMainTargetGuid();
            string unityFrameworkTargetGuid = pbxProject.GetUnityFrameworkTargetGuid();

            // Automatic code signing
            pbxProject.SetBuildProperty(targetGuid, "CODE_SIGN_STYLE", "Automatic");

            // DEVELOPMENT_TEAM is set from Jenkinsfile (actual team ID)
            string developmentTeam = System.Environment.GetEnvironmentVariable("DEVELOPMENT_TEAM");
            if (!string.IsNullOrEmpty(developmentTeam))
            {
                pbxProject.SetBuildProperty(targetGuid, "DEVELOPMENT_TEAM", developmentTeam);
            }

            // Add and link EmotivCortexLib.xcframework to main target
            string frameworkPath = Path.Combine(pathToBuiltProject, "Frameworks/EmotivCortexLib.xcframework");
            string fileGuid = pbxProject.AddFile(frameworkPath, "Frameworks/EmotivCortexLib.xcframework", PBXSourceTree.Source);
            pbxProject.AddFileToBuild(targetGuid, fileGuid);
            pbxProject.AddFileToBuild(unityFrameworkTargetGuid, fileGuid);

            // Add the framework to the "Embed Frameworks" build phase
            string embedPhase = pbxProject.AddCopyFilesBuildPhase(targetGuid, "Embed Frameworks", "", "10");
            pbxProject.AddFileToBuildSection(targetGuid, embedPhase, fileGuid);

            // Ensure "Code Sign On Copy" is enabled for the framework
            PBXProjectExtensions.AddFileToEmbedFrameworks(pbxProject, targetGuid, fileGuid);

            pbxProject.AddBuildProperty(targetGuid, "FRAMEWORK_SEARCH_PATHS", "$(PROJECT_DIR)/Frameworks");
            pbxProject.AddBuildProperty(targetGuid, "OTHER_LDFLAGS", "-framework EmotivCortexLib");
            pbxProject.AddBuildProperty(unityFrameworkTargetGuid, "FRAMEWORK_SEARCH_PATHS", "$(PROJECT_DIR)/Frameworks");
            pbxProject.AddBuildProperty(unityFrameworkTargetGuid, "OTHER_LDFLAGS", "-framework EmotivCortexLib");

            pbxProject.WriteToFile(pbxProjectPath);

            // Add NSBluetoothAlwaysUsageDescription
            string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
            PlistDocument plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

            PlistElementDict rootDict = plist.root;
            rootDict.SetString("NSBluetoothAlwaysUsageDescription", "This will allow app to find and connect to Bluetooth accessories.");
            rootDict.SetBoolean("ITSAppUsesNonExemptEncryption", false); 

            plist.WriteToFile(plistPath);
        }
    }
}
#endif