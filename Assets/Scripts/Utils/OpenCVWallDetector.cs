using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UtilsModule;
using System.Linq;
using System.Collections;

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
            [SerializeField] private ARPlaneVisibilityController planeController;

            [Header("OpenCV Settings")]
            [SerializeField] private float minWallArea = 5000f; // Минимальная площадь стены в пикселях
            [SerializeField] private float maxWallArea = 100000f; // Максимальная площадь стены в пикселях
            [SerializeField] private float approxPolyEpsilon = 15f; // Точность аппроксимации контура
            [SerializeField] private int cannyThreshold1 = 50; // Нижний порог для алгоритма Canny
            [SerializeField] private int cannyThreshold2 = 150; // Верхний порог для алгоритма Canny
            [SerializeField] private bool drawDebug = true; // Отображать ли отладочную информацию на экране
            [SerializeField] private float detectionInterval = 1.0f;
            [SerializeField] private float minimumWallConfidence = 0.7f;
            [SerializeField] private float minimumWallSize = 0.5f;

            [Header("Custom Wall")]
            [SerializeField] private GameObject wallPrefab; // Префаб для создания стены
            [SerializeField] private Material defaultWallMaterial; // Материал по умолчанию для стены

            [Header("Debug")]
            [SerializeField] private bool createTestWallOnStart = false;
            [SerializeField] private KeyCode testWallKey = KeyCode.T;
            [SerializeField] private bool simulateWallDetection = true; // Флаг для симуляции обнаружения стен

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
            private bool isProcessing = false;

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

                  if (planeController == null)
                  {
                        planeController = FindFirstObjectByType<ARPlaneVisibilityController>();
                        if (planeController == null)
                        {
                              Debug.LogError("Не найден ARPlaneVisibilityController");
                              enabled = false;
                              return;
                        }
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
                  {
                        Debug.LogWarning("[OpenCVWallDetector] cameraTexture is null");
                        return;
                  }

                  try
                  {
                        // Преобразуем текстуру в Mat для OpenCV
                        Utils.texture2DToMat(cameraTexture, srcMat);

                        // Преобразуем в оттенки серого для лучшего обнаружения контуров
                        Imgproc.cvtColor(srcMat, grayMat, Imgproc.COLOR_RGBA2GRAY);

                        // Применяем размытие для уменьшения шума
                        Imgproc.GaussianBlur(grayMat, grayMat, new Size(5, 5), 0);

                        // Применяем алгоритм Canny для обнаружения краев
                        Imgproc.Canny(grayMat, cannyMat, cannyThreshold1, cannyThreshold2);

                        // Находим контуры
                        contours.Clear();
                        Imgproc.findContours(cannyMat, contours, hierarchy, Imgproc.RETR_EXTERNAL, Imgproc.CHAIN_APPROX_SIMPLE);

                        Debug.Log($"[OpenCVWallDetector] Найдено {contours.Count} контуров");

                        // Отрисовываем контуры на изображении для отладки
                        if (drawDebug)
                        {
                              Imgproc.cvtColor(grayMat, srcMat, Imgproc.COLOR_GRAY2RGBA);
                              for (int i = 0; i < contours.Count; i++)
                              {
                                    Imgproc.drawContours(srcMat, contours, i, new Scalar(255, 0, 0, 255), 2);
                              }
                              Utils.matToTexture2D(srcMat, cameraTexture);
                        }

                        // Ищем потенциальные стены среди контуров
                        DetectWallFromContours();

                        // Преобразуем обнаруженные контуры в AR плоскости
                        ConvertDetectedWallsToARPlaneFormat();
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"[OpenCVWallDetector] Ошибка при обработке изображения: {e.Message}");
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
                  Debug.Log("[OpenCVWallDetector] Компонент запущен");

                  if (planeController == null)
                  {
                        planeController = FindFirstObjectByType<ARPlaneVisibilityController>();
                        Debug.Log($"[OpenCVWallDetector] Найден ARPlaneVisibilityController: {(planeController != null)}");
                  }

                  // Принудительно создаем тестовую стену через 3 секунды после запуска
                  if (createTestWallOnStart)
                  {
                        Invoke("CreateTestWall", 3.0f);
                  }

                  // Запускаем симуляцию обнаружения стен для быстрой демонстрации
                  Invoke("SimulateInitialWallDetection", 2.0f);

                  // Запускаем корутину для периодического обнаружения стен
                  StartCoroutine(WallDetectionRoutine());

                  Debug.Log("[OpenCVWallDetector] Двойной подход к обнаружению стен активирован: AR Foundation + OpenCV");
            }

            /// <summary>
            /// Симулирует первоначальное обнаружение стен для быстрой демонстрации
            /// </summary>
            private void SimulateInitialWallDetection()
            {
                  // Используем симуляцию для быстрой демонстрации даже до реальной обработки видео
                  if (planeController != null)
                  {
                        SimulateWallDetection();
                        Debug.Log("Начальная симуляция стен завершена");
                  }
            }

            private IEnumerator WallDetectionRoutine()
            {
                  yield return new WaitForSeconds(2f); // Ждем инициализацию AR

                  while (true)
                  {
                        if (!isProcessing)
                        {
                              isProcessing = true;

                              if (simulateWallDetection)
                              {
                                    // Симулируем обнаружение стен в Unity Editor для тестирования
                                    SimulateWallDetection();
                              }
                              else
                              {
                                    // Получаем текущий кадр с камеры
                                    if (cameraManager != null && cameraManager.TryAcquireLatestCpuImage(out var image))
                                    {
                                          Debug.Log("Получен кадр с камеры, размер: " + image.width + "x" + image.height);

                                          // Здесь будет вызов OpenCV функций для обнаружения стен
                                          // ProcessImageWithOpenCV(image);

                                          image.Dispose();
                                    }
                              }

                              isProcessing = false;
                        }

                        yield return new WaitForSeconds(detectionInterval);
                  }
            }

            private void SimulateWallDetection()
            {
                  Debug.Log("Симуляция обнаружения стен с помощью OpenCV");

                  List<ARPlaneVisibilityController.OpenCVWallData> simulatedWalls = new List<ARPlaneVisibilityController.OpenCVWallData>();

                  // Получаем текущую позицию камеры
                  Vector3 cameraPosition = Camera.main.transform.position;
                  Vector3 cameraForward = Camera.main.transform.forward;
                  Vector3 cameraRight = Camera.main.transform.right;
                  Vector3 cameraUp = Camera.main.transform.up;

                  // Проводим несколько рейкастов для обнаружения реальных поверхностей
                  bool foundSurface = false;

                  // Проверяем поверхности спереди от пользователя
                  RaycastHit frontHit;
                  if (Physics.Raycast(cameraPosition, cameraForward, out frontHit, 5.0f))
                  {
                        // Обнаружена поверхность впереди
                        Vector3 wallPosition = frontHit.point;
                        Vector3 wallNormal = frontHit.normal;

                        // Определяем тип поверхности
                        bool isVertical = Mathf.Abs(Vector3.Dot(wallNormal, Vector3.up)) < 0.3f;

                        // Вычисляем приблизительный размер поверхности
                        float width = isVertical ? 2.0f : 3.0f;
                        float height = isVertical ? 2.0f : 2.0f;

                        // Задаем уровень уверенности, который должен быть больше минимального порога
                        float confidence = Mathf.Max(0.95f, minimumWallConfidence + 0.1f);

                        ARPlaneVisibilityController.OpenCVWallData frontWall = new ARPlaneVisibilityController.OpenCVWallData(
                            wallPosition,
                            wallNormal,
                            new Vector2(width, height),
                            confidence,
                            1
                        );
                        simulatedWalls.Add(frontWall);
                        Debug.Log($"Обнаружена реальная поверхность спереди: позиция {wallPosition}, нормаль {wallNormal}");
                        foundSurface = true;
                  }

                  // Проверяем поверхность справа от пользователя
                  RaycastHit rightHit;
                  if (Physics.Raycast(cameraPosition, cameraRight, out rightHit, 5.0f))
                  {
                        // Обнаружена поверхность справа
                        Vector3 wallPosition = rightHit.point;
                        Vector3 wallNormal = rightHit.normal;

                        // Определяем тип поверхности
                        bool isVertical = Mathf.Abs(Vector3.Dot(wallNormal, Vector3.up)) < 0.3f;

                        // Вычисляем приблизительный размер поверхности
                        float width = isVertical ? 2.0f : 3.0f;
                        float height = isVertical ? 2.0f : 2.0f;

                        // Задаем уровень уверенности, который должен быть больше минимального порога
                        float confidence = Mathf.Max(0.9f, minimumWallConfidence + 0.05f);

                        ARPlaneVisibilityController.OpenCVWallData rightWall = new ARPlaneVisibilityController.OpenCVWallData(
                            wallPosition,
                            wallNormal,
                            new Vector2(width, height),
                            confidence,
                            2
                        );
                        simulatedWalls.Add(rightWall);
                        Debug.Log($"Обнаружена реальная поверхность справа: позиция {wallPosition}, нормаль {wallNormal}");
                        foundSurface = true;
                  }

                  // Проверяем поверхность слева от пользователя
                  RaycastHit leftHit;
                  if (Physics.Raycast(cameraPosition, -cameraRight, out leftHit, 5.0f))
                  {
                        // Обнаружена поверхность слева
                        Vector3 wallPosition = leftHit.point;
                        Vector3 wallNormal = leftHit.normal;

                        // Определяем тип поверхности
                        bool isVertical = Mathf.Abs(Vector3.Dot(wallNormal, Vector3.up)) < 0.3f;

                        // Вычисляем приблизительный размер поверхности
                        float width = isVertical ? 2.0f : 3.0f;
                        float height = isVertical ? 2.0f : 2.0f;

                        // Задаем уровень уверенности, который должен быть больше минимального порога
                        float confidence = Mathf.Max(0.85f, minimumWallConfidence + 0.05f);

                        ARPlaneVisibilityController.OpenCVWallData leftWall = new ARPlaneVisibilityController.OpenCVWallData(
                            wallPosition,
                            wallNormal,
                            new Vector2(width, height),
                            confidence,
                            3
                        );
                        simulatedWalls.Add(leftWall);
                        Debug.Log($"Обнаружена реальная поверхность слева: позиция {wallPosition}, нормаль {wallNormal}");
                        foundSurface = true;
                  }

                  // Проверяем пол
                  RaycastHit floorHit;
                  if (Physics.Raycast(cameraPosition, -Vector3.up, out floorHit, 3.0f))
                  {
                        // Обнаружен пол
                        Vector3 floorPosition = floorHit.point;
                        Vector3 floorNormal = floorHit.normal;

                        // Задаем уровень уверенности, который должен быть больше минимального порога
                        float confidence = Mathf.Max(0.9f, minimumWallConfidence + 0.05f);

                        ARPlaneVisibilityController.OpenCVWallData floor = new ARPlaneVisibilityController.OpenCVWallData(
                            floorPosition,
                            floorNormal,
                            new Vector2(4.0f, 4.0f),
                            confidence,
                            4
                        );
                        simulatedWalls.Add(floor);
                        Debug.Log($"Обнаружен пол: позиция {floorPosition}, нормаль {floorNormal}");
                        foundSurface = true;
                  }

                  // Если не нашли ни одной реальной поверхности, создаем стены в относительных координатах
                  if (!foundSurface)
                  {
                        Debug.Log("Не обнаружено реальных поверхностей, создаем стены относительно камеры");

                        // Симулируем обнаружение стены прямо перед камерой
                        Vector3 wallPosition = cameraPosition + cameraForward * 2.0f; // 2 метра перед камерой
                        Vector3 wallNormal = -cameraForward; // Нормаль смотрит в сторону камеры

                        // Задаем уровень уверенности
                        float firstWallConfidence = Mathf.Max(0.9f, minimumWallConfidence + 0.1f);

                        ARPlaneVisibilityController.OpenCVWallData frontWall = new ARPlaneVisibilityController.OpenCVWallData(
                            wallPosition,
                            wallNormal,
                            new Vector2(2.0f, 2.0f),
                            firstWallConfidence,
                            1
                        );
                        simulatedWalls.Add(frontWall);

                        // Симулируем обнаружение стены справа от камеры
                        Vector3 rightWallPosition = cameraPosition + cameraRight * 2.0f + cameraForward * 1.0f;
                        Vector3 rightWallNormal = -cameraRight;

                        // Задаем уровень уверенности
                        float secondWallConfidence = Mathf.Max(0.8f, minimumWallConfidence + 0.05f);

                        ARPlaneVisibilityController.OpenCVWallData rightWall = new ARPlaneVisibilityController.OpenCVWallData(
                            rightWallPosition,
                            rightWallNormal,
                            new Vector2(1.5f, 1.8f),
                            secondWallConfidence,
                            2
                        );
                        simulatedWalls.Add(rightWall);

                        Debug.Log($"Созданы симулированные стены без привязки к реальным поверхностям");
                  }

                  // Отправляем данные в контроллер плоскостей
                  if (simulatedWalls.Count > 0)
                  {
                        planeController.ProcessOpenCVWalls(simulatedWalls);
                        Debug.Log($"Отправлено {simulatedWalls.Count} поверхностей в обработку");
                  }
            }

            /// <summary>
            /// Преобразует обнаруженные с помощью OpenCV контуры в формат данных для ARPlaneVisibilityController
            /// </summary>
            private void ConvertDetectedWallsToARPlaneFormat()
            {
                  if (contours == null || contours.Count == 0)
                  {
                        Debug.Log("Нет контуров для преобразования");
                        return;
                  }

                  List<ARPlaneVisibilityController.OpenCVWallData> wallsData = new List<ARPlaneVisibilityController.OpenCVWallData>();

                  for (int i = 0; i < contours.Count; i++)
                  {
                        MatOfPoint contour = contours[i];
                        if (contour == null || contour.empty())
                              continue;

                        // Получаем площадь контура
                        double area = Imgproc.contourArea(contour);

                        // Проверяем, соответствует ли площадь нашим критериям
                        if (area < minWallArea || area > maxWallArea)
                              continue;

                        // Аппроксимируем контур для получения более простой формы
                        MatOfPoint2f contour2f = new MatOfPoint2f();
                        contour.convertTo(contour2f, CvType.CV_32F);

                        MatOfPoint2f approxCurve = new MatOfPoint2f();
                        double epsilon = approxPolyEpsilon * Imgproc.arcLength(contour2f, true);
                        Imgproc.approxPolyDP(contour2f, approxCurve, epsilon, true);

                        // Преобразуем обратно в MatOfPoint
                        MatOfPoint approxContour = new MatOfPoint();
                        approxCurve.convertTo(approxContour, CvType.CV_32S);

                        Point[] points = approxContour.toArray();

                        // Нам нужны только контуры с 4 точками (примерно прямоугольные)
                        if (points.Length == 4)
                        {
                              // Преобразуем точки экрана в мировые координаты
                              Vector2[] screenCorners = new Vector2[4];
                              for (int j = 0; j < 4; j++)
                              {
                                    screenCorners[j] = new Vector2((float)points[j].x, (float)points[j].y);
                              }

                              // Сортируем углы по часовой стрелке
                              screenCorners = SortCornersClockwise(screenCorners);

                              // Тут мы должны преобразовать экранные координаты в мировые
                              List<Vector3> worldCorners = new List<Vector3>();
                              bool allCornersValid = true;

                              foreach (Vector2 screenPos in screenCorners)
                              {
                                    // Используем raycast для определения позиции в мире
                                    if (raycastManager != null && raycastManager.Raycast(screenPos, raycastHits, TrackableType.PlaneWithinPolygon | TrackableType.PlaneEstimated))
                                    {
                                          if (raycastHits.Count > 0)
                                          {
                                                worldCorners.Add(raycastHits[0].pose.position);
                                          }
                                          else
                                          {
                                                allCornersValid = false;
                                                break;
                                          }
                                    }
                                    else
                                    {
                                          allCornersValid = false;
                                          break;
                                    }
                              }

                              if (allCornersValid && worldCorners.Count == 4)
                              {
                                    // Вычисляем центр стены
                                    Vector3 center = (worldCorners[0] + worldCorners[1] + worldCorners[2] + worldCorners[3]) / 4f;

                                    // Вычисляем нормаль (предполагаем, что стена плоская)
                                    Vector3 edge1 = worldCorners[1] - worldCorners[0];
                                    Vector3 edge2 = worldCorners[2] - worldCorners[1];
                                    Vector3 normal = Vector3.Cross(edge1, edge2).normalized;

                                    // Убеждаемся, что нормаль направлена в сторону камеры
                                    Vector3 toCameraDir = (Camera.main.transform.position - center).normalized;
                                    if (Vector3.Dot(normal, toCameraDir) < 0)
                                    {
                                          normal = -normal;
                                    }

                                    // Вычисляем размеры стены
                                    float width = Vector3.Distance(worldCorners[0], worldCorners[1]);
                                    float height = Vector3.Distance(worldCorners[1], worldCorners[2]);

                                    // Создаем данные для стены
                                    ARPlaneVisibilityController.OpenCVWallData wallData = new ARPlaneVisibilityController.OpenCVWallData(
                                        center,
                                        normal,
                                        new Vector2(width, height),
                                        CalculateWallConfidence(area, width, height),
                                        i // используем индекс контура как ID стены
                                    );

                                    wallsData.Add(wallData);
                                    Debug.Log($"Преобразован контур {i} в данные стены: позиция {center}, размер {width}x{height}");
                              }
                        }
                  }

                  // Если нашли стены, отправляем их в ARPlaneVisibilityController
                  if (wallsData.Count > 0 && planeController != null)
                  {
                        // Фильтруем стены по уровню уверенности
                        List<ARPlaneVisibilityController.OpenCVWallData> filteredWalls =
                              wallsData.Where(w => w.confidence >= minimumWallConfidence).ToList();

                        if (filteredWalls.Count > 0)
                        {
                              Debug.Log($"Отправляем {filteredWalls.Count} стен в ARPlaneVisibilityController (отфильтровано {wallsData.Count - filteredWalls.Count} с низкой уверенностью)");
                              planeController.ProcessOpenCVWalls(filteredWalls);
                        }
                        else
                        {
                              Debug.Log($"Найдено {wallsData.Count} стен, но все они имеют уверенность ниже порога {minimumWallConfidence}");
                        }
                  }
            }

            /// <summary>
            /// Рассчитывает уверенность в обнаружении стены на основе различных параметров
            /// </summary>
            private float CalculateWallConfidence(double area, float width, float height)
            {
                  // Проверяем соотношение сторон (хорошая стена должна быть примерно прямоугольной)
                  float aspectRatio = width / height;
                  float aspectConfidence = 1.0f;

                  if (aspectRatio < 0.2f || aspectRatio > 5.0f)
                  {
                        aspectConfidence = 0.5f; // Странное соотношение сторон
                  }

                  // Проверяем размер (слишком маленькие или большие стены менее надежны)
                  float sizeConfidence = Mathf.Clamp01(
                        Mathf.Min(
                              width / minimumWallSize,
                              height / minimumWallSize,
                              5.0f / width,
                              5.0f / height
                        )
                  );

                  // Проверяем площадь контура
                  float areaConfidence = Mathf.Clamp01((float)((area - minWallArea) / (maxWallArea - minWallArea)));

                  // Взвешиваем все факторы
                  float confidence = (aspectConfidence * 0.3f) + (sizeConfidence * 0.4f) + (areaConfidence * 0.3f);

                  return Mathf.Clamp01(confidence);
            }
      }
}