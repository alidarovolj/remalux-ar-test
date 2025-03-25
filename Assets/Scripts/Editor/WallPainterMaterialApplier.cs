#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Remalux.WallPainting;

namespace Remalux.AR
{
      [CustomEditor(typeof(MonoBehaviour), true)]
      public class WallPainterMaterialApplier : Editor
      {
            private bool isWallPainter = false;
            private MonoBehaviour wallPainter;
            private FieldInfo availableMaterialsField;
            private FieldInfo currentPaintMaterialField;
            private MethodInfo paintWallAtPositionMethod;
            private MethodInfo handleWallHitMethod;
            private FieldInfo mainCameraField;
            private FieldInfo wallLayerMaskField;

            private Material[] availableMaterials;
            private Material currentPaintMaterial;
            private int selectedMaterialIndex = 0;
            private bool showDebugOptions = false;
            private bool showMaterialOptions = false;

            private void OnEnable()
            {
                  // Проверяем, является ли целевой объект WallPainter
                  wallPainter = target as MonoBehaviour;
                  if (wallPainter != null && wallPainter.GetType().Name == "WallPainter")
                  {
                        isWallPainter = true;
                        Debug.Log("WallPainterMaterialApplier: Обнаружен компонент WallPainter");

                        // Получаем доступ к полям и методам через рефлексию
                        System.Type wallPainterType = wallPainter.GetType();
                        availableMaterialsField = wallPainterType.GetField("availableMaterials");
                        currentPaintMaterialField = wallPainterType.GetField("currentPaintMaterial");
                        paintWallAtPositionMethod = wallPainterType.GetMethod("PaintWallAtPosition");
                        handleWallHitMethod = wallPainterType.GetMethod("HandleWallHit", BindingFlags.NonPublic | BindingFlags.Instance);
                        mainCameraField = wallPainterType.GetField("mainCamera");
                        wallLayerMaskField = wallPainterType.GetField("wallLayerMask");

                        // Получаем доступные материалы
                        if (availableMaterialsField != null)
                        {
                              availableMaterials = availableMaterialsField.GetValue(wallPainter) as Material[];
                              Debug.Log($"WallPainterMaterialApplier: Найдено {(availableMaterials != null ? availableMaterials.Length : 0)} доступных материалов");
                        }

                        // Получаем текущий материал
                        if (currentPaintMaterialField != null)
                        {
                              currentPaintMaterial = currentPaintMaterialField.GetValue(wallPainter) as Material;
                              Debug.Log($"WallPainterMaterialApplier: Текущий материал: {(currentPaintMaterial != null ? currentPaintMaterial.name : "Не задан")}");

                              // Находим индекс текущего материала
                              if (availableMaterials != null && currentPaintMaterial != null)
                              {
                                    for (int i = 0; i < availableMaterials.Length; i++)
                                    {
                                          if (availableMaterials[i] == currentPaintMaterial)
                                          {
                                                selectedMaterialIndex = i;
                                                break;
                                          }
                                    }
                              }
                        }
                  }
            }

            public override void OnInspectorGUI()
            {
                  // Отображаем стандартный инспектор
                  base.OnInspectorGUI();

                  if (!isWallPainter)
                        return;

                  EditorGUILayout.Space();
                  EditorGUILayout.LabelField("Wall Painter Material Applier", EditorStyles.boldLabel);

                  // Отображаем секцию материалов
                  showMaterialOptions = EditorGUILayout.Foldout(showMaterialOptions, "Материалы для покраски");
                  if (showMaterialOptions)
                  {
                        EditorGUI.indentLevel++;

                        // Отображаем доступные материалы
                        if (availableMaterials != null && availableMaterials.Length > 0)
                        {
                              List<string> materialNames = new List<string>();
                              foreach (Material mat in availableMaterials)
                              {
                                    materialNames.Add(mat != null ? mat.name : "Пустой материал");
                              }

                              int newSelectedIndex = EditorGUILayout.Popup("Выбрать материал", selectedMaterialIndex, materialNames.ToArray());
                              if (newSelectedIndex != selectedMaterialIndex)
                              {
                                    selectedMaterialIndex = newSelectedIndex;
                                    currentPaintMaterial = availableMaterials[selectedMaterialIndex];
                                    currentPaintMaterialField.SetValue(wallPainter, currentPaintMaterial);
                                    Debug.Log($"WallPainterMaterialApplier: Выбран материал {currentPaintMaterial.name}");
                              }

                              // Кнопка для применения материала ко всем стенам
                              if (GUILayout.Button("Применить материал ко всем стенам"))
                              {
                                    ApplyCurrentMaterialToAllWalls();
                              }
                        }
                        else
                        {
                              EditorGUILayout.HelpBox("Нет доступных материалов", MessageType.Warning);
                        }

                        EditorGUI.indentLevel--;
                  }

                  // Отображаем отладочные опции
                  showDebugOptions = EditorGUILayout.Foldout(showDebugOptions, "Отладка материалов");
                  if (showDebugOptions)
                  {
                        EditorGUI.indentLevel++;

                        // Кнопка для проверки материалов стен
                        if (GUILayout.Button("Проверить материалы стен"))
                        {
                              CheckWallMaterials();
                        }

                        // Кнопка для исправления общих материалов
                        if (GUILayout.Button("Исправить общие материалы"))
                        {
                              FixSharedMaterials();
                        }

                        // Кнопка для тестирования покраски в центре экрана
                        if (GUILayout.Button("Тест покраски в центре экрана"))
                        {
                              TestPaintAtScreenCenter();
                        }

                        // Кнопка для прямого применения материала к объекту под курсором
                        if (GUILayout.Button("Применить материал под курсором"))
                        {
                              ApplyMaterialUnderCursor();
                        }

                        EditorGUI.indentLevel--;
                  }
            }

