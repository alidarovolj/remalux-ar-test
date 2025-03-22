using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

namespace Remalux.Editor
{
    /// <summary>
    /// Инструмент для настройки системы покраски стен без использования AR
    /// </summary>
    public static class FixSimulationAssets
    {
        [MenuItem("Remalux/Инструменты/Настроить систему без AR")]
        public static void SetupNonARSystem()
        {
            // Проверяем наличие сцены
            string scenePath = EditorUtility.OpenFilePanel("Выберите сцену для настройки", "Assets", "unity");
            if (string.IsNullOrEmpty(scenePath))
            {
                Debug.Log("Операция отменена пользователем");
                return;
            }

            // Конвертируем абсолютный путь в относительный путь проекта
            string relativePath = "Assets" + scenePath.Substring(Application.dataPath.Length);
            
            // Открываем сцену
            if (!EditorSceneManager.OpenScene(relativePath).IsValid())
            {
                Debug.LogError($"Не удалось открыть сцену по пути {relativePath}");
                return;
            }

            // Удаляем AR компоненты
            RemoveARComponents();
            
            // Настраиваем компоненты компьютерного зрения
            SetupComputerVisionComponents();
            
            // Сохраняем сцену
            EditorSceneManager.SaveOpenScenes();
            
            Debug.Log("Система настроена для работы без AR. Используется только компьютерное зрение.");
        }

        private static void RemoveARComponents()
        {
            // Находим и удаляем AR Session
            var arSessions = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            List<MonoBehaviour> toRemove = new List<MonoBehaviour>();
            
            foreach (var component in arSessions)
            {
                if (component.GetType().Name.Contains("ARSession") || 
                    component.GetType().Name.Contains("ARPlane") ||
                    component.GetType().Name.Contains("ARRaycast"))
                {
                    toRemove.Add(component);
                }
            }
            
            foreach (var component in toRemove)
            {
                Debug.Log($"Удаляем AR компонент: {component.GetType().Name}");
                Object.DestroyImmediate(component);
            }
        }

        private static void SetupComputerVisionComponents()
        {
            // Находим контроллер покраски стен
            var controller = Object.FindAnyObjectByType<Remalux.AR.Vision.RealWallPaintingController>();
            if (controller == null)
            {
                Debug.LogError("Не найден контроллер RealWallPaintingController");
                return;
            }
            
            // Находим или создаем детектор стен на основе компьютерного зрения
            var wallDetector = Object.FindAnyObjectByType<Remalux.AR.Vision.WallDetector>();
            if (wallDetector == null)
            {
                // Создаем новый объект для детектора стен
                GameObject detectorObj = new GameObject("WallDetector");
                wallDetector = detectorObj.AddComponent<Remalux.AR.Vision.WallDetector>();
                Debug.Log("Создан новый детектор стен на основе компьютерного зрения");
            }
            
            // Настраиваем камеру
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("Не найдена основная камера");
                return;
            }
            
            // Убеждаемся, что камера настроена правильно
            mainCamera.clearFlags = CameraClearFlags.Skybox;
            mainCamera.backgroundColor = Color.black;
            
            Debug.Log("Камера настроена для работы с компьютерным зрением");
        }
    }
} 