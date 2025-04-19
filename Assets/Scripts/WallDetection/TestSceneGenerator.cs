using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Unity.Barracuda;

namespace Remalux.WallDetection
{
    /// <summary>
    /// Generates test scenes for wall detection with batch processing support.
    /// Handles loading test images, creating test UI, and running automated tests.
    /// </summary>
    public class TestSceneGenerator : MonoBehaviour
    {
        [Header("Model Configuration")]
        public List<NNModel> modelsToTest = new List<NNModel>();
        public List<string> modelLabels = new List<string>();
        
        [Header("Test Images")]
        public Texture2D[] testImages;
        public bool loadImagesFromDisk = true;
        public string testImageFolder = "TestImages";
        public bool includeSubfolders = true;
        
        [Header("Testing Components")]
        public DeepLabDecoder decoder;
        public WallDetectionTester detectionTester;
        public TestResultReporter resultReporter;
        
        [Header("UI Elements")]
        public RawImage originalImageDisplay;
        public RawImage maskImageDisplay;
        public RawImage finalResultDisplay;
        public Dropdown modelDropdown;
        public Button nextImageButton;
        public Button prevImageButton;
        public Button runBatchTestButton;
        public Button generateReportButton;
        public Text statusText;
        public Slider progressSlider;
        public Toggle saveResultToggle;
        
        [Header("Test Settings")]
        public MonoBehaviour testSettings;
        public bool applyPostProcessing = true;
        public float confidenceThreshold = 0.5f;
        
        // Private fields
        private List<Texture2D> loadedImages = new List<Texture2D>();
        private int currentImageIndex = 0;
        private int currentModelIndex = 0;
        private bool isBatchTesting = false;
        private Dictionary<string, List<float>> testResults = new Dictionary<string, List<float>>();
        
        private void Start()
        {
            // Load settings if available
            if (testSettings != null)
            {
                // Try to call LoadFromPlayerPrefs via reflection
                try
                {
                    var loadMethod = testSettings.GetType().GetMethod("LoadFromPlayerPrefs");
                    if (loadMethod != null)
                    {
                        loadMethod.Invoke(testSettings, null);
                    }
                    
                    ApplySettings();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to load test settings: {e.Message}");
                }
            }
            
            // Load test images
            LoadTestImages();
            
            // Setup UI
            SetupUI();
            
            // Initialize components
            InitializeComponents();
            
            // Show first image and model
            DisplayCurrentImage();
        }
        
