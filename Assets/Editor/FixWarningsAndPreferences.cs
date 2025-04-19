using UnityEngine;
using UnityEditor;
using System.IO;
using System;

namespace Remalux.Editor
{
    [InitializeOnLoad]
    public class FixWarningsAndPreferences
    {
        static FixWarningsAndPreferences()
        {
            EditorApplication.delayCall += ApplyPreferences;
        }
        
        [MenuItem("Remalux/Fix Warnings")]
        public static void ApplyPreferences()
        {
            Debug.Log("Applying custom preferences to fix warnings...");
            
            // Отключаем предупреждения о symlinks
            EditorPrefs.SetBool("WarnOnSymlinks", false);
            
            // Отключаем предупреждения о TMP
            EditorPrefs.SetBool("TMPWillBeDeprecated", true);
            
            // Устанавливаем Metal API для улучшения производительности на M1/M2
            #if UNITY_EDITOR_OSX
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneOSX, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneOSX, new UnityEngine.Rendering.GraphicsDeviceType[] { 
                UnityEngine.Rendering.GraphicsDeviceType.Metal 
            });
            
            // Устанавливаем Metal API для iOS
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS, new UnityEngine.Rendering.GraphicsDeviceType[] { 
                UnityEngine.Rendering.GraphicsDeviceType.Metal 
            });
            #endif
            
            // Исключаем Python venv из импорта
            ExcludePythonVenvFromProject();
            
            Debug.Log("Fixed warnings and applied optimal preferences");
        }
        
        private static void ExcludePythonVenvFromProject()
        {
            try
            {
                string venvPath = "Assets/Scripts/Utils/venv";
                
                if (Directory.Exists(venvPath))
                {
                    // Проверяем наличие метафайла
                    string metaPath = venvPath + ".meta";
                    
                    if (File.Exists(metaPath))
                    {
                        // Читаем метафайл
                        string metaContent = File.ReadAllText(metaPath);
                        
                        // Изменяем свойство exclude на true, если оно уже не установлено
                        if (!metaContent.Contains("excluded: 1"))
                        {
                            metaContent = metaContent.Replace("excluded: 0", "excluded: 1");
                            
                            // Если свойства нет вообще, добавляем его
                            if (!metaContent.Contains("excluded:"))
                            {
                                metaContent += "\nexcluded: 1";
                            }
                            
                            // Сохраняем обновленный метафайл
                            File.WriteAllText(metaPath, metaContent);
                            Debug.Log("Excluded venv directory from project");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to exclude Python venv: {e.Message}");
            }
        }
    }
} 