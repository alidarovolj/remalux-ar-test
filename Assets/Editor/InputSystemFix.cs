using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Linq;

namespace Remalux.Editor
{
    [InitializeOnLoad]
    public class InputSystemFix
    {
        static InputSystemFix()
        {
            EditorApplication.delayCall += FixInputSystemAssets;
        }
        
        [MenuItem("Tools/Fix Input System Issues")]
        public static void FixInputSystemAssets()
        {
            Debug.Log("Fixing Input System references...");
            
            try
            {
                // Проверка наличия пакета InputSystem
                bool hasInputSystem = Directory.Exists(Path.Combine(Application.dataPath, "..", "Library", "PackageCache")
                    .Where(dir => dir.Contains("com.unity.inputsystem@")).Any());
                
                if (!hasInputSystem)
                {
                    Debug.LogWarning("Input System package not detected. Consider installing it via Package Manager.");
                }
                
                // Добавляем define symbol для условной компиляции кода с InputSystem
                string currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(
                    EditorUserBuildSettings.selectedBuildTargetGroup);
                    
                if (!currentDefines.Contains("USE_INPUT_SYSTEM_PACKAGE"))
                {
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(
                        EditorUserBuildSettings.selectedBuildTargetGroup,
                        currentDefines + ";USE_INPUT_SYSTEM_PACKAGE");
                    
                    Debug.Log("Added USE_INPUT_SYSTEM_PACKAGE define symbol");
                }
                
                // Исправляем файлы UI Builder, если они имеют ссылки на InputSystem
                FixInputSystemReferencesInUIXML();
                
                // Временно отключаем проблемные UXML файлы
                DisableProblemUXMLFiles();
                
                Debug.Log("Input System references fix completed");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error fixing Input System references: {e.Message}\n{e.StackTrace}");
            }
        }
        
        private static void FixInputSystemReferencesInUIXML()
        {
            // Находим все UXML файлы
            string[] uxmlFiles = Directory.GetFiles(Application.dataPath, "*.uxml", SearchOption.AllDirectories);
            
            foreach (string uxmlPath in uxmlFiles)
            {
                try
                {
                    string content = File.ReadAllText(uxmlPath);
                    
                    bool contentModified = false;
                    
                    // Проверяем наличие проблемных ссылок на InputSystem
                    if (content.Contains("UnityEngine.InputSystem.InputActionAsset"))
                    {
                        // Заменяем на условное использование
                        content = content.Replace(
                            "UnityEngine.InputSystem.InputActionAsset",
                            "UnityEngine.Object");
                        contentModified = true;
                    }
                    
                    if (contentModified)
                    {
                        File.WriteAllText(uxmlPath, content);
                        Debug.Log($"Fixed InputSystem reference in {uxmlPath}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Could not process {uxmlPath}: {e.Message}");
                }
            }
        }
        
        private static void DisableProblemUXMLFiles()
        {
            // Находим и временно переименовываем UXML файлы, которые могут вызывать проблемы
            string[] uxmlFiles = Directory.GetFiles(Application.dataPath, "*.uxml", SearchOption.AllDirectories)
                .Where(path => path.Contains("InputSystem") || File.ReadAllText(path).Contains("InputSystem"))
                .ToArray();
                
            foreach (string uxmlPath in uxmlFiles)
            {
                try
                {
                    string backupPath = uxmlPath + ".bak";
                    File.Copy(uxmlPath, backupPath, overwrite: true);
                    File.Delete(uxmlPath);
                    Debug.Log($"Temporarily disabled problematic UXML file: {uxmlPath}");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Could not disable {uxmlPath}: {e.Message}");
                }
            }
        }
    }
} 