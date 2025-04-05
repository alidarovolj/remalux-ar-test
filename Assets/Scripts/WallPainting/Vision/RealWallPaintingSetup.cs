using UnityEngine;
using UnityEngine.UI;

namespace Remalux.WallPainting.Vision
{
      public static class RealWallPaintingSetup
      {
            public static void CreateRealWallPaintingScene()
            {
                  Debug.Log("Creating real wall painting scene...");

                  // Create necessary tags and layers
                  CreateWallTag();
                  CreateWallLayer();

                  // Create game controller
                  GameObject controllerObject = new GameObject("RealWallPaintingController");
                  RealWallPaintingController controller = controllerObject.AddComponent<RealWallPaintingController>();

                  // Create camera
                  GameObject cameraObject = new GameObject("MainCamera");
                  Camera camera = cameraObject.AddComponent<Camera>();
                  camera.tag = "MainCamera";
                  cameraObject.AddComponent<AudioListener>();

                  // Create wall detector
                  GameObject detectorObject = new GameObject("WallDetector");
                  WallDetector wallDetector = detectorObject.AddComponent<WallDetector>();

                  // Create texture manager
                  GameObject textureManagerObject = new GameObject("TextureManager");
                  TextureManager textureManager = textureManagerObject.AddComponent<TextureManager>();

                  // Create room manager
                  GameObject roomManagerObject = new GameObject("RoomManager");
                  RoomManager roomManager = roomManagerObject.AddComponent<RoomManager>();

                  // Create improved lighting
                  CreateLighting();

                  // Create test room with walls
                  CreateTestRoom();

                  // Создаем градиентный фон вместо розового
                  GameObject bgCanvasObject = CreateBackgroundCanvas();

                  // Create UI background for camera preview
                  GameObject bgCanvas = new GameObject("Camera Preview Background Canvas");
                  Canvas cameraCanvas = bgCanvas.AddComponent<Canvas>();
                  cameraCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                  cameraCanvas.sortingOrder = 0;
                  CanvasScaler canvasScaler = bgCanvas.AddComponent<CanvasScaler>();
                  canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                  canvasScaler.referenceResolution = new Vector2(1920, 1080);

                  // Create a RawImage to display the camera feed
                  GameObject previewObj = new GameObject("Camera Preview");
                  previewObj.transform.SetParent(bgCanvas.transform, false);
                  RawImage preview = previewObj.AddComponent<RawImage>();
                  preview.color = Color.white;

                  // Setup RectTransform for the preview
                  RectTransform rectTransform = preview.GetComponent<RectTransform>();
                  rectTransform.anchorMin = new Vector2(0.7f, 0.05f);
                  rectTransform.anchorMax = new Vector2(0.95f, 0.25f);
                  rectTransform.offsetMin = Vector2.zero;
                  rectTransform.offsetMax = Vector2.zero;

                  // Create UI canvas for buttons
                  GameObject uiCanvasObject = new GameObject("UI Canvas");
                  Canvas uiCanvas = uiCanvasObject.AddComponent<Canvas>();
                  uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                  uiCanvas.sortingOrder = 1; // Ensure it's in front of the camera preview
                  var uiScaler = uiCanvasObject.AddComponent<CanvasScaler>();
                  uiScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                  uiScaler.referenceResolution = new Vector2(1920, 1080);
                  uiCanvasObject.AddComponent<GraphicRaycaster>();

                  // Удаляем создание кнопок Capture и Reset
                  // var captureButton = CreateButton("Capture Button", uiCanvasObject.transform, new Vector2(0.5f, 0.1f), "Capture");
                  // var resetButton = CreateButton("Reset Button", uiCanvasObject.transform, new Vector2(0.8f, 0.1f), "Reset");

                  // Setup references directly
                  controller.mainCamera = camera;
                  controller.wallDetector = wallDetector;
                  controller.textureManager = textureManager;
                  controller.cameraPreview = preview;
                  // controller.captureButton = captureButton;
                  // controller.resetButton = resetButton;

                  // Setup wall detector
                  wallDetector.SetDebugImageDisplay(preview);
                  wallDetector.mainCamera = camera;

                  // Начинаем детекцию
                  wallDetector.StartDetection();

                  // Organize hierarchy
                  roomManagerObject.transform.SetParent(controllerObject.transform);
                  textureManagerObject.transform.SetParent(controllerObject.transform);
                  detectorObject.transform.SetParent(controllerObject.transform);
                  bgCanvasObject.transform.SetParent(controllerObject.transform);
                  uiCanvasObject.transform.SetParent(controllerObject.transform);

                  // Position the camera
                  cameraObject.transform.position = new Vector3(0, 1.6f, 0); // Примерная высота глаз
                  cameraObject.transform.SetParent(controllerObject.transform);

                  // Добавим коллайдер для камеры, чтобы она не проходила сквозь стены
                  var cameraCollider = cameraObject.AddComponent<SphereCollider>();
                  cameraCollider.radius = 0.5f;
                  cameraCollider.isTrigger = false;

                  // Добавим Rigidbody к камере для физического взаимодействия
                  var cameraRigidbody = cameraObject.AddComponent<Rigidbody>();
                  cameraRigidbody.useGravity = true;
                  cameraRigidbody.freezeRotation = true; // Замораживаем вращение

                  Debug.Log("Камера настроена на позиции: " + cameraObject.transform.position);

                  // Ensure the controller is enabled
                  controller.enabled = true;

                  Debug.Log("Real wall painting scene created successfully!");
            }

