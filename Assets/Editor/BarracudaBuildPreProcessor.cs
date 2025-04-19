using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Remalux.Editor
{
    /// <summary>
    /// Препроцессор сборки для исправления проблем с зависимостями Barracuda
    /// </summary>
    public class BarracudaBuildPreProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        
        private static readonly string[] DependentAssemblies = new[]
        {
            "Remalux.Core.dll",
            "Remalux.WallDetection.dll",
            "Assembly-CSharp.dll"
        };
        
        private static readonly string BarracudaDll = "Unity.Barracuda.dll";
        
        public void OnPreprocessBuild(BuildReport report)
        {
            Debug.Log("Applying Barracuda build fixes...");
            try
            {
                FixBarracudaDependencies();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error applying Barracuda build fixes: {e.Message}\n{e.StackTrace}");
            }
        }
        
        [MenuItem("Tools/Fix Barracuda Build Issues")]
        public static void FixBarracudaDependencies()
        {
            // 1. Ensure the Barracuda DLL is copied to our Plugins folder
            CopyBarracudaToPlugins();
            
            // 2. Update assembly references
            UpdateAssemblyReferences();
            
            // 3. Add scripting define symbols
            AddScriptingDefineSymbols();
            
            // 4. Force Unity to regenerate project files
            AssetDatabase.Refresh();
            
            Debug.Log("Barracuda build fixes applied. Please restart Unity for changes to take effect.");
        }
        
        private static void CopyBarracudaToPlugins()
        {
            string pluginsDir = Path.Combine(Application.dataPath, "Plugins", "BarracudaDeps");
            if (!Directory.Exists(pluginsDir))
            {
                Directory.CreateDirectory(pluginsDir);
            }
            
            // Find Barracuda DLL
            string libraryDir = Path.Combine(Application.dataPath, "..", "Library");
            string[] barracudaFiles = Directory.GetFiles(libraryDir, BarracudaDll, SearchOption.AllDirectories);
            
            if (barracudaFiles.Length == 0)
            {
                throw new Exception("Could not find Unity.Barracuda.dll in the Library folder");
            }
            
            // Copy to plugins folder
            string destPath = Path.Combine(pluginsDir, BarracudaDll);
            File.Copy(barracudaFiles[0], destPath, overwrite: true);
            
            Debug.Log($"Copied {BarracudaDll} to Plugins folder");
        }
        
        private static void UpdateAssemblyReferences()
        {
            // Create asmref file to reference Barracuda
            string asmrefPath = Path.Combine(Application.dataPath, "BarracudaReference.asmref");
            File.WriteAllText(asmrefPath, "{\n    \"reference\": \"Unity.Barracuda\"\n}");
            
            Debug.Log("Updated assembly references");
        }
        
        private static void AddScriptingDefineSymbols()
        {
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup);
                
            // Add necessary symbols
            List<string> symbolList = defines.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            bool changed = false;
            
            if (!symbolList.Contains("USE_BARRACUDA_INDIRECTLY"))
            {
                symbolList.Add("USE_BARRACUDA_INDIRECTLY");
                changed = true;
            }
            
            if (!symbolList.Contains("BARRACUDA_VERBOSE_IMPORT"))
            {
                symbolList.Add("BARRACUDA_VERBOSE_IMPORT");
                changed = true;
            }
            
            if (changed)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(
                    EditorUserBuildSettings.selectedBuildTargetGroup,
                    string.Join(";", symbolList));
                    
                Debug.Log("Added scripting define symbols");
            }
        }
    }
} 