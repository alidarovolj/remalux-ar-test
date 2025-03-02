using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace Remalux.AR
{
      [CustomEditor(typeof(MonoBehaviour), true)]
      public class WallPainterRaycastDebugger : Editor
      {
            private bool isWallPainter = false;
            private MonoBehaviour targetComponent;
            private FieldInfo mainCameraField;
            private FieldInfo wallLayerMaskField;
            private MethodInfo handleWallHitMethod;
            private MethodInfo paintWallAtPositionMethod;

            private bool showRaycastSettings = true;
            private bool showDebugOptions = true;
            private bool enableRaycastVisualization = false;
            private bool enableRaycastLogging = false;

            private void OnEnable()
            {
                  targetComponent = (MonoBehaviour)target;
                  isWallPainter = targetComponent.GetType().Name == "WallPainter";

                  if (isWallPainter)
                  {
                        // Получаем доступ к полям и методам через рефлексию
                        mainCameraField = targetComponent.GetType().GetField("mainCamera");
                        wallLayerMaskField = targetComponent.GetType().GetField("wallLayerMask");
                        handleWallHitMethod = targetComponent.GetType().GetMethod("HandleWallHit",
                            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        paintWallAtPositionMethod = targetComponent.GetType().GetMethod("PaintWallAtPosition");
                  }
            }

            public override void OnInspectorGUI()
            {
                  base.OnInspectorGUI();

                  if (!isWallPainter)
                        return;

                  EditorGUILayout.Space();
                  EditorGUILayout.LabelField("Отладка рейкастов WallPainter", EditorStyles.boldLabel);

                  showRaycastSettings = EditorGUILayout.Foldout(showRaycastSettings, "Настройки рейкастов");
                  if (showRaycastSettings)
                  {
                        EditorGUI.indentLevel++;

                        // Проверка и отображение настроек рейкастов
                        if (mainCameraField != null)
                        {
                              Camera camera = (Camera)mainCameraField.GetValue(targetComponent);
                              EditorGUILayout.LabelField("Камера:", camera != null ? camera.name : "Не задана");

                              if (camera == null)
                              {
                                    EditorGUILayout.HelpBox("Камера не задана! Рейкасты не будут работать.", MessageType.Error);
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
                                    EditorGUILayout.HelpBox("Слой 'Wall' не включен в маску! Рейкасты не будут обнаруживать стены.", MessageType.Error);
                                    if (GUILayout.Button("Исправить маску слоя"))
                                    {
                                          LayerMask newMask = 1 << wallLayer;
                                          wallLayerMaskField.SetValue(targetComponent, newMask);
                                          EditorUtility.SetDirty(targetComponent);
                                    }
                              }
                        }

                        if (handleWallHitMethod != null)
                        {
                              EditorGUILayout.LabelField("Метод HandleWallHit:", "Найден");
                        }
                        else
                        {
                              EditorGUILayout.HelpBox("Метод HandleWallHit не найден! Обработка попаданий в стену может не работать.", MessageType.Warning);
                        }

                        EditorGUI.indentLevel--;
                  }

                  showDebugOptions = EditorGUILayout.Foldout(showDebugOptions, "Опции отладки");
                  if (showDebugOptions)
                  {
                        EditorGUI.indentLevel++;

                        enableRaycastVisualization = EditorGUILayout.Toggle("Визуализация рейкастов", enableRaycastVisualization);
                        if (enableRaycastVisualization)
                        {
                              EditorGUILayout.HelpBox("Визуализация рейкастов будет активна в режиме игры", MessageType.Info);

                              if (GUILayout.Button("Добавить визуализатор рейкастов"))
                              {
                                    AddRaycastVisualizer();
                              }
                        }

                        enableRaycastLogging = EditorGUILayout.Toggle("Логирование рейкастов", enableRaycastLogging);
                        if (enableRaycastLogging)
                        {
                              if (GUILayout.Button("Добавить логгер рейкастов"))
                              {
                                    AddRaycastLogger();
                              }
                        }

                        EditorGUI.indentLevel--;
                  }

                  EditorGUILayout.Space();
                  if (GUILayout.Button("Проверить настройки рейкастов"))
                  {
                        CheckRaycastSettings();
                  }

                  if (GUILayout.Button("Исправить настройки рейкастов"))
                  {
                        FixRaycastSettings();
                  }

                  if (GUILayout.Button("Тест рейкаста в центре экрана"))
                  {
                        TestRaycastAtScreenCenter();
                  }
            }

            private void AddRaycastVisualizer()
            {
                  GameObject visualizerObject = GameObject.Find("RaycastVisualizer");
                  if (visualizerObject == null)
                  {
                        visualizerObject = new GameObject("RaycastVisualizer");
                        RaycastVisualizer visualizer = visualizerObject.AddComponent<RaycastVisualizer>();
                        visualizer.wallPainter = targetComponent;
                        Debug.Log("Добавлен визуализатор рейкастов");
                  }
                  else
                  {
                        Debug.Log("Визуализатор рейкастов уже существует");
                  }
            }

            private void AddRaycastLogger()
            {
                  GameObject loggerObject = GameObject.Find("RaycastLogger");
                  if (loggerObject == null)
                  {
                        loggerObject = new GameObject("RaycastLogger");
                        RaycastLogger logger = loggerObject.AddComponent<RaycastLogger>();
                        logger.wallPainter = targetComponent;
                        Debug.Log("Добавлен логгер рейкастов");
                  }
                  else
                  {
                        Debug.Log("Логгер рейкастов уже существует");
                  }
            }

            private void CheckRaycastSettings()
            {
                  Debug.Log("=== Проверка настроек рейкастов в WallPainter ===");

                  // Проверяем камеру
                  if (mainCameraField != null)
                  {
                        Camera camera = (Camera)mainCameraField.GetValue(targetComponent);
                        if (camera == null)
                        {
                              Debug.LogError("Камера не задана! Рейкасты не будут работать.");
                        }
                        else
                        {
                              Debug.Log($"Камера задана: {camera.name}");
                        }
                  }

                  // Проверяем маску слоя
                  if (wallLayerMaskField != null)
                  {
                        LayerMask mask = (LayerMask)wallLayerMaskField.GetValue(targetComponent);
                        Debug.Log($"Маска слоя: {mask.value}");

                        // Проверяем, включен ли слой "Wall" (обычно 8)
                        int wallLayer = 8;
                        bool wallLayerIncluded = (mask.value & (1 << wallLayer)) != 0;

                        if (!wallLayerIncluded)
                        {
                              Debug.LogError("Слой 'Wall' не включен в маску! Рейкасты не будут обнаруживать стены.");
                        }
                        else
                        {
                              Debug.Log("Слой 'Wall' включен в маску.");
                        }
                  }

                  // Проверяем наличие метода HandleWallHit
                  if (handleWallHitMethod == null)
                  {
                        Debug.LogWarning("Метод HandleWallHit не найден! Обработка попаданий в стену может не работать.");
                  }
                  else
                  {
                        Debug.Log("Метод HandleWallHit найден.");
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

                  Debug.Log("=== Завершение проверки настроек рейкастов ===");
            }

            private void FixRaycastSettings()
            {
                  Debug.Log("=== Исправление настроек рейкастов в WallPainter ===");

                  bool anyChanges = false;

                  // Исправляем камеру
                  if (mainCameraField != null)
                  {
                        Camera camera = (Camera)mainCameraField.GetValue(targetComponent);
                        if (camera == null)
                        {
                              Camera mainCamera = Camera.main;
                              if (mainCamera != null)
                              {
                                    mainCameraField.SetValue(targetComponent, mainCamera);
                                    anyChanges = true;
                                    Debug.Log($"Установлена основная камера: {mainCamera.name}");
                              }
                              else
                              {
                                    Debug.LogError("Не удалось найти основную камеру в сцене.");
                              }
                        }
                  }

                  // Исправляем маску слоя
                  if (wallLayerMaskField != null)
                  {
                        LayerMask mask = (LayerMask)wallLayerMaskField.GetValue(targetComponent);
                        int wallLayer = 8;
                        bool wallLayerIncluded = (mask.value & (1 << wallLayer)) != 0;

                        if (!wallLayerIncluded)
                        {
                              LayerMask newMask = 1 << wallLayer;
                              wallLayerMaskField.SetValue(targetComponent, newMask);
                              anyChanges = true;
                              Debug.Log($"Исправлена маска слоя. Новое значение: {newMask.value}");
                        }
                  }

                  // Добавляем визуализатор рейкастов
                  GameObject visualizerObject = GameObject.Find("RaycastVisualizer");
                  if (visualizerObject == null)
                  {
                        visualizerObject = new GameObject("RaycastVisualizer");
                        RaycastVisualizer visualizer = visualizerObject.AddComponent<RaycastVisualizer>();
                        visualizer.wallPainter = targetComponent;
                        Debug.Log("Добавлен визуализатор рейкастов");
                  }

                  if (anyChanges)
                  {
                        EditorUtility.SetDirty(targetComponent);
                        Debug.Log("Настройки рейкастов исправлены.");
                  }
                  else
                  {
                        Debug.Log("Настройки рейкастов уже корректны.");
                  }

                  Debug.Log("=== Завершение исправления настроек рейкастов ===");
            }

            private void TestRaycastAtScreenCenter()
            {
                  Debug.Log("=== Тест рейкаста в центре экрана ===");

                  if (mainCameraField == null || wallLayerMaskField == null)
                  {
                        Debug.LogError("Не удалось получить доступ к полям камеры или маски слоя.");
                        return;
                  }

                  Camera camera = (Camera)mainCameraField.GetValue(targetComponent);
                  if (camera == null)
                  {
                        Debug.LogError("Камера не задана! Рейкаст не может быть выполнен.");
                        return;
                  }

                  LayerMask mask = (LayerMask)wallLayerMaskField.GetValue(targetComponent);
                  Debug.Log($"Используемая маска слоя: {mask.value}");

                  // Выполняем рейкаст из центра экрана
                  Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                  RaycastHit hit;
                  bool didHit = Physics.Raycast(ray, out hit, 100f, mask);

                  if (didHit)
                  {
                        Debug.Log($"Рейкаст попал в объект: {hit.collider.gameObject.name}");
                        Debug.Log($"Слой объекта: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
                        Debug.Log($"Точка попадания: {hit.point}");
                        Debug.Log($"Расстояние: {hit.distance}");

                        // Пытаемся вызвать метод HandleWallHit
                        if (handleWallHitMethod != null)
                        {
                              Debug.Log("Вызов метода HandleWallHit...");
                              handleWallHitMethod.Invoke(targetComponent, new object[] { hit });
                        }

                        // Пытаемся вызвать метод PaintWallAtPosition
                        if (paintWallAtPositionMethod != null)
                        {
                              Debug.Log("Вызов метода PaintWallAtPosition...");
                              Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
                              paintWallAtPositionMethod.Invoke(targetComponent, new object[] { screenCenter });
                        }
                  }
                  else
                  {
                        Debug.LogWarning("Рейкаст не попал ни в один объект.");
                        Debug.Log($"Направление луча: {ray.direction}");
                  }

                  Debug.Log("=== Завершение теста рейкаста ===");
            }
      }

      // Класс для визуализации рейкастов
      public class RaycastVisualizer : MonoBehaviour
      {
            [HideInInspector]
            public MonoBehaviour wallPainter;

            private FieldInfo mainCameraField;
            private FieldInfo wallLayerMaskField;

            private Camera mainCamera;
            private LayerMask wallLayerMask;

            [Header("Настройки визуализации")]
            public bool showRays = true;
            public float raycastDistance = 100f;
            public Color hitColor = Color.green;
            public Color missColor = Color.red;

            [Header("Статистика")]
            public int raycastsPerSecond = 0;
            public int successfulHits = 0;
            public int totalRaycasts = 0;
            public string lastHitObjectName = "Нет";

            private int frameCount = 0;
            private float timer = 0f;
            private int raycastCount = 0;

            private void Start()
            {
                  if (wallPainter != null)
                  {
                        // Получаем доступ к полям через рефлексию
                        mainCameraField = wallPainter.GetType().GetField("mainCamera");
                        wallLayerMaskField = wallPainter.GetType().GetField("wallLayerMask");

                        if (mainCameraField != null)
                        {
                              mainCamera = (Camera)mainCameraField.GetValue(wallPainter);
                        }

                        if (wallLayerMaskField != null)
                        {
                              wallLayerMask = (LayerMask)wallLayerMaskField.GetValue(wallPainter);
                        }

                        if (mainCamera == null)
                        {
                              mainCamera = Camera.main;
                        }

                        Debug.Log($"RaycastVisualizer: инициализация завершена. Камера: {(mainCamera != null ? mainCamera.name : "Не задана")}, Маска слоя: {wallLayerMask.value}");
                  }
                  else
                  {
                        Debug.LogError("Не задана ссылка на компонент WallPainter");
                        enabled = false;
                  }
            }

            private void Update()
            {
                  if (!showRays || wallPainter == null || mainCamera == null)
                        return;

                  // Обновляем счетчик FPS и рейкастов
                  frameCount++;
                  timer += Time.deltaTime;

                  if (timer >= 1f)
                  {
                        raycastsPerSecond = raycastCount;
                        raycastCount = 0;
                        timer = 0f;
                        frameCount = 0;
                  }

                  // Выполняем рейкаст из центра экрана
                  Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                  RaycastHit hit;
                  bool didHit = Physics.Raycast(ray, out hit, raycastDistance, wallLayerMask);

                  raycastCount++;
                  totalRaycasts++;

                  if (didHit)
                  {
                        lastHitObjectName = hit.collider.gameObject.name;
                        successfulHits++;
                        Debug.DrawLine(ray.origin, hit.point, hitColor, 0.1f);
                        Debug.DrawRay(hit.point, hit.normal * 0.2f, Color.yellow, 0.1f);
                  }
                  else
                  {
                        Debug.DrawRay(ray.origin, ray.direction * raycastDistance, missColor, 0.1f);
                  }
            }

            private void OnGUI()
            {
                  if (!showRays)
                        return;

                  GUILayout.BeginArea(new Rect(10, 10, 300, 150));
                  GUILayout.BeginVertical("box");

                  GUILayout.Label("Статистика рейкастов", EditorStyles.boldLabel);
                  GUILayout.Label($"Рейкастов в секунду: {raycastsPerSecond}");
                  GUILayout.Label($"Всего рейкастов: {totalRaycasts}");
                  GUILayout.Label($"Успешных попаданий: {successfulHits}");
                  GUILayout.Label($"Последний объект: {lastHitObjectName}");

                  if (wallLayerMask.value > 0)
                  {
                        GUILayout.Label($"Маска слоя: {wallLayerMask.value}");

                        // Проверяем, включен ли слой "Wall" (обычно 8)
                        int wallLayer = 8;
                        if ((wallLayerMask.value & (1 << wallLayer)) == 0)
                        {
                              GUILayout.Label("Слой 'Wall' НЕ включен в маску!", "box");
                        }
                        else
                        {
                              GUILayout.Label("Слой 'Wall' включен в маску", "box");
                        }
                  }
                  else
                  {
                        GUILayout.Label("Маска слоя не задана!", "box");
                  }

                  GUILayout.EndVertical();
                  GUILayout.EndArea();
            }
      }

      // Класс для логирования рейкастов
      public class RaycastLogger : MonoBehaviour
      {
            [HideInInspector]
            public MonoBehaviour wallPainter;

            private FieldInfo mainCameraField;
            private FieldInfo wallLayerMaskField;

            private Camera mainCamera;
            private LayerMask wallLayerMask;

            [Header("Настройки логирования")]
            public bool logEveryFrame = false;
            public bool logOnlyHits = true;
            public float logInterval = 1f;

            private float timer = 0f;

            private void Start()
            {
                  if (wallPainter != null)
                  {
                        // Получаем доступ к полям через рефлексию
                        mainCameraField = wallPainter.GetType().GetField("mainCamera");
                        wallLayerMaskField = wallPainter.GetType().GetField("wallLayerMask");

                        if (mainCameraField != null)
                        {
                              mainCamera = (Camera)mainCameraField.GetValue(wallPainter);
                        }

                        if (wallLayerMaskField != null)
                        {
                              wallLayerMask = (LayerMask)wallLayerMaskField.GetValue(wallPainter);
                        }

                        if (mainCamera == null)
                        {
                              mainCamera = Camera.main;
                        }

                        Debug.Log($"RaycastLogger: инициализация завершена. Камера: {(mainCamera != null ? mainCamera.name : "Не задана")}, Маска слоя: {wallLayerMask.value}");
                  }
                  else
                  {
                        Debug.LogError("Не задана ссылка на компонент WallPainter");
                        enabled = false;
                  }
            }

            private void Update()
            {
                  if (wallPainter == null || mainCamera == null)
                        return;

                  timer += Time.deltaTime;

                  if (logEveryFrame || timer >= logInterval)
                  {
                        timer = 0f;
                        LogRaycast();
                  }
            }

            private void LogRaycast()
            {
                  // Выполняем рейкаст из центра экрана
                  Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                  RaycastHit hit;
                  bool didHit = Physics.Raycast(ray, out hit, 100f, wallLayerMask);

                  if (didHit)
                  {
                        Debug.Log($"Рейкаст попал в объект: {hit.collider.gameObject.name}, слой: {LayerMask.LayerToName(hit.collider.gameObject.layer)}, точка: {hit.point}");
                  }
                  else if (!logOnlyHits)
                  {
                        Debug.Log("Рейкаст не попал ни в один объект");
                  }
            }
      }
}