using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UtilsModule;
using System.Linq;

namespace Remalux.AR
{
      /// <summary>
      /// Класс для обнаружения стен с помощью OpenCV
      /// </summary>
      public class OpenCVWallDetector : MonoBehaviour
      {
            [Header("AR Components")]
            [SerializeField] private ARCameraManager cameraManager;
            [SerializeField] private ARRaycastManager raycastManager;
            [SerializeField] private WallPainter wallPainter;

            [Header("OpenCV Settings")]
            [SerializeField] private float minWallArea = 5000f; // Минимальная площадь стены в пикселях
            [SerializeField] private float maxWallArea = 100000f; // Максимальная площадь стены в пикселях
            [SerializeField] private float approxPolyEpsilon = 15f; // Точность аппроксимации контура
            [SerializeField] private int cannyThreshold1 = 50; // Нижний порог для алгоритма Canny
            [SerializeField] private int cannyThreshold2 = 150; // Верхний порог для алгоритма Canny
            [SerializeField] private bool drawDebug = true; // Отображать ли отладочную информацию на экране

            [Header("Custom Wall")]
            [SerializeField] private GameObject wallPrefab; // Префаб для создания стены
            [SerializeField] private Material defaultWallMaterial; // Материал по умолчанию для стены

            [Header("Debug")]
            [SerializeField] private bool createTestWallOnStart = false;
            [SerializeField] private KeyCode testWallKey = KeyCode.T;

            // Приватные переменные
            private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();
            private Texture2D cameraTexture;
            private Mat srcMat;
            private Mat grayMat;
            private Mat cannyMat;
            private Mat hierarchy;
            private GameObject currentDetectedWall;
            private List<MatOfPoint> contours;
            private bool isProcessingFrame = false;
            private Vector2[] lastDetectedWallCorners; // Углы последней обнаруженной стены в пикселях

            // Публичные свойства
            public bool IsWallDetected => currentDetectedWall != null;

            void Awake()
            {
                  Debug.Log("[OpenCVWallDetector] Инициализация компонента");

                  // Проверка и получение необходимых компонентов
                  if (cameraManager == null)
                  {
                        cameraManager = UnityEngine.Object.FindFirstObjectByType<ARCameraManager>();
                        Debug.Log($"[OpenCVWallDetector] Найден ARCameraManager: {(cameraManager != null)}");
                  }

                  if (raycastManager == null)
                  {
                        raycastManager = UnityEngine.Object.FindFirstObjectByType<ARRaycastManager>();
                        Debug.Log($"[OpenCVWallDetector] Найден ARRaycastManager: {(raycastManager != null)}");
                  }

                  if (wallPainter == null)
                  {
                        wallPainter = UnityEngine.Object.FindFirstObjectByType<WallPainter>();
                        Debug.Log($"[OpenCVWallDetector] Найден WallPainter: {(wallPainter != null)}");
                  }

                  // Инициализация OpenCV матриц
                  try
                  {
                        srcMat = new Mat();
                        grayMat = new Mat();
                        cannyMat = new Mat();
                        hierarchy = new Mat();
                        contours = new List<MatOfPoint>();
                        Debug.Log("[OpenCVWallDetector] OpenCV матрицы инициализированы успешно");
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"[OpenCVWallDetector] Ошибка при инициализации OpenCV: {e.Message}");
                  }
            }

            void OnEnable()
            {
                  if (cameraManager != null)
                  {
                        cameraManager.frameReceived += OnCameraFrameReceived;
                        Debug.Log("[OpenCVWallDetector] Подписка на событие frameReceived");
                  }
                  else
                  {
                        Debug.LogError("[OpenCVWallDetector] Не удалось подписаться на событие frameReceived: cameraManager = null");
                  }
            }

            void OnDisable()
            {
                  if (cameraManager != null)
                  {
                        cameraManager.frameReceived -= OnCameraFrameReceived;
                  }
            }

