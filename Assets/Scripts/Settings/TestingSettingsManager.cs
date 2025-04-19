using System;
using System.Collections.Generic;
using UnityEngine;

namespace Remalux.Settings
{
    /// <summary>
    /// Manages settings for wall detection testing, including model parameters, 
    /// test image preferences, and result saving options.
    /// </summary>
    [CreateAssetMenu(fileName = "TestingSettings", menuName = "Remalux/Settings/Testing Settings")]
    public class TestingSettingsManager : ScriptableObject
    {
        [Serializable]
        public class ModelSettings
        {
            public string modelName = "DeepLabV3_MobileNet";
            public string modelPath = "Models/deeplabv3_mobilenet_v3_large";
            public Vector2Int inputSize = new Vector2Int(512, 512);
            public float confidenceThreshold = 0.5f;
            public bool applyPostProcessing = true;
        }

        [Serializable]
        public class TestImageSettings
        {
            public List<string> testImageFolders = new List<string>() { "TestImages/Indoor", "TestImages/Outdoor" };
            public bool includeSubfolders = true;
            public string[] supportedExtensions = new string[] { ".jpg", ".jpeg", ".png" };
        }

        [Serializable]
        public class ResultSettings
        {
            public bool saveResults = true;
            public string resultsFolderPath = "TestResults";
            public bool saveOriginalImages = true;
            public bool saveMasks = true;
            public bool saveProcessedImages = true;
            public bool generateReport = true;
        }

        [Serializable]
        public class UISettings
        {
            public bool showPerformanceStats = true;
            public bool showProcessingSettings = true;
            public bool autoRefreshResults = true;
            public float refreshRate = 1.0f;
        }

        [Header("Model Settings")]
        public List<ModelSettings> availableModels = new List<ModelSettings>();
        public int defaultModelIndex = 0;

        [Header("Test Image Settings")]
        public TestImageSettings testImageSettings = new TestImageSettings();

        [Header("Result Settings")]
        public ResultSettings resultSettings = new ResultSettings();

        [Header("UI Settings")]
        public UISettings uiSettings = new UISettings();
        
        [Header("Batch Testing")]
        public bool enableBatchTesting = false;
        public int batchSize = 10;
        public float delayBetweenTests = 0.5f;

        private void OnEnable()
        {
            // Add default model if list is empty
            if (availableModels.Count == 0)
            {
                availableModels.Add(new ModelSettings());
            }
        }

        /// <summary>
        /// Returns the currently selected model settings
        /// </summary>
        public ModelSettings GetCurrentModelSettings()
        {
            if (availableModels.Count == 0)
                return new ModelSettings();
                
            if (defaultModelIndex >= 0 && defaultModelIndex < availableModels.Count)
                return availableModels[defaultModelIndex];
                
            return availableModels[0];
        }
        
        /// <summary>
        /// Save the settings to PlayerPrefs
        /// </summary>
        public void SaveToPlayerPrefs()
        {
            string json = JsonUtility.ToJson(this);
            PlayerPrefs.SetString("TestingSettings", json);
            PlayerPrefs.Save();
            Debug.Log("Testing settings saved to PlayerPrefs");
        }
        
        /// <summary>
        /// Load the settings from PlayerPrefs if they exist
        /// </summary>
        public void LoadFromPlayerPrefs()
        {
            if (PlayerPrefs.HasKey("TestingSettings"))
            {
                string json = PlayerPrefs.GetString("TestingSettings");
                JsonUtility.FromJsonOverwrite(json, this);
                Debug.Log("Testing settings loaded from PlayerPrefs");
            }
        }
    }
} 