using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using System.Collections.Generic;

namespace Remalux.Editor
{
    [InitializeOnLoad]
    public static class ForcePackageImport
    {
        private static readonly string[] RequiredPackages = new string[]
        {
            "com.unity.barracuda",
            "com.unity.xr.arfoundation",
            "com.unity.xr.arsubsystems",
            "com.unity.xr.core-utils"
        };
        
        private static AddRequest Request;
        private static int CurrentPackageIndex = 0;
        
        static ForcePackageImport()
        {
            EditorApplication.delayCall += CheckPackages;
        }
        
        [MenuItem("Tools/Force Reinstall Packages")]
        public static void CheckPackages()
        {
            Debug.Log("Checking required packages...");
            
            // Start with the first package
            CurrentPackageIndex = 0;
            CheckNextPackage();
        }
        
        private static void CheckNextPackage()
        {
            if (CurrentPackageIndex >= RequiredPackages.Length)
            {
                Debug.Log("All packages checked and imported if needed.");
                return;
            }
            
            string packageId = RequiredPackages[CurrentPackageIndex];
            
            // Check if package exists and is not malformed
            Request = Client.Add(packageId);
            EditorApplication.update += Progress;
        }
        
        private static void Progress()
        {
            if (Request.IsCompleted)
            {
                EditorApplication.update -= Progress;
                
                if (Request.Status == StatusCode.Success)
                {
                    Debug.Log($"Package {RequiredPackages[CurrentPackageIndex]} installed successfully.");
                }
                else if (Request.Status == StatusCode.Failure)
                {
                    Debug.LogError($"Failed to install package {RequiredPackages[CurrentPackageIndex]}: {Request.Error.message}");
                }
                
                // Move to next package
                CurrentPackageIndex++;
                CheckNextPackage();
            }
        }
        
        [MenuItem("Tools/Modify Project For M1 Mac")]
        public static void ModifyProjectForM1Mac()
        {
            Debug.Log("Applying M1 Mac specific fixes...");
            
            // Create a build for Mac with ARM64 architecture
            PlayerSettings.SetArchitecture(BuildTargetGroup.Standalone, 1); // 1 = ARM64
            
            // Switch to Metal API
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneOSX, new UnityEngine.Rendering.GraphicsDeviceType[] { 
                UnityEngine.Rendering.GraphicsDeviceType.Metal 
            });
            
            // Add platform specific define
            string currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
            if (!currentDefines.Contains("UNITY_APPLE_SILICON"))
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(
                    BuildTargetGroup.Standalone,
                    currentDefines + ";UNITY_APPLE_SILICON");
            }
            
            // Force shader recompile
            AssetDatabase.ImportAsset("Assets/Materials/WallPaintMaterial.mat", ImportAssetOptions.ForceUpdate);
            
            Debug.Log("M1 Mac specific fixes applied.");
        }
    }
} 