        private void ApplySettings()
        {
            // Access settings via reflection to avoid direct dependency
            try
            {
                if (testSettings != null)
                {
                    var getSettingsMethod = testSettings.GetType().GetMethod("GetCurrentModelSettings");
                    if (getSettingsMethod != null)
                    {
                        var modelSettings = getSettingsMethod.Invoke(testSettings, null);
                        var settingsType = modelSettings.GetType();
                        
                        var thresholdField = settingsType.GetField("confidenceThreshold");
                        var postProcessingField = settingsType.GetField("applyPostProcessing");
                        
                        if (thresholdField != null)
                        {
                            confidenceThreshold = (float)thresholdField.GetValue(modelSettings);
                        }
                        
                        if (postProcessingField != null)
                        {
                            applyPostProcessing = (bool)postProcessingField.GetValue(modelSettings);
                        }
                    }
                }
                
                if (decoder != null)
                {
                    decoder.confidenceThreshold = confidenceThreshold;
                    
                    // Check if applyPostProcessing exists on decoder through reflection
                    var postProcessingField = decoder.GetType().GetField("applyPostProcessing");
                    if (postProcessingField != null)
                    {
                        postProcessingField.SetValue(decoder, applyPostProcessing);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error applying test settings: {e.Message}");
            }
        }
        
        private void LoadTestImages()
        {
            loadedImages.Clear();
            
            // First add any test images assigned in the inspector
            if (testImages != null && testImages.Length > 0)
            {
                loadedImages.AddRange(testImages);
            }
            
            // Then load images from disk if enabled
            if (loadImagesFromDisk)
            {
                string basePath = Path.Combine(Application.dataPath, testImageFolder);
                try
                {
                    string[] filePaths;
                    if (includeSubfolders)
                    {
                        filePaths = Directory.GetFiles(basePath, "*.*", SearchOption.AllDirectories)
                            .Where(file => IsImageFile(file)).ToArray();
                    }
                    else
                    {
                        filePaths = Directory.GetFiles(basePath)
                            .Where(file => IsImageFile(file)).ToArray();
                    }
                    
                    foreach (string filePath in filePaths)
                    {
                        try
                        {
                            byte[] fileData = File.ReadAllBytes(filePath);
                            Texture2D texture = new Texture2D(2, 2);
                            if (texture.LoadImage(fileData))
                            {
                                texture.name = Path.GetFileName(filePath);
                                loadedImages.Add(texture);
                                Debug.Log($"Loaded test image: {filePath}");
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Error loading image {filePath}: {e.Message}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error accessing test image folder: {e.Message}");
                }
            }
            
            Debug.Log($"Loaded {loadedImages.Count} test images");
            
            if (loadedImages.Count == 0)
            {
                // Create a default red/white test image if no images were loaded
                Texture2D defaultImage = new Texture2D(512, 512);
                Color[] colors = new Color[512 * 512];
                for (int i = 0; i < colors.Length; i++)
                {
                    int x = i % 512;
                    int y = i / 512;
                    colors[i] = (x < 256) ? Color.white : Color.red;
                }
                defaultImage.SetPixels(colors);
                defaultImage.Apply();
                defaultImage.name = "Default Test Image";
                loadedImages.Add(defaultImage);
            }
        }
        
        private bool IsImageFile(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png";
        }
        
        private void SetupUI()
        {
            // Setup model dropdown
            if (modelDropdown != null)
            {
                modelDropdown.ClearOptions();
                List<string> options = new List<string>();
                
                for (int i = 0; i < modelsToTest.Count; i++)
                {
                    string label = i < modelLabels.Count ? modelLabels[i] : $"Model {i+1}";
                    options.Add(label);
                }
                
                if (options.Count > 0)
                {
                    modelDropdown.AddOptions(options);
                    modelDropdown.onValueChanged.AddListener(OnModelChanged);
                }
                else
                {
                    options.Add("No models available");
                    modelDropdown.AddOptions(options);
                    modelDropdown.interactable = false;
                }
            }
            
            // Setup navigation buttons
            if (nextImageButton != null)
                nextImageButton.onClick.AddListener(NextImage);
                
            if (prevImageButton != null)
                prevImageButton.onClick.AddListener(PreviousImage);
                
            if (runBatchTestButton != null)
                runBatchTestButton.onClick.AddListener(StartBatchTest);
                
            if (generateReportButton != null)
                generateReportButton.onClick.AddListener(GenerateReport);
                
            // Update status text
            UpdateStatusText();
        }
        
        private void InitializeComponents()
        {
            // Initialize decoder if not already set
            if (decoder == null)
            {
                decoder = FindFirstObjectByType<DeepLabDecoder>();
                if (decoder == null)
                {
                    decoder = gameObject.AddComponent<DeepLabDecoder>();
                }
            }
            
            // Initialize wall detection tester
            if (detectionTester == null)
            {
                detectionTester = FindFirstObjectByType<WallDetectionTester>();
                if (detectionTester == null)
                {
                    detectionTester = gameObject.AddComponent<WallDetectionTester>();
                }
            }
            
            // Initialize result reporter
            if (resultReporter == null)
            {
                resultReporter = FindFirstObjectByType<TestResultReporter>();
                if (resultReporter == null)
                {
                    resultReporter = gameObject.AddComponent<TestResultReporter>();
                }
            }
            
            // Apply settings to components
            if (decoder != null)
            {
                decoder.confidenceThreshold = confidenceThreshold;
                decoder.applyPostProcessing = applyPostProcessing;
            }
        }
        
        private void DisplayCurrentImage()
        {
            if (loadedImages.Count == 0)
                return;
                
            if (currentImageIndex < 0 || currentImageIndex >= loadedImages.Count)
                currentImageIndex = 0;
                
            Texture2D currentImage = loadedImages[currentImageIndex];
            
            // Display the image
            if (originalImageDisplay != null)
            {
                originalImageDisplay.texture = currentImage;
            }
            
            // Process the image with the current model
            ProcessCurrentImage();
            
            // Update UI
            UpdateStatusText();
        }
        
        private void ProcessCurrentImage()
        {
            if (loadedImages.Count == 0 || currentImageIndex < 0 || currentImageIndex >= loadedImages.Count)
                return;
                
            Texture2D currentImage = loadedImages[currentImageIndex];
            
            if (decoder != null && currentModelIndex < modelsToTest.Count)
            {
                // Set the current model
                NNModel currentModel = modelsToTest[currentModelIndex];
                decoder.model = currentModel;
                
                // Process the image
                decoder.ProcessImage(currentImage);
                
                // Display result in UI
                if (maskImageDisplay != null && decoder.maskTexture != null)
                {
                    maskImageDisplay.texture = decoder.maskTexture;
                }
                
                // If we have a detection tester, use it to further process the results
                if (detectionTester != null)
                {
                    detectionTester.ProcessTestImage(currentImage, decoder.maskTexture);
                    
                    if (finalResultDisplay != null && detectionTester.resultTexture != null)
                    {
                        finalResultDisplay.texture = detectionTester.resultTexture;
                    }
                }
            }
        }
        
        private void OnModelChanged(int index)
        {
            currentModelIndex = index;
            ProcessCurrentImage();
            UpdateStatusText();
        }
        
        private void NextImage()
        {
            if (loadedImages.Count == 0)
                return;
                
            currentImageIndex = (currentImageIndex + 1) % loadedImages.Count;
            DisplayCurrentImage();
        }
        
        private void PreviousImage()
        {
            if (loadedImages.Count == 0)
                return;
                
            currentImageIndex = (currentImageIndex - 1 + loadedImages.Count) % loadedImages.Count;
            DisplayCurrentImage();
        }
        
        private void UpdateStatusText()
        {
            if (statusText == null)
                return;
                
            string modelName = "No model";
            if (modelsToTest.Count > 0 && currentModelIndex < modelsToTest.Count)
            {
                modelName = currentModelIndex < modelLabels.Count ? 
                    modelLabels[currentModelIndex] : modelsToTest[currentModelIndex].name;
            }
            
            string imageName = "No image";
            if (loadedImages.Count > 0 && currentImageIndex < loadedImages.Count)
            {
                imageName = loadedImages[currentImageIndex].name;
            }
            
            statusText.text = $"Image: {currentImageIndex + 1}/{loadedImages.Count} [{imageName}]\n" +
                             $"Model: {modelName}\n" +
                             $"Settings: Threshold={confidenceThreshold}, PostProcess={applyPostProcessing}";
        }
        
        public void StartBatchTest()
        {
            if (isBatchTesting || loadedImages.Count == 0 || modelsToTest.Count == 0)
                return;
                
            StartCoroutine(RunBatchTest());
        }
        
        private IEnumerator RunBatchTest()
        {
            isBatchTesting = true;
            testResults.Clear();
            
            if (statusText != null)
                statusText.text = "Starting batch test...";
                
            if (progressSlider != null)
            {
                progressSlider.minValue = 0;
                progressSlider.maxValue = modelsToTest.Count * loadedImages.Count;
                progressSlider.value = 0;
            }
            
            // Initialize result reporter for this test session
            if (resultReporter != null)
            {
                resultReporter.InitializeSession();
            }
            
            int progress = 0;
            
            // Test each model
            for (int modelIdx = 0; modelIdx < modelsToTest.Count; modelIdx++)
            {
                NNModel model = modelsToTest[modelIdx];
                string modelName = modelIdx < modelLabels.Count ? modelLabels[modelIdx] : model.name;
                
                if (decoder != null)
                    decoder.model = model;
                    
                List<float> processingTimes = new List<float>();
                testResults[modelName] = processingTimes;
                
                // Test each image with this model
                for (int imgIdx = 0; imgIdx < loadedImages.Count; imgIdx++)
                {
                    Texture2D testImage = loadedImages[imgIdx];
                    
                    if (statusText != null)
                    {
                        statusText.text = $"Testing {modelName} on image {imgIdx + 1}/{loadedImages.Count}...";
                    }
                    
                    // Process the image and measure time
                    float startTime = Time.realtimeSinceStartup;
                    
                    if (decoder != null)
                    {
                        decoder.ProcessImage(testImage);
                        
                        // Wait for processing to complete
                        yield return null;
                        
                        float processingTime = (Time.realtimeSinceStartup - startTime) * 1000f; // Convert to ms
                        processingTimes.Add(processingTime);
                        
                        // If we have a detection tester, use it to further process the results
                        if (detectionTester != null)
                        {
                            detectionTester.ProcessTestImage(testImage, decoder.maskTexture);
                        }
                        
                        // If we have a result reporter, add the result
                        if (resultReporter != null && saveResultToggle != null && saveResultToggle.isOn)
                        {
                            // Create a test result
                            TestResultReporter.TestResult result = new TestResultReporter.TestResult
                            {
                                modelName = modelName,
                                imageName = testImage.name,
                                inputSize = new Vector2Int(testImage.width, testImage.height),
                                confidenceThreshold = confidenceThreshold,
                                postProcessingApplied = applyPostProcessing,
                                processingTimeMs = processingTime,
                                testTime = DateTime.Now
                            };
                            
                            // Add additional metrics if available
                            if (detectionTester != null)
                            {
                                result.wallCoverage = detectionTester.GetWallCoverage();
                                result.edgeAccuracy = detectionTester.CalculateEdgeQuality();
                                result.noiseLevel = detectionTester.CalculateNoiseLevel();
                            }
                            
                            resultReporter.AddResult(result);
                        }
                    }
                    
                    // Update progress
                    progress++;
                    if (progressSlider != null)
                        progressSlider.value = progress;
                        
                    // Small delay to avoid locking the UI
                    yield return new WaitForSeconds(0.1f);
                }
            }
            
            // Finalize the test session
            if (resultReporter != null && saveResultToggle != null && saveResultToggle.isOn)
            {
                resultReporter.FinalizeSession();
            }
            
            // Show summary
            ShowTestResults();
            
            isBatchTesting = false;
        }
        
        private void ShowTestResults()
        {
            string resultText = "Batch Test Results:\n\n";
            
            foreach (var modelResult in testResults)
            {
                string modelName = modelResult.Key;
                List<float> times = modelResult.Value;
                
                if (times.Count > 0)
                {
                    float avgTime = times.Average();
                    float minTime = times.Min();
                    float maxTime = times.Max();
                    float fps = 1000f / avgTime;
                    
                    resultText += $"{modelName}:\n" +
                                 $"  Avg: {avgTime:F2} ms\n" +
                                 $"  Min: {minTime:F2} ms\n" +
                                 $"  Max: {maxTime:F2} ms\n" +
                                 $"  FPS: {fps:F2}\n\n";
                }
            }
            
            if (statusText != null)
            {
                statusText.text = resultText;
            }
            
            Debug.Log(resultText);
        }
        
        public void GenerateReport()
        {
            if (resultReporter != null)
            {
                resultReporter.FinalizeSession();
                
                if (statusText != null)
                {
                    statusText.text = "Report generated successfully!";
                }
            }
        }
        
        public float CalculateEdgeQuality()
        {
            // This would be a more sophisticated algorithm in a real implementation
            // For now, return a random value for demonstration
            return UnityEngine.Random.Range(0.7f, 0.95f);
        }
        
        public float CalculateNoiseLevel()
        {
            // This would be a more sophisticated algorithm in a real implementation
            // For now, return a random value for demonstration
            return UnityEngine.Random.Range(0.05f, 0.3f);
        }
    }
} 