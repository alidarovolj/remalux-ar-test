using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Remalux.WallDetection;
using Remalux.WallPainting;

namespace Remalux.Settings
{
    /// <summary>
    /// Class for managing application settings
    /// </summary>
    public class AppSettings : MonoBehaviour
    {
        [Serializable]
        public class Settings
        {
            // Wall Detection Settings
            [Header("Wall Detection")]
            public int resolutionPreset = 1; // 0: 224x224, 1: 320x320, 2: 512x512, 3: 768x768
            public bool autoAdjustLighting = true;
            public float lowLightThreshold = 0.3f;
            public float lowLightBoost = 1.5f;
            public float contrastEnhancement = 1.2f;
            public float processingInterval = 0.2f; // Seconds between processing frames
            
            // Painting Settings
            [Header("Painting")]
            public float brushSize = 20f;
            public float brushOpacity = 0.8f;
            public Color[] recentColors = new Color[]
            {
                new Color(1, 0, 0, 1),      // Red
                new Color(0, 0, 1, 1),      // Blue
                new Color(0, 1, 0, 1),      // Green
                new Color(1, 1, 0, 1),      // Yellow
                new Color(1, 0, 1, 1),      // Magenta
                new Color(0, 1, 1, 1),      // Cyan
                new Color(1, 1, 1, 1),      // White
                new Color(0, 0, 0, 1)       // Black
            };
            public Color currentColor = Color.white;
            public bool useCoroutineForPainting = true;
            public float paintingInterval = 0.05f;
            
            // UI Settings
            [Header("UI")]
            public bool showPerformanceMetrics = false;
            public bool showMask = false;
            public bool showDetectionEdges = false;
            public float uiScale = 1.0f;
            
            // Performance Settings
            [Header("Performance")]
            public bool highQualityMode = false; // Higher quality but slower performance
            public bool saveThumbnails = true;
        }
        
        // Singleton instance
        public static AppSettings Instance { get; private set; }
        
        // Current settings
        public Settings CurrentSettings = new Settings();
        
        // File path for saving settings
        private string settingsFilePath;
        
        // Events
        public event Action OnSettingsChanged;
        
        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Set settings file path
            settingsFilePath = Path.Combine(Application.persistentDataPath, "app_settings.json");
            
            // Load settings
            LoadSettings();
        }
        
        public void SaveSettings()
        {
            try
            {
                string json = JsonUtility.ToJson(CurrentSettings, true);
                File.WriteAllText(settingsFilePath, json);
                Debug.Log("Settings saved successfully");
                
                // Notify listeners
                OnSettingsChanged?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving settings: {e.Message}");
            }
        }
        
        public void LoadSettings()
        {
            try
            {
                if (File.Exists(settingsFilePath))
                {
                    string json = File.ReadAllText(settingsFilePath);
                    CurrentSettings = JsonUtility.FromJson<Settings>(json);
                    Debug.Log("Settings loaded successfully");
                    
                    // Notify listeners
                    OnSettingsChanged?.Invoke();
                }
                else
                {
                    // Create default settings
                    CurrentSettings = new Settings();
                    SaveSettings();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading settings: {e.Message}");
                
                // Create default settings
                CurrentSettings = new Settings();
            }
        }
        
        public void ResetToDefaults()
        {
            CurrentSettings = new Settings();
            SaveSettings();
        }
        
        public void AddRecentColor(Color color)
        {
            // Check if color already exists
            bool exists = false;
            for (int i = 0; i < CurrentSettings.recentColors.Length; i++)
            {
                if (ColorEquals(CurrentSettings.recentColors[i], color))
                {
                    exists = true;
                    
                    // Move to front if not already at front
                    if (i > 0)
                    {
                        Color temp = CurrentSettings.recentColors[i];
                        
                        // Shift colors down
                        for (int j = i; j > 0; j--)
                        {
                            CurrentSettings.recentColors[j] = CurrentSettings.recentColors[j - 1];
                        }
                        
                        // Move to front
                        CurrentSettings.recentColors[0] = temp;
                    }
                    
                    break;
                }
            }
            
            // If color doesn't exist, add to front and shift others down
            if (!exists)
            {
                for (int i = CurrentSettings.recentColors.Length - 1; i > 0; i--)
                {
                    CurrentSettings.recentColors[i] = CurrentSettings.recentColors[i - 1];
                }
                
                CurrentSettings.recentColors[0] = color;
            }
            
            // Set as current color
            CurrentSettings.currentColor = color;
            
            // Save settings
            SaveSettings();
        }
        
        private bool ColorEquals(Color a, Color b, float tolerance = 0.01f)
        {
            return Mathf.Abs(a.r - b.r) < tolerance &&
                   Mathf.Abs(a.g - b.g) < tolerance &&
                   Mathf.Abs(a.b - b.b) < tolerance &&
                   Mathf.Abs(a.a - b.a) < tolerance;
        }
        
        // Apply settings to the specified DeepLabDecoder
        public void ApplyToWallDetector(DeepLabDecoder detector)
        {
            if (detector == null)
                return;
                
            detector.resolutionPreset = (DeepLabDecoder.InputResolution)CurrentSettings.resolutionPreset;
            detector.autoAdjustLighting = CurrentSettings.autoAdjustLighting;
            detector.lowLightThreshold = CurrentSettings.lowLightThreshold;
            detector.lowLightBoost = CurrentSettings.lowLightBoost;
            detector.contrastEnhancement = CurrentSettings.contrastEnhancement;
            detector.processingInterval = CurrentSettings.processingInterval;
            detector.showPerformanceMetrics = CurrentSettings.showPerformanceMetrics;
        }
        
        // Apply settings to the specified WallPainter
        public void ApplyToWallPainter(WallPainter painter)
        {
            if (painter == null)
                return;
                
            painter.brushSize = CurrentSettings.brushSize;
            painter.brushOpacity = CurrentSettings.brushOpacity;
            painter.SetBrushColor(CurrentSettings.currentColor);
        }
        
        private void OnApplicationQuit()
        {
            SaveSettings();
        }
    }
} 