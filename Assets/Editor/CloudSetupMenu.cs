using UnityEditor;
using UnityEngine;
using PhobiaReliefTherapy.Data;

public static class CloudSetupMenu
{
    [MenuItem("Window/PRT Cloud Setup")]
    [MenuItem("Tools/PRT/Apply Cloud Setup")]
    [MenuItem("Tools/Build Auth Scenes/Apply Cloud Setup")]
    public static void ApplyCloudSetup()
    {
        if (!CloudSetup.ApplyOnce(out string error))
        {
            EditorUtility.DisplayDialog("Cloud setup", error, "OK");
            return;
        }

        EditorUtility.DisplayDialog(
            "Cloud setup",
            "Setup SQL is copied. Click Run in the SQL tab that opened.",
            "OK");
    }
}
