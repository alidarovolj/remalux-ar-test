#pragma warning disable CS0414 // Disable warnings about assigned but unused fields
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UtilsModule;
using OpenCVForUnity.Calib3dModule;
using OpenCVRect = OpenCVForUnity.CoreModule.Rect;
using UnityEngine.EventSystems; // Добавляем для доступа к EventSystem
using Remalux.WallPainting.Vision;

namespace Remalux.WallPainting.Vision
{
      public class RealWallPaintingController : MonoBehaviour
      {
            [Header("Components")]
            public Camera mainCamera;
            public WallDetector wallDetector;
            public TextureManager textureManager;

            [Header("UI")]
            public RawImage cameraPreview;
            public Button captureButton;
            public Button resetButton;
            public Button toggleModeButton;

            [Header("Painting")]
            public GameObject colorPalettePrefab;
            public Transform colorPaletteContainer;
            public List<Color> duluxColors = new List<Color>
                  {
                        new Color(0.96f, 0.96f, 0.96f), // White
                        new Color(0.95f, 0.95f, 0.70f), // Cream
                        new Color(0.85f, 0.85f, 0.85f), // Light Grey
                        new Color(0.90f, 0.80f, 0.70f), // Beige
                        new Color(0.95f, 0.75f, 0.55f), // Peach
                        new Color(0.98f, 0.83f, 0.80f), // Pink
                        new Color(0.70f, 0.90f, 0.95f), // Sky Blue
                        new Color(0.60f, 0.80f, 0.60f), // Mint Green
                        new Color(0.80f, 0.60f, 0.80f), // Lavender
                        new Color(1.00f, 0.85f, 0.40f), // Yellow
                        new Color(0.95f, 0.55f, 0.40f), // Coral
                        new Color(0.40f, 0.65f, 0.85f), // Blue
                        new Color(0.45f, 0.75f, 0.45f), // Green
                        new Color(0.70f, 0.45f, 0.45f), // Burgundy
                        new Color(0.50f, 0.50f, 0.50f), // Grey
                        new Color(0.30f, 0.30f, 0.30f), // Dark Grey
                  };
            private Color currentPaintColor;
            private GameObject colorPalettePanel;

            private bool isCapturing = false;
            private bool isPaintingMode = false; // Flag to track current mode
            private List<WallData> detectedWalls = new List<WallData>();
            private Material blueMaterial;
            private GameObject messagePanel; // Панель для сообщений
            private Coroutine messageCoroutine; // Корутин для скрытия сообщений
            private bool isFullScreenCamera = false; // Переменная для отслеживания полноэкранного режима
            private WebCamTexture webCamTexture; // Текстура веб-камеры

            // Добавляем недостающие переменные
            private List<GameObject> wallMarkers = new List<GameObject>();
            private List<GameObject> createdWallObjects = new List<GameObject>();

            private GameObject highlightedWall = null;
            private Material highlightMaterial;

            // Additional fields for painting functionality
            private GameObject brushSizeContainer; // Контейнер для кнопок размера кисти
            private Color selectedColor; // Выбранный цвет для покраски
            private Dictionary<int, Texture2D> wallPaintTextures = new Dictionary<int, Texture2D>(); // Текстуры для стен
            private Dictionary<GameObject, Material> originalMaterials = new Dictionary<GameObject, Material>(); // Оригинальные материалы
            private GameObject brushPreview; // Объект для предпросмотра размера кисти
            private System.Action<GameObject> OnWallPainted; // Событие при окрашивании стены

            // Paint parameters
            private float brushSize = 0.1f; // Размер кисти по умолчанию
            // These fields are intentionally marked as NonSerialized to suppress unused warnings
            [System.NonSerialized] private float paintOpacity = 0.8f; // Непрозрачность покраски
            [System.NonSerialized] private float maxPaintDistance = 5.0f; // Максимальное расстояние для покраски
            [System.NonSerialized] private float maxPaintAngle = 60.0f; // Максимальный угол для покраски
            [System.NonSerialized] private float paintInterval = 0.05f; // Интервал между покрасками

            // Camera stability tracking
            [System.NonSerialized] private float cameraStabilityPositionThreshold = 0.01f; // Порог стабильности позиции
            [System.NonSerialized] private float cameraStabilityRotationThreshold = 1.0f; // Порог стабильности поворота
            private Vector3 lastCameraPosition; // Последняя позиция камеры
            private Quaternion lastCameraRotation; // Последний поворот камеры

            // State control variables
            [System.NonSerialized] private bool isPaintMode = false; // Флаг режима покраски
            [System.NonSerialized] private bool isColorSelectionMode = false; // Флаг выбора цвета
            [System.NonSerialized] private bool paintingEnabled = false; // Флаг включения покраски

            // Класс для создания Billboard объектов, которые всегда поворачиваются к камере
            private class Billboard : MonoBehaviour
            {
                  private Camera mainCamera;

                  void Start()
                  {
                        mainCamera = Camera.main;
                        if (mainCamera == null)
                        {
                              mainCamera = FindFirstObjectByType<Camera>();
                        }
                  }

                  void Update()
                  {
                        if (mainCamera != null)
                        {
                              transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                                              mainCamera.transform.rotation * Vector3.up);
                        }
                  }
            }

            // Метод для отображения сообщений пользователю
            private void ShowMessage(string message, float duration = 3f)
            {
                  Debug.Log($"Showing message: {message}");

                  // Create message panel if it doesn't exist
                  if (messagePanel == null)
                  {
                        CreateMessagePanel();
                  }

                  // Update the message text
                  Text messageText = messagePanel.GetComponentInChildren<Text>();
                  if (messageText != null)
                  {
                        messageText.text = message;
                  }

                  // Show the panel
                  messagePanel.SetActive(true);

                  // Hide after delay
                  if (messageCoroutine != null)
                  {
                        StopCoroutine(messageCoroutine);
                  }

                  messageCoroutine = StartCoroutine(HideMessageAfterDelay(duration));
            }

            // Создание панели сообщений
            private void CreateMessagePanel()
            {
                  // Create a new UI panel for messages
                  messagePanel = new GameObject("MessagePanel");

                  // Find canvas or create one
                  Canvas canvas = FindFirstObjectByType<Canvas>();
                  if (canvas == null)
                  {
                        GameObject canvasObj = new GameObject("Canvas");
                        canvas = canvasObj.AddComponent<Canvas>();
                        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                        canvasObj.AddComponent<CanvasScaler>();
                        canvasObj.AddComponent<GraphicRaycaster>();
                  }

                  messagePanel.transform.SetParent(canvas.transform, false);

                  // Add panel components
                  RectTransform rectTransform = messagePanel.AddComponent<RectTransform>();
                  Image panelImage = messagePanel.AddComponent<Image>();
                  panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

                  // Configure panel layout
                  rectTransform.anchorMin = new Vector2(0.3f, 0.05f);
                  rectTransform.anchorMax = new Vector2(0.7f, 0.15f);
                  rectTransform.offsetMin = Vector2.zero;
                  rectTransform.offsetMax = Vector2.zero;

                  // Add text to the panel
                  GameObject textObj = new GameObject("MessageText");
                  textObj.transform.SetParent(messagePanel.transform, false);

                  Text text = textObj.AddComponent<Text>();
                  text.text = "";
                  text.fontSize = 24;
                  text.alignment = TextAnchor.MiddleCenter;
                  text.color = Color.white;

                  // Find a font
                  Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                  if (font == null)
                  {
                        Font[] fonts = Object.FindObjectsByType<Font>(FindObjectsSortMode.None);
                        if (fonts.Length > 0)
                        {
                              font = fonts[0];
                        }
                  }
                  text.font = font;

                  // Configure text layout
                  RectTransform textRect = text.rectTransform;
                  textRect.anchorMin = new Vector2(0.05f, 0.05f);
                  textRect.anchorMax = new Vector2(0.95f, 0.95f);
                  textRect.offsetMin = Vector2.zero;
                  textRect.offsetMax = Vector2.zero;

                  // Initially hide the panel
                  messagePanel.SetActive(false);
            }

            // Корутина для скрытия сообщения
            private IEnumerator HideMessageAfterDelay(float delay)
            {
                  yield return new WaitForSeconds(delay);

                  if (messagePanel != null)
                  {
                        messagePanel.SetActive(false);
                  }
            }

            // Метод для показа большого сообщения на весь экран
            private void ShowBigScreenMessage(string message, float duration = 5.0f)
            {
                  // Create a new GameObject for the full-screen message
                  GameObject msgObj = new GameObject("BigScreenMessage");

                  // Find canvas or create one
                  Canvas canvas = FindFirstObjectByType<Canvas>();
                  Debug.Log("Creating EventSystem");
                  GameObject eventSystem = new GameObject("EventSystem");
                  eventSystem.AddComponent<EventSystem>();
                  eventSystem.AddComponent<StandaloneInputModule>();

                  // Set the message object to be destroyed after the specified duration
                  Destroy(msgObj, duration);
            }

            // Метод инициализации, вызывается при старте
            private void Start()
            {
                  Debug.Log("RealWallPaintingController starting...");

                  // Initialize the paint color
                  currentPaintColor = duluxColors.Count > 0 ? duluxColors[0] : Color.white;

                  // Create highlight material
                  CreateHighlightMaterial();

                  // Create the color palette UI
                  CreateColorPalette();

                  // Проверяем все необходимые компоненты
                  ValidateComponents();

                  // Set up user interface buttons
                  SetupUI();

                  // Инициализируем камеру
                  InitializeCamera();

                  // Устанавливаем режим камеры на полный экран изначально
                  isFullScreenCamera = true;
                  SetCameraViewMode(true);

                  // Удаляем все рамки вокруг изображения камеры
                  RemoveCameraPreviewBorders();

                  // Настраиваем WallDetector для лучшего обнаружения стен
                  if (wallDetector != null)
                  {
                        Debug.Log("Настраиваю WallDetector для лучшего обнаружения стен");
                        wallDetector.mainCamera = mainCamera;

                        // Подписываемся на событие обнаружения стен
                        wallDetector.OnWallsDetected += OnWallsDetected;

                        // Подписываемся на событие выбора стены
                        wallDetector.OnWallSelected += OnWallSelected;

                        // Начинаем обнаружение стен
                        wallDetector.StartDetection();
                  }
                  else
                  {
                        Debug.LogError("WallDetector не назначен! Обнаружение стен не будет работать.");
                  }

                  Debug.Log("RealWallPaintingController started successfully");
            }

