#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Remalux.WallPainting;

namespace Remalux.AR
{
      public static class WallPainterAutoInitializerMenu
      {
            [MenuItem("Tools/Wall Painting/Fix/Add Auto-Initializer to WallPainters")]
            public static void AddAutoInitializerToWallPainters()
            {
                  Debug.Log("=== Добавление компонента автоинициализации к WallPainter ===");

                  // Находим все WallPainter в сцене
                  MonoBehaviour[] allComponents = Object.FindObjectsOfType<MonoBehaviour>();
                  int addedCount = 0;

                  foreach (MonoBehaviour component in allComponents)
                  {
                        if (component.GetType().Name == "WallPainter")
                        {
                              // Проверяем, есть ли уже компонент автоинициализации
                              WallPainterAutoInitializer existingInitializer = component.GetComponent<WallPainterAutoInitializer>();
                              if (existingInitializer == null)
                              {
                                    // Добавляем компонент автоинициализации
                                    WallPainterAutoInitializer initializer = component.gameObject.AddComponent<WallPainterAutoInitializer>();
                                    Debug.Log($"  - Добавлен компонент WallPainterAutoInitializer к объекту {component.gameObject.name}");
                                    addedCount++;
                              }
                              else
                              {
                                    Debug.Log($"  - Компонент WallPainterAutoInitializer уже существует на объекте {component.gameObject.name}");
                              }
                        }
                  }

                  Debug.Log($"=== Добавлено {addedCount} компонентов автоинициализации ===");
            }

            [MenuItem("Tools/Wall Painting/Fix/Complete WallPainter Setup")]
            public static void CompleteWallPainterSetup()
            {
                  Debug.Log("=== Начало полной настройки WallPainter ===");

                  try
                  {
                        // Инициализируем компоненты
                        WallPainterInitializer.InitializeWallPainters();
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Ошибка при инициализации WallPainter: {e.Message}");
                  }

                  try
                  {
                        // Удаляем временные компоненты
                        WallPainterInitializer.RemoveTempWallPainter();
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Ошибка при удалении временных WallPainter: {e.Message}");
                  }

                  try
                  {
                        // Добавляем компоненты автоинициализации
                        AddAutoInitializerToWallPainters();
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Ошибка при добавлении компонентов автоинициализации: {e.Message}");
                  }

                  try
                  {
                        // Проверяем, установлен ли currentPaintMaterial
                        EnsureCurrentPaintMaterialIsSet();
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Ошибка при проверке currentPaintMaterial: {e.Message}");
                  }

                  try
                  {
                        // Применяем текущий материал ко всем стенам
                        WallPainterSharedMaterialFixer.ApplyCurrentPaintMaterial();
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Ошибка при применении текущего материала: {e.Message}");
                  }

                  try
                  {
                        // Исправляем общие материалы
                        FixSharedMaterials();
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Ошибка при исправлении общих материалов: {e.Message}");
                  }

                  Debug.Log("=== Настройка WallPainter завершена ===");
            }

