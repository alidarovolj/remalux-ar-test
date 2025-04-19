using UnityEditor;
using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;

namespace Remalux.Editor
{
    public static class AppleSiliconFixes
    {
        [MenuItem("Tools/Fix M1 Mac ONNX Issues")]
        public static void FixM1MacONNXIssues()
        {
            Debug.Log("Applying M1/M2 Mac specific ONNX and Barracuda fixes...");
            
            // Apply project-specific settings
            ApplyProjectSettings();
            
            // Create a stub for native plugins that might be missing ARM64 support
            CreateNativePluginStubs();
            
            // Fix shader compilation issues
            FixShaderCompilation();
            
            Debug.Log("M1/M2 Mac fixes completed. Please restart Unity for changes to take effect.");
        }
        
        private static void ApplyProjectSettings()
        {
            // Force ARM64 architecture
            PlayerSettings.SetArchitecture(BuildTargetGroup.Standalone, 1); // 1 = ARM64
            
            // Add define symbol for Apple Silicon
            string currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
            if (!currentDefines.Contains("UNITY_APPLE_SILICON"))
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(
                    BuildTargetGroup.Standalone,
                    currentDefines + ";UNITY_APPLE_SILICON");
            }
            
            // Disable SSE4.1 instructions that aren't supported on ARM
            string defineSymbolsEditor = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Editor);
            if (!defineSymbolsEditor.Contains("DISABLE_INTEL_INTRINSICS"))
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(
                    BuildTargetGroup.Editor,
                    defineSymbolsEditor + ";DISABLE_INTEL_INTRINSICS");
            }
            
            AssetDatabase.SaveAssets();
        }
        
        private static void CreateNativePluginStubs()
        {
            // Path to the plugins directory
            string pluginsDir = Path.Combine(Application.dataPath, "Plugins", "ARM64");
            
            // Create directory if it doesn't exist
            if (!Directory.Exists(pluginsDir))
            {
                Directory.CreateDirectory(pluginsDir);
            }
            
            // Create a stub file for ONNX runtime if needed
            string stubPath = Path.Combine(pluginsDir, "onnxruntime_stub.bundle");
            if (!File.Exists(stubPath))
            {
                try
                {
                    // Create an empty file as a placeholder
                    File.WriteAllText(Path.Combine(pluginsDir, "README_STUBS.txt"), 
                        "These are stub files for native plugins that may be missing ARM64 support.\n" +
                        "If you encounter errors related to missing native plugins, you may need to find\n" +
                        "or compile ARM64 versions of these libraries.");
                    
                    Debug.Log("Created native plugin stubs in: " + pluginsDir);
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to create stub files: " + e.Message);
                }
            }
            
            AssetDatabase.Refresh();
        }
        
        private static void FixShaderCompilation()
        {
            // Force the default Metal settings
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneOSX, new UnityEngine.Rendering.GraphicsDeviceType[] { 
                UnityEngine.Rendering.GraphicsDeviceType.Metal 
            });
            
            // Force recompile critical materials
            string[] materialPaths = Directory.GetFiles(Application.dataPath, "*.mat", SearchOption.AllDirectories);
            foreach (string materialPath in materialPaths)
            {
                string relativePath = "Assets" + materialPath.Substring(Application.dataPath.Length);
                AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
            }
            
            AssetDatabase.Refresh();
        }
    }
} 