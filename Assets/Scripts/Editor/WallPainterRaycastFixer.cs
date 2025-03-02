using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace Remalux.AR
{
      public static class WallPainterRaycastFixer
      {
            private const int WALL_LAYER = 8; // Слой "Wall" имеет индекс 8

            [MenuItem("Tools/Wall Painting/Fix/Fix Raycast Issues")]
            public static void FixRaycastIssues()
            {
                  Debug.Log("=== Исправление проблем с рейкастами в WallPainter ===");

                  // Находим все WallPainter в сцене
                  MonoBehaviour[] allComponents = Object.FindObjectsOfType<MonoBehaviour>();
                  int fixedCount = 0;

                  foreach (MonoBehaviour component in allComponents)
                  {
                        if (component.GetType().Name == "WallPainter")
                        {
                              bool wasFixed = FixWallPainterRaycasts(component);
                              if (wasFixed)
                              {
                                    fixedCount++;
                              }
                        }
                  }

                  Debug.Log($"=== Исправлено {fixedCount} компонентов WallPainter ===");
            }

            private static bool FixWallPainterRaycasts(MonoBehaviour wallPainter)
            {
                  Debug.Log($"Исправление рейкастов в WallPainter на объекте {wallPainter.gameObject.name}...");
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

                  // Добавляем компонент для расширенной обработки рейкастов
                  WallPainterRaycastEnhancer enhancer = wallPainter.gameObject.GetComponent<WallPainterRaycastEnhancer>();
                  if (enhancer == null)
                  {
                        enhancer = wallPainter.gameObject.AddComponent<WallPainterRaycastEnhancer>();
                        enhancer.wallPainter = wallPainter;
                        anyChanges = true;
                        Debug.Log($"  - Добавлен компонент WallPainterRaycastEnhancer для улучшения рейкастов");
                  }

                  // Если были изменения, помечаем объект как измененный
                  if (anyChanges)
                  {
                        EditorUtility.SetDirty(wallPainter);
                  }

                  return anyChanges;
            }

            [MenuItem("Tools/Wall Painting/Debug/Test Raycasts")]
            public static void TestRaycasts()
            {
                  Debug.Log("=== Тестирование рейкастов для стен ===");

                  // Находим все объекты на слое "Wall"
                  GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
                  int wallObjectsCount = 0;

                  foreach (GameObject obj in allObjects)
                  {
                        if (obj.layer == WALL_LAYER)
                        {
                              wallObjectsCount++;
                              Debug.Log($"Объект на слое Wall: {obj.name}");

                              // Проверяем наличие коллайдера
                              Collider collider = obj.GetComponent<Collider>();
                              if (collider == null)
                              {
                                    Debug.LogError($"  - Объект {obj.name} не имеет коллайдера. Рейкасты не будут его обнаруживать.");
                              }
                              else
                              {
                                    Debug.Log($"  - Коллайдер: {collider.GetType().Name}, Enabled: {collider.enabled}, Trigger: {collider.isTrigger}");
                              }
                        }
                  }

                  if (wallObjectsCount == 0)
                  {
                        Debug.LogWarning("Не найдено объектов на слое Wall. Рейкасты не будут ничего обнаруживать.");
                  }

                  // Тестируем рейкасты из камеры
                  Camera mainCamera = Camera.main;
                  if (mainCamera != null)
                  {
                        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                        RaycastHit hit;
                        bool didHit = Physics.Raycast(ray, out hit, 100f, 1 << WALL_LAYER);

                        if (didHit)
                        {
                              Debug.Log($"Рейкаст из камеры попал в объект: {hit.collider.gameObject.name}, расстояние: {hit.distance}");
                        }
                        else
                        {
                              Debug.LogWarning("Рейкаст из камеры не попал ни в один объект на слое Wall.");
                        }
                  }
                  else
                  {
                        Debug.LogError("Не найдена основная камера для тестирования рейкастов.");
                  }

                  Debug.Log("=== Завершение тестирования рейкастов ===");
            }

            [MenuItem("Tools/Wall Painting/Fix/Add Raycast Enhancers")]
            public static void AddRaycastEnhancers()
            {
                  Debug.Log("=== Добавление компонентов для улучшения рейкастов ===");

                  // Находим все WallPainter в сцене
                  MonoBehaviour[] allComponents = Object.FindObjectsOfType<MonoBehaviour>();
                  int addedCount = 0;

                  foreach (MonoBehaviour component in allComponents)
                  {
                        if (component.GetType().Name == "WallPainter")
                        {
                              GameObject gameObject = component.gameObject;

                              // Проверяем, есть ли уже компонент WallPainterRaycastEnhancer
                              WallPainterRaycastEnhancer existingEnhancer = gameObject.GetComponent<WallPainterRaycastEnhancer>();
                              if (existingEnhancer == null)
                              {
                                    WallPainterRaycastEnhancer enhancer = gameObject.AddComponent<WallPainterRaycastEnhancer>();
                                    enhancer.wallPainter = component;
                                    addedCount++;
                                    Debug.Log($"Добавлен WallPainterRaycastEnhancer к объекту {gameObject.name}");
                              }
                              else
                              {
                                    Debug.Log($"WallPainterRaycastEnhancer уже присутствует на объекте {gameObject.name}");
                              }
                        }
                  }

                  Debug.Log($"=== Добавлено {addedCount} компонентов WallPainterRaycastEnhancer ===");
            }
      }

      // Компонент для улучшения рейкастов в WallPainter
      public class WallPainterRaycastEnhancer : MonoBehaviour
      {
            [HideInInspector]
            public MonoBehaviour wallPainter;

            private FieldInfo mainCameraField;
            private FieldInfo wallLayerMaskField;
            private MethodInfo paintWallAtPositionMethod;

            private Camera mainCamera;
            private LayerMask wallLayerMask;

            [Header("Настройки рейкастов")]
            public bool enableEnhancedRaycasts = true;
            public float raycastDistance = 100f;
            public bool showDebugInfo = true;

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
                        // Получаем доступ к полям и методам через рефлексию
                        mainCameraField = wallPainter.GetType().GetField("mainCamera");
                        wallLayerMaskField = wallPainter.GetType().GetField("wallLayerMask");
                        paintWallAtPositionMethod = wallPainter.GetType().GetMethod("PaintWallAtPosition");

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
                              if (mainCamera != null && mainCameraField != null)
                              {
                                    mainCameraField.SetValue(wallPainter, mainCamera);
                              }
                        }

                        Debug.Log($"WallPainterRaycastEnhancer: инициализация завершена. Камера: {(mainCamera != null ? mainCamera.name : "Не задана")}, Маска слоя: {wallLayerMask.value}");
                  }
                  else
                  {
                        Debug.LogError("Не задана ссылка на компонент WallPainter");
                        enabled = false;
                  }
            }

            private void Update()
            {
                  if (!enableEnhancedRaycasts || wallPainter == null || mainCamera == null)
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

                        if (showDebugInfo)
                        {
                              Debug.DrawLine(ray.origin, hit.point, Color.green, 0.1f);
                              Debug.DrawRay(hit.point, hit.normal * 0.2f, Color.yellow, 0.1f);
                        }

                        // Обработка нажатия мыши
                        if (Input.GetMouseButtonDown(0))
                        {
                              if (paintWallAtPositionMethod != null)
                              {
                                    // Исправление: конвертируем Vector3 в Vector2
                                    Vector2 mousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                                    Debug.Log($"WallPainterRaycastEnhancer: вызов покраски стены в позиции {mousePos}");
                                    paintWallAtPositionMethod.Invoke(wallPainter, new object[] { mousePos });
                              }
                        }

                        // Обработка касаний (для мобильных устройств)
                        if (Input.touchCount > 0)
                        {
                              Touch touch = Input.GetTouch(0);
                              if (touch.phase == TouchPhase.Began)
                              {
                                    if (paintWallAtPositionMethod != null)
                                    {
                                          Debug.Log($"WallPainterRaycastEnhancer: вызов покраски стены в позиции {touch.position}");
                                          paintWallAtPositionMethod.Invoke(wallPainter, new object[] { touch.position });
                                    }
                              }
                        }
                  }
                  else if (showDebugInfo)
                  {
                        Debug.DrawRay(ray.origin, ray.direction * raycastDistance, Color.red, 0.1f);
                  }
            }

            private void OnGUI()
            {
                  if (!showDebugInfo || !enableEnhancedRaycasts)
                        return;

                  GUILayout.BeginArea(new Rect(10, 10, 300, 150));
                  GUILayout.BeginVertical("box");

                  GUILayout.Label("Отладка рейкастов WallPainter", EditorStyles.boldLabel);
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
}