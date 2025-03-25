using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UtilsModule;

namespace Remalux.WallPainting.Vision
{
      public class WallDetector : MonoBehaviour
      {
            public event System.Action<List<WallData>> OnWallsDetected;

            [Header("Camera Settings")]
            [SerializeField] private bool useWebcam = true;
            [SerializeField] private int webcamDeviceIndex = 0;
            [SerializeField] private Vector2Int webcamResolution = new Vector2Int(640, 480);
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

            private void Start()
            {
                  InitializeCamera();
                  InitializeOpenCV();
            }

            private void InitializeOpenCV()
            {
                  processedMat = new Mat();
                  debugMat = new Mat();
                  if (useProcessingResolution)
                  {
                        resizedMat = new Mat();
                  }
            }

            private void InitializeCamera()
            {
                  if (!useWebcam) return;

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
                        return;
                  }

                  // Log available devices
                  Debug.Log($"Found {devices.Length} webcam devices:");
                  for (int i = 0; i < devices.Length; i++)
                  {
                        Debug.Log($"Device {i}: {devices[i].name} (isFrontFacing: {devices[i].isFrontFacing})");
                  }

                  // Use specified device index or default to 0
                  webcamDeviceIndex = Mathf.Clamp(webcamDeviceIndex, 0, devices.Length - 1);

                  // Create and configure webcam texture
                  webCamTexture = new WebCamTexture(devices[webcamDeviceIndex].name, webcamResolution.x, webcamResolution.y, targetFPS);

                  // Start the webcam
                  webCamTexture.Play();

                  // Wait for webcam to start
                  float startTime = Time.time;
                  while (!webCamTexture.isPlaying && !webCamTexture.didUpdateThisFrame && (Time.time - startTime) < 3f)
                  {
                        System.Threading.Thread.Sleep(100);
                  }

                  if (!webCamTexture.isPlaying)
                  {
                        Debug.LogError("Failed to start webcam!");
                        return;
                  }

                  // Log webcam info
                  Debug.Log($"Webcam started: {webCamTexture.width}x{webCamTexture.height} @ {webCamTexture.requestedFPS}fps");
                  Debug.Log($"Device: {webCamTexture.deviceName}");

                  // Set initial texture for debug display
                  if (debugImageDisplay != null)
                  {
                        debugImageDisplay.texture = webCamTexture;
                        Debug.Log("Debug image display set with webcam texture");
                  }
                  else
                  {
                        Debug.LogWarning("Debug image display is not assigned!");
                  }
            }

            public void StartDetection()
            {
                  if (webCamTexture == null || !webCamTexture.isPlaying)
                  {
                        InitializeCamera();
                  }
                  isDetecting = true;
                  lastDetectionTime = Time.time;
                  nextFpsUpdate = Time.time + fpsUpdateInterval;
                  frameCount = 0;
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
                  if (!isDetecting) return;

                  if (Time.time - lastDetectionTime >= detectionInterval)
                  {
                        float startTime = Time.realtimeSinceStartup;
                        DetectWalls();
                        processingTime = Time.realtimeSinceStartup - startTime;

                        frameCount++;
                        if (Time.time >= nextFpsUpdate)
                        {
                              currentFPS = frameCount / fpsUpdateInterval;
                              frameCount = 0;
                              nextFpsUpdate = Time.time + fpsUpdateInterval;

                              if (showPerformanceStats)
                              {
                                    Debug.Log($"FPS: {currentFPS:F1}, Processing time: {processingTime * 1000:F1}ms");
                              }
                        }

                        lastDetectionTime = Time.time;
                  }
            }

            private void DetectWalls()
            {
                  if (!webCamTexture.isPlaying || !webCamTexture.didUpdateThisFrame)
                        return;

                  try
                  {
                        // Convert WebCamTexture to Mat
                        Mat inputMat = new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC3);
                        Utils.webCamTextureToMat(webCamTexture, inputMat);

                        Mat workingMat = inputMat;
                        if (useProcessingResolution)
                        {
                              // Resize for processing
                              Imgproc.resize(inputMat, resizedMat, new Size(processingResolution.x, processingResolution.y));
                              workingMat = resizedMat;
                        }

                        // Convert to grayscale
                        Imgproc.cvtColor(workingMat, processedMat, Imgproc.COLOR_RGB2GRAY);

                        // Apply Gaussian blur
                        Imgproc.GaussianBlur(processedMat, processedMat, new Size(5, 5), 0);

                        // Edge detection
                        Imgproc.Canny(processedMat, processedMat, cannyThreshold1, cannyThreshold2);

                        // Line detection
                        Mat lines = new Mat();
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
                                          Texture2D texture = new Texture2D(debugMat.cols(), debugMat.rows(), TextureFormat.RGBA32, false);
                                          Utils.matToTexture2D(debugMat, texture);
                                          debugImageDisplay.texture = texture;
                                    }
                              }

                              if (walls.Count > 0)
                              {
                                    OnWallsDetected?.Invoke(walls);
                              }
                        }

                        inputMat.Dispose();
                        lines.Dispose();
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

                  if (processedMat != null)
                  {
                        processedMat.Dispose();
                  }

                  if (debugMat != null)
                  {
                        debugMat.Dispose();
                  }

                  if (resizedMat != null)
                  {
                        resizedMat.Dispose();
                  }
            }
      }

      public struct WallData
      {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
      }
}