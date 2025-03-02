using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace Remalux.AR
{
      public static class WallPainterFixer
      {
            private const int WALL_LAYER = 8; // Слой "Wall" имеет индекс 8

            [MenuItem("Tools/Wall Painting/Fix/Fix All WallPainters")]
            public static void FixAllWallPainters()
            {
                  Debug.Log("=== Начало исправления всех компонентов WallPainter ===");

                  MonoBehaviour[] allComponents = Object.FindObjectsOfType<MonoBehaviour>();
                  int fixedCount = 0;

                  foreach (MonoBehaviour component in allComponents)
                  {
                        if (component.GetType().Name == "WallPainter")
                        {
                              bool wasFixed = FixWallPainter(component);
                              if (wasFixed)
                              {
                                    fixedCount++;
                              }
                        }
                  }

                  Debug.Log($"=== Исправлено {fixedCount} компонентов WallPainter ===");
            }

            [MenuItem("Tools/Wall Painting/Fix/Fix TempWallPainter")]
            public static void FixTempWallPainter()
            {
                  Debug.Log("=== Поиск и исправление TempWallPainter ===");

                  MonoBehaviour[] allComponents = Object.FindObjectsOfType<MonoBehaviour>();
                  bool found = false;

                  foreach (MonoBehaviour component in allComponents)
                  {
                        if (component.GetType().Name == "WallPainter" && component.gameObject.name == "TempWallPainter")
                        {
                              bool wasFixed = FixWallPainter(component);
                              found = true;

                              if (wasFixed)
                              {
                                    Debug.Log("TempWallPainter был успешно исправлен.");
                              }
                              else
                              {
                                    Debug.LogWarning("Не удалось полностью исправить TempWallPainter.");
                              }

                              break;
                        }
                  }

                  if (!found)
                  {
                        Debug.LogWarning("TempWallPainter не найден в сцене.");
                  }
            }

            [MenuItem("Tools/Wall Painting/Fix/Copy Settings From Main WallPainter")]
            public static void CopySettingsFromMainWallPainter()
            {
                  Debug.Log("=== Копирование настроек с основного WallPainter на TempWallPainter ===");

                  MonoBehaviour mainWallPainter = null;
                  MonoBehaviour tempWallPainter = null;

                  // Находим оба компонента
                  MonoBehaviour[] allComponents = Object.FindObjectsOfType<MonoBehaviour>();
                  foreach (MonoBehaviour component in allComponents)
                  {
                        if (component.GetType().Name == "WallPainter")
                        {
                              if (component.gameObject.name == "WallPainter")
                              {
                                    mainWallPainter = component;
                              }
                              else if (component.gameObject.name == "TempWallPainter")
                              {
                                    tempWallPainter = component;
                              }
                        }
                  }

                  if (mainWallPainter == null)
                  {
                        Debug.LogError("Основной WallPainter не найден в сцене.");
                        return;
                  }

                  if (tempWallPainter == null)
                  {
                        Debug.LogError("TempWallPainter не найден в сцене.");
                        return;
                  }

                  // Копируем настройки
                  CopyFieldValue(mainWallPainter, tempWallPainter, "wallLayerMask");
                  CopyFieldValue(mainWallPainter, tempWallPainter, "mainCamera");
                  CopyFieldValue(mainWallPainter, tempWallPainter, "availablePaints");

                  // Помечаем объект как измененный
                  EditorUtility.SetDirty(tempWallPainter);

                  Debug.Log("Настройки успешно скопированы с основного WallPainter на TempWallPainter.");
            }

            private static bool FixWallPainter(MonoBehaviour wallPainter)
            {
                  Debug.Log($"Исправление WallPainter на объекте {wallPainter.gameObject.name}...");
                  bool anyChanges = false;

                  // Исправляем маску слоя
                  FieldInfo wallLayerMaskField = wallPainter.GetType().GetField("wallLayerMask");
                  if (wallLayerMaskField != null)
                  {
                        LayerMask currentMask = (LayerMask)wallLayerMaskField.GetValue(wallPainter);
                        if (currentMask.value != (1 << WALL_LAYER))
                        {
                              // Создаем маску, которая включает только слой "Wall"
                              LayerMask newMask = 1 << WALL_LAYER;
                              wallLayerMaskField.SetValue(wallPainter, newMask);
                              anyChanges = true;
                              Debug.Log($"  - Исправлена маска слоя. Новое значение: {newMask.value}");
                        }
                  }

                  // Проверяем и исправляем камеру
                  FieldInfo mainCameraField = wallPainter.GetType().GetField("mainCamera");
                  if (mainCameraField != null)
                  {
                        Camera camera = (Camera)mainCameraField.GetValue(wallPainter);
                        if (camera == null)
                        {
                              // Пытаемся найти основную камеру
                              Camera mainCamera = Camera.main;
                              if (mainCamera != null)
                              {
                                    mainCameraField.SetValue(wallPainter, mainCamera);
                                    anyChanges = true;
                                    Debug.Log($"  - Установлена основная камера: {mainCamera.gameObject.name}");
                              }
                              else
                              {
                                    Debug.LogError("  - Не удалось найти основную камеру в сцене.");
                              }
                        }
                  }

                  // Проверяем и исправляем материалы
                  FieldInfo availablePaintsField = wallPainter.GetType().GetField("availablePaints");
                  if (availablePaintsField != null)
                  {
                        Material[] paints = (Material[])availablePaintsField.GetValue(wallPainter);
                        if (paints == null || paints.Length == 0)
                        {
                              // Пытаемся найти материалы у другого WallPainter
                              MonoBehaviour[] allComponents = Object.FindObjectsOfType<MonoBehaviour>();
                              foreach (MonoBehaviour component in allComponents)
                              {
                                    if (component != wallPainter && component.GetType().Name == "WallPainter")
                                    {
                                          Material[] otherPaints = (Material[])availablePaintsField.GetValue(component);
                                          if (otherPaints != null && otherPaints.Length > 0)
                                          {
                                                availablePaintsField.SetValue(wallPainter, otherPaints);
                                                anyChanges = true;
                                                Debug.Log($"  - Скопированы материалы с другого WallPainter: {component.gameObject.name}");
                                                break;
                                          }
                                    }
                              }

                              if (paints == null || paints.Length == 0)
                              {
                                    Debug.LogWarning("  - Не удалось найти материалы для копирования.");
                              }
                        }
                  }

                  // Если были изменения, помечаем объект как измененный
                  if (anyChanges)
                  {
                        EditorUtility.SetDirty(wallPainter);
                  }

                  return anyChanges;
            }

            private static void CopyFieldValue(MonoBehaviour source, MonoBehaviour target, string fieldName)
            {
                  FieldInfo field = source.GetType().GetField(fieldName);
                  if (field != null)
                  {
                        object value = field.GetValue(source);
                        field.SetValue(target, value);
                        Debug.Log($"  - Скопировано поле {fieldName}");
                  }
            }
      }
}