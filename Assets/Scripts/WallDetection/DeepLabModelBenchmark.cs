using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System;

namespace Remalux.WallDetection
{
    public class DeepLabModelBenchmark : MonoBehaviour
    {
        [System.Serializable]
        public class ModelTestResult
        {
            public string modelName;
            public float averageProcessingTime;
            public float minProcessingTime;
            public float maxProcessingTime;
            public float fps;
            public float memoryUsage;
            public int inputResolution;
        }

        [Header("Test Configuration")]
        [SerializeField] private List<NNModel> modelsToTest;
        [SerializeField] private List<string> modelNames;
        [SerializeField] private List<int> inputResolutions;
        [SerializeField] private int testIterations = 50;
        [SerializeField] private bool useGPU = true;
        [SerializeField] private bool saveResultsToFile = true;
        [SerializeField] private string resultsFilePath = "ModelBenchmarkResults.json";

        [Header("Test Input")]
        [SerializeField] private Texture2D testImage;
        [SerializeField] private bool useCameraInput = false;
        [SerializeField] private int selectedCameraDevice = 0;

        [Header("UI Elements")]
        [SerializeField] private RawImage outputDisplay;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button startButton;
        [SerializeField] private TMP_Text resultsText;
        [SerializeField] private ScrollRect resultsScrollView;

        private bool isTesting = false;
        private List<ModelTestResult> results = new List<ModelTestResult>();
        private WebCamTexture webCamTexture;

        private void Start()
        {
            if (startButton != null)
                startButton.onClick.AddListener(StartBenchmark);

            if (useCameraInput)
                InitializeCamera();

            ValidateConfiguration();
        }

        private void ValidateConfiguration()
        {
            if (modelsToTest == null || modelsToTest.Count == 0)
            {
                Debug.LogError("No models specified for testing.");
                UpdateStatus("Error: No models specified for testing", Color.red);
                if (startButton != null)
                    startButton.interactable = false;
                return;
            }

            // Ensure modelNames list matches modelsToTest list size
            if (modelNames == null || modelNames.Count != modelsToTest.Count)
            {
                modelNames = new List<string>();
                for (int i = 0; i < modelsToTest.Count; i++)
                    modelNames.Add($"Model {i+1}");
            }

            // Ensure inputResolutions list matches modelsToTest list size
            if (inputResolutions == null || inputResolutions.Count != modelsToTest.Count)
            {
                inputResolutions = new List<int>();
                for (int i = 0; i < modelsToTest.Count; i++)
                    inputResolutions.Add(320); // Default resolution
            }

            UpdateStatus("Ready to start benchmark", Color.white);
        }

        private void InitializeCamera()
        {
            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices.Length == 0)
            {
                Debug.LogError("No camera devices found.");
                useCameraInput = false;
                return;
            }

            // Clamp to valid device index
            selectedCameraDevice = Mathf.Clamp(selectedCameraDevice, 0, devices.Length - 1);
            webCamTexture = new WebCamTexture(devices[selectedCameraDevice].name, 1280, 720, 30);
            webCamTexture.Play();
        }

        public void StartBenchmark()
        {
            if (isTesting)
                return;

            results.Clear();
            StartCoroutine(RunBenchmark());
        }

