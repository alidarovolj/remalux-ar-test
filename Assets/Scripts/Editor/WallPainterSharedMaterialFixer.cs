using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace Remalux.AR
{
      public static class WallPainterSharedMaterialFixer
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

            [MenuItem("Tools/Wall Painting/Fix/Fix Shared Materials")]
            public static void FixSharedMaterials()
            {
                  Debug.Log("=== Исправление проблем с общими материалами в WallPainter ===");

                  // Находим все объекты на слое "Wall"
                  GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
                  int fixedCount = 0;

                  foreach (GameObject obj in allObjects)
                  {
                        if (obj.layer == 8) // Слой "Wall"
                        {
                              bool wasFixed = FixWallMaterial(obj);
                              if (wasFixed)
                              {
                                    fixedCount++;
                              }
                        }
                  }

                  Debug.Log($"=== Исправлено {fixedCount} объектов стен ===");
            }

            private static bool FixWallMaterial(GameObject wallObject)
            {
                  Debug.Log($"Checking materials for object {wallObject.name}...");
                  bool anyChanges = false;

                  // Check for renderer
                  Renderer renderer = wallObject.GetComponent<Renderer>();
                  if (renderer != null)
                  {
                        // Check if using shared material
                        Material sharedMaterial = renderer.sharedMaterial;
                        if (sharedMaterial != null)
                        {
                              Debug.Log($"  - Current shared material: {sharedMaterial.name}");

                              // Add or update WallMaterialInstanceTracker
                              WallMaterialInstanceTracker tracker = wallObject.GetComponent<WallMaterialInstanceTracker>();
                              if (tracker == null)
                              {
                                    tracker = wallObject.AddComponent<WallMaterialInstanceTracker>();
                                    tracker.OriginalSharedMaterial = sharedMaterial;
                                    Debug.Log("  - Added WallMaterialInstanceTracker component");
                              }

                              // Create and apply material instance if needed
                              if (!sharedMaterial.name.Contains("_Instance_"))
                              {
                                    Material instanceMaterial = new Material(sharedMaterial);
                                    instanceMaterial.name = $"{sharedMaterial.name}_Instance_{wallObject.name}";
                                    renderer.sharedMaterial = instanceMaterial;
                                    tracker.instancedMaterial = instanceMaterial;
                                    Debug.Log($"  - Created and applied material instance: {instanceMaterial.name}");
                                    anyChanges = true;
                              }

                              // Mark objects as dirty
                              EditorUtility.SetDirty(wallObject);
                              EditorUtility.SetDirty(renderer);
                              EditorUtility.SetDirty(tracker);
                        }
                        else
                        {
                              Debug.LogWarning($"  - Object {wallObject.name} has no shared material");
                        }
                  }
                  else
                  {
                        Debug.LogError($"  - Object {wallObject.name} has no Renderer component");
                  }

                  return anyChanges;
            }

            [MenuItem("Tools/Wall Painting/Debug/Check Material Instances")]
            public static void CheckMaterialInstances()
            {
                  Debug.Log("=== Проверка экземпляров материалов для стен ===");

                  // Находим все объекты на слое "Wall"
                  GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
                  int sharedMaterialCount = 0;
                  int instancedMaterialCount = 0;

                  foreach (GameObject obj in allObjects)
                  {
                        if (obj.layer == 8) // Слой "Wall"
                        {
                              Renderer renderer = obj.GetComponent<Renderer>();
                              if (renderer != null)
                              {
                                    Material material = renderer.material;
                                    Material sharedMaterial = renderer.sharedMaterial;

                                    if (material == sharedMaterial)
                                    {
                                          Debug.LogWarning($"  - Объект {obj.name} использует общий материал: {sharedMaterial.name}");
                                          sharedMaterialCount++;
                                    }
                                    else
                                    {
                                          Debug.Log($"  - Объект {obj.name} использует экземпляр материала: {material.name}");
                                          instancedMaterialCount++;
                                    }
                              }
                        }
                  }

                  Debug.Log($"=== Результаты проверки: {sharedMaterialCount} объектов с общими материалами, {instancedMaterialCount} объектов с экземплярами материалов ===");
            }

            [MenuItem("Tools/Wall Painting/Fix/Apply Current Paint Material")]
            public static void ApplyCurrentPaintMaterial()
            {
                  Debug.Log("=== Применение текущего материала покраски ко всем стенам ===");

                  // Находим WallPainter
                  MonoBehaviour[] allComponents = Object.FindObjectsOfType<MonoBehaviour>();
                  MonoBehaviour wallPainter = null;
                  Material currentPaintMaterial = null;

                  foreach (MonoBehaviour component in allComponents)
                  {
                        if (component.GetType().Name == "WallPainter")
                        {
                              wallPainter = component;
                              break;
                        }
                  }

                  if (wallPainter != null)
                  {
                        // Пробуем получить текущий материал для покраски
                        FieldInfo currentPaintMaterialField = wallPainter.GetType().GetField("currentPaintMaterial");
                        if (currentPaintMaterialField != null)
                        {
                              currentPaintMaterial = (Material)currentPaintMaterialField.GetValue(wallPainter);
                              Debug.Log($"  - Текущий материал для покраски: {(currentPaintMaterial != null ? currentPaintMaterial.name : "Не задан")}");
                        }

                        // Если текущий материал не найден, пробуем получить его из доступных материалов
                        if (currentPaintMaterial == null)
                        {
                              // Проверяем разные возможные имена полей для материалов
                              string[] possibleFieldNames = { "availablePaints", "paintMaterials", "materials" };

                              foreach (string fieldName in possibleFieldNames)
                              {
                                    FieldInfo materialsField = wallPainter.GetType().GetField(fieldName);
                                    if (materialsField != null)
                                    {
                                          Material[] materials = (Material[])materialsField.GetValue(wallPainter);
                                          if (materials != null && materials.Length > 0)
                                          {
                                                currentPaintMaterial = materials[0];
                                                Debug.Log($"  - Использую первый доступный материал: {currentPaintMaterial.name}");

                                                // Устанавливаем этот материал как текущий
                                                if (currentPaintMaterialField != null)
                                                {
                                                      currentPaintMaterialField.SetValue(wallPainter, currentPaintMaterial);
                                                      Debug.Log("  - Материал установлен как текущий в WallPainter");
                                                }

                                                break;
                                          }
                                    }
                              }
                        }
                  }

                  // Если материал все еще не найден, используем запасной вариант
                  if (currentPaintMaterial == null)
                  {
                        // Создаем новый материал как запасной вариант
                        currentPaintMaterial = new Material(GetAppropriateShader());
                        currentPaintMaterial.color = Color.white;
                        currentPaintMaterial.name = "Default_Wall_Material";
                        Debug.LogWarning("  - Не удалось получить материал из WallPainter, используется стандартный белый материал");
                  }

                  // Применяем материал ко всем стенам
                  GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
                  int paintedCount = 0;

                  foreach (GameObject obj in allObjects)
                  {
                        if (obj.layer == 8) // Слой "Wall"
                        {
                              Renderer renderer = obj.GetComponent<Renderer>();
                              if (renderer != null)
                              {
                                    // Проверяем, есть ли компонент для отслеживания материалов
                                    WallMaterialInstanceTracker tracker = obj.GetComponent<WallMaterialInstanceTracker>();
                                    if (tracker != null)
                                    {
                                          // Используем метод компонента для применения материала
                                          tracker.ApplyMaterial(currentPaintMaterial);
                                          paintedCount++;
                                    }
                                    else
                                    {
                                          // Создаем экземпляр материала
                                          Material instancedMaterial = new Material(currentPaintMaterial);
                                          instancedMaterial.name = $"{currentPaintMaterial.name}_Instance_{obj.name}";

                                          // Применяем экземпляр материала
                                          renderer.material = instancedMaterial;

                                          // Добавляем компонент для отслеживания
                                          tracker = obj.AddComponent<WallMaterialInstanceTracker>();
                                          tracker.OriginalSharedMaterial = renderer.sharedMaterial;
                                          tracker.instancedMaterial = instancedMaterial;

                                          Debug.Log($"  - Применен материал {instancedMaterial.name} к объекту {obj.name}");
                                          paintedCount++;
                                    }
                              }
                        }
                  }

                  Debug.Log($"=== Покрашено {paintedCount} объектов стен ===");
            }
      }
}