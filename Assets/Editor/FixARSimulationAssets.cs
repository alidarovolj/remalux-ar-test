using UnityEngine;
using UnityEditor;
using System.IO;

namespace Remalux.AR.Editor
{
    /// <summary>
    /// Инструмент для исправления проблем с ассетами симуляции AR Foundation
    /// </summary>
    public static class FixARSimulationAssets
    {
        private const string PackagePrefabPath = "Packages/com.unity.xr.arfoundation/Assets/Prefabs/DefaultSimulationEnvironment.prefab";
        private const string LocalPrefabPath = "Assets/Prefabs/Simulation/DefaultSimulationEnvironment.prefab";
        private const string SimulationSettingsPath = "Assets/XR/UserSimulationSettings/SimulationEnvironmentAssetsManager.asset";

        [MenuItem("Remalux/Инструменты/Исправить ассеты симуляции AR")]
        public static void FixSimulationAssets()
        {
            // Создаем директорию, если она не существует
            string directory = Path.GetDirectoryName(LocalPrefabPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Копируем префаб из пакета в локальную директорию
            bool prefabCopied = AssetDatabase.CopyAsset(PackagePrefabPath, LocalPrefabPath);
            if (prefabCopied)
            {
                Debug.Log($"Префаб симуляции успешно скопирован в {LocalPrefabPath}");
                
                // Обновляем настройки симуляции
                UpdateSimulationSettings();
            }
            else
            {
                Debug.LogError($"Не удалось скопировать префаб из {PackagePrefabPath} в {LocalPrefabPath}");
            }
            
            AssetDatabase.Refresh();
        }

        private static void UpdateSimulationSettings()
        {
            // Загружаем настройки симуляции
            var settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(SimulationSettingsPath);
            if (settings == null)
            {
                Debug.LogError($"Не удалось найти настройки симуляции по пути {SimulationSettingsPath}");
                return;
            }

            // Получаем доступ к полю через SerializedObject
            SerializedObject serializedSettings = new SerializedObject(settings);
            SerializedProperty pathsProperty = serializedSettings.FindProperty("m_EnvironmentPrefabPaths");
            
            if (pathsProperty != null && pathsProperty.isArray)
            {
                // Очищаем существующие пути
                pathsProperty.ClearArray();
                
                // Добавляем новый путь
                pathsProperty.arraySize = 1;
                SerializedProperty element = pathsProperty.GetArrayElementAtIndex(0);
                element.stringValue = LocalPrefabPath;
                
                // Применяем изменения
                serializedSettings.ApplyModifiedProperties();
                Debug.Log("Настройки симуляции успешно обновлены");
            }
            else
            {
                Debug.LogError("Не удалось найти свойство m_EnvironmentPrefabPaths в настройках симуляции");
            }
        }
    }
} 