        private IEnumerator RunBenchmark()
        {
            isTesting = true;
            UpdateStatus("Starting benchmark...", Color.yellow);
            resultsText.text = "";

            yield return null; // Wait one frame to ensure UI updates

            // Run test for each model
            for (int modelIndex = 0; modelIndex < modelsToTest.Count; modelIndex++)
            {
                NNModel nnModel = modelsToTest[modelIndex];
                string modelName = modelNames[modelIndex];
                int inputResolution = inputResolutions[modelIndex];

                UpdateStatus($"Testing model: {modelName} ({modelIndex + 1}/{modelsToTest.Count})", Color.yellow);

                if (nnModel == null)
                {
                    Debug.LogError($"Model at index {modelIndex} is null. Skipping.");
                    continue;
                }

                // Prepare model
                Model model = ModelLoader.Load(nnModel);
                WorkerFactory.Type workerType = useGPU ? WorkerFactory.Type.ComputePrecompiled : WorkerFactory.Type.CSharp;
                
                // First try GPU if selected
                IWorker worker = null;
                try
                {
                    worker = WorkerFactory.CreateWorker(workerType, model);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to create {workerType} worker: {e.Message}. Falling back to CPU.");
                    workerType = WorkerFactory.Type.CSharp;
                    worker = WorkerFactory.CreateWorker(workerType, model);
                }

                // Prepare input
                Texture2D inputTexture = GetInputTexture(inputResolution);
                if (inputTexture == null)
                {
                    Debug.LogError("Failed to get input texture for testing.");
                    continue;
                }

                // Show the test image
                if (outputDisplay != null)
                    outputDisplay.texture = inputTexture;

                float[] processingTimes = new float[testIterations];
                float totalMemory = 0f;

                GC.Collect(); // Collect garbage before test
                yield return new WaitForSeconds(0.5f); // Wait to stabilize

                // Run warm-up iterations
                for (int i = 0; i < 5; i++)
                {
                    RunInference(worker, inputTexture, inputResolution);
                    yield return null;
                }

                // Run test iterations
                for (int i = 0; i < testIterations; i++)
                {
                    yield return null; // Wait one frame between iterations
                    
                    // Run inference and measure time
                    float startTime = Time.realtimeSinceStartup;
                    Tensor output = RunInference(worker, inputTexture, inputResolution);
                    float endTime = Time.realtimeSinceStartup;
                    
                    processingTimes[i] = (endTime - startTime) * 1000f; // Convert to ms
                    
                    output.Dispose();

                    // Get current memory usage
                    totalMemory += GC.GetTotalMemory(false) / (1024f * 1024f); // MB

                    // Update progress
                    if (i % 5 == 0 || i == testIterations - 1)
                    {
                        UpdateStatus($"Testing {modelName}: {i + 1}/{testIterations}", Color.yellow);
                    }

                    yield return null;
                }

                // Calculate statistics
                float sum = 0f;
                float min = float.MaxValue;
                float max = float.MinValue;

                foreach (float time in processingTimes)
                {
                    sum += time;
                    if (time < min) min = time;
                    if (time > max) max = time;
                }

                float avgTime = sum / testIterations;
                float avgMemory = totalMemory / testIterations;
                float fps = 1000f / avgTime;

                // Record results
                ModelTestResult result = new ModelTestResult
                {
                    modelName = modelName,
                    averageProcessingTime = avgTime,
                    minProcessingTime = min,
                    maxProcessingTime = max,
                    fps = fps,
                    memoryUsage = avgMemory,
                    inputResolution = inputResolution
                };

                results.Add(result);
                
                // Display result
                DisplayResult(result);

                // Clean up
                worker.Dispose();
                yield return null;
            }

            // Save results if enabled
            if (saveResultsToFile)
            {
                SaveResults();
            }

            isTesting = false;
            UpdateStatus("Benchmark completed", Color.green);
        }

        private Texture2D GetInputTexture(int resolution)
        {
            Texture2D inputTexture;
            
            if (useCameraInput && webCamTexture != null && webCamTexture.isPlaying)
            {
                // Create texture from webcam
                inputTexture = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
                inputTexture.SetPixels(webCamTexture.GetPixels());
                inputTexture.Apply();
            }
            else if (testImage != null)
            {
                // Use provided test image
                inputTexture = testImage;
            }
            else
            {
                // Create a dummy test image with a simple pattern
                inputTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
                Color[] colors = new Color[resolution * resolution];
                
                for (int y = 0; y < resolution; y++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        if ((x / 50) % 2 == (y / 50) % 2)
                            colors[y * resolution + x] = Color.white;
                        else
                            colors[y * resolution + x] = Color.gray;
                    }
                }
                
                inputTexture.SetPixels(colors);
                inputTexture.Apply();
            }

            // Resize to target resolution if needed
            if (inputTexture.width != resolution || inputTexture.height != resolution)
            {
                inputTexture = ResizeTexture(inputTexture, resolution, resolution);
            }

            return inputTexture;
        }

        private Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
            Graphics.Blit(source, rt);
            
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            
            Texture2D result = new Texture2D(targetWidth, targetHeight);
            result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            result.Apply();
            
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            
            return result;
        }

        private Tensor RunInference(IWorker worker, Texture2D inputTexture, int inputResolution)
        {
            // Preprocess input: normalize to [0,1] range
            using (var input = new Tensor(inputTexture, channels: 3))
            {
                // Execute network
                worker.Execute(input);
                return worker.PeekOutput();
            }
        }

        private void DisplayResult(ModelTestResult result)
        {
            string resultText = $"Model: {result.modelName}\n" +
                               $"Resolution: {result.inputResolution}x{result.inputResolution}\n" +
                               $"Avg Time: {result.averageProcessingTime:F2} ms\n" +
                               $"Min Time: {result.minProcessingTime:F2} ms\n" +
                               $"Max Time: {result.maxProcessingTime:F2} ms\n" +
                               $"FPS: {result.fps:F2}\n" +
                               $"Memory: {result.memoryUsage:F2} MB\n" +
                               $"----------------------------\n";

            resultsText.text += resultText;
            
            // Scroll to bottom of results
            if (resultsScrollView != null)
            {
                Canvas.ForceUpdateCanvases();
                resultsScrollView.verticalNormalizedPosition = 0f;
            }
        }

        private void SaveResults()
        {
            try
            {
                string json = JsonUtility.ToJson(new { results = results }, true);
                string path = Path.Combine(Application.persistentDataPath, resultsFilePath);
                File.WriteAllText(path, json);
                Debug.Log($"Benchmark results saved to: {path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving benchmark results: {e.Message}");
            }
        }

        private void UpdateStatus(string message, Color color)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = color;
            }
            Debug.Log(message);
        }

        private void OnDestroy()
        {
            if (webCamTexture != null)
            {
                webCamTexture.Stop();
            }
        }
    }
} 