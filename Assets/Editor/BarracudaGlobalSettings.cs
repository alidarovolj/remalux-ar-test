using UnityEditor;
using UnityEngine;
using System.IO;
using System;

namespace Remalux.Editor
{
    public static class BarracudaGlobalSettings
    {
        // Путь к файлу с ранее сохраненными настройками
        private const string SettingsFilePath = "Assets/Editor/BarracudaSettings.json";
        
        // Класс для хранения настроек сборки
        [Serializable]
        private class BuildSettings
        {
            public bool disableBarracudaDirectReferences = true;
            public bool useBarracudaIndirectly = true;
            public string[] barracudaAssemblyOrder = new[] 
            {
                "Unity.Barracuda.dll",
                "Remalux.Core.dll",
                "Remalux.WallDetection.dll"
            };
        }
        
        [MenuItem("Tools/Fix All Barracuda Issues")]
        public static void FixAllBarracudaIssues()
        {
            Debug.Log("Fixing all Barracuda-related issues...");
            
            try
            {
                // 1. Fix assembly references
                FixAssemblyReferences();
                
                // 2. Fix build order
                BuildOrderFixer.FixBuildOrder();
                
                // 3. Fix dependency issues
                BarracudaDependencyFix.FixBarracudaDependency();
                
                // 4. Fix build preprocessor
                BarracudaBuildPreProcessor.FixBarracudaDependencies();
                
                // 5. Save settings
                SaveSettings();
                
                Debug.Log("All Barracuda issues fixed. Please restart Unity for changes to take full effect.");
                
                // 6. Request script reload to apply changes immediately
                AssetDatabase.Refresh();
                EditorUtility.RequestScriptReload();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error fixing Barracuda issues: {e.Message}\n{e.StackTrace}");
            }
        }
        
        [MenuItem("Tools/Display Barracuda Import Status")]
        public static void DisplayBarracudaImportStatus()
        {
            string barracudaDllPath = Path.Combine(Application.dataPath, "..", "Library", "ScriptAssemblies", "Unity.Barracuda.dll");
            bool barracudaDllExists = File.Exists(barracudaDllPath);
            
            Debug.Log($"Barracuda DLL status:");
            Debug.Log($"- Unity.Barracuda.dll exists: {barracudaDllExists}");
            
            // Проверка asmref файла
            string asmrefPath = Path.Combine(Application.dataPath, "asmref-barracuda-fix.asmref");
            bool asmrefExists = File.Exists(asmrefPath);
            
            Debug.Log($"- asmref-barracuda-fix.asmref exists: {asmrefExists}");
            
            // Проверка копии DLL в Plugins
            string pluginDllPath = Path.Combine(Application.dataPath, "Plugins", "BarracudaFix", "Unity.Barracuda.dll");
            bool pluginDllExists = File.Exists(pluginDllPath);
            
            Debug.Log($"- Plugins copy of Unity.Barracuda.dll exists: {pluginDllExists}");
            
            // Проверка define symbols
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            bool hasIndirectSymbol = defines.Contains("USE_BARRACUDA_INDIRECTLY");
            
            Debug.Log($"- USE_BARRACUDA_INDIRECTLY symbol defined: {hasIndirectSymbol}");
            
            // Предложение по исправлению
            if (!barracudaDllExists || !asmrefExists || !pluginDllExists || !hasIndirectSymbol)
            {
                Debug.Log("Some issues detected with Barracuda import. Recommend running Tools > Fix All Barracuda Issues.");
            }
            else
            {
                Debug.Log("Barracuda import configuration looks good!");
            }
        }
        
        private static void FixAssemblyReferences()
        {
            // Путь к asmref файлу
            string asmrefPath = Path.Combine(Application.dataPath, "asmref-barracuda-fix.asmref");
            
            // Создаем или перезаписываем файл
            string asmrefContent = "{\n    \"reference\": \"Unity.Barracuda\"\n}";
            File.WriteAllText(asmrefPath, asmrefContent);
            
            Debug.Log("Assembly references fixed");
        }
        
        private static void SaveSettings()
        {
            BuildSettings settings = new BuildSettings();
            string json = JsonUtility.ToJson(settings, true);
            File.WriteAllText(SettingsFilePath, json);
            
            Debug.Log("Barracuda settings saved");
        }
    }
} 