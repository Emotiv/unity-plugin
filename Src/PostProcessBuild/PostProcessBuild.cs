using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
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

            // automatic code signing
            pbxProject.SetBuildProperty(targetGuid, "CODE_SIGN_STYLE", "Automatic");

            // DEVELOPMENT_TEAM is set from Jenkinsfile (actual team ID)
            string developmentTeam = System.Environment.GetEnvironmentVariable("DEVELOPMENT_TEAM");
            if (!string.IsNullOrEmpty(developmentTeam))
            {
                pbxProject.SetBuildProperty(targetGuid, "DEVELOPMENT_TEAM", developmentTeam);
            }

            // Add and link EmotivCortexLib.xcframework
            string frameworkPath = Path.Combine(pathToBuiltProject, "Frameworks/EmotivCortexLib.xcframework");
            pbxProject.AddFile(frameworkPath, "Frameworks/EmotivCortexLib.xcframework", PBXSourceTree.Source);
            pbxProject.AddFileToBuild(unityFrameworkTargetGuid, pbxProject.FindFileGuidByProjectPath("Frameworks/EmotivCortexLib.xcframework"));

            // Ensure the framework is marked as "Required"
            pbxProject.AddBuildProperty(unityFrameworkTargetGuid, "FRAMEWORK_SEARCH_PATHS", "$(PROJECT_DIR)/Frameworks");
            pbxProject.AddBuildProperty(unityFrameworkTargetGuid, "OTHER_LDFLAGS", "-framework EmotivCortexLib");

            pbxProject.WriteToFile(pbxProjectPath);
        }
    }
}