            private void InitializeCamera()
            {
                  Debug.Log("Initializing camera system...");

                  // Create camera preview if needed
                  if (cameraPreview == null)
                  {
                        Debug.LogWarning("Camera preview RawImage not assigned, creating a new one");
                        TryCreateCameraPreview();
                  }

                  // Early exit if still null
                  if (cameraPreview == null)
                  {
                        Debug.LogError("Failed to create camera preview. Camera initialization failed.");
                        return;
                  }

                  try
                  {
                        // Check if webcam is available
                        WebCamDevice[] devices = WebCamTexture.devices;
                        if (devices.Length == 0)
                        {
                              Debug.LogWarning("No webcam found. Using placeholder texture.");
                              CreatePlaceholderTexture();
                              return;
                        }

                        // Create and start webcam texture
                        int desiredWidth = 1280;
                        int desiredHeight = 720;
                        int desiredFPS = 30;

                        string deviceName = string.Empty;
                        // Try to find back camera on mobile, otherwise use first camera
                        for (int i = 0; i < devices.Length; i++)
                        {
                              if (!devices[i].isFrontFacing)
                              {
                                    deviceName = devices[i].name;
                                    break;
                              }
                        }

                        if (string.IsNullOrEmpty(deviceName) && devices.Length > 0)
                        {
                              deviceName = devices[0].name;
                        }

                        try
                        {
                              // If already exists, stop and destroy it
                              if (webCamTexture != null)
                              {
                                    webCamTexture.Stop();
                                    Destroy(webCamTexture);
                              }

                              // Create new webcam texture
                              webCamTexture = new WebCamTexture(deviceName, desiredWidth, desiredHeight, desiredFPS);

                              // Start webcam
                              webCamTexture.Play();

                              // Assign to camera preview
                              cameraPreview.texture = webCamTexture;

                              Debug.Log($"Webcam started: {webCamTexture.width}x{webCamTexture.height}");

                              // Make camera preview visible
                              cameraPreview.enabled = true;

                              // Set the camera view mode
                              isFullScreenCamera = false; // Start with small preview in corner
                              SetCameraViewMode(isFullScreenCamera);

                              // Pass webcam texture to wall detector
                              if (wallDetector != null)
                              {
                                    // Connect components
                                    wallDetector.mainCamera = mainCamera;
                                    wallDetector.SetDebugImageDisplay(cameraPreview);

                                    // Start detection
                                    wallDetector.StartDetection();
                              }
                        }
                        catch (System.Exception e)
                        {
                              Debug.LogError($"Error starting webcam: {e.Message}");
                              CreatePlaceholderTexture();
                        }
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Error initializing camera: {e.Message}");
                        CreatePlaceholderTexture();
                  }
            }

            private void CreatePlaceholderTexture()
            {
                  // Create a placeholder texture
                  Texture2D placeholderTexture = new Texture2D(256, 256);

                  // Create checkerboard pattern
                  Color32[] colors = new Color32[256 * 256];
                  for (int y = 0; y < 256; y++)
                  {
                        for (int x = 0; x < 256; x++)
                        {
                              bool isEvenX = (x / 32) % 2 == 0;
                              bool isEvenY = (y / 32) % 2 == 0;

                              if (isEvenX == isEvenY)
                              {
                                    colors[y * 256 + x] = new Color32(60, 60, 60, 255);
                              }
                              else
                              {
                                    colors[y * 256 + x] = new Color32(120, 120, 120, 255);
                              }
                        }
                  }

                  // Set and apply texture
                  placeholderTexture.SetPixels32(colors);
                  placeholderTexture.Apply();

                  // Set to camera preview
                  if (cameraPreview != null)
                  {
                        cameraPreview.texture = placeholderTexture;
                        cameraPreview.enabled = true;
                  }

                  Debug.Log("Created placeholder texture for camera preview");
            }

            // Метод для создания тестовых стен при запуске, если не обнаруживаются реальные
            private void CreateTestWallsIfNeeded()
            {
                  // Большое, заметное сообщение на весь экран
                  ShowBigScreenMessage("ВНИМАНИЕ! СОЗДАНЫ ТЕСТОВЫЕ СТЕНЫ\n\nНажмите ПРОБЕЛ для покраски стен\nНажмите T для пересоздания стен");

                  Debug.Log("ВНИМАНИЕ: Создаются тестовые стены для демонстрации");

                  // Явно очищаем старые маркеры стен перед созданием новых
                  ClearWallMarkers();

                  // Поворачиваем камеру в нужное положение для тестовых стен
                  RotateCameraToFaceTestWalls();

                  // Создаем либо тестовую комнату, либо отдельные стены
                  bool createRoom = true; // Флаг для выбора режима создания

                  if (createRoom)
                  {
                        CreateTestRoom();
                  }
                  else
                  {
                        CreateIndividualTestWalls();
                  }

                  // Показываем сообщение пользователю
                  ShowMessage("Созданы тестовые стены для демонстрации", 5.0f);
            }

            // Метод для поворота камеры в нужное положение для тестовых стен
            private void RotateCameraToFaceTestWalls()
            {
                  if (mainCamera == null) return;

                  // Устанавливаем камеру в позицию для просмотра тестовых стен
                  Vector3 cameraPosition = new Vector3(0, 1.7f, -3.0f);
                  Quaternion cameraRotation = Quaternion.Euler(0, 0, 0);

                  mainCamera.transform.position = cameraPosition;
                  mainCamera.transform.rotation = cameraRotation;

                  Debug.Log("Камера повернута для просмотра тестовых стен");
            }

            // Метод для создания индивидуальных тестовых стен (старый вариант)
            private void CreateIndividualTestWalls()
            {
                  List<WallData> testWalls = new List<WallData>();

                  // Создаем большую стену прямо перед камерой очень близко
                  testWalls.Add(new WallData(
                        mainCamera.transform.position + mainCamera.transform.forward * 3.0f,
                        Quaternion.LookRotation(-mainCamera.transform.forward),
                        new Vector3(3.0f, 2.0f, 0.1f),
                        1
                  ));

                  // Создаем стену справа от камеры, очень близко
                  testWalls.Add(new WallData(
                        mainCamera.transform.position + mainCamera.transform.right * 2.0f + mainCamera.transform.forward * 2.0f,
                        Quaternion.LookRotation(-mainCamera.transform.right),
                        new Vector3(2.0f, 1.5f, 0.1f),
                        2
                  ));

                  // Создаем стену слева от камеры, очень близко
                  testWalls.Add(new WallData(
                        mainCamera.transform.position - mainCamera.transform.right * 2.0f + mainCamera.transform.forward * 2.0f,
                        Quaternion.LookRotation(mainCamera.transform.right),
                        new Vector3(2.0f, 1.5f, 0.1f),
                        3
                  ));

                  // Создаем стену над камерой, очень близко
                  testWalls.Add(new WallData(
                        mainCamera.transform.position + Vector3.up * 2.0f + mainCamera.transform.forward * 2.0f,
                        Quaternion.LookRotation(Vector3.down),
                        new Vector3(2.0f, 2.0f, 0.1f),
                        4
                  ));

                  // Создаем стену под камерой, очень близко
                  testWalls.Add(new WallData(
                        mainCamera.transform.position + Vector3.down * 0.5f + mainCamera.transform.forward * 2.0f,
                        Quaternion.LookRotation(Vector3.up),
                        new Vector3(2.0f, 2.0f, 0.1f),
                        5
                  ));

                  // Обрабатываем тестовые стены
                  OnWallsDetected(testWalls);

                  Debug.Log($"Созданы {testWalls.Count} отдельных тестовых стен относительно камеры в позиции {mainCamera.transform.position}");
            }

            // Метод для создания тестовой комнаты вокруг камеры
            private void CreateTestRoom()
            {
                  List<WallData> roomWalls = new List<WallData>();

                  // Задаем размеры комнаты
                  float roomWidth = 5.0f;  // По оси X
                  float roomLength = 5.0f; // По оси Z
                  float roomHeight = 3.0f; // По оси Y
                  float wallThickness = 0.1f;

                  // Определяем центр комнаты относительно камеры
                  Vector3 roomCenter = mainCamera.transform.position;
                  roomCenter.y = mainCamera.transform.position.y - 0.5f; // Слегка опускаем центр комнаты ниже камеры

                  // Определяем позиции стен относительно центра комнаты
                  float halfWidth = roomWidth / 2;
                  float halfLength = roomLength / 2;
                  float halfHeight = roomHeight / 2;

                  // Создаем переднюю стену (впереди от камеры)
                  roomWalls.Add(new WallData(
                        roomCenter + new Vector3(0, 0, halfLength),
                        Quaternion.LookRotation(Vector3.back), // Смотрит на камеру
                        new Vector3(roomWidth, roomHeight, wallThickness),
                        6
                  ));

                  // Создаем заднюю стену (позади камеры)
                  roomWalls.Add(new WallData(
                        roomCenter + new Vector3(0, 0, -halfLength),
                        Quaternion.LookRotation(Vector3.forward),
                        new Vector3(roomWidth, roomHeight, wallThickness),
                        7
                  ));

                  // Создаем правую стену
                  roomWalls.Add(new WallData(
                        roomCenter + new Vector3(halfWidth, 0, 0),
                        Quaternion.LookRotation(Vector3.left),
                        new Vector3(roomLength, roomHeight, wallThickness),
                        8
                  ));

                  // Создаем левую стену
                  roomWalls.Add(new WallData(
                        roomCenter + new Vector3(-halfWidth, 0, 0),
                        Quaternion.LookRotation(Vector3.right),
                        new Vector3(roomLength, roomHeight, wallThickness),
                        9
                  ));

                  // Создаем пол
                  roomWalls.Add(new WallData(
                        roomCenter + new Vector3(0, -halfHeight, 0),
                        Quaternion.LookRotation(Vector3.up),
                        new Vector3(roomWidth, roomLength, wallThickness),
                        10
                  ));

                  // Создаем потолок
                  roomWalls.Add(new WallData(
                        roomCenter + new Vector3(0, halfHeight, 0),
                        Quaternion.LookRotation(Vector3.down),
                        new Vector3(roomWidth, roomLength, wallThickness),
                        11
                  ));

                  // Добавляем мебель или объекты для демонстрации (например, картину на стене)
                  roomWalls.Add(new WallData(
                        roomCenter + new Vector3(0, 0, halfLength - 0.05f) + new Vector3(1.0f, 0.3f, 0),
                        Quaternion.LookRotation(Vector3.back),
                        new Vector3(1.5f, 1.0f, 0.05f),
                        12
                  ));

                  // Обрабатываем стены комнаты
                  OnWallsDetected(roomWalls);

                  Debug.Log($"Создана тестовая комната с {roomWalls.Count} стенами вокруг камеры в позиции {mainCamera.transform.position}");
            }

