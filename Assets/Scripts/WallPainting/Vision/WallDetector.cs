#pragma warning disable CS0414 // Disable warnings about assigned but unused fields
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UtilsModule;
using System.Collections;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using OpenCVRect = OpenCVForUnity.CoreModule.Rect;

namespace Remalux.WallPainting.Vision
{
      public class WallDetector : MonoBehaviour
      {
            public event System.Action<List<WallData>> OnWallsDetected;

            [Header("Camera Settings")]
            [SerializeField] private bool useWebcam = true;
            [SerializeField] private int webcamDeviceIndex = 1; // Используем iPhone камеру по умолчанию
            [SerializeField] private Vector2Int webcamResolution = new Vector2Int(1280, 720);
            [SerializeField] private int targetFPS = 30;

            [Header("Detection Settings")]
            // These fields are intentionally marked as NonSerialized to suppress unused warnings
            [System.NonSerialized][SerializeField] private float detectionInterval = 0.03f; // Увеличиваем частоту обнаружения
            [System.NonSerialized][SerializeField] private float minWallHeight = 0.2f; // Дальнейшее снижение минимальной высоты
            [System.NonSerialized][SerializeField] private float minWallWidth = 0.2f; // Дальнейшее снижение минимальной ширины
            [SerializeField] private double cannyThreshold1 = 20; // Еще ниже порог для большей чувствительности
            [SerializeField] private double cannyThreshold2 = 80; // Еще ниже верхний порог
            [System.NonSerialized][SerializeField] private int houghThreshold = 20; // Еще ниже порог Hough
            [System.NonSerialized][SerializeField] private double minLineLength = 30; // Еще меньше минимальная длина линии
            [System.NonSerialized][SerializeField] private double maxLineGap = 30; // Увеличиваем разрыв между линиями

            [Header("Performance")]
            [SerializeField] private bool useProcessingResolution = true;
            [SerializeField] private Vector2Int processingResolution = new Vector2Int(640, 480); // Повышаем разрешение обработки
            [System.NonSerialized][SerializeField] private bool showPerformanceStats = true;
            [SerializeField] private float processingInterval = 0.05f; // Увеличиваем частоту обработки
            [SerializeField] private bool useGPUAcceleration = true; // Оставляем GPU ускорение

            [Header("Debug")]
            [SerializeField] private RawImage debugImageDisplay;
            [SerializeField] private bool showDebugLines = true;
            [SerializeField] private Color debugLineColor = Color.red;

            [Header("Components")]
            [SerializeField] public Camera mainCamera; // Публичная ссылка на основную камеру для преобразования координат

            private bool isDetecting = false;
            private float lastDetectionTime;
            private WebCamTexture webCamTexture;
            private Mat processedMat;
            private Mat debugMat;
            private Mat resizedMat;
            private float processingTime;
            private float frameCount = 0;
            private float lastFPSUpdate = 0;
            private const float FPS_UPDATE_INTERVAL = 1.0f;
            private float nextFpsUpdate;
            private float currentFPS;
            private Mat inputMat;
            private Mat workingMat;
            private Mat lines;
            private Texture2D debugTexture;
            private bool isInitialized = false;
            private float lastProcessingTime;
            private bool isProcessing = false;
            private ComputeShader lineDetector;
            private ComputeBuffer linesBuffer;
            private ComputeBuffer resultBuffer;
            private ComputeBuffer lineCountBuffer;
            private Color32[] webcamBuffer;
            // These fields are intentionally marked as NonSerialized to suppress unused warnings
            [System.NonSerialized] private bool isWebcamPlaying = false;
            [System.NonSerialized] private bool didUpdateThisFrame = false;
            [System.NonSerialized] private bool hasNewFrame = false;
            private Mat frameMat;
            private bool supportsComputeShaders;
            private byte[] mainThreadTextureData;
            private bool newFrameReady = false;

            // Класс для хранения информации о 2D контурах, которые могут быть стенами
            public class WallContourData
            {
                  public OpenCVRect boundRect;
                  public float angle;
                  public Vector2 center;
                  public float aspectRatio;
            }

            // Добавляем список для промежуточных результатов обработки
            private List<WallContourData> detectedWallContours = new List<WallContourData>();
            private object contoursLock = new object();

            // Переменные для хранения обнаруженных контуров
            private object detectedWallsLock = new object();
            private List<WallContourData> pendingWallContours = new List<WallContourData>();
            private object debugMatLock = new object(); // Для безопасного доступа к debugMat
            private bool debugMatUpdated = false; // Флаг обновления отладочного изображения

            private bool isCameraInitialized = false;
            private int framesToSkip = 0;

