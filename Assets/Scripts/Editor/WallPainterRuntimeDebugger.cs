#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Reflection;

namespace Remalux.AR
{
      public class WallPainterRuntimeDebugger : MonoBehaviour
      {
            [Header("Ссылки на компоненты")]
            public MonoBehaviour wallPainter;

            [Header("Настройки отладки")]
            public bool enableDebug = true;
            public bool visualizeRaycasts = true;
            public float raycastVisualizationDuration = 0.5f;
            public Color rayHitColor = Color.green;
            public Color rayMissColor = Color.red;

            [Header("Информация")]
            [SerializeField] private string lastHitObjectName = "Нет";
            [SerializeField] private string lastHitObjectLayer = "Нет";
            [SerializeField] private Vector3 lastHitPoint = Vector3.zero;
            [SerializeField] private int raycastsPerSecond = 0;
            [SerializeField] private int successfulHitsCount = 0;

            private Camera mainCamera;
            private LayerMask wallLayerMask;
            private int frameCount = 0;
            private float timer = 0f;
            private int raycastCount = 0;

            private void Start()
            {
                  if (wallPainter == null)
                  {
                        // Пытаемся найти WallPainter в сцене
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
                              Debug.LogError("WallPainterRuntimeDebugger: Не найден компонент WallPainter в сцене.");
                              enabled = false;
                              return;
                        }
                  }

                  // Получаем камеру из WallPainter
                  var mainCameraField = wallPainter.GetType().GetField("mainCamera");
                  if (mainCameraField != null)
                  {
                        mainCamera = (Camera)mainCameraField.GetValue(wallPainter);
                  }

                  if (mainCamera == null)
                  {
                        mainCamera = Camera.main;
                        if (mainCamera == null)
                        {
                              Debug.LogError("WallPainterRuntimeDebugger: Не удалось найти камеру.");
                              enabled = false;
                              return;
                        }
                  }

                  // Получаем маску слоя из WallPainter
                  var wallLayerMaskField = wallPainter.GetType().GetField("wallLayerMask");
                  if (wallLayerMaskField != null)
                  {
                        wallLayerMask = (LayerMask)wallLayerMaskField.GetValue(wallPainter);
                  }

                  Debug.Log($"WallPainterRuntimeDebugger: Инициализация завершена. Камера: {mainCamera.name}, Маска слоя: {wallLayerMask.value}");
            }

            private void Update()
            {
                  if (!enableDebug || wallPainter == null || mainCamera == null)
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
                  bool didHit = Physics.Raycast(ray, out hit, 100f, wallLayerMask);

                  raycastCount++;

                  if (didHit)
                  {
                        lastHitObjectName = hit.collider.gameObject.name;
                        lastHitObjectLayer = LayerMask.LayerToName(hit.collider.gameObject.layer);
                        lastHitPoint = hit.point;
                        successfulHitsCount++;

                        if (visualizeRaycasts)
                        {
                              Debug.DrawLine(ray.origin, hit.point, rayHitColor, raycastVisualizationDuration);
                              Debug.DrawRay(hit.point, hit.normal * 0.2f, Color.yellow, raycastVisualizationDuration);
                        }
                  }
                  else
                  {
                        if (visualizeRaycasts)
                        {
                              Debug.DrawRay(ray.origin, ray.direction * 100f, rayMissColor, raycastVisualizationDuration);
                        }
                  }
            }

            private void OnGUI()
            {
                  if (!enableDebug)
                        return;

                  // Создаем окно отладки
                  GUILayout.BeginArea(new Rect(10, 10, 300, 200));
                  GUILayout.BeginVertical("box");

                  GUILayout.Label("Отладка WallPainter", EditorStyles.boldLabel);
                  GUILayout.Label($"Рейкастов в секунду: {raycastsPerSecond}");
                  GUILayout.Label($"Успешных попаданий: {successfulHitsCount}");
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

            [MenuItem("Tools/Wall Painting/Debug/Add Runtime Debugger")]
            public static void AddRuntimeDebugger()
            {
                  // Проверяем, есть ли уже отладчик в сцене
                  WallPainterRuntimeDebugger existingDebugger = FindObjectOfType<WallPainterRuntimeDebugger>();
                  if (existingDebugger != null)
                  {
                        Debug.Log("WallPainterRuntimeDebugger уже добавлен в сцену.");
                        Selection.activeGameObject = existingDebugger.gameObject;
                        return;
                  }

                  // Создаем новый объект для отладчика
                  GameObject debuggerObject = new GameObject("WallPainterRuntimeDebugger");
                  WallPainterRuntimeDebugger debugger = debuggerObject.AddComponent<WallPainterRuntimeDebugger>();

                  // Пытаемся найти WallPainter в сцене
                  MonoBehaviour wallPainter = null;
                  MonoBehaviour[] allComponents = FindObjectsOfType<MonoBehaviour>();
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
                        debugger.wallPainter = wallPainter;
                        Debug.Log($"WallPainterRuntimeDebugger добавлен в сцену и связан с WallPainter на объекте {wallPainter.gameObject.name}.");
                  }
                  else
                  {
                        Debug.LogWarning("WallPainterRuntimeDebugger добавлен в сцену, но не найден компонент WallPainter. Пожалуйста, назначьте его вручную.");
                  }

                  // Выбираем созданный объект
                  Selection.activeGameObject = debuggerObject;
            }
      }
}
#endif