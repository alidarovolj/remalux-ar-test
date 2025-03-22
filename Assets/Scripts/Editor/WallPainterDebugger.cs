#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Reflection;

namespace Remalux.AR
{
      [ExecuteInEditMode]
      public class WallPainterDebugger : MonoBehaviour
      {
            public bool enableDebugging = true;
            public bool showRaycastDebug = true;
            public bool showMaterialDebug = true;
            public bool logToConsole = true;
            public Color raycastHitColor = Color.green;
            public Color raycastMissColor = Color.red;

            private WallPainter wallPainter;
            private Camera mainCamera;
            private LineRenderer debugLineRenderer;
            private float lineWidth = 0.02f;
            private float lineLength = 10f;

            [MenuItem("Tools/Wall Painting/Debug/Enable Wall Painter Debugger")]
            public static void EnableDebugger()
            {
                  GameObject debuggerObj = GameObject.Find("WallPainterDebugger");
                  if (debuggerObj == null)
                  {
                        debuggerObj = new GameObject("WallPainterDebugger");
                        debuggerObj.AddComponent<WallPainterDebugger>();
                        Debug.Log("WallPainterDebugger: Создан новый объект для отладки");
                  }
                  else
                  {
                        Debug.Log("WallPainterDebugger: Объект для отладки уже существует");
                  }
            }

            [MenuItem("Tools/Wall Painting/Debug/Test Paint Wall")]
            public static void TestPaintWall()
            {
                  WallPainter wallPainter = FindObjectOfType<WallPainter>();
                  if (wallPainter == null)
                  {
                        Debug.LogError("WallPainterDebugger: Не найден компонент WallPainter в сцене");
                        return;
                  }

                  // Найдем все стены в сцене
                  GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
                  if (walls.Length == 0)
                  {
                        // Попробуем найти по слою
                        List<GameObject> wallsList = new List<GameObject>();
                        foreach (GameObject obj in FindObjectsOfType<GameObject>())
                        {
                              if (obj.layer == LayerMask.NameToLayer("Wall"))
                              {
                                    wallsList.Add(obj);
                              }
                        }
                        walls = wallsList.ToArray();
                  }

                  if (walls.Length == 0)
                  {
                        Debug.LogError("WallPainterDebugger: Не найдены объекты стен в сцене");
                        return;
                  }

                  Debug.Log($"WallPainterDebugger: Найдено {walls.Length} объектов стен");

                  // Проверим текущий материал для покраски
                  Material currentPaintMaterial = null;
                  FieldInfo fieldInfo = typeof(WallPainter).GetField("currentPaintMaterial",
                      BindingFlags.NonPublic | BindingFlags.Instance);

                  if (fieldInfo != null)
                  {
                        currentPaintMaterial = fieldInfo.GetValue(wallPainter) as Material;
                        Debug.Log($"WallPainterDebugger: Текущий материал для покраски: {(currentPaintMaterial != null ? currentPaintMaterial.name : "NULL")}");
                  }
                  else
                  {
                        Debug.LogError("WallPainterDebugger: Не удалось получить поле currentPaintMaterial");
                  }

                  // Попробуем напрямую применить материал к первой стене
                  if (currentPaintMaterial != null && walls.Length > 0)
                  {
                        GameObject wall = walls[0];
                        Renderer renderer = wall.GetComponent<Renderer>();

                        if (renderer != null)
                        {
                              Debug.Log($"WallPainterDebugger: Текущий материал стены: {renderer.sharedMaterial.name}");

                              // Создаем экземпляр материала
                              Material instancedMaterial = new Material(currentPaintMaterial);
                              instancedMaterial.name = $"{currentPaintMaterial.name}_Instance_{wall.name}";

                              // Сохраняем оригинальный материал
                              Material originalMaterial = renderer.sharedMaterial;

                              // Применяем новый материал
                              renderer.sharedMaterial = instancedMaterial;

                              Debug.Log($"WallPainterDebugger: Применен материал {instancedMaterial.name} к объекту {wall.name}");

                              // Проверяем, изменился ли материал
                              if (renderer.sharedMaterial.name == instancedMaterial.name)
                              {
                                    Debug.Log("WallPainterDebugger: Материал успешно применен");
                              }
                              else
                              {
                                    Debug.LogError($"WallPainterDebugger: Материал не применен. Текущий материал: {renderer.sharedMaterial.name}");
                              }
                        }
                        else
                        {
                              Debug.LogError($"WallPainterDebugger: У объекта {wall.name} отсутствует компонент Renderer");
                        }
                  }

                  // Попробуем вызвать метод HandleWallHit напрямую
                  Debug.Log("WallPainterDebugger: Попытка вызвать HandleWallHit напрямую");

                  if (walls.Length > 0)
                  {
                        GameObject wall = walls[0];

                        // Создаем реальный рейкаст вместо попытки создать RaycastHit вручную
                        Ray ray = new Ray(wall.transform.position - Vector3.up * 2, Vector3.up);
                        RaycastHit hit;

                        // Используем слой стены для рейкаста
                        int wallLayerMask = 1 << LayerMask.NameToLayer("Wall");

                        if (Physics.Raycast(ray, out hit, 10f, wallLayerMask))
                        {
                              Debug.Log($"WallPainterDebugger: Рейкаст попал в объект {hit.transform.name}");

                              MethodInfo methodInfo = typeof(WallPainter).GetMethod("HandleWallHit",
                                  BindingFlags.NonPublic | BindingFlags.Instance);

                              if (methodInfo != null)
                              {
                                    try
                                    {
                                          methodInfo.Invoke(wallPainter, new object[] { hit });
                                          Debug.Log("WallPainterDebugger: Метод HandleWallHit вызван успешно");
                                    }
                                    catch (System.Exception e)
                                    {
                                          Debug.LogError($"WallPainterDebugger: Ошибка при вызове HandleWallHit: {e.Message}");
                                          if (e.InnerException != null)
                                          {
                                                Debug.LogError($"WallPainterDebugger: Внутреннее исключение: {e.InnerException.Message}");
                                                Debug.LogError($"WallPainterDebugger: Стек вызовов: {e.InnerException.StackTrace}");
                                          }
                                    }
                              }
                              else
                              {
                                    Debug.LogError("WallPainterDebugger: Не удалось найти метод HandleWallHit");
                              }
                        }
                        else
                        {
                              Debug.LogError("WallPainterDebugger: Рейкаст не попал в стену. Проверьте настройки слоев.");
                        }
                  }
            }

