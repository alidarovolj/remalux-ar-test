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
            [SerializeField] private float detectionInterval = 0.1f;
            [SerializeField] private float minWallHeight = 1.0f;
            [SerializeField] private float minWallWidth = 0.5f;
            [SerializeField] private double cannyThreshold1 = 50;
            [SerializeField] private double cannyThreshold2 = 150;
            [SerializeField] private int houghThreshold = 50;
            [SerializeField] private double minLineLength = 100;
            [SerializeField] private double maxLineGap = 10;

            [Header("Performance")]
            [SerializeField] private bool useProcessingResolution = true;
            [SerializeField] private Vector2Int processingResolution = new Vector2Int(320, 240);
            [SerializeField] private bool showPerformanceStats = true;
            [SerializeField] private float processingInterval = 0.1f; // Process every 100ms instead of every frame
            [SerializeField] private bool useGPUAcceleration = true; // Enable GPU acceleration for OpenCV operations

            [Header("Debug")]
            [SerializeField] private RawImage debugImageDisplay;
            [SerializeField] private bool showDebugLines = true;
            [SerializeField] private Color debugLineColor = Color.red;

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
            private bool isWebcamPlaying = false;
            private bool didUpdateThisFrame = false;
            private bool hasNewFrame = false;
            private Mat frameMat;
            private bool supportsComputeShaders;

            private void Start()
            {
                  if (!isInitialized)
                  {
                        // Check compute shader support on main thread
                        supportsComputeShaders = SystemInfo.supportsComputeShaders;
                        InitializeCamera();
                        InitializeOpenCV();
                        isInitialized = true;
                  }
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

                  // Set initial texture for debug display
                  if (debugImageDisplay != null)
                  {
                        debugImageDisplay.texture = webCamTexture;
                        // Fix image orientation
                        debugImageDisplay.rectTransform.localRotation = Quaternion.Euler(0, 0, -webCamTexture.videoRotationAngle);
                        debugImageDisplay.rectTransform.localScale = new Vector3(
                              webCamTexture.videoVerticallyMirrored ? -1 : 1,
                              1,
                              1
                        );
                        Debug.Log("Debug image display set with webcam texture");
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
                  if (!isDetecting || webCamTexture == null)
                  {
                        return;
                  }

                  // Increment frame counter
                  frameCount++;

                  // Calculate FPS every second
                  if (Time.time - lastFPSUpdate >= FPS_UPDATE_INTERVAL)
                  {
                        float fps = frameCount / (Time.time - lastFPSUpdate);
                        Debug.Log($"FPS: {fps:F1}, Processing time: {processingTime:F1}ms");

                        // Reset counters
                        frameCount = 0;
                        lastFPSUpdate = Time.time;
                  }

                  // Check if we should process a new frame
                  if (webCamTexture.isPlaying && webCamTexture.didUpdateThisFrame)
                  {
                        float currentTime = Time.time;

                        // Process frame if enough time has passed and we're not already processing
                        if (currentTime - lastDetectionTime >= detectionInterval && !isProcessing)
                        {
                              lastDetectionTime = currentTime;

                              try
                              {
                                    // Initialize buffer if needed
                                    if (webcamBuffer == null || webcamBuffer.Length != webCamTexture.width * webCamTexture.height)
                                    {
                                          webcamBuffer = new Color32[webCamTexture.width * webCamTexture.height];
                                    }

                                    // Get webcam data
                                    webCamTexture.GetPixels32(webcamBuffer);

                                    // Convert to Mat on main thread
                                    if (frameMat == null || frameMat.width() != webCamTexture.width || frameMat.height() != webCamTexture.height)
                                    {
                                          if (frameMat != null) frameMat.Dispose();
                                          frameMat = new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);
                                    }

                                    Utils.webCamTextureToMat(webCamTexture, frameMat, webcamBuffer, webCamTexture.videoVerticallyMirrored,
                                          webCamTexture.videoRotationAngle == 180 ? 1 : 0);

                                    // Start processing in background
                                    isProcessing = true;
                                    System.Threading.ThreadPool.QueueUserWorkItem(state => ProcessFrameAsync());
                              }
                              catch (System.Exception e)
                              {
                                    Debug.LogError($"Error capturing frame: {e.Message}");
                                    isProcessing = false;
                              }
                        }
                  }
            }

            private void ProcessFrameAsync()
            {
                  Stopwatch stopwatch = new Stopwatch();
                  stopwatch.Start();

                  try
                  {
                        // Ensure Mat objects are initialized
                        if (!EnsureMatInitialization())
                        {
                              Debug.LogError("Failed to initialize Mat objects");
                              isProcessing = false;
                              return;
                        }

                        // Copy frame data to input Mat
                        frameMat.copyTo(inputMat);

                        // Process at lower resolution if enabled
                        Mat processingMat = inputMat;
                        if (useProcessingResolution && resizedMat != null)
                        {
                              Imgproc.resize(inputMat, resizedMat, new Size(processingResolution.x, processingResolution.y));
                              processingMat = resizedMat;
                        }

                        // Convert to grayscale
                        Imgproc.cvtColor(processingMat, processedMat, Imgproc.COLOR_RGBA2GRAY);

                        // Apply Gaussian blur for noise reduction
                        Imgproc.GaussianBlur(processedMat, processedMat, new Size(3, 3), 0);

                        // Edge detection with optimized thresholds
                        Imgproc.Canny(processedMat, processedMat, cannyThreshold1, cannyThreshold2, 3, true);

                        // Line detection
                        if (useGPUAcceleration && supportsComputeShaders && lineDetector != null)
                        {
                              ProcessLinesGPU();
                        }
                        else
                        {
                              ProcessLinesCPU();
                        }

                        var walls = new List<WallData>();
                        if (lines != null && !lines.empty())
                        {
                              ProcessDetectedLines(walls);
                        }

                        stopwatch.Stop();
                        float threadProcessingTime = stopwatch.ElapsedMilliseconds;

                        // Update processing time on main thread
                        UnityMainThread.Execute(() =>
                        {
                              processingTime = threadProcessingTime;
                              if (walls.Count > 0)
                              {
                                    OnWallsDetected?.Invoke(walls);
                                    UpdateDebugDisplay();
                              }
                        });
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Error in ProcessFrameAsync: {e.Message}\n{e.StackTrace}");
                  }
                  finally
                  {
                        isProcessing = false;
                  }
            }

            private void ProcessLinesCPU()
            {
                  if (lines != null) lines.release();
                  lines = new Mat();

                  Imgproc.HoughLinesP(
                        processedMat,
                        lines,
                        1,
                        Mathf.Deg2Rad,
                        houghThreshold,
                        minLineLength,
                        maxLineGap
                  );
            }

            private bool EnsureMatInitialization()
            {
                  try
                  {
                        if (inputMat == null || inputMat.width() != webCamTexture.width || inputMat.height() != webCamTexture.height)
                        {
                              InitializeOpenCV();
                        }
                        return inputMat != null && processedMat != null && debugMat != null;
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Error in EnsureMatInitialization: {e.Message}");
                        return false;
                  }
            }

            private void ProcessDetectedLines(List<WallData> walls)
            {
                  if (!showDebugLines) return;

                  if (useProcessingResolution)
                  {
                        Imgproc.resize(workingMat, debugMat, new Size(inputMat.cols(), inputMat.rows()));
                  }
                  else
                  {
                        workingMat.copyTo(debugMat);
                  }

                  float scaleX = useProcessingResolution ? (float)inputMat.cols() / processingResolution.x : 1f;
                  float scaleY = useProcessingResolution ? (float)inputMat.rows() / processingResolution.y : 1f;

                  for (int i = 0; i < lines.rows(); i++)
                  {
                        double[] line = lines.get(i, 0);
                        if (line == null) continue;

                        if (useProcessingResolution)
                        {
                              line[0] *= scaleX;
                              line[1] *= scaleY;
                              line[2] *= scaleX;
                              line[3] *= scaleY;
                        }

                        var wallData = ConvertLineToWallData(line);
                        if (IsValidWall(wallData))
                        {
                              walls.Add(wallData);
                              DrawDebugLine(line);
                        }
                  }

                  if (walls.Count > 0)
                  {
                        UnityMainThread.Execute(() =>
                        {
                              OnWallsDetected?.Invoke(walls);
                              UpdateDebugDisplay();
                        });
                  }
            }

            private void DrawDebugLine(double[] line)
            {
                  Imgproc.line(
                        debugMat,
                        new Point(line[0], line[1]),
                        new Point(line[2], line[3]),
                        new Scalar(debugLineColor.r * 255, debugLineColor.g * 255, debugLineColor.b * 255),
                        2
                  );
            }

            private void ProcessLinesGPU()
            {
                  if (lineDetector == null || linesBuffer == null || resultBuffer == null || lineCountBuffer == null)
                  {
                        Debug.LogError("GPU resources not initialized!");
                        return;
                  }

                  // Convert Mat to ComputeBuffer
                  float[] data = new float[processedMat.total() * processedMat.channels()];
                  processedMat.get(0, 0, data);
                  linesBuffer.SetData(data);

                  // Set compute shader parameters
                  lineDetector.SetBuffer(0, "lines", linesBuffer);
                  lineDetector.SetBuffer(0, "result", resultBuffer);
                  lineDetector.SetBuffer(0, "lineCount", lineCountBuffer);
                  lineDetector.SetInt("width", (int)processedMat.width());
                  lineDetector.SetInt("height", (int)processedMat.height());
                  lineDetector.SetFloat("threshold", (float)cannyThreshold1);
                  lineDetector.SetFloat("minLength", (float)minLineLength);
                  lineDetector.SetFloat("maxGap", (float)maxLineGap);

                  // Reset line count
                  uint[] initialCount = new uint[] { 0 };
                  lineCountBuffer.SetData(initialCount);

                  // Dispatch compute shader
                  int threadGroupsX = Mathf.CeilToInt(processedMat.width() / 8f);
                  int threadGroupsY = Mathf.CeilToInt(processedMat.height() / 8f);
                  lineDetector.Dispatch(0, threadGroupsX, threadGroupsY, 1);

                  // Get results
                  uint[] count = new uint[1];
                  lineCountBuffer.GetData(count);
                  int lineCount = (int)count[0];

                  if (lineCount > 0)
                  {
                        // Create Mat with actual line count
                        lines = new Mat(lineCount, 4, CvType.CV_32F);
                        float[] lineData = new float[lineCount * 4];
                        resultBuffer.GetData(lineData);
                        lines.put(0, 0, lineData);
                  }
                  else
                  {
                        lines = new Mat();
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
                              Utils.matToTexture2D(debugMat, debugTexture, false);
                              debugTexture.Apply();
                              debugImageDisplay.texture = debugTexture;
                        }
                        catch (System.Exception e)
                        {
                              Debug.LogError($"Error updating debug display: {e.Message}");
                        }
                  }
            }

            private WallData ConvertLineToWallData(double[] line)
            {
                  // Convert 2D line to 3D wall data
                  // This is a simplified conversion - you might want to adjust based on your camera setup
                  Vector3 start = new Vector3((float)line[0], 0, (float)line[1]);
                  Vector3 end = new Vector3((float)line[2], 0, (float)line[3]);
                  Vector3 direction = end - start;
                  float length = direction.magnitude;
                  Vector3 center = (start + end) / 2f;

                  return new WallData
                  {
                        position = center,
                        rotation = Quaternion.LookRotation(direction),
                        scale = new Vector3(length, minWallHeight, 0.1f)
                  };
            }

            private bool IsValidWall(WallData wall)
            {
                  // Check if wall meets minimum size requirements
                  return wall.scale.x >= minWallWidth && wall.scale.y >= minWallHeight;
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
      }

      public struct WallData
      {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
      }
}