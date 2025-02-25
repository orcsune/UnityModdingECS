using UnityEditor;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;

public class CreateModPopup : EditorWindow
{

    static string modName;
    static string version;
    static int modPriority;
    static bool includeAllDLLs;
    static List<string> includedDLLs = new List<string>();
    static List<string> excludedDLLs = new List<string>();
    static string exportFolder;
    static BuildOptions buildOptions = BuildOptions.Development;

    [MenuItem("Modding/Export Mod")]
    public static void ShowExportPopup()
    {
        CreateModPopup window = CreateInstance<CreateModPopup>();
        window.position = new Rect(Screen.width / 2, Screen.height / 2, 350, 350);
        window.ShowUtility();
    }

    public static void ExportMod() {
        string buildSubfolder = string.IsNullOrWhiteSpace(exportFolder) ? "TempBuild" : exportFolder;
        string projectFolder = Path.Combine(Application.dataPath, "..");
        string buildFolder = Path.Combine(projectFolder, buildSubfolder);

        FileUtil.DeleteFileOrDirectory(buildFolder);
        Directory.CreateDirectory(buildFolder);

        // Build player.
        var report = BuildPipeline.BuildPlayer(
            new[] { "Assets/Scenes/SampleScene.unity" },
            Path.Combine(buildFolder, $"{modName}.exe"),
            BuildTarget.StandaloneWindows64,
            buildOptions
        );
    }

    void OnGUI() {
        EditorGUILayout.LabelField("Export Mod");
        modName         = EditorGUILayout.TextField("Name: ", modName);
        version         = EditorGUILayout.TextField("Version: ", version);
        modPriority     = EditorGUILayout.IntField("Mod Priority:", modPriority);
        includeAllDLLs  = EditorGUILayout.Toggle("Include All DLLs:", includeAllDLLs);
        exportFolder    = EditorGUILayout.TextField("Export Folder: ", exportFolder);
        buildOptions    = (BuildOptions)EditorGUILayout.EnumFlagsField("BuildOptions", buildOptions);

        if (GUILayout.Button("Build and Export")) {
            ExportMod();
        }
    }

}
