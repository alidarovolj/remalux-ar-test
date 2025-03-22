#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace Remalux.AR
{
      [CustomEditor(typeof(MonoBehaviour), true)]
      public class WallPainterInputDebugger : Editor
      {
            private bool isWallPainter = false;
            private MonoBehaviour targetComponent;
            private FieldInfo mainCameraField;
            private FieldInfo wallLayerMaskField;
            private FieldInfo currentPaintMaterialField;
            private MethodInfo paintWallAtPositionMethod;

            private bool showInputSettings = true;
            private bool showDebugOptions = true;
            private bool enableManualPainting = false;
            private bool enableTouchSimulation = false;
            private bool enableInputLogging = false;

            private void OnEnable()
            {
                  if (target == null) return;

                  try
                  {
                        targetComponent = target as MonoBehaviour;
                        if (targetComponent == null)
                        {
                              Debug.LogError("WallPainterInputDebugger: Target is not a MonoBehaviour");
                              return;
                        }

                        isWallPainter = targetComponent.GetType().Name == "WallPainter";

                        if (isWallPainter)
                        {
                              // Получаем доступ к полям и методам через рефлексию
                              mainCameraField = targetComponent.GetType().GetField("mainCamera");
                              wallLayerMaskField = targetComponent.GetType().GetField("wallLayerMask");
                              currentPaintMaterialField = targetComponent.GetType().GetField("currentPaintMaterial",
                                  BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                              paintWallAtPositionMethod = targetComponent.GetType().GetMethod("PaintWallAtPosition");
                        }
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"WallPainterInputDebugger: Error in OnEnable: {e.Message}");
                  }
            }

            public override void OnInspectorGUI()
            {
                  base.OnInspectorGUI();

                  if (!isWallPainter)
                        return;

                  EditorGUILayout.Space();
                  EditorGUILayout.LabelField("Отладка ввода WallPainter", EditorStyles.boldLabel);

                  showInputSettings = EditorGUILayout.Foldout(showInputSettings, "Настройки ввода");
                  if (showInputSettings)
                  {
                        EditorGUI.indentLevel++;

                        // Проверка и отображение настроек ввода
                        if (mainCameraField != null)
                        {
                              Camera camera = (Camera)mainCameraField.GetValue(targetComponent);
                              EditorGUILayout.LabelField("Камера:", camera != null ? camera.name : "Не задана");

                              if (camera == null)
                              {
                                    EditorGUILayout.HelpBox("Камера не задана! Система покраски не будет работать.", MessageType.Error);
                                    if (GUILayout.Button("Назначить основную камеру"))
                                    {
                                          Camera mainCamera = Camera.main;
                                          if (mainCamera != null)
                                          {
                                                mainCameraField.SetValue(targetComponent, mainCamera);
                                                EditorUtility.SetDirty(targetComponent);
                                          }
                                    }
                              }
                        }

                        if (wallLayerMaskField != null)
                        {
                              LayerMask mask = (LayerMask)wallLayerMaskField.GetValue(targetComponent);
                              EditorGUILayout.LabelField("Маска слоя:", mask.value.ToString());

                              // Проверяем, включен ли слой "Wall" (обычно 8)
                              int wallLayer = 8;
                              bool wallLayerIncluded = (mask.value & (1 << wallLayer)) != 0;

                              EditorGUILayout.LabelField("Слой 'Wall' включен:", wallLayerIncluded ? "Да" : "Нет");

                              if (!wallLayerIncluded)
                              {
                                    EditorGUILayout.HelpBox("Слой 'Wall' не включен в маску! Система покраски не будет работать.", MessageType.Error);
                                    if (GUILayout.Button("Исправить маску слоя"))
                                    {
                                          LayerMask newMask = 1 << wallLayer;
                                          wallLayerMaskField.SetValue(targetComponent, newMask);
                                          EditorUtility.SetDirty(targetComponent);
                                    }
                              }
                        }

                        if (currentPaintMaterialField != null)
                        {
                              Material material = (Material)currentPaintMaterialField.GetValue(targetComponent);
                              EditorGUILayout.LabelField("Текущий материал:", material != null ? material.name : "Не задан");

                              if (material == null)
                              {
                                    EditorGUILayout.HelpBox("Материал для покраски не выбран!", MessageType.Warning);
                              }
                        }

                        EditorGUI.indentLevel--;
                  }

                  showDebugOptions = EditorGUILayout.Foldout(showDebugOptions, "Опции отладки");
                  if (showDebugOptions)
                  {
                        EditorGUI.indentLevel++;

                        enableManualPainting = EditorGUILayout.Toggle("Включить ручную покраску", enableManualPainting);
                        if (enableManualPainting)
                        {
                              EditorGUILayout.HelpBox("Нажмите на сцену, чтобы покрасить стену в этой точке", MessageType.Info);

                              if (GUILayout.Button("Тест покраски в центре экрана"))
                              {
                                    TestPaintAtScreenCenter();
                              }
                        }

                        enableTouchSimulation = EditorGUILayout.Toggle("Симуляция касания", enableTouchSimulation);
                        if (enableTouchSimulation)
                        {
                              EditorGUILayout.HelpBox("Симуляция касания будет активна в режиме игры", MessageType.Info);

                              if (GUILayout.Button("Добавить симулятор касаний"))
                              {
                                    AddTouchSimulator();
                              }
                        }

                        enableInputLogging = EditorGUILayout.Toggle("Логирование ввода", enableInputLogging);
                        if (enableInputLogging)
                        {
                              if (GUILayout.Button("Добавить логгер ввода"))
                              {
                                    AddInputLogger();
                              }
                        }

                        EditorGUI.indentLevel--;
                  }

                  EditorGUILayout.Space();
                  if (GUILayout.Button("Проверить обработку ввода"))
                  {
                        CheckInputHandling();
                  }

                  if (GUILayout.Button("Исправить обработку ввода"))
                  {
                        FixInputHandling();
                  }
            }

            private void TestPaintAtScreenCenter()
            {
                  if (paintWallAtPositionMethod != null)
                  {
                        Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
                        paintWallAtPositionMethod.Invoke(targetComponent, new object[] { screenCenter });
                        Debug.Log("Тест покраски в центре экрана выполнен");
                  }
            }

            private void AddTouchSimulator()
            {
                  GameObject simulatorObject = GameObject.Find("TouchSimulator");
                  if (simulatorObject == null)
                  {
                        simulatorObject = new GameObject("TouchSimulator");
                        simulatorObject.AddComponent<TouchSimulator>();
                        Debug.Log("Добавлен симулятор касаний");
                  }
                  else
                  {
                        Debug.Log("Симулятор касаний уже существует");
                  }
            }

            private void AddInputLogger()
            {
                  GameObject loggerObject = GameObject.Find("InputLogger");
                  if (loggerObject == null)
                  {
                        loggerObject = new GameObject("InputLogger");
                        loggerObject.AddComponent<InputLogger>();
                        Debug.Log("Добавлен логгер ввода");
                  }
                  else
                  {
                        Debug.Log("Логгер ввода уже существует");
                  }
            }

            private void CheckInputHandling()
            {
                  Debug.Log("=== Проверка обработки ввода в WallPainter ===");

                  // Проверяем наличие метода HandleInput
                  MethodInfo handleInputMethod = targetComponent.GetType().GetMethod("HandleInput",
                      BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                  if (handleInputMethod == null)
                  {
                        Debug.LogError("Метод HandleInput не найден! Обработка ввода может не работать.");
                  }
                  else
                  {
                        Debug.Log("Метод HandleInput найден.");

                        // Проверяем код метода HandleInput через рефлексию
                        // Это сложно сделать напрямую, поэтому проверяем косвенно

                        // Проверяем, вызывается ли HandleInput из Update
                        MethodInfo updateMethod = targetComponent.GetType().GetMethod("Update",
                            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                        if (updateMethod != null)
                        {
                              Debug.Log("Метод Update найден. Предполагается, что он вызывает HandleInput.");
                        }
                        else
                        {
                              Debug.LogWarning("Метод Update не найден! Обработка ввода может не вызываться.");
                        }
                  }

                  // Проверяем наличие метода PaintWallAtPosition
                  if (paintWallAtPositionMethod == null)
                  {
                        Debug.LogError("Метод PaintWallAtPosition не найден! Покраска стен не будет работать.");
                  }
                  else
                  {
                        Debug.Log("Метод PaintWallAtPosition найден.");
                  }

                  // Проверяем настройки Input в ProjectSettings
                  Debug.Log("Проверка настроек Input в ProjectSettings...");
                  Debug.Log("Эту проверку нужно выполнить вручную: Edit -> Project Settings -> Input");

                  Debug.Log("=== Завершение проверки обработки ввода ===");
            }

            private void FixInputHandling()
            {
                  Debug.Log("=== Исправление обработки ввода в WallPainter ===");

                  // Создаем новый скрипт для улучшенной обработки ввода
                  GameObject inputHandlerObject = GameObject.Find("WallPainterInputHandler");
                  if (inputHandlerObject == null)
                  {
                        inputHandlerObject = new GameObject("WallPainterInputHandler");
                        WallPainterInputHandler handler = inputHandlerObject.AddComponent<WallPainterInputHandler>();

                        // Устанавливаем ссылку на WallPainter
                        handler.SetWallPainter(targetComponent);

                        Debug.Log("Создан WallPainterInputHandler для улучшенной обработки ввода");
                  }
                  else
                  {
                        Debug.Log("WallPainterInputHandler уже существует");
                  }

                  Debug.Log("=== Завершение исправления обработки ввода ===");
            }
      }

      // Класс для симуляции касаний в редакторе
      public class TouchSimulator : MonoBehaviour
      {
            private void Update()
            {
                  // Use UnityEngine.Input directly
                  if (UnityEngine.Input.GetMouseButtonDown(0))
                  {
                        Vector2 mousePos = UnityEngine.Input.mousePosition;
                        Debug.Log($"TouchSimulator: Simulating touch at {mousePos}");
                  }
            }
      }

      // Класс для логирования ввода
      public class InputLogger : MonoBehaviour
      {
            private void Update()
            {
                  // Use UnityEngine.Input directly
                  if (UnityEngine.Input.GetMouseButtonDown(0))
                  {
                        Vector2 mousePos = UnityEngine.Input.mousePosition;
                        Debug.Log($"InputLogger: Mouse click at {mousePos}");
                  }

                  if (UnityEngine.Input.touchCount > 0)
                  {
                        Touch touch = UnityEngine.Input.GetTouch(0);
                        if (touch.phase == TouchPhase.Began)
                        {
                              Debug.Log($"InputLogger: Touch at {touch.position}");
                        }
                  }
            }
      }

      // Класс для улучшенной обработки ввода
      public class WallPainterInputHandler : MonoBehaviour
      {
            private MonoBehaviour wallPainter;
            private MethodInfo paintWallAtPositionMethod;

            public void SetWallPainter(MonoBehaviour wp)
            {
                  wallPainter = wp;
                  paintWallAtPositionMethod = wallPainter?.GetType().GetMethod("PaintWallAtPosition");
            }

            private void Update()
            {
                  if (wallPainter == null || paintWallAtPositionMethod == null) return;

                  // Use UnityEngine.Input directly
                  if (UnityEngine.Input.GetMouseButtonDown(0))
                  {
                        Vector2 mousePos = UnityEngine.Input.mousePosition;
                        paintWallAtPositionMethod.Invoke(wallPainter, new object[] { mousePos });
                  }

                  if (UnityEngine.Input.touchCount > 0)
                  {
                        Touch touch = UnityEngine.Input.GetTouch(0);
                        if (touch.phase == TouchPhase.Began)
                        {
                              paintWallAtPositionMethod.Invoke(wallPainter, new object[] { touch.position });
                        }
                  }
            }
      }
}
#endif