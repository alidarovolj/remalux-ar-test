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

            private bool isCapturing = false;
            private List<WallData> detectedWalls = new List<WallData>();
            private Material blueMaterial;
            private GameObject messagePanel; // Панель для сообщений
            private Coroutine messageCoroutine; // Корутин для скрытия сообщений
            private bool isFullScreenCamera = false; // Переменная для отслеживания полноэкранного режима
            private WebCamTexture webCamTexture; // Текстура веб-камеры

            // Добавляем недостающие переменные
            private List<GameObject> wallMarkers = new List<GameObject>();
            private List<GameObject> createdWallObjects = new List<GameObject>();

            private void Start()
            {
                  Debug.Log("RealWallPaintingController.Start()");

                  // Проверяем, что EventSystem существует
                  if (EventSystem.current == null)
                  {
                        Debug.Log("Creating EventSystem");
                        GameObject eventSystem = new GameObject("EventSystem");
                        eventSystem.AddComponent<EventSystem>();
                        eventSystem.AddComponent<StandaloneInputModule>();
                  }

                  // Создаем панели для сообщений
                  CreateMessagePanel();

                  // Создаем панель помощи
                  CreateHelpPanel();

                  // Инициализируем материал по умолчанию
                  blueMaterial = new Material(Shader.Find("Standard"));
                  blueMaterial.color = Color.blue;

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

                  if (wallDetector != null && cameraPreview != null)
                  {
                        wallDetector.SetDebugImageDisplay(cameraPreview);
                  }
            }

            private void OnCaptureButtonClicked()
            {
                  if (!isCapturing)
                  {
                        StartCapture();
                  }
                  else
                  {
                        StopCapture();
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
                  ConfigureAdvancedWallDetection(wallDetector);

                  // Включаем отображение контуров на видеопотоке
                  EnableContoursOnCamera(wallDetector);

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
                  // Process any pending actions on the main thread
                  UnityMainThread.Update();

                  // Обработка ввода пользователя
                  HandleInput();

                  // Обработка движения камеры, если необходимо
                  HandleCameraMovement();

                  // Добавляем счетчик для автоматического создания тестовых стен,
                  // если ничего не обнаружено в течение 5 секунд
                  if (wallMarkers.Count == 0 && Time.time > lastWallCreationTime + 5f)
                  {
                        Debug.Log("Стены не обнаружены в течение 5 секунд, создаю тестовые стены");
                        CreateTestWalls();
                        lastWallCreationTime = Time.time;
                  }

                  // Обработка ввода для создания тестовых стен
                  if (Input.GetKeyDown(KeyCode.T))
                  {
                        CreateTestWalls();
                        ShowMessage("Созданы тестовые стены. Используйте мышь для рисования.");
                  }

                  // Добавляем простой режим рисования
                  if (Input.GetMouseButton(0) && wallMarkers.Count > 0)
                  {
                        // Проверяем, попадает ли луч от мыши в стену
                        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                        RaycastHit hit;

                        if (Physics.Raycast(ray, out hit))
                        {
                              // Проверяем, это ли наша стена
                              if (hit.collider.CompareTag("Wall"))
                              {
                                    // Создаем позицию для рисования в текстурных координатах
                                    Vector2 textureCoord = hit.textureCoord;
                                    GameObject wall = hit.collider.gameObject;

                                    // Рисуем на стене
                                    PaintOnWall(wall, textureCoord, Color.red);
                              }
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

                  // Получаем цвет для покраски
                  Color paintColor = GetPaintColor();

                  // Выполняем рейкаст из точки на экране для определения стены
                  Ray ray = mainCamera.ScreenPointToRay(new Vector3(screenPosition.x, screenPosition.y, 0));
                  RaycastHit hit;

                  // Логируем информацию о луче для отладки
                  Debug.DrawRay(ray.origin, ray.direction * 10, Color.red, 2.0f);
                  Debug.Log($"Рейкаст из {ray.origin} в направлении {ray.direction}");

                  // Указываем максимальное расстояние 100f и используем все слои
                  if (Physics.Raycast(ray, out hit, 100f, Physics.AllLayers))
                  {
                        GameObject hitObject = hit.collider.gameObject;
                        Debug.Log($"Рейкаст попал в объект: {hitObject.name} с тегом: {hitObject.tag}, расстояние: {hit.distance}");

                        // Проверяем, является ли объект стеной
                        if (hitObject.CompareTag("Wall"))
                        {
                              // Используем метод PaintWallTexture для применения реалистичной покраски
                              PaintWallTexture(hitObject, paintColor);

                              // Создаем эффект брызг в точке попадания для визуальной обратной связи
                              CreatePaintEffectAtHitPoint(hit.point, hit.normal, paintColor);

                              // Создаем частицы для дополнительного эффекта
                              CreatePaintParticles(hit.point, hit.normal);

                              // Добавляем анимацию клика на экране
                              ShowClickFeedback(screenPosition);

                              // Сообщаем другим компонентам о покраске стены
                              OnWallPainted?.Invoke(hitObject);

                              // Показываем сообщение о покраске
                              ShowMessage("Поверхность окрашена!", 2f);
                              return;
                        }
                        else
                        {
                              Debug.Log($"Объект {hitObject.name} не помечен тегом Wall. Пробуем применить покраску к родительскому объекту.");

                              // Проверяем родительский объект, если он есть
                              Transform parent = hitObject.transform.parent;
                              while (parent != null)
                              {
                                    if (parent.CompareTag("Wall"))
                                    {
                                          // Применяем покраску к родительскому объекту
                                          PaintWallTexture(parent.gameObject, paintColor);
                                          CreatePaintEffectAtHitPoint(hit.point, hit.normal, paintColor);
                                          CreatePaintParticles(hit.point, hit.normal);
                                          ShowClickFeedback(screenPosition);
                                          OnWallPainted?.Invoke(parent.gameObject);
                                          ShowMessage("Поверхность окрашена!", 2f);
                                          return;
                                    }
                                    parent = parent.parent;
                              }

                              // Если не нашли объект с тегом Wall - просто применяем покраску к рендереру объекта
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
                                    ShowClickFeedback(screenPosition);
                                    ShowMessage("Поверхность окрашена!", 2f);
                                    return;
                              }
                              else
                              {
                                    Debug.Log($"Объект {hitObject.name} не имеет компонента Renderer");
                              }
                        }
                  }
                  else
                  {
                        Debug.Log("Рейкаст не попал ни в один объект. Создаем UI-эффект покраски.");
                  }

                  // Если рейкаст не попал ни в один объект или это не стена, создаем UI-эффект
                  GameObject paintedArea = CreateRealPaintedArea(screenPosition, paintColor);
                  ShowClickFeedback(screenPosition); // Добавляем эффект клика
                  ShowMessage("Поверхность окрашена!", 2f);
            }

            // Новый метод для создания эффекта брызг краски в точке попадания
            private void CreatePaintEffectAtHitPoint(Vector3 hitPoint, Vector3 normal, Color paintColor)
            {
                  // Создаем объект для эффекта покраски
                  GameObject paintEffect = new GameObject("PaintEffect_" + System.DateTime.Now.Ticks);
                  paintEffect.transform.position = hitPoint;
                  paintEffect.transform.rotation = Quaternion.LookRotation(normal);

                  // Создаем quad для отображения эффекта
                  GameObject paintQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                  paintQuad.transform.SetParent(paintEffect.transform, false);
                  paintQuad.transform.localScale = new Vector3(0.3f, 0.3f, 0.01f);
                  paintQuad.transform.localPosition = new Vector3(0, 0, 0.001f); // Небольшое смещение от поверхности

                  // Создаем материал для эффекта
                  Material paintMaterial = new Material(Shader.Find("Unlit/Transparent"));

                  // Создаем текстуру для эффекта покраски
                  Texture2D paintTexture = CreatePaintSplatterTexture(256, paintColor);
                  paintMaterial.mainTexture = paintTexture;
                  paintMaterial.color = new Color(paintColor.r, paintColor.g, paintColor.b, 0.9f);

                  // Применяем материал к эффекту
                  Renderer renderer = paintQuad.GetComponent<Renderer>();
                  renderer.material = paintMaterial;

                  // Автоматически удаляем эффект через некоторое время
                  Destroy(paintEffect, 5f);
            }

            // Метод для создания текстуры брызг краски
            private Texture2D CreatePaintSplatterTexture(int size, Color baseColor)
            {
                  Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                  Color[] pixels = new Color[size * size];

                  // Центр текстуры
                  Vector2 center = new Vector2(size / 2f, size / 2f);
                  float maxRadius = size * 0.45f;

                  // Создаем эффект краски с неровными краями
                  for (int y = 0; y < size; y++)
                  {
                        for (int x = 0; x < size; x++)
                        {
                              int index = y * size + x;

                              // Расстояние от центра
                              float distX = x - center.x;
                              float distY = y - center.y;
                              float dist = Mathf.Sqrt(distX * distX + distY * distY);

                              // По умолчанию прозрачный
                              pixels[index] = new Color(0, 0, 0, 0);

                              if (dist < maxRadius)
                              {
                                    // Добавляем шум для неровных краев
                                    float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f);
                                    float edgeFactor = 1.0f - dist / maxRadius;

                                    if (dist < maxRadius * (0.8f + noise * 0.3f))
                                    {
                                          // Вычисляем прозрачность на основе расстояния и шума
                                          float alpha = edgeFactor * (0.5f + noise * 0.5f);
                                          alpha = Mathf.Clamp01(alpha);

                                          // Небольшое изменение оттенка для реализма
                                          float hueShift = (noise - 0.5f) * 0.1f;
                                          Color adjustedColor = baseColor;

                                          // Применяем цвет с учетом прозрачности
                                          pixels[index] = new Color(
                                                adjustedColor.r,
                                                adjustedColor.g,
                                                adjustedColor.b,
                                                alpha
                                          );
                                    }
                              }
                        }
                  }

                  // Рисуем капли по краям
                  int numDrops = Random.Range(6, 12);
                  for (int i = 0; i < numDrops; i++)
                  {
                        float angle = Random.Range(0, Mathf.PI * 2);
                        float distance = Random.Range(maxRadius * 0.7f, maxRadius * 0.9f);
                        Vector2 dropPosition = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

                        // Создаем каплю с вытянутым "хвостом"
                        float dropLength = Random.Range(10, 30);
                        float dropWidth = Random.Range(3, 7);

                        // Направление капли (обычно вниз)
                        Vector2 dropDirection = new Vector2(Mathf.Cos(angle + Mathf.PI), Mathf.Sin(angle + Mathf.PI)).normalized;

                        // Рисуем каплю
                        for (float d = 0; d < dropLength; d += 0.5f)
                        {
                              float currentWidth = dropWidth * (1 - d / dropLength);
                              Vector2 currentPos = dropPosition + dropDirection * d;

                              for (float w = -currentWidth; w <= currentWidth; w += 0.5f)
                              {
                                    // Позиция перпендикулярно направлению
                                    Vector2 perpendicular = new Vector2(-dropDirection.y, dropDirection.x);
                                    Vector2 pixelPos = currentPos + perpendicular * w;

                                    int pixelX = Mathf.RoundToInt(pixelPos.x);
                                    int pixelY = Mathf.RoundToInt(pixelPos.y);

                                    if (pixelX >= 0 && pixelX < size && pixelY >= 0 && pixelY < size)
                                    {
                                          int pixelIndex = pixelY * size + pixelX;
                                          float alpha = (1 - Mathf.Abs(w) / currentWidth) * (1 - d / dropLength);
                                          pixels[pixelIndex] = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                                    }
                              }
                        }
                  }

                  texture.SetPixels(pixels);
                  texture.Apply();
                  return texture;
            }

            // Добавляем событие для оповещения о покраске стены
            public event System.Action<GameObject> OnWallPainted;

            // Метод для создания реалистичного окрашенного участка с текстурой
            private GameObject CreateRealPaintedArea(Vector2 center, Color baseColor)
            {
                  // Создаем новый объект для окрашенной области
                  GameObject paintedArea = new GameObject("PaintedArea_" + System.DateTime.Now.Ticks);

                  // Добавляем Canvas для UI элементов
                  Canvas canvas = paintedArea.AddComponent<Canvas>();
                  canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                  canvas.sortingOrder = 1;

                  // Добавляем CanvasScaler
                  paintedArea.AddComponent<CanvasScaler>();

                  // Создаем маску, ограничивающую область покраски
                  GameObject maskObj = new GameObject("PaintMask");
                  maskObj.transform.SetParent(paintedArea.transform, false);

                  // Вычисляем размер области покраски (примерно 40% ширины экрана)
                  float areaWidth = Screen.width * 0.4f;
                  float areaHeight = Screen.height * 0.6f;

                  // Добавляем компонент RectMask2D для маскирования области
                  RectMask2D mask = maskObj.AddComponent<RectMask2D>();
                  RectTransform maskRect = mask.rectTransform;
                  maskRect.anchorMin = new Vector2(0, 0);
                  maskRect.anchorMax = new Vector2(0, 0);
                  maskRect.sizeDelta = new Vector2(areaWidth, areaHeight);
                  maskRect.anchoredPosition = center;

                  // Создаем текстурированное изображение для окрашенной области
                  GameObject paintImage = new GameObject("PaintTexture");
                  paintImage.transform.SetParent(maskObj.transform, false);

                  // Добавляем компонент RawImage
                  RawImage paintRawImage = paintImage.AddComponent<RawImage>();

                  // Создаем реалистичную текстуру окрашенной поверхности
                  Texture2D paintTexture = CreateRealisticPaintTexture(256, baseColor);
                  paintRawImage.texture = paintTexture;
                  paintRawImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.95f);

                  // Настраиваем размер и позицию изображения
                  RectTransform imageRect = paintRawImage.rectTransform;
                  imageRect.anchorMin = new Vector2(0, 0);
                  imageRect.anchorMax = new Vector2(1, 1);
                  imageRect.sizeDelta = Vector2.zero;
                  imageRect.anchoredPosition = Vector2.zero;

                  Debug.Log($"Создана реалистичная покраска стены размером ({areaWidth}x{areaHeight}) в позиции {center}");

                  return paintedArea;
            }

            // Метод для создания реалистичной текстуры окрашенной поверхности
            private Texture2D CreateRealisticPaintTexture(int size, Color baseColor)
            {
                  Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                  Color[] pixels = new Color[size * size];

                  // Параметры шума для имитации неровностей краски
                  float noiseScale = 0.03f;
                  float edgeScale = 0.1f;

                  // Генерируем текстуру с эффектом реалистичной покраски
                  for (int y = 0; y < size; y++)
                  {
                        for (int x = 0; x < size; x++)
                        {
                              int index = y * size + x;

                              // Добавляем шум Перлина для естественных неровностей
                              float perlinNoise = Mathf.PerlinNoise(x * noiseScale, y * noiseScale);

                              // Создаем эффект неровностей на краске
                              float brightnessFactor = 0.92f + perlinNoise * 0.08f;

                              // Немного меняем оттенок цвета для естественного вида
                              Color pixelColor = new Color(
                                    baseColor.r * brightnessFactor,
                                    baseColor.g * brightnessFactor,
                                    baseColor.b * brightnessFactor,
                                    1.0f
                              );

                              pixels[index] = pixelColor;
                        }
                  }

                  texture.SetPixels(pixels);
                  texture.Apply();
                  return texture;
            }

            // Метод для покраски всех стен одновременно
            private void PaintAllWalls()
            {
                  if (detectedWalls.Count == 0)
                  {
                        ShowBigScreenMessage("НЕТ СТЕН ДЛЯ ПОКРАСКИ!\nНажмите клавишу T для создания тестовых стен");
                        return;
                  }

                  // Получаем синий материал (или создаем новый, если он ещё не создан)
                  if (blueMaterial == null)
                  {
                        blueMaterial = new Material(Shader.Find("Standard"));
                        blueMaterial.color = new Color(0.1f, 0.4f, 1.0f); // Насыщенный голубой
                        blueMaterial.EnableKeyword("_EMISSION");
                        blueMaterial.SetColor("_EmissionColor", new Color(0.1f, 0.3f, 1.0f) * 0.8f); // Голубое свечение
                        blueMaterial.SetFloat("_Glossiness", 0.8f); // Высокий глянец
                        blueMaterial.SetFloat("_Metallic", 0.2f); // Немного металлический
                  }

                  // Находим все объекты с тегом Wall
                  GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");

                  if (walls.Length == 0)
                  {
                        ShowBigScreenMessage("НЕТ СТЕН ДЛЯ ПОКРАСКИ!\nНажмите клавишу T для создания тестовых стен");
                        return;
                  }

                  // Последовательно окрашиваем стены с анимацией
                  StartCoroutine(SequentialPaintCoroutine(walls));
            }

            // Корутина для последовательной покраски стен с эффектами
            private IEnumerator SequentialPaintCoroutine(GameObject[] walls)
            {
                  int paintedCount = 0;
                  float delayBetweenWalls = 0.5f; // Пауза между покраской стен
                  List<GameObject> paintedWalls = new List<GameObject>();

                  // Показываем сообщение о начале покраски
                  ShowMessage("Начинаем окраску стен...", 2.0f);

                  foreach (GameObject wall in walls)
                  {
                        Renderer renderer = wall.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                              // Сохраняем оригинальный материал
                              Material originalMaterial = renderer.material;

                              // Создаем эффект "подготовки к покраске" - мигание стены
                              for (int i = 0; i < 3; i++) // Мигаем 3 раза
                              {
                                    // Создаем материал "подготовки" - белый
                                    Material prepMaterial = new Material(Shader.Find("Standard"));
                                    prepMaterial.color = Color.white;
                                    prepMaterial.EnableKeyword("_EMISSION");
                                    prepMaterial.SetColor("_EmissionColor", Color.white * 0.5f);

                                    renderer.material = prepMaterial;
                                    yield return new WaitForSeconds(0.1f);

                                    renderer.material = originalMaterial;
                                    yield return new WaitForSeconds(0.1f);
                              }

                              // Применяем синий материал
                              renderer.material = blueMaterial;
                              paintedCount++;
                              paintedWalls.Add(wall);

                              // Создаем эффект покраски в позиции каждой стены
                              Vector3 centerPos = wall.transform.position;
                              CreatePaintEffectAtHitPoint(centerPos, -wall.transform.forward, blueMaterial.color);

                              // Показываем текст с прогрессом
                              ShowMessage($"Покрашено {paintedCount}/{walls.Length} поверхностей", 1.0f);

                              // Ждем перед покраской следующей стены
                              yield return new WaitForSeconds(delayBetweenWalls);
                        }
                  }

                  // Показываем сообщение о результате
                  if (paintedCount > 0)
                  {
                        ShowBigScreenMessage($"УСПЕШНО ОКРАШЕНО {paintedCount} ПОВЕРХНОСТЕЙ!");
                        // Создаем эффект "завершения" - вспышка всех покрашенных стен
                        StartCoroutine(FlashPaintedWalls(paintedWalls));
                        Debug.Log($"Окрашено {paintedCount} стен одним нажатием пробела");
                  }
                  else
                  {
                        ShowBigScreenMessage("НЕ УДАЛОСЬ ОКРАСИТЬ СТЕНЫ\nПопробуйте создать тестовые стены клавишей T");
                        Debug.Log("Не удалось найти стены с тегом Wall для покраски");
                  }
            }

            // Корутина для создания эффекта завершения окраски - вспышка всех стен
            private IEnumerator FlashPaintedWalls(List<GameObject> paintedWalls)
            {
                  // Ждем немного после сообщения об успешной покраске
                  yield return new WaitForSeconds(0.5f);

                  // Вспышка всех покрашенных стен
                  foreach (GameObject wall in paintedWalls)
                  {
                        Renderer renderer = wall.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                              // Сохраняем оригинальный материал
                              Material origMaterial = renderer.material;

                              // Создаем яркий материал для вспышки
                              Material flashMaterial = new Material(Shader.Find("Standard"));
                              flashMaterial.color = new Color(0.3f, 0.6f, 1.0f);
                              flashMaterial.EnableKeyword("_EMISSION");
                              flashMaterial.SetColor("_EmissionColor", new Color(0.3f, 0.6f, 1.0f) * 2.0f);

                              renderer.material = flashMaterial;

                              // Создаем частицы в центре стены, если в проекте есть поддержка частиц
                              try
                              {
                                    CreatePaintParticles(wall.transform.position, wall.transform.forward);
                              }
                              catch (System.Exception)
                              {
                                    // Игнорируем ошибки, если частицы не поддерживаются
                              }
                        }
                  }

                  // Ждем немного, чтобы показать эффект вспышки
                  yield return new WaitForSeconds(0.3f);

                  // Возвращаем оригинальный материал
                  foreach (GameObject wall in paintedWalls)
                  {
                        Renderer renderer = wall.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                              renderer.material = blueMaterial;
                        }
                  }
            }

            // Метод для создания частиц краски для дополнительного эффекта
            private void CreatePaintParticles(Vector3 position, Vector3 normal)
            {
                  // Создаем объект для системы частиц
                  GameObject particleObj = new GameObject("PaintParticles");
                  particleObj.transform.position = position;
                  particleObj.transform.rotation = Quaternion.LookRotation(normal);

                  // Добавляем компонент ParticleSystem
                  ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();

                  // Настраиваем основные параметры
                  var main = ps.main;
                  main.startSpeed = 1.0f;
                  main.startSize = 0.05f;
                  main.startLifetime = 1.5f;
                  main.maxParticles = 100;

                  // Получаем цвет покраски через специальный метод
                  Color paintColor = GetPaintColor();
                  main.startColor = paintColor;

                  // Форма эмиссии - конус
                  var shape = ps.shape;
                  shape.shapeType = ParticleSystemShapeType.Cone;
                  shape.angle = 25f;

                  // Добавляем настройки для более красивых брызг
                  var emission = ps.emission;
                  emission.rateOverTime = 50;
                  emission.burstCount = 1;
                  emission.SetBurst(0, new ParticleSystem.Burst(0f, 30));

                  // Настраиваем размер частиц и скорость
                  var sizeOverLifetime = ps.sizeOverLifetime;
                  sizeOverLifetime.enabled = true;
                  AnimationCurve sizeOverLifetimeCurve = new AnimationCurve();
                  sizeOverLifetimeCurve.AddKey(0f, 1f);
                  sizeOverLifetimeCurve.AddKey(1f, 0f);
                  sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeOverLifetimeCurve);

                  // Настраиваем рендерер частиц для более качественного отображения
                  var renderer = ps.GetComponent<ParticleSystemRenderer>();
                  renderer.renderMode = ParticleSystemRenderMode.Billboard;
                  renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
                  renderer.material.color = paintColor;

                  // Автоматическое уничтожение
                  Destroy(particleObj, 2.0f);

                  // Запускаем систему частиц
                  ps.Play();
            }

            // Компонент для плавного проявления покраски
            public class PaintFadeIn : MonoBehaviour
            {
                  public float fadeInTime = 0.5f;
                  private float elapsedTime = 0f;
                  private RawImage paintImage;
                  private Color targetColor;

                  void Start()
                  {
                        paintImage = GetComponent<RawImage>();
                        if (paintImage != null)
                        {
                              targetColor = paintImage.color;
                              // Начинаем с полностью прозрачного цвета
                              Color startColor = targetColor;
                              startColor.a = 0f;
                              paintImage.color = startColor;
                        }
                  }

                  void Update()
                  {
                        if (paintImage != null && elapsedTime < fadeInTime)
                        {
                              elapsedTime += Time.deltaTime;
                              float t = Mathf.Clamp01(elapsedTime / fadeInTime);

                              // Интерполируем прозрачность от 0 до целевого значения
                              Color currentColor = paintImage.color;
                              currentColor.a = Mathf.Lerp(0f, targetColor.a, t);
                              paintImage.color = currentColor;
                        }
                  }
            }

            // Обновленный метод для получения цвета краски
            private Color GetPaintColor()
            {
                  // Используем синий материал, если он уже создан
                  if (blueMaterial != null)
                  {
                        return blueMaterial.color;
                  }

                  // Выбираем яркий, насыщенный голубой цвет
                  return new Color(0.1f, 0.4f, 1.0f, 1.0f);
            }

            // Метод для создания текстуры круга
            private Texture2D CreateCircleTexture(int size, Color color)
            {
                  Texture2D texture = new Texture2D(size, size);
                  Color[] colors = new Color[size * size];

                  float radius = size / 2f;
                  float radiusSq = radius * radius;

                  for (int y = 0; y < size; y++)
                  {
                        for (int x = 0; x < size; x++)
                        {
                              int index = y * size + x;
                              float dx = x - radius;
                              float dy = y - radius;
                              float distSq = dx * dx + dy * dy;

                              // Создаем круг с мягкими краями
                              if (distSq <= radiusSq)
                              {
                                    float dist = Mathf.Sqrt(distSq);
                                    float alpha = 1.0f;

                                    // Мягкая граница
                                    if (dist > radius * 0.8f)
                                    {
                                          alpha = 1.0f - (dist - radius * 0.8f) / (radius * 0.2f);
                                    }

                                    colors[index] = new Color(color.r, color.g, color.b, alpha * color.a);
                              }
                              else
                              {
                                    colors[index] = Color.clear;
                              }
                        }
                  }

                  texture.SetPixels(colors);
                  texture.Apply();

                  return texture;
            }

            // Создаем панель для отображения сообщений пользователю
            private void CreateMessagePanel()
            {
                  try
                  {
                        // Создаем объект панели
                        messagePanel = new GameObject("Message Panel");
                        messagePanel.transform.SetParent(transform);

                        // Создаем канвас для UI элементов
                        Canvas canvas = messagePanel.AddComponent<Canvas>();
                        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                        canvas.sortingOrder = 10; // Поверх всего остального UI

                        // Добавляем компоненты для масштабирования UI
                        messagePanel.AddComponent<CanvasScaler>();
                        messagePanel.AddComponent<GraphicRaycaster>();

                        // Создаем фон для сообщения
                        GameObject background = new GameObject("Message Background");
                        background.transform.SetParent(messagePanel.transform, false);

                        // Добавляем компонент изображения для фона
                        Image bgImage = background.AddComponent<Image>();
                        bgImage.color = new Color(0f, 0f, 0f, 0.7f); // Полупрозрачный черный

                        // Настраиваем размер и позицию фона
                        RectTransform bgRect = bgImage.rectTransform;
                        bgRect.anchorMin = new Vector2(0.2f, 0.8f);
                        bgRect.anchorMax = new Vector2(0.8f, 0.9f);
                        bgRect.offsetMin = Vector2.zero;
                        bgRect.offsetMax = Vector2.zero;

                        // Создаем текст сообщения
                        GameObject textObject = new GameObject("Message Text");
                        textObject.transform.SetParent(background.transform, false);

                        // Добавляем компонент текста
                        Text messageText = textObject.AddComponent<Text>();
                        messageText.text = ""; // Изначально пустой

                        // Используем любой доступный шрифт вместо Arial.ttf
                        Font font = null;

                        // Пробуем загрузить новый системный шрифт
                        try
                        {
                              font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                        }
                        catch (System.Exception)
                        {
                              Debug.LogWarning("LegacyRuntime.ttf не найден, ищу другие шрифты");
                        }

                        // Если не найден, ищем любой шрифт в ресурсах проекта
                        if (font == null)
                        {
                              Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
                              if (fonts.Length > 0)
                              {
                                    font = fonts[0];
                                    Debug.Log($"Использую первый доступный шрифт: {font.name}");
                              }
                        }

                        // Назначаем шрифт, если нашли
                        if (font != null)
                        {
                              messageText.font = font;
                        }
                        else
                        {
                              Debug.LogError("Не удалось найти ни одного шрифта!");
                        }

                        messageText.fontSize = 24;
                        messageText.alignment = TextAnchor.MiddleCenter;
                        messageText.color = Color.white;

                        // Настраиваем размер и позицию текста
                        RectTransform textRect = messageText.rectTransform;
                        textRect.anchorMin = Vector2.zero;
                        textRect.anchorMax = Vector2.one;
                        textRect.offsetMin = new Vector2(10, 5);
                        textRect.offsetMax = new Vector2(-10, -5);

                        // Скрываем панель до момента, когда нужно показать сообщение
                        messagePanel.SetActive(false);

                        Debug.Log("Панель сообщений успешно создана");
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Ошибка при создании панели сообщений: {e.Message}");

                        // Если не удалось создать панель сообщений, создадим простую альтернативу
                        CreateSimpleMessageDisplay();
                  }
            }

            // Создаем простую альтернативу для сообщений
            private void CreateSimpleMessageDisplay()
            {
                  Debug.Log("Создаю простую альтернативу для сообщений");

                  // Создаем пустой объект для сообщений
                  messagePanel = new GameObject("Simple Message Panel");
                  messagePanel.transform.SetParent(transform);

                  // Добавляем компонент TextMesh для отображения 3D текста в мире
                  TextMesh textMesh = messagePanel.AddComponent<TextMesh>();
                  textMesh.text = "";
                  textMesh.fontSize = 24;
                  textMesh.alignment = TextAlignment.Center;
                  textMesh.anchor = TextAnchor.MiddleCenter;
                  textMesh.color = Color.white;

                  // Установим размер текста, чтобы он был виден
                  textMesh.characterSize = 0.1f;

                  // Добавим компонент для того, чтобы текст всегда смотрел в камеру
                  messagePanel.AddComponent<Billboard>();

                  // Скрываем объект до момента вывода сообщения
                  messagePanel.SetActive(false);
            }

            // Отдельный класс для поворота объекта к камере
            public class Billboard : MonoBehaviour
            {
                  private Camera targetCamera;
                  private Transform cachedTransform;

                  void Start()
                  {
                        targetCamera = Camera.main;
                        cachedTransform = transform;

                        if (targetCamera == null)
                        {
                              Debug.LogError("Billboard: Не найдена основная камера!");
                              targetCamera = Camera.allCameras.Length > 0 ? Camera.allCameras[0] : null;

                              if (targetCamera != null)
                                    Debug.Log($"Billboard: Используется альтернативная камера {targetCamera.name}");
                        }
                  }

                  void Update()
                  {
                        if (targetCamera == null)
                        {
                              targetCamera = Camera.main;
                              if (targetCamera == null)
                              {
                                    return;
                              }
                        }

                        // Принудительно поворачиваем лицом к камере
                        Vector3 direction = targetCamera.transform.position - cachedTransform.position;

                        // Если направление не нулевое
                        if (direction != Vector3.zero)
                        {
                              // Поворачиваем маркер лицом к камере
                              cachedTransform.rotation = Quaternion.LookRotation(-direction);
                        }
                  }

                  void OnBecameInvisible()
                  {
                        // При исчезновении из вида камеры, выводим лог для отладки
                        Debug.Log($"Billboard {name} стал невидимым для камеры");
                  }

                  void OnBecameVisible()
                  {
                        // При появлении в поле зрения камеры, выводим лог для отладки
                        Debug.Log($"Billboard {name} стал видимым для камеры");
                  }
            }

            // Метод для отображения сообщения (с поддержкой разных типов панелей)
            private void ShowMessage(string message, float duration = 3f)
            {
                  // Если панель сообщений еще не создана, просто логируем сообщение
                  if (messagePanel == null)
                  {
                        Debug.Log($"Сообщение (панель не создана): {message}");
                        return;
                  }

                  try
                  {
                        // Активируем панель
                        messagePanel.SetActive(true);

                        // Сначала пробуем найти UI Text компонент
                        Text uiText = messagePanel.GetComponentInChildren<Text>();
                        if (uiText != null)
                        {
                              uiText.text = message;
                        }
                        else
                        {
                              // Если UI Text не найден, пробуем найти TextMesh
                              TextMesh textMesh = messagePanel.GetComponentInChildren<TextMesh>();
                              if (textMesh != null)
                              {
                                    textMesh.text = message;
                              }
                              else
                              {
                                    Debug.LogWarning($"Не найден компонент для отображения текста: {message}");
                              }
                        }

                        // Останавливаем предыдущий корутин, если он был запущен
                        if (messageCoroutine != null)
                        {
                              StopCoroutine(messageCoroutine);
                        }

                        // Запускаем корутин для скрытия сообщения через указанное время
                        messageCoroutine = StartCoroutine(HideMessageAfterDelay(duration));
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Ошибка при отображении сообщения: {e.Message}");
                  }
            }

            // Корутина для скрытия сообщения
            private IEnumerator HideMessageAfterDelay(float delay)
            {
                  yield return new WaitForSeconds(delay);
                  messagePanel.SetActive(false);
            }

            // Новый метод для настройки расширенных параметров обнаружения стен
            private void ConfigureAdvancedWallDetection(WallDetector detector)
            {
                  // Используем рефлексию для доступа к приватным полям класса WallDetector
                  var cannyThreshold1Field = detector.GetType().GetField("cannyThreshold1", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                  var cannyThreshold2Field = detector.GetType().GetField("cannyThreshold2", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                  var houghThresholdField = detector.GetType().GetField("houghThreshold", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                  var minLineLengthField = detector.GetType().GetField("minLineLength", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                  var maxLineGapField = detector.GetType().GetField("maxLineGap", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                  var showPerformanceStatsField = detector.GetType().GetField("showPerformanceStats", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                  var processingIntervalField = detector.GetType().GetField("processingInterval", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                  var useGPUAccelerationField = detector.GetType().GetField("useGPUAcceleration", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                  var webcamDeviceIndexField = detector.GetType().GetField("webcamDeviceIndex", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                  // Оптимизируем параметры для обнаружения стен
                  if (cannyThreshold1Field != null) cannyThreshold1Field.SetValue(detector, 30.0); // Более низкий порог для лучшего обнаружения краев
                  if (cannyThreshold2Field != null) cannyThreshold2Field.SetValue(detector, 90.0); // Более низкий высокий порог
                  if (houghThresholdField != null) houghThresholdField.SetValue(detector, 30); // Меньший порог для обнаружения линий
                  if (minLineLengthField != null) minLineLengthField.SetValue(detector, 50.0); // Меньшая минимальная длина линии
                  if (maxLineGapField != null) maxLineGapField.SetValue(detector, 20.0); // Больший допустимый разрыв между сегментами
                  if (showPerformanceStatsField != null) showPerformanceStatsField.SetValue(detector, true); // Включаем статистику производительности
                  if (processingIntervalField != null) processingIntervalField.SetValue(detector, 0.05f); // Обрабатываем кадры чаще
                  if (useGPUAccelerationField != null) useGPUAccelerationField.SetValue(detector, true); // Используем GPU ускорение

                  // Выбираем правильную камеру - индекс 0 для FaceTime, индекс 1 для iPhone
                  if (webcamDeviceIndexField != null) webcamDeviceIndexField.SetValue(detector, 0); // Используем фронтальную камеру

                  Debug.Log("Настроены оптимальные параметры для обнаружения контуров стен");
            }

            // Новый метод для точного вычисления границ стены на основе данных 3D точек
            private Bounds CalculateWallBounds(WallData wallData)
            {
                  // Получаем размеры стены из данных
                  Vector3 wallSize = wallData.scale;

                  // Создаем 8 угловых точек стены
                  Vector3 halfSize = wallSize * 0.5f;
                  Vector3[] corners = new Vector3[8];

                  corners[0] = new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);
                  corners[1] = new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
                  corners[2] = new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
                  corners[3] = new Vector3(halfSize.x, halfSize.y, -halfSize.z);
                  corners[4] = new Vector3(-halfSize.x, -halfSize.y, halfSize.z);
                  corners[5] = new Vector3(halfSize.x, -halfSize.y, halfSize.z);
                  corners[6] = new Vector3(-halfSize.x, halfSize.y, halfSize.z);
                  corners[7] = new Vector3(halfSize.x, halfSize.y, halfSize.z);

                  // Применяем вращение и позицию стены к каждой точке
                  for (int i = 0; i < corners.Length; i++)
                  {
                        corners[i] = wallData.rotation * corners[i] + wallData.position;
                  }

                  // Создаем ограничивающий бокс
                  Bounds bounds = new Bounds(corners[0], Vector3.zero);
                  foreach (Vector3 corner in corners)
                  {
                        bounds.Encapsulate(corner);
                  }

                  return bounds;
            }

            // Новый метод для эффекта реалистичной покраски на текстуре стены
            private void PaintWallTexture(GameObject wall, Color paintColor)
            {
                  // Получаем или создаем рендерер для стены
                  Renderer renderer = wall.GetComponent<Renderer>();
                  if (renderer == null) return;

                  // Проверяем текущий материал
                  Material currentMaterial = renderer.material;
                  if (currentMaterial == null) return;

                  // Создаем новый материал на основе Standard Shader для лучшего визуального эффекта
                  Material newMaterial = new Material(Shader.Find("Standard"));

                  // Копируем базовые свойства из текущего материала, если они есть
                  if (currentMaterial.HasProperty("_Color"))
                  {
                        newMaterial.color = currentMaterial.color;
                  }

                  // Если у стены нет текстуры, создаем новую реалистичную текстуру стены
                  Texture2D currentTexture = currentMaterial.mainTexture as Texture2D;
                  if (currentTexture == null || !currentMaterial.HasProperty("_MainTex"))
                  {
                        // Создаем текстуру для стены с базовым паттерном
                        Texture2D wallTexture = CreateRealisticWallTexture(1024, 1024);
                        currentTexture = wallTexture;
                  }
                  else
                  {
                        // Если текстура существует, создаем ее копию для модификации
                        currentTexture = CopyTexture(currentTexture);
                  }

                  // Применяем эффект покраски
                  Texture2D paintedTexture = ApplyPaintLayerToTexture(currentTexture, paintColor);

                  // Добавляем глянец и металличность для эффекта свежей краски
                  newMaterial.SetFloat("_Glossiness", 0.6f);
                  newMaterial.SetFloat("_Metallic", 0.2f);

                  // Добавляем свечение для выделения покрашенной поверхности
                  newMaterial.EnableKeyword("_EMISSION");
                  newMaterial.SetColor("_EmissionColor", paintColor * 0.3f);

                  // Устанавливаем текстуру
                  newMaterial.mainTexture = paintedTexture;

                  // Применяем новый материал к объекту
                  renderer.material = newMaterial;

                  // Сохраняем материал и для будущего использования
                  if (blueMaterial == null)
                  {
                        blueMaterial = newMaterial;
                  }

                  Debug.Log($"Применен реалистичный эффект покраски к объекту {wall.name}");
            }

            // Новый метод для создания реалистичной текстуры стены
            private Texture2D CreateRealisticWallTexture(int width, int height)
            {
                  Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                  Color[] pixels = new Color[width * height];

                  // Базовый цвет стены (светло-серый)
                  Color baseColor = new Color(0.9f, 0.9f, 0.9f, 1.0f);

                  // Добавляем шум Перлина для эффекта неровности стены
                  float scale1 = 0.03f; // Крупный шум для основной текстуры
                  float scale2 = 0.2f;  // Мелкий шум для деталей

                  for (int y = 0; y < height; y++)
                  {
                        for (int x = 0; x < width; x++)
                        {
                              int index = y * width + x;

                              // Генерируем многослойный шум Перлина для реалистичной текстуры
                              float noise1 = Mathf.PerlinNoise(x * scale1, y * scale1);
                              float noise2 = Mathf.PerlinNoise(x * scale2, y * scale2);

                              // Комбинируем шум разных масштабов
                              float combinedNoise = (noise1 * 0.7f + noise2 * 0.3f);

                              // Преобразуем в диапазон 0.85-1.0 для легкой вариации цвета
                              float colorVariation = 0.85f + combinedNoise * 0.15f;

                              // Создаем цвет пикселя с небольшими вариациями оттенка
                              pixels[index] = new Color(
                                    baseColor.r * colorVariation,
                                    baseColor.g * colorVariation,
                                    baseColor.b * colorVariation,
                                    1.0f
                              );

                              // Добавляем случайные "дефекты" стены для реализма
                              if (Random.Range(0f, 1f) < 0.001f)
                              {
                                    // Небольшие темные точки или царапины
                                    pixels[index] = new Color(
                                          baseColor.r * 0.7f,
                                          baseColor.g * 0.7f,
                                          baseColor.b * 0.7f,
                                          1.0f
                                    );
                              }
                        }
                  }

                  texture.SetPixels(pixels);
                  texture.Apply();

                  return texture;
            }

            // Метод для нанесения слоя краски на текстуру
            private Texture2D ApplyPaintLayerToTexture(Texture2D baseTexture, Color paintColor)
            {
                  // Создаем копию исходной текстуры
                  Texture2D paintedTexture = CopyTexture(baseTexture);
                  int width = paintedTexture.width;
                  int height = paintedTexture.height;
                  Color[] pixels = paintedTexture.GetPixels();

                  // Создаем маску покраски с неравномерным покрытием для реализма
                  float[,] paintMask = new float[width, height];

                  // Инициализируем маску базовым покрытием 
                  for (int y = 0; y < height; y++)
                  {
                        for (int x = 0; x < width; x++)
                        {
                              // Базовое покрытие с шумом для неравномерности
                              float noise = Mathf.PerlinNoise(x * 0.01f, y * 0.01f);
                              paintMask[x, y] = 0.7f + noise * 0.3f; // 70-100% покрытия

                              // Добавляем мелкие пузырьки воздуха (места, где краска не легла)
                              if (Random.Range(0f, 1f) < 0.005f)
                              {
                                    // Создаем маленький пузырек
                                    int bubbleSize = Random.Range(1, 4);
                                    for (int by = -bubbleSize; by <= bubbleSize; by++)
                                    {
                                          for (int bx = -bubbleSize; bx <= bubbleSize; bx++)
                                          {
                                                int px = x + bx;
                                                int py = y + by;
                                                if (px >= 0 && px < width && py >= 0 && py < height)
                                                {
                                                      float dist = Mathf.Sqrt(bx * bx + by * by);
                                                      if (dist <= bubbleSize)
                                                      {
                                                            // Уменьшаем покрытие в области пузырька
                                                            paintMask[px, py] *= (dist / bubbleSize);
                                                      }
                                                }
                                          }
                                    }
                              }

                              // Создаем подтеки краски в случайных местах
                              if (Random.Range(0f, 1f) < 0.0005f && y < height - 20)
                              {
                                    // Длина подтека
                                    int dripsLength = Random.Range(10, 30);
                                    float dripWidth = Random.Range(1f, 3f);

                                    // Создаем подтек вниз
                                    for (int dy = 0; dy < dripsLength; dy++)
                                    {
                                          int py = y + dy;
                                          if (py < height)
                                          {
                                                // Ширина подтека уменьшается книзу
                                                float currentWidth = dripWidth * (1f - (float)dy / dripsLength);

                                                for (int dx = -Mathf.FloorToInt(currentWidth); dx <= Mathf.CeilToInt(currentWidth); dx++)
                                                {
                                                      int px = x + dx;
                                                      if (px >= 0 && px < width)
                                                      {
                                                            // Интенсивность подтека уменьшается к краям и книзу
                                                            float intensity = (1f - Mathf.Abs(dx) / currentWidth) * (1f - (float)dy / dripsLength);
                                                            paintMask[px, py] = Mathf.Max(paintMask[px, py], intensity * 0.9f);
                                                      }
                                                }
                                          }
                                    }
                              }
                        }
                  }

                  // Применяем маску покраски к текстуре
                  for (int y = 0; y < height; y++)
                  {
                        for (int x = 0; x < width; x++)
                        {
                              int index = y * width + x;

                              // Вычисляем новый цвет пикселя с учетом маски покраски
                              float coverage = paintMask[x, y];
                              Color pixelColor = Color.Lerp(pixels[index], paintColor, coverage);

                              // Добавляем небольшие вариации в оттенок для реалистичности
                              float hueVariation = (Mathf.PerlinNoise(x * 0.05f, y * 0.05f) - 0.5f) * 0.05f;
                              float satVariation = (Mathf.PerlinNoise(x * 0.03f, y * 0.03f) - 0.5f) * 0.05f;

                              // Преобразуем в HSV для изменения оттенка
                              float h, s, v;
                              Color.RGBToHSV(pixelColor, out h, out s, out v);

                              // Применяем вариации
                              h += hueVariation;
                              s += satVariation;
                              h = Mathf.Repeat(h, 1f); // Оттенок должен быть в диапазоне 0-1
                              s = Mathf.Clamp01(s);    // Насыщенность должна быть в диапазоне 0-1

                              // Преобразуем обратно в RGB
                              pixelColor = Color.HSVToRGB(h, s, v);

                              // Устанавливаем новый цвет пикселя
                              pixels[index] = pixelColor;
                        }
                  }

                  paintedTexture.SetPixels(pixels);
                  paintedTexture.Apply();

                  return paintedTexture;
            }

            // Метод для копирования текстуры
            private Texture2D CopyTexture(Texture2D source)
            {
                  // Создаем копию текстуры с теми же размерами и форматом
                  RenderTexture renderTex = RenderTexture.GetTemporary(
                        source.width,
                        source.height,
                        0,
                        RenderTextureFormat.ARGB32
                  );

                  // Копируем исходную текстуру в рендертекстуру
                  Graphics.Blit(source, renderTex);

                  // Запоминаем текущую активную рендертекстуру
                  RenderTexture previous = RenderTexture.active;
                  RenderTexture.active = renderTex;

                  // Создаем новую текстуру и считываем пиксели из рендертекстуры
                  Texture2D copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                  copy.ReadPixels(new UnityEngine.Rect(0, 0, source.width, source.height), 0, 0);
                  copy.Apply();

                  // Восстанавливаем предыдущую активную рендертекстуру
                  RenderTexture.active = previous;
                  RenderTexture.ReleaseTemporary(renderTex);

                  return copy;
            }

            // Метод для нанесения эффекта покраски на текстуру
            private void ApplyPaintEffectToTexture(Texture2D texture, Color paintColor)
            {
                  int width = texture.width;
                  int height = texture.height;
                  Color[] pixels = texture.GetPixels();

                  // Применяем эффект покраски с шумом Перлина для реалистичности
                  for (int y = 0; y < height; y++)
                  {
                        for (int x = 0; x < width; x++)
                        {
                              int index = y * width + x;

                              // Добавляем шум Перлина для естественного эффекта
                              float noise = Mathf.PerlinNoise(x * 0.01f, y * 0.01f);
                              float edgeNoise = Mathf.PerlinNoise(x * 0.05f, y * 0.05f);

                              // Смешиваем цвет покраски с базовым цветом текстуры и добавляем вариации
                              Color pixelColor = Color.Lerp(pixels[index], paintColor, 0.9f + edgeNoise * 0.1f);

                              // Добавляем вариации яркости для реализма
                              float brightnessVariation = 0.95f + noise * 0.1f;
                              pixelColor = new Color(
                                    pixelColor.r * brightnessVariation,
                                    pixelColor.g * brightnessVariation,
                                    pixelColor.b * brightnessVariation,
                                    1.0f
                              );

                              pixels[index] = pixelColor;
                        }
                  }

                  // Применяем изменения к текстуре
                  texture.SetPixels(pixels);
                  texture.Apply();
            }

            // Метод для отображения визуальной обратной связи при клике
            private void ShowClickFeedback(Vector2 screenPosition)
            {
                  // Создаем временный UI элемент для обратной связи о клике
                  GameObject feedbackObj = new GameObject("ClickFeedback");
                  feedbackObj.transform.SetParent(transform);

                  // Создаем Canvas для отображения эффекта
                  Canvas canvas = feedbackObj.AddComponent<Canvas>();
                  canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                  canvas.sortingOrder = 100; // Отображаем поверх всего

                  // Добавляем изображение для обратной связи
                  GameObject imageObj = new GameObject("FeedbackImage");
                  imageObj.transform.SetParent(canvas.transform, false);

                  Image feedbackImage = imageObj.AddComponent<Image>();

                  // Создаем текстуру круга для эффекта клика
                  Texture2D circleTexture = CreateCircleTexture(128, new Color(1f, 1f, 1f, 0.7f));
                  Sprite circleSprite = Sprite.Create(
                        circleTexture,
                        new UnityEngine.Rect(0, 0, circleTexture.width, circleTexture.height),
                        new Vector2(0.5f, 0.5f)
                  );

                  feedbackImage.sprite = circleSprite;
                  feedbackImage.color = new Color(0.2f, 0.9f, 1f, 0.8f);

                  // Устанавливаем позицию и размер
                  RectTransform rectTransform = feedbackImage.rectTransform;
                  rectTransform.anchoredPosition = screenPosition - new Vector2(Screen.width / 2, Screen.height / 2);
                  rectTransform.sizeDelta = new Vector2(100, 100);

                  // Добавляем анимацию затухания
                  ClickFeedbackAnimation animation = feedbackObj.AddComponent<ClickFeedbackAnimation>();
                  animation.duration = 0.5f;

                  // Автоматически уничтожаем объект через 0.5 секунды
                  Destroy(feedbackObj, 0.5f);
            }

            // Класс для анимации эффекта клика
            public class ClickFeedbackAnimation : MonoBehaviour
            {
                  public float duration = 0.5f;
                  private float elapsedTime = 0f;
                  private Image feedbackImage;
                  private RectTransform rectTransform;

                  void Start()
                  {
                        feedbackImage = GetComponentInChildren<Image>();
                        if (feedbackImage != null)
                        {
                              rectTransform = feedbackImage.rectTransform;
                        }
                  }

                  void Update()
                  {
                        elapsedTime += Time.deltaTime;
                        float t = elapsedTime / duration;

                        if (feedbackImage != null)
                        {
                              // Уменьшаем прозрачность со временем
                              Color color = feedbackImage.color;
                              color.a = Mathf.Lerp(0.8f, 0f, t);
                              feedbackImage.color = color;

                              // Увеличиваем размер
                              float scale = Mathf.Lerp(1f, 2f, t);
                              rectTransform.localScale = new Vector3(scale, scale, 1f);
                        }
                  }
            }

            // Метод для создания панели с инструкцией для пользователя
            private void CreateHelpPanel()
            {
                  // Создаем родительский объект для UI
                  GameObject helpPanel = new GameObject("HelpPanel");

                  // Создаем канвас для UI
                  Canvas canvas = helpPanel.AddComponent<Canvas>();
                  canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                  canvas.sortingOrder = 10;
                  helpPanel.AddComponent<CanvasScaler>();
                  helpPanel.AddComponent<GraphicRaycaster>();

                  // Создаем панель с инструкцией
                  GameObject panel = new GameObject("InstructionPanel");
                  panel.transform.SetParent(canvas.transform, false);

                  // Добавляем фон
                  Image panelImage = panel.AddComponent<Image>();
                  panelImage.color = new Color(0, 0, 0, 0.8f); // Почти непрозрачный черный

                  // Настраиваем размер и позицию панели - вертикальная панель слева
                  RectTransform panelRect = panelImage.rectTransform;
                  panelRect.anchorMin = new Vector2(0.01f, 0.2f);
                  panelRect.anchorMax = new Vector2(0.25f, 0.9f);
                  panelRect.pivot = new Vector2(0.5f, 0.5f);
                  panelRect.offsetMin = Vector2.zero;
                  panelRect.offsetMax = Vector2.zero;

                  // Добавляем заголовок
                  GameObject titleObj = new GameObject("TitleText");
                  titleObj.transform.SetParent(panel.transform, false);

                  Text titleText = titleObj.AddComponent<Text>();
                  titleText.text = "DULUX VISUALIZER";
                  titleText.color = new Color(1f, 0.8f, 0.2f); // Золотистый цвет
                  titleText.fontSize = 28;
                  titleText.fontStyle = FontStyle.Bold;
                  titleText.alignment = TextAnchor.UpperCenter;

                  // Настраиваем размер и позицию заголовка
                  RectTransform titleRect = titleText.rectTransform;
                  titleRect.anchorMin = new Vector2(0.05f, 0.9f);
                  titleRect.anchorMax = new Vector2(0.95f, 1.0f);
                  titleRect.pivot = new Vector2(0.5f, 1.0f);
                  titleRect.offsetMin = Vector2.zero;
                  titleRect.offsetMax = Vector2.zero;

                  // Добавляем текст инструкции
                  GameObject textObj = new GameObject("HelpText");
                  textObj.transform.SetParent(panel.transform, false);

                  Text helpText = textObj.AddComponent<Text>();
                  helpText.text =
                        "УПРАВЛЕНИЕ:\n\n" +
                        "[ T ] - создать тестовые стены\n\n" +
                        "[ ПРОБЕЛ ] - покрасить все стены\n\n" +
                        "[ F1 ] - создать одну стену\n\n" +
                        "[ C ] - переключить камеру\n\n\n" +
                        "Если стены не видны, нажмите\nклавишу T для их пересоздания\n\n" +
                        "Камера показана в нижнем\nправом углу экрана";

                  helpText.color = Color.white;
                  helpText.fontSize = 20;
                  helpText.fontStyle = FontStyle.Bold;
                  helpText.alignment = TextAnchor.UpperLeft;
                  helpText.lineSpacing = 1.2f; // Увеличиваем интервал между строками

                  // Настраиваем размер и позицию текста
                  RectTransform textRect = helpText.rectTransform;
                  textRect.anchorMin = new Vector2(0.05f, 0.1f);
                  textRect.anchorMax = new Vector2(0.95f, 0.85f);
                  textRect.pivot = new Vector2(0.5f, 0.5f);
                  textRect.offsetMin = Vector2.zero;
                  textRect.offsetMax = Vector2.zero;

                  // Добавляем контрастную рамку вокруг панели
                  GameObject border = new GameObject("PanelBorder");
                  border.transform.SetParent(panel.transform, false);
                  border.transform.SetAsFirstSibling(); // Помещаем под остальными элементами

                  Image borderImage = border.AddComponent<Image>();
                  borderImage.color = new Color(1f, 0.8f, 0.2f, 0.7f); // Золотистая рамка

                  RectTransform borderRect = borderImage.rectTransform;
                  borderRect.anchorMin = new Vector2(0, 0);
                  borderRect.anchorMax = new Vector2(1, 1);
                  borderRect.offsetMin = new Vector2(-3, -3);
                  borderRect.offsetMax = new Vector2(3, 3);

                  // Добавляем кнопку скрытия/показа панели
                  GameObject toggleButton = new GameObject("ToggleButton");
                  toggleButton.transform.SetParent(panel.transform, false);

                  Image buttonImage = toggleButton.AddComponent<Image>();
                  buttonImage.color = new Color(1f, 0f, 0f, 0.8f); // Ярко-красный

                  // Настраиваем размер и позицию кнопки
                  RectTransform buttonRect = buttonImage.rectTransform;
                  buttonRect.anchorMin = new Vector2(1, 1);
                  buttonRect.anchorMax = new Vector2(1, 1);
                  buttonRect.pivot = new Vector2(1, 1);
                  buttonRect.sizeDelta = new Vector2(40, 40);
                  buttonRect.anchoredPosition = new Vector2(-5, -5);

                  // Добавляем обработчик нажатия
                  Button button = toggleButton.AddComponent<Button>();
                  button.targetGraphic = buttonImage;

                  // Создаем эффект при наведении
                  ColorBlock colors = button.colors;
                  colors.highlightedColor = new Color(1f, 0.5f, 0.5f);
                  colors.pressedColor = new Color(0.7f, 0f, 0f);
                  button.colors = colors;

                  // Создаем текст для кнопки
                  GameObject buttonTextObj = new GameObject("ButtonText");
                  buttonTextObj.transform.SetParent(toggleButton.transform, false);

                  Text buttonText = buttonTextObj.AddComponent<Text>();
                  buttonText.text = "X";
                  buttonText.color = Color.white;
                  buttonText.fontSize = 24;
                  buttonText.alignment = TextAnchor.MiddleCenter;
                  buttonText.fontStyle = FontStyle.Bold;

                  // Настраиваем размер и позицию текста кнопки
                  RectTransform buttonTextRect = buttonText.rectTransform;
                  buttonTextRect.anchorMin = new Vector2(0, 0);
                  buttonTextRect.anchorMax = new Vector2(1, 1);
                  buttonTextRect.pivot = new Vector2(0.5f, 0.5f);
                  buttonTextRect.offsetMin = Vector2.zero;
                  buttonTextRect.offsetMax = Vector2.zero;

                  // Добавляем обработчик нажатия
                  button.onClick.AddListener(() =>
                  {
                        // При первом скрытии запоминаем позицию и уменьшаем панель
                        if (helpText.gameObject.activeSelf)
                        {
                              // Сохраняем текущее положение панели
                              Vector2 currentAnchorMin = panelRect.anchorMin;
                              Vector2 currentAnchorMax = panelRect.anchorMax;

                              // Сворачиваем панель до маленького значка в верхнем левом углу
                              panelRect.anchorMin = new Vector2(0.01f, 0.85f);
                              panelRect.anchorMax = new Vector2(0.07f, 0.95f);

                              // Скрываем все элементы, кроме кнопки
                              helpText.gameObject.SetActive(false);
                              titleText.gameObject.SetActive(false);

                              // Меняем текст кнопки
                              buttonText.text = "?";
                        }
                        else
                        {
                              // Восстанавливаем полный размер панели
                              panelRect.anchorMin = new Vector2(0.01f, 0.2f);
                              panelRect.anchorMax = new Vector2(0.25f, 0.9f);

                              // Показываем все элементы
                              helpText.gameObject.SetActive(true);
                              titleText.gameObject.SetActive(true);

                              // Возвращаем текст кнопки
                              buttonText.text = "X";
                        }
                  });

                  Debug.Log("Создана панель с инструкцией для пользователя");

                  // Через 30 секунд автоматически сворачиваем панель
                  StartCoroutine(AutoCollapseHelpPanel(button, 30f));
            }

            // Корутина для автоматического сворачивания панели подсказок через указанное время
            private IEnumerator AutoCollapseHelpPanel(Button collapseButton, float delay)
            {
                  yield return new WaitForSeconds(delay);
                  collapseButton.onClick.Invoke();
            }

            private IEnumerator HidePanelAfterDelay(GameObject panel, float delay)
            {
                  yield return new WaitForSeconds(delay);
                  panel.SetActive(false);
            }

            // Метод для поворота камеры в правильное положение, чтобы увидеть тестовые стены
            private void RotateCameraToFaceTestWalls()
            {
                  if (mainCamera == null) return;

                  // Устанавливаем стандартную позицию для просмотра тестовых стен
                  Vector3 bestPosition = new Vector3(0, 1.7f, 0); // Примерно в центре комнаты, на уровне глаз человека
                  mainCamera.transform.position = bestPosition;

                  // Устанавливаем стандартный поворот, смотрящий вперед на переднюю стену комнаты
                  mainCamera.transform.rotation = Quaternion.Euler(0, 0, 0);

                  // Добавим компонент управления камерой, если его нет
                  SimpleCameraController cameraController = mainCamera.GetComponent<SimpleCameraController>();
                  if (cameraController == null)
                  {
                        cameraController = mainCamera.gameObject.AddComponent<SimpleCameraController>();
                  }

                  // Показываем подсказку пользователю
                  ShowMessage("Камера установлена в центр тестовой комнаты. Используйте WASD для перемещения и стрелки для поворота.", 7.0f);

                  Debug.Log($"Камера установлена в оптимальную позицию {mainCamera.transform.position} с поворотом {mainCamera.transform.rotation.eulerAngles}");
            }

            // Метод для переключения между доступными камерами
            private void ToggleCamera()
            {
                  if (wallDetector == null) return;

                  // Используем рефлексию для доступа к приватным полям класса WallDetector
                  var webcamDeviceIndexField = wallDetector.GetType().GetField("webcamDeviceIndex", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                  if (webcamDeviceIndexField != null)
                  {
                        int currentIndex = (int)webcamDeviceIndexField.GetValue(wallDetector);
                        int newIndex = currentIndex == 0 ? 1 : 0; // Переключаемся между камерами 0 и 1

                        webcamDeviceIndexField.SetValue(wallDetector, newIndex);

                        // Перезапускаем определение стен для применения изменений
                        wallDetector.StopDetection();
                        wallDetector.StartDetection();

                        string cameraName = newIndex == 0 ? "фронтальную (FaceTime)" : "заднюю (iPhone)";
                        ShowMessage($"Переключено на {cameraName} камеру", 2f);
                        Debug.Log($"Переключение на камеру с индексом {newIndex}");
                  }
            }

            // Метод для добавления заметной текстовой метки на стену
            private void AddWallLabel(Transform parent, WallData wall)
            {
                  // Создаем объект для текста
                  GameObject labelObj = new GameObject("WallLabel");
                  labelObj.transform.SetParent(parent);

                  // Смещаем немного вперед, чтобы текст был перед стеной
                  labelObj.transform.localPosition = new Vector3(0, 0, -0.1f);

                  // Добавляем TextMesh компонент
                  TextMesh textMesh = labelObj.AddComponent<TextMesh>();
                  textMesh.text = $"СТЕНА #{parent.parent.childCount}\nРазмер: {wall.scale.x:F1}x{wall.scale.y:F1}м\nНажмите ПРОБЕЛ для покраски";
                  textMesh.fontSize = 100; // Очень крупный шрифт
                  textMesh.alignment = TextAlignment.Center;
                  textMesh.anchor = TextAnchor.MiddleCenter;
                  textMesh.color = Color.yellow; // Яркий цвет

                  // Увеличиваем размер символов
                  textMesh.characterSize = 0.05f;

                  // Добавляем компонент для поворота к камере
                  labelObj.AddComponent<Billboard>();
            }

            // Метод для отображения очень большого заметного сообщения на экране
            private void ShowBigScreenMessage(string message, float duration = 5.0f)
            {
                  // Создаем родительский объект для сообщения
                  GameObject messageObj = new GameObject("BigScreenMessage");

                  // Добавляем Canvas
                  Canvas canvas = messageObj.AddComponent<Canvas>();
                  canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                  canvas.sortingOrder = 999; // Отображать поверх всего

                  // Добавляем компоненты для корректной работы Canvas
                  messageObj.AddComponent<CanvasScaler>();
                  messageObj.AddComponent<GraphicRaycaster>();

                  // Создаем фон
                  GameObject background = new GameObject("Background");
                  background.transform.SetParent(canvas.transform, false);

                  // Добавляем Image компонент для фона
                  Image bgImage = background.AddComponent<Image>();
                  bgImage.color = new Color(0, 0, 0, 0.7f); // Полупрозрачный черный фон

                  // Настраиваем размер фона на весь экран
                  RectTransform bgRect = bgImage.rectTransform;
                  bgRect.anchorMin = new Vector2(0.2f, 0.7f);  // Показываем только в верхней части экрана
                  bgRect.anchorMax = new Vector2(0.8f, 0.95f);
                  bgRect.offsetMin = Vector2.zero;
                  bgRect.offsetMax = Vector2.zero;

                  // Создаем текст
                  GameObject textObj = new GameObject("MessageText");
                  textObj.transform.SetParent(background.transform, false);

                  // Добавляем Text компонент
                  Text text = textObj.AddComponent<Text>();
                  text.text = message;
                  text.fontSize = 28; // Хороший размер шрифта
                  text.fontStyle = FontStyle.Bold;
                  text.alignment = TextAnchor.MiddleCenter;
                  text.color = Color.white;

                  // Настраиваем размер текста по центру экрана
                  RectTransform textRect = text.rectTransform;
                  textRect.anchorMin = new Vector2(0.05f, 0.05f);
                  textRect.anchorMax = new Vector2(0.95f, 0.95f);
                  textRect.offsetMin = Vector2.zero;
                  textRect.offsetMax = Vector2.zero;

                  // Автоматически уничтожаем объект через указанное время
                  Destroy(messageObj, duration);

                  Debug.Log($"<color=red><b>ВАЖНОЕ СООБЩЕНИЕ:</b> {message}</color>");
            }

            // Метод для настройки отображения контуров на видеопотоке камеры
            private void EnableContoursOnCamera(WallDetector detector)
            {
                  // Используем рефлексию для доступа к приватным полям
                  var showDebugLinesField = detector.GetType().GetField("showDebugLines", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                  var debugLineColorField = detector.GetType().GetField("debugLineColor", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                  var debugLineThicknessField = detector.GetType().GetField("debugLineThickness", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                  var fillContoursField = detector.GetType().GetField("fillContours", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                  // Устанавливаем параметры отображения
                  if (showDebugLinesField != null)
                        showDebugLinesField.SetValue(detector, true); // Включаем отображение контуров

                  if (debugLineColorField != null)
                        debugLineColorField.SetValue(detector, Color.magenta); // Яркий цвет для линий

                  if (debugLineThicknessField != null)
                        debugLineThicknessField.SetValue(detector, 5); // Толстые линии

                  if (fillContoursField != null)
                        fillContoursField.SetValue(detector, true); // Заполнять контуры цветом

                  Debug.Log("Включено отображение контуров на видеопотоке камеры");
            }

            // Метод для переключения режима отображения камеры
            public void SetCameraViewMode(bool fullscreen)
            {
                  isFullScreenCamera = fullscreen;

                  if (cameraPreview == null)
                  {
                        Debug.LogWarning("Camera preview is not available");
                        return;
                  }

                  RectTransform rectTransform = cameraPreview.GetComponent<RectTransform>();
                  if (rectTransform == null)
                  {
                        Debug.LogWarning("RectTransform not found on camera preview");
                        return;
                  }

                  // Сохраняем текущий масштаб без изменения
                  Vector3 currentScale = rectTransform.localScale;

                  if (fullscreen)
                  {
                        // Полноэкранный режим:
                        // - Размер на весь экран
                        // - Позиция по центру
                        // - Более прозрачный для видимости стен
                        rectTransform.anchorMin = Vector2.zero;
                        rectTransform.anchorMax = Vector2.one;
                        rectTransform.sizeDelta = Vector2.zero;
                        rectTransform.anchoredPosition = Vector2.zero;
                        cameraPreview.color = new Color(1, 1, 1, 0.7f); // Полупрозрачный для видимости стен

                        // Обновляем видимость маркеров стен
                        UpdateWallMarkersVisibility(true);

                        ShowMessage("Полноэкранный режим камеры", 1.5f);
                  }
                  else
                  {
                        // Режим предпросмотра:
                        // - Маленький размер в углу
                        // - Меньший приоритет отображения
                        rectTransform.anchorMin = new Vector2(0.7f, 0);
                        rectTransform.anchorMax = new Vector2(1, 0.3f);
                        rectTransform.sizeDelta = Vector2.zero;
                        rectTransform.anchoredPosition = Vector2.zero;
                        cameraPreview.color = Color.white; // Полная непрозрачность

                        // Возвращаем нормальную видимость маркеров стен
                        UpdateWallMarkersVisibility(false);

                        ShowMessage("Режим предпросмотра камеры", 1.5f);
                  }
            }

            // Метод для обновления видимости маркеров стен в зависимости от режима камеры
            private void UpdateWallMarkersVisibility(bool fullscreenMode)
            {
                  if (wallMarkers == null || wallMarkers.Count == 0)
                        return;

                  foreach (GameObject marker in wallMarkers)
                  {
                        if (marker == null) continue;

                        // Получаем все рендереры в маркере
                        Renderer[] renderers = marker.GetComponentsInChildren<Renderer>();

                        foreach (Renderer renderer in renderers)
                        {
                              if (renderer == null || renderer.material == null) continue;

                              // В полноэкранном режиме делаем маркеры ярче
                              if (fullscreenMode)
                              {
                                    // Увеличиваем яркость и насыщенность материалов
                                    Color currentColor = renderer.material.color;
                                    renderer.material.color = new Color(
                                        Mathf.Min(currentColor.r * 1.5f, 1f),
                                        Mathf.Min(currentColor.g * 1.5f, 1f),
                                        Mathf.Min(currentColor.b * 1.5f, 1f),
                                        currentColor.a
                                    );
                              }
                              else
                              {
                                    // Возвращаем нормальную яркость
                                    Color currentColor = renderer.material.color;
                                    if (currentColor.r > 0.7f || currentColor.g > 0.7f || currentColor.b > 0.7f)
                                    {
                                          renderer.material.color = new Color(
                                              currentColor.r / 1.5f,
                                              currentColor.g / 1.5f,
                                              currentColor.b / 1.5f,
                                              currentColor.a
                                          );
                                    }
                              }
                        }

                        // Улучшаем видимость текстовых меток
                        TextMesh[] textMeshes = marker.GetComponentsInChildren<TextMesh>();
                        foreach (TextMesh textMesh in textMeshes)
                        {
                              if (textMesh == null) continue;

                              if (fullscreenMode)
                              {
                                    // Делаем текст ярче и крупнее в полноэкранном режиме
                                    textMesh.color = Color.white;
                                    textMesh.characterSize = 0.15f;
                              }
                              else
                              {
                                    // Возвращаем обычный вид в режиме предпросмотра
                                    textMesh.color = new Color(0.9f, 0.9f, 0.0f);
                                    textMesh.characterSize = 0.1f;
                              }
                        }
                  }
            }

            // Метод для переключения режима отображения камеры
            private void ToggleCameraViewMode()
            {
                  // Переключаем режим отображения камеры
                  bool newMode = !isFullScreenCamera;
                  SetCameraViewMode(newMode);
            }

            private void PaintOnWall(GameObject wall, Vector2 position, Color color)
            {
                  // Получаем или создаем материал для рисования
                  MeshRenderer renderer = wall.GetComponent<MeshRenderer>();
                  if (renderer == null) return;

                  // Проверяем, есть ли у стены текстура
                  Texture2D paintTexture = renderer.material.mainTexture as Texture2D;

                  // Если текстуры нет, создаем новую
                  if (paintTexture == null)
                  {
                        paintTexture = new Texture2D(512, 512, TextureFormat.RGBA32, false);
                        // Заполняем текстуру прозрачным белым цветом
                        Color[] colors = new Color[512 * 512];
                        for (int i = 0; i < colors.Length; i++)
                        {
                              colors[i] = new Color(1f, 1f, 1f, 0.1f);
                        }
                        paintTexture.SetPixels(colors);
                        paintTexture.Apply();

                        // Устанавливаем текстуру материалу
                        renderer.material.mainTexture = paintTexture;
                  }

                  // Конвертируем позицию из текстурных координат в пиксели
                  int x = Mathf.FloorToInt(position.x * paintTexture.width);
                  int y = Mathf.FloorToInt(position.y * paintTexture.height);

                  // Рисуем кружок
                  int brushSize = 10;
                  for (int i = -brushSize; i <= brushSize; i++)
                  {
                        for (int j = -brushSize; j <= brushSize; j++)
                        {
                              int pixelX = x + i;
                              int pixelY = y + j;

                              // Проверяем, находится ли пиксель в пределах текстуры
                              if (pixelX >= 0 && pixelX < paintTexture.width && pixelY >= 0 && pixelY < paintTexture.height)
                              {
                                    // Проверяем, что пиксель внутри круга кисти
                                    if (i * i + j * j <= brushSize * brushSize)
                                    {
                                          paintTexture.SetPixel(pixelX, pixelY, color);
                                    }
                              }
                        }
                  }

                  // Применяем изменения
                  paintTexture.Apply();
            }

            // Простой компонент для управления камерой в тестовой сцене
            public class SimpleCameraController : MonoBehaviour
            {
                  private float moveSpeed = 2.0f;
                  private float rotateSpeed = 120.0f;
                  private bool showInfo = true;
                  private float lastInfoTime = 0;

                  void Start()
                  {
                        // Показываем инструкцию при старте
                        ShowControlsInfo();
                  }

                  void Update()
                  {
                        // Обработка перемещения
                        float horizontal = 0;
                        float vertical = 0;

                        if (Input.GetKey(KeyCode.W)) vertical += 1;
                        if (Input.GetKey(KeyCode.S)) vertical -= 1;
                        if (Input.GetKey(KeyCode.A)) horizontal -= 1;
                        if (Input.GetKey(KeyCode.D)) horizontal += 1;

                        float actualMoveSpeed = moveSpeed;
                        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                        {
                              actualMoveSpeed *= 2.0f; // Ускорение при Shift
                        }

                        // Перемещаем камеру
                        Vector3 movement = new Vector3(horizontal, 0, vertical) * actualMoveSpeed * Time.deltaTime;
                        transform.Translate(movement);

                        // Вертикальное перемещение с Q и E
                        if (Input.GetKey(KeyCode.Q)) transform.Translate(Vector3.up * actualMoveSpeed * Time.deltaTime);
                        if (Input.GetKey(KeyCode.E)) transform.Translate(Vector3.down * actualMoveSpeed * Time.deltaTime);

                        // Обработка вращения
                        float rotateHorizontal = 0;
                        float rotateVertical = 0;

                        if (Input.GetKey(KeyCode.LeftArrow)) rotateHorizontal -= 1;
                        if (Input.GetKey(KeyCode.RightArrow)) rotateHorizontal += 1;
                        if (Input.GetKey(KeyCode.UpArrow)) rotateVertical += 1;
                        if (Input.GetKey(KeyCode.DownArrow)) rotateVertical -= 1;

                        // Поворачиваем камеру
                        transform.Rotate(Vector3.up, rotateHorizontal * rotateSpeed * Time.deltaTime);
                        transform.Rotate(Vector3.right, rotateVertical * rotateSpeed * Time.deltaTime);

                        // Сброс положения по нажатию R
                        if (Input.GetKeyDown(KeyCode.R))
                        {
                              transform.position = new Vector3(0, 1.7f, 0);
                              transform.rotation = Quaternion.Euler(0, 0, 0);
                              Debug.Log("Положение камеры сброшено к началу координат");
                        }

                        // Периодически показываем управление
                        if (Time.time - lastInfoTime > 30 && showInfo)
                        {
                              ShowControlsInfo();
                              lastInfoTime = Time.time;
                        }
                  }

                  private void ShowControlsInfo()
                  {
                        // Отображаем инструкцию в верхнем левом углу
                        GameObject infoObj = new GameObject("ControlsInfo");
                        infoObj.transform.SetParent(transform);

                        // Создаем Canvas
                        Canvas canvas = infoObj.AddComponent<Canvas>();
                        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                        infoObj.AddComponent<CanvasScaler>();
                        infoObj.AddComponent<GraphicRaycaster>();

                        // Создаем панель с инструкцией
                        GameObject panel = new GameObject("InfoPanel");
                        panel.transform.SetParent(canvas.transform, false);

                        // Добавляем фон
                        Image bgImage = panel.AddComponent<Image>();
                        bgImage.color = new Color(0, 0, 0, 0.7f);

                        // Настраиваем размер и позицию
                        RectTransform panelRect = bgImage.rectTransform;
                        panelRect.anchorMin = new Vector2(0, 0.85f);
                        panelRect.anchorMax = new Vector2(0.3f, 1);
                        panelRect.offsetMin = Vector2.zero;
                        panelRect.offsetMax = Vector2.zero;

                        // Создаем текст инструкции
                        GameObject textObj = new GameObject("InfoText");
                        textObj.transform.SetParent(panel.transform, false);

                        Text infoText = textObj.AddComponent<Text>();
                        infoText.text =
                              "УПРАВЛЕНИЕ КАМЕРОЙ:\n" +
                              "WASD - перемещение\n" +
                              "Q/E - вверх/вниз\n" +
                              "Стрелки - поворот\n" +
                              "Shift - ускорение\n" +
                              "R - сброс позиции";

                        infoText.color = Color.white;
                        infoText.fontSize = 16;
                        infoText.fontStyle = FontStyle.Bold;

                        // Настраиваем размер и позицию текста
                        RectTransform textRect = infoText.rectTransform;
                        textRect.anchorMin = Vector2.zero;
                        textRect.anchorMax = Vector2.one;
                        textRect.offsetMin = new Vector2(10, 5);
                        textRect.offsetMax = new Vector2(-10, -5);

                        // Автоматическое уничтожение через 10 секунд
                        Destroy(infoObj, 10f);
                  }
            }
      }
}