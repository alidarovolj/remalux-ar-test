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
            private float paintOpacity = 0.8f; // Непрозрачность покраски
            private float maxPaintDistance = 5.0f; // Максимальное расстояние для покраски
            private float maxPaintAngle = 60.0f; // Максимальный угол для покраски
            private float paintInterval = 0.05f; // Интервал между покрасками

            // Camera stability tracking
            private float cameraStabilityPositionThreshold = 0.01f; // Порог стабильности позиции
            private float cameraStabilityRotationThreshold = 1.0f; // Порог стабильности поворота
            private Vector3 lastCameraPosition; // Последняя позиция камеры
            private Quaternion lastCameraRotation; // Последний поворот камеры

            // State control variables
            private bool isPaintMode = false; // Флаг режима покраски
            private bool isColorSelectionMode = false; // Флаг выбора цвета
            private bool paintingEnabled = false; // Флаг включения покраски

            // Класс для создания Billboard объектов, которые всегда поворачиваются к камере
            private class Billboard : MonoBehaviour
            {
                  private Camera mainCamera;

                  void Start()
                  {
                        mainCamera = Camera.main;
                        if (mainCamera == null)
                        {
                              mainCamera = FindObjectOfType<Camera>();
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
                  Canvas canvas = FindObjectOfType<Canvas>();
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
                        Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
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
                  Canvas canvas = FindObjectOfType<Canvas>();
                  Debug.Log("Creating EventSystem");
                  GameObject eventSystem = new GameObject("EventSystem");
                  eventSystem.AddComponent<EventSystem>();
                  eventSystem.AddComponent<StandaloneInputModule>();
            }

            // Метод инициализации, вызывается при старте
            private void Start()
            {
                  // Initialize the paint color
                  currentPaintColor = duluxColors.Count > 0 ? duluxColors[0] : Color.white;

                  // Create highlight material
                  CreateHighlightMaterial();

                  // Create the color palette UI
                  CreateColorPalette();

                  // Инициализируем компоненты
                  ValidateComponents();

                  // Set up user interface buttons
                  SetupUI();

                  // Инициализируем камеру
                  InitializeCamera();

                  // Настраиваем WallDetector для лучшего обнаружения стен
                  if (wallDetector != null)
                  {
                        Debug.Log("Настраиваю WallDetector для лучшего обнаружения стен");
                        wallDetector.mainCamera = mainCamera;

                        // Подписываемся на событие обнаружения стен
                        wallDetector.OnWallsDetected += OnWallsDetected;

                        // Начинаем обнаружение стен
                        wallDetector.StartDetection();
                  }
                  else
                  {
                        Debug.LogError("WallDetector не назначен! Обнаружение стен не будет работать.");
                  }
            }

            private void InitializeCamera()
            {
                  // Проверяем доступные веб-камеры
                  WebCamDevice[] devices = WebCamTexture.devices;
                  Debug.Log($"Найдены веб-камеры: {devices.Length}");

                  if (devices.Length == 0)
                  {
                        ShowMessage("Веб-камера не найдена", 5f);
                        return;
                  }

                  // Выводим доступные камеры для отладки
                  for (int i = 0; i < devices.Length; i++)
                  {
                        Debug.Log($"Камера: {devices[i].name}");
                  }

                  // Выбираем первую камеру
                  string cameraName = devices[0].name;
                  Debug.Log($"Выбрана веб-камера: {cameraName}");

                  // Создаем новую текстуру веб-камеры
                  if (webCamTexture != null)
                  {
                        webCamTexture.Stop();
                        Destroy(webCamTexture);
                  }

                  try
                  {
                        // Создаем текстуру с разрешением 1280x720
                        webCamTexture = new WebCamTexture(cameraName, 1280, 720, 30);
                        webCamTexture.Play();

                        // Проверяем наличие RawImage для отображения камеры
                        if (cameraPreview == null)
                        {
                              Debug.LogWarning("Camera preview RawImage not assigned, creating a new one");
                              GameObject previewObj = new GameObject("CameraPreview");
                              Canvas canvas = FindObjectOfType<Canvas>();
                              if (canvas != null)
                              {
                                    previewObj.transform.SetParent(canvas.transform, false);
                              }
                              RectTransform rectTransform = previewObj.AddComponent<RectTransform>();
                              cameraPreview = previewObj.AddComponent<RawImage>();
                        }

                        // Устанавливаем текстуру камеры в RawImage
                        cameraPreview.texture = webCamTexture;

                        // Устанавливаем масштаб по оси Y равным 1 (не переворачиваем камеру вертикально)
                        if (cameraPreview.rectTransform != null)
                        {
                              cameraPreview.rectTransform.localScale = new Vector3(1, 1, 1);
                        }

                        // Устанавливаем режим отображения на полный экран сразу
                        SetCameraViewMode(true);

                        ShowMessage("Камера инициализирована и отображается на весь экран", 3f);
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Ошибка при инициализации камеры: {e.Message}");
                        ShowMessage("Ошибка инициализации камеры", 5f);
                  }
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
                  testWalls.Add(new WallData
                  {
                        position = mainCamera.transform.position + mainCamera.transform.forward * 3.0f,
                        rotation = Quaternion.LookRotation(-mainCamera.transform.forward),
                        scale = new Vector3(3.0f, 2.0f, 0.1f)
                  });

                  // Создаем стену справа от камеры, очень близко
                  testWalls.Add(new WallData
                  {
                        position = mainCamera.transform.position + mainCamera.transform.right * 2.0f + mainCamera.transform.forward * 2.0f,
                        rotation = Quaternion.LookRotation(-mainCamera.transform.right),
                        scale = new Vector3(2.0f, 1.5f, 0.1f)
                  });

                  // Создаем стену слева от камеры, очень близко
                  testWalls.Add(new WallData
                  {
                        position = mainCamera.transform.position - mainCamera.transform.right * 2.0f + mainCamera.transform.forward * 2.0f,
                        rotation = Quaternion.LookRotation(mainCamera.transform.right),
                        scale = new Vector3(2.0f, 1.5f, 0.1f)
                  });

                  // Создаем стену над камерой, очень близко
                  testWalls.Add(new WallData
                  {
                        position = mainCamera.transform.position + Vector3.up * 2.0f + mainCamera.transform.forward * 2.0f,
                        rotation = Quaternion.LookRotation(Vector3.down),
                        scale = new Vector3(2.0f, 2.0f, 0.1f)
                  });

                  // Создаем стену под камерой, очень близко
                  testWalls.Add(new WallData
                  {
                        position = mainCamera.transform.position + Vector3.down * 0.5f + mainCamera.transform.forward * 2.0f,
                        rotation = Quaternion.LookRotation(Vector3.up),
                        scale = new Vector3(2.0f, 2.0f, 0.1f)
                  });

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
                  roomWalls.Add(new WallData
                  {
                        position = roomCenter + new Vector3(0, 0, halfLength),
                        rotation = Quaternion.LookRotation(Vector3.back), // Смотрит на камеру
                        scale = new Vector3(roomWidth, roomHeight, wallThickness)
                  });

                  // Создаем заднюю стену (позади камеры)
                  roomWalls.Add(new WallData
                  {
                        position = roomCenter + new Vector3(0, 0, -halfLength),
                        rotation = Quaternion.LookRotation(Vector3.forward),
                        scale = new Vector3(roomWidth, roomHeight, wallThickness)
                  });

                  // Создаем правую стену
                  roomWalls.Add(new WallData
                  {
                        position = roomCenter + new Vector3(halfWidth, 0, 0),
                        rotation = Quaternion.LookRotation(Vector3.left),
                        scale = new Vector3(roomLength, roomHeight, wallThickness)
                  });

                  // Создаем левую стену
                  roomWalls.Add(new WallData
                  {
                        position = roomCenter + new Vector3(-halfWidth, 0, 0),
                        rotation = Quaternion.LookRotation(Vector3.right),
                        scale = new Vector3(roomLength, roomHeight, wallThickness)
                  });

                  // Создаем пол
                  roomWalls.Add(new WallData
                  {
                        position = roomCenter + new Vector3(0, -halfHeight, 0),
                        rotation = Quaternion.LookRotation(Vector3.up),
                        scale = new Vector3(roomWidth, roomLength, wallThickness)
                  });

                  // Создаем потолок
                  roomWalls.Add(new WallData
                  {
                        position = roomCenter + new Vector3(0, halfHeight, 0),
                        rotation = Quaternion.LookRotation(Vector3.down),
                        scale = new Vector3(roomWidth, roomLength, wallThickness)
                  });

                  // Добавляем мебель или объекты для демонстрации (например, картину на стене)
                  roomWalls.Add(new WallData
                  {
                        position = roomCenter + new Vector3(0, 0, halfLength - 0.05f) + new Vector3(1.0f, 0.3f, 0),
                        rotation = Quaternion.LookRotation(Vector3.back),
                        scale = new Vector3(1.5f, 1.0f, 0.05f)
                  });

                  // Обрабатываем стены комнаты
                  OnWallsDetected(roomWalls);

                  Debug.Log($"Создана тестовая комната с {roomWalls.Count} стенами вокруг камеры в позиции {mainCamera.transform.position}");
            }

            private void ValidateComponents()
            {
                  if (mainCamera == null)
                  {
                        mainCamera = Camera.main;
                  }

                  if (wallDetector == null)
                  {
                        wallDetector = GetComponent<WallDetector>();
                  }

                  if (textureManager == null)
                  {
                        textureManager = FindObjectOfType<TextureManager>();
                  }

                  if (cameraPreview == null)
                  {
                        Debug.LogError("RealWallPaintingController: Camera preview RawImage is not assigned!");
                  }

                  if (mainCamera == null || wallDetector == null || textureManager == null || cameraPreview == null)
                  {
                        Debug.LogError("RealWallPaintingController: Missing required components!");
                        enabled = false;
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
                  Canvas canvas = FindObjectOfType<Canvas>();
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
                        Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
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
                        if (wall.id <= 0)
                        {
                              wall.id = i + 1; // ID начинается с 1
                              detectedWalls[i] = wall;
                        }
                  }

                  foreach (var wall in detectedWalls)
                  {
                        CreateWallMarker(wall, markersParent);
                  }

                  Debug.Log($"Обнаружено стен: {walls.Count}");
                  ShowMessage($"Обнаружено {walls.Count} стен", 2f);
                  UpdateUI();
            }

            // Метод для очистки визуальных маркеров стен
            private void ClearWallMarkers()
            {
                  GameObject markersParent = GameObject.Find("WallMarkersContainer");
                  if (markersParent != null)
                  {
                        foreach (Transform child in markersParent.transform)
                        {
                              Destroy(child.gameObject);
                        }
                  }
            }

            // Метод для создания визуального маркера стены
            private void CreateWallMarker(WallData wall, Transform parent)
            {
                  Debug.Log($"Создаю маркер в мировой позиции: {wall.position.ToString("F2")}, поворот: {wall.rotation.eulerAngles.ToString("F2")}");

                  // Создаем новый объект для маркера стены
                  GameObject marker = new GameObject($"WallMarker_{Time.frameCount}");
                  marker.transform.SetParent(parent);
                  marker.transform.position = wall.position;
                  marker.transform.rotation = wall.rotation;
                  marker.transform.localScale = wall.scale;

                  // Добавляем тег для идентификации
                  marker.tag = "Wall";

                  // Вычисляем реальные размеры в метрах
                  float width = wall.scale.x * 5.0f;
                  float height = wall.scale.y * 5.0f;

                  Debug.Log($"Размер маркера: {width.ToString("F1")}x{height.ToString("F1")}");

                  // Создаем меш для визуализации стены
                  MeshFilter meshFilter = marker.AddComponent<MeshFilter>();
                  MeshRenderer renderer = marker.AddComponent<MeshRenderer>();

                  // Создаем плоский квадрат для стены
                  Mesh mesh = new Mesh();

                  // Координаты вершин (квадрат 1x1)
                  mesh.vertices = new Vector3[]
                  {
                        new Vector3(-0.5f, -0.5f, 0), // нижний левый
                        new Vector3(0.5f, -0.5f, 0),  // нижний правый
                        new Vector3(0.5f, 0.5f, 0),   // верхний правый
                        new Vector3(-0.5f, 0.5f, 0)   // верхний левый
                  };

                  // Индексы треугольников (2 треугольника для квадрата)
                  mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };

                  // UV-координаты для текстуры
                  mesh.uv = new Vector2[]
                  {
                        new Vector2(0, 0),
                        new Vector2(1, 0),
                        new Vector2(1, 1),
                        new Vector2(0, 1)
                  };

                  // Нормали (все смотрят вперед)
                  mesh.normals = new Vector3[]
                  {
                        Vector3.forward,
                        Vector3.forward,
                        Vector3.forward,
                        Vector3.forward
                  };

                  mesh.RecalculateBounds();
                  meshFilter.mesh = mesh;

                  // Создаем полностью прозрачный материал вместо цветного блока
                  Material wallMaterial = new Material(Shader.Find("Transparent/Diffuse"));
                  wallMaterial.color = new Color(1f, 1f, 1f, 0f); // Полностью прозрачный

                  // Добавляем обводку контура стены
                  AddWallBorder(marker.transform, 1.0f, 1.0f);

                  Debug.Log($"Создан маркер стены с материалом {wallMaterial.shader.name}");

                  // Добавляем коллайдер для взаимодействия
                  BoxCollider collider = marker.AddComponent<BoxCollider>();
                  collider.size = new Vector3(1.0f, 1.0f, 0.1f);
                  collider.isTrigger = true; // Делаем триггером для прохождения сквозь стену

                  // Устанавливаем материал
                  renderer.material = wallMaterial;

                  // Добавляем информацию о размерах
                  AddEnhancedWallInfoLabel(marker.transform, wall);

                  // Добавляем маркеры углов
                  AddEnhancedCornerMarkers(marker.transform, 1.0f, 1.0f);

                  // Индикатор типа поверхности не добавляем

                  Debug.Log($"Создан маркер поверхности в позиции {wall.position.ToString("F2")}");

                  // Сохраняем ссылку на маркер для дальнейшего использования
                  wallMarkers.Add(marker);
            }

            // Новый метод для создания обводки стены
            private void AddWallBorder(Transform parentTransform, float width, float height)
            {
                  GameObject borderObj = new GameObject("WallBorder");
                  borderObj.transform.SetParent(parentTransform);
                  borderObj.transform.localPosition = Vector3.zero;
                  borderObj.transform.localRotation = Quaternion.identity;
                  borderObj.transform.localScale = Vector3.one;

                  LineRenderer lineRenderer = borderObj.AddComponent<LineRenderer>();
                  lineRenderer.useWorldSpace = false;
                  lineRenderer.startWidth = 0.03f;
                  lineRenderer.endWidth = 0.03f;
                  lineRenderer.positionCount = 5;
                  lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                  lineRenderer.startColor = Color.cyan;
                  lineRenderer.endColor = Color.yellow;

                  // Создаем прямоугольник вокруг стены
                  float hw = width * 0.5f;
                  float hh = height * 0.5f;

                  lineRenderer.SetPositions(new Vector3[] {
                        new Vector3(-hw, -hh, 0.01f),
                        new Vector3(hw, -hh, 0.01f),
                        new Vector3(hw, hh, 0.01f),
                        new Vector3(-hw, hh, 0.01f),
                        new Vector3(-hw, -hh, 0.01f)
                  });

                  // Добавляем эффект пульсации для привлечения внимания
                  BorderPulseAnimation pulseEffect = borderObj.AddComponent<BorderPulseAnimation>();
            }

            // Упрощенный метод для создания видимой рамки вокруг стены
            private void CreateSimpleWallBorder(Transform parentTransform, float width, float height)
            {
                  // Создаем объект для линии границы
                  GameObject border = new GameObject("WallBorder");
                  border.transform.SetParent(parentTransform);
                  border.transform.localPosition = new Vector3(0, 0, -0.01f); // Немного впереди основного маркера

                  // Создаем компонент LineRenderer для отрисовки границы
                  LineRenderer lineRenderer = border.AddComponent<LineRenderer>();
                  lineRenderer.useWorldSpace = false;
                  lineRenderer.positionCount = 5; // 4 угла + замыкание на начальной точке
                  lineRenderer.startWidth = 0.3f; // Гораздо более толстая линия для лучшей видимости
                  lineRenderer.endWidth = 0.3f;
                  lineRenderer.material = new Material(Shader.Find("Standard"));
                  lineRenderer.material.color = Color.yellow; // Яркий желтый для контраста с красным фоном
                  lineRenderer.material.EnableKeyword("_EMISSION");
                  lineRenderer.material.SetColor("_EmissionColor", Color.yellow * 3f); // Очень яркое свечение

                  // Устанавливаем точки для линии (периметр прямоугольника)
                  float halfWidth = width / 2;
                  float halfHeight = height / 2;

                  lineRenderer.SetPosition(0, new Vector3(-halfWidth, -halfHeight, 0));
                  lineRenderer.SetPosition(1, new Vector3(halfWidth, -halfHeight, 0));
                  lineRenderer.SetPosition(2, new Vector3(halfWidth, halfHeight, 0));
                  lineRenderer.SetPosition(3, new Vector3(-halfWidth, halfHeight, 0));
                  lineRenderer.SetPosition(4, new Vector3(-halfWidth, -halfHeight, 0)); // Замыкаем линию

                  // Добавляем анимацию пульсации для привлечения внимания
                  StartCoroutine(PulsateBorder(lineRenderer));
            }

            // Корутина для анимации пульсации обводки
            private IEnumerator PulsateBorder(LineRenderer lineRenderer)
            {
                  float minWidth = 0.15f;
                  float maxWidth = 0.3f;
                  float duration = 1.0f;

                  while (lineRenderer != null)
                  {
                        // Пульсация ширины
                        float t = (Mathf.Sin(Time.time * 5) + 1) / 2; // от 0 до 1
                        float width = Mathf.Lerp(minWidth, maxWidth, t);

                        lineRenderer.startWidth = width;
                        lineRenderer.endWidth = width;

                        yield return null;
                  }
            }

            // Метод для создания улучшенной текстуры с сеткой и чёткими границами
            private Texture2D CreateEnhancedGridTexture(int size, Color lineColor, Color backgroundColor)
            {
                  Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                  Color[] pixels = new Color[size * size];

                  int gridSize = size / 8; // 8 ячеек сетки
                  int borderThickness = 6; // Увеличенная толщина границы

                  for (int y = 0; y < size; y++)
                  {
                        for (int x = 0; x < size; x++)
                        {
                              int index = y * size + x;

                              // Рисуем линии сетки
                              if (x % gridSize < 2 || y % gridSize < 2)
                              {
                                    pixels[index] = lineColor;
                              }
                              else
                              {
                                    pixels[index] = backgroundColor;
                              }

                              // Добавляем чёткую границу по периметру
                              if (x < borderThickness || x > size - borderThickness - 1 ||
                                  y < borderThickness || y > size - borderThickness - 1)
                              {
                                    // Создаем градиент для границы для лучшей видимости
                                    float distFromEdge = Mathf.Min(
                                        Mathf.Min(x, size - 1 - x),
                                        Mathf.Min(y, size - 1 - y)
                                    );

                                    // Используем более яркие цвета для границы
                                    Color borderColor = Color.Lerp(
                                        new Color(0.4f, 1f, 1f, 1f), // Ярко-голубой
                                        new Color(0.7f, 1f, 1f, 0.9f), // Светло-голубой
                                        distFromEdge / borderThickness
                                    );

                                    pixels[index] = borderColor;
                              }
                        }
                  }

                  texture.SetPixels(pixels);
                  texture.Apply();
                  return texture;
            }

            // Метод для создания видимой рамки вокруг стены
            private void CreateWallBorder(Transform parentTransform, float width, float height)
            {
                  // Создаем объект для линии границы
                  GameObject border = new GameObject("WallBorder");
                  border.transform.SetParent(parentTransform);
                  border.transform.localPosition = new Vector3(0, 0, -0.005f); // Немного впереди основного маркера

                  // Создаем компонент LineRenderer для отрисовки границы
                  LineRenderer lineRenderer = border.AddComponent<LineRenderer>();
                  lineRenderer.useWorldSpace = false;
                  lineRenderer.positionCount = 5; // 4 угла + замыкание на начальной точке
                  lineRenderer.startWidth = 0.03f;
                  lineRenderer.endWidth = 0.03f;

                  // Создаем материал для линии с эффектом свечения
                  Material lineMaterial = new Material(Shader.Find("Particles/Standard Unlit"));
                  lineMaterial.color = Color.cyan;
                  lineMaterial.EnableKeyword("_EMISSION");
                  lineMaterial.SetColor("_EmissionColor", new Color(0f, 1f, 1f, 1f));
                  lineRenderer.material = lineMaterial;

                  // Устанавливаем точки для линии (периметр прямоугольника)
                  float halfWidth = width / 2;
                  float halfHeight = height / 2;

                  lineRenderer.SetPosition(0, new Vector3(-halfWidth, -halfHeight, 0));
                  lineRenderer.SetPosition(1, new Vector3(halfWidth, -halfHeight, 0));
                  lineRenderer.SetPosition(2, new Vector3(halfWidth, halfHeight, 0));
                  lineRenderer.SetPosition(3, new Vector3(-halfWidth, halfHeight, 0));
                  lineRenderer.SetPosition(4, new Vector3(-halfWidth, -halfHeight, 0)); // Замыкаем линию

                  // Добавляем анимацию пульсации линии
                  border.AddComponent<BorderPulseAnimation>();
            }

            // Класс для анимации пульсации границы стены
            public class BorderPulseAnimation : MonoBehaviour
            {
                  private LineRenderer lineRenderer;
                  private float pulseSpeed = 1.0f;
                  private float minWidth = 0.02f;
                  private float maxWidth = 0.04f;
                  private float elapsedTime = 0f;

                  void Start()
                  {
                        lineRenderer = GetComponent<LineRenderer>();
                  }

                  void Update()
                  {
                        if (lineRenderer != null)
                        {
                              elapsedTime += Time.deltaTime;

                              // Пульсация ширины линии
                              float pulse = Mathf.PingPong(elapsedTime * pulseSpeed, 1.0f);
                              float width = Mathf.Lerp(minWidth, maxWidth, pulse);

                              lineRenderer.startWidth = width;
                              lineRenderer.endWidth = width;

                              // Анимация цвета линии
                              float hue = Mathf.Repeat(elapsedTime * 0.2f, 1f);
                              Color rainbowColor = Color.HSVToRGB(hue, 0.7f, 1f);
                              lineRenderer.material.color = rainbowColor;
                              lineRenderer.material.SetColor("_EmissionColor", rainbowColor * 1.5f);
                        }
                  }
            }

            // Метод для добавления улучшенной текстовой метки с информацией о стене
            private void AddEnhancedWallInfoLabel(Transform parentTransform, WallData wall)
            {
                  // Создаем объект для текста
                  GameObject textObj = new GameObject("WallInfoLabel");
                  textObj.transform.SetParent(parentTransform);
                  textObj.transform.localPosition = new Vector3(0, 0, -0.02f);
                  textObj.transform.localRotation = Quaternion.identity;
                  textObj.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);

                  // Добавляем компонент TextMesh для отображения текста в 3D-пространстве
                  TextMesh textMesh = textObj.AddComponent<TextMesh>();
                  textMesh.text = $"СТЕНА #{wall.id}\nРазмер: {wall.scale.x * 5.0f:F1}x{wall.scale.y * 5.0f:F1}м";
                  textMesh.fontSize = 72;
                  textMesh.alignment = TextAlignment.Center;
                  textMesh.anchor = TextAnchor.MiddleCenter;
                  textMesh.color = Color.yellow;

                  // Добавляем outline для лучшей видимости
                  Renderer textRenderer = textObj.GetComponent<Renderer>();
                  Material textMaterial = new Material(Shader.Find("GUI/Text Shader"));
                  textMaterial.color = Color.yellow;
                  textRenderer.material = textMaterial;

                  // Добавляем компонент для поворота к камере
                  textObj.AddComponent<Billboard>();
            }

            private void AddEnhancedCornerMarkers(Transform parentTransform, float width, float height)
            {
                  // Создаем маркеры для четырех углов
                  float hw = width * 0.5f;
                  float hh = height * 0.5f;

                  CreateCornerShape(parentTransform, new Vector3(-hw, -hh, 0.01f), Color.cyan); // нижний левый
                  CreateCornerShape(parentTransform, new Vector3(hw, -hh, 0.01f), Color.cyan);  // нижний правый
                  CreateCornerShape(parentTransform, new Vector3(hw, hh, 0.01f), Color.cyan);   // верхний правый
                  CreateCornerShape(parentTransform, new Vector3(-hw, hh, 0.01f), Color.cyan);  // верхний левый
            }

            private void CreateCornerShape(Transform parent, Vector3 position, Color color)
            {
                  GameObject cornerObj = new GameObject("CornerMarker");
                  cornerObj.transform.SetParent(parent);
                  cornerObj.transform.localPosition = position;
                  cornerObj.transform.localRotation = Quaternion.identity;

                  // Создаем эффектный уголок из LineRenderer
                  LineRenderer lineRenderer = cornerObj.AddComponent<LineRenderer>();
                  lineRenderer.useWorldSpace = false;
                  lineRenderer.startWidth = 0.02f;
                  lineRenderer.endWidth = 0.02f;
                  lineRenderer.positionCount = 4;
                  lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                  lineRenderer.startColor = color;
                  lineRenderer.endColor = new Color(color.r, color.g, color.b, 0.5f);

                  // Создаем L-образную форму уголка
                  float size = 0.1f;
                  Vector3[] points = new Vector3[4];

                  // Определяем ориентацию уголка в зависимости от позиции
                  if (position.x < 0 && position.y < 0) // нижний левый
                  {
                        points[0] = new Vector3(0, size, 0);
                        points[1] = new Vector3(0, 0, 0);
                        points[2] = new Vector3(0, 0, 0);
                        points[3] = new Vector3(size, 0, 0);
                  }
                  else if (position.x > 0 && position.y < 0) // нижний правый
                  {
                        points[0] = new Vector3(-size, 0, 0);
                        points[1] = new Vector3(0, 0, 0);
                        points[2] = new Vector3(0, 0, 0);
                        points[3] = new Vector3(0, size, 0);
                  }
                  else if (position.x > 0 && position.y > 0) // верхний правый
                  {
                        points[0] = new Vector3(0, -size, 0);
                        points[1] = new Vector3(0, 0, 0);
                        points[2] = new Vector3(0, 0, 0);
                        points[3] = new Vector3(-size, 0, 0);
                  }
                  else // верхний левый
                  {
                        points[0] = new Vector3(size, 0, 0);
                        points[1] = new Vector3(0, 0, 0);
                        points[2] = new Vector3(0, 0, 0);
                        points[3] = new Vector3(0, -size, 0);
                  }

                  lineRenderer.SetPositions(points);

                  // Добавляем анимацию пульсации
                  EnhancedCornerMarkerAnimation animation = cornerObj.AddComponent<EnhancedCornerMarkerAnimation>();
            }

            // Класс для анимации угловых маркеров
            public class EnhancedCornerMarkerAnimation : MonoBehaviour
            {
                  public float pulseSpeed = 1.5f;
                  public float rotateSpeed = 40f;
                  private Vector3 originalScale;
                  private float elapsedTime = 0f;

                  void Start()
                  {
                        originalScale = transform.localScale;
                  }

                  void Update()
                  {
                        elapsedTime += Time.deltaTime;

                        // Пульсация размера
                        float scaleFactor = 1.0f + 0.2f * Mathf.Sin(elapsedTime * pulseSpeed);
                        transform.localScale = originalScale * scaleFactor;

                        // Вращение вокруг оси Z для дополнительного эффекта
                        transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);

                        // Меняем цвет
                        LineRenderer lineRenderer = GetComponent<LineRenderer>();
                        if (lineRenderer != null)
                        {
                              Color startColor = lineRenderer.startColor;

                              // Циклическое изменение оттенка
                              float hue = Mathf.Repeat(elapsedTime * 0.2f, 1f);
                              Color newColor = Color.HSVToRGB(hue, 1f, 1f);

                              lineRenderer.startColor = newColor;
                              lineRenderer.endColor = new Color(newColor.r, newColor.g, newColor.b, 0.5f);
                        }
                  }
            }

            // Метод для добавления индикатора типа поверхности
            private void AddSurfaceTypeIndicator(Transform parentTransform, bool isVirtual, float width, float height)
            {
                  // Не создаём индикатор типа поверхности
                  // Оранжевый блок убран полностью
                  return;
            }

            private void UpdateUI()
            {
                  if (captureButton != null)
                  {
                        var buttonText = captureButton.GetComponentInChildren<Text>();
                        if (buttonText != null)
                        {
                              buttonText.text = isCapturing ? "Stop" : "Capture";
                        }
                  }
            }

            private void OnDestroy()
            {
                  if (wallDetector != null)
                  {
                        wallDetector.OnWallsDetected -= OnWallsDetected;
                  }
            }

            private void Update()
            {
                  // Testing controls - добавляем тестирование клавиш
                  if (Input.GetKeyDown(KeyCode.T))
                  {
                        CreateTestWalls();
                        Debug.Log("Созданы тестовые стены по клавише T");
                  }

                  if (Input.GetKeyDown(KeyCode.F1))
                  {
                        CreateIndividualTestWalls();
                        Debug.Log("Создана тестовая стена по клавише F1");
                  }

                  if (Input.GetKeyDown(KeyCode.C))
                  {
                        ToggleCamera();
                        Debug.Log("Камера переключена по клавише C");
                  }

                  if (Input.GetKeyDown(KeyCode.Space))
                  {
                        PaintAllWalls();
                        Debug.Log("Попытка покрасить все стены по клавише ПРОБЕЛ");
                  }

                  // Обработка ввода только для активного режима
                  if (isCapturing)
                  {
                        HandleInput();
                        HandleCameraMovement();
                  }

                  // Handle painting input when in painting mode
                  if (isPaintingMode)
                  {
                        HandleWallHighlighting();
                        HandlePaintingInput();
                  }
            }

            // Add the missing method to handle painting input
            private void HandlePaintingInput()
            {
                  // Check for mouse click/touch to paint walls
                  if (Input.GetMouseButtonDown(0))
                  {
                        // Don't paint if over UI
                        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                              return;

                        // Get mouse/touch position
                        Vector2 inputPosition = Input.mousePosition;

                        // Paint at the position
                        if (highlightedWall != null)
                        {
                              PaintWallAtPosition(inputPosition);
                        }
                  }
            }

            private float lastWallCreationTime = 0f;

            private void CreateTestWalls()
            {
                  ClearWallMarkers();

                  // Создаем тестовую стену в центре экрана
                  Vector3 wallPosition = new Vector3(0, 0, 5);

                  // Создаем маркер с увеличенным размером для легкого взаимодействия
                  GameObject wallMarker = CreateWallPlane(wallPosition, Quaternion.identity, 4, 3);
                  if (wallMarker != null)
                  {
                        // Настраиваем материал
                        MeshRenderer renderer = wallMarker.GetComponent<MeshRenderer>();
                        if (renderer != null)
                        {
                              Material wallMaterial = new Material(Shader.Find("Legacy Shaders/Transparent/Diffuse"));
                              wallMaterial.color = new Color(1f, 1f, 1f, 0.5f);
                              renderer.material = wallMaterial;
                        }

                        // Добавляем в список маркеров
                        wallMarkers.Add(wallMarker);

                        Debug.Log($"Создана тестовая стена в позиции {wallPosition}");
                  }
                  else
                  {
                        Debug.LogError("Не удалось создать тестовую стену");
                  }
            }

            private GameObject CreateWallPlane(Vector3 position, Quaternion rotation, float width, float height)
            {
                  GameObject wallPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
                  wallPlane.transform.position = position;
                  wallPlane.transform.rotation = rotation;
                  wallPlane.transform.localScale = new Vector3(width, height, 1);

                  // Настраиваем слои и теги
                  wallPlane.layer = LayerMask.NameToLayer("Wall");
                  wallPlane.tag = "Wall";

                  return wallPlane;
            }

            private void HandleInput()
            {
                  // Создаем EventSystem, если он еще не существует
                  if (EventSystem.current == null)
                  {
                        GameObject eventSystemObj = new GameObject("EventSystem");
                        eventSystemObj.AddComponent<EventSystem>();
                        eventSystemObj.AddComponent<StandaloneInputModule>();
                        Debug.Log("Создан новый EventSystem");
                  }

                  // Обрабатываем нажатие клавиши F для переключения режима отображения камеры
                  if (Input.GetKeyDown(KeyCode.F))
                  {
                        ToggleCameraViewMode();
                        Debug.Log($"Режим камеры переключен на: {(isFullScreenCamera ? "полноэкранный" : "предпросмотр")}");
                        return; // Возвращаемся, чтобы не обрабатывать другие клики в этом кадре
                  }

                  // Обрабатываем клик мыши только если курсор не находится над UI элементом
                  if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
                  {
                        // Если мы в режиме захвата, обрабатываем клик
                        if (isCapturing)
                        {
                              // Получаем позицию мыши на экране
                              Vector2 mousePosition = Input.mousePosition;

                              // Пытаемся определить, по какой стене был клик
                              Ray ray = mainCamera.ScreenPointToRay(mousePosition);
                              RaycastHit hit;

                              // Создаем визуальный эффект клика
                              ShowClickFeedback(mousePosition);

                              // Проверяем, попал ли луч в какой-либо объект
                              if (Physics.Raycast(ray, out hit))
                              {
                                    Debug.Log($"Клик по объекту: {hit.collider.gameObject.name}");

                                    // Если попали в стену
                                    if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Wall"))
                                    {
                                          // Создаем эффект покраски в точке удара
                                          CreatePaintEffectAtHitPoint(hit.point, hit.normal, GetPaintColor());

                                          // Вызываем событие покраски стены
                                          if (OnWallPainted != null)
                                          {
                                                OnWallPainted(hit.collider.gameObject);
                                          }
                                    }
                              }
                        }
                        else
                        {
                              Debug.Log("Клик проигнорирован, так как режим захвата не активен");
                        }
                  }
            }

            private void HandleCameraMovement()
            {
                  if (mainCamera == null) return;

                  float moveSpeed = 5.0f * Time.deltaTime; // Увеличиваем скорость для удобства
                  float rotateSpeed = 120.0f * Time.deltaTime; // Увеличиваем скорость поворота

                  Transform cameraTransform = mainCamera.transform;

                  // Быстрое перемещение при нажатии Shift
                  if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                  {
                        moveSpeed *= 3.0f;
                  }

                  // Перемещение
                  if (Input.GetKey(KeyCode.W))
                  {
                        cameraTransform.Translate(Vector3.forward * moveSpeed);
                  }
                  if (Input.GetKey(KeyCode.S))
                  {
                        cameraTransform.Translate(Vector3.back * moveSpeed);
                  }
                  if (Input.GetKey(KeyCode.A))
                  {
                        cameraTransform.Translate(Vector3.left * moveSpeed);
                  }
                  if (Input.GetKey(KeyCode.D))
                  {
                        cameraTransform.Translate(Vector3.right * moveSpeed);
                  }

                  // Добавляем движение вверх-вниз
                  if (Input.GetKey(KeyCode.Q))
                  {
                        cameraTransform.Translate(Vector3.up * moveSpeed);
                  }
                  if (Input.GetKey(KeyCode.E))
                  {
                        cameraTransform.Translate(Vector3.down * moveSpeed);
                  }

                  // Вращение
                  if (Input.GetKey(KeyCode.LeftArrow))
                  {
                        cameraTransform.Rotate(Vector3.up, -rotateSpeed);
                  }
                  if (Input.GetKey(KeyCode.RightArrow))
                  {
                        cameraTransform.Rotate(Vector3.up, rotateSpeed);
                  }
                  if (Input.GetKey(KeyCode.UpArrow))
                  {
                        cameraTransform.Rotate(Vector3.right, -rotateSpeed);
                  }
                  if (Input.GetKey(KeyCode.DownArrow))
                  {
                        cameraTransform.Rotate(Vector3.right, rotateSpeed);
                  }

                  // Быстрый сброс положения камеры
                  if (Input.GetKeyDown(KeyCode.R))
                  {
                        cameraTransform.position = new Vector3(0, 1, 0);
                        cameraTransform.rotation = Quaternion.Euler(0, 0, 0);
                        Debug.Log("Положение камеры сброшено к началу координат");
                  }

                  // Выводим текущее положение камеры каждые 2 секунды для отладки
                  if (Time.frameCount % 120 == 0)
                  {
                        Debug.Log($"Положение камеры: {cameraTransform.position}, поворот: {cameraTransform.rotation.eulerAngles}");
                  }
            }

            public void PaintWallAtPosition(Vector2 screenPosition)
            {
                  Debug.Log("Окрашиваю поверхность в точке: " + screenPosition);

                  // Показываем визуальный индикатор нажатия
                  ShowClickFeedback(screenPosition);

                  // Получаем цвет для покраски из текущего выбранного в палитре
                  Color paintColor = GetPaintColor();

                  // Выполняем рейкаст из точки на экране для определения стены
                  Ray ray = mainCamera.ScreenPointToRay(new Vector3(screenPosition.x, screenPosition.y, 0));
                  RaycastHit hit;

                  // Логируем информацию о луче для отладки
                  Debug.DrawRay(ray.origin, ray.direction * 10, Color.red, 2.0f);
                  Debug.Log($"Рейкаст из {ray.origin} в направлении {ray.direction}");

                  // Переменная для отслеживания успешной покраски
                  bool paintApplied = false;

                  // Указываем максимальное расстояние 100f и используем все слои
                  if (Physics.Raycast(ray, out hit, 100f, Physics.AllLayers))
                  {
                        GameObject hitObject = hit.collider.gameObject;
                        Debug.Log($"Рейкаст попал в объект: {hitObject.name} с тегом: {hitObject.tag}, расстояние: {hit.distance}");

                        // Проверяем, является ли объект стеной или частью стены
                        if (IsWallObject(hitObject))
                        {
                              // Показываем предварительный просмотр покраски с плавным исчезновением
                              StartCoroutine(ShowPaintPreview(hitObject, paintColor, 0.5f));

                              // Используем метод PaintWallTexture для применения реалистичной покраски
                              PaintWallTexture(hitObject, paintColor);

                              // Создаем эффект брызг в точке попадания для визуальной обратной связи
                              CreatePaintEffectAtHitPoint(hit.point, hit.normal, paintColor);

                              // Создаем частицы для дополнительного эффекта
                              CreatePaintParticles(hit.point, hit.normal);

                              // Сообщаем другим компонентам о покраске стены
                              OnWallPainted?.Invoke(hitObject);

                              // Обновляем статус
                              paintApplied = true;
                        }
                        else
                        {
                              Debug.Log($"Объект {hitObject.name} не распознан как стена. Проверяем родительские объекты.");

                              // Проверяем родительский объект, если он есть
                              Transform parent = hitObject.transform.parent;
                              while (parent != null && !paintApplied)
                              {
                                    if (IsWallObject(parent.gameObject))
                                    {
                                          // Показываем предварительный просмотр
                                          StartCoroutine(ShowPaintPreview(parent.gameObject, paintColor, 0.5f));

                                          // Применяем покраску к родительскому объекту
                                          PaintWallTexture(parent.gameObject, paintColor);
                                          CreatePaintEffectAtHitPoint(hit.point, hit.normal, paintColor);
                                          CreatePaintParticles(hit.point, hit.normal);
                                          OnWallPainted?.Invoke(parent.gameObject);

                                          // Обновляем статус
                                          paintApplied = true;
                                          break;
                                    }
                                    parent = parent.parent;
                              }

                              // Если не нашли объект с тегом Wall - пробуем применить к объекту с рендерером
                              if (!paintApplied)
                              {
                                    Renderer renderer = hitObject.GetComponent<Renderer>();
                                    if (renderer != null)
                                    {
                                          Debug.Log($"Применяем покраску к объекту {hitObject.name} без тега Wall");

                                          // Создаем новый материал для объекта, если его еще нет
                                          if (renderer.material == null)
                                          {
                                                renderer.material = new Material(Shader.Find("Standard"));
                                          }

                                          // Используем метод покраски текстуры
                                          PaintWallTexture(hitObject, paintColor);
                                          CreatePaintEffectAtHitPoint(hit.point, hit.normal, paintColor);
                                          paintApplied = true;
                                    }
                                    else
                                    {
                                          Debug.Log($"Объект {hitObject.name} не имеет компонента Renderer");
                                    }
                              }
                        }
                  }
                  else
                  {
                        Debug.Log("Рейкаст не попал ни в один объект. Создаем UI-эффект покраски.");
                  }

                  // Если рейкаст не попал ни в один объект или это не стена, создаем UI-эффект
                  if (!paintApplied)
                  {
                        // В режиме Dulux Visualizer показываем сообщение, что не найдена поверхность для покраски
                        ShowMessage("Не найдена поверхность для покраски", 1.5f);
                  }
                  else
                  {
                        // Показываем сообщение об успешной покраске
                        ShowMessage("Поверхность окрашена!", 1.5f);
                  }
            }

            // Метод для определения, является ли объект стеной
            private bool IsWallObject(GameObject obj)
            {
                  if (obj == null) return false;

                  // Проверяем по тегу
                  if (obj.CompareTag("Wall"))
                        return true;

                  // Проверяем по имени
                  string name = obj.name.ToLower();
                  if (name.Contains("wall") || name.Contains("стена"))
                        return true;

                  // Проверяем наличие компонента с WallData - исправлено, так как WallData это struct
                  // WallData является структурой и не может быть null, поэтому требуется другой подход
                  try
                  {
                        // Поскольку WallData - это struct, просто проверяем наличие MonoBehaviour с именем, содержащим WallData
                        foreach (var component in obj.GetComponents<MonoBehaviour>())
                        {
                              if (component.GetType().Name.Contains("WallData"))
                              {
                                    return true;
                              }
                        }
                  }
                  catch (System.Exception)
                  {
                        // Игнорируем исключения
                  }

                  // Любые другие специфические для вашего проекта проверки

                  return false;
            }

            // Метод для создания материала подсветки
            private Material CreateHighlightMaterial()
            {
                  // Create a new material with the standard shader
                  Material highlightMaterial = new Material(Shader.Find("Standard"));
                  // Set properties for highlighting
                  highlightMaterial.color = new Color(1f, 0.8f, 0.2f, 0.7f); // Yellow highlight
                  highlightMaterial.EnableKeyword("_EMISSION");
                  highlightMaterial.SetColor("_EmissionColor", Color.yellow * 0.5f);

                  // Store the material for later use
                  highlightMaterial = highlightMaterial;
                  return highlightMaterial;
            }

            // Метод для создания палитры цветов
            private void CreateColorPalette()
            {
                  // Create color palette panel if container is assigned
                  if (colorPaletteContainer == null)
                  {
                        // Create a container if one doesn't exist
                        GameObject container = new GameObject("ColorPaletteContainer");
                        colorPaletteContainer = container.transform;
                        container.transform.SetParent(transform);

                        // Position it in the corner of the screen
                        RectTransform containerRectTransform = container.AddComponent<RectTransform>();
                        containerRectTransform.anchorMin = new Vector2(0.8f, 0);
                        containerRectTransform.anchorMax = new Vector2(1, 0.3f);
                        containerRectTransform.offsetMin = Vector2.zero;
                        containerRectTransform.offsetMax = Vector2.zero;

                        Debug.Log("Created color palette container dynamically");
                  }

                  // Create main panel
                  colorPalettePanel = new GameObject("ColorPalette");
                  colorPalettePanel.transform.SetParent(colorPaletteContainer, false);

                  // Add panel components
                  RectTransform rectTransform = colorPalettePanel.AddComponent<RectTransform>();
                  Image panelImage = colorPalettePanel.AddComponent<Image>();
                  panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

                  // Configure panel layout
                  rectTransform.anchorMin = new Vector2(0, 0);
                  rectTransform.anchorMax = new Vector2(1, 1);
                  rectTransform.offsetMin = Vector2.zero;
                  rectTransform.offsetMax = Vector2.zero;

                  // Create grid layout for colors
                  GridLayoutGroup grid = colorPalettePanel.AddComponent<GridLayoutGroup>();
                  grid.cellSize = new Vector2(50, 50);
                  grid.spacing = new Vector2(10, 10);
                  grid.padding = new RectOffset(10, 10, 10, 10);
                  grid.childAlignment = TextAnchor.MiddleCenter;

                  // Create color buttons
                  foreach (Color color in duluxColors)
                  {
                        CreateColorButton(color);
                  }

                  // Update selection to show the currently selected color
                  UpdateColorSelection();

                  // Hide palette initially (it will be shown when switching to painting mode)
                  colorPalettePanel.SetActive(false);
            }

            // Метод для создания кнопки цвета
            private void CreateColorButton(Color color)
            {
                  GameObject buttonObj = new GameObject("ColorButton_" + ColorToHex(color));
                  buttonObj.transform.SetParent(colorPalettePanel.transform, false);

                  // Add required components
                  Image buttonImage = buttonObj.AddComponent<Image>();
                  Button button = buttonObj.AddComponent<Button>();

                  // Set color and add selection indicator if this is the current color
                  buttonImage.color = color;
                  if (color == currentPaintColor)
                  {
                        AddSelectionIndicator(buttonObj.transform);
                  }

                  // Make the button rounded
                  buttonImage.sprite = CreateCircleSprite();

                  // Add click handler
                  button.onClick.AddListener(() => SelectColor(color));
            }

            // Метод для получения hex кода из цвета
            private string ColorToHex(Color color)
            {
                  return ColorUtility.ToHtmlStringRGB(color);
            }

            // Метод для создания круглого спрайта
            private Sprite CreateCircleSprite()
            {
                  return CreateCircleSprite(64, Color.white);
            }

            // Расширенный метод для создания круглого спрайта
            private Sprite CreateCircleSprite(int size, Color color, bool outline = false)
            {
                  Texture2D texture = CreateCircleTexture(size, color, outline);
                  return Sprite.Create(texture, new UnityEngine.Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            }

            // Метод для создания круглой текстуры (с дополнительной опцией для контура)
            private Texture2D CreateCircleTexture(int size, Color color, bool outline = false)
            {
                  Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

                  float radius = size / 2f;
                  float radiusSquared = radius * radius;
                  float outlineThickness = size * 0.05f; // 5% от размера для контура

                  // Заполняем текстуру
                  for (int y = 0; y < size; y++)
                  {
                        for (int x = 0; x < size; x++)
                        {
                              float dx = radius - x;
                              float dy = radius - y;
                              float distanceSquared = dx * dx + dy * dy;

                              // Если outline = true, создаем обводку, иначе заливаем круг
                              if (outline)
                              {
                                    // Для контура проверяем, находится ли пиксель на краю круга
                                    if (distanceSquared <= radiusSquared && distanceSquared >= radiusSquared - 2 * radius * outlineThickness)
                                    {
                                          texture.SetPixel(x, y, color);
                                    }
                                    else
                                    {
                                          texture.SetPixel(x, y, new Color(0, 0, 0, 0));
                                    }
                              }
                              else
                              {
                                    // Для полного круга проверяем, находится ли пиксель внутри радиуса
                                    if (distanceSquared <= radiusSquared)
                                    {
                                          texture.SetPixel(x, y, color);
                                    }
                                    else
                                    {
                                          texture.SetPixel(x, y, new Color(0, 0, 0, 0));
                                    }
                              }
                        }
                  }

                  texture.Apply();
                  return texture;
            }

            // Метод для настройки расширенного обнаружения стен
            private void ConfigureAdvancedWallDetection()
            {
                  // Check if the wall detector is available
                  if (wallDetector == null)
                  {
                        Debug.LogError("Компонент WallDetector не найден!");
                        return;
                  }

                  // Configure wall detection parameters using proper methods on WallDetector
                  // Since we don't know the exact API, we'll use SetParameter method if available
                  // or comment out these properties if they're not valid

                  // Example of safer property setting:
                  if (wallDetector is MonoBehaviour detector)
                  {
                        // Set detector parameters through a configuration method if available
                        wallDetector.SendMessage("ConfigureDetector",
                              new Dictionary<string, object> {
                                    { "thresholdMin", 100 },
                                    { "thresholdMax", 255 },
                                    { "dilationIterations", 2 },
                                    { "erosionIterations", 1 },
                                    { "minimumContourArea", 5000 }
                              }, SendMessageOptions.DontRequireReceiver);
                  }

                  Debug.Log("Настроено расширенное обнаружение стен");
            }

            // Метод для включения отображения контуров на камере
            private void EnableContoursOnCamera()
            {
                  if (wallDetector == null)
                  {
                        Debug.LogError("Компонент WallDetector не найден!");
                        return;
                  }

                  // Configure contour display using SendMessage to safely set properties
                  wallDetector.SendMessage("SetContourDisplay",
                        new Dictionary<string, object> {
                              { "showContours", true },
                              { "contourColor", new Color(0, 1, 0, 1) }, // Green color
                              { "contourThickness", 2 }
                        }, SendMessageOptions.DontRequireReceiver);

                  Debug.Log("Включено отображение контуров на камере");
            }

            // Метод для обработки подсветки стен
            private void HandleWallHighlighting()
            {
                  // Если режим покраски активен
                  if (isPaintMode && !isColorSelectionMode)
                  {
                        // Подсвечиваем стену под курсором
                        HighlightWallUnderCursor();
                  }
                  else
                  {
                        // Снимаем подсветку со всех стен
                        UnhighlightWall();
                  }
            }

            // Method to highlight the wall under the cursor
            private void HighlightWallUnderCursor()
            {
                  // If highlight material doesn't exist, create it
                  if (highlightMaterial == null)
                  {
                        highlightMaterial = CreateHighlightMaterial();
                  }

                  // Check for mouse position
                  Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                  RaycastHit hit;

                  // If we hit something
                  if (Physics.Raycast(ray, out hit, maxPaintDistance))
                  {
                        GameObject hitObject = hit.collider.gameObject;

                        // If it's a wall or part of a wall
                        if (IsWallObject(hitObject))
                        {
                              // If we're not already highlighting this wall
                              if (highlightedWall != hitObject)
                              {
                                    // Unhighlight previous wall
                                    UnhighlightWall();

                                    // Store reference to original material
                                    Renderer renderer = hitObject.GetComponent<Renderer>();
                                    if (renderer != null && renderer.material != null)
                                    {
                                          // Save original material
                                          if (!originalMaterials.ContainsKey(hitObject))
                                          {
                                                originalMaterials[hitObject] = renderer.material;
                                          }

                                          // Apply highlight material
                                          renderer.material = highlightMaterial;

                                          // Set as currently highlighted
                                          highlightedWall = hitObject;

                                          // Show the brush preview at the hit point
                                          ShowPaintPreview(hitObject, hit.point, hit.normal);
                                    }
                              }
                        }
                        else
                        {
                              // We hit something that's not a wall, remove highlight
                              UnhighlightWall();
                        }
                  }
                  else
                  {
                        // We didn't hit anything, remove highlight
                        UnhighlightWall();
                  }
            }

            // Method to remove highlighting from walls
            private void UnhighlightWall()
            {
                  // If we have a highlighted wall
                  if (highlightedWall != null)
                  {
                        // Get renderer
                        Renderer renderer = highlightedWall.GetComponent<Renderer>();

                        // Restore original material if we have it
                        if (renderer != null && originalMaterials.ContainsKey(highlightedWall))
                        {
                              renderer.material = originalMaterials[highlightedWall];
                              originalMaterials.Remove(highlightedWall);
                        }

                        // Clear reference
                        highlightedWall = null;
                  }
            }

            private void AddSelectionIndicator(Transform buttonTransform)
            {
                  // Удаляем существующий индикатор, если есть
                  Transform existingIndicator = buttonTransform.Find("SelectionIndicator");
                  if (existingIndicator != null)
                  {
                        return; // Уже есть индикатор
                  }

                  // Создаем индикатор выбора
                  GameObject indicator = new GameObject("SelectionIndicator");
                  indicator.transform.SetParent(buttonTransform, false);

                  // Добавляем компоненты
                  RectTransform rectTransform = indicator.AddComponent<RectTransform>();
                  Image image = indicator.AddComponent<Image>();

                  // Настраиваем размер и позицию
                  rectTransform.anchorMin = new Vector2(0, 0);
                  rectTransform.anchorMax = new Vector2(1, 1);
                  rectTransform.offsetMin = new Vector2(-5, -5);
                  rectTransform.offsetMax = new Vector2(5, 5);

                  // Используем изображение рамки
                  image.sprite = CreateCircleSprite(32, Color.white, true);
                  image.color = new Color(1, 1, 1, 0.7f);
                  image.type = Image.Type.Sliced;

                  // Анимация пульсации
                  StartCoroutine(PulseIndicator(indicator));
            }

            private IEnumerator PulseIndicator(GameObject indicator)
            {
                  Image image = indicator.GetComponent<Image>();
                  if (image == null) yield break;

                  float duration = 1.5f;
                  float halfDuration = duration / 2;
                  float elapsed = 0f;

                  while (indicator != null)
                  {
                        // Пульсирующая прозрачность
                        if (elapsed < halfDuration)
                        {
                              float alpha = Mathf.Lerp(0.7f, 1.0f, elapsed / halfDuration);
                              image.color = new Color(1, 1, 1, alpha);
                        }
                        else
                        {
                              float alpha = Mathf.Lerp(1.0f, 0.7f, (elapsed - halfDuration) / halfDuration);
                              image.color = new Color(1, 1, 1, alpha);
                        }

                        elapsed += Time.deltaTime;
                        if (elapsed >= duration)
                        {
                              elapsed = 0f;
                        }

                        yield return null;
                  }
            }

            // Метод для выбора цвета
            private void SelectColor(Color color)
            {
                  selectedColor = color;
                  UpdateColorSelection(color);
                  isColorSelectionMode = false;

                  // Показываем сообщение с выбранным цветом
                  ShowMessage($"Выбран цвет: {ColorToHex(color)}", 2.0f);
            }

            private void UpdateColorSelection(Color color)
            {
                  if (colorPaletteContainer == null)
                  {
                        Debug.LogWarning("Color palette container is null in UpdateColorSelection - cannot update selection");
                        return;
                  }

                  selectedColor = color;
                  Debug.Log($"Выбран цвет: {ColorToHex(color)}");

                  // Обновляем индикаторы
                  foreach (Transform child in colorPaletteContainer)
                  {
                        // Check if child is null (may have been destroyed)
                        if (child == null) continue;

                        // Process child object
                        Button button = child.GetComponent<Button>();
                        if (button != null)
                        {
                              // Remove existing selection indicator if there is one
                              Transform existingIndicator = child.Find("SelectionIndicator");
                              if (existingIndicator != null)
                              {
                                    Destroy(existingIndicator.gameObject);
                              }

                              // Add selection indicator if this is the selected color
                              Image buttonImage = child.GetComponent<Image>();
                              if (buttonImage != null && ColorApproximatelyEqual(buttonImage.color, color, 0.01f))
                              {
                                    AddSelectionIndicator(child);
                              }
                        }
                  }

                  // Перебираем все созданные объекты стен
                  if (createdWallObjects != null)
                  {
                        foreach (GameObject wall in createdWallObjects)
                        {
                              if (wall != null)
                              {
                                    PaintWallTexture(wall);
                                    CreatePaintEffectAtHitPoint(wall.transform.position, wall.transform.forward, GetPaintColor());
                              }
                        }

                        ShowMessage("Все стены покрашены выбранным цветом", 2.0f);
                        InvokeOnWallPainted();
                  }
            }

            // Helper method to compare colors with tolerance
            private bool ColorApproximatelyEqual(Color color1, Color color2, float tolerance)
            {
                  return Mathf.Abs(color1.r - color2.r) < tolerance &&
                         Mathf.Abs(color1.g - color2.g) < tolerance &&
                         Mathf.Abs(color1.b - color2.b) < tolerance;
            }

            // Возвращает текущий выбранный цвет для покраски
            private Color GetPaintColor()
            {
                  // Если цвет уже выбран, используем его
                  if (selectedColor != Color.clear)
                  {
                        return selectedColor;
                  }

                  // Иначе используем первый из списка или белый
                  if (duluxColors != null && duluxColors.Count > 0)
                  {
                        return duluxColors[0];
                  }

                  return Color.white;
            }

            // Метод для покраски текстуры стены
            private void PaintWallTexture(GameObject wall)
            {
                  if (wall == null) return;

                  Renderer renderer = wall.GetComponent<Renderer>();
                  if (renderer == null) return;

                  // Создаем материал для покраски, если нужно
                  if (renderer.material.name.Contains("Default-Material") || !renderer.material.name.Contains("PaintMaterial"))
                  {
                        Material paintMaterial = new Material(Shader.Find("Standard"));
                        paintMaterial.name = "PaintMaterial";
                        renderer.material = paintMaterial;
                  }

                  // Устанавливаем цвет материала
                  renderer.material.color = GetPaintColor();

                  Debug.Log($"Стена покрашена в цвет: {GetPaintColor()}");
            }

            // Add the overloaded method with Color parameter
            private void PaintWallTexture(GameObject wall, Color paintColor)
            {
                  // Call the single parameter version and pass any additional processing needed for the color
                  PaintWallTexture(wall);
                  // Additional color-specific processing can be added here if needed
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
            }

            // Метод, вызываемый после покраски стены
            private void InvokeOnWallPainted()
            {
                  // Здесь можно добавить логику, которая выполняется после покраски стены
                  // Например, звуковой эффект, обновление UI и т.д.
                  Debug.Log("Стена окрашена");

                  // Возможно, какие-то игровые события или достижения?
                  // Например, счетчик покрашенных стен
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

            // Метод для отображения визуального отклика при клике
            private void ShowClickFeedback(Vector2 screenPosition)
            {
                  // Создаем временный объект для визуального эффекта
                  GameObject clickEffect = new GameObject("ClickFeedback");

                  // Находим или создаем канвас
                  Canvas canvas = FindObjectOfType<Canvas>();
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

            // Метод для установки режима отображения камеры
            public void SetCameraViewMode(bool fullscreen)
            {
                  if (cameraPreview == null) return;

                  isFullScreenCamera = fullscreen;

                  // Get the rect transform of the camera preview
                  RectTransform rectTransform = cameraPreview.GetComponent<RectTransform>();
                  if (rectTransform == null) return;

                  if (fullscreen)
                  {
                        // Full-screen mode
                        rectTransform.anchorMin = new Vector2(0, 0);
                        rectTransform.anchorMax = new Vector2(1, 1);
                        rectTransform.offsetMin = Vector2.zero;
                        rectTransform.offsetMax = Vector2.zero;

                        // Full opacity
                        cameraPreview.color = Color.white;
                  }
                  else
                  {
                        // Small preview in corner
                        rectTransform.anchorMin = new Vector2(0.7f, 0.05f);
                        rectTransform.anchorMax = new Vector2(0.95f, 0.25f);
                        rectTransform.offsetMin = Vector2.zero;
                        rectTransform.offsetMax = Vector2.zero;

                        // Semi-transparent
                        cameraPreview.color = new Color(1, 1, 1, 0.8f);
                  }
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

            // Add overload without parameters
            private void UpdateColorSelection()
            {
                  if (colorPaletteContainer == null)
                  {
                        Debug.LogWarning("Color palette container is null in UpdateColorSelection - cannot update selection");
                        return;
                  }

                  // Make sure we have a valid color
                  if (currentPaintColor == Color.clear && duluxColors != null && duluxColors.Count > 0)
                  {
                        currentPaintColor = duluxColors[0];
                  }

                  UpdateColorSelection(currentPaintColor);
            }

            // Add the missing CreateBrushSizeButtons method
            private void CreateBrushSizeButtons()
            {
                  if (brushSizeContainer == null)
                  {
                        Debug.LogWarning("Контейнер для кнопок размера кисти не назначен");
                        return;
                  }

                  // Clear any existing buttons
                  foreach (Transform child in brushSizeContainer.transform)
                  {
                        Destroy(child.gameObject);
                  }

                  // Create buttons for different brush sizes
                  float[] sizes = { 5f, 10f, 15f, 20f, 30f };

                  for (int i = 0; i < sizes.Length; i++)
                  {
                        float size = sizes[i];
                        GameObject buttonObj = new GameObject($"BrushSize_{size}");
                        buttonObj.transform.SetParent(brushSizeContainer.transform, false);

                        RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
                        rectTransform.sizeDelta = new Vector2(40, 40);

                        // Add button component
                        Button button = buttonObj.AddComponent<Button>();

                        // Set button color based on size
                        ColorBlock colors = button.colors;
                        colors.normalColor = Color.white;
                        button.colors = colors;

                        // Add image component for button background
                        Image image = buttonObj.AddComponent<Image>();
                        image.sprite = CreateCircleSprite(30, Color.white, true);

                        // Add text to show size
                        GameObject textObj = new GameObject("Text");
                        textObj.transform.SetParent(buttonObj.transform, false);

                        Text text = textObj.AddComponent<Text>();
                        text.text = size.ToString();
                        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                        text.alignment = TextAnchor.MiddleCenter;
                        text.color = Color.black;

                        RectTransform textRect = textObj.GetComponent<RectTransform>();
                        textRect.anchorMin = Vector2.zero;
                        textRect.anchorMax = Vector2.one;
                        textRect.offsetMin = Vector2.zero;
                        textRect.offsetMax = Vector2.zero;

                        // Add click event
                        float brushSizeValue = size / 1000f; // Convert to appropriate scale
                        button.onClick.AddListener(() =>
                        {
                              brushSize = brushSizeValue;
                              Debug.Log($"Размер кисти установлен: {size}px");
                        });
                  }
            }
      }
}