            private void ValidateComponents()
            {
                  bool hasErrors = false;

                  if (mainCamera == null)
                  {
                        mainCamera = Camera.main;
                        if (mainCamera == null)
                        {
                              Debug.LogWarning("RealWallPaintingController: Main camera not found. Some features may not work properly.");
                              hasErrors = true;
                        }
                  }

                  if (cameraPreview == null)
                  {
                        Debug.LogWarning("RealWallPaintingController: Camera preview RawImage is not assigned! Creating one...");
                        TryCreateCameraPreview();
                  }

                  if (wallDetector == null)
                  {
                        Debug.LogWarning("Wall detection will not work without WallDetector component.");
                        // Try to find or create wall detector
                        wallDetector = GetComponent<WallDetector>();
                        if (wallDetector == null)
                        {
                              wallDetector = gameObject.AddComponent<WallDetector>();
                              Debug.Log("Created a new WallDetector component.");
                        }
                  }

                  if (textureManager == null)
                  {
                        Debug.LogWarning("Texture management will not work without TextureManager component.");

                        // Try to find texture manager
                        textureManager = FindFirstObjectByType<TextureManager>();
                        if (textureManager == null)
                        {
                              // Create a new texture manager
                              GameObject textureManagerObj = new GameObject("TextureManager");
                              textureManagerObj.transform.SetParent(transform);
                              textureManager = textureManagerObj.AddComponent<TextureManager>();
                              Debug.Log("Created a new TextureManager component.");

                              // Initialize texture manager with default materials
                              InitializeTextureManager();
                        }
                  }

                  if (hasErrors)
                  {
                        Debug.LogWarning("RealWallPaintingController: Some required components are missing. Some features may not work properly.");
                  }
            }

            private void InitializeTextureManager()
            {
                  if (textureManager == null) return;

                  try
                  {
                        // Create default material
                        Material defaultMaterial = new Material(Shader.Find("Standard"));
                        defaultMaterial.color = Color.white;

                        // Create some additional materials with different colors
                        Material[] materials = new Material[5];
                        materials[0] = defaultMaterial;

                        for (int i = 1; i < materials.Length; i++)
                        {
                              materials[i] = new Material(Shader.Find("Standard"));
                        }

                        // Set different colors
                        materials[1].color = new Color(0.9f, 0.6f, 0.3f);  // Beige
                        materials[2].color = new Color(0.8f, 0.8f, 0.8f);  // Light gray
                        materials[3].color = new Color(0.3f, 0.6f, 0.9f);  // Light blue
                        materials[4].color = new Color(0.8f, 0.9f, 0.8f);  // Light green

                        // Try to set materials through reflection
                        var presetsField = textureManager.GetType().GetField("texturePresets",
                                                                          System.Reflection.BindingFlags.Instance |
                                                                          System.Reflection.BindingFlags.Public |
                                                                          System.Reflection.BindingFlags.NonPublic);

                        if (presetsField != null)
                        {
                              // Create appropriate type of presets collection
                              var presetType = System.Type.GetType("Remalux.WallPainting.TextureManager+TexturePreset, Assembly-CSharp");
                              if (presetType == null)
                              {
                                    presetType = System.Type.GetType("Remalux.WallPainting.TexturePreset, Assembly-CSharp");
                              }

                              if (presetType != null)
                              {
                                    // Create a list to hold presets
                                    var listType = typeof(List<>).MakeGenericType(presetType);
                                    var presetsList = System.Activator.CreateInstance(listType);
                                    var addMethod = listType.GetMethod("Add");

                                    // Add materials to presets
                                    for (int i = 0; i < materials.Length; i++)
                                    {
                                          var preset = System.Activator.CreateInstance(presetType);

                                          // Set properties
                                          var nameField = presetType.GetField("name");
                                          var materialField = presetType.GetField("material");
                                          var colorField = presetType.GetField("tintColor");

                                          if (nameField != null) nameField.SetValue(preset, $"Color {i + 1}");
                                          if (materialField != null) materialField.SetValue(preset, materials[i]);
                                          if (colorField != null) colorField.SetValue(preset, materials[i].color);

                                          // Add to list
                                          addMethod.Invoke(presetsList, new[] { preset });
                                    }

                                    // Set presets in texture manager
                                    presetsField.SetValue(textureManager, presetsList);
                                    Debug.Log("TextureManager initialized with default materials");
                              }
                        }
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Failed to initialize TextureManager: {e.Message}");
                  }
            }

            private void TryCreateCameraPreview()
            {
                  try
                  {
                        // Try to find a canvas
                        Canvas canvas = FindFirstObjectByType<Canvas>();
                        if (canvas == null)
                        {
                              // Create a new canvas
                              GameObject canvasObj = new GameObject("Camera Preview Canvas");
                              canvas = canvasObj.AddComponent<Canvas>();
                              canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                              canvasObj.AddComponent<CanvasScaler>();
                              canvasObj.AddComponent<GraphicRaycaster>();
                        }

                        // Create a RawImage for camera preview
                        GameObject previewObj = new GameObject("Camera Preview");
                        previewObj.transform.SetParent(canvas.transform, false);

                        // Set up RawImage and RectTransform
                        cameraPreview = previewObj.AddComponent<RawImage>();
                        RectTransform rectTransform = cameraPreview.rectTransform;

                        // Position in corner
                        rectTransform.anchorMin = new Vector2(0.7f, 0.05f);
                        rectTransform.anchorMax = new Vector2(0.95f, 0.25f);
                        rectTransform.offsetMin = Vector2.zero;
                        rectTransform.offsetMax = Vector2.zero;

                        Debug.Log("Camera preview RawImage not assigned, creating a new one.");
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError("Failed to create camera preview: " + e.Message);
                  }
            }

            private void SetupUI()
            {
                  if (captureButton != null)
                  {
                        captureButton.onClick.AddListener(OnCaptureButtonClicked);
                  }

                  if (resetButton != null)
                  {
                        resetButton.onClick.AddListener(OnResetButtonClicked);
                  }

                  if (toggleModeButton != null)
                  {
                        toggleModeButton.onClick.AddListener(OnToggleModeButtonClicked);
                  }
                  else
                  {
                        // Create toggle mode button if not assigned
                        CreateToggleModeButton();
                  }

                  if (wallDetector != null && cameraPreview != null)
                  {
                        wallDetector.SetDebugImageDisplay(cameraPreview);
                  }

                  // Hide color palette initially since we start in detection mode
                  if (colorPalettePanel != null)
                  {
                        colorPalettePanel.SetActive(false);
                  }
            }

            private void CreateToggleModeButton()
            {
                  // Check if we have a canvas to add the button to
                  Canvas canvas = FindFirstObjectByType<Canvas>();
                  if (canvas == null)
                  {
                        Debug.LogWarning("No canvas found to create toggle mode button");
                        return;
                  }

                  // Create button GameObject
                  GameObject buttonObj = new GameObject("ToggleModeButton");
                  buttonObj.transform.SetParent(canvas.transform, false);

                  // Add button components
                  Image buttonImage = buttonObj.AddComponent<Image>();
                  buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                  toggleModeButton = buttonObj.AddComponent<Button>();

                  // Add text
                  GameObject textObj = new GameObject("Text");
                  textObj.transform.SetParent(buttonObj.transform, false);
                  Text buttonText = textObj.AddComponent<Text>();

                  // Try to find a font
                  Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                  if (font == null)
                  {
                        Font[] fonts = Object.FindObjectsByType<Font>(FindObjectsSortMode.None);
                        if (fonts.Length > 0)
                        {
                              font = fonts[0];
                        }
                  }

                  buttonText.font = font;
                  buttonText.text = "MODE";
                  buttonText.fontSize = 18;
                  buttonText.alignment = TextAnchor.MiddleCenter;
                  buttonText.color = Color.white;

                  // Configure button layout
                  RectTransform textRect = textObj.GetComponent<RectTransform>();
                  textRect.anchorMin = Vector2.zero;
                  textRect.anchorMax = Vector2.one;
                  textRect.offsetMin = Vector2.zero;
                  textRect.offsetMax = Vector2.zero;

                  // Position the button in top-right corner
                  RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
                  buttonRect.anchorMin = new Vector2(1, 1);
                  buttonRect.anchorMax = new Vector2(1, 1);
                  buttonRect.pivot = new Vector2(1, 1);
                  buttonRect.sizeDelta = new Vector2(80, 40);
                  buttonRect.anchoredPosition = new Vector2(-10, -10);

                  // Add listener
                  toggleModeButton.onClick.AddListener(OnToggleModeButtonClicked);
            }

            private void OnToggleModeButtonClicked()
            {
                  // Toggle between detection and painting modes
                  isPaintingMode = !isPaintingMode;

                  // Update UI elements based on mode
                  UpdateModeUI();

                  // Show feedback message
                  if (isPaintingMode)
                  {
                        ShowMessage("Режим покраски активирован", 2f);
                  }
                  else
                  {
                        ShowMessage("Режим обнаружения стен активирован", 2f);
                  }
            }

            private void UpdateModeUI()
            {
                  // Update button text if available
                  if (toggleModeButton != null)
                  {
                        Text buttonText = toggleModeButton.GetComponentInChildren<Text>();
                        if (buttonText != null)
                        {
                              buttonText.text = isPaintingMode ? "DETECT" : "PAINT";
                        }
                  }

                  // Show/hide color palette based on mode
                  if (colorPalettePanel != null)
                  {
                        colorPalettePanel.SetActive(isPaintingMode);
                  }

                  // Enable/disable wall detector based on mode
                  if (wallDetector != null)
                  {
                        if (isPaintingMode)
                        {
                              wallDetector.StopDetection();
                        }
                        else
                        {
                              wallDetector.StartDetection();
                        }
                  }

                  // Show/hide wall markers based on mode
                  UpdateWallMarkersVisibility(!isPaintingMode);

                  // Update capture button text/icon if available
                  if (captureButton != null)
                  {
                        Text captureText = captureButton.GetComponentInChildren<Text>();
                        if (captureText != null)
                        {
                              captureText.text = isPaintingMode ? "Apply" : "Capture";
                        }
                  }
            }