            private static void CreateWallLayer()
            {
                  // Check if the Wall layer exists, create it if it doesn't
                  if (LayerMask.NameToLayer("Wall") == -1)
                  {
                        Debug.LogWarning("Wall layer doesn't exist. Please create it manually in Project Settings > Tags and Layers.");
                        Debug.Log("Add a new layer named 'Wall' (typically layer 8)");
                  }
            }

            private static void CreateLighting()
            {
                  Debug.Log("Создаю улучшенное освещение для лучшей видимости материалов");

                  // Основной направленный свет (имитация солнца)
                  var mainLight = new GameObject("Main Directional Light");
                  var mainLightComponent = mainLight.AddComponent<Light>();
                  mainLightComponent.type = LightType.Directional;
                  mainLightComponent.intensity = 1.0f;
                  mainLightComponent.color = Color.white;
                  mainLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

                  // Добавляем второй направленный свет с противоположной стороны (заполняющий)
                  var fillLight = new GameObject("Fill Directional Light");
                  var fillLightComponent = fillLight.AddComponent<Light>();
                  fillLightComponent.type = LightType.Directional;
                  fillLightComponent.intensity = 0.6f;
                  fillLightComponent.color = new Color(0.9f, 0.9f, 1.0f); // Слегка синеватый
                  fillLight.transform.rotation = Quaternion.Euler(30f, 130f, 0f);

                  // Добавляем точечный свет в центре комнаты для дополнительного освещения
                  var pointLight = new GameObject("Room Point Light");
                  var pointLightComponent = pointLight.AddComponent<Light>();
                  pointLightComponent.type = LightType.Point;
                  pointLightComponent.intensity = 2.0f;
                  pointLightComponent.range = 15f;
                  pointLightComponent.color = Color.white;
                  pointLight.transform.position = new Vector3(0f, 1f, 0f);

                  // Настраиваем глобальное окружающее освещение
                  RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
                  RenderSettings.ambientIntensity = 1.5f;
                  RenderSettings.reflectionIntensity = 1.0f;

                  Debug.Log("Улучшенное освещение создано");
            }

