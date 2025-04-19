using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Remalux.Editor
{
    [InitializeOnLoad]
    public class PackageLinkResolver
    {
        private static readonly string[] RequiredPackages = new[]
        {
            "com.unity.barracuda",
            "com.unity.xr.arfoundation",
            "com.unity.xr.arsubsystems",
            "com.unity.xr.core-utils"
        };

        static PackageLinkResolver()
        {
            EditorApplication.delayCall += CheckPackages;
        }

        [MenuItem("Tools/Packages/Fix Package References")]
        public static void CheckPackages()
        {
            Debug.Log("Checking package references...");
            
            bool allFound = true;
            
            // Check if all required packages are installed
            foreach (var package in RequiredPackages)
            {
                bool found = IsPackageInstalled(package);
                Debug.Log($"Package {package}: {(found ? "Found" : "Missing")}");
                allFound &= found;
            }
            
            if (!allFound)
            {
                Debug.LogWarning("Some required packages are missing. Check the Package Manager.");
            }
            
            // Create or update link.xml
            CreateOrUpdateLinkXml();
            
            // Force reimport assembly definitions to refresh references
            ForceReimportAsmDefs();
            
            Debug.Log("Package reference check completed.");
        }

        private static bool IsPackageInstalled(string packageId)
        {
            var packagePath = Path.Combine("Packages", packageId);
            var manifestPath = Path.Combine("Packages", "manifest.json");
            
            if (Directory.Exists(packagePath))
                return true;
            
            // Check in manifest
            if (File.Exists(manifestPath))
            {
                string content = File.ReadAllText(manifestPath);
                return content.Contains($"\"{packageId}\"");
            }
            
            return false;
        }

        private static void CreateOrUpdateLinkXml()
        {
            const string linkXmlPath = "Assets/link.xml";
            
            // Create basic link.xml if it doesn't exist
            if (!File.Exists(linkXmlPath))
            {
                string linkXml = @"<linker>
  <assembly fullname=""Unity.XR.ARSubsystems"" preserve=""all""/>
  <assembly fullname=""Unity.Barracuda"" preserve=""all""/>
  <assembly fullname=""Unity.XR.ARFoundation"" preserve=""all""/>
  <assembly fullname=""Unity.XR.CoreUtils"" preserve=""all""/>
</linker>";
                
                File.WriteAllText(linkXmlPath, linkXml);
                AssetDatabase.ImportAsset(linkXmlPath);
                Debug.Log($"Created {linkXmlPath}");
            }
        }

        private static void ForceReimportAsmDefs()
        {
            string[] asmdefPaths = Directory.GetFiles("Assets", "*.asmdef", SearchOption.AllDirectories);
            foreach (var asmdefPath in asmdefPaths)
            {
                AssetDatabase.ImportAsset(asmdefPath, ImportAssetOptions.ForceUpdate);
                Debug.Log($"Reimported {asmdefPath}");
            }
        }
    }
} 