            private void OnCaptureButtonClicked()
            {
                  if (isPaintingMode)
                  {
                        // In painting mode, apply colors to all walls
                        PaintAllWalls();
                  }
                  else
                  {
                        // In detection mode, toggle wall detection
                        if (!isCapturing)
                        {
                              StartCapture();
                        }
                        else
                        {
                              StopCapture();
                        }
                  }
            }

            private void OnResetButtonClicked()
            {
                  detectedWalls.Clear();
                  if (wallDetector != null)
                  {
                        wallDetector.StopDetection();
                  }
                  isCapturing = false;
                  UpdateUI();
            }

            private void StartCapture()
            {
                  Debug.Log("Запуск обнаружения стен через OpenCV...");
                  // Получаем компонент WallDetector и настраиваем его
                  wallDetector = GetComponent<WallDetector>();
                  if (wallDetector == null)
                  {
                        wallDetector = gameObject.AddComponent<WallDetector>();
                  }

                  // Настраиваем параметры детектора для более точного обнаружения стен
                  ConfigureAdvancedWallDetection();

                  // Включаем отображение контуров на видеопотоке
                  EnableContoursOnCamera();

                  // Убедимся что RawImage для предпросмотра камеры виден и настроен
                  if (cameraPreview != null)
                  {
                        // Делаем компонент активным и видимым
                        cameraPreview.gameObject.SetActive(true);
                        cameraPreview.color = Color.white; // Полная непрозрачность

                        // Настраиваем превью камеры как маленькое окно в нижнем правом углу
                        RectTransform rectTransform = cameraPreview.GetComponent<RectTransform>();
                        if (rectTransform != null)
                        {
                              // Устанавливаем якорь в правый нижний угол
                              rectTransform.anchorMin = new Vector2(0.7f, 0.05f);
                              rectTransform.anchorMax = new Vector2(0.95f, 0.25f);
                              rectTransform.offsetMin = Vector2.zero;
                              rectTransform.offsetMax = Vector2.zero;

                              // Устанавливаем правильное соотношение сторон и выравнивание
                              rectTransform.pivot = new Vector2(0.5f, 0.5f);
                              // RawImage не поддерживает preserveAspect, поэтому настраиваем UVRect
                              cameraPreview.uvRect = new UnityEngine.Rect(0, 0, 1, 1);
                        }

                        // Создаем отдельный объект для границы вместо добавления Image компонента к самому cameraPreview
                        Transform parentTransform = cameraPreview.transform.parent;

                        // Проверяем, существует ли уже граница
                        GameObject borderObj = GameObject.Find("CameraPreviewBorder");
                        if (borderObj == null)
                        {
                              // Создаем новый объект для границы
                              borderObj = new GameObject("CameraPreviewBorder");
                              borderObj.transform.SetParent(parentTransform);

                              // Копируем RectTransform настройки от cameraPreview
                              RectTransform borderRect = borderObj.AddComponent<RectTransform>();
                              RectTransform previewRect = cameraPreview.GetComponent<RectTransform>();

                              if (previewRect != null)
                              {
                                    borderRect.anchorMin = previewRect.anchorMin;
                                    borderRect.anchorMax = previewRect.anchorMax;
                                    borderRect.offsetMin = new Vector2(previewRect.offsetMin.x - 5, previewRect.offsetMin.y - 5);
                                    borderRect.offsetMax = new Vector2(previewRect.offsetMax.x + 5, previewRect.offsetMax.y + 5);
                                    borderRect.pivot = previewRect.pivot;
                              }

                              // Добавляем Image компонент для рамки
                              Image borderImage = borderObj.AddComponent<Image>();
                              borderImage.color = new Color(1f, 1f, 1f, 0.3f); // Полупрозрачная белая рамка

                              // Размещаем объект границы за превью камеры
                              borderObj.transform.SetSiblingIndex(cameraPreview.transform.GetSiblingIndex());
                        }
                  }

                  // Настраиваем отображение отладочной информации в WallDetector
                  var showDebugLinesField = wallDetector.GetType().GetField("showDebugLines", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                  if (showDebugLinesField != null)
                  {
                        showDebugLinesField.SetValue(wallDetector, true); // Всегда включаем отображение контуров
                  }

                  // Подписываемся на событие обнаружения стен
                  wallDetector.OnWallsDetected += OnWallsDetected;

                  // Запускаем обнаружение реальных стен через камеру
                  wallDetector.StartDetection();

                  // Показываем подсказку пользователю о необходимости направить камеру на стены
                  ShowMessage("Камера показана в нижнем правом углу. Нажмите ПРОБЕЛ для покраски стен.", 5.0f);
            }

            private void StopCapture()
            {
                  isCapturing = false;
                  if (wallDetector != null)
                  {
                        wallDetector.StopDetection();
                  }
                  UpdateUI();
            }

            private void OnWallsDetected(List<WallData> walls)
            {
                  if (walls == null || walls.Count == 0)
                  {
                        Debug.Log("Стены не обнаружены");
                        return;
                  }

                  ClearWallMarkers();
                  detectedWalls = new List<WallData>(walls);

                  Transform markersParent = GameObject.Find("WallMarkersContainer")?.transform;
                  if (markersParent == null)
                  {
                        GameObject parentObj = new GameObject("WallMarkersContainer");
                        markersParent = parentObj.transform;
                  }

                  // Присваиваем ID каждой стене, если он не был установлен
                  for (int i = 0; i < detectedWalls.Count; i++)
                  {
                        var wall = detectedWalls[i];
                        if (string.IsNullOrEmpty(wall.id))
                        {
                              wall.id = (i + 1).ToString(); // ID начинается с 1
                              detectedWalls[i] = wall;
                        }
                  }

                  List<GameObject> createdMarkers = new List<GameObject>();
                  foreach (var wall in detectedWalls)
                  {
                        GameObject marker = CreateWallMarker(wall, markersParent);
                        if (marker != null)
                        {
                              createdMarkers.Add(marker);
                        }
                  }

                  Debug.Log($"Обнаружено стен: {walls.Count}");
                  ShowMessage($"Обнаружено {walls.Count} стен", 2f);

                  // Автоматически красим все найденные стены
                  if (createdMarkers.Count > 0)
                  {
                        // Используем немного отложенный вызов, чтобы все маркеры успели инициализироваться
                        StartCoroutine(PaintDetectedWalls(createdMarkers));
                  }

                  UpdateUI();
            }

            // Корутина для автоматической покраски стен после их обнаружения
            private IEnumerator PaintDetectedWalls(List<GameObject> walls)
            {
                  // Небольшая задержка для завершения всех инициализаций
                  yield return new WaitForSeconds(0.2f);

                  // Выбираем цвет для покраски - яркий голубой
                  Color paintColor = new Color(0.0f, 0.7f, 1.0f, 1.0f);

                  Debug.Log($"Автоматическая покраска {walls.Count} стен полным закрашиванием");

                  // Счетчик окрашенных стен
                  int paintedCount = 0;

                  // Красим каждую стену
                  foreach (GameObject wall in walls)
                  {
                        if (wall != null)
                        {
                              Renderer renderer = wall.GetComponent<Renderer>();
                              if (renderer != null)
                              {
                                    // Создаем материал и сразу применяем его
                                    Material paintMaterial = new Material(Shader.Find("Standard"));
                                    paintMaterial.color = paintColor;
                                    paintMaterial.SetFloat("_Glossiness", 0.1f);
                                    paintMaterial.SetFloat("_Metallic", 0.0f);
                                    paintMaterial.EnableKeyword("_EMISSION");
                                    paintMaterial.SetColor("_EmissionColor", paintColor * 0.3f);
                                    renderer.material = paintMaterial;

                                    paintedCount++;

                                    // Запускаем корутину с эффектами на стене
                                    StartCoroutine(CreateMultiplePaintEffects(wall));

                                    Debug.Log($"Стена {wall.name} полностью закрашена");
                              }
                        }

                        // Задержка между стенами для визуализации процесса
                        yield return new WaitForSeconds(0.1f);
                  }

                  // Уведомляем пользователя
                  if (paintedCount > 0)
                  {
                        ShowMessage($"Полностью закрашено {paintedCount} стен", 2.0f);
                  }
                  else
                  {
                        ShowMessage("Не удалось закрасить стены", 2.0f);
                  }
            }

            // Корутина для создания множественных эффектов покраски с защитой от ошибок
            private IEnumerator CreateMultiplePaintEffects(GameObject wall)
            {
                  if (wall == null)
                  {
                        Debug.LogWarning("CreateMultiplePaintEffects: Wall is null");
                        yield break;
                  }

                  // Получаем все необходимые данные в начале корутины
                  Renderer renderer = null;
                  Bounds bounds = default;
                  Vector3 center = Vector3.zero;
                  Vector3 forward = Vector3.forward;
                  float width = 1.0f;
                  float height = 1.0f;
                  Color materialColor = Color.blue;

                  try
                  {
                        // Проверяем, активен ли объект
                        if (wall == null || !wall.activeInHierarchy)
                        {
                              Debug.LogWarning("CreateMultiplePaintEffects: Wall object is null or inactive");
                              yield break;
                        }

                        renderer = wall.GetComponent<Renderer>();
                        if (renderer == null)
                        {
                              Debug.LogWarning("CreateMultiplePaintEffects: Renderer component not found");
                              yield break;
                        }

                        // Сохраняем все необходимые данные в локальных переменных
                        bounds = renderer.bounds;
                        center = bounds.center;
                        forward = wall.transform.forward;
                        width = bounds.size.x;
                        height = bounds.size.y;
                        materialColor = renderer.material.color;
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogWarning($"CreateMultiplePaintEffects: Failed to get wall data: {e.Message}");
                        yield break;
                  }

                  // Создаем сетку эффектов покраски
                  int columns = 5;
                  int rows = 5;

                  // Теперь используем только локальные переменные, без обращения к wall
                  for (int x = 0; x < columns; x++)
                  {
                        for (int y = 0; y < rows; y++)
                        {
                              // Вычисляем позицию для эффекта используя ранее полученные данные
                              float xPos = center.x - width / 2 + width * (x + 0.5f) / columns;
                              float yPos = center.y - height / 2 + height * (y + 0.5f) / rows;
                              Vector3 effectPosition = new Vector3(xPos, yPos, center.z);

                              try
                              {
                                    // Используем сохраненные данные вместо обращения к стене
                                    CreatePaintEffectAtHitPoint(effectPosition, forward, materialColor);
                              }
                              catch (System.Exception e)
                              {
                                    Debug.LogWarning($"CreateMultiplePaintEffects: Error creating effect at position {effectPosition}: {e.Message}");
                              }

                              // Небольшая задержка между созданием эффектов
                              yield return new WaitForSeconds(0.01f);
                        }
                  }

                  yield return null;
            }

            // Метод для создания визуального эффекта в точке контакта с поверхностью
            private void CreatePaintEffectAtHitPoint(Vector3 position, Vector3 normal, Color color)
            {
                  // Создаем сферу как индикатор точки покраски
                  GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                  sphere.name = "PaintEffect";
                  sphere.transform.position = position;
                  sphere.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);

                  // Устанавливаем материал
                  Renderer renderer = sphere.GetComponent<Renderer>();
                  if (renderer != null)
                  {
                        Material material = new Material(Shader.Find("Standard"));
                        material.color = color;
                        material.SetFloat("_Glossiness", 0.8f); // Высокий уровень глянца
                        renderer.material = material;
                  }

                  // Отключаем коллайдер
                  Collider collider = sphere.GetComponent<Collider>();
                  if (collider != null)
                  {
                        collider.enabled = false;
                  }

                  // Создаем эффект капли
                  StartCoroutine(PaintDropEffect(sphere));

                  // Удаляем через 2 секунды
                  Destroy(sphere, 2.0f);
            }

