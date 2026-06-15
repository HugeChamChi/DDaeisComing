using UnityEditor;
using UnityEngine;
using System.Linq;

public class iOSBuildScript
{
    [MenuItem("Build/Build iOS (Xcode)")]
    public static void BuildIOS()
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        string buildPath = "Builds/iOS";

        Debug.Log("Starting iOS Build to: " + buildPath);

        BuildPipeline.BuildPlayer(
            scenes,
            buildPath,
            BuildTarget.iOS,
            BuildOptions.None
        );

        Debug.Log("iOS Build Completed.");
    }
}