            /// <summary>
            /// Обработчик события получения кадра с камеры AR
            /// </summary>
            void OnCameraFrameReceived(ARCameraFrameEventArgs args)
            {
                  if (isProcessingFrame)
                        return;

                  isProcessingFrame = true;

                  // Получаем текущее изображение с камеры
                  if (!TryGetCameraImage(out XRCpuImage image))
                  {
                        isProcessingFrame = false;
                        return;
                  }

                  // Конвертируем изображение в текстуру
                  ConvertImageToTexture(image, ref cameraTexture);
                  image.Dispose();

                  // Обрабатываем изображение с помощью OpenCV
                  ProcessImageWithOpenCV();

                  isProcessingFrame = false;
            }

            /// <summary>
            /// Попытка получить изображение с камеры AR
            /// </summary>
            bool TryGetCameraImage(out XRCpuImage image)
            {
                  image = default;
                  if (cameraManager == null || !cameraManager.TryAcquireLatestCpuImage(out image))
                        return false;

                  return true;
            }

            /// <summary>
            /// Конвертирует XRCpuImage в Texture2D
            /// </summary>
            void ConvertImageToTexture(XRCpuImage image, ref Texture2D texture)
            {
                  // Настройка параметров конвертации
                  XRCpuImage.ConversionParams conversionParams = new XRCpuImage.ConversionParams
                  {
                        inputRect = new RectInt(0, 0, image.width, image.height),
                        outputDimensions = new Vector2Int(image.width, image.height),
                        outputFormat = TextureFormat.RGBA32,
                        transformation = XRCpuImage.Transformation.MirrorY
                  };

                  // Создаем текстуру, если ее еще нет или изменились размеры
                  if (texture == null || texture.width != image.width || texture.height != image.height)
                  {
                        texture = new Texture2D(image.width, image.height, TextureFormat.RGBA32, false);
                  }

                  // Используем NativeArray для работы с изображением
                  using (var rawData = new Unity.Collections.NativeArray<byte>(
                        image.GetConvertedDataSize(conversionParams),
                        Unity.Collections.Allocator.Temp))
                  {
                        // Конвертируем изображение
                        image.Convert(conversionParams, rawData);

                        // Обновляем текстуру данными из NativeArray
                        texture.LoadRawTextureData(rawData);
                        texture.Apply();
                  }
            }