            // Корутина для эффекта капли краски
            private IEnumerator PaintDropEffect(GameObject paintDrop)
            {
                  float duration = 1.0f;
                  float elapsed = 0f;
                  Vector3 originalScale = paintDrop.transform.localScale;
                  Vector3 targetScale = originalScale * 2.5f;

                  while (elapsed < duration)
                  {
                        float t = elapsed / duration;

                        // Растягиваем каплю вниз
                        float scaleY = Mathf.Lerp(originalScale.y, targetScale.y, t);
                        paintDrop.transform.localScale = new Vector3(originalScale.x, scaleY, originalScale.z);

                        // Смещаем вниз
                        paintDrop.transform.position += Vector3.down * Time.deltaTime * 0.05f;

                        elapsed += Time.deltaTime;
                        yield return null;
                  }

                  // Возвращаем null для завершения корутины
                  yield return null;
            }

            // Метод для установки режима отображения камеры
            public void SetCameraViewMode(bool fullscreen)
            {
                  if (cameraPreview == null)
                  {
                        Debug.LogError("Cannot set camera mode - cameraPreview is null");
                        return;
                  }

                  // Сохраняем режим отображения
                  isFullScreenCamera = fullscreen;

                  // Получаем родительский объект для настройки размещения
                  Transform parent = cameraPreview.transform.parent;
                  RectTransform parentRect = null;
                  if (parent != null)
                  {
                        parentRect = parent.GetComponent<RectTransform>();
                  }

                  // Настраиваем размер и позицию в зависимости от режима
                  RectTransform rectTransform = cameraPreview.rectTransform;

                  if (fullscreen)
                  {
                        // Полноэкранный режим - занимает весь экран без рамок

                        // Если у превью есть родитель, настраиваем его тоже на весь экран
                        if (parentRect != null)
                        {
                              parentRect.anchorMin = Vector2.zero;
                              parentRect.anchorMax = Vector2.one;
                              parentRect.offsetMin = Vector2.zero;
                              parentRect.offsetMax = Vector2.zero;
                              parentRect.sizeDelta = Vector2.zero;
                        }

                        // Настраиваем само превью
                        rectTransform.anchorMin = Vector2.zero;
                        rectTransform.anchorMax = Vector2.one;
                        rectTransform.offsetMin = Vector2.zero;
                        rectTransform.offsetMax = Vector2.zero;
                        rectTransform.sizeDelta = Vector2.zero;

                        // Удаляем любые границы/рамки, если они есть
                        Image borderImage = cameraPreview.GetComponent<Image>();
                        if (borderImage != null)
                        {
                              borderImage.enabled = false;
                        }

                        // Полная непрозрачность
                        cameraPreview.color = Color.white;

                        // Делаем превью видимым
                        cameraPreview.gameObject.SetActive(true);
                  }
                  else
                  {
                        // Режим превью - маленькое окно в правом нижнем углу

                        // Возвращаем родительский rect к нормальному состоянию если нужно
                        if (parentRect != null)
                        {
                              parentRect.anchorMin = new Vector2(0.7f, 0.05f);
                              parentRect.anchorMax = new Vector2(0.95f, 0.25f);
                              parentRect.offsetMin = Vector2.zero;
                              parentRect.offsetMax = Vector2.zero;
                        }

                        // Настраиваем превью
                        rectTransform.anchorMin = Vector2.zero;
                        rectTransform.anchorMax = Vector2.one;
                        rectTransform.offsetMin = Vector2.zero;
                        rectTransform.offsetMax = Vector2.zero;

                        // Включаем рамку если есть
                        Image borderImage = cameraPreview.GetComponent<Image>();
                        if (borderImage != null)
                        {
                              borderImage.enabled = true;
                        }

                        // Небольшая прозрачность
                        cameraPreview.color = new Color(1, 1, 1, 0.8f);
                  }

                  Debug.Log($"Camera view mode set to {(fullscreen ? "fullscreen" : "preview")} with rect: {rectTransform.rect}");
            }

            // Метод для обновления видимости маркеров стен
            private void UpdateWallMarkersVisibility(bool visible)
            {
                  foreach (var marker in wallMarkers)
                  {
                        if (marker != null)
                        {
                              marker.SetActive(visible);
                        }
                  }
            }

            // Method to paint all detected walls with the current color
            private void PaintAllWalls()
            {
                  Debug.Log("Painting all walls with color: " + ColorToHex(GetPaintColor()));

                  // Check if we have walls to paint
                  bool wallsPainted = false;

                  // First try to paint created wall objects
                  if (createdWallObjects != null && createdWallObjects.Count > 0)
                  {
                        foreach (GameObject wall in createdWallObjects)
                        {
                              if (wall != null)
                              {
                                    PaintWallTexture(wall);
                                    CreatePaintEffectAtHitPoint(wall.transform.position, wall.transform.forward, GetPaintColor());
                                    wallsPainted = true;
                              }
                        }
                  }

                  // Then try wall markers
                  if (wallMarkers != null && wallMarkers.Count > 0)
                  {
                        foreach (GameObject marker in wallMarkers)
                        {
                              if (marker != null)
                              {
                                    PaintWallTexture(marker);
                                    CreatePaintEffectAtHitPoint(marker.transform.position, marker.transform.forward, GetPaintColor());
                                    wallsPainted = true;
                              }
                        }
                  }

                  // Notify the user about the result
                  if (wallsPainted)
                  {
                        ShowMessage("Все стены окрашены в выбранный цвет", 2.0f);
                        InvokeOnWallPainted();
                  }
                  else
                  {
                        ShowMessage("Не найдены стены для покраски", 2.0f);
                  }
            }

            // Метод, вызываемый после покраски стены
            private void InvokeOnWallPainted()
            {
                  // Здесь можно добавить логику, которая выполняется после покраски стены
                  // Например, звуковой эффект, обновление UI и т.д.
                  Debug.Log("Стена окрашена");

                  // Вызываем делегат, если он установлен
                  OnWallPainted?.Invoke(highlightedWall);
            }

            // Method to toggle camera view mode
            private void ToggleCameraViewMode()
            {
                  SetCameraViewMode(!isFullScreenCamera);
            }

            // Method to toggle between camera modes
            private void ToggleCamera()
            {
                  // Toggle between the AR camera and webcam views
                  ToggleCameraViewMode();
            }

            // Метод для отображения визуального отклика при клике
            private void ShowClickFeedback(Vector2 screenPosition)
            {
                  // Создаем временный объект для визуального эффекта
                  GameObject clickEffect = new GameObject("ClickFeedback");

                  // Находим или создаем канвас
                  Canvas canvas = FindFirstObjectByType<Canvas>();
                  if (canvas == null)
                  {
                        GameObject canvasObj = new GameObject("Canvas");
                        canvas = canvasObj.AddComponent<Canvas>();
                        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                        canvasObj.AddComponent<CanvasScaler>();
                        canvasObj.AddComponent<GraphicRaycaster>();
                  }

                  clickEffect.transform.SetParent(canvas.transform, false);

                  // Добавляем компоненты
                  RectTransform rectTransform = clickEffect.AddComponent<RectTransform>();
                  Image image = clickEffect.AddComponent<Image>();

                  // Создаем текстуру для эффекта клика
                  image.sprite = CreateCircleSprite(64, Color.white);
                  image.color = new Color(1, 1, 1, 0.5f);

                  // Настраиваем позицию и размер
                  rectTransform.anchorMin = new Vector2(0, 0);
                  rectTransform.anchorMax = new Vector2(0, 0);
                  rectTransform.pivot = new Vector2(0.5f, 0.5f);
                  rectTransform.sizeDelta = new Vector2(50, 50);
                  rectTransform.position = screenPosition;

                  // Запускаем анимацию затухания
                  StartCoroutine(FadeOutClickEffect(clickEffect));

                  // Удаляем эффект через короткое время
                  Destroy(clickEffect, 0.5f);
            }
            // Корутина для эффекта затухания клика
            private IEnumerator FadeOutClickEffect(GameObject effect)
            {
                  Image image = effect.GetComponent<Image>();
                  if (image == null) yield break;

                  float duration = 0.4f;
                  float elapsed = 0f;

                  Color startColor = image.color;
                  Vector2 startSize = effect.GetComponent<RectTransform>().sizeDelta;
                  Vector2 targetSize = startSize * 2.0f;

                  while (elapsed < duration)
                  {
                        float t = elapsed / duration;

                        // Уменьшаем прозрачность
                        float alpha = Mathf.Lerp(startColor.a, 0f, t);
                        image.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

                        // Увеличиваем размер
                        effect.GetComponent<RectTransform>().sizeDelta = Vector2.Lerp(startSize, targetSize, t);

                        elapsed += Time.deltaTime;
                        yield return null;
                  }
            }

