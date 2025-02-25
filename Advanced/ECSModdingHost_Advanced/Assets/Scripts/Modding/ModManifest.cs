

using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Orcsune.Core.Modding {
    [System.Serializable]
    public class ModManifest : IModInfo {
        
        public static string ModManifestFilename = "manifest.json";

        public class ValidationResult {
            public ValidationStatus Status;
            public string           Problem;
            public object           Data;
            public List<string>     Warnings;
            public ValidationResult() {
                Warnings = new List<string>();
            }
        }
        public enum ValidationStatus {
            VALID,              // The mod is valid.
            DLL_NOT_EXIST,      // A DLL referenced in inclusions/exclusions does not exist.
            NESTED_MODS,        // There is another mod manifest located in a subfolder of this mod.
            DOMAIN_UNLOAD_ERROR // There was a problem unloading the verification AppDomain.
        }

        // Basic Information
        public string   Name;
        public string   Version;
        public bool     Enabled { get; set; }
        public int      ModPriority { get; set; }

        // Libraries
        /// <summary>
        /// Determines whether all DLLs in
        /// all subfolders should be included
        /// by default.
        /// </summary>
        public bool UseAllDLLs = true;
        /// <summary>
        /// When UseAllDLLs is false, this specifies
        /// which DLLs should still be used.
        /// </summary>
        public List<string> IncludedDLLs;
        /// <summary>
        /// When UseAllDLLs is true, this specifies
        /// which DLLs should be excluded as an override.
        /// </summary>
        public List<string> ExcludedDLLs;
        /// <summary>
        /// List of DLLs files specifying Burst-compiled
        /// code. These must be loaded differently.
        /// </summary>
        public List<string> BurstDLLs;

        // Assets

        
        [JsonIgnore] private List<string>   _CachedDLLs;
        [JsonIgnore] private string         _CachedModFolder;

        [JsonIgnore] private string ManifestPath;

        public ModManifest(string manifestPath) {
            Enabled = false;
            ModPriority = 0;
            IncludedDLLs    = new List<string>();
            ExcludedDLLs    = new List<string>();
            BurstDLLs       = new List<string>();
            ManifestPath = manifestPath;
            // if (!File.Exists(manifestPath) || Path.GetFileName(manifestPath) != ModManifestFilename) {
            //     throw new ArgumentException(
            //         $"Cannot construct ModManifest with manifest path '{manifestPath}' because it is either not a file or is not a mod manifest file.",
            //         "manifestPath"
            //     );
            // } else {
            //     ManifestPath = manifestPath;
            // }
        }
        public override string ToString()
        {
            return $"ModManifest(Name={Name}, Version={Version}, Enabled={Enabled}, Priority={ModPriority}, Path={ManifestPath})";
        }

        public static bool TryManifestFromFile(string manifestPath, out ModManifest manifest) {
            manifest = ManifestFromFile(manifestPath);
            return manifest != null;
        }
        public static ModManifest ManifestFromFile(string manifestPath) {
            try {
                // If the file does not exist, then create one
                if (!File.Exists(manifestPath)) {
                    File.WriteAllText(manifestPath, "{}");
                }
                ModManifest manifest = new ModManifest(manifestPath);
                JsonConvert.PopulateObject(File.ReadAllText(manifestPath), manifest);
                UnityEngine.Debug.Log($"Returning new manifest: {manifest}");
                return manifest;
            } catch (JsonSerializationException s) {
                ModManifest manifest = new ModManifest(manifestPath);
                return manifest;
            } catch (Exception e) {
                UnityEngine.Debug.Log($"Problem making manifest from file: '{e}'");
                return null;
            }
        }

        public void SaveManifest(string saveFolder=null) {
            if (string.IsNullOrWhiteSpace(saveFolder)) {
                saveFolder = ManifestPath;
            }
            File.WriteAllText(saveFolder, JsonConvert.SerializeObject(this, Formatting.Indented));;
        }

        /// <summary>
        /// Scans subfolders, gathering DLLs.
        /// Heeds inclusion/exclusion rules.
        /// </summary>
        /// <param name="modFolder">The path to this mod folder.</param>
        /// <returns>List of DLLs available by the mod.</returns>
        public List<string> GatherDLLs(string modFolder=null) {
            // Return cached DLLs if mod path has not changed
            if (
                _CachedModFolder == modFolder &&
                _CachedDLLs != null
            ) {
                return _CachedDLLs;
            }
            if (modFolder==null) {
                modFolder = Path.GetDirectoryName(ManifestPath);
            }
            
            List<string> gathered       = new List<string>();
            HashSet<string> included    = IncludedDLLs.ToHashSet();
            HashSet<string> excluded    = ExcludedDLLs.ToHashSet();
            HashSet<string> bursted     = BurstDLLs.ToHashSet();
            // Iterate DLLs
            string[] dlls = Directory.GetFiles(modFolder, "*.dll", SearchOption.AllDirectories);
            foreach (string dll in dlls) {
                string relativeDLLPath = Path.GetRelativePath(modFolder, dll);
                // If we include all by default, then only add if not
                // explicitly excluded
                if (UseAllDLLs) {
                    if (!excluded.Contains(relativeDLLPath) && !bursted.Contains(relativeDLLPath)) {
                        gathered.Add(dll);
                    }
                }
                // If we exclude all by default, then only add if
                // explicitly included
                else {
                    if (included.Contains(relativeDLLPath) && !bursted.Contains(relativeDLLPath)) {
                        gathered.Add(dll);
                    }
                }
            }
            return gathered;
        }

        public List<string> GatherBurstDLLs(string modFolder=null) {
            if (modFolder==null) {
                modFolder = Path.GetDirectoryName(ManifestPath);
            }
            return BurstDLLs.Select(bdll => Path.Combine(modFolder, bdll)).ToList();
        }

        public ValidationResult ValidateMod(string modFolder=null) {
            if (modFolder==null) {
                modFolder = Path.GetDirectoryName(ManifestPath);
            }

            ValidationResult result = new ValidationResult();

            HashSet<string> included    = IncludedDLLs.ToHashSet();
            HashSet<string> excluded    = ExcludedDLLs.ToHashSet();
            // Check that all excluded/included DLLs exist
            // Check included DLLs
            foreach (string subpath in included) {
                string finalPath = Path.Combine(modFolder, subpath);
                if (!File.Exists(finalPath)) {
                    result.Status = ValidationStatus.DLL_NOT_EXIST;
                    result.Problem = $"Cannot find included referenced DLL '{subpath}' at path '{finalPath}'.";
                    return result;
                }
            }
            // Check excluded DLLs
            foreach (string subpath in excluded) {
                string finalPath = Path.Combine(modFolder, subpath);
                if (!File.Exists(finalPath)) {
                    result.Status = ValidationStatus.DLL_NOT_EXIST;
                    result.Problem = $"Cannot find excluded referenced DLL '{subpath}' at path '{finalPath}'.";
                    return result;
                }
            }
            // If not UseAllDLLs, then check that all included DLL
            string[] dlls = Directory.GetFiles(modFolder, "*.dll", SearchOption.AllDirectories);
            foreach (string dll in dlls) {
                string relativeDLLPath = Path.GetRelativePath(modFolder, dll);
            }
            // Check that there are no files in subfolders with the
            // same name as ModManifestFilename
            bool anyNestedMods = false;
            string[] potentialManifests = Directory.GetFiles(modFolder, $"*{Path.GetExtension(ModManifestFilename)}", SearchOption.AllDirectories);
            foreach (string potentialManifest in potentialManifests) {
                string filename = Path.GetFileName(potentialManifest);
                if (filename == ModManifestFilename) {
                    anyNestedMods = true;
                    result.Status = ValidationStatus.NESTED_MODS;
                    result.Problem = $"Has nested mods. At least 1 other file called '{ModManifestFilename}' in the mod.";
                    result.Warnings.Add($"Has nested mod at: '{potentialManifest}'");
                }
            }
            if (anyNestedMods) { return result; }

            // _CachedDLLs of null should force a
            // re-gather of used mods
            _CachedModFolder = modFolder;
            _CachedDLLs = null;
            _CachedDLLs = GatherDLLs(modFolder);
            // Check if there are any DLLs that are included.
            if (_CachedDLLs.Count == 0) {
                result.Warnings.Add(
                    $"There are no DLLs included in this mod. Make sure your includes/excludes do not exclude everything and make sure your path names to your DLLs are correct in the manifest."
                );
            }
            // Check if all normal DLLs can be loaded
            // into an app assembly. If BadImageFormatException,
            // then the DLL may be a burst DLL, which
            // should be treated differently
            AppDomain testDomain = AppDomain.CreateDomain("Mod Test Domain");
            foreach (string dll in _CachedDLLs) {
                try {
                    if (File.Exists(dll)) {
                        testDomain.Load(File.ReadAllBytes(dll));
                    }
                } catch (BadImageFormatException e) {
                    result.Warnings.Add(
                        $"The DLL '{dll}' was gathered as a normal mod DLL, but its assembly could not be normally loaded into an AppDomain. Maybe this is a Burst-compiled assembly? If so, please include it in BurstDLLs"
                    );
                }
            }
            try {
                AppDomain.Unload(testDomain);
            } catch (AppDomainUnloadedException e) {
                result.Status = ValidationStatus.DOMAIN_UNLOAD_ERROR;
                result.Problem = "The AppDomain used for a mod verification could not be unloaded.";
            }

            result.Status = ValidationStatus.VALID;
            return result;
        }
    }
}