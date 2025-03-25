using UnityEngine;
using UnityEngine.UI;

namespace Remalux.WallPainting.Vision
{
      public static class NonARWallPaintingSetup
      {
            public static void CreateNonARWallPaintingScene()
            {
                  // Create main camera
                  var cameraObject = new GameObject("Main Camera");
                  var camera = cameraObject.AddComponent<Camera>();
                  camera.tag = "MainCamera";
                  camera.clearFlags = CameraClearFlags.Skybox;
                  camera.backgroundColor = Color.black;

                  // Create wall detector
                  var detectorObject = new GameObject("WallDetector");
                  var wallDetector = detectorObject.AddComponent<WallDetector>();

                  // Create controller
                  var controllerObject = new GameObject("WallPaintingController");
                  var controller = controllerObject.AddComponent<RealWallPaintingController>();

                  // Create UI canvas
                  var canvasObject = new GameObject("UI Canvas");
                  var canvas = canvasObject.AddComponent<Canvas>();
                  canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                  canvasObject.AddComponent<CanvasScaler>();
                  canvasObject.AddComponent<GraphicRaycaster>();

                  // Create camera preview
                  var previewObject = new GameObject("Camera Preview");
                  previewObject.transform.SetParent(canvasObject.transform, false);
                  var preview = previewObject.AddComponent<RawImage>();
                  var previewRect = preview.rectTransform;
                  previewRect.anchorMin = Vector2.zero;
                  previewRect.anchorMax = Vector2.one;
                  previewRect.sizeDelta = Vector2.zero;

                  // Create buttons
                  CreateButton("Capture Button", canvasObject.transform, new Vector2(0.5f, 0.1f), "Capture");
                  CreateButton("Reset Button", canvasObject.transform, new Vector2(0.8f, 0.1f), "Reset");

                  // Setup references
                  controller.GetComponent<RealWallPaintingController>();
                  // Note: You'll need to set up the references in the inspector
                  // or add public methods to set them programmatically

                  Debug.Log("Non-AR wall painting scene created successfully!");
            }

            private static void CreateButton(string name, Transform parent, Vector2 anchorPosition, string text)
            {
                  var buttonObject = new GameObject(name);
                  buttonObject.transform.SetParent(parent, false);
                  var button = buttonObject.AddComponent<Button>();
                  var image = buttonObject.AddComponent<Image>();

                  var textObject = new GameObject("Text");
                  textObject.transform.SetParent(buttonObject.transform, false);
                  var textComponent = textObject.AddComponent<Text>();
                  textComponent.text = text;
                  textComponent.alignment = TextAnchor.MiddleCenter;
                  textComponent.color = Color.black;

                  var buttonRect = button.GetComponent<RectTransform>();
                  buttonRect.anchorMin = buttonRect.anchorMax = anchorPosition;
                  buttonRect.sizeDelta = new Vector2(160, 40);

                  var textRect = textComponent.rectTransform;
                  textRect.anchorMin = Vector2.zero;
                  textRect.anchorMax = Vector2.one;
                  textRect.sizeDelta = Vector2.zero;
            }
      }
}