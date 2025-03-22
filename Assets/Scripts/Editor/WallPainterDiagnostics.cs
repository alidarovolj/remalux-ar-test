#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Remalux.AR
{
      public static class WallPainterDiagnostics
      {
            [MenuItem("Tools/Wall Painting/Debug/Diagnose WallPainter")]
            public static void DiagnoseWallPainter()
            {
                  Debug.Log("=== Диагностика компонента WallPainter ===");

                  // Находим все WallPainter в сцене
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
                        Debug.LogError("Не найдено ни одного компонента WallPainter в сцене");
                        return;
                  }

                  Debug.Log($"Найдено {wallPainters.Count} компонентов WallPainter");

                  foreach (MonoBehaviour wallPainter in wallPainters)
                  {
                        DiagnoseComponent(wallPainter);
                  }

                  Debug.Log("=== Диагностика завершена ===");
            }

            private static void DiagnoseComponent(MonoBehaviour component)
            {
                  Debug.Log($"\nДиагностика компонента {component.GetType().Name} на объекте {component.gameObject.name}");

                  // Получаем все поля компонента
                  FieldInfo[] fields = component.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                  Debug.Log($"Найдено {fields.Length} полей");

                  // Ищем поля с материалами
                  foreach (FieldInfo field in fields)
                  {
                        try
                        {
                              object value = field.GetValue(component);
                              if (value != null)
                              {
                                    if (field.FieldType == typeof(Material))
                                    {
                                          Material material = (Material)value;
                                          Debug.Log($"  - Поле материала: {field.Name} = {material.name}");
                                    }
                                    else if (field.FieldType == typeof(Material[]))
                                    {
                                          Material[] materials = (Material[])value;
                                          Debug.Log($"  - Поле массива материалов: {field.Name} = {materials.Length} материалов");
                                          for (int i = 0; i < materials.Length; i++)
                                          {
                                                if (materials[i] != null)
                                                {
                                                      Debug.Log($"    - Материал {i}: {materials[i].name}");
                                                }
                                                else
                                                {
                                                      Debug.Log($"    - Материал {i}: null");
                                                }
                                          }
                                    }
                                    else if (field.FieldType == typeof(Camera))
                                    {
                                          Camera camera = (Camera)value;
                                          Debug.Log($"  - Поле камеры: {field.Name} = {(camera != null ? camera.name : "null")}");
                                    }
                                    else if (field.FieldType == typeof(int) && (field.Name.Contains("Layer") || field.Name.Contains("Mask")))
                                    {
                                          int layerMask = (int)value;
                                          Debug.Log($"  - Поле маски слоя: {field.Name} = {layerMask} (двоичное: {System.Convert.ToString(layerMask, 2)})");
                                    }
                                    else
                                    {
                                          Debug.Log($"  - Другое поле: {field.Name} ({field.FieldType.Name}) = {value}");
                                    }
                              }
                              else
                              {
                                    Debug.Log($"  - Поле: {field.Name} ({field.FieldType.Name}) = null");
                              }
                        }
                        catch (System.Exception e)
                        {
                              Debug.LogError($"Ошибка при получении значения поля {field.Name}: {e.Message}");
                        }
                  }

                  // Получаем все методы компонента
                  MethodInfo[] methods = component.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                  List<MethodInfo> relevantMethods = new List<MethodInfo>();

                  foreach (MethodInfo method in methods)
                  {
                        // Фильтруем только интересующие нас методы
                        if (method.Name.Contains("Paint") || method.Name.Contains("Wall") ||
                            method.Name.Contains("Material") || method.Name.Contains("Hit") ||
                            method.Name.Contains("Raycast"))
                        {
                              relevantMethods.Add(method);
                        }
                  }

                  Debug.Log($"Найдено {relevantMethods.Count} релевантных методов из {methods.Length} общих");
                  foreach (MethodInfo method in relevantMethods)
                  {
                        System.Text.StringBuilder paramInfo = new System.Text.StringBuilder();
                        ParameterInfo[] parameters = method.GetParameters();
                        foreach (ParameterInfo param in parameters)
                        {
                              if (paramInfo.Length > 0)
                                    paramInfo.Append(", ");
                              paramInfo.Append($"{param.ParameterType.Name} {param.Name}");
                        }
                        Debug.Log($"  - Метод: {method.ReturnType.Name} {method.Name}({paramInfo})");
                  }

                  // Проверяем наличие компонентов для отслеживания материалов
                  WallMaterialInstanceTracker instanceTracker = component.GetComponent<WallMaterialInstanceTracker>();
                  if (instanceTracker != null)
                  {
                        Debug.Log("  - Found WallMaterialInstanceTracker component");
                        Debug.Log($"    - Original material: {(instanceTracker.OriginalSharedMaterial != null ? instanceTracker.OriginalSharedMaterial.name : "null")}");
                        Debug.Log($"    - Current material: {(instanceTracker.instancedMaterial != null ? instanceTracker.instancedMaterial.name : "null")}");
                  }
                  else
                  {
                        Debug.Log("  - WallMaterialInstanceTracker component not found");
                  }

                  // Проверяем наличие объектов на слое Wall
                  int wallLayerCount = 0;
                  GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
                  foreach (GameObject obj in allObjects)
                  {
                        if (obj.layer == 8) // Слой "Wall"
                        {
                              wallLayerCount++;
                        }
                  }
                  Debug.Log($"Найдено {wallLayerCount} объектов на слое Wall (8)");
            }

            [MenuItem("Tools/Wall Painting/Debug/Test All Materials")]
            public static void TestAllMaterials()
            {
                  Debug.Log("=== Тестирование всех материалов на стенах ===");

                  // Находим все объекты на слое "Wall"
                  GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
                  List<GameObject> wallObjects = new List<GameObject>();

                  foreach (GameObject obj in allObjects)
                  {
                        if (obj.layer == 8) // Слой "Wall"
                        {
                              wallObjects.Add(obj);
                        }
                  }

                  if (wallObjects.Count == 0)
                  {
                        Debug.LogError("Не найдено ни одного объекта на слое Wall (8)");
                        return;
                  }

                  Debug.Log($"Найдено {wallObjects.Count} объектов на слое Wall");

                  // Находим все материалы в проекте
                  string[] materialGuids = AssetDatabase.FindAssets("t:Material");
                  List<Material> allMaterials = new List<Material>();

                  foreach (string guid in materialGuids)
                  {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                        if (material != null)
                        {
                              allMaterials.Add(material);
                        }
                  }

                  Debug.Log($"Найдено {allMaterials.Count} материалов в проекте");

                  // Тестируем каждый материал на первой стене
                  GameObject testWall = wallObjects[0];
                  Renderer renderer = testWall.GetComponent<Renderer>();

                  if (renderer != null)
                  {
                        Material originalMaterial = renderer.sharedMaterial;
                        Debug.Log($"Тестирование материалов на объекте {testWall.name}");
                        Debug.Log($"Оригинальный материал: {(originalMaterial != null ? originalMaterial.name : "null")}");

                        foreach (Material material in allMaterials)
                        {
                              try
                              {
                                    // Создаем экземпляр материала
                                    Material instancedMaterial = new Material(material);
                                    instancedMaterial.name = $"{material.name}_Test_Instance";

                                    // Применяем материал
                                    renderer.material = instancedMaterial;
                                    Debug.Log($"Применен материал: {material.name} -> {instancedMaterial.name}");

                                    // Небольшая пауза для обновления редактора
                                    System.Threading.Thread.Sleep(100);
                              }
                              catch (System.Exception e)
                              {
                                    Debug.LogError($"Ошибка при применении материала {material.name}: {e.Message}");
                              }
                        }

                        // Восстанавливаем оригинальный материал
                        renderer.sharedMaterial = originalMaterial;
                        Debug.Log($"Восстановлен оригинальный материал: {(originalMaterial != null ? originalMaterial.name : "null")}");
                  }
                  else
                  {
                        Debug.LogError($"Объект {testWall.name} не имеет компонента Renderer");
                  }

                  Debug.Log("=== Тестирование материалов завершено ===");
            }
      }
}
#endif