            private static void CreateTestRoom()
            {
                  // Создаем простую комнату с 4 стенами для тестирования
                  Debug.Log("Создаю тестовую комнату для покраски");

                  // Размеры комнаты
                  float width = 10f;
                  float height = 3f;
                  float depth = 10f;
                  float wallThickness = 0.1f;

                  // Создаем пол
                  var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                  floor.name = "Floor";
                  floor.transform.position = new Vector3(0, -height / 2, 0);
                  floor.transform.localScale = new Vector3(width, wallThickness, depth);

                  // Установим более светлый материал для пола
                  Renderer floorRenderer = floor.GetComponent<Renderer>();
                  if (floorRenderer != null)
                  {
                        Material floorMaterial = new Material(Shader.Find("Standard"));
                        floorMaterial.color = new Color(0.8f, 0.8f, 0.8f); // Светло-серый
                        floorMaterial.SetFloat("_Glossiness", 0.2f);
                        floorRenderer.material = floorMaterial;
                  }

                  // Создаем потолок
                  var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
                  ceiling.name = "Ceiling";
                  ceiling.transform.position = new Vector3(0, height / 2, 0);
                  ceiling.transform.localScale = new Vector3(width, wallThickness, depth);

                  // Установим более светлый материал для потолка
                  Renderer ceilingRenderer = ceiling.GetComponent<Renderer>();
                  if (ceilingRenderer != null)
                  {
                        Material ceilingMaterial = new Material(Shader.Find("Standard"));
                        ceilingMaterial.color = new Color(0.9f, 0.9f, 0.9f); // Почти белый
                        ceilingMaterial.SetFloat("_Glossiness", 0.1f);
                        ceilingRenderer.material = ceilingMaterial;
                  }

                  // Создаем стены и назначаем тег "Wall"
                  var frontWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                  frontWall.name = "Front Wall";
                  frontWall.transform.position = new Vector3(0, 0, depth / 2);
                  frontWall.transform.localScale = new Vector3(width, height, wallThickness);
                  frontWall.layer = LayerMask.NameToLayer("Wall");
                  frontWall.tag = "Wall"; // Установка тега Wall

                  // Установим базовый материал для стен
                  Renderer frontWallRenderer = frontWall.GetComponent<Renderer>();
                  if (frontWallRenderer != null)
                  {
                        SetDefaultWallMaterial(frontWallRenderer);
                  }

                  var backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                  backWall.name = "Back Wall";
                  backWall.transform.position = new Vector3(0, 0, -depth / 2);
                  backWall.transform.localScale = new Vector3(width, height, wallThickness);
                  backWall.layer = LayerMask.NameToLayer("Wall");
                  backWall.tag = "Wall"; // Установка тега Wall

                  // Установим базовый материал для стен
                  Renderer backWallRenderer = backWall.GetComponent<Renderer>();
                  if (backWallRenderer != null)
                  {
                        SetDefaultWallMaterial(backWallRenderer);
                  }

                  var leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                  leftWall.name = "Left Wall";
                  leftWall.transform.position = new Vector3(-width / 2, 0, 0);
                  leftWall.transform.localScale = new Vector3(wallThickness, height, depth);
                  leftWall.layer = LayerMask.NameToLayer("Wall");
                  leftWall.tag = "Wall"; // Установка тега Wall

                  // Установим базовый материал для стен
                  Renderer leftWallRenderer = leftWall.GetComponent<Renderer>();
                  if (leftWallRenderer != null)
                  {
                        SetDefaultWallMaterial(leftWallRenderer);
                  }

                  var rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                  rightWall.name = "Right Wall";
                  rightWall.transform.position = new Vector3(width / 2, 0, 0);
                  rightWall.transform.localScale = new Vector3(wallThickness, height, depth);
                  rightWall.layer = LayerMask.NameToLayer("Wall");
                  rightWall.tag = "Wall"; // Установка тега Wall

                  // Установим базовый материал для стен
                  Renderer rightWallRenderer = rightWall.GetComponent<Renderer>();
                  if (rightWallRenderer != null)
                  {
                        SetDefaultWallMaterial(rightWallRenderer);
                  }

                  // Создаем родительский объект для комнаты
                  var roomParent = new GameObject("Test Room");
                  floor.transform.SetParent(roomParent.transform);
                  ceiling.transform.SetParent(roomParent.transform);
                  frontWall.transform.SetParent(roomParent.transform);
                  backWall.transform.SetParent(roomParent.transform);
                  leftWall.transform.SetParent(roomParent.transform);
                  rightWall.transform.SetParent(roomParent.transform);

                  Debug.Log("Тестовая комната успешно создана. Все стены помечены тегом 'Wall'");

                  // Убеждаемся, что в проекте существует тег Wall
                  CreateWallTag();
            }