            // Метод для показа предпросмотра покраски при наведении
            private void ShowPaintPreview(GameObject wall, Vector3 hitPoint, Vector3 hitNormal)
            {
                  if (wall == null) return;

                  // Указатель для предпросмотра
                  GameObject previewIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                  previewIndicator.name = "PaintPreviewIndicator";
                  previewIndicator.transform.position = hitPoint;
                  previewIndicator.transform.localScale = new Vector3(brushSize * 2, brushSize * 0.2f, brushSize * 2);
                  previewIndicator.transform.up = hitNormal;

                  // Настраиваем материал
                  Renderer renderer = previewIndicator.GetComponent<Renderer>();
                  if (renderer != null)
                  {
                        Material material = new Material(Shader.Find("Transparent/Diffuse"));
                        Color previewColor = GetPaintColor();
                        previewColor.a = 0.5f; // Полупрозрачный
                        material.color = previewColor;
                        renderer.material = material;
                  }

                  // Отключаем коллайдер
                  Collider collider = previewIndicator.GetComponent<Collider>();
                  if (collider != null)
                  {
                        collider.enabled = false;
                  }

                  // Удаляем через короткое время
                  Destroy(previewIndicator, 0.2f);
            }

            // Add the overloaded method used in code
            private IEnumerator ShowPaintPreview(GameObject wall, Color color, float duration)
            {
                  // Find a position on the wall to preview the paint
                  Renderer renderer = wall.GetComponent<Renderer>();
                  if (renderer != null)
                  {
                        // Use the center of the object for preview
                        Vector3 position = renderer.bounds.center;
                        Vector3 normal = wall.transform.forward;

                        // Show paint preview effect
                        CreatePaintEffectAtHitPoint(position, normal, color);

                        // Wait for duration
                        yield return new WaitForSeconds(duration);
                  }
                  else
                  {
                        yield return null;
                  }
            }

