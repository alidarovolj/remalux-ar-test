using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace Remalux.WallPainting.Setup
{
      public class WallPaintingSceneSetup : MonoBehaviour
      {
            [Header("XR Setup")]
            [SerializeField] private bool setupXR = true;
            [SerializeField] private float cameraYOffset = 1.6f;

            [Header("UI Setup")]
            [SerializeField] private bool setupUI = true;

            [Header("Wall Painting")]
            [SerializeField] private bool setupWallPainting = true;

            // References to created objects
            private GameObject cameraRig;
            private GameObject cameraOffset;
            private Camera mainCamera;
            private Canvas mainCanvas;
            private RawImage cameraPreview;
            private Vision.RealWallPaintingController paintingController;
            private Vision.WallDetector wallDetector;

            // Добавляем класс для обработки нажатия кнопки
            private class CameraToggleButton : MonoBehaviour
            {
                  public RawImage cameraPreview;
                  private bool isFullscreen = false;

                  void Start()
                  {
                        GetComponent<Button>().onClick.AddListener(ToggleCameraMode);
                  }

                  void ToggleCameraMode()
                  {
                        isFullscreen = !isFullscreen;

                        // Находим контроллер для переключения режима
                        var controller = FindFirstObjectByType<Vision.RealWallPaintingController>();
                        if (controller != null)
                        {
                              controller.SetCameraViewMode(isFullscreen);
                        }
                        else if (cameraPreview != null)
                        {
                              // Если контроллер не найден, меняем размер напрямую
                              RectTransform rectTransform = cameraPreview.rectTransform;

                              if (isFullscreen)
                              {
                                    rectTransform.anchorMin = new Vector2(0, 0);
                                    rectTransform.anchorMax = new Vector2(1, 1);
                                    cameraPreview.color = Color.white;
                              }
                              else
                              {
                                    rectTransform.anchorMin = new Vector2(0.7f, 0.05f);
                                    rectTransform.anchorMax = new Vector2(0.95f, 0.25f);
                                    cameraPreview.color = new Color(1, 1, 1, 0.8f);
                              }

                              rectTransform.offsetMin = Vector2.zero;
                              rectTransform.offsetMax = Vector2.zero;
                        }

                        // Обновляем текст кнопки
                        Text buttonText = GetComponentInChildren<Text>();
                        if (buttonText != null)
                        {
                              buttonText.text = isFullscreen ? "ПРЕВЬЮ" : "КАМЕРА";
                        }
                  }
            }

            private void Start()
            {
                  // Setup in the correct order
                  if (setupXR)
                        SetupXR();

                  if (setupUI)
                        SetupUI();

                  if (setupWallPainting)
                        SetupWallPainting();

                  // Final connections
                  ConnectComponents();

                  // Display setup completion message
                  Debug.Log("WallPaintingSceneSetup: Scene setup completed successfully!");
            }

            private void SetupXR()
            {
                  // Check if camera already exists
                  var existingCamera = Camera.main;
                  if (existingCamera != null)
                  {
                        mainCamera = existingCamera;
                        Debug.Log("Using existing main camera");
                  }
                  else
                  {
                        // Create new camera setup
                        cameraRig = new GameObject("Camera Rig");

                        // Create camera offset
                        cameraOffset = new GameObject("Camera Offset");
                        cameraOffset.transform.SetParent(cameraRig.transform, false);
                        cameraOffset.transform.localPosition = new Vector3(0, cameraYOffset, 0);

                        // Create the camera
                        GameObject cameraObj = new GameObject("Main Camera");
                        mainCamera = cameraObj.AddComponent<Camera>();
                        mainCamera.tag = "MainCamera";
                        cameraObj.AddComponent<AudioListener>();

                        // Set parent
                        mainCamera.transform.SetParent(cameraOffset.transform, false);

                        Debug.Log("Created new camera rig");
                  }

                  // Add tracked pose driver using reflection to avoid direct dependencies
                  TryAddTrackedPoseDriver();
            }

            private void SetupUI()
            {
                  // Create Canvas if it doesn't exist
                  var existingCanvas = FindFirstObjectByType<Canvas>();
                  if (existingCanvas != null && existingCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                  {
                        mainCanvas = existingCanvas;
                        Debug.Log("Using existing canvas");
                  }
                  else
                  {
                        // Create new canvas
                        GameObject canvasObj = new GameObject("Main Canvas");
                        mainCanvas = canvasObj.AddComponent<Canvas>();
                        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                        canvasObj.AddComponent<CanvasScaler>();
                        canvasObj.AddComponent<GraphicRaycaster>();
                        Debug.Log("Created new canvas");
                  }

                  // Create EventSystem if it doesn't exist
                  if (FindFirstObjectByType<EventSystem>() == null)
                  {
                        GameObject eventSystemObj = new GameObject("Event System");
                        EventSystem eventSystem = eventSystemObj.AddComponent<EventSystem>();

                        // Пытаемся добавить InputSystemUIInputModule
                        bool success = TryAddInputSystemModule(eventSystemObj);

                        if (!success)
                        {
                              Debug.LogError("Failed to add InputSystemUIInputModule. Please add the Input System package (Window > Package Manager).");
                              Debug.LogError("As a workaround, a disabled StandaloneInputModule has been added to prevent errors.");

                              // Add disabled StandaloneInputModule as fallback
                              var standaloneModule = eventSystemObj.AddComponent<StandaloneInputModule>();
                              standaloneModule.enabled = false;
                        }

                        Debug.Log("Created new EventSystem with appropriate input module");
                  }

                  // Создаем родительский объект для превью камеры без рамки
                  GameObject previewParentObj = new GameObject("Camera Preview Parent");
                  previewParentObj.transform.SetParent(mainCanvas.transform, false);
                  RectTransform previewParentRect = previewParentObj.AddComponent<RectTransform>();

                  // Позиционируем на весь экран без рамок
                  previewParentRect.anchorMin = Vector2.zero;
                  previewParentRect.anchorMax = Vector2.one;
                  previewParentRect.offsetMin = Vector2.zero;
                  previewParentRect.offsetMax = Vector2.zero;
                  previewParentRect.sizeDelta = Vector2.zero;

                  // Создаем объект превью камеры
                  GameObject previewObj = new GameObject("Camera Preview");
                  previewObj.transform.SetParent(previewParentObj.transform, false);

                  RectTransform previewRect = previewObj.AddComponent<RectTransform>();
                  previewRect.anchorMin = Vector2.zero;
                  previewRect.anchorMax = Vector2.one;
                  previewRect.offsetMin = Vector2.zero;
                  previewRect.offsetMax = Vector2.zero;
                  previewRect.sizeDelta = Vector2.zero;

                  cameraPreview = previewObj.AddComponent<RawImage>();
                  cameraPreview.color = Color.white; // Полная непрозрачность для полноэкранного режима

                  // Создаем кнопку переключения размера камеры
                  GameObject toggleCameraButtonObj = new GameObject("Toggle Camera Button");
                  toggleCameraButtonObj.transform.SetParent(mainCanvas.transform, false);

                  RectTransform toggleButtonRect = toggleCameraButtonObj.AddComponent<RectTransform>();
                  toggleButtonRect.anchorMin = new Vector2(0.85f, 0.90f);
                  toggleButtonRect.anchorMax = new Vector2(0.98f, 0.97f);
                  toggleButtonRect.offsetMin = Vector2.zero;
                  toggleButtonRect.offsetMax = Vector2.zero;

                  Image toggleButtonImage = toggleCameraButtonObj.AddComponent<Image>();
                  toggleButtonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

                  Button toggleCameraButton = toggleCameraButtonObj.AddComponent<Button>();
                  toggleCameraButton.targetGraphic = toggleButtonImage;

                  // Добавляем текст к кнопке
                  GameObject buttonTextObj = new GameObject("Text");
                  buttonTextObj.transform.SetParent(toggleCameraButtonObj.transform, false);

                  Text buttonText = buttonTextObj.AddComponent<Text>();
                  buttonText.text = "КАМЕРА";
                  buttonText.fontSize = 16;
                  buttonText.alignment = TextAnchor.MiddleCenter;
                  buttonText.color = Color.white;

                  // Пытаемся найти шрифт
                  Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                  if (font == null)
                  {
                        Font[] fonts = Object.FindObjectsByType<Font>(FindObjectsSortMode.None);
                        if (fonts.Length > 0)
                        {
                              font = fonts[0];
                        }
                  }
                  if (font != null)
                  {
                        buttonText.font = font;
                  }

                  // Устанавливаем размеры текста
                  RectTransform textRect = buttonTextObj.GetComponent<RectTransform>();
                  textRect.anchorMin = Vector2.zero;
                  textRect.anchorMax = Vector2.one;
                  textRect.offsetMin = Vector2.zero;
                  textRect.offsetMax = Vector2.zero;

                  // Добавляем обработчик нажатия через компонент
                  var buttonHandler = toggleCameraButtonObj.AddComponent<CameraToggleButton>();
                  buttonHandler.cameraPreview = cameraPreview;

                  Debug.Log("Created camera preview UI with toggle button");
            }

            private void SetupWallPainting()
            {
                  // Create controller if not already in scene
                  var existingController = FindFirstObjectByType<Vision.RealWallPaintingController>();
                  if (existingController != null)
                  {
                        paintingController = existingController;
                        Debug.Log("Using existing wall painting controller");
                  }
                  else
                  {
                        // Create controller
                        GameObject controllerObj = new GameObject("WallPaintingController");
                        paintingController = controllerObj.AddComponent<Vision.RealWallPaintingController>();
                        Debug.Log("Created new wall painting controller");
                  }

                  // Create wall detector if not already in scene
                  var existingDetector = FindFirstObjectByType<Vision.WallDetector>();
                  if (existingDetector != null)
                  {
                        wallDetector = existingDetector;
                        Debug.Log("Using existing wall detector");
                  }
                  else
                  {
                        // Create wall detector (on the controller object to avoid reference issues)
                        wallDetector = paintingController.gameObject.AddComponent<Vision.WallDetector>();
                        Debug.Log("Created new wall detector");
                  }

                  // Create texture manager if not already in scene
                  var existingTextureManager = FindFirstObjectByType<TextureManager>();
                  if (existingTextureManager != null)
                  {
                        Debug.Log("Using existing texture manager");
                  }
                  else
                  {
                        // Create a new texture manager
                        GameObject textureManagerObj = new GameObject("TextureManager");
                        textureManagerObj.transform.SetParent(paintingController.transform);
                        var textureManager = textureManagerObj.AddComponent<TextureManager>();

                        // Create default materials
                        Material defaultMaterial = new Material(Shader.Find("Standard"));
                        defaultMaterial.color = Color.white;

                        // Set up basic colors
                        CreateDefaultMaterials(textureManager);

                        Debug.Log("Created new TextureManager");
                  }

                  // Create a placeholder texture for the camera preview
                  var placeholderTexture = new Texture2D(2, 2);
                  var colors = new Color[4] { Color.black, Color.black, Color.black, Color.black };
                  placeholderTexture.SetPixels(colors);
                  placeholderTexture.Apply();

                  if (cameraPreview != null)
                  {
                        cameraPreview.texture = placeholderTexture;
                  }
            }

            private void CreateDefaultMaterials(TextureManager textureManager)
            {
                  // Create default materials and colors using reflection to avoid direct field access
                  try
                  {
                        // Create sample materials
                        Material[] materials = new Material[5];
                        for (int i = 0; i < materials.Length; i++)
                        {
                              materials[i] = new Material(Shader.Find("Standard"));
                        }

                        // Set different colors
                        materials[0].color = Color.white;
                        materials[1].color = new Color(0.9f, 0.6f, 0.3f);  // Beige
                        materials[2].color = new Color(0.8f, 0.8f, 0.8f);  // Light gray
                        materials[3].color = new Color(0.3f, 0.6f, 0.9f);  // Light blue
                        materials[4].color = new Color(0.8f, 0.9f, 0.8f);  // Light green

                        // Use reflection to set the materials in TextureManager
                        var texturePresetsField = textureManager.GetType().GetField("texturePresets",
                                                                                  System.Reflection.BindingFlags.Instance |
                                                                                  System.Reflection.BindingFlags.Public |
                                                                                  System.Reflection.BindingFlags.NonPublic);

                        if (texturePresetsField != null)
                        {
                              // Create TexturePreset objects using the TexturePreset struct/class from TextureManager
                              var texturePresetType = System.Type.GetType("Remalux.WallPainting.TextureManager+TexturePreset, Assembly-CSharp");
                              if (texturePresetType == null)
                              {
                                    // Try as a standalone type
                                    texturePresetType = System.Type.GetType("Remalux.WallPainting.TexturePreset, Assembly-CSharp");
                              }

                              if (texturePresetType != null)
                              {
                                    // Create a list to hold the presets
                                    var listType = typeof(List<>).MakeGenericType(texturePresetType);
                                    var presetsList = System.Activator.CreateInstance(listType);
                                    var addMethod = listType.GetMethod("Add");

                                    for (int i = 0; i < materials.Length; i++)
                                    {
                                          var preset = System.Activator.CreateInstance(texturePresetType);

                                          // Set properties through reflection
                                          var nameField = texturePresetType.GetField("name");
                                          var materialField = texturePresetType.GetField("material");
                                          var colorField = texturePresetType.GetField("tintColor");

                                          if (nameField != null) nameField.SetValue(preset, $"Color {i + 1}");
                                          if (materialField != null) materialField.SetValue(preset, materials[i]);
                                          if (colorField != null) colorField.SetValue(preset, materials[i].color);

                                          // Add to list
                                          addMethod.Invoke(presetsList, new[] { preset });
                                    }

                                    // Assign the list to TextureManager
                                    texturePresetsField.SetValue(textureManager, presetsList);
                                    Debug.Log("Successfully created default material presets for TextureManager");
                              }
                        }
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogWarning($"Failed to create default materials: {e.Message}");
                  }
            }

            private void ConnectComponents()
            {
                  if (paintingController != null)
                  {
                        // Set main camera
                        var cameraProp = paintingController.GetType().GetField("mainCamera",
                                                                             System.Reflection.BindingFlags.Instance |
                                                                             System.Reflection.BindingFlags.Public);
                        if (cameraProp != null)
                              cameraProp.SetValue(paintingController, mainCamera);

                        // Set wall detector
                        var detectorProp = paintingController.GetType().GetField("wallDetector",
                                                                              System.Reflection.BindingFlags.Instance |
                                                                              System.Reflection.BindingFlags.Public);
                        if (detectorProp != null)
                              detectorProp.SetValue(paintingController, wallDetector);

                        // Set camera preview
                        var previewProp = paintingController.GetType().GetField("cameraPreview",
                                                                              System.Reflection.BindingFlags.Instance |
                                                                              System.Reflection.BindingFlags.Public);
                        if (previewProp != null)
                              previewProp.SetValue(paintingController, cameraPreview);
                  }

                  if (wallDetector != null)
                  {
                        // Set main camera
                        wallDetector.mainCamera = mainCamera;

                        // Initialize other required components explicitly
                        var textureResolutionProp = wallDetector.GetType().GetField("textureResolution",
                                                                               System.Reflection.BindingFlags.Instance |
                                                                               System.Reflection.BindingFlags.Public);
                        if (textureResolutionProp != null && textureResolutionProp.GetValue(wallDetector) == null)
                        {
                              textureResolutionProp.SetValue(wallDetector, new Vector2Int(640, 480));
                        }

                        // Ensure there's a default material
                        var defaultMaterialProp = wallDetector.GetType().GetField("defaultWallMaterial",
                                                                    System.Reflection.BindingFlags.Instance |
                                                                    System.Reflection.BindingFlags.Public);
                        if (defaultMaterialProp != null && defaultMaterialProp.GetValue(wallDetector) == null)
                        {
                              var defaultMaterial = new Material(Shader.Find("Standard"));
                              defaultMaterial.color = Color.white;
                              defaultMaterialProp.SetValue(wallDetector, defaultMaterial);
                        }
                  }

                  Debug.Log("Connected all components");
            }

            private void TryAddTrackedPoseDriver()
            {
                  if (mainCamera == null) return;

                  bool driverAdded = false;

                  // Try to add InputSystem Tracked Pose Driver using reflection to avoid direct dependencies
                  var tpdType = System.Type.GetType("UnityEngine.InputSystem.XR.TrackedPoseDriver");
                  if (tpdType != null)
                  {
                        // InputSystem is available
                        var trackedPoseDriver = mainCamera.gameObject.GetComponent(tpdType);
                        if (trackedPoseDriver == null)
                        {
                              trackedPoseDriver = mainCamera.gameObject.AddComponent(tpdType);
                              driverAdded = true;

                              // Try to set up actions using reflection
                              try
                              {
                                    var inputActionType = System.Type.GetType("UnityEngine.InputSystem.InputAction");
                                    var inputActionTypeType = System.Type.GetType("UnityEngine.InputSystem.InputActionType");

                                    if (inputActionType != null && inputActionTypeType != null)
                                    {
                                          // Get the Value enum value
                                          var valueField = inputActionTypeType.GetField("Value");
                                          object valueEnum = valueField.GetValue(null);

                                          // Create position action
                                          var posAction = System.Activator.CreateInstance(
                                              inputActionType,
                                              new object[] { "Position", valueEnum, "<XRHMD>/centerEyePosition" }
                                          );

                                          // Create rotation action
                                          var rotAction = System.Activator.CreateInstance(
                                              inputActionType,
                                              new object[] { "Rotation", valueEnum, "<XRHMD>/centerEyeRotation" }
                                          );

                                          // Set properties
                                          var posProperty = tpdType.GetProperty("positionAction");
                                          var rotProperty = tpdType.GetProperty("rotationAction");

                                          posProperty.SetValue(trackedPoseDriver, posAction);
                                          rotProperty.SetValue(trackedPoseDriver, rotAction);

                                          // Enable actions
                                          var enableMethod = inputActionType.GetMethod("Enable");
                                          enableMethod.Invoke(posAction, null);
                                          enableMethod.Invoke(rotAction, null);

                                          Debug.Log("Successfully configured InputSystem Tracked Pose Driver");
                                    }
                              }
                              catch (System.Exception e)
                              {
                                    Debug.LogWarning("Could not fully configure TrackedPoseDriver: " + e.Message);
                              }
                        }
                        else
                        {
                              driverAdded = true;
                        }
                  }

                  if (!driverAdded)
                  {
                        // Check for the legacy tracked pose driver
                        var legacyType = System.Type.GetType("UnityEngine.SpatialTracking.TrackedPoseDriver");
                        if (legacyType != null)
                        {
                              var driver = mainCamera.gameObject.GetComponent(legacyType);
                              if (driver == null)
                              {
                                    mainCamera.gameObject.AddComponent(legacyType);
                                    Debug.Log("Added legacy TrackedPoseDriver");
                                    driverAdded = true;
                              }
                              else
                              {
                                    driverAdded = true;
                              }
                        }
                  }

                  if (!driverAdded)
                  {
                        // No TrackedPoseDriver available - add a simple script to simulate head movement
                        var simulatorType = typeof(CameraSimulation);
                        if (mainCamera.gameObject.GetComponent(simulatorType) == null)
                        {
                              mainCamera.gameObject.AddComponent(simulatorType);
                              Debug.Log("Added CameraSimulation component as fallback for head tracking");
                        }
                  }
            }

            // Fallback camera movement simulation if no TrackedPoseDriver is available
            private class CameraSimulation : MonoBehaviour
            {
                  private float rotationSpeed = 1.0f;
                  private float moveSpeed = 0.1f;

                  private void Update()
                  {
                        // Use Input System if available
                        bool useInputSystem = CheckForInputSystemPackage();

                        if (useInputSystem)
                        {
                              // Use the new input system via reflection
                              TryUseInputSystemForCamera();
                        }
                        else
                        {
                              // Use legacy input system safely
                              TryUseLegacyInputForCamera();
                        }
                  }

                  private void TryUseInputSystemForCamera()
                  {
                        try
                        {
                              // Try to access Keyboard class
                              var keyboardType = System.Type.GetType("UnityEngine.InputSystem.Keyboard");
                              if (keyboardType != null)
                              {
                                    // Get keyboard.current
                                    var currentProp = keyboardType.GetProperty("current");
                                    var keyboard = currentProp.GetValue(null);

                                    // Define method to check if key is pressed
                                    var keyType = System.Type.GetType("UnityEngine.InputSystem.Key");
                                    var isKeyPressedMethod = keyboardType.GetMethod("IsKeyPressed");

                                    if (keyboard != null && isKeyPressedMethod != null && keyType != null)
                                    {
                                          // Forward/Backward movement
                                          if ((bool)isKeyPressedMethod.Invoke(keyboard, new object[] { keyType.GetField("W").GetValue(null) }))
                                          {
                                                transform.Translate(Vector3.forward * moveSpeed);
                                          }
                                          if ((bool)isKeyPressedMethod.Invoke(keyboard, new object[] { keyType.GetField("S").GetValue(null) }))
                                          {
                                                transform.Translate(Vector3.back * moveSpeed);
                                          }

                                          // Left/Right movement
                                          if ((bool)isKeyPressedMethod.Invoke(keyboard, new object[] { keyType.GetField("A").GetValue(null) }))
                                          {
                                                transform.Translate(Vector3.left * moveSpeed);
                                          }
                                          if ((bool)isKeyPressedMethod.Invoke(keyboard, new object[] { keyType.GetField("D").GetValue(null) }))
                                          {
                                                transform.Translate(Vector3.right * moveSpeed);
                                          }

                                          // Up/Down movement
                                          if ((bool)isKeyPressedMethod.Invoke(keyboard, new object[] { keyType.GetField("Q").GetValue(null) }))
                                          {
                                                transform.Translate(Vector3.up * moveSpeed);
                                          }
                                          if ((bool)isKeyPressedMethod.Invoke(keyboard, new object[] { keyType.GetField("E").GetValue(null) }))
                                          {
                                                transform.Translate(Vector3.down * moveSpeed);
                                          }

                                          // Rotation
                                          if ((bool)isKeyPressedMethod.Invoke(keyboard, new object[] { keyType.GetField("LeftArrow").GetValue(null) }))
                                          {
                                                transform.Rotate(Vector3.up, -rotationSpeed);
                                          }
                                          if ((bool)isKeyPressedMethod.Invoke(keyboard, new object[] { keyType.GetField("RightArrow").GetValue(null) }))
                                          {
                                                transform.Rotate(Vector3.up, rotationSpeed);
                                          }
                                          if ((bool)isKeyPressedMethod.Invoke(keyboard, new object[] { keyType.GetField("UpArrow").GetValue(null) }))
                                          {
                                                transform.Rotate(Vector3.right, -rotationSpeed);
                                          }
                                          if ((bool)isKeyPressedMethod.Invoke(keyboard, new object[] { keyType.GetField("DownArrow").GetValue(null) }))
                                          {
                                                transform.Rotate(Vector3.right, rotationSpeed);
                                          }
                                    }
                              }
                        }
                        catch (System.Exception e)
                        {
                              Debug.LogWarning("Error using Input System for camera simulation: " + e.Message);
                        }
                  }

                  private void TryUseLegacyInputForCamera()
                  {
                        try
                        {
                              // Check if we can access legacy Input safely
                              if (Input.GetKeyDown(KeyCode.Alpha1))
                              {
                                    // Legacy Input is available, so we can use it
                                    // Forward/Backward movement
                                    if (Input.GetKey(KeyCode.W))
                                    {
                                          transform.Translate(Vector3.forward * moveSpeed);
                                    }
                                    if (Input.GetKey(KeyCode.S))
                                    {
                                          transform.Translate(Vector3.back * moveSpeed);
                                    }

                                    // Left/Right movement
                                    if (Input.GetKey(KeyCode.A))
                                    {
                                          transform.Translate(Vector3.left * moveSpeed);
                                    }
                                    if (Input.GetKey(KeyCode.D))
                                    {
                                          transform.Translate(Vector3.right * moveSpeed);
                                    }

                                    // Up/Down movement
                                    if (Input.GetKey(KeyCode.Q))
                                    {
                                          transform.Translate(Vector3.up * moveSpeed);
                                    }
                                    if (Input.GetKey(KeyCode.E))
                                    {
                                          transform.Translate(Vector3.down * moveSpeed);
                                    }

                                    // Rotation
                                    if (Input.GetKey(KeyCode.LeftArrow))
                                    {
                                          transform.Rotate(Vector3.up, -rotationSpeed);
                                    }
                                    if (Input.GetKey(KeyCode.RightArrow))
                                    {
                                          transform.Rotate(Vector3.up, rotationSpeed);
                                    }
                                    if (Input.GetKey(KeyCode.UpArrow))
                                    {
                                          transform.Rotate(Vector3.right, -rotationSpeed);
                                    }
                                    if (Input.GetKey(KeyCode.DownArrow))
                                    {
                                          transform.Rotate(Vector3.right, rotationSpeed);
                                    }
                              }
                        }
                        catch (System.Exception)
                        {
                              // Silently ignore legacy input errors
                        }
                  }
            }

            // Utility method to check if Input System package is present and enabled
            private static bool CheckForInputSystemPackage()
            {
                  try
                  {
                        // Check if the InputSystem types exist
                        var type = System.Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem");
                        return type != null;
                  }
                  catch
                  {
                        return false;
                  }
            }

            // Try to add InputSystemUIInputModule using reflection
            private bool TryAddInputSystemModule(GameObject eventSystemObj)
            {
                  try
                  {
                        var moduleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                        if (moduleType != null)
                        {
                              eventSystemObj.AddComponent(moduleType);
                              Debug.Log("Added InputSystemUIInputModule for new Input System");
                              return true;
                        }
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogWarning("Could not add InputSystemUIInputModule: " + e.Message);
                  }
                  return false;
            }

            // Добавляем публичный метод для запуска настройки сцены из других скриптов
            public void StartSetup()
            {
                  Start();
            }

            // Использовать по контекстному меню в редакторе
            [ContextMenu("Setup Scene Automatically")]
            private void AutoSetupScene()
            {
                  StartSetup();
            }
      }
}