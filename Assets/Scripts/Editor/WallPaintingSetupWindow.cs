using UnityEngine;
using UnityEditor;
// Use reflection instead of direct reference
// using Unity.XR.CoreUtils;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

namespace Remalux.WallPainting.Editor
{
      // Renamed from WallPaintingSetupWindow to WallPaintingLegacySetupWindow
      // to clarify that this is the old version
      public class WallPaintingLegacySetupWindow : EditorWindow
      {
            private bool setupXR = true;
            private bool setupWallPainting = true;
            private float cameraYOffset = 1.6f;

            // Changed menu path to avoid conflicts
            [MenuItem("Window/Remalux/Legacy Setup Tools/Legacy Wall Painting Setup")]
            public static void ShowWindow()
            {
                  GetWindow<WallPaintingLegacySetupWindow>("Legacy Wall Painting Setup");
            }

            private void OnGUI()
            {
                  GUILayout.Label("Legacy Wall Painting System Setup", EditorStyles.boldLabel);
                  EditorGUILayout.Space();

                  EditorGUILayout.HelpBox("This is the legacy setup tool. Consider using the new setup from 'Window/Remalux/Wall Painting Setup Window'", MessageType.Warning);
                  EditorGUILayout.Space();

                  setupXR = EditorGUILayout.Toggle("Setup XR", setupXR);
                  setupWallPainting = EditorGUILayout.Toggle("Setup Wall Painting", setupWallPainting);

                  if (setupXR)
                  {
                        cameraYOffset = EditorGUILayout.FloatField("Camera Y Offset", cameraYOffset);
                  }

                  EditorGUILayout.Space();
                  if (GUILayout.Button("Setup Scene (Legacy)"))
                  {
                        SetupScene();
                  }
            }

            private void SetupScene()
            {
                  // To avoid errors with missing types, use Type.GetType instead of direct references
                  GameObject root = null;

                  // Legacy XR setup with reflection to avoid dependencies
                  if (setupXR)
                  {
                        // Find XROrigin by reflection
                        var xrOriginType = System.Type.GetType("Unity.XR.CoreUtils.XROrigin, Unity.XR.CoreUtils");
                        MonoBehaviour xrOrigin = null;

                        if (xrOriginType != null)
                        {
                              xrOrigin = Object.FindFirstObjectByType(xrOriginType) as MonoBehaviour;
                        }

                        if (xrOrigin == null)
                        {
                              // Create XR Origin structure
                              root = new GameObject("XR Origin");

                              if (xrOriginType != null)
                              {
                                    xrOrigin = root.AddComponent(xrOriginType) as MonoBehaviour;
                              }
                        }
                        else
                        {
                              root = xrOrigin.gameObject;
                        }

                        // Set up camera with safer approach
                        GameObject cameraOffset = null;

                        if (xrOrigin != null)
                        {
                              var offsetProperty = xrOriginType.GetProperty("CameraFloorOffsetObject");
                              if (offsetProperty != null)
                              {
                                    cameraOffset = offsetProperty.GetValue(xrOrigin) as GameObject;

                                    if (cameraOffset == null)
                                    {
                                          cameraOffset = new GameObject("Camera Offset");
                                          cameraOffset.transform.SetParent(root.transform, false);
                                          offsetProperty.SetValue(xrOrigin, cameraOffset);
                                    }
                              }
                        }
                        else
                        {
                              // Fallback if XROrigin not available
                              cameraOffset = new GameObject("Camera Offset");
                              if (root != null)
                              {
                                    cameraOffset.transform.SetParent(root.transform, false);
                              }
                        }

                        // Setup Camera
                        Camera mainCamera = Camera.main;
                        if (mainCamera == null)
                        {
                              GameObject cameraObj = new GameObject("Main Camera");
                              mainCamera = cameraObj.AddComponent<Camera>();
                              mainCamera.tag = "MainCamera";
                        }

                        if (cameraOffset != null && mainCamera != null)
                        {
                              mainCamera.transform.SetParent(cameraOffset.transform, false);

                              if (xrOrigin != null)
                              {
                                    var cameraProperty = xrOriginType.GetProperty("Camera");
                                    if (cameraProperty != null)
                                    {
                                          cameraProperty.SetValue(xrOrigin, mainCamera);
                                    }

                                    var offsetProperty = xrOriginType.GetProperty("CameraYOffset");
                                    if (offsetProperty != null)
                                    {
                                          offsetProperty.SetValue(xrOrigin, cameraYOffset);
                                    }
                              }
                        }
                  }

                  // Setup in a safer way
                  if (setupWallPainting)
                  {
                        GameObject controllerObj = new GameObject("RealWallPaintingController");

                        // Try to find and add components using reflection
                        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                        bool controllerAdded = false;

                        foreach (var assembly in assemblies)
                        {
                              var controllerType = assembly.GetType("Remalux.WallPainting.Vision.RealWallPaintingController");
                              if (controllerType != null)
                              {
                                    controllerObj.AddComponent(controllerType);
                                    controllerAdded = true;
                                    break;
                              }
                        }

                        if (!controllerAdded)
                        {
                              Debug.LogWarning("Could not find RealWallPaintingController type. The component was not added.");
                        }

                        // Create camera preview
                        var previewCanvas = SetupCanvas("Preview Canvas", controllerObj.transform);
                        var previewImage = CreateRawImage("Camera Preview", previewCanvas.transform);

                        // Create test room
                        GameObject testRoom = new GameObject("Test Room");
                        testRoom.transform.SetParent(controllerObj.transform, false);

                        string[] walls = { "Floor", "Front Wall", "Back Wall", "Left Wall", "Right Wall" };
                        foreach (var wall in walls)
                        {
                              GameObject wallObj = new GameObject(wall);
                              wallObj.transform.SetParent(testRoom.transform, false);
                        }
                  }

                  Debug.Log("Legacy Wall Painting System setup completed!");
            }

            private Canvas SetupCanvas(string name, Transform parent)
            {
                  GameObject canvasObj = new GameObject(name);
                  canvasObj.transform.SetParent(parent, false);

                  Canvas canvas = canvasObj.AddComponent<Canvas>();
                  canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                  canvasObj.AddComponent<CanvasScaler>();
                  canvasObj.AddComponent<GraphicRaycaster>();

                  return canvas;
            }

            private RawImage CreateRawImage(string name, Transform parent)
            {
                  GameObject imageObj = new GameObject(name);
                  imageObj.transform.SetParent(parent, false);

                  RawImage rawImage = imageObj.AddComponent<RawImage>();
                  RectTransform rectTransform = imageObj.GetComponent<RectTransform>();

                  rectTransform.anchorMin = new Vector2(0.7f, 0.05f);
                  rectTransform.anchorMax = new Vector2(0.95f, 0.25f);
                  rectTransform.offsetMin = Vector2.zero;
                  rectTransform.offsetMax = Vector2.zero;

                  rawImage.color = new Color(1, 1, 1, 0.8f);

                  return rawImage;
            }
      }
}