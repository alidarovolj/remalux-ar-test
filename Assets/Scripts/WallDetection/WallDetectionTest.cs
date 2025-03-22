#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace Remalux.WallDetection
{
      public class WallDetectionTest : MonoBehaviour
      {
            [Header("Prefabs")]
            [SerializeField] private GameObject debugQuadPrefab;

            private void Awake()
            {
                  SetupScene();
            }

            private void SetupScene()
            {
                  // Создаем Canvas для UI
                  GameObject canvasObj = new GameObject("WallDetectionCanvas");
                  Canvas canvas = canvasObj.AddComponent<Canvas>();
                  canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                  canvasObj.AddComponent<CanvasScaler>();
                  canvasObj.AddComponent<GraphicRaycaster>();

                  // Создаем RawImage для отображения видео
                  GameObject rawImageObj = new GameObject("CameraView");
                  rawImageObj.transform.SetParent(canvasObj.transform, false);
                  RawImage rawImage = rawImageObj.AddComponent<RawImage>();
                  AspectRatioFitter aspectFitter = rawImageObj.AddComponent<AspectRatioFitter>();
                  aspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

                  // Настраиваем RawImage на весь экран
                  RectTransform rectTransform = rawImageObj.GetComponent<RectTransform>();
                  rectTransform.anchorMin = Vector2.zero;
                  rectTransform.anchorMax = Vector2.one;
                  rectTransform.sizeDelta = Vector2.zero;
                  rectTransform.anchoredPosition = Vector2.zero;

                  // Создаем GameObject для детектора стен
                  GameObject detectorObj = new GameObject("WallDetector");

                  // Добавляем компоненты
                  WallDetector wallDetector = detectorObj.AddComponent<WallDetector>();
                  CameraController cameraController = detectorObj.AddComponent<CameraController>();

                  // Создаем quad для отладочной визуализации
                  GameObject debugQuad = null;
                  if (debugQuadPrefab != null)
                  {
                        debugQuad = Instantiate(debugQuadPrefab);
                  }
                  else
                  {
                        debugQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        debugQuad.transform.localScale = new Vector3(1.6f, 0.9f, 1); // 16:9 aspect ratio
                  }
                  debugQuad.name = "DebugView";
                  debugQuad.transform.position = new Vector3(0, 0, 2);

                  // Настраиваем связи между компонентами
                  var cameraControllerType = cameraController.GetType();
                  var wallDetectorField = cameraControllerType.GetField("wallDetector", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                  var displayImageField = cameraControllerType.GetField("displayImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                  var aspectRatioFitterField = cameraControllerType.GetField("aspectRatioFitter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                  if (wallDetectorField != null) wallDetectorField.SetValue(cameraController, wallDetector);
                  if (displayImageField != null) displayImageField.SetValue(cameraController, rawImage);
                  if (aspectRatioFitterField != null) aspectRatioFitterField.SetValue(cameraController, aspectFitter);

#if UNITY_EDITOR
                  // Настраиваем параметры детектора в редакторе
                  var serializedWallDetector = new SerializedObject(wallDetector);
                  serializedWallDetector.FindProperty("debugQuad").objectReferenceValue = debugQuad;
                  serializedWallDetector.FindProperty("minWallLength").floatValue = 100f;
                  serializedWallDetector.FindProperty("verticalAngleThreshold").floatValue = 5f;
                  serializedWallDetector.FindProperty("lineClusterThreshold").floatValue = 30f;
                  serializedWallDetector.FindProperty("cannyThreshold1").doubleValue = 50;
                  serializedWallDetector.FindProperty("cannyThreshold2").doubleValue = 150;
                  serializedWallDetector.FindProperty("cannyApertureSize").intValue = 3;
                  serializedWallDetector.FindProperty("showDebugImage").boolValue = true;
                  serializedWallDetector.ApplyModifiedProperties();
#else
                  // Настраиваем параметры детектора в рантайме через рефлексию
                  var wallDetectorType = wallDetector.GetType();
                  var debugQuadField = wallDetectorType.GetField("debugQuad", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                  var minWallLengthField = wallDetectorType.GetField("minWallLength", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                  var verticalAngleThresholdField = wallDetectorType.GetField("verticalAngleThreshold", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                  var lineClusterThresholdField = wallDetectorType.GetField("lineClusterThreshold", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                  var cannyThreshold1Field = wallDetectorType.GetField("cannyThreshold1", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                  var cannyThreshold2Field = wallDetectorType.GetField("cannyThreshold2", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                  var cannyApertureSizeField = wallDetectorType.GetField("cannyApertureSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                  var showDebugImageField = wallDetectorType.GetField("showDebugImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                  if (debugQuadField != null) debugQuadField.SetValue(wallDetector, debugQuad);
                  if (minWallLengthField != null) minWallLengthField.SetValue(wallDetector, 100f);
                  if (verticalAngleThresholdField != null) verticalAngleThresholdField.SetValue(wallDetector, 5f);
                  if (lineClusterThresholdField != null) lineClusterThresholdField.SetValue(wallDetector, 30f);
                  if (cannyThreshold1Field != null) cannyThreshold1Field.SetValue(wallDetector, 50d);
                  if (cannyThreshold2Field != null) cannyThreshold2Field.SetValue(wallDetector, 150d);
                  if (cannyApertureSizeField != null) cannyApertureSizeField.SetValue(wallDetector, 3);
                  if (showDebugImageField != null) showDebugImageField.SetValue(wallDetector, true);
#endif

                  Debug.Log("Wall Detection test scene setup complete!");
            }
      }
}