            private void ApplyCurrentMaterialToAllWalls()
            {
                  if (currentPaintMaterial == null)
                  {
                        Debug.LogError("WallPainterMaterialApplier: Текущий материал не задан");
                        return;
                  }

                  Debug.Log($"WallPainterMaterialApplier: Применение материала {currentPaintMaterial.name} ко всем стенам...");

                  // Находим все объекты на слое "Wall"
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
                                          tracker.InstancedMaterial = instancedMaterial;
                                    }

                                    paintedCount++;
                              }
                        }
                  }

                  Debug.Log($"WallPainterMaterialApplier: Покрашено {paintedCount} объектов стен");
            }

            private void CheckWallMaterials()
            {
                  Debug.Log("WallPainterMaterialApplier: Проверка материалов стен...");

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

                  Debug.Log($"WallPainterMaterialApplier: Результаты проверки: {sharedMaterialCount} объектов с общими материалами, {instancedMaterialCount} объектов с экземплярами материалов");
            }

            private void FixSharedMaterials()
            {
                  Debug.Log("WallPainterMaterialApplier: Исправление общих материалов...");

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
                                          // Создаем экземпляр материала
                                          Material instancedMaterial = new Material(sharedMaterial);
                                          instancedMaterial.name = $"{sharedMaterial.name}_Instance_{obj.name}";

                                          // Применяем экземпляр материала
                                          renderer.material = instancedMaterial;

                                          // Добавляем компонент для отслеживания
                                          WallMaterialInstanceTracker tracker = obj.GetComponent<WallMaterialInstanceTracker>();
                                          if (tracker == null)
                                          {
                                                tracker = obj.AddComponent<WallMaterialInstanceTracker>();
                                                tracker.OriginalSharedMaterial = sharedMaterial;
                                                tracker.InstancedMaterial = instancedMaterial;
                                          }

                                          fixedCount++;
                                    }
                              }
                        }
                  }

                  Debug.Log($"WallPainterMaterialApplier: Исправлено {fixedCount} объектов стен");
            }

            private void TestPaintAtScreenCenter()
            {
                  if (currentPaintMaterial == null)
                  {
                        Debug.LogError("WallPainterMaterialApplier: Текущий материал не задан");
                        return;
                  }

                  if (paintWallAtPositionMethod == null)
                  {
                        Debug.LogError("WallPainterMaterialApplier: Метод PaintWallAtPosition не найден");
                        return;
                  }

                  // Получаем центр экрана
                  Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
                  Debug.Log($"WallPainterMaterialApplier: Тестирование покраски в центре экрана ({screenCenter.x}, {screenCenter.y})");

                  // Вызываем метод PaintWallAtPosition
                  try
                  {
                        paintWallAtPositionMethod.Invoke(wallPainter, new object[] { screenCenter });
                        Debug.Log("WallPainterMaterialApplier: Метод PaintWallAtPosition вызван успешно");
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"WallPainterMaterialApplier: Ошибка при вызове метода PaintWallAtPosition: {e.Message}");
                  }

                  // Также пробуем напрямую выполнить рейкаст и применить материал
                  Camera mainCamera = mainCameraField.GetValue(wallPainter) as Camera;
                  int wallLayerMask = (int)wallLayerMaskField.GetValue(wallPainter);

                  if (mainCamera != null)
                  {
                        Ray ray = mainCamera.ScreenPointToRay(screenCenter);
                        RaycastHit hit;

                        if (Physics.Raycast(ray, out hit, 100f, wallLayerMask))
                        {
                              Debug.Log($"WallPainterMaterialApplier: Рейкаст попал в объект {hit.collider.gameObject.name} на расстоянии {hit.distance}");

                              // Пробуем вызвать HandleWallHit
                              if (handleWallHitMethod != null)
                              {
                                    try
                                    {
                                          handleWallHitMethod.Invoke(wallPainter, new object[] { hit });
                                          Debug.Log("WallPainterMaterialApplier: Метод HandleWallHit вызван успешно");
                                    }
                                    catch (System.Exception e)
                                    {
                                          Debug.LogError($"WallPainterMaterialApplier: Ошибка при вызове метода HandleWallHit: {e.Message}");
                                    }
                              }

                              // Напрямую применяем материал
                              Renderer renderer = hit.collider.GetComponent<Renderer>();
                              if (renderer != null)
                              {
                                    // Проверяем, есть ли компонент для отслеживания материалов
                                    WallMaterialInstanceTracker tracker = hit.collider.GetComponent<WallMaterialInstanceTracker>();
                                    if (tracker != null)
                                    {
                                          // Используем метод компонента для применения материала
                                          tracker.ApplyMaterial(currentPaintMaterial);
                                    }
                                    else
                                    {
                                          // Создаем экземпляр материала
                                          Material instancedMaterial = new Material(currentPaintMaterial);
                                          instancedMaterial.name = $"{currentPaintMaterial.name}_Instance_{hit.collider.gameObject.name}";

                                          // Применяем экземпляр материала
                                          renderer.material = instancedMaterial;

                                          // Добавляем компонент для отслеживания
                                          tracker = hit.collider.gameObject.AddComponent<WallMaterialInstanceTracker>();
                                          tracker.OriginalSharedMaterial = renderer.sharedMaterial;
                                          tracker.InstancedMaterial = instancedMaterial;
                                    }

                                    Debug.Log($"WallPainterMaterialApplier: Материал {currentPaintMaterial.name} применен напрямую к объекту {hit.collider.gameObject.name}");
                              }
                              else
                              {
                                    Debug.LogWarning($"WallPainterMaterialApplier: Объект {hit.collider.gameObject.name} не имеет компонента Renderer");
                              }
                        }
                        else
                        {
                              Debug.LogWarning("WallPainterMaterialApplier: Рейкаст не попал ни в один объект");
                        }
                  }
                  else
                  {
                        Debug.LogError("WallPainterMaterialApplier: Основная камера не задана");
                  }
            }

            private void ApplyMaterialUnderCursor()
            {
                  if (currentPaintMaterial == null)
                  {
                        Debug.LogError("WallPainterMaterialApplier: Текущий материал не задан");
                        return;
                  }

                  // Получаем позицию мыши
                  Vector2 mousePosition = Event.current.mousePosition;
                  mousePosition.y = Screen.height - mousePosition.y; // Конвертируем координаты

                  Debug.Log($"WallPainterMaterialApplier: Применение материала под курсором ({mousePosition.x}, {mousePosition.y})");

                  // Получаем камеру
                  Camera mainCamera = mainCameraField.GetValue(wallPainter) as Camera;
                  int wallLayerMask = (int)wallLayerMaskField.GetValue(wallPainter);

                  if (mainCamera != null)
                  {
                        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
                        RaycastHit hit;

                        if (Physics.Raycast(ray, out hit, 100f, wallLayerMask))
                        {
                              Debug.Log($"WallPainterMaterialApplier: Рейкаст попал в объект {hit.collider.gameObject.name} на расстоянии {hit.distance}");

                              // Напрямую применяем материал
                              Renderer renderer = hit.collider.GetComponent<Renderer>();
                              if (renderer != null)
                              {
                                    // Проверяем, есть ли компонент для отслеживания материалов
                                    WallMaterialInstanceTracker tracker = hit.collider.GetComponent<WallMaterialInstanceTracker>();
                                    if (tracker != null)
                                    {
                                          // Используем метод компонента для применения материала
                                          tracker.ApplyMaterial(currentPaintMaterial);
                                    }
                                    else
                                    {
                                          // Создаем экземпляр материала
                                          Material instancedMaterial = new Material(currentPaintMaterial);
                                          instancedMaterial.name = $"{currentPaintMaterial.name}_Instance_{hit.collider.gameObject.name}";

                                          // Применяем экземпляр материала
                                          renderer.material = instancedMaterial;

                                          // Добавляем компонент для отслеживания
                                          tracker = hit.collider.gameObject.AddComponent<WallMaterialInstanceTracker>();
                                          tracker.OriginalSharedMaterial = renderer.sharedMaterial;
                                          tracker.InstancedMaterial = instancedMaterial;
                                    }

                                    Debug.Log($"WallPainterMaterialApplier: Материал {currentPaintMaterial.name} применен напрямую к объекту {hit.collider.gameObject.name}");
                              }
                              else
                              {
                                    Debug.LogWarning($"WallPainterMaterialApplier: Объект {hit.collider.gameObject.name} не имеет компонента Renderer");
                              }
                        }
                        else
                        {
                              Debug.LogWarning("WallPainterMaterialApplier: Рейкаст не попал ни в один объект");
                        }
                  }
                  else
                  {
                        Debug.LogError("WallPainterMaterialApplier: Основная камера не задана");
                  }
            }
      }
}
#endif