            private static void CreateWallTag()
            {
                  // В Unity тег можно назначить только если он существует в проекте
                  // Эта функция выводит предупреждение, если тег Wall нужно создать вручную
                  try
                  {
                        GameObject tempObject = new GameObject("TempObject");
                        tempObject.tag = "Wall";
                        GameObject.DestroyImmediate(tempObject);
                        Debug.Log("Тег 'Wall' существует в проекте");
                  }
                  catch (System.Exception)
                  {
                        Debug.LogWarning("Тег 'Wall' не существует в проекте. Создайте его вручную в меню Edit -> Project Settings -> Tags and Layers");
                  }
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

            // Метод для установки базового материала стены
            private static void SetDefaultWallMaterial(Renderer renderer)
            {
                  Material wallMaterial = new Material(Shader.Find("Standard"));
                  wallMaterial.color = new Color(0.95f, 0.95f, 0.9f); // Кремовый/бежевый цвет
                  wallMaterial.SetFloat("_Glossiness", 0.1f); // Матовый
                  renderer.material = wallMaterial;
            }

            private static GameObject CreateBackgroundCanvas()
            {
                  Debug.Log("Создаю улучшенный фон для приложения");

                  // Создаем канвас для фона
                  GameObject bgCanvasObject = new GameObject("Background Canvas");
                  Canvas bgCanvas = bgCanvasObject.AddComponent<Canvas>();
                  bgCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                  bgCanvas.sortingOrder = -10; // Позади всех других UI элементов
                  bgCanvasObject.AddComponent<CanvasScaler>();

                  // Создаем панель для фона
                  GameObject bgPanel = new GameObject("Background Panel");
                  bgPanel.transform.SetParent(bgCanvasObject.transform, false);
                  Image bgImage = bgPanel.AddComponent<Image>();

                  // Создаем градиентную текстуру для фона
                  Texture2D gradientTexture = CreateGradientTexture(
                        new Color(0.1f, 0.2f, 0.4f), // Темно-синий внизу
                        new Color(0.5f, 0.7f, 0.9f)  // Светло-голубой вверху
                  );

                  // Создаем спрайт из текстуры
                  Sprite bgSprite = Sprite.Create(
                        gradientTexture,
                        new UnityEngine.Rect(0, 0, gradientTexture.width, gradientTexture.height),
                        new Vector2(0.5f, 0.5f)
                  );

                  // Применяем спрайт к Image компоненту
                  bgImage.sprite = bgSprite;
                  bgImage.color = Color.white; // Полная непрозрачность

                  // Растягиваем на весь экран
                  RectTransform bgRect = bgPanel.GetComponent<RectTransform>();
                  bgRect.anchorMin = Vector2.zero;
                  bgRect.anchorMax = Vector2.one;
                  bgRect.sizeDelta = Vector2.zero;
                  bgRect.anchoredPosition = Vector2.zero;

                  Debug.Log("Фон с градиентом успешно создан");

                  return bgCanvasObject;
            }

            // Метод для создания градиентной текстуры
            private static Texture2D CreateGradientTexture(Color bottomColor, Color topColor)
            {
                  int width = 1;
                  int height = 256;
                  Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

                  // Заполняем текстуру градиентом
                  for (int y = 0; y < height; y++)
                  {
                        float t = (float)y / height;
                        Color color = Color.Lerp(bottomColor, topColor, t);
                        texture.SetPixel(0, y, color);
                  }

                  texture.Apply();
                  return texture;
            }
      }
}