            // Метод для создания частиц при покраске стены
            private void CreatePaintParticles(Vector3 position, Vector3 normal)
            {
                  // Создаем объект для системы частиц
                  GameObject particleObj = new GameObject("PaintParticles");
                  particleObj.transform.position = position;
                  particleObj.transform.rotation = Quaternion.LookRotation(normal);

                  // Добавляем компонент системы частиц
                  ParticleSystem particleSystem = particleObj.AddComponent<ParticleSystem>();

                  // Настраиваем систему частиц
                  var main = particleSystem.main;
                  main.startSpeed = 1.0f;
                  main.startSize = 0.05f;
                  main.startLifetime = 0.5f;
                  main.startColor = GetPaintColor();

                  // Настраиваем форму эмиссии
                  var shape = particleSystem.shape;
                  shape.shapeType = ParticleSystemShapeType.Cone;
                  shape.angle = 25f;

                  // Настраиваем эмиссию
                  var emission = particleSystem.emission;
                  emission.rateOverTime = 0;
                  emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 20) });

                  // Удаляем объект через 2 секунды
                  Destroy(particleObj, 2.0f);
            }

            // Add overload without parameters for UpdateColorSelection method
            private void UpdateColorSelection()
            {
                  try
                  {
                        // If we have a current paint color, use it
                        if (currentPaintColor != Color.clear)
                        {
                              // Call the version that takes a color
                              UpdateColorSelection(currentPaintColor);
                        }
                        else if (selectedColor != Color.clear)
                        {
                              // Or use selected color if available
                              UpdateColorSelection(selectedColor);
                        }
                        else if (duluxColors != null && duluxColors.Count > 0)
                        {
                              // Otherwise use the first color in our palette
                              UpdateColorSelection(duluxColors[0]);
                        }
                        else
                        {
                              // Default to white if nothing else available
                              UpdateColorSelection(Color.white);
                        }
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Error in parameterless UpdateColorSelection: {e.Message}");
                  }
            }

            // Метод для очистки маркеров стен
            private void ClearWallMarkers()
            {
                  // Удаляем все объекты маркеров стен
                  foreach (GameObject marker in wallMarkers)
                  {
                        if (marker != null)
                        {
                              Destroy(marker);
                        }
                  }

                  // Очищаем список
                  wallMarkers.Clear();
            }

            // Метод для создания маркера стены из данных о стене
            private GameObject CreateWallMarker(WallData wall, Transform parent)
            {
                  if (wall.position == Vector3.zero && wall.rotation == Quaternion.identity)
                  {
                        Debug.LogWarning("CreateWallMarker: Wall position or rotation is invalid");
                        return null;
                  }

                  // Создаем квадратный маркер для стены
                  GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
                  marker.name = $"WallMarker_{wall.id}";
                  marker.transform.position = wall.position;
                  marker.transform.rotation = wall.rotation;

                  // Увеличиваем размер стены на 20% для лучшей видимости
                  Vector3 wallSize = wall.scale;
                  if (wallSize == Vector3.zero) wallSize = new Vector3(2f, 1.5f, 0.1f);
                  marker.transform.localScale = wallSize * 1.2f;

                  // Настраиваем материал для стены
                  Renderer renderer = marker.GetComponent<Renderer>();
                  if (renderer != null)
                  {
                        Material wallMaterial = new Material(Shader.Find("Standard"));
                        wallMaterial.color = new Color(0.0f, 0.7f, 1.0f, 1.0f); // Голубой цвет

                        // Добавляем свечение для лучшей видимости
                        wallMaterial.EnableKeyword("_EMISSION");
                        wallMaterial.SetColor("_EmissionColor", new Color(0.0f, 0.5f, 0.7f));

                        renderer.material = wallMaterial;
                  }

                  // Устанавливаем родителя
                  if (parent != null)
                  {
                        marker.transform.SetParent(parent);
                  }

                  // Добавляем маркер в список
                  wallMarkers.Add(marker);

                  // Добавляем стену в список созданных объектов стен
                  createdWallObjects.Add(marker);

                  return marker;
            }

            // Метод для обновления UI
            private void UpdateUI()
            {
                  // Обновляем видимость UI элементов в зависимости от режима
                  if (captureButton != null)
                  {
                        Text captureText = captureButton.GetComponentInChildren<Text>();
                        if (captureText != null)
                        {
                              captureText.text = isCapturing ? "Stop" : "Capture";
                        }
                  }

                  if (resetButton != null)
                  {
                        resetButton.interactable = detectedWalls.Count > 0;
                  }

                  if (toggleModeButton != null)
                  {
                        Text toggleText = toggleModeButton.GetComponentInChildren<Text>();
                        if (toggleText != null)
                        {
                              toggleText.text = isPaintingMode ? "Detect" : "Paint";
                        }
                  }

                  // Обновляем видимость цветовой палитры
                  if (colorPalettePanel != null)
                  {
                        colorPalettePanel.SetActive(isPaintingMode);
                  }
            }

            // Метод для настройки продвинутого детектирования стен
            private void ConfigureAdvancedWallDetection()
            {
                  if (wallDetector == null) return;

                  // Устанавливаем параметры через рефлексию, если доступны
                  var lineThresholdField = wallDetector.GetType().GetField("lineThreshold", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                  if (lineThresholdField != null)
                  {
                        lineThresholdField.SetValue(wallDetector, 50); // Уменьшенный порог для лучшего обнаружения линий
                  }

                  var minLineThresholdField = wallDetector.GetType().GetField("minLineLength", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                  if (minLineThresholdField != null)
                  {
                        minLineThresholdField.SetValue(wallDetector, 30); // Минимальная длина линии
                  }

                  var maxLineGapField = wallDetector.GetType().GetField("maxLineGap", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                  if (maxLineGapField != null)
                  {
                        maxLineGapField.SetValue(wallDetector, 10); // Максимальный разрыв между линиями
                  }

                  // Настраиваем камеру для лучшего обнаружения
                  if (mainCamera != null)
                  {
                        mainCamera.fieldOfView = 60; // Оптимальное поле зрения для обнаружения стен
                  }

                  Debug.Log("Настроено продвинутое детектирование стен");
            }

            // Метод для включения отображения контуров на камере
            private void EnableContoursOnCamera()
            {
                  if (wallDetector == null) return;

                  // Настройка отображения контуров через рефлексию
                  var showContoursField = wallDetector.GetType().GetField("showContours", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                  if (showContoursField != null)
                  {
                        showContoursField.SetValue(wallDetector, true);
                  }

                  var showDebugLines = wallDetector.GetType().GetField("showDebugLines", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                  if (showDebugLines != null)
                  {
                        showDebugLines.SetValue(wallDetector, true);
                  }

                  Debug.Log("Отображение контуров на камере включено");
            }

            // Метод для покраски текстуры стены
            private void PaintWallTexture(GameObject wall)
            {
                  if (wall == null) return;

                  try
                  {
                        // Получаем компонент рендерера
                        Renderer renderer = wall.GetComponent<Renderer>();
                        if (renderer == null) return;

                        // Создаем материал для покраски, если нужно
                        if (renderer.material.name.Contains("Default-Material") || !renderer.material.name.Contains("PaintMaterial"))
                        {
                              Material paintMaterial = new Material(Shader.Find("Standard"));
                              paintMaterial.name = "PaintMaterial";
                              // Настраиваем свойства материала для хорошего отображения цвета
                              paintMaterial.SetFloat("_Glossiness", 0.1f); // Низкий блеск для матового эффекта
                              paintMaterial.SetFloat("_Metallic", 0.0f);   // Не металлический материал
                              renderer.material = paintMaterial;
                        }

                        // Получаем цвет для покраски
                        Color paintColor = GetPaintColor();

                        // Устанавливаем непрозрачность, чтобы стена была полностью видна
                        paintColor.a = 1.0f;

                        // Применяем цвет к материалу
                        renderer.material.color = paintColor;

                        Debug.Log($"Стена покрашена в цвет: {ColorToHex(paintColor)}");
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Ошибка при покраске стены: {e.Message}");
                  }
            }

            // Перегруженный метод покраски стены с указанным цветом
            private void PaintWallTexture(GameObject wall, Color paintColor)
            {
                  if (wall == null) return;

                  try
                  {
                        // Получаем компонент рендерера
                        Renderer renderer = wall.GetComponent<Renderer>();
                        if (renderer == null) return;

                        // Создаем материал для покраски, если нужно
                        if (renderer.material.name.Contains("Default-Material") || !renderer.material.name.Contains("PaintMaterial"))
                        {
                              Material paintMaterial = new Material(Shader.Find("Standard"));
                              paintMaterial.name = "PaintMaterial";
                              // Настраиваем свойства материала для хорошего отображения цвета
                              paintMaterial.SetFloat("_Glossiness", 0.1f); // Низкий блеск для матового эффекта
                              paintMaterial.SetFloat("_Metallic", 0.0f);   // Не металлический материал
                              renderer.material = paintMaterial;
                        }

                        // Устанавливаем непрозрачность, чтобы стена была полностью видна
                        paintColor.a = 1.0f;

                        // Применяем цвет к материалу
                        renderer.material.color = paintColor;

                        Debug.Log($"Стена покрашена в цвет: {ColorToHex(paintColor)}");
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Ошибка при покраске стены: {e.Message}");
                  }

            }

            // Метод для получения текущего цвета покраски
            private Color GetPaintColor()
            {
                  // Если уже есть выбранный цвет, возвращаем его
                  if (currentPaintColor != Color.clear)
                  {
                        return currentPaintColor;
                  }

                  // Иначе возвращаем первый цвет из палитры или белый, если палитра пуста
                  return duluxColors.Count > 0 ? duluxColors[0] : Color.white;
            }

            // Метод для преобразования цвета в шестнадцатеричную строку
            private string ColorToHex(Color color)
            {
                  return string.Format("#{0:X2}{1:X2}{2:X2}",
                        (int)(color.r * 255),
                        (int)(color.g * 255),
                        (int)(color.b * 255));
            }

            // Метод для создания текстуры круга
            private Sprite CreateCircleSprite(int resolution, Color color)
            {
                  // Создаем новую текстуру
                  Texture2D texture = new Texture2D(resolution, resolution);

                  // Заполняем круг
                  float radius = resolution / 2f;
                  float radiusSquared = radius * radius;

                  for (int x = 0; x < resolution; x++)
                  {
                        for (int y = 0; y < resolution; y++)
                        {
                              // Расстояние от центра
                              float dx = x - radius;
                              float dy = y - radius;
                              float distanceSquared = dx * dx + dy * dy;

                              // Если внутри круга - полностью непрозрачный
                              // Если на границе - полупрозрачный для сглаживания
                              if (distanceSquared <= radiusSquared)
                              {
                                    texture.SetPixel(x, y, color);
                              }
                              else
                              {
                                    texture.SetPixel(x, y, Color.clear);
                              }

                        }
                  }

                  // Применяем изменения
                  texture.Apply();

                  // Создаем спрайт из текстуры, используем полное имя UnityEngine.Rect для устранения неоднозначности
                  return Sprite.Create(texture, new UnityEngine.Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f));
            }
            // Метод для создания материала подсветки
            private void CreateHighlightMaterial()
            {
                  try
                  {
                        // Create a new material for highlighting walls
                        highlightMaterial = new Material(Shader.Find("Standard"));
                        if (highlightMaterial != null)
                        {
                              highlightMaterial.color = new Color(1, 0.92f, 0.016f, 0.5f); // Semi-transparent yellow
                              highlightMaterial.EnableKeyword("_EMISSION");
                              highlightMaterial.SetColor("_EmissionColor", new Color(1f, 0.9f, 0.1f, 1f));
                        }
                        else
                        {
                              Debug.LogWarning("Failed to create highlight material using Standard shader");

                              // Try with a different shader as fallback
                              highlightMaterial = new Material(Shader.Find("Unlit/Color"));
                              if (highlightMaterial != null)
                              {
                                    highlightMaterial.color = new Color(1, 0.92f, 0.016f, 0.5f);
                              }
                        }
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Error creating highlight material: {e.Message}");
                  }
            }

            // Метод для создания цветовой палитры
            private void CreateColorPalette()
            {
                  // Проверка, существует ли уже палитра
                  if (colorPalettePanel != null)
                  {
                        // Удаляем старую палитру
                        Destroy(colorPalettePanel);
                  }

                  try
                  {
                        // Ищем или создаем канвас
                        Canvas canvas = FindFirstObjectByType<Canvas>();
                        if (canvas == null)
                        {
                              GameObject canvasObj = new GameObject("Canvas");
                              canvas = canvasObj.AddComponent<Canvas>();
                              canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                              canvasObj.AddComponent<CanvasScaler>();
                              canvasObj.AddComponent<GraphicRaycaster>();
                        }

                        // Создаем панель цветовой палитры
                        colorPalettePanel = new GameObject("ColorPalettePanel");
                        colorPalettePanel.transform.SetParent(canvas.transform, false);

                        // Добавляем компоненты для панели
                        RectTransform rectTransform = colorPalettePanel.AddComponent<RectTransform>();
                        Image panelImage = colorPalettePanel.AddComponent<Image>();
                        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

                        // Настраиваем расположение панели
                        rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
                        rectTransform.anchorMax = new Vector2(1.0f, 0.15f);
                        rectTransform.offsetMin = Vector2.zero;
                        rectTransform.offsetMax = Vector2.zero;

                        // Создаем контейнер для кнопок цветов
                        GameObject container = new GameObject("ColorButtonsContainer");
                        container.transform.SetParent(colorPalettePanel.transform, false);

                        // Добавляем компоненты для контейнера
                        RectTransform containerRect = container.AddComponent<RectTransform>();
                        containerRect.anchorMin = new Vector2(0.0f, 0.0f);
                        containerRect.anchorMax = new Vector2(1.0f, 1.0f);
                        containerRect.offsetMin = new Vector2(10, 10);
                        containerRect.offsetMax = new Vector2(-10, -10);

                        // Добавляем горизонтальный лейаут
                        HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
                        layout.spacing = 10;
                        layout.childAlignment = TextAnchor.MiddleCenter;
                        layout.childForceExpandWidth = false;
                        layout.childForceExpandHeight = false;
                        layout.childControlWidth = false;
                        layout.childControlHeight = false;

                        // Устанавливаем ссылку на контейнер
                        colorPaletteContainer = container.transform;

                        // Проверяем, что контейнер создан успешно
                        if (colorPaletteContainer != null)
                        {
                              CreateColorButtons();
                              Debug.Log("Цветовая палитра и кнопки созданы успешно");
                        }
                        else
                        {
                              Debug.LogError("Не удалось создать colorPaletteContainer");
                        }

                        // Скрываем панель цветов, если не в режиме покраски
                        colorPalettePanel.SetActive(isPaintingMode);
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Ошибка при создании цветовой палитры: {e.Message}");
                        // Если не удалось создать палитру, используем дефолтный цвет
                        currentPaintColor = Color.white;
                  }
            }

            // Метод для создания кнопок цветов
            private void CreateColorButtons()
            {
                  if (colorPaletteContainer == null)
                  {
                        Debug.LogError("Не найден контейнер для кнопок цветов!");
                        return;
                  }

                  try
                  {
                        // Очищаем контейнер от старых кнопок
                        foreach (Transform child in colorPaletteContainer)
                        {
                              Destroy(child.gameObject);
                        }

                        // Если список цветов пуст, добавляем несколько стандартных цветов
                        if (duluxColors == null || duluxColors.Count == 0)
                        {
                              duluxColors = new List<Color>
                              {
                                    Color.white,
                                    Color.gray,
                                    Color.black,
                                    Color.red,
                                    Color.green,
                                    Color.blue,
                                    Color.yellow,
                                    new Color(1, 0.5f, 0) // Оранжевый
                              };
                        }

                        // Создаем кнопку для каждого цвета
                        foreach (Color color in duluxColors)
                        {
                              CreateColorButton(color);
                        }

                        // Выбираем первый цвет по умолчанию
                        if (duluxColors.Count > 0)
                        {
                              currentPaintColor = duluxColors[0];
                              UpdateColorSelection(duluxColors[0]);
                        }
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Ошибка при создании кнопок цветов: {e.Message}");
                  }
            }

            // Метод для создания отдельной кнопки цвета
            private void CreateColorButton(Color color)
            {
                  // Создаем объект кнопки
                  GameObject buttonObj = new GameObject($"ColorButton_{ColorToHex(color)}");
                  buttonObj.transform.SetParent(colorPaletteContainer, false);

                  // Добавляем компоненты для кнопки
                  RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
                  Image buttonImage = buttonObj.AddComponent<Image>();
                  Button button = buttonObj.AddComponent<Button>();

                  // Настраиваем внешний вид кнопки
                  buttonImage.color = color;
                  rectTransform.sizeDelta = new Vector2(50, 50);

                  // Добавляем обработчик события клика
                  button.onClick.AddListener(() => SelectColor(color));

                  // Создаем индикатор выбора (по умолчанию скрыт)
                  GameObject selectionIndicator = new GameObject("SelectionIndicator");
                  selectionIndicator.transform.SetParent(buttonObj.transform, false);

                  RectTransform indicatorRect = selectionIndicator.AddComponent<RectTransform>();
                  Image indicatorImage = selectionIndicator.AddComponent<Image>();

                  // Настраиваем внешний вид индикатора
                  indicatorRect.anchorMin = Vector2.zero;
                  indicatorRect.anchorMax = Vector2.one;
                  indicatorRect.offsetMin = new Vector2(2, 2);
                  indicatorRect.offsetMax = new Vector2(-2, -2);

                  // Устанавливаем цвет индикатора в зависимости от яркости кнопки
                  float brightness = color.r * 0.299f + color.g * 0.587f + color.b * 0.114f;
                  indicatorImage.color = brightness > 0.5f ? Color.black : Color.white;

                  // Скрываем индикатор по умолчанию
                  selectionIndicator.SetActive(false);
            }

            // Метод для выбора цвета
            private void SelectColor(Color color)
            {
                  currentPaintColor = color;

                  // Обновляем визуальное отображение выбранного цвета
                  UpdateColorSelection(color);

                  // Выводим информацию о выбранном цвете
                  Debug.Log($"Выбран цвет: {ColorToHex(color)}");

                  // Показываем сообщение пользователю
                  ShowMessage($"Выбран цвет: {ColorToHex(color)}", 1.5f);
            }

            // Метод для обновления выбранного цвета в палитре
            private void UpdateColorSelection(Color selectedColor)
            {
                  try
                  {
                        // Проверяем, существует ли контейнер цветовой палитры
                        if (colorPaletteContainer == null || colorPaletteContainer.gameObject == null || !colorPaletteContainer.gameObject.activeInHierarchy)
                        {
                              Debug.LogWarning("UpdateColorSelection: colorPaletteContainer is null or inactive");
                              return;
                        }

                        // Перебираем все дочерние объекты (кнопки цветов)
                        for (int i = 0; i < colorPaletteContainer.childCount; i++)
                        {
                              Transform child = colorPaletteContainer.GetChild(i);

                              if (child == null)
                              {
                                    Debug.LogWarning($"UpdateColorSelection: Child at index {i} is null");
                                    continue;
                              }

                              try
                              {
                                    // Ищем компонент Image для проверки цвета
                                    Image buttonImage = child.GetComponent<Image>();
                                    if (buttonImage == null)
                                    {
                                          continue;
                                    }

                                    // Ищем индикатор выбора
                                    Transform indicatorTransform = child.Find("SelectionIndicator");
                                    if (indicatorTransform == null)
                                    {
                                          continue;
                                    }

                                    GameObject indicator = indicatorTransform.gameObject;

                                    // Проверяем, соответствует ли цвет кнопки выбранному
                                    bool isSelected = ColorCloseTo(buttonImage.color, selectedColor);

                                    // Обновляем активность индикатора
                                    if (indicator.activeSelf != isSelected)
                                    {
                                          indicator.SetActive(isSelected);

                                          // Если кнопка выбрана, запускаем анимацию индикатора
                                          if (isSelected)
                                          {
                                                StartCoroutine(PulseIndicator(indicator));
                                          }
                                    }
                              }
                              catch (System.Exception e)
                              {
                                    Debug.LogError($"UpdateColorSelection: Error processing child at index {i}: {e.Message}");
                              }
                        }

                        // Обновляем текущий цвет
                        currentPaintColor = selectedColor;

                        // Пытаемся покрасить все выделенные стены
                        PaintSelectedWalls();
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Error in UpdateColorSelection: {e.Message}");
                  }
            }

            // Метод для сравнения близости цветов
            private bool ColorCloseTo(Color a, Color b, float threshold = 0.05f)
            {
                  return Mathf.Abs(a.r - b.r) < threshold &&
                        Mathf.Abs(a.g - b.g) < threshold &&
                        Mathf.Abs(a.b - b.b) < threshold;
            }

            // Метод для пульсации индикатора выбора
            private IEnumerator PulseIndicator(GameObject indicator)
            {
                  if (indicator == null)
                  {
                        Debug.LogWarning("PulseIndicator: indicator is null");
                        yield break;
                  }

                  Image image = indicator.GetComponent<Image>();
                  if (image == null)
                  {
                        Debug.LogWarning("PulseIndicator: Image component not found");
                        yield break;
                  }

                  // Проверяем, активен ли еще индикатор
                  if (!indicator.activeInHierarchy || image == null)
                  {
                        yield break;
                  }

                  // Сохраняем оригинальный цвет
                  Color originalColor = image.color;

                  // Настройки анимации
                  float duration = 1.0f;
                  float t = 0;

                  // Пока индикатор активен
                  while (indicator != null && indicator.activeInHierarchy)
                  {
                        // Проверка и выполнение в отдельном блоке
                        bool shouldContinue = true;

                        try
                        {
                              // Проверяем активность во время выполнения
                              if (!indicator.activeInHierarchy || image == null)
                              {
                                    shouldContinue = false;
                              }
                              else
                              {
                                    // Вычисляем значение для пульсации (от 0.6 до 1.0) с использованием duration
                                    float pulse = 0.6f + 0.4f * Mathf.PingPong(t / duration, 1.0f);

                                    // Применяем пульсацию к прозрачности
                                    image.color = new Color(originalColor.r, originalColor.g, originalColor.b, pulse);

                                    // Инкрементируем время
                                    t += Time.deltaTime;
                              }
                        }
                        catch (System.Exception e)
                        {
                              Debug.LogWarning($"PulseIndicator: Exception caught: {e.Message}");
                              shouldContinue = false;
                        }

                        // Если возникла ошибка или объект уничтожен, завершаем корутину
                        if (!shouldContinue)
                        {
                              yield break;
                        }

                        // Ждем следующий кадр вне блока try-catch
                        yield return null;
                  }
            }

            // Метод для покраски выбранных стен
            private void PaintSelectedWalls()
            {
                  if (highlightedWall != null)
                  {
                        // Красим только выделенную стену
                        PaintWallTexture(highlightedWall);

                        // Создаем эффект покраски
                        CreatePaintEffectAtHitPoint(highlightedWall.transform.position,
                                                  highlightedWall.transform.forward,
                                                  currentPaintColor);

                        Debug.Log($"Покрашена выделенная стена: {highlightedWall.name}");
                  }
            }

            // Метод для удаления всех рамок и визуальных элементов, которые могут мешать полноэкранному изображению камеры
            private void RemoveCameraPreviewBorders()
            {
                  if (cameraPreview == null) return;

                  // Находим все возможные родительские объекты с компонентами, которые могут создавать визуальные рамки
                  Transform current = cameraPreview.transform;
                  while (current != null)
                  {
                        // Отключаем все изображения, которые могут создавать рамки
                        Image img = current.GetComponent<Image>();
                        if (img != null && img.gameObject != cameraPreview.gameObject)
                        {
                              img.enabled = false;
                              Debug.Log($"Disabled Image component on {current.name} to remove borders");
                        }

                        // Настраиваем все RectTransform на заполнение всего доступного пространства
                        RectTransform rect = current.GetComponent<RectTransform>();
                        if (rect != null)
                        {
                              rect.anchorMin = Vector2.zero;
                              rect.anchorMax = Vector2.one;
                              rect.offsetMin = Vector2.zero;
                              rect.offsetMax = Vector2.zero;
                              rect.sizeDelta = Vector2.zero;
                              Debug.Log($"Set RectTransform on {current.name} to fill entire available space");
                        }

                        current = current.parent;
                  }

                  // Убеждаемся, что само превью настроено правильно
                  cameraPreview.color = Color.white;
                  // RawImage не имеет свойства preserveAspect - используем uvRect вместо этого
                  cameraPreview.uvRect = new UnityEngine.Rect(0, 0, 1, 1);

                  // Заставляем Canvas перерисоваться
                  Canvas.ForceUpdateCanvases();

                  // Обновляем текстуру превью
                  // ... existing code ...
            }

            // Метод для обработки события выбора стены
            private void OnWallSelected(WallData wallData)
            {
                  // Проверяем данные
                  if (wallData == null)
                  {
                        Debug.LogWarning("Получены пустые данные о стене");
                        return;
                  }

                  Debug.Log($"Стена {wallData.id} выбрана для покраски");

                  // Получаем текущий цвет для покраски
                  Color paintColor = GetPaintColor();

                  // Находим или создаем GameObject для стены
                  GameObject wallObject = FindWallObjectById(int.Parse(wallData.id));

                  if (wallObject == null)
                  {
                        // Создаем новый объект стены
                        wallObject = CreateWallObject(wallData);

                        if (wallObject == null)
                        {
                              Debug.LogError("Не удалось создать объект стены");
                              return;
                        }
                  }

                  // Красим стену
                  PaintWallTexture(wallObject, paintColor);

                  // Эффекты покраски для визуального отклика
                  CreatePaintEffectAtHitPoint(wallObject.transform.position, wallObject.transform.forward, paintColor);

                  // Показываем сообщение пользователю
                  ShowMessage($"Стена #{wallData.id} окрашена в выбранный цвет", 2.0f);

                  // Вызываем событие покраски стены
                  OnWallPainted?.Invoke(wallObject);
            }

            // Метод для поиска объекта стены по ID
            private GameObject FindWallObjectById(int wallId)
            {
                  // Ищем среди существующих стен по имени
                  foreach (var wall in createdWallObjects)
                  {
                        if (wall != null && wall.name == $"Wall_{wallId}")
                        {
                              return wall;
                        }
                  }

                  // Если ещё не создан, возвращаем null
                  return null;
            }

            // Метод для создания 3D объекта стены
            private GameObject CreateWallObject(WallData wallData)
            {
                  try
                  {
                        // Создаем объект стены
                        GameObject wallObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        wallObject.name = $"Wall_{wallData.id}";

                        // Устанавливаем позицию, вращение и масштаб
                        wallObject.transform.position = wallData.position;
                        wallObject.transform.rotation = wallData.rotation;
                        wallObject.transform.localScale = wallData.scale;

                        // Настраиваем материал
                        Renderer renderer = wallObject.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                              Material material = new Material(Shader.Find("Standard"));
                              material.name = "WallMaterial";
                              material.SetFloat("_Glossiness", 0.1f);
                              material.SetFloat("_Metallic", 0.0f);

                              // Полупрозрачный материал, чтобы видеть камеру сквозь стену
                              Color defaultColor = new Color(0.9f, 0.9f, 0.9f, 0.7f);
                              material.color = defaultColor;

                              // Включаем прозрачность
                              material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                              material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                              material.SetInt("_ZWrite", 0);
                              material.DisableKeyword("_ALPHATEST_ON");
                              material.EnableKeyword("_ALPHABLEND_ON");
                              material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                              material.renderQueue = 3000;

                              renderer.material = material;
                        }

                        // Добавляем коллайдер для обработки кликов
                        wallObject.AddComponent<BoxCollider>();

                        // Назначаем слой Wall
                        wallObject.layer = LayerMask.NameToLayer("Wall");

                        // Добавляем в список созданных стен
                        createdWallObjects.Add(wallObject);

                        Debug.Log($"Создан объект стены {wallData.id} в позиции {wallData.position}");

                        return wallObject;
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Ошибка при создании объекта стены: {e.Message}");
                        return null;
                  }
            }
      }
}
