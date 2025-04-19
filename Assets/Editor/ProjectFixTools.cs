using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Remalux.Editor
{
    /// <summary>
    /// Центральный инструмент для исправления всех распространенных проблем проекта
    /// </summary>
    public static class ProjectFixTools
    {
        [MenuItem("Remalux/Fix All Project Issues", priority = 0)]
        public static void FixAllProjectIssues()
        {
            try
            {
                Debug.Log("=== Starting comprehensive project issues fix ===");
                
                // 1. Исправление проблем с Barracuda
                BarracudaGlobalSettings.FixAllBarracudaIssues();
                
                // 2. Исправление проблем с Input System
                InputSystemFix.FixInputSystemAssets();
                
                // 3. Исправление проблем с Apple Silicon
                FixAppleSiliconIssues();
                
                // 4. Исправление проблем с порядком сборки
                BuildOrderFixer.FixBuildOrder();
                
                // 5. Очистка кэша
                CleanProjectCache();
                
                // 6. Добавление специальных define symbols для Apple Silicon
                AddAppleSiliconDefines();
                
                // 7. Исправление предупреждений
                FixWarningsAndPreferences.ApplyPreferences();
                
                // 8. Переименование плагина Barracuda если нужно
                RenameBarracudaPluginIfNeeded();
                
                AssetDatabase.Refresh();
                EditorUtility.RequestScriptReload();
                
                Debug.Log("=== All project issues fixed ===");
                Debug.Log("Пожалуйста, перезапустите Unity для применения всех изменений");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error fixing project issues: {e.Message}\n{e.StackTrace}");
            }
        }
        
        [MenuItem("Remalux/Project Status", priority = 1)]
        public static void ShowProjectStatus()
        {
            Debug.Log("=== Project Status ===");
            
            // Проверка статуса Barracuda
            string barracudaDllPath = Path.Combine(Application.dataPath, "..", "Library", "ScriptAssemblies", "Unity.Barracuda.dll");
            bool barracudaDllExists = File.Exists(barracudaDllPath);
            Debug.Log($"- Unity.Barracuda.dll exists: {barracudaDllExists}");
            
            // Проверка наличия плагина
            string pluginPath = "Assets/Plugins/BarracudaFix/Barracuda.Runtime.dll";
            bool pluginExists = File.Exists(pluginPath);
            Debug.Log($"- Barracuda plugin exists: {pluginExists}");
            
            // Проверка define symbols
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            bool hasBarracudaSymbol = defines.Contains("USE_BARRACUDA_INDIRECTLY");
            bool hasAppleSiliconSymbol = defines.Contains("UNITY_APPLE_SILICON");
            bool hasInputSystemSymbol = defines.Contains("USE_INPUT_SYSTEM_PACKAGE");
            
            Debug.Log($"- Define symbols:");
            Debug.Log($"  - USE_BARRACUDA_INDIRECTLY: {hasBarracudaSymbol}");
            Debug.Log($"  - UNITY_APPLE_SILICON: {hasAppleSiliconSymbol}");
            Debug.Log($"  - USE_INPUT_SYSTEM_PACKAGE: {hasInputSystemSymbol}");
            
            // Проверка платформы
            bool isAppleSilicon = IsAppleSiliconPlatform();
            Debug.Log($"- Running on Apple Silicon: {isAppleSilicon}");
            
            // Проверка дублирующих asmref файлов
            string[] asmrefFiles = Directory.GetFiles(Application.dataPath, "*.asmref", SearchOption.AllDirectories);
            if (asmrefFiles.Length > 0)
            {
                Debug.Log("- Assembly reference files:");
                foreach (string file in asmrefFiles)
                {
                    Debug.Log($"  - {file}");
                }
            }
            
            // Отчет о результатах и возможных действиях
            List<string> suggestedActions = new List<string>();
            
            if (!barracudaDllExists && !pluginExists)
            {
                suggestedActions.Add("Запустите 'Remalux/Fix All Project Issues' для исправления проблем с Barracuda");
            }
            
            if (isAppleSilicon && !hasAppleSiliconSymbol)
            {
                suggestedActions.Add("Добавьте define symbol UNITY_APPLE_SILICON для оптимизаций под Apple Silicon");
            }
            
            if (!hasInputSystemSymbol)
            {
                suggestedActions.Add("Проверьте и установите Input System package если он требуется в проекте");
            }
            
            if (suggestedActions.Count > 0)
            {
                Debug.Log("Рекомендуемые действия:");
                foreach (string action in suggestedActions)
                {
                    Debug.Log($"- {action}");
                }
            }
            else
            {
                Debug.Log("Проект в рабочем состоянии, дополнительных действий не требуется");
            }
        }
        
        private static void FixAppleSiliconIssues()
        {
            if (!IsAppleSiliconPlatform())
            {
                Debug.Log("Not running on Apple Silicon, skipping specific fixes");
                return;
            }
            
            Debug.Log("Applying Apple Silicon specific fixes...");
            
            // Устанавливаем Metal API для улучшения производительности
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneOSX, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneOSX, new UnityEngine.Rendering.GraphicsDeviceType[] { 
                UnityEngine.Rendering.GraphicsDeviceType.Metal 
            });
            
            // Применяем другие оптимизации для Apple Silicon
            PlayerSettings.SetMobileMTRendering(BuildTarget.iOS, true);
            
            Debug.Log("Apple Silicon fixes applied");
        }
        
        private static void CleanProjectCache()
        {
            Debug.Log("Cleaning Unity cache...");
            
            // Запуск команды очистки кэша через Unity API
            UnityEditor.Caching.ClearCache();
            
            // Очистка директории Temp
            string tempDir = Path.Combine(Application.dataPath, "..", "Temp");
            if (Directory.Exists(tempDir))
            {
                try
                {
                    // Удаляем файлы, которые можно удалить без проблем
                    foreach (string file in Directory.GetFiles(tempDir, "*", SearchOption.TopDirectoryOnly))
                    {
                        try { File.Delete(file); } catch { }
                    }
                    Debug.Log("Temp directory cleaned");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Could not clean Temp directory: {e.Message}");
                }
            }
            
            Debug.Log("Cache cleaning completed");
        }
        
        private static void AddAppleSiliconDefines()
        {
            if (!IsAppleSiliconPlatform())
            {
                return;
            }
            
            // Добавляем define symbol для Apple Silicon
            string currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup);
                
            if (!currentDefines.Contains("UNITY_APPLE_SILICON"))
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(
                    EditorUserBuildSettings.selectedBuildTargetGroup,
                    currentDefines + ";UNITY_APPLE_SILICON");
                
                Debug.Log("Added UNITY_APPLE_SILICON define symbol");
            }
        }
        
        private static void RenameBarracudaPluginIfNeeded()
        {
            string pluginPath = Path.Combine(Application.dataPath, "Plugins", "BarracudaFix", "Unity.Barracuda.dll");
            string newPluginPath = Path.Combine(Application.dataPath, "Plugins", "BarracudaFix", "Barracuda.Runtime.dll");
            
            // Если существует старый файл, но нет нового
            if (File.Exists(pluginPath) && !File.Exists(newPluginPath))
            {
                try
                {
                    File.Move(pluginPath, newPluginPath);
                    Debug.Log("Renamed Barracuda plugin to avoid naming conflicts");
                    
                    // Обновляем метафайл, если он существует
                    string metaPath = pluginPath + ".meta";
                    string newMetaPath = newPluginPath + ".meta";
                    
                    if (File.Exists(metaPath) && !File.Exists(newMetaPath))
                    {
                        File.Move(metaPath, newMetaPath);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Could not rename Barracuda plugin: {e.Message}");
                }
            }
        }
        
        private static bool IsAppleSiliconPlatform()
        {
            #if UNITY_EDITOR_OSX
            // Проверка наличия arm64 на macOS
            try
            {
                return SystemInfo.processorType.ToLower().Contains("apple") || 
                       Environment.OSVersion.Platform == PlatformID.MacOSX && 
                       System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64;
            }
            catch
            {
                // Альтернативный способ проверки через родной API
                try
                {
                    string architecture = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "/usr/bin/uname",
                        Arguments = "-m",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    })?.StandardOutput.ReadToEnd()?.Trim();
                    
                    return architecture == "arm64";
                }
                catch
                {
                    // Если оба метода не сработали, проверяем имя процессора
                    return SystemInfo.processorType.Contains("Apple");
                }
            }
            #else
            return false;
            #endif
        }
    }
} 