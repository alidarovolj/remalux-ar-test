using UnityEngine;
using UnityEngine.UI;

namespace Remalux.WallPainting.Vision
{
      public static class RealWallPaintingSetup
      {
            public static void CreateRealWallPaintingScene()
            {
                  // Create main camera
                  var cameraObject = new GameObject("Main Camera");
                  var camera = cameraObject.AddComponent<Camera>();
                  camera.tag = "MainCamera";
                  camera.clearFlags = CameraClearFlags.Skybox;
                  camera.backgroundColor = Color.black;

                  // Create wall detector with OpenCV
                  var detectorObject = new GameObject("WallDetector");
                  var wallDetector = detectorObject.AddComponent<WallDetector>();

                  // Create room manager
                  var roomManagerObject = new GameObject("RoomManager");
                  var roomManager = roomManagerObject.AddComponent<RoomManager>();
                  roomManager.CreateDefaultRoom(); // Create a default room to start with

                  // Create texture manager
                  var textureManagerObject = new GameObject("TextureManager");
                  var textureManager = textureManagerObject.AddComponent<TextureManager>();

                  // Create controller
                  var controllerObject = new GameObject("WallPaintingController");
                  var controller = controllerObject.AddComponent<RealWallPaintingController>();

                  // Create background canvas for camera preview
                  var bgCanvasObject = new GameObject("Background Canvas");
                  var bgCanvas = bgCanvasObject.AddComponent<Canvas>();
                  bgCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                  bgCanvas.sortingOrder = 0; // Ensure it's behind the UI
                  var bgScaler = bgCanvasObject.AddComponent<CanvasScaler>();
                  bgScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                  bgScaler.referenceResolution = new Vector2(1920, 1080);
                  bgCanvasObject.AddComponent<GraphicRaycaster>();

                  // Create camera preview
                  var previewObject = new GameObject("Camera Preview");
                  previewObject.transform.SetParent(bgCanvasObject.transform, false);
                  var preview = previewObject.AddComponent<RawImage>();
                  var previewRect = preview.rectTransform;
                  previewRect.anchorMin = Vector2.zero;
                  previewRect.anchorMax = Vector2.one;
                  previewRect.sizeDelta = Vector2.zero;
                  previewRect.offsetMin = Vector2.zero;
                  previewRect.offsetMax = Vector2.zero;

                  // Create UI canvas for buttons
                  var uiCanvasObject = new GameObject("UI Canvas");
                  var uiCanvas = uiCanvasObject.AddComponent<Canvas>();
                  uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                  uiCanvas.sortingOrder = 1; // Ensure it's in front of the camera preview
                  var uiScaler = uiCanvasObject.AddComponent<CanvasScaler>();
                  uiScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                  uiScaler.referenceResolution = new Vector2(1920, 1080);
                  uiCanvasObject.AddComponent<GraphicRaycaster>();

                  // Create buttons
                  var captureButton = CreateButton("Capture Button", uiCanvasObject.transform, new Vector2(0.5f, 0.1f), "Capture");
                  var resetButton = CreateButton("Reset Button", uiCanvasObject.transform, new Vector2(0.8f, 0.1f), "Reset");

                  // Make buttons semi-transparent
                  var captureImage = captureButton.GetComponent<Image>();
                  var resetImage = resetButton.GetComponent<Image>();
                  var buttonColor = new Color(1f, 1f, 1f, 0.8f);
                  captureImage.color = buttonColor;
                  resetImage.color = buttonColor;

                  // Setup references directly
                  controller.mainCamera = camera;
                  controller.wallDetector = wallDetector;
                  controller.textureManager = textureManager;
                  controller.cameraPreview = preview;
                  controller.captureButton = captureButton;
                  controller.resetButton = resetButton;

                  // Setup wall detector
                  wallDetector.SetDebugImageDisplay(preview);

                  // Organize hierarchy
                  roomManagerObject.transform.SetParent(controllerObject.transform);
                  textureManagerObject.transform.SetParent(controllerObject.transform);
                  detectorObject.transform.SetParent(controllerObject.transform);
                  bgCanvasObject.transform.SetParent(controllerObject.transform);
                  uiCanvasObject.transform.SetParent(controllerObject.transform);

                  // Position the camera
                  cameraObject.transform.position = new Vector3(0, 1.6f, 0); // Примерная высота глаз
                  cameraObject.transform.SetParent(controllerObject.transform);

                  // Ensure the controller is enabled
                  controller.enabled = true;

                  Debug.Log("Real wall painting scene created successfully!");
            }

            private static Button CreateButton(string name, Transform parent, Vector2 anchorPosition, string text)
            {
                  // Create button object
                  var buttonObject = new GameObject(name);
                  buttonObject.transform.SetParent(parent, false);

                  // Add required components
                  var button = buttonObject.AddComponent<Button>();
                  var image = buttonObject.AddComponent<Image>();

                  // Set button colors
                  var colors = button.colors;
                  colors.normalColor = new Color(1f, 1f, 1f, 0.8f);
                  colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
                  colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
                  button.colors = colors;

                  // Create text object
                  var textObject = new GameObject("Text");
                  textObject.transform.SetParent(buttonObject.transform, false);
                  var textComponent = textObject.AddComponent<Text>();
                  textComponent.text = text;
                  textComponent.alignment = TextAnchor.MiddleCenter;
                  textComponent.color = Color.black;

                  // Try to get the default font
                  var fonts = Resources.FindObjectsOfTypeAll<Font>();
                  if (fonts != null && fonts.Length > 0)
                  {
                        textComponent.font = fonts[0];
                  }
                  else
                  {
                        Debug.LogWarning("No fonts found in the project. Text might not be visible.");
                  }

                  textComponent.fontSize = 24;
                  textComponent.resizeTextForBestFit = true;
                  textComponent.resizeTextMinSize = 12;
                  textComponent.resizeTextMaxSize = 32;

                  // Set button rectangle transform
                  var buttonRect = button.GetComponent<RectTransform>();
                  buttonRect.anchorMin = buttonRect.anchorMax = anchorPosition;
                  buttonRect.sizeDelta = new Vector2(160, 40);
                  buttonRect.anchoredPosition = Vector2.zero;

                  // Set text rectangle transform
                  var textRect = textComponent.rectTransform;
                  textRect.anchorMin = Vector2.zero;
                  textRect.anchorMax = Vector2.one;
                  textRect.sizeDelta = Vector2.zero;
                  textRect.offsetMin = Vector2.zero;
                  textRect.offsetMax = Vector2.zero;

                  return button;
            }
      }
}