using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Unity.Burst;
using UnityEngine;

namespace Orcsune.Core.Modding {
    public static class ModLoader {
        // public static string ModPath = @"path/to/mod/folder";
        public static string ModPath = @"C:\_TempMods";
        public static bool EnforceGameReload = true;
        public static bool IsLoaded = false;

        private static AppDomain appDomain;

        private static ProjectModManifest ProjectManifest;

        private static string MetafilePath {
            get => Path.Combine(ModPath, "mods.meta");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        public static void LoadModDLLs() {
            Debug.Log($"ModLoader calling LoadModDLLs");
            if (IsLoaded) { return; }
            IsLoaded = true;
            if (appDomain == null) { CreateAppDomain(); }
            ReadMetadata();
            ValidateMods();
            PreLoadInitialize();
            List<KeyValuePair<string, ModManifest>> sortedMods = SortedMods();
            foreach (var kvp in sortedMods) {
                string modpath = Path.Combine(ModPath, kvp.Key);
                Debug.Log($"ModLoader calling LoadSingleMod on mod file '{modpath}'");
                LoadSingleMod(kvp.Value);
            }
            PostLoadInitialize();
            WriteMetadata();
        }

        private static void CreateAppDomain() {
            appDomain = AppDomain.CurrentDomain;
        }

        private static void PreLoadInitialize() {

        }

        private static void PostLoadInitialize() {

        }

        private static void LoadSingleMod(ModManifest mod) {
            foreach (string dllPath in mod.GatherBurstDLLs()) {
                Debug.LogError($"Loading mod Burst DLL at path '{dllPath}'.");
#if !UNITY_EDITOR
                BurstRuntime.LoadAdditionalLibrary(dllPath);
#endif
            }
            foreach (string dllPath in mod.GatherDLLs()) {
                Debug.LogError($"Loading mod DLL at path '{dllPath}'.");
                if (BurstCompiler.IsLoadAdditionalLibrarySupported()) {
                    Debug.LogError($"Additional burst libraries SUPPORTED.");
                } else {
                    Debug.LogError($"Additional burst libraries NOT supported.");
                }
                try {
                    Assembly.LoadFile(dllPath);
                } catch (BadImageFormatException e) {
                    Debug.LogError($"Mod DLL at path '{dllPath}' could not be loaded normally. If this is a Burst-compiled assembly, specify it in BurstDLLs.");
                }
            }
            // ModLoaderHelpers.LoadDeclarationsOfType(modpath, typeof(SystemBase), appDomain);
        }

        public static void ApplyModChanges() {
            // If enforceGameReload, then force application quit after application
            if (EnforceGameReload) {
                Application.Quit();
            }
        }

        private static List<KeyValuePair<string, ModManifest>> SortedMods(bool filterUsed=true) {
            // Potentially filter out unused mods
            List<KeyValuePair<string, ModManifest>> sortedMods = ProjectManifest.Manifests.Where(kvp => !filterUsed || kvp.Value.Enabled).ToList();
            // We want to sort in DESCENDING order of ModPriority
            // and ASCENDING alphabetical order by name
            sortedMods.Sort((kvp1, kvp2) => {
                if (kvp1.Value.ModPriority == kvp2.Value.ModPriority) {
                    Debug.Log($"Mods '{kvp1.Key}' and '{kvp2.Key}' have same priority. CompareTo={kvp1.Key.CompareTo(kvp2.Key)}");
                    return kvp1.Key.CompareTo(kvp2.Key);
                }
                Debug.Log($"Mod '{kvp1.Key}' has priority {kvp1.Value.ModPriority} and '{kvp2.Key}' has priority {kvp2.Value.ModPriority}. CompareTo={kvp1.Value.ModPriority.CompareTo(kvp2.Value.ModPriority)}");
                // Comparison order switched so we have highest ModPriority first
                return kvp2.Value.ModPriority.CompareTo(kvp1.Value.ModPriority);
            });
            return sortedMods;
        }

        private static void ReadMetadata() {
            Debug.Log($"ModLoader ReadMetadata");
            // Potentially set up new metadata file
            if (!File.Exists(MetafilePath)) {
                Debug.Log($"ModLoader Metafile does not exist, creating new file.");
                FileStream fs = File.Create(MetafilePath);
                fs.Close();
                string emptyMetaString = JsonConvert.SerializeObject(new ProjectModManifest());
                File.WriteAllText(MetafilePath, emptyMetaString);
            }
            // Read in current metafile on disk
            string metadataString = File.ReadAllText(MetafilePath);
            Debug.Log($"ModLoader read the following string from metafile: '{metadataString}'.");
            ProjectManifest = JsonConvert.DeserializeObject<ProjectModManifest>(metadataString);
            if (ProjectManifest == null) { ProjectManifest = new ProjectModManifest(); Debug.Log($"ModLoader deserialization was null."); }
            ProjectManifest.InitializeModManifests(ModPath);
            Debug.Log($"ModLoader ModFiles has {ProjectManifest.ManifestPaths.Count} existing mods recognized.");
            foreach (string key in ProjectManifest.ManifestPaths) {
                Debug.Log($"ModLoader mod '{key}': enabled={ProjectManifest.Manifests[key].Enabled}, priority={ProjectManifest.Manifests[key].ModPriority}.");
            }
        }

        private static bool ValidateMods() {
            return ProjectManifest.ValidateMods();
        }

        private static void WriteMetadata() {
            if (!File.Exists(MetafilePath)) {
                FileStream fs = File.Create(MetafilePath);
                fs.Close();
            }
            string metadataString = JsonConvert.SerializeObject(ProjectManifest);
            File.WriteAllText(MetafilePath, metadataString);
            ProjectManifest.SaveModManifests();
        }
    }
}