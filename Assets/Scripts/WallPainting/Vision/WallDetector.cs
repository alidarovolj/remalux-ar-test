using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UtilsModule;
using System.Collections;

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
            private int frameCount;
            private float fpsUpdateInterval = 1f;
            private float nextFpsUpdate;
            private float currentFPS;
            private Mat inputMat;
            private Mat workingMat;
            private Mat lines;
            private Texture2D debugTexture;
            private bool isInitialized = false;

            private void Start()
            {
                  if (!isInitialized)
                  {
                        InitializeCamera();
                        InitializeOpenCV();
                        isInitialized = true;
                  }
            }

            private void InitializeOpenCV()
            {
                  if (webCamTexture == null) return;

                  // Initialize Mats with correct size
                  inputMat = new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);
                  processedMat = new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC1);
                  debugMat = new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);
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
                  nextFpsUpdate = Time.time + fpsUpdateInterval;
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
                  if (!isDetecting || webCamTexture == null || !webCamTexture.isPlaying)
                  {
                        return;
                  }

                  float deltaTime = Time.deltaTime;
                  if (deltaTime > 0)
                  {
                        currentFPS = Mathf.Lerp(currentFPS, 1.0f / deltaTime, 0.1f);
                  }

                  // Only process if enough time has passed since last detection
                  if (Time.time - lastDetectionTime >= detectionInterval)
                  {
                        lastDetectionTime = Time.time;
                        float startTime = Time.realtimeSinceStartup;

                        DetectWalls();

                        float processingTime = (Time.realtimeSinceStartup - startTime) * 1000f;
                        if (showPerformanceStats)
                        {
                              Debug.Log($"FPS: {currentFPS:F1}, Processing time: {processingTime:F1}ms");
                        }
                  }
            }

            private void DetectWalls()
            {
                  if (webCamTexture == null || !webCamTexture.isPlaying || !webCamTexture.didUpdateThisFrame)
                        return;

                  try
                  {
                        // Ensure Mat objects are initialized
                        if (inputMat == null || processedMat == null || debugMat == null || lines == null)
                        {
                              Debug.LogWarning("Mat objects not initialized. Reinitializing OpenCV...");
                              InitializeOpenCV();
                              if (inputMat == null || processedMat == null || debugMat == null || lines == null)
                              {
                                    Debug.LogError("Failed to initialize Mat objects!");
                                    return;
                              }
                        }

                        // Ensure Mat sizes match
                        if (inputMat.width() != webCamTexture.width || inputMat.height() != webCamTexture.height)
                        {
                              Debug.Log($"Reinitializing Mats to match camera resolution: {webCamTexture.width}x{webCamTexture.height}");
                              InitializeOpenCV();
                        }

                        // Convert WebCamTexture to Mat
                        Utils.webCamTextureToMat(webCamTexture, inputMat);

                        if (useProcessingResolution && resizedMat != null)
                        {
                              // Resize for processing
                              Imgproc.resize(inputMat, resizedMat, new Size(processingResolution.x, processingResolution.y));
                        }

                        // Convert to grayscale
                        Imgproc.cvtColor(workingMat, processedMat, Imgproc.COLOR_RGBA2GRAY);

                        // Apply Gaussian blur
                        Imgproc.GaussianBlur(processedMat, processedMat, new Size(5, 5), 0);

                        // Edge detection
                        lines.release(); // Clear previous lines
                        Imgproc.Canny(processedMat, processedMat, cannyThreshold1, cannyThreshold2);

                        // Line detection
                        Imgproc.HoughLinesP(
                              processedMat,
                              lines,
                              1,
                              Mathf.Deg2Rad,
                              houghThreshold,
                              minLineLength,
                              maxLineGap
                        );

                        var walls = new List<WallData>();
                        if (!lines.empty())
                        {
                              if (showDebugLines)
                              {
                                    if (useProcessingResolution)
                                    {
                                          Imgproc.resize(workingMat, debugMat, new Size(inputMat.cols(), inputMat.rows()));
                                    }
                                    else
                                    {
                                          workingMat.copyTo(debugMat);
                                    }

                                    for (int i = 0; i < lines.rows(); i++)
                                    {
                                          double[] line = lines.get(i, 0);
                                          if (line == null) continue;

                                          // Scale line coordinates if using processing resolution
                                          if (useProcessingResolution)
                                          {
                                                float scaleX = (float)inputMat.cols() / processingResolution.x;
                                                float scaleY = (float)inputMat.rows() / processingResolution.y;
                                                line[0] *= scaleX;
                                                line[1] *= scaleY;
                                                line[2] *= scaleX;
                                                line[3] *= scaleY;
                                          }

                                          // Convert line to wall data
                                          var wallData = ConvertLineToWallData(line);
                                          if (IsValidWall(wallData))
                                          {
                                                walls.Add(wallData);
                                          }

                                          // Draw debug lines
                                          Imgproc.line(
                                                debugMat,
                                                new Point(line[0], line[1]),
                                                new Point(line[2], line[3]),
                                                new Scalar(debugLineColor.r * 255, debugLineColor.g * 255, debugLineColor.b * 255),
                                                2
                                          );
                                    }

                                    // Update debug display
                                    if (debugImageDisplay != null)
                                    {
                                          if (debugTexture == null)
                                          {
                                                debugTexture = new Texture2D(debugMat.cols(), debugMat.rows(), TextureFormat.RGBA32, false);
                                          }
                                          Utils.matToTexture2D(debugMat, debugTexture, true);
                                          debugImageDisplay.texture = debugTexture;
                                    }
                              }

                              if (walls.Count > 0 && OnWallsDetected != null)
                              {
                                    OnWallsDetected.Invoke(walls);
                              }
                        }
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Error in DetectWalls: {e.Message}\n{e.StackTrace}");
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
                  if (debugTexture != null) Destroy(debugTexture);
            }
      }

      public struct WallData
      {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
      }
}