            [MenuItem("Tools/Wall Painting/Debug/Check Wall Layers")]
            public static void CheckWallLayers()
            {
                  // Проверяем все объекты в сцене
                  int wallLayerIndex = LayerMask.NameToLayer("Wall");
                  Debug.Log($"WallPainterDebugger: Индекс слоя Wall: {wallLayerIndex}");

                  List<GameObject> wallObjects = new List<GameObject>();
                  foreach (GameObject obj in FindObjectsOfType<GameObject>())
                  {
                        if (obj.layer == wallLayerIndex)
                        {
                              wallObjects.Add(obj);
                        }
                  }

                  Debug.Log($"WallPainterDebugger: Найдено {wallObjects.Count} объектов на слое Wall");

                  foreach (GameObject wall in wallObjects)
                  {
                        Renderer renderer = wall.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                              Debug.Log($"WallPainterDebugger: Стена {wall.name}, материал: {renderer.sharedMaterial.name}");
                        }
                        else
                        {
                              Debug.LogWarning($"WallPainterDebugger: У объекта {wall.name} отсутствует компонент Renderer");
                        }
                  }

                  // Проверяем настройки WallPainter
                  WallPainter wallPainter = FindObjectOfType<WallPainter>();
                  if (wallPainter != null)
                  {
                        // Получаем значение wallLayerMask через рефлексию
                        FieldInfo fieldInfo = typeof(WallPainter).GetField("wallLayerMask",
                            BindingFlags.NonPublic | BindingFlags.Instance);

                        if (fieldInfo != null)
                        {
                              object value = fieldInfo.GetValue(wallPainter);
                              if (value is int)
                              {
                                    int mask = (int)value;
                                    Debug.Log($"WallPainterDebugger: wallLayerMask (int): {mask}, двоичное представление: {System.Convert.ToString(mask, 2)}");
                              }
                              else if (value is LayerMask)
                              {
                                    LayerMask mask = (LayerMask)value;
                                    Debug.Log($"WallPainterDebugger: wallLayerMask (LayerMask): {mask.value}, двоичное представление: {System.Convert.ToString(mask.value, 2)}");
                              }
                              else
                              {
                                    Debug.LogError($"WallPainterDebugger: wallLayerMask имеет неожиданный тип: {value.GetType().Name}");
                              }
                        }
                        else
                        {
                              Debug.LogError("WallPainterDebugger: Не удалось получить поле wallLayerMask");
                        }
                  }
                  else
                  {
                        Debug.LogError("WallPainterDebugger: Не найден компонент WallPainter в сцене");
                  }
            }

