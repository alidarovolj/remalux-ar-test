using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace Remalux.AR
{
      public static class WallPainterInitializer
      {
            /// <summary>
            /// Возвращает подходящий шейдер в зависимости от используемого рендер пайплайна
            /// </summary>
            private static Shader GetAppropriateShader()
            {
                  if (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null)
                  {
                        // Для URP
                        Debug.Log("Используется URP, возвращаем URP шейдер");
                        return Shader.Find("Universal Render Pipeline/Lit");
                  }
                  else
                  {
                        // Для стандартного рендер пайплайна
                        return Shader.Find("Standard");
                  }
            }

            [MenuItem("Tools/Wall Painting/Fix/Initialize WallPainter")]
            public static void InitializeWallPainters()
            {
                  Debug.Log("=== Инициализация компонентов WallPainter ===");

                  // Находим все WallPainter в сцене
                  MonoBehaviour[] allComponents = Object.FindObjectsOfType<MonoBehaviour>();
                  int initializedCount = 0;

                  foreach (MonoBehaviour component in allComponents)
                  {
                        if (component.GetType().Name == "WallPainter")
                        {
                              bool wasInitialized = InitializeWallPainter(component);
                              if (wasInitialized)
                              {
                                    initializedCount++;
                              }
                        }
                  }

                  Debug.Log($"=== Инициализировано {initializedCount} компонентов WallPainter ===");
            }

            private static bool InitializeWallPainter(MonoBehaviour wallPainter)
            {
                  Debug.Log($"Инициализация WallPainter на объекте {wallPainter.gameObject.name}...");
                  bool anyChanges = false;

                  // Получаем тип компонента
                  System.Type wallPainterType = wallPainter.GetType();

                  // Проверяем и инициализируем currentPaintMaterial
                  FieldInfo currentPaintMaterialField = wallPainterType.GetField("currentPaintMaterial");
                  if (currentPaintMaterialField != null)
                  {
                        Material currentMaterial = (Material)currentPaintMaterialField.GetValue(wallPainter);
                        if (currentMaterial == null)
                        {
                              // Пробуем получить материалы из availablePaints
                              FieldInfo availablePaintsField = wallPainterType.GetField("availablePaints");
                              if (availablePaintsField != null)
                              {
                                    Material[] availablePaints = (Material[])availablePaintsField.GetValue(wallPainter);
                                    if (availablePaints != null && availablePaints.Length > 0)
                                    {
                                          // Используем первый доступный материал
                                          currentPaintMaterialField.SetValue(wallPainter, availablePaints[0]);
                                          Debug.Log($"  - Установлен currentPaintMaterial = {availablePaints[0].name}");
                                          anyChanges = true;
                                    }
                              }
                        }
                        else
                        {
                              Debug.Log($"  - currentPaintMaterial уже установлен: {currentMaterial.name}");
                        }
                  }

                  // Проверяем и инициализируем defaultMaterial
                  FieldInfo defaultMaterialField = wallPainterType.GetField("defaultMaterial");
                  if (defaultMaterialField != null)
                  {
                        Material defaultMaterial = (Material)defaultMaterialField.GetValue(wallPainter);
                        if (defaultMaterial == null)
                        {
                              // Создаем стандартный материал
                              Material newDefaultMaterial = new Material(GetAppropriateShader());
                              newDefaultMaterial.color = Color.white;
                              newDefaultMaterial.name = "DefaultWallMaterial";

                              defaultMaterialField.SetValue(wallPainter, newDefaultMaterial);
                              Debug.Log($"  - Создан и установлен defaultMaterial");
                              anyChanges = true;
                        }
                        else
                        {
                              Debug.Log($"  - defaultMaterial уже установлен: {defaultMaterial.name}");
                        }
                  }

                  // Проверяем и инициализируем mainCamera
                  FieldInfo mainCameraField = wallPainterType.GetField("mainCamera");
                  if (mainCameraField != null)
                  {
                        Camera mainCamera = (Camera)mainCameraField.GetValue(wallPainter);
                        if (mainCamera == null)
                        {
                              // Ищем главную камеру
                              Camera camera = Camera.main;
                              if (camera != null)
                              {
                                    mainCameraField.SetValue(wallPainter, camera);
                                    Debug.Log($"  - Установлена mainCamera = {camera.name}");
                                    anyChanges = true;
                              }
                              else
                              {
                                    Debug.LogWarning("  - Не удалось найти главную камеру");
                              }
                        }
                        else
                        {
                              Debug.Log($"  - mainCamera уже установлена: {mainCamera.name}");
                        }
                  }

                  // Проверяем и инициализируем wallLayerMask
                  FieldInfo wallLayerMaskField = wallPainterType.GetField("wallLayerMask");
                  if (wallLayerMaskField != null)
                  {
                        try
                        {
                              // Проверяем тип поля
                              if (wallLayerMaskField.FieldType == typeof(int))
                              {
                                    int wallLayerMask = (int)wallLayerMaskField.GetValue(wallPainter);
                                    if (wallLayerMask == 0)
                                    {
                                          // Устанавливаем маску для слоя Wall (8)
                                          int newLayerMask = 1 << 8;
                                          wallLayerMaskField.SetValue(wallPainter, newLayerMask);
                                          Debug.Log($"  - Установлен wallLayerMask (int) для слоя Wall (8)");
                                          anyChanges = true;
                                    }
                                    else
                                    {
                                          Debug.Log($"  - wallLayerMask (int) уже установлен: {wallLayerMask}");
                                    }
                              }
                              else if (wallLayerMaskField.FieldType == typeof(LayerMask))
                              {
                                    LayerMask layerMask = (LayerMask)wallLayerMaskField.GetValue(wallPainter);
                                    int maskValue = layerMask.value;
                                    if (maskValue == 0)
                                    {
                                          // Устанавливаем маску для слоя Wall (8)
                                          LayerMask newLayerMask = 1 << 8;
                                          wallLayerMaskField.SetValue(wallPainter, newLayerMask);
                                          Debug.Log($"  - Установлен wallLayerMask (LayerMask) для слоя Wall (8)");
                                          anyChanges = true;
                                    }
                                    else
                                    {
                                          Debug.Log($"  - wallLayerMask (LayerMask) уже установлен: {maskValue}");
                                    }
                              }
                              else
                              {
                                    Debug.LogWarning($"  - Поле wallLayerMask имеет неожиданный тип: {wallLayerMaskField.FieldType.Name}");
                              }
                        }
                        catch (System.Exception e)
                        {
                              Debug.LogError($"  - Ошибка при работе с wallLayerMask: {e.Message}");
                        }
                  }

                  // Вызываем метод SelectPaintMaterial для инициализации
                  MethodInfo selectPaintMaterialMethod = wallPainterType.GetMethod("SelectPaintMaterial");
                  if (selectPaintMaterialMethod != null)
                  {
                        try
                        {
                              selectPaintMaterialMethod.Invoke(wallPainter, new object[] { 0 });
                              Debug.Log("  - Вызван метод SelectPaintMaterial(0) для инициализации");
                              anyChanges = true;
                        }
                        catch (System.Exception e)
                        {
                              Debug.LogError($"  - Ошибка при вызове метода SelectPaintMaterial: {e.Message}");
                        }
                  }

                  return anyChanges;
            }

            [MenuItem("Tools/Wall Painting/Fix/Remove TempWallPainter")]
            public static void RemoveTempWallPainter()
            {
                  Debug.Log("=== Поиск и удаление временных компонентов WallPainter ===");

                  // Находим все WallPainter в сцене
                  MonoBehaviour[] allComponents = Object.FindObjectsOfType<MonoBehaviour>();
                  int removedCount = 0;

                  foreach (MonoBehaviour component in allComponents)
                  {
                        if (component.GetType().Name == "WallPainter" &&
                            component.gameObject.name.Contains("Temp"))
                        {
                              Debug.Log($"Удаление временного WallPainter на объекте {component.gameObject.name}");
                              Object.DestroyImmediate(component);
                              removedCount++;
                        }
                  }

                  Debug.Log($"=== Удалено {removedCount} временных компонентов WallPainter ===");
            }

            [MenuItem("Tools/Wall Painting/Fix/Fix and Test WallPainter")]
            public static void FixAndTestWallPainter()
            {
                  // Инициализируем компоненты
                  InitializeWallPainters();

                  // Удаляем временные компоненты
                  RemoveTempWallPainter();

                  // Применяем текущий материал ко всем стенам
                  WallPainterSharedMaterialFixer.ApplyCurrentPaintMaterial();

                  Debug.Log("=== WallPainter исправлен и протестирован ===");
            }
      }
}