#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace Remalux.AR
{
      public static class WallLayerSetup
      {
            private const string WALL_TAG = "Wall";
            private const int WALL_LAYER = 8; // Слой "Wall" имеет индекс 8

            [MenuItem("Tools/Wall Painting/Set Wall Layer for Selected Objects")]
            public static void SetWallLayerForSelectedObjects()
            {
                  GameObject[] selectedObjects = Selection.gameObjects;
                  if (selectedObjects.Length == 0)
                  {
                        Debug.LogWarning("Не выбрано ни одного объекта. Пожалуйста, выберите объекты, которые нужно пометить как стены.");
                        return;
                  }

                  int count = 0;
                  foreach (GameObject obj in selectedObjects)
                  {
                        obj.layer = WALL_LAYER;
                        count++;
                  }

                  Debug.Log($"Установлен слой 'Wall' для {count} объектов.");
            }

            [MenuItem("Tools/Wall Painting/Find and Set Wall Layer for Tagged Objects")]
            public static void FindAndSetWallLayerForTaggedObjects()
            {
                  GameObject[] wallObjects = GameObject.FindGameObjectsWithTag(WALL_TAG);
                  if (wallObjects.Length == 0)
                  {
                        Debug.LogWarning("Не найдено объектов с тегом 'Wall'. Пожалуйста, установите тег 'Wall' для объектов, которые нужно пометить как стены.");
                        return;
                  }

                  int count = 0;
                  foreach (GameObject obj in wallObjects)
                  {
                        obj.layer = WALL_LAYER;
                        count++;
                  }

                  Debug.Log($"Установлен слой 'Wall' для {count} объектов с тегом 'Wall'.");
            }

            [MenuItem("GameObject/Set Wall Layer", false, 20)]
            static void SetWallLayerForSelectedObjectsContextMenu()
            {
                  SetWallLayerForSelectedObjects();
            }

            [MenuItem("GameObject/Set Wall Layer", true)]
            static bool ValidateSetWallLayerForSelectedObjectsContextMenu()
            {
                  return Selection.gameObjects.Length > 0;
            }

            [MenuItem("Tools/Wall Painting/Check Wall Layer Setup")]
            public static void CheckWallLayerSetup()
            {
                  // Проверяем, настроен ли слой "Wall" в проекте
                  string layerName = LayerMask.LayerToName(WALL_LAYER);
                  if (string.IsNullOrEmpty(layerName) || layerName != "Wall")
                  {
                        Debug.LogError($"Слой с индексом {WALL_LAYER} не настроен как 'Wall'. Пожалуйста, настройте слои в Project Settings -> Tags and Layers.");
                        return;
                  }

                  // Проверяем, есть ли объекты на слое "Wall"
                  GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
                  List<GameObject> wallLayerObjects = new List<GameObject>();

                  foreach (GameObject obj in allObjects)
                  {
                        if (obj.layer == WALL_LAYER)
                        {
                              wallLayerObjects.Add(obj);
                        }
                  }

                  if (wallLayerObjects.Count == 0)
                  {
                        Debug.LogWarning("Не найдено объектов на слое 'Wall'. Система покраски стен может не работать корректно.");
                  }
                  else
                  {
                        Debug.Log($"Найдено {wallLayerObjects.Count} объектов на слое 'Wall'.");
                  }

                  // Проверяем настройки WallPainter
                  MonoBehaviour[] allComponents = Object.FindObjectsOfType<MonoBehaviour>();
                  List<MonoBehaviour> wallPainters = new List<MonoBehaviour>();

                  foreach (MonoBehaviour component in allComponents)
                  {
                        if (component.GetType().Name == "WallPainter")
                        {
                              wallPainters.Add(component);
                        }
                  }

                  if (wallPainters.Count == 0)
                  {
                        Debug.LogWarning("Не найден компонент WallPainter в сцене. Система покраски стен может не работать корректно.");
                  }
                  else
                  {
                        foreach (MonoBehaviour wallPainter in wallPainters)
                        {
                              var wallLayerMaskField = wallPainter.GetType().GetField("wallLayerMask");
                              if (wallLayerMaskField != null)
                              {
                                    LayerMask mask = (LayerMask)wallLayerMaskField.GetValue(wallPainter);
                                    if ((mask.value & (1 << WALL_LAYER)) == 0)
                                    {
                                          Debug.LogError($"WallPainter на объекте {wallPainter.gameObject.name} не настроен на слой 'Wall'. Система покраски стен не будет работать корректно.");
                                    }
                                    else
                                    {
                                          Debug.Log($"WallPainter на объекте {wallPainter.gameObject.name} корректно настроен на слой 'Wall'.");
                                    }
                              }
                        }
                  }
            }

            [MenuItem("Tools/Wall Painting/Fix WallPainter Layer Mask")]
            public static void FixWallPainterLayerMask()
            {
                  MonoBehaviour[] allComponents = Object.FindObjectsOfType<MonoBehaviour>();
                  List<MonoBehaviour> wallPainters = new List<MonoBehaviour>();

                  foreach (MonoBehaviour component in allComponents)
                  {
                        if (component.GetType().Name == "WallPainter")
                        {
                              wallPainters.Add(component);
                        }
                  }

                  if (wallPainters.Count == 0)
                  {
                        Debug.LogWarning("Не найден компонент WallPainter в сцене.");
                        return;
                  }

                  int count = 0;
                  foreach (MonoBehaviour wallPainter in wallPainters)
                  {
                        var wallLayerMaskField = wallPainter.GetType().GetField("wallLayerMask");
                        if (wallLayerMaskField != null)
                        {
                              // Создаем маску, которая включает только слой "Wall"
                              LayerMask mask = 1 << WALL_LAYER;
                              wallLayerMaskField.SetValue(wallPainter, mask);
                              count++;
                        }
                  }

                  Debug.Log($"Исправлена маска слоя для {count} компонентов WallPainter.");
            }
      }
}
#endif