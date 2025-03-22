#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Reflection;

namespace Remalux.AR
{
      public class WallPainterRaycastVisualizer : MonoBehaviour
      {
            [Header("Настройки")]
            public bool visualizeRaycasts = true;
            public float raycastDistance = 10f;
            public Color rayHitColor = Color.green;
            public Color rayMissColor = Color.red;
            public float rayDuration = 0.1f;
            public bool showDebugWindow = true;

            [Header("Статистика")]
            [SerializeField] private int raycastsPerSecond = 0;
            [SerializeField] private int successfulHits = 0;
            [SerializeField] private int totalRaycasts = 0;
            [SerializeField] private string lastHitObjectName = "Нет";
            [SerializeField] private string lastHitObjectLayer = "Нет";
            [SerializeField] private Vector3 lastHitPoint = Vector3.zero;

            private MonoBehaviour wallPainter;
            private Camera mainCamera;
            private LayerMask wallLayerMask;
            private int frameCount = 0;
            private float timer = 0f;
            private int raycastCount = 0;
            private bool isInitialized = false;

            private void Start()
            {
                  InitializeVisualizer();
            }

            private void InitializeVisualizer()
            {
                  // Находим WallPainter в сцене
                  MonoBehaviour[] allComponents = FindObjectsOfType<MonoBehaviour>();
                  foreach (MonoBehaviour component in allComponents)
                  {
                        if (component.GetType().Name == "WallPainter")
                        {
                              wallPainter = component;
                              break;
                        }
                  }

                  if (wallPainter == null)
                  {
                        Debug.LogError("WallPainterRaycastVisualizer: Не найден компонент WallPainter в сцене.");
                        enabled = false;
                        return;
                  }

                  // Получаем камеру из WallPainter
                  FieldInfo mainCameraField = wallPainter.GetType().GetField("mainCamera");
                  if (mainCameraField != null)
                  {
                        mainCamera = (Camera)mainCameraField.GetValue(wallPainter);
                  }

                  if (mainCamera == null)
                  {
                        mainCamera = Camera.main;
                        if (mainCamera == null)
                        {
                              Debug.LogError("WallPainterRaycastVisualizer: Не удалось найти камеру.");
                              enabled = false;
                              return;
                        }
                  }

                  // Получаем маску слоя из WallPainter
                  FieldInfo wallLayerMaskField = wallPainter.GetType().GetField("wallLayerMask");
                  if (wallLayerMaskField != null)
                  {
                        wallLayerMask = (LayerMask)wallLayerMaskField.GetValue(wallPainter);
                  }

                  Debug.Log($"WallPainterRaycastVisualizer: Инициализация завершена. WallPainter: {wallPainter.gameObject.name}, Камера: {mainCamera.name}, Маска слоя: {wallLayerMask.value}");
                  isInitialized = true;
            }

            private void Update()
            {
                  if (!isInitialized)
                  {
                        InitializeVisualizer();
                        if (!isInitialized) return;
                  }

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

                  if (visualizeRaycasts)
                  {
                        if (didHit)
                        {
                              Debug.DrawLine(ray.origin, hit.point, rayHitColor, rayDuration);
                              Debug.DrawRay(hit.point, hit.normal * 0.2f, Color.yellow, rayDuration);

                              lastHitObjectName = hit.collider.gameObject.name;
                              lastHitObjectLayer = LayerMask.LayerToName(hit.collider.gameObject.layer);
                              lastHitPoint = hit.point;
                              successfulHits++;
                        }
                        else
                        {
                              Debug.DrawRay(ray.origin, ray.direction * raycastDistance, rayMissColor, rayDuration);
                        }
                  }

                  // Проверяем, вызывается ли метод обработки попадания в WallPainter
                  if (didHit)
                  {
                        MethodInfo handleHitMethod = wallPainter.GetType().GetMethod("HandleWallHit",
                            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                        if (handleHitMethod != null)
                        {
                              Debug.Log($"Попадание в стену: {hit.collider.gameObject.name}, слой: {LayerMask.LayerToName(hit.collider.gameObject.layer)}, точка: {hit.point}");
                        }
                  }
            }

            private void OnGUI()
            {
                  if (!showDebugWindow || !isInitialized)
                        return;

                  GUILayout.BeginArea(new Rect(10, 10, 300, 200));
                  GUILayout.BeginVertical("box");

                  GUILayout.Label("Отладка WallPainter", EditorStyles.boldLabel);
                  GUILayout.Label($"Рейкастов в секунду: {raycastsPerSecond}");
                  GUILayout.Label($"Всего рейкастов: {totalRaycasts}");
                  GUILayout.Label($"Успешных попаданий: {successfulHits}");
                  GUILayout.Label($"Последний объект: {lastHitObjectName}");
                  GUILayout.Label($"Слой объекта: {lastHitObjectLayer}");
                  GUILayout.Label($"Точка попадания: {lastHitPoint}");

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

            [MenuItem("Tools/Wall Painting/Debug/Add Raycast Visualizer")]
            public static void AddRaycastVisualizer()
            {
                  // Проверяем, есть ли уже визуализатор в сцене
                  WallPainterRaycastVisualizer existingVisualizer = FindObjectOfType<WallPainterRaycastVisualizer>();
                  if (existingVisualizer != null)
                  {
                        Debug.Log("WallPainterRaycastVisualizer уже добавлен в сцену.");
                        Selection.activeGameObject = existingVisualizer.gameObject;
                        return;
                  }

                  // Создаем новый объект для визуализатора
                  GameObject visualizerObject = new GameObject("WallPainterRaycastVisualizer");
                  WallPainterRaycastVisualizer visualizer = visualizerObject.AddComponent<WallPainterRaycastVisualizer>();

                  Debug.Log("WallPainterRaycastVisualizer добавлен в сцену.");
                  Selection.activeGameObject = visualizerObject;
            }
      }
}
#endif