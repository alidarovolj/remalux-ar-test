using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Remalux.WallPainting.Vision
{
      [RequireComponent(typeof(RealWallPaintingController))]
      public class RealWallPaintingSetup : MonoBehaviour
      {
            [Header("Camera Setup")]
            [SerializeField] private Camera mainCamera;

            [Header("UI Setup")]
            [SerializeField] private Canvas mainCanvas;
            [SerializeField] private RectTransform cameraPreviewParent;

            [Header("Wall Detection")]
            [SerializeField] private bool createWallDetector = true;

            private RealWallPaintingController paintingController;
            private WallDetector wallDetector;
            private RawImage cameraPreview;

            private void Awake()
            {
                  // Get or create components
                  paintingController = GetComponent<RealWallPaintingController>();

                  // Find camera if not set
                  if (mainCamera == null)
                        mainCamera = Camera.main;

                  if (mainCamera == null)
                  {
                        Debug.LogError("RealWallPaintingSetup: No main camera found in the scene!");
                        return;
                  }

                  // Set up canvas if needed
                  SetupCanvas();

                  // Set up camera preview
                  SetupCameraPreview();

                  // Set up wall detector
                  SetupWallDetector();

                  // Set up the painting controller with all our references
                  ConfigurePaintingController();
            }

            private void SetupCanvas()
            {
                  if (mainCanvas == null)
                  {
                        // Look for existing canvas
                        mainCanvas = FindFirstObjectByType<Canvas>();

                        // Create canvas if not found
                        if (mainCanvas == null)
                        {
                              GameObject canvasObj = new GameObject("Main Canvas");
                              mainCanvas = canvasObj.AddComponent<Canvas>();
                              mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                              canvasObj.AddComponent<CanvasScaler>();
                              canvasObj.AddComponent<GraphicRaycaster>();
                        }
                  }

                  // Make sure we have EventSystem
                  if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                  {
                        GameObject eventSystem = new GameObject("Event System");
                        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                  }
            }

            private void SetupCameraPreview()
            {
                  // Create camera preview parent if needed
                  if (cameraPreviewParent == null)
                  {
                        GameObject previewParentObj = new GameObject("Camera Preview Parent");
                        previewParentObj.transform.SetParent(mainCanvas.transform, false);
                        cameraPreviewParent = previewParentObj.AddComponent<RectTransform>();

                        // Position in corner
                        cameraPreviewParent.anchorMin = new Vector2(0.7f, 0.05f);
                        cameraPreviewParent.anchorMax = new Vector2(0.95f, 0.25f);
                        cameraPreviewParent.offsetMin = Vector2.zero;
                        cameraPreviewParent.offsetMax = Vector2.zero;
                  }

                  // Create RawImage for camera preview
                  GameObject previewObj = new GameObject("Camera Preview");
                  previewObj.transform.SetParent(cameraPreviewParent, false);

                  RectTransform previewRect = previewObj.AddComponent<RectTransform>();
                  previewRect.anchorMin = Vector2.zero;
                  previewRect.anchorMax = Vector2.one;
                  previewRect.offsetMin = Vector2.zero;
                  previewRect.offsetMax = Vector2.zero;

                  cameraPreview = previewObj.AddComponent<RawImage>();
                  cameraPreview.color = new Color(1, 1, 1, 0.8f);
            }

            private void SetupWallDetector()
            {
                  if (createWallDetector)
                  {
                        // Create or get wall detector
                        wallDetector = GetComponent<WallDetector>();
                        if (wallDetector == null)
                        {
                              wallDetector = gameObject.AddComponent<WallDetector>();
                        }

                        // Configure wall detector
                        wallDetector.mainCamera = mainCamera;

                        // Set reasonable default values
                        var properties = wallDetector.GetType().GetFields(System.Reflection.BindingFlags.Instance |
                                                                         System.Reflection.BindingFlags.Public |
                                                                         System.Reflection.BindingFlags.NonPublic);

                        foreach (var prop in properties)
                        {
                              if (prop.Name == "lineThreshold")
                                    prop.SetValue(wallDetector, 50);
                              else if (prop.Name == "minLineLength")
                                    prop.SetValue(wallDetector, 30);
                              else if (prop.Name == "maxLineGap")
                                    prop.SetValue(wallDetector, 10);
                              else if (prop.Name == "showContours")
                                    prop.SetValue(wallDetector, true);
                              else if (prop.Name == "showDebugLines")
                                    prop.SetValue(wallDetector, true);
                        }
                  }
            }

            private void ConfigurePaintingController()
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

                        Debug.Log("RealWallPaintingSetup: Successfully configured RealWallPaintingController");
                  }
            }

            // Simplified static method for setup - removes AR dependency issues
            public static void CreateRealWallPaintingScene()
            {
                  Debug.Log("Creating real wall painting scene...");

                  // Create necessary tags and layers
                  CreateWallTag();
                  CreateWallLayer();

                  // Create game controller
                  GameObject controllerObject = new GameObject("RealWallPaintingController");
                  RealWallPaintingController controller = controllerObject.AddComponent<RealWallPaintingController>();

                  // Add setup script that will handle all the component connections
                  var setup = controllerObject.AddComponent<RealWallPaintingSetup>();

                  // Create camera
                  GameObject cameraObject = new GameObject("MainCamera");
                  Camera camera = cameraObject.AddComponent<Camera>();
                  camera.tag = "MainCamera";
                  cameraObject.AddComponent<AudioListener>();

                  // Create simple camera rig
                  GameObject cameraParent = new GameObject("Camera Rig");
                  cameraObject.transform.SetParent(cameraParent.transform);
                  cameraParent.transform.SetParent(controllerObject.transform);

                  // Create lighting
                  CreateLighting();

                  // Create test room with walls
                  CreateTestRoom();

                  // Create UI elements
                  GameObject canvasObject = new GameObject("UI Canvas");
                  Canvas canvas = canvasObject.AddComponent<Canvas>();
                  canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                  canvasObject.AddComponent<CanvasScaler>();
                  canvasObject.AddComponent<GraphicRaycaster>();
                  canvasObject.transform.SetParent(controllerObject.transform);

                  // Create background
                  GameObject bgCanvasObject = CreateBackgroundCanvas();
                  bgCanvasObject.transform.SetParent(controllerObject.transform);

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

                  Debug.Log("Улучшенное освещение создано");
            }

            private static void CreateTestRoom()
            {
                  // Создаем простую комнату с 4 стенами для тестирования
                  Debug.Log("Создаю тестовую комнату для покраски");

                  GameObject roomContainer = new GameObject("Test Room");

                  // Размеры комнаты
                  float width = 10f;
                  float height = 3f;
                  float depth = 10f;
                  float wallThickness = 0.1f;

                  // Создаем пол
                  var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                  floor.name = "Floor";
                  floor.transform.SetParent(roomContainer.transform);
                  floor.transform.position = new Vector3(0, -height / 2, 0);
                  floor.transform.localScale = new Vector3(width, wallThickness, depth);

                  // Установим более светлый материал для пола
                  Renderer floorRenderer = floor.GetComponent<Renderer>();
                  if (floorRenderer != null)
                  {
                        Material floorMaterial = new Material(Shader.Find("Standard"));
                        floorMaterial.color = new Color(0.8f, 0.8f, 0.8f);
                        floorMaterial.SetFloat("_Glossiness", 0.2f);
                        floorRenderer.material = floorMaterial;
                  }

                  // Создаем стены
                  // Передняя стена
                  CreateWall("Front Wall", roomContainer.transform,
                      new Vector3(0, 0, depth / 2),
                      new Vector3(width, height, wallThickness));

                  // Задняя стена
                  CreateWall("Back Wall", roomContainer.transform,
                      new Vector3(0, 0, -depth / 2),
                      new Vector3(width, height, wallThickness));

                  // Левая стена
                  CreateWall("Left Wall", roomContainer.transform,
                      new Vector3(-width / 2, 0, 0),
                      new Vector3(wallThickness, height, depth));

                  // Правая стена
                  CreateWall("Right Wall", roomContainer.transform,
                      new Vector3(width / 2, 0, 0),
                      new Vector3(wallThickness, height, depth));

                  Debug.Log("Тестовая комната создана успешно");
            }

            private static GameObject CreateWall(string name, Transform parent, Vector3 position, Vector3 scale)
            {
                  var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                  wall.name = name;
                  wall.transform.SetParent(parent);
                  wall.transform.position = position;
                  wall.transform.localScale = scale;

                  // Настраиваем материал стены
                  Renderer wallRenderer = wall.GetComponent<Renderer>();
                  if (wallRenderer != null)
                  {
                        SetDefaultWallMaterial(wallRenderer);
                  }

                  // Добавляем компонент для определения стены
                  wall.AddComponent<WallIdentifier>();

                  // Настраиваем коллайдер
                  var collider = wall.GetComponent<BoxCollider>();
                  if (collider != null)
                  {
                        collider.isTrigger = false;
                  }

                  return wall;
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

            // Метод для установки базового материала стены
            private static void SetDefaultWallMaterial(Renderer renderer)
            {
                  // Create a simple material instead of relying on custom shader
                  Material wallMaterial = new Material(Shader.Find("Standard"));
                  if (wallMaterial != null)
                  {
                        // Set basic material properties
                        wallMaterial.color = Color.white;
                        wallMaterial.SetFloat("_Glossiness", 0.1f);
                        wallMaterial.SetFloat("_Metallic", 0.0f);

                        // Apply material to renderer
                        renderer.material = wallMaterial;

                        // Set layer if available
                        int wallLayer = LayerMask.NameToLayer("Wall");
                        if (wallLayer != -1)
                        {
                              renderer.gameObject.layer = wallLayer;
                        }
                  }
                  else
                  {
                        Debug.LogError("Failed to create wall material. Standard shader not found.");
                  }
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