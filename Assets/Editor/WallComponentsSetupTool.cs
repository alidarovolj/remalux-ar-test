#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Remalux.WallPainting;

namespace Remalux.AR
{
    public class WallComponentsSetupTool : EditorWindow
    {
        private bool setupAllWalls = true;
        private bool setupSelectedOnly = false;
        private Material defaultWallMaterial;

        [MenuItem("Remalux/Инструменты/Настроить компоненты стен")]
        public static void ShowWindow()
        {
            GetWindow<WallComponentsSetupTool>("Настройка компонентов стен");
        }

        private void OnEnable()
        {
            // Попытка найти материал по умолчанию
            string[] guids = AssetDatabase.FindAssets("t:Material WallMaterial");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                defaultWallMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Настройка компонентов стен", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("Этот инструмент добавит компонент WallMaterialInstanceTracker ко всем объектам на слое Wall.", MessageType.Info);
            EditorGUILayout.Space();

            defaultWallMaterial = EditorGUILayout.ObjectField("Материал стены по умолчанию", defaultWallMaterial, typeof(Material), false) as Material;

            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            setupAllWalls = EditorGUILayout.ToggleLeft("Настроить все объекты на слое Wall", setupAllWalls);
            setupSelectedOnly = EditorGUILayout.ToggleLeft("Настроить только выбранные объекты", setupSelectedOnly);
            EditorGUILayout.EndVertical();

            if (setupAllWalls && setupSelectedOnly)
            {
                setupAllWalls = !setupSelectedOnly;
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Настроить компоненты стен"))
            {
                SetupWallComponents();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Примечание: Убедитесь, что слой 'Wall' создан и назначен объектам, которые вы хотите красить.", MessageType.Warning);
        }

        private void SetupWallComponents()
        {
            // Проверяем, существует ли слой Wall
            int wallLayer = LayerMask.NameToLayer("Wall");
            if (wallLayer == -1)
            {
                EditorUtility.DisplayDialog("Ошибка", "Слой 'Wall' не найден в настройках проекта. Сначала создайте этот слой.", "OK");
                return;
            }

            List<GameObject> wallObjects = new List<GameObject>();

            if (setupAllWalls)
            {
                // Находим все объекты на слое Wall в сцене
                GameObject[] allObjects = FindObjectsOfType<GameObject>();
                foreach (GameObject obj in allObjects)
                {
                    if (obj.layer == wallLayer)
                    {
                        wallObjects.Add(obj);
                    }
                }
            }
            else if (setupSelectedOnly)
            {
                // Используем только выбранные объекты
                GameObject[] selectedObjects = Selection.gameObjects;
                foreach (GameObject obj in selectedObjects)
                {
                    if (obj.layer == wallLayer)
                    {
                        wallObjects.Add(obj);
                    }
                    else
                    {
                        Debug.LogWarning($"Объект {obj.name} не находится на слое Wall и будет пропущен.");
                    }
                }
            }

            if (wallObjects.Count == 0)
            {
                EditorUtility.DisplayDialog("Информация", "Не найдено объектов на слое Wall для настройки.", "OK");
                return;
            }

            int objectsProcessed = 0;
            int trackersAdded = 0;
            int collidersAdded = 0;

            foreach (GameObject wallObj in wallObjects)
            {
                Undo.RecordObject(wallObj, "Setup Wall Components");

                // Проверяем наличие Renderer
                Renderer renderer = wallObj.GetComponent<Renderer>();
                if (renderer == null)
                {
                    Debug.LogWarning($"Объект {wallObj.name} не имеет компонента Renderer. Он не будет отображаться при покраске.");
                }

                // Проверяем наличие Collider
                Collider collider = wallObj.GetComponent<Collider>();
                if (collider == null)
                {
                    // Добавляем BoxCollider
                    Undo.AddComponent<BoxCollider>(wallObj);
                    collidersAdded++;
                    Debug.Log($"Добавлен BoxCollider к объекту {wallObj.name}");
                }

                // Проверяем наличие WallMaterialInstanceTracker
                WallMaterialInstanceTracker tracker = wallObj.GetComponent<WallMaterialInstanceTracker>();
                if (tracker == null)
                {
                    tracker = Undo.AddComponent<WallMaterialInstanceTracker>(wallObj);
                    trackersAdded++;
                    Debug.Log($"Добавлен WallMaterialInstanceTracker к объекту {wallObj.name}");
                }

                // Настраиваем WallMaterialInstanceTracker
                if (renderer != null && defaultWallMaterial != null)
                {
                    tracker.OriginalSharedMaterial = defaultWallMaterial;

                    // Создаем экземпляр материала
                    Material instanceMaterial = new Material(defaultWallMaterial);
                    instanceMaterial.name = $"{defaultWallMaterial.name}_Instance_{wallObj.name}";
                    renderer.sharedMaterial = instanceMaterial;

                    Debug.Log($"Настроен материал для объекта {wallObj.name}");
                }

                objectsProcessed++;
            }

            EditorUtility.DisplayDialog("Готово",
                $"Обработано {objectsProcessed} объектов.\n" +
                $"Добавлено {trackersAdded} компонентов WallMaterialInstanceTracker.\n" +
                $"Добавлено {collidersAdded} компонентов Collider.",
                "OK");
        }
    }
}
#endif