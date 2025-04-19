using UnityEngine;
using UnityEditor;
using System.IO;

namespace Remalux.Editor
{
    // Этот класс загружается на самой ранней стадии запуска Unity и применяет фиксы до компиляции
    [InitializeOnLoad]
    public static class BarracudaRuntimeSettings
    {
        // Путь к файлу Barracuda в Plugins
        private const string PluginPath = "Assets/Plugins/BarracudaFix/Barracuda.Runtime.dll";
        
        static BarracudaRuntimeSettings()
        {
            // Выполняется при загрузке редактора
            Debug.Log("Applying emergency Barracuda fixes...");
            
            // Добавляем define symbols еще до компиляции
            AddDefineSymbols();
            
            // Проверяем наличие плагина Barracuda
            CheckBarracudaPlugin();
            
            // Запрашиваем перезагрузку скриптов
            EditorApplication.delayCall += () => { 
                AssetDatabase.Refresh(); 
                EditorUtility.RequestScriptReload();
            };
        }
        
        private static void AddDefineSymbols()
        {
            string currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup);
                
            // Добавляем все необходимые символы
            string[] requiredSymbols = new string[] {
                "USE_BARRACUDA_INDIRECTLY",
                "FIXED_BUILD_ORDER",
                "UNITY_APPLE_SILICON"
            };
            
            bool needsUpdate = false;
            foreach (string symbol in requiredSymbols)
            {
                if (!currentDefines.Contains(symbol))
                {
                    currentDefines += ";" + symbol;
                    needsUpdate = true;
                }
            }
            
            if (needsUpdate)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(
                    EditorUserBuildSettings.selectedBuildTargetGroup,
                    currentDefines);
                    
                Debug.Log("Updated define symbols: " + currentDefines);
            }
        }
        
        private static void CheckBarracudaPlugin()
        {
            // Проверяем существование файла плагина
            if (!File.Exists(PluginPath))
            {
                Debug.LogWarning("Barracuda plugin not found at: " + PluginPath);
            }
            else
            {
                Debug.Log("Barracuda plugin found at: " + PluginPath);
            }
            
            // Создаем asmref файл, если его нет
            string asmrefPath = "Assets/Editor/barracuda-reference.asmref";
            if (!File.Exists(asmrefPath))
            {
                string asmrefContent = "{\n    \"reference\": \"Unity.Barracuda\"\n}";
                File.WriteAllText(asmrefPath, asmrefContent);
                Debug.Log("Created asmref file at: " + asmrefPath);
            }
        }
    }
} 