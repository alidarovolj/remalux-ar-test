using UnityEngine;
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
            private GUIStyle debugLabelStyle;

            private void Start()
            {
                  InitializeVisualizer();
                  InitializeGUIStyle();
            }

            private void InitializeGUIStyle()
            {
                  debugLabelStyle = new GUIStyle();
                  debugLabelStyle.normal.textColor = Color.white;
                  debugLabelStyle.fontSize = 14;
                  debugLabelStyle.padding = new RectOffset(5, 5, 5, 5);
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
            }

            private void OnGUI()
            {
                  if (!showDebugWindow || !isInitialized)
                        return;

                  float width = 300;
                  float height = 200;
                  float x = 10;
                  float y = 10;

                  // Создаем полупрозрачный фон
                  GUI.color = new Color(0, 0, 0, 0.7f);
                  GUI.Box(new Rect(x, y, width, height), "");
                  GUI.color = Color.white;

                  // Отображаем статистику
                  float lineHeight = 20;
                  float currentY = y + 10;

                  GUI.Label(new Rect(x + 10, currentY, width - 20, lineHeight), "Отладка WallPainter", debugLabelStyle);
                  currentY += lineHeight;

                  GUI.Label(new Rect(x + 10, currentY, width - 20, lineHeight), $"Рейкастов в секунду: {raycastsPerSecond}", debugLabelStyle);
                  currentY += lineHeight;

                  GUI.Label(new Rect(x + 10, currentY, width - 20, lineHeight), $"Всего рейкастов: {totalRaycasts}", debugLabelStyle);
                  currentY += lineHeight;

                  GUI.Label(new Rect(x + 10, currentY, width - 20, lineHeight), $"Успешных попаданий: {successfulHits}", debugLabelStyle);
                  currentY += lineHeight;

                  GUI.Label(new Rect(x + 10, currentY, width - 20, lineHeight), $"Последний объект: {lastHitObjectName}", debugLabelStyle);
                  currentY += lineHeight;

                  GUI.Label(new Rect(x + 10, currentY, width - 20, lineHeight), $"Слой объекта: {lastHitObjectLayer}", debugLabelStyle);
                  currentY += lineHeight;

                  GUI.Label(new Rect(x + 10, currentY, width - 20, lineHeight), $"Точка попадания: {lastHitPoint}", debugLabelStyle);
                  currentY += lineHeight;

                  if (wallLayerMask.value > 0)
                  {
                        GUI.Label(new Rect(x + 10, currentY, width - 20, lineHeight), $"Маска слоя: {wallLayerMask.value}", debugLabelStyle);
                        currentY += lineHeight;

                        int wallLayer = 8;
                        string layerStatus = ((wallLayerMask.value & (1 << wallLayer)) == 0) ?
                              "Слой 'Wall' НЕ включен в маску!" :
                              "Слой 'Wall' включен в маску";
                        GUI.Label(new Rect(x + 10, currentY, width - 20, lineHeight), layerStatus, debugLabelStyle);
                  }
                  else
                  {
                        GUI.Label(new Rect(x + 10, currentY, width - 20, lineHeight), "Маска слоя не задана!", debugLabelStyle);
                  }
            }
      }
}