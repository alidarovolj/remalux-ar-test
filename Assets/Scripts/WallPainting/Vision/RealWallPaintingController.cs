using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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

            private void Start()
            {
                  ValidateComponents();
                  SetupUI();
                  if (wallDetector != null)
                  {
                        wallDetector.OnWallsDetected += OnWallsDetected;
                        // Automatically start wall detection
                        StartCapture();
                  }
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
                  isCapturing = true;
                  if (wallDetector != null)
                  {
                        wallDetector.StartDetection();
                  }
                  UpdateUI();
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
                  detectedWalls = walls;
                  // TODO: Implement wall visualization and texture application
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
            }
      }
}