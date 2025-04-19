using UnityEditor;
using UnityEditor.Compilation;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Remalux.Editor
{
    [InitializeOnLoad]
    public static class BarracudaDependencyFix
    {
        private const string BarracudaDllName = "Unity.Barracuda.dll";
        private const string AssetPath = "Assets/Plugins/BarracudaFix/";
        
        static BarracudaDependencyFix()
        {
            EditorApplication.delayCall += FixBarracudaDependency;
        }
        
        [MenuItem("Tools/Fix Barracuda Dependency")]
        public static void FixBarracudaDependency()
        {
            Debug.Log("Applying Barracuda dependency fix...");
            
            // 1. Create a backup copy of the Barracuda DLL in our project
            CopyBarracudaToPlugins();
            
            // 2. Create assembly definition references
            EnsureAssemblyReferences();
            
            // 3. Add define symbols to avoid direct Barracuda usage
            AddDefineSymbols();
            
            Debug.Log("Barracuda dependency fix applied. Please restart Unity.");
        }
        
        private static void CopyBarracudaToPlugins()
        {
            // Find the Barracuda DLL in the Library folder
            string[] dllPaths = Directory.GetFiles(
                Path.Combine(Application.dataPath, "..", "Library"), 
                BarracudaDllName, 
                SearchOption.AllDirectories);
            
            if (dllPaths.Length == 0)
            {
                Debug.LogError("Could not find Unity.Barracuda.dll in Library folder");
                return;
            }
            
            // Create plugins directory if it doesn't exist
            string pluginsDir = Path.Combine(Application.dataPath, "Plugins", "BarracudaFix");
            if (!Directory.Exists(pluginsDir))
            {
                Directory.CreateDirectory(pluginsDir);
            }
            
            // Copy the DLL to the plugins folder
            string destPath = Path.Combine(pluginsDir, BarracudaDllName);
            File.Copy(dllPaths[0], destPath, true);
            
            // Create meta file to ensure proper import settings
            string metaContent = @"fileFormatVersion: 2
guid: a5d45a35f5e0b49b8b32f799c9f31c5f
PluginImporter:
  externalObjects: {}
  serializedVersion: 2
  iconMap: {}
  executionOrder: {}
  defineConstraints: []
  isPreloaded: 0
  isOverridable: 0
  isExplicitlyReferenced: 1
  validateReferences: 1
  platformData:
  - first:
      Any: 
    second:
      enabled: 1
      settings: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
";
            File.WriteAllText(destPath + ".meta", metaContent);
            
            AssetDatabase.Refresh();
        }
        
        private static void EnsureAssemblyReferences()
        {
            // Create or update assembly reference file
            string asmrefPath = Path.Combine(Application.dataPath, "asmref-barracuda-reference.asmref");
            string asmrefContent = "{\n    \"reference\": \"Unity.Barracuda\"\n}";
            
            if (!File.Exists(asmrefPath) || File.ReadAllText(asmrefPath) != asmrefContent)
            {
                File.WriteAllText(asmrefPath, asmrefContent);
            }
            
            AssetDatabase.Refresh();
        }
        
        private static void AddDefineSymbols()
        {
            // Add define symbols to avoid direct Barracuda usage
            string currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup);
                
            if (!currentDefines.Contains("USE_BARRACUDA_INDIRECTLY"))
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(
                    EditorUserBuildSettings.selectedBuildTargetGroup,
                    currentDefines + ";USE_BARRACUDA_INDIRECTLY");
            }
        }
    }
} 