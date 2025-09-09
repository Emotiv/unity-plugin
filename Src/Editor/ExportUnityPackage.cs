// filepath: Assets/Editor/ExportUnityPackage.cs
using UnityEditor;

public class ExportUnityPackage
{
    [MenuItem("Tools/Export EmotivUnityPlugin")]
    public static void Export()
    {
        AssetDatabase.ExportPackage(
            "Assets/EmotivUnityPlugin",
            "EmotivUnityPlugin.unitypackage",
            ExportPackageOptions.Recurse
        );
        UnityEngine.Debug.Log("Exported EmotivUnityPlugin.unitypackage");
    }
}