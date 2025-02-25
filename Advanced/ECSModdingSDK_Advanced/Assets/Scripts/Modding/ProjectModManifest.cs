

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Orcsune.Core.Modding {
    public class ProjectModManifest {
        [JsonIgnore] public Dictionary<string, ModManifest> Manifests;
        [JsonIgnore] public List<string> ManifestPaths {
            get => Manifests.Keys.ToList();
        }
        /// <summary>
        /// When true, if any Warnings are reported in any
        /// mod validations, then ValidateMods should
        /// report false. Otherwise, validation is
        /// successful as long as there are no errors.
        /// </summary>
        public bool PreventValidationOnWarnings;

        public ProjectModManifest() {
            Manifests = new Dictionary<string, ModManifest>();
        }

        public void InitializeModManifests(string modsFolder) {
            UnityEngine.Debug.Log($"InitializeModManifests: getting files with extension: '{Path.GetExtension(ModManifest.ModManifestFilename)}' recursively from folder '{modsFolder}'");
            string[] potentialManifests = 
                Directory.GetFiles(modsFolder, $"*{Path.GetExtension(ModManifest.ModManifestFilename)}", SearchOption.AllDirectories);
            UnityEngine.Debug.Log($"Found {potentialManifests.Length} potential manifests");
            foreach (string potentialManifest in potentialManifests) {
                string modFolder            = Path.GetDirectoryName(potentialManifest);
                string relativeModFolder    = Path.GetRelativePath(modsFolder, modFolder);
                string relativeModManifest  = Path.GetRelativePath(modsFolder, potentialManifest);
                // string filename             = Path.GetFileName(potentialManifest);
                UnityEngine.Debug.Log($"Trying potential manifest '{potentialManifest}'");
                if (ModManifest.TryManifestFromFile(potentialManifest, out ModManifest manifest)) {
                    Manifests[relativeModManifest] = manifest;
                }
            }
        }
        public bool ValidateMods() {
            Dictionary<string, ModManifest.ValidationResult> results = GatherValidationResults();
            // If warnings exist, then do not pass validation
            if (PreventValidationOnWarnings) {
                return results
                        .Values
                        .All(x => x.Status == ModManifest.ValidationStatus.VALID && x.Warnings.Count == 0);
            }
            // Allow validation to pass with warnings
            else {
                return results
                        .Values
                        .All(x => x.Status == ModManifest.ValidationStatus.VALID);
            }
        }
        public Dictionary<string, ModManifest.ValidationResult> GatherValidationResults() {
            Dictionary<string, ModManifest.ValidationResult> results = new Dictionary<string, ModManifest.ValidationResult>();
            foreach ((string mod, ModManifest manifest) in Manifests) {
                results.Add(mod, manifest.ValidateMod());
            }
            return results;
        }
        public void SaveModManifests() {
            foreach (ModManifest manifest in Manifests.Values) {
                manifest.SaveManifest();
            }
        }
    }
}