            void Start()
            {
                  wallPainter = FindObjectOfType<WallPainter>();
                  if (wallPainter == null)
                  {
                        Debug.LogError("WallPainterDebugger: Не найден компонент WallPainter в сцене");
                        return;
                  }

                  mainCamera = Camera.main;
                  if (mainCamera == null)
                  {
                        Debug.LogError("WallPainterDebugger: Не найдена основная камера");
                        return;
                  }

                  // Создаем LineRenderer для визуализации лучей
                  if (debugLineRenderer == null)
                  {
                        debugLineRenderer = gameObject.AddComponent<LineRenderer>();
                        debugLineRenderer.startWidth = lineWidth;
                        debugLineRenderer.endWidth = lineWidth;
                        debugLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                        debugLineRenderer.positionCount = 2;
                  }

                  Debug.Log("WallPainterDebugger: Инициализация завершена");
            }

            void Update()
            {
                  if (!enableDebugging || wallPainter == null)
                        return;

                  // Use UnityEngine.Input directly
                  if (UnityEngine.Input.GetMouseButtonDown(0))
                  {
                        Vector2 mousePosition = UnityEngine.Input.mousePosition;
                        Debug.Log($"WallPainterDebugger: Клик мыши в позиции {mousePosition}");

                        Ray ray = mainCamera.ScreenPointToRay(mousePosition);

                        // Получаем значение wallLayerMask через рефлексию
                        int wallLayerMask = 0;
                        FieldInfo fieldInfo = typeof(WallPainter).GetField("wallLayerMask",
                            BindingFlags.NonPublic | BindingFlags.Instance);

                        if (fieldInfo != null)
                        {
                              object value = fieldInfo.GetValue(wallPainter);
                              if (value is int)
                              {
                                    wallLayerMask = (int)value;
                              }
                              else if (value is LayerMask)
                              {
                                    wallLayerMask = ((LayerMask)value).value;
                              }
                        }

                        Debug.Log($"WallPainterDebugger: Используем маску слоя: {wallLayerMask}, двоичное представление: {System.Convert.ToString(wallLayerMask, 2)}");

                        RaycastHit hit;
                        bool didHit = Physics.Raycast(ray, out hit, 100f, wallLayerMask);

                        if (showRaycastDebug)
                        {
                              // Визуализируем луч
                              debugLineRenderer.SetPosition(0, ray.origin);
                              debugLineRenderer.SetPosition(1, ray.origin + ray.direction * (didHit ? hit.distance : lineLength));
                              debugLineRenderer.startColor = didHit ? raycastHitColor : raycastMissColor;
                              debugLineRenderer.endColor = didHit ? raycastHitColor : raycastMissColor;
                        }

                        if (didHit)
                        {
                              Debug.Log($"WallPainterDebugger: Луч попал в объект {hit.transform.name} на слое {LayerMask.LayerToName(hit.transform.gameObject.layer)}");

                              if (showMaterialDebug)
                              {
                                    Renderer renderer = hit.transform.GetComponent<Renderer>();
                                    if (renderer != null)
                                    {
                                          Debug.Log($"WallPainterDebugger: Текущий материал объекта: {renderer.sharedMaterial.name}");
                                    }
                              }

                              // Проверяем, вызывается ли HandleWallHit
                              Debug.Log("WallPainterDebugger: Проверка вызова HandleWallHit...");
                        }
                        else
                        {
                              Debug.Log("WallPainterDebugger: Луч не попал ни в один объект на слое Wall");
                        }
                  }
            }
      }
}
#endif