            private static void EnsureCurrentPaintMaterialIsSet()
            {
                  Debug.Log("=== Проверка установки currentPaintMaterial ===");

                  // Находим WallPainter
                  MonoBehaviour[] allComponents = Object.FindObjectsOfType<MonoBehaviour>();
                  int fixedCount = 0;

                  foreach (MonoBehaviour component in allComponents)
                  {
                        if (component.GetType().Name == "WallPainter")
                        {
                              System.Type wallPainterType = component.GetType();

                              // Проверяем currentPaintMaterial
                              System.Reflection.FieldInfo currentPaintMaterialField = wallPainterType.GetField("currentPaintMaterial");
                              if (currentPaintMaterialField != null)
                              {
                                    Material currentMaterial = (Material)currentPaintMaterialField.GetValue(component);
                                    if (currentMaterial == null)
                                    {
                                          // Пробуем получить материалы из availablePaints
                                          System.Reflection.FieldInfo availablePaintsField = wallPainterType.GetField("availablePaints");
                                          if (availablePaintsField != null)
                                          {
                                                Material[] availablePaints = (Material[])availablePaintsField.GetValue(component);
                                                if (availablePaints != null && availablePaints.Length > 0)
                                                {
                                                      // Используем первый доступный материал
                                                      currentPaintMaterialField.SetValue(component, availablePaints[0]);
                                                      Debug.Log($"  - Установлен currentPaintMaterial = {availablePaints[0].name} для {component.gameObject.name}");
                                                      fixedCount++;

                                                      // Вызываем метод SelectPaintMaterial для инициализации
                                                      System.Reflection.MethodInfo selectPaintMaterialMethod = wallPainterType.GetMethod("SelectPaintMaterial");
                                                      if (selectPaintMaterialMethod != null)
                                                      {
                                                            try
                                                            {
                                                                  selectPaintMaterialMethod.Invoke(component, new object[] { 0 });
                                                                  Debug.Log($"  - Вызван метод SelectPaintMaterial(0) для {component.gameObject.name}");
                                                            }
                                                            catch (System.Exception e)
                                                            {
                                                                  Debug.LogError($"  - Ошибка при вызове метода SelectPaintMaterial: {e.Message}");
                                                            }
                                                      }
                                                }
                                                else
                                                {
                                                      Debug.LogWarning($"  - Не найдены доступные материалы для {component.gameObject.name}");
                                                }
                                          }
                                          else
                                          {
                                                Debug.LogWarning($"  - Не найдено поле availablePaints для {component.gameObject.name}");
                                          }
                                    }
                                    else
                                    {
                                          Debug.Log($"  - currentPaintMaterial уже установлен: {currentMaterial.name} для {component.gameObject.name}");
                                    }
                              }
                              else
                              {
                                    Debug.LogWarning($"  - Не найдено поле currentPaintMaterial для {component.gameObject.name}");
                              }
                        }
                  }

                  Debug.Log($"=== Исправлено {fixedCount} компонентов WallPainter ===");
            }

            private static void FixSharedMaterials()
            {
                  Debug.Log("=== Исправление общих материалов на стенах ===");

                  // Находим все объекты на слое "Wall"
                  GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
                  int fixedCount = 0;

                  foreach (GameObject obj in allObjects)
                  {
                        if (obj.layer == 8) // Слой "Wall"
                        {
                              Renderer renderer = obj.GetComponent<Renderer>();
                              if (renderer != null)
                              {
                                    Material sharedMaterial = renderer.sharedMaterial;
                                    if (sharedMaterial != null)
                                    {
                                          // Проверяем, есть ли уже компонент для отслеживания материалов
                                          WallMaterialInstanceTracker tracker = obj.GetComponent<WallMaterialInstanceTracker>();
                                          if (tracker == null)
                                          {
                                                // Добавляем компонент для отслеживания
                                                tracker = obj.AddComponent<WallMaterialInstanceTracker>();
                                                tracker.OriginalSharedMaterial = sharedMaterial;

                                                // Создаем экземпляр материала
                                                Material instanceMaterial = new Material(sharedMaterial);
                                                instanceMaterial.name = $"{sharedMaterial.name}_Instance_{obj.name}";

                                                // Применяем материал через новый метод
                                                tracker.SetInstancedMaterial(instanceMaterial, true);

                                                Debug.Log($"  - Fixed material for object {obj.name}: {sharedMaterial.name} -> {instanceMaterial.name}");
                                                fixedCount++;
                                          }
                                          else
                                          {
                                                // Обновляем экземпляр материала
                                                tracker.UpdateMaterialInstance();
                                          }
                                    }
                              }
                        }
                  }

                  Debug.Log($"=== Исправлено {fixedCount} объектов стен ===");
            }

            /// <summary>
            /// Безопасно применяет материал к объекту, создавая экземпляр материала
            /// </summary>
            private static void ApplyMaterialDirectly(GameObject wallObject, Material material)
            {
                  if (wallObject == null || material == null)
                        return;

                  // Проверяем наличие компонента WallMaterialInstanceTracker
                  WallMaterialInstanceTracker tracker = wallObject.GetComponent<WallMaterialInstanceTracker>();
                  if (tracker == null)
                  {
                        // Если компонента нет, добавляем его
                        tracker = wallObject.AddComponent<WallMaterialInstanceTracker>();
                        Debug.Log($"Добавлен компонент WallMaterialInstanceTracker к объекту {wallObject.name}");
                  }

                  // Применяем материал через трекер
                  tracker.ApplyMaterial(material);
            }
      }
}
#endif
