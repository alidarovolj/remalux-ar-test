using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;

namespace Remalux.Editor
{
    [InitializeOnLoad]
    public static class BuildOrderFixer
    {
        // Имена сборок, порядок которых нужно исправить
        private static readonly string[] AssembliesInCorrectOrder = new[]
        {
            "Unity.Barracuda.dll",
            "Unity.XR.ARSubsystems.dll",
            "Unity.XR.ARFoundation.dll",
            "Remalux.Core.dll",
            "Remalux.WallDetection.dll"
        };
        
        // Выполняется при загрузке редактора
        static BuildOrderFixer()
        {
            // Используем отложенный вызов, чтобы Unity успела инициализироваться
            EditorApplication.delayCall += FixBuildOrder;
        }
        
        [MenuItem("Tools/Fix Build Order")]
        public static void FixBuildOrder()
        {
            Debug.Log("Fixing build order to prevent circular dependencies...");
            
            try
            {
                // Получаем тип ScriptCompilationPipeline через рефлексию
                Type pipelineType = Type.GetType("UnityEditor.Scripting.ScriptCompilationPipeline, UnityEditor");
                if (pipelineType == null)
                {
                    Debug.LogError("Could not find ScriptCompilationPipeline type");
                    return;
                }
                
                // Получаем приватное статическое поле s_assemblyLoadOrder
                FieldInfo loadOrderField = pipelineType.GetField("s_assemblyLoadOrder", 
                    BindingFlags.NonPublic | BindingFlags.Static);
                
                if (loadOrderField == null)
                {
                    Debug.LogError("Could not find s_assemblyLoadOrder field");
                    return;
                }
                
                // Получаем текущий порядок загрузки сборок
                string[] currentOrder = loadOrderField.GetValue(null) as string[];
                if (currentOrder == null)
                {
                    Debug.LogError("Failed to get assembly load order");
                    return;
                }
                
                // Создаем новый порядок, где важные сборки идут первыми
                // и все в нужном порядке
                string[] newOrder = new string[currentOrder.Length];
                
                // Сначала добавляем приоритетные сборки
                int priorityCount = 0;
                foreach (string assembly in AssembliesInCorrectOrder)
                {
                    for (int i = 0; i < currentOrder.Length; i++)
                    {
                        if (currentOrder[i] == assembly)
                        {
                            newOrder[priorityCount++] = assembly;
                            currentOrder[i] = null; // Отмечаем как обработанную
                            break;
                        }
                    }
                }
                
                // Затем добавляем остальные сборки
                for (int i = 0; i < currentOrder.Length; i++)
                {
                    if (currentOrder[i] != null)
                    {
                        newOrder[priorityCount++] = currentOrder[i];
                    }
                }
                
                // Устанавливаем новый порядок загрузки
                loadOrderField.SetValue(null, newOrder);
                
                Debug.Log("Build order fixed successfully. Please restart Unity for changes to take full effect.");
                
                // Добавляем define symbol для индикации исправленного порядка сборки
                string currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(
                    EditorUserBuildSettings.selectedBuildTargetGroup);
                
                if (!currentDefines.Contains("FIXED_BUILD_ORDER"))
                {
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(
                        EditorUserBuildSettings.selectedBuildTargetGroup,
                        currentDefines + ";FIXED_BUILD_ORDER");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error fixing build order: {e.Message}\n{e.StackTrace}");
            }
        }
    }
} 