            /// <summary>
            /// Обрабатывает изображение с помощью OpenCV для обнаружения стен
            /// </summary>
            void ProcessImageWithOpenCV()
            {
                  if (cameraTexture == null)
                        return;

                  try
                  {
                        // Конвертируем текстуру в Mat
                        Utils.texture2DToMat(cameraTexture, srcMat);

                        // Добавим предварительную обработку для улучшения обнаружения
                        // Уменьшаем шум с помощью GaussianBlur
                        Mat blurredMat = new Mat();
                        Imgproc.GaussianBlur(srcMat, blurredMat, new Size(5, 5), 0);

                        // Преобразуем в оттенки серого
                        Imgproc.cvtColor(blurredMat, grayMat, Imgproc.COLOR_RGBA2GRAY);

                        // Можно усилить контраст, если нужно
                        Core.normalize(grayMat, grayMat, 0, 255, Core.NORM_MINMAX);

                        // Обнаружение краев с использованием Canny
                        Imgproc.Canny(grayMat, cannyMat, cannyThreshold1, cannyThreshold2);

                        // Применяем морфологическое закрытие для соединения близких краев
                        Mat kernel = Imgproc.getStructuringElement(Imgproc.MORPH_RECT, new Size(3, 3));
                        Mat closedMat = new Mat();
                        Imgproc.morphologyEx(cannyMat, closedMat, Imgproc.MORPH_CLOSE, kernel);

                        // Находим контуры
                        contours.Clear();
                        Imgproc.findContours(closedMat, contours, hierarchy, Imgproc.RETR_EXTERNAL, Imgproc.CHAIN_APPROX_SIMPLE);

                        Debug.Log($"[OpenCVWallDetector] Найдено контуров: {contours.Count}");

                        // Ищем самый подходящий контур для стены (прямоугольник)
                        DetectWallFromContours();

                        // Отображаем отладочную информацию
                        if (drawDebug)
                        {
                              DrawDebugOverlay();
                        }

                        // Освобождаем временные ресурсы
                        blurredMat.Dispose();
                        closedMat.Dispose();
                        kernel.Dispose();
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"[OpenCVWallDetector] Ошибка при обработке OpenCV: {e.Message}\n{e.StackTrace}");
                  }
            }

            /// <summary>
            /// Ищет контур, соответствующий стене
            /// </summary>
            void DetectWallFromContours()
            {
                  if (contours.Count == 0)
                        return;

                  MatOfPoint bestWallContour = null;
                  MatOfPoint2f approxCurve = new MatOfPoint2f();
                  double largestArea = 0;
                  Vector2[] bestCorners = null;

                  List<MatOfPoint> filteredContours = new List<MatOfPoint>();

                  foreach (var contour in contours)
                  {
                        // Вычисляем площадь контура
                        double area = Imgproc.contourArea(contour);

                        // Пропускаем слишком маленькие или слишком большие контуры
                        if (area < minWallArea || area > maxWallArea)
                              continue;

                        // Получаем выпуклую оболочку для более надежной работы с формой контура
                        MatOfInt hull = new MatOfInt();
                        Imgproc.convexHull(contour, hull);

                        // Преобразуем индексы выпуклой оболочки в точки
                        MatOfPoint mopHull = new MatOfPoint();
                        mopHull.create((int)hull.size().height, 1, CvType.CV_32SC2);

                        Point[] contourArray = contour.toArray();
                        Point[] hullPoints = new Point[hull.toArray().Length];
                        for (int i = 0; i < hull.toArray().Length; i++)
                        {
                              hullPoints[i] = contourArray[hull.toArray()[i]];
                        }
                        mopHull.fromArray(hullPoints);

                        // Добавляем улучшенный контур в отфильтрованный список
                        filteredContours.Add(mopHull);

                        // Аппроксимируем контур для поиска прямоугольников
                        MatOfPoint2f contour2f = new MatOfPoint2f(mopHull.toArray());
                        Imgproc.approxPolyDP(contour2f, approxCurve, approxPolyEpsilon, true);

                        int vertexCount = (int)approxCurve.total();

                        // Стена должна быть аппроксимирована как четырехугольник (4 угла)
                        // Или допускаем контуры с близким к 4 числом вершин
                        if ((vertexCount == 4 || (vertexCount >= 3 && vertexCount <= 6)) && area > largestArea)
                        {
                              largestArea = area;
                              bestWallContour = contour;

                              // Если получили неточно 4 угла, преобразуем контур в четырехугольник
                              if (vertexCount != 4)
                              {
                                    Debug.Log($"[OpenCVWallDetector] Найден контур с {vertexCount} вершинами, преобразую к четырехугольнику");

                                    // Находим ограничивающий прямоугольник для преобразования в 4 точки
                                    RotatedRect rect = Imgproc.minAreaRect(new MatOfPoint2f(mopHull.toArray()));
                                    Point[] rectPoints = new Point[4];
                                    rect.points(rectPoints);

                                    // Конвертируем углы в Unity координаты (перевернутые по Y)
                                    bestCorners = new Vector2[4];
                                    for (int i = 0; i < 4; i++)
                                    {
                                          bestCorners[i] = new Vector2((float)rectPoints[i].x, cameraTexture.height - (float)rectPoints[i].y);
                                    }
                              }
                              else
                              {
                                    // Конвертируем углы в Unity координаты (перевернутые по Y)
                                    Point[] points = approxCurve.toArray();
                                    bestCorners = new Vector2[4];
                                    for (int i = 0; i < 4; i++)
                                    {
                                          bestCorners[i] = new Vector2((float)points[i].x, cameraTexture.height - (float)points[i].y);
                                    }
                              }

                              // Сортируем углы по часовой стрелке, начиная с верхнего левого
                              if (bestCorners != null)
                              {
                                    bestCorners = SortCornersClockwise(bestCorners);
                              }
                        }

                        // Освобождаем ресурсы
                        hull.Dispose();
                        mopHull.Dispose();
                        contour2f.Dispose();
                  }

                  // Обновляем список контуров для отладочного отображения
                  contours.Clear();
                  contours.AddRange(filteredContours);

                  // Если найден подходящий контур, создаем стену
                  if (bestWallContour != null && bestCorners != null)
                  {
                        Debug.Log($"[OpenCVWallDetector] Найден подходящий контур для стены: {bestCorners[0]}, {bestCorners[1]}, {bestCorners[2]}, {bestCorners[3]}");
                        lastDetectedWallCorners = bestCorners;
                        CreateWallFromContour(bestCorners);
                  }

                  // Освобождаем ресурсы
                  approxCurve.Dispose();
            }

            /// <summary>
            /// Сортирует углы четырехугольника по часовой стрелке, начиная с верхнего левого
            /// </summary>
            private Vector2[] SortCornersClockwise(Vector2[] corners)
            {
                  if (corners == null || corners.Length != 4)
                        return corners;

                  // Находим центр четырехугольника
                  Vector2 center = new Vector2(0, 0);
                  foreach (var corner in corners)
                  {
                        center += corner;
                  }
                  center /= 4;

                  // Сортируем углы по углу относительно центра (по часовой стрелке)
                  System.Array.Sort(corners, (a, b) =>
                  {
                        return Mathf.Atan2(a.y - center.y, a.x - center.x)
                             .CompareTo(Mathf.Atan2(b.y - center.y, b.x - center.x));
                  });

                  return corners;
            }

            /// <summary>
            /// Создает 3D-стену из 2D-контура
            /// </summary>
            async void CreateWallFromContour(Vector2[] screenCorners)
            {
                  if (screenCorners == null || screenCorners.Length != 4)
                        return;

                  Debug.Log($"[OpenCVWallDetector] Начинаю проекцию 2D углов в 3D: {string.Join(", ", screenCorners)}");

                  // Выполняем raycast для каждого угла, чтобы получить 3D-координаты
                  List<Vector3> worldCorners = new List<Vector3>();

                  // Используем AR Foundation Raycast для проекции точек в 3D
                  foreach (var screenPoint in screenCorners)
                  {
                        Debug.Log($"[OpenCVWallDetector] Выполняю raycast для экранной точки: {screenPoint}");

                        bool hitFound = false;
                        if (raycastManager.Raycast(screenPoint, raycastHits, TrackableType.FeaturePoint | TrackableType.Planes))
                        {
                              worldCorners.Add(raycastHits[0].pose.position);
                              Debug.Log($"[OpenCVWallDetector] Raycast успешен: {raycastHits[0].pose.position}");
                              hitFound = true;
                        }

                        // Если raycast не сработал, пробуем альтернативный способ через камеру
                        if (!hitFound)
                        {
                              Debug.Log("[OpenCVWallDetector] Raycast не сработал, использую альтернативный метод через камеру");

                              // Направление луча из камеры через экранную точку
                              Vector3 viewportPoint = Camera.main.ScreenToViewportPoint(new Vector3(screenPoint.x, screenPoint.y, 0));
                              Ray ray = Camera.main.ViewportPointToRay(viewportPoint);

                              // Проецируем точку на расстояние перед камерой
                              float distanceFromCamera = 2.0f; // Можно настроить это значение
                              Vector3 worldPoint = ray.origin + ray.direction * distanceFromCamera;

                              worldCorners.Add(worldPoint);
                              Debug.Log($"[OpenCVWallDetector] Создана точка альтернативным методом: {worldPoint}");
                        }

                        // Небольшая задержка между raycast-ами
                        await System.Threading.Tasks.Task.Delay(10);
                  }

                  if (worldCorners.Count == 4)
                  {
                        Debug.Log($"[OpenCVWallDetector] Получены все 4 точки для постройки стены: {string.Join(", ", worldCorners)}");
                        BuildWallMesh(worldCorners);
                  }
                  else
                  {
                        Debug.LogWarning($"[OpenCVWallDetector] Не удалось создать все 4 точки (получено: {worldCorners.Count})");
                  }
            }

            /// <summary>
            /// Строит меш стены по 3D-координатам углов
            /// </summary>
            void BuildWallMesh(List<Vector3> corners)
            {
                  // Удаляем предыдущую стену, если она есть
                  if (currentDetectedWall != null)
                  {
                        Destroy(currentDetectedWall);
                  }

                  // Создаем новый объект для стены
                  if (wallPrefab != null)
                  {
                        currentDetectedWall = Instantiate(wallPrefab, Vector3.zero, Quaternion.identity);
                  }
                  else
                  {
                        currentDetectedWall = new GameObject("DetectedWall");
                  }

                  // Добавляем компоненты меша
                  MeshFilter meshFilter = currentDetectedWall.GetComponent<MeshFilter>();
                  if (meshFilter == null)
                        meshFilter = currentDetectedWall.AddComponent<MeshFilter>();

                  MeshRenderer meshRenderer = currentDetectedWall.GetComponent<MeshRenderer>();
                  if (meshRenderer == null)
                        meshRenderer = currentDetectedWall.AddComponent<MeshRenderer>();

                  // Создаем меш стены
                  Mesh mesh = new Mesh();
                  mesh.vertices = corners.ToArray();

                  // Рассчитываем правильную ориентацию треугольников
                  // Проверяем, не нужно ли перевернуть порядок вершин для правильной отрисовки
                  Vector3 normal = Vector3.Cross(corners[1] - corners[0], corners[2] - corners[0]).normalized;
                  if (Vector3.Dot(normal, Vector3.forward) < 0)  // Если нормаль смотрит от камеры
                  {
                        mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };  // Инвертированный порядок
                  }
                  else
                  {
                        mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };  // Обычный порядок
                  }

                  // Создаем UV координаты
                  mesh.uv = new Vector2[]
                  {
                        new Vector2(0, 0),
                        new Vector2(1, 0),
                        new Vector2(1, 1),
                        new Vector2(0, 1)
                  };

                  mesh.RecalculateNormals();
                  mesh.RecalculateBounds();

                  // Назначаем меш и материал
                  meshFilter.mesh = mesh;
                  if (defaultWallMaterial != null)
                  {
                        meshRenderer.material = defaultWallMaterial;
                  }
                  else
                  {
                        // Если материал не задан, создаем базовый материал
                        meshRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        meshRenderer.material.color = new Color(0.8f, 0.8f, 0.8f, 1.0f);
                  }

                  // Выводим подробную информацию в лог
                  Debug.Log($"[OpenCVWallDetector] Создана стена с углами: {string.Join(", ", corners)}");
                  Debug.Log($"[OpenCVWallDetector] Имя объекта: {currentDetectedWall.name}, позиция: {currentDetectedWall.transform.position}");

                  // Задаем активность объекта
                  currentDetectedWall.SetActive(true);

                  // Уведомляем WallPainter о новой стене
                  if (wallPainter != null)
                  {
                        wallPainter.SendMessage("OnWallDetected", currentDetectedWall, SendMessageOptions.DontRequireReceiver);
                        Debug.Log($"[OpenCVWallDetector] Отправлено сообщение WallPainter об обнаруженной стене");
                  }
                  else
                  {
                        Debug.LogWarning("[OpenCVWallDetector] WallPainter не найден, не удалось отправить сообщение");
                  }
            }

            /// <summary>
            /// Рисует отладочную информацию поверх исходного изображения
            /// </summary>
            void DrawDebugOverlay()
            {
                  // Рисуем все обнаруженные контуры
                  Imgproc.drawContours(srcMat, contours, -1, new Scalar(0, 255, 0, 255), 2);

                  // Рисуем углы последней обнаруженной стены
                  if (lastDetectedWallCorners != null)
                  {
                        for (int i = 0; i < lastDetectedWallCorners.Length; i++)
                        {
                              Point pt = new Point(lastDetectedWallCorners[i].x, cameraTexture.height - lastDetectedWallCorners[i].y);
                              Imgproc.circle(srcMat, pt, 10, new Scalar(255, 0, 0, 255), -1);
                        }
                  }

                  // Конвертируем Mat обратно в текстуру для отображения
                  Texture2D debugTexture = new Texture2D(srcMat.cols(), srcMat.rows(), TextureFormat.RGBA32, false);
                  Utils.matToTexture2D(srcMat, debugTexture);

                  // Обновляем отладочное изображение через OpenCVDebugView
                  OpenCVDebugView debugView = UnityEngine.Object.FindFirstObjectByType<OpenCVDebugView>();
                  if (debugView != null)
                  {
                        debugView.SetDebugTexture(debugTexture);
                  }
            }

            /// <summary>
            /// Получает текущую обнаруженную стену
            /// </summary>
            public GameObject GetDetectedWall()
            {
                  return currentDetectedWall;
            }

            /// <summary>
            /// Устанавливает параметры обнаружения стен
            /// </summary>
            public void SetDetectionParameters(DetectionParameters parameters)
            {
                  cannyThreshold1 = parameters.CannyThreshold1;
                  cannyThreshold2 = parameters.CannyThreshold2;
                  minWallArea = parameters.MinWallArea;
                  maxWallArea = parameters.MaxWallArea;
                  approxPolyEpsilon = parameters.ApproxPolyEpsilon;

                  Debug.Log($"Параметры обнаружения стен обновлены: Canny={cannyThreshold1}/{cannyThreshold2}, " +
                            $"Площадь={minWallArea}-{maxWallArea}, Точность={approxPolyEpsilon}");
            }

            /// <summary>
            /// Освобождает ресурсы при уничтожении объекта
            /// </summary>
            void OnDestroy()
            {
                  // Освобождаем ресурсы OpenCV
                  if (srcMat != null) srcMat.Dispose();
                  if (grayMat != null) grayMat.Dispose();
                  if (cannyMat != null) cannyMat.Dispose();
                  if (hierarchy != null) hierarchy.Dispose();

                  foreach (var contour in contours)
                  {
                        if (contour != null) contour.Dispose();
                  }
                  contours.Clear();
            }

            /// <summary>
            /// Принудительно создает тестовую стену для отладки
            /// </summary>
            public void CreateTestWall()
            {
                  // Создаем заглушку углов стены в пространстве
                  Vector3 cameraPosition = Camera.main.transform.position;
                  Vector3 cameraForward = Camera.main.transform.forward;
                  Vector3 cameraRight = Camera.main.transform.right;
                  Vector3 cameraUp = Camera.main.transform.up;

                  // Создаем плоскость перед камерой
                  float distance = 2.0f;
                  float width = 1.5f;
                  float height = 1.0f;

                  List<Vector3> corners = new List<Vector3>
                  {
                        cameraPosition + cameraForward * distance - cameraRight * width/2 - cameraUp * height/2,  // Нижний левый
                        cameraPosition + cameraForward * distance + cameraRight * width/2 - cameraUp * height/2,  // Нижний правый
                        cameraPosition + cameraForward * distance + cameraRight * width/2 + cameraUp * height/2,  // Верхний правый
                        cameraPosition + cameraForward * distance - cameraRight * width/2 + cameraUp * height/2   // Верхний левый
                  };

                  Debug.Log($"[OpenCVWallDetector] Создаю тестовую стену перед камерой: {string.Join(", ", corners)}");
                  BuildWallMesh(corners);
            }

            void Update()
            {
                  // Если нажата клавиша тестовой стены
                  if (Input.GetKeyDown(testWallKey))
                  {
                        Debug.Log("[OpenCVWallDetector] Создание тестовой стены по запросу");
                        CreateTestWall();
                  }
            }

            void Start()
            {
                  Debug.Log("[OpenCVWallDetector] Начало работы компонента");

                  // Если включен режим тестовой стены, создаем стену сразу после запуска
                  if (createTestWallOnStart)
                  {
                        Debug.Log("[OpenCVWallDetector] Создание тестовой стены при запуске");
                        CreateTestWall();
                  }

                  // Принудительно создаем тестовую стену через 3 секунды после запуска
                  Invoke("CreateTestWall", 3.0f);
            }
      }
}