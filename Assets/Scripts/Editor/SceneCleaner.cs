using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace Remalux.AR
{
      public class SceneCleaner : EditorWindow
      {
            [MenuItem("Tools/Wall Painting/Fix/Clean Scene")]
            public static void CleanScene()
            {
                  Debug.Log("Starting scene cleanup...");

                  // Сначала собираем все объекты, которые нужно удалить
                  var objectsToDestroy = new List<GameObject>();
                  var gameObjects = Object.FindObjectsOfType<GameObject>(true);
                  int removedCount = 0;

                  // Проверяем отсутствующие скрипты
                  foreach (var go in gameObjects)
                  {
                        if (go == null) continue;

                        var components = go.GetComponents<Component>();
                        bool hasMissingScripts = false;

                        foreach (var component in components)
                        {
                              if (component == null)
                              {
                                    hasMissingScripts = true;
                                    removedCount++;
                                    break;
                              }
                        }

                        if (hasMissingScripts)
                        {
                              GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                        }
                  }

                  // Находим дубликаты WallPaintingManager
                  var managers = Object.FindObjectsOfType<WallPaintingManager>(true);
                  if (managers != null && managers.Length > 1)
                  {
                        Debug.Log($"Found {managers.Length} WallPaintingManager instances. Will keep only the first one.");
                        for (int i = 1; i < managers.Length; i++)
                        {
                              if (managers[i] != null && managers[i].gameObject != null)
                              {
                                    objectsToDestroy.Add(managers[i].gameObject);
                              }
                        }
                  }

                  // Находим дубликаты EventSystem
                  var eventSystems = Object.FindObjectsOfType<UnityEngine.EventSystems.EventSystem>(true);
                  if (eventSystems != null && eventSystems.Length > 1)
                  {
                        Debug.Log($"Found {eventSystems.Length} EventSystem instances. Will keep only the first one.");
                        for (int i = 1; i < eventSystems.Length; i++)
                        {
                              if (eventSystems[i] != null && eventSystems[i].gameObject != null)
                              {
                                    objectsToDestroy.Add(eventSystems[i].gameObject);
                              }
                        }
                  }

                  // Находим временные объекты
                  foreach (var go in gameObjects)
                  {
                        if (go == null) continue;

                        if (go.name.Contains("Temp") || go.name.Contains("temp"))
                        {
                              if (!objectsToDestroy.Contains(go))
                              {
                                    objectsToDestroy.Add(go);
                              }
                        }
                  }

                  // Теперь удаляем все собранные объекты
                  int destroyedCount = 0;
                  foreach (var obj in objectsToDestroy)
                  {
                        if (obj != null)
                        {
                              Debug.Log($"Destroying object: {obj.name}");
                              DestroyImmediate(obj);
                              destroyedCount++;
                        }
                  }

                  Debug.Log($"Cleanup complete: Removed {removedCount} missing scripts, destroyed {destroyedCount} objects.");

                  // Сохраняем изменения
                  if (removedCount > 0 || destroyedCount > 0)
                  {
                        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                  }
            }
      }
}