            private void Start()
            {
                  Debug.Log("WallDetector.Start()");

                  // Remove any existing debug panels from OpenCVForUnity
                  OpenCVForUnity.UnityUtils.DebugMatUtils.clear();

                  // Находим основную камеру, если она не задана
                  if (mainCamera == null)
                  {
                        mainCamera = Camera.main;

                        if (mainCamera == null)
                        {
                              Debug.LogError("Не задана основная камера! Преобразование координат не будет работать.");
                              mainCamera = FindFirstObjectByType<Camera>();

                              if (mainCamera != null)
                              {
                                    Debug.Log($"Автоматически найдена камера: {mainCamera.name}");
                              }
                        }
                        else
                        {
                              Debug.Log($"Использую основную камеру: {mainCamera.name}");
                        }
                  }

                  // Инициализация OpenCV и запуск детекции
                  isInitialized = false;

                  // Проверяем поддержку Compute Shaders
                  supportsComputeShaders = SystemInfo.supportsComputeShaders;
                  Debug.Log($"Поддержка Compute Shaders: {supportsComputeShaders}");

                  debugImageDisplay.gameObject.SetActive(true);

                  StartCoroutine(InitializeCameraCoroutine());

                  InitializeOpenCV();
            }

            private void InitializeOpenCV()
            {
                  if (webCamTexture == null) return;

                  try
                  {
                        // Dispose existing Mats
                        if (inputMat != null) inputMat.Dispose();
                        if (processedMat != null) processedMat.Dispose();
                        if (debugMat != null) debugMat.Dispose();
                        if (resizedMat != null) resizedMat.Dispose();
                        if (workingMat != null && workingMat != inputMat && workingMat != resizedMat) workingMat.Dispose();
                        if (lines != null) lines.Dispose();
                        if (frameMat != null) frameMat.Dispose();

                        // Initialize Mats with correct size
                        inputMat = new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);
                        processedMat = new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC1);
                        debugMat = new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);
                        frameMat = new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);
                        lines = new Mat();

                        if (useProcessingResolution)
                        {
                              resizedMat = new Mat(processingResolution.y, processingResolution.x, CvType.CV_8UC4);
                              workingMat = resizedMat;
                        }
                        else
                        {
                              workingMat = inputMat;
                        }

                        Debug.Log($"OpenCV Mats initialized: {webCamTexture.width}x{webCamTexture.height}");

                        // Initialize GPU buffers if available
                        if (useGPUAcceleration && supportsComputeShaders)
                        {
                              InitializeGPUResources();
                        }
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Error initializing OpenCV: {e.Message}\n{e.StackTrace}");
                        isInitialized = false;
                  }
            }

            private void InitializeGPUResources()
            {
                  try
                  {
                        lineDetector = Resources.Load<ComputeShader>("LineDetector");
                        if (lineDetector == null)
                        {
                              Debug.LogError("Failed to load LineDetector compute shader!");
                              useGPUAcceleration = false;
                              return;
                        }

                        // Release existing buffers
                        if (linesBuffer != null) linesBuffer.Release();
                        if (resultBuffer != null) resultBuffer.Release();
                        if (lineCountBuffer != null) lineCountBuffer.Release();

                        // Create new buffers
                        linesBuffer = new ComputeBuffer((int)(processedMat.total() * processedMat.channels()), sizeof(float));
                        resultBuffer = new ComputeBuffer(1000, sizeof(float) * 4);
                        lineCountBuffer = new ComputeBuffer(1, sizeof(uint));

                        Debug.Log("GPU resources initialized successfully");
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogWarning($"Failed to initialize GPU resources: {e.Message}");
                        useGPUAcceleration = false;
                  }
            }

            private IEnumerator InitializeCameraCoroutine()
            {
                  if (!useWebcam) yield break;

                  // Stop any existing webcam
                  if (webCamTexture != null)
                  {
                        webCamTexture.Stop();
                        Destroy(webCamTexture);
                        webCamTexture = null;
                  }

                  // Get available webcams
                  WebCamDevice[] devices = WebCamTexture.devices;
                  if (devices.Length == 0)
                  {
                        Debug.LogError("No webcam found!");
                        yield break;
                  }

                  // Log available devices
                  Debug.Log($"Found {devices.Length} webcam devices:");
                  for (int i = 0; i < devices.Length; i++)
                  {
                        Debug.Log($"Device {i}: {devices[i].name} (isFrontFacing: {devices[i].isFrontFacing})");
                  }

                  // Use specified device index or default to 0
                  webcamDeviceIndex = Mathf.Clamp(webcamDeviceIndex, 0, devices.Length - 1);
                  string deviceName = devices[webcamDeviceIndex].name;

                  // Try different resolutions in order of preference
                  Vector2Int[] resolutions = new Vector2Int[] {
                        new Vector2Int(1280, 720),
                        new Vector2Int(640, 480),
                        new Vector2Int(320, 240)
                  };

                  bool initialized = false;
                  foreach (var resolution in resolutions)
                  {
                        Debug.Log($"Trying resolution: {resolution.x}x{resolution.y}");

                        webCamTexture = new WebCamTexture(deviceName, resolution.x, resolution.y, targetFPS);
                        webCamTexture.Play();

                        // Wait for webcam to start
                        float startTime = Time.time;
                        int attempts = 0;
                        while (attempts < 10)
                        {
                              yield return new WaitForSeconds(0.5f);

                              if (webCamTexture.width > 16 && webCamTexture.height > 16)
                              {
                                    Debug.Log($"Successfully initialized camera at {webCamTexture.width}x{webCamTexture.height}");
                                    initialized = true;
                                    break;
                              }
                              attempts++;
                        }

                        if (initialized) break;

                        Debug.Log($"Failed to initialize at {resolution.x}x{resolution.y}, got {webCamTexture.width}x{webCamTexture.height}");
                        webCamTexture.Stop();
                        Destroy(webCamTexture);
                        yield return new WaitForSeconds(0.5f);
                  }

                  if (!initialized)
                  {
                        Debug.LogError("Failed to initialize camera at any resolution!");
                        yield break;
                  }

                  // Setup debug image display with the webcam texture
                  if (debugImageDisplay != null)
                  {
                        debugImageDisplay.texture = webCamTexture;
                        debugImageDisplay.material.mainTexture = webCamTexture;

                        // Make the RawImage full screen and correct orientation
                        debugImageDisplay.rectTransform.anchorMin = new Vector2(0, 0);
                        debugImageDisplay.rectTransform.anchorMax = new Vector2(1, 1);
                        debugImageDisplay.rectTransform.sizeDelta = Vector2.zero;
                        debugImageDisplay.rectTransform.anchoredPosition = Vector2.zero;

                        // Fix camera orientation based on the webcam rotation
                        int rotationAngle = webCamTexture.videoRotationAngle;
                        debugImageDisplay.rectTransform.localRotation = Quaternion.Euler(0, 0, -rotationAngle);

                        // Fix vertical inversion - invert y-scale for mobile devices which often have inverted camera
                        bool needsVerticalFlip = true; // Default to true for mobile devices
                        debugImageDisplay.rectTransform.localScale = new Vector3(
                            1,
                            needsVerticalFlip ? -1 : 1,
                            1
                        );

                        // Apply proper UV rect for WebCamTexture
                        if (webCamTexture.videoVerticallyMirrored)
                        {
                              debugImageDisplay.uvRect = new UnityEngine.Rect(0, 1, 1, -1);
                        }
                        else
                        {
                              debugImageDisplay.uvRect = new UnityEngine.Rect(0, 0, 1, 1);
                        }

                        // Clear any OpenCV debug panels
                        OpenCVForUnity.UnityUtils.DebugMatUtils.clear();

                        Debug.Log("Debug image display set with webcam texture and made fullscreen");
                  }

                  // Initialize OpenCV Mats with correct size
                  InitializeOpenCV();
            }

            public void StartDetection()
            {
                  if (!isInitialized)
                  {
                        isInitialized = true;
                        StartCoroutine(InitializeCameraCoroutine());
                  }
                  isDetecting = true;
                  lastDetectionTime = Time.time;
                  nextFpsUpdate = Time.time + FPS_UPDATE_INTERVAL;
                  frameCount = 0;
            }

            private void InitializeCamera()
            {
                  StartCoroutine(InitializeCameraCoroutine());
            }

            public void StopDetection()
            {
                  isDetecting = false;
            }

            public void SetDebugImageDisplay(RawImage display)
            {
                  debugImageDisplay = display;
                  if (webCamTexture != null && webCamTexture.isPlaying && debugImageDisplay != null)
                  {
                        debugImageDisplay.texture = webCamTexture;
                  }
            }

            private void Update()
            {
                  // Clear any OpenCV debug panels periodically
                  if (Time.frameCount % 60 == 0)
                  {
                        OpenCVForUnity.UnityUtils.DebugMatUtils.clear();
                  }

                  // Show the current FPS and processing time in debug logs periodically
                  if (Time.frameCount % 180 == 0)
                  {
                        float fps = 1.0f / Time.smoothDeltaTime;
                        Debug.Log($"FPS: {fps:F1}, Processing time: {lastProcessingTime:F1}ms");
                  }

                  // Check if the camera is initialized
                  if (!isCameraInitialized && webCamTexture != null && webCamTexture.isPlaying && webCamTexture.width > 100)
                  {
                        isCameraInitialized = true;
                        framesToSkip = 20; // Skip a few frames to stabilize the camera after startup
                  }

                  // Update the FPS counter
                  frameCount++;
                  float timeNow = Time.realtimeSinceStartup;

                  if (timeNow > nextFpsUpdate)
                  {
                        currentFPS = frameCount / (timeNow - lastFPSUpdate);
                        frameCount = 0;
                        lastFPSUpdate = timeNow;
                        nextFpsUpdate = timeNow + FPS_UPDATE_INTERVAL;
                  }

                  // Skip if not initialized or not detecting
                  if (!isInitialized || !isDetecting || webCamTexture == null || !webCamTexture.isPlaying)
                  {
                        return;
                  }

                  // Skip frames if needed to stabilize camera
                  if (framesToSkip > 0)
                  {
                        framesToSkip--;
                        return;
                  }

                  // Обработка найденных 2D контуров и преобразование их в 3D стены
                  ProcessDetectedContoursMainThread();

                  // Обновляем отладочное изображение, если оно было изменено
                  lock (debugMatLock)
                  {
                        if (debugMatUpdated && debugMat != null)
                        {
                              UpdateDebugDisplay();
                              debugMatUpdated = false;
                        }
                  }

                  // Обработка ввода для переключения режимов отображения
                  if (Input.GetKeyDown(KeyCode.D))
                  {
                        showDebugLines = !showDebugLines;
                        Debug.Log($"Режим отладки: {(showDebugLines ? "включен" : "выключен")}");
                  }

                  // Process frames at regular intervals or when we explicitly have a new frame
                  if (!isProcessing && webCamTexture != null && webCamTexture.didUpdateThisFrame)
                  {
                        float timeSinceLastProcess = Time.realtimeSinceStartup - lastProcessingTime;
                        if (timeSinceLastProcess >= processingInterval)
                        {
                              try
                              {
                                    // Захват данных текстуры в основном потоке
                                    CaptureTextureDataOnMainThread();

                                    // Capture and process the current frame
                                    isProcessing = true;
                                    lastProcessingTime = Time.realtimeSinceStartup;

                                    // Process frame on a background thread to avoid freezing the main thread
                                    System.Threading.ThreadPool.QueueUserWorkItem((_) =>
                                    {
                                          try
                                          {
                                                Process2DDataAsync();
                                                isProcessing = false;
                                          }
                                          catch (System.Exception e)
                                          {
                                                Debug.LogError($"Error processing frame: {e.Message}");
                                                isProcessing = false;
                                          }
                                    });
                              }
                              catch (System.Exception e)
                              {
                                    Debug.LogError($"Error capturing frame data: {e.Message}");
                                    isProcessing = false;
                              }
                        }
                  }
            }

            private void CaptureTextureDataOnMainThread()
            {
                  try
                  {
                        // Захват данных из текстуры здесь - в основном потоке
                        if (webCamTexture != null)
                        {
                              // Проверяем размеры и инициализируем inputMat, если необходимо
                              if (inputMat == null || inputMat.width() != webCamTexture.width || inputMat.height() != webCamTexture.height)
                              {
                                    InitializeOpenCV();
                              }

                              // Захватываем данные из webCamTexture в основном потоке
                              Utils.webCamTextureToMat(webCamTexture, inputMat);
                              newFrameReady = true;
                        }
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Error capturing texture data on main thread: {e.Message}");
                  }
            }

            // Метод для обработки 2D данных (запускается в фоновом потоке)
            private void Process2DDataAsync()
            {
                  try
                  {
                        if (!newFrameReady || inputMat == null) return;

                        // Create a stopwatch to measure processing time
                        Stopwatch stopwatch = Stopwatch.StartNew();

                        // Создаем копию исходного изображения для отображения результатов
                        inputMat.copyTo(debugMat);

                        // REMOVED: No orange overlay for scanning effect

                        // Добавляем сетку для эффекта сканирования - используем системное время вместо Unity Time
                        float currentTime = (float)Stopwatch.GetTimestamp() / Stopwatch.Frequency;
                        int gridSize = 40 + (int)(10 * Mathf.Sin(currentTime));
                        DrawStaticScanningGrid(debugMat, gridSize);

                        // Рисуем центральный прицел
                        int centerX = debugMat.cols() / 2;
                        int centerY = debugMat.rows() / 2;
                        int crosshairSize = 30 + (int)(10 * Mathf.Sin(currentTime * 3)); // Пульсирующий размер с системным временем
                        Imgproc.circle(debugMat, new Point(centerX, centerY), crosshairSize, new Scalar(0, 255, 255, 200), 2);
                        Imgproc.line(debugMat, new Point(centerX - crosshairSize, centerY), new Point(centerX + crosshairSize, centerY), new Scalar(255, 255, 255, 220), 2);
                        Imgproc.line(debugMat, new Point(centerX, centerY - crosshairSize), new Point(centerX, centerY + crosshairSize), new Scalar(255, 255, 255, 220), 2);

                        // Добавляем текст с инструкцией
                        Imgproc.putText(
                            debugMat,
                            "СКАНИРОВАНИЕ ПОВЕРХНОСТЕЙ...",
                            new Point(centerX - 150, 30),
                            Imgproc.FONT_HERSHEY_DUPLEX,
                            0.7,
                            new Scalar(255, 255, 0), // Яркий желтый
                            1
                        );

                        // Обрабатываем найденные контуры
                        List<WallContourData> wallContours = new List<WallContourData>();
                        int wallCount = 0; // Счетчик обнаруженных стен для нумерации

                        // Create a grayscale image for contour detection
                        Mat grayMat = new Mat();
                        Imgproc.cvtColor(inputMat, grayMat, Imgproc.COLOR_RGBA2GRAY);

                        // Apply Gaussian blur to reduce noise
                        Imgproc.GaussianBlur(grayMat, grayMat, new Size(5, 5), 0);

                        // Apply Canny edge detection
                        Mat edgeMat = new Mat();
                        Imgproc.Canny(grayMat, edgeMat, cannyThreshold1, cannyThreshold2);

                        // Apply morphological operations to close gaps
                        Mat kernel = Imgproc.getStructuringElement(Imgproc.MORPH_RECT, new Size(3, 3));
                        Imgproc.morphologyEx(edgeMat, edgeMat, Imgproc.MORPH_CLOSE, kernel);

                        // Important: Make sure we have a proper binary image for contour detection
                        // Threshold the image to ensure it's binary (0 or 255)
                        Mat binaryMat = new Mat();
                        Imgproc.threshold(edgeMat, binaryMat, 1, 255, Imgproc.THRESH_BINARY);

                        // Ensure we have an 8-bit single channel matrix
                        if (binaryMat.channels() > 1)
                        {
                              Mat tmp = new Mat();
                              Imgproc.cvtColor(binaryMat, tmp, Imgproc.COLOR_RGBA2GRAY);
                              binaryMat.release();
                              binaryMat = tmp;
                        }

                        // Находим контуры - это будут потенциальные стены
                        List<MatOfPoint> contours = new List<MatOfPoint>();
                        Mat hierarchy = new Mat();
                        // Use the PROPERLY prepared binary matrix for contour detection
                        Imgproc.findContours(binaryMat, contours, hierarchy, Imgproc.RETR_EXTERNAL, Imgproc.CHAIN_APPROX_SIMPLE);

                        // Draw edges on debug image for visualization
                        Imgproc.cvtColor(binaryMat, edgeMat, Imgproc.COLOR_GRAY2RGBA);
                        OpenCVForUnity.CoreModule.Core.addWeighted(debugMat, 0.7, edgeMat, 0.3, 0, debugMat);

                        // Проходим по всем обработанным контурам и рисуем на них информацию
                        for (int i = 0; i < contours.Count; i++)
                        {
                              MatOfPoint contour = contours[i];
                              double area = Imgproc.contourArea(contour);
                              if (area < 200) continue; // Еще меньше минимальная площадь

                              // Аппроксимируем контур многоугольником для получения более прямых линий
                              MatOfPoint2f contour2f = new MatOfPoint2f(contour.toArray());
                              MatOfPoint2f approxCurve = new MatOfPoint2f();
                              double epsilon = 0.04 * Imgproc.arcLength(contour2f, true);
                              Imgproc.approxPolyDP(contour2f, approxCurve, epsilon, true);

                              // Преобразуем обратно в MatOfPoint
                              MatOfPoint approxContour = new MatOfPoint(approxCurve.toArray());

                              // Получаем ограничивающий прямоугольник контура
                              OpenCVRect boundRect = Imgproc.boundingRect(approxContour);

                              // Принимаем практически любой контур подходящего размера
                              float aspectRatio = (float)boundRect.width / boundRect.height;
                              bool isValidSurface = aspectRatio > 0.1 && aspectRatio < 10.0; // Еще более широкий диапазон

                              // Дополнительная проверка - должен быть достаточно большим
                              bool isBigEnough = boundRect.width > 30 && boundRect.height > 30;

                              // Если контур достаточно большой и имеет подходящие пропорции, добавляем его
                              if (isValidSurface && isBigEnough)
                              {
                                    // Вычисляем центр и ориентацию
                                    Vector2 center = new Vector2(
                                          boundRect.x + boundRect.width / 2f,
                                          boundRect.y + boundRect.height / 2f
                                    );

                                    // Определяем ориентацию контура
                                    RotatedRect rotatedRect = Imgproc.minAreaRect(new MatOfPoint2f(approxContour.toArray()));
                                    float angle = (float)rotatedRect.angle;

                                    // Сохраняем только 2D данные для последующей обработки в основном потоке
                                    WallContourData wallContour = new WallContourData
                                    {
                                          boundRect = boundRect,
                                          angle = angle,
                                          center = center,
                                          aspectRatio = aspectRatio
                                    };

                                    wallContours.Add(wallContour);
                                    wallCount++;

                                    // Рисуем контур на изображении
                                    Imgproc.drawContours(debugMat, new List<MatOfPoint> { approxContour }, 0, new Scalar(0, 255, 255, 255), 2);
                              }
                        }

                        // Рисуем обнаруженные контуры на изображении для визуализации
                        Imgproc.drawContours(debugMat, contours, -1, new Scalar(0, 255, 0, 255), 2);

                        // Проходим по всем обработанным контурам и рисуем на них информацию
                        for (int i = 0; i < wallContours.Count; i++)
                        {
                              WallContourData wallContour = wallContours[i];
                              OpenCVRect rect = wallContour.boundRect;

                              // Рисуем прямоугольник вокруг контура
                              Imgproc.rectangle(
                                  debugMat,
                                  new Point(rect.x, rect.y),
                                  new Point(rect.x + rect.width, rect.y + rect.height),
                                  new Scalar(255, 0, 0, 255),
                                  2
                              );

                              // Добавляем текст с информацией о контуре
                              Imgproc.putText(
                                  debugMat,
                                  $"Wall {i}: {rect.width}x{rect.height}",
                                  new Point(rect.x, rect.y - 5),
                                  Imgproc.FONT_HERSHEY_SIMPLEX,
                                  0.5,
                                  new Scalar(255, 255, 0),
                                  1
                              );

                              // Отмечаем центр контура
                              Point center = new Point(rect.x + rect.width / 2, rect.y + rect.height / 2);
                              Imgproc.circle(debugMat, center, 5, new Scalar(0, 0, 255), -1);
                        }

                        // Отображаем общее количество найденных контуров
                        Imgproc.putText(
                            debugMat,
                            $"Контуры: {contours.Count}, Стены: {wallContours.Count}",
                            new Point(10, debugMat.rows() - 10),
                            Imgproc.FONT_HERSHEY_SIMPLEX,
                            0.6,
                            new Scalar(255, 255, 255),
                            1
                        );

                        // Сохраняем данные для последующей обработки в основном потоке
                        lock (detectedWallsLock)
                        {
                              pendingWallContours.Clear();
                              pendingWallContours.AddRange(wallContours);

                              // Сразу выводим информацию о найденных контурах
                              if (wallContours.Count > 0)
                              {
                                    Debug.Log($"Найдено {wallContours.Count} потенциальных стен:");
                                    for (int i = 0; i < Mathf.Min(5, wallContours.Count); i++)
                                    {
                                          WallContourData contour = wallContours[i];
                                          OpenCVRect rect = contour.boundRect;
                                          Debug.Log($"  Стена {i}: размер {rect.width}x{rect.height}, соотношение {contour.aspectRatio:F2}");
                                    }
                                    if (wallContours.Count > 5)
                                    {
                                          Debug.Log($"  ... и ещё {wallContours.Count - 5} контуров");
                                    }
                              }
                              else
                              {
                                    Debug.Log("Не найдено контуров, подходящих для создания стен");
                              }
                        }

                        // Задаем время обработки для отображения в статистике
                        processingTime = (float)Stopwatch.GetTimestamp() / Stopwatch.Frequency - currentTime;

                        // Сигнализируем, что новый кадр готов для отображения
                        lock (contoursLock)
                        {
                              detectedWallContours.Clear();
                              detectedWallContours.AddRange(wallContours);
                        }

                        // Сигнализируем, что отладочное изображение обновлено
                        lock (debugMatLock)
                        {
                              debugMatUpdated = true;
                        }

                        // Cleanup temporary Mats to avoid memory leaks
                        grayMat.release();
                        edgeMat.release();
                        kernel.release();
                        hierarchy.release();
                        binaryMat.release();
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Ошибка при обработке кадра: {e.Message}\n{e.StackTrace}");
                  }
                  finally
                  {
                        newFrameReady = false;
                  }
            }

            // Метод для обработки обнаруженных контуров в основном потоке Unity
            private void ProcessDetectedContoursMainThread()
            {
                  // Проверяем, есть ли контуры для обработки
                  bool hasContours = false;
                  lock (detectedWallsLock)
                  {
                        hasContours = pendingWallContours.Count > 0;
                  }

                  // Если есть контуры для обработки, вызываем метод создания стен
                  if (hasContours)
                  {
                        List<WallContourData> wallsToProcess;
                        lock (detectedWallsLock)
                        {
                              wallsToProcess = new List<WallContourData>(pendingWallContours);
                              pendingWallContours.Clear();
                        }

                        Debug.Log($"Передаю {wallsToProcess.Count} контуров для создания стен");

                        // Вызов метода для преобразования 2D контуров в 3D стены
                        ProcessDetectedWalls(wallsToProcess);
                  }

                  // Обновляем отладочное изображение, если оно было изменено
                  lock (debugMatLock)
                  {
                        if (debugMatUpdated && debugMat != null)
                        {
                              UpdateDebugDisplay();
                              debugMatUpdated = false;
                        }
                  }
            }

            private void UpdateDebugDisplay()
            {
                  if (debugImageDisplay != null && debugMat != null)
                  {
                        try
                        {
                              if (debugTexture == null || debugTexture.width != debugMat.cols() || debugTexture.height != debugMat.rows())
                              {
                                    if (debugTexture != null)
                                    {
                                          Destroy(debugTexture);
                                    }
                                    debugTexture = new Texture2D(debugMat.cols(), debugMat.rows(), TextureFormat.RGBA32, false);
                              }

                              // Преобразовать Mat в текстуру
                              Utils.matToTexture2D(debugMat, debugTexture, webCamTexture.videoVerticallyMirrored);
                              debugTexture.Apply();

                              // Обновление текстуры на UI
                              debugImageDisplay.texture = debugTexture;

                              // Проверить и обновить UV-координаты при необходимости
                              if (webCamTexture.videoVerticallyMirrored)
                              {
                                    debugImageDisplay.uvRect = new UnityEngine.Rect(0, 1, 1, -1);
                              }
                              else
                              {
                                    debugImageDisplay.uvRect = new UnityEngine.Rect(0, 0, 1, 1);
                              }
                        }
                        catch (System.Exception e)
                        {
                              Debug.LogError($"Error updating debug display: {e.Message}");
                        }
                  }
            }

            private void OnDestroy()
            {
                  if (webCamTexture != null)
                  {
                        webCamTexture.Stop();
                        Destroy(webCamTexture);
                  }

                  if (processedMat != null) processedMat.Dispose();
                  if (debugMat != null) debugMat.Dispose();
                  if (resizedMat != null) resizedMat.Dispose();
                  if (inputMat != null) inputMat.Dispose();
                  if (lines != null) lines.Dispose();
                  if (frameMat != null) frameMat.Dispose();
                  if (debugTexture != null) Destroy(debugTexture);
                  if (linesBuffer != null) linesBuffer.Release();
                  if (resultBuffer != null) resultBuffer.Release();
                  if (lineCountBuffer != null) lineCountBuffer.Release();
            }

            // Метод для рисования сетки сканирования AR, который не использует Time.time (для фонового потока)
            private void DrawStaticScanningGrid(Mat image, int gridSize)
            {
                  int width = image.cols();
                  int height = image.rows();

                  // Параметры сетки
                  Scalar gridColor = new Scalar(0, 255, 255, 120); // Голубой полупрозрачный

                  // Рисуем горизонтальные и вертикальные линии
                  for (int x = 0; x < width; x += gridSize)
                  {
                        Imgproc.line(image,
                              new Point(x, 0),
                              new Point(x, height),
                              gridColor,
                              1,
                              Imgproc.LINE_AA);
                  }

                  for (int y = 0; y < height; y += gridSize)
                  {
                        Imgproc.line(image,
                              new Point(0, y),
                              new Point(width, y),
                              gridColor,
                              1,
                              Imgproc.LINE_AA);
                  }

                  // Добавляем пульсирующий прицел в центре
                  int centerX = width / 2;
                  int centerY = height / 2;

                  // Внешний круг
                  Imgproc.circle(image,
                        new Point(centerX, centerY),
                        gridSize / 2 + 10,
                        new Scalar(255, 255, 0, 180), // Желтый
                        2,
                        Imgproc.LINE_AA);
            }

            // Метод для рисования угловых маркеров на обнаруженной поверхности
            private void DrawCornerMarkers(Mat image, OpenCVRect rect)
            {
                  int markerSize = 20; // Размер угловых маркеров
                  int thickness = 2;   // Толщина линии
                  Scalar markerColor = new Scalar(255, 255, 0, 255); // Яркий желтый

                  // Левый верхний угол
                  Imgproc.line(image,
                        new Point(rect.x, rect.y),
                        new Point(rect.x + markerSize, rect.y),
                        markerColor,
                        thickness);

                  Imgproc.line(image,
                        new Point(rect.x, rect.y),
                        new Point(rect.x, rect.y + markerSize),
                        markerColor,
                        thickness);

                  // Правый верхний угол
                  Imgproc.line(image,
                        new Point(rect.x + rect.width, rect.y),
                        new Point(rect.x + rect.width - markerSize, rect.y),
                        markerColor,
                        thickness);

                  Imgproc.line(image,
                        new Point(rect.x + rect.width, rect.y),
                        new Point(rect.x + rect.width, rect.y + markerSize),
                        markerColor,
                        thickness);

                  // Левый нижний угол
                  Imgproc.line(image,
                        new Point(rect.x, rect.y + rect.height),
                        new Point(rect.x + markerSize, rect.y + rect.height),
                        markerColor,
                        thickness);

                  Imgproc.line(image,
                        new Point(rect.x, rect.y + rect.height),
                        new Point(rect.x, rect.y + rect.height - markerSize),
                        markerColor,
                        thickness);

                  // Правый нижний угол
                  Imgproc.line(image,
                        new Point(rect.x + rect.width, rect.y + rect.height),
                        new Point(rect.x + rect.width - markerSize, rect.y + rect.height),
                        markerColor,
                        thickness);

                  Imgproc.line(image,
                        new Point(rect.x + rect.width, rect.y + rect.height),
                        new Point(rect.x + rect.width, rect.y + rect.height - markerSize),
                        markerColor,
                        thickness);
            }

            // Метод для рисования сетки сканирования AR для использования в основном потоке
            private void DrawScanningGrid(Mat image)
            {
                  int width = image.cols();
                  int height = image.rows();

                  // Параметры сетки
                  int gridSize = 40; // Размер ячейки сетки
                  Scalar gridColor = new Scalar(0, 255, 255, 120); // Голубой полупрозрачный

                  // Рисуем горизонтальные и вертикальные линии
                  for (int x = 0; x < width; x += gridSize)
                  {
                        Imgproc.line(image,
                              new Point(x, 0),
                              new Point(x, height),
                              gridColor,
                              1,
                              Imgproc.LINE_AA);
                  }

                  for (int y = 0; y < height; y += gridSize)
                  {
                        Imgproc.line(image,
                              new Point(0, y),
                              new Point(width, y),
                              gridColor,
                              1,
                              Imgproc.LINE_AA);
                  }

                  // Добавляем пульсирующий прицел в центре
                  int centerX = width / 2;
                  int centerY = height / 2;
                  int crosshairSize = 20 + (int)(10 * Mathf.Sin(Time.realtimeSinceStartup * 3)); // Пульсирующий размер

                  // Внешний круг
                  Imgproc.circle(image,
                        new Point(centerX, centerY),
                        crosshairSize + 10,
                        new Scalar(255, 255, 0, 180), // Желтый
                        2,
                        Imgproc.LINE_AA);

                  // Внутренний круг
                  Imgproc.circle(image,
                        new Point(centerX, centerY),
                        crosshairSize / 2,
                        new Scalar(0, 255, 255, 200), // Циан
                        2,
                        Imgproc.LINE_AA);

                  // Перекрестье
                  Imgproc.line(image,
                        new Point(centerX - crosshairSize, centerY),
                        new Point(centerX + crosshairSize, centerY),
                        new Scalar(255, 255, 255, 220),
                        2,
                        Imgproc.LINE_AA);

                  Imgproc.line(image,
                        new Point(centerX, centerY - crosshairSize),
                        new Point(centerX, centerY + crosshairSize),
                        new Scalar(255, 255, 255, 220),
                        2,
                        Imgproc.LINE_AA);
            }

            public void ProcessDetectedWalls(List<WallContourData> wallContours)
            {
                  if (wallContours.Count == 0)
                  {
                        Debug.Log("Не найдены контуры для создания стен");
                        return;
                  }

                  // Проверяем наличие камеры
                  if (mainCamera == null)
                  {
                        Debug.LogError("Не задана камера для создания стен! Попытка найти доступную камеру...");
                        mainCamera = Camera.main;

                        if (mainCamera == null)
                        {
                              mainCamera = FindFirstObjectByType<Camera>();
                              if (mainCamera == null)
                              {
                                    Debug.LogError("Не найдена камера! Невозможно создать стены.");
                                    return;
                              }
                              else
                              {
                                    Debug.Log($"Найдена альтернативная камера: {mainCamera.name}");
                              }
                        }
                        else
                        {
                              Debug.Log($"Найдена основная камера: {mainCamera.name}");
                        }
                  }

                  Debug.Log($"Обрабатываем {wallContours.Count} контуров стен");

                  // Конвертируем 2D контуры в 3D стены
                  List<WallData> walls = new List<WallData>();

                  for (int i = 0; i < wallContours.Count; i++)
                  {
                        WallContourData wallContour = wallContours[i];

                        // Пропускаем контуры, которые находятся слишком близко к краю изображения
                        // так как они могут быть неполными
                        OpenCVRect rect = wallContour.boundRect;
                        int imageWidth = workingMat.cols();
                        int imageHeight = workingMat.rows();
                        int borderMargin = 10;

                        if (rect.x <= borderMargin || rect.y <= borderMargin ||
                            rect.x + rect.width >= imageWidth - borderMargin ||
                            rect.y + rect.height >= imageHeight - borderMargin)
                        {
                              // Пропускаем контур у края
                              Debug.Log($"Пропускаем контур {i} у края изображения: {rect.x},{rect.y},{rect.width},{rect.height}");
                              continue;
                        }

                        // Нормализуем координаты контура, чтобы он был в интервале 0-1
                        float normalizedCenterX = wallContour.center.x / imageWidth;
                        float normalizedCenterY = wallContour.center.y / imageHeight;

                        // Учитываем перевернутость камеры при создании луча
                        // Если камера отображается перевернутой, то и координаты центра контура нужно перевернуть
                        if (webCamTexture.videoVerticallyMirrored)
                        {
                              normalizedCenterY = 1 - normalizedCenterY;
                        }

                        // Создаем луч из центра камеры в направлении контура
                        // Инвертируем Y для правильной проекции в мировые координаты
                        Ray ray = mainCamera.ViewportPointToRay(new Vector3(normalizedCenterX, normalizedCenterY, 0));

                        // Определяем позицию и размеры стены
                        Vector3 position;
                        Quaternion rotation;
                        Vector3 scale;

                        // Определяем среднее расстояние до стены (5-10 метров)
                        float distanceToWall = Random.Range(5f, 10f);

                        // Позиция стены - направление луча умноженное на расстояние
                        position = ray.origin + ray.direction * distanceToWall;

                        // Ориентация стены - повернута лицом к камере
                        rotation = Quaternion.LookRotation(-ray.direction);

                        // Размеры стены - пропорциональны размеру контура
                        float widthRatio = (float)rect.width / imageWidth;
                        float heightRatio = (float)rect.height / imageHeight;

                        // Преобразуем соотношения размеров в метры (например, 5 метров для полного экрана)
                        float wallWidth = 5f * widthRatio;
                        float wallHeight = 5f * heightRatio;

                        // Добавляем случайность для более естественного вида
                        wallWidth = Mathf.Max(1f, wallWidth + Random.Range(-0.5f, 0.5f));
                        wallHeight = Mathf.Max(1f, wallHeight + Random.Range(-0.5f, 0.5f));

                        scale = new Vector3(wallWidth, wallHeight, 0.1f);

                        // Создаем данные о стене
                        WallData wallData = new WallData
                        {
                              position = position,
                              rotation = rotation,
                              scale = scale,
                              id = i + 1 // ID стены, начиная с 1
                        };

                        walls.Add(wallData);
                        Debug.Log($"Создана стена {i + 1} в позиции {position}, размер {scale.x}x{scale.y}");
                  }

                  // Оповещаем об обнаруженных стенах только если они есть
                  if (walls.Count > 0)
                  {
                        Debug.Log($"Найдено стен: {walls.Count}");
                        OnWallsDetected?.Invoke(walls);
                  }
                  else
                  {
                        Debug.Log("Не найдено подходящих стен после обработки");
                  }
            }
      }

      public struct WallData
      {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
            public int id;
      }
}
#pragma warning restore CS0414 // Restore warnings