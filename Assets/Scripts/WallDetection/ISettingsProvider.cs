using UnityEngine;
using System;

namespace Remalux.WallDetection
{
    /// <summary>
    /// Interface for providing settings to wall detection components without direct dependency on Settings module
    /// </summary>
    public interface ISettingsProvider
    {
        // Resolution settings
        Vector2Int GetCurrentResolution();
        Vector2Int CustomResolution { get; }
        
        // Processing settings
        float ProcessingInterval { get; }
        bool ShowPerformanceMetrics { get; }
        
        // Lighting settings
        bool AutoAdjustLighting { get; }
        float LowLightBoost { get; }
        float LowLightThreshold { get; }
        float ContrastEnhancement { get; }
        
        // Events
        event Action OnSettingsChanged;
    }
    
    /// <summary>
    /// Default implementation of ISettingsProvider that uses hardcoded values when Settings module is not available
    /// </summary>
    public class DefaultSettingsProvider : MonoBehaviour, ISettingsProvider
    {
        public static DefaultSettingsProvider Instance { get; private set; }
        
        [Header("Resolution Settings")]
        [SerializeField] private int _resolutionPreset = 1; // 0: 224x224, 1: 320x320, 2: 512x512, 3: 768x768
        [SerializeField] private Vector2Int _customResolution = new Vector2Int(320, 320);
        
        [Header("Processing Settings")]
        [SerializeField] private float _processingInterval = 0.1f;
        [SerializeField] private bool _showPerformanceMetrics = false;
        
        [Header("Lighting Settings")]
        [SerializeField] private bool _autoAdjustLighting = true;
        [SerializeField] private float _lowLightBoost = 1.5f;
        [SerializeField] private float _lowLightThreshold = 0.3f;
        [SerializeField] private float _contrastEnhancement = 1.2f;
        
        public event Action OnSettingsChanged;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        public Vector2Int GetCurrentResolution()
        {
            switch (_resolutionPreset)
            {
                case 0: return new Vector2Int(224, 224);
                case 1: return new Vector2Int(320, 320);
                case 2: return new Vector2Int(512, 512);
                case 3: return new Vector2Int(768, 768);
                case 4: return _customResolution;
                default: return new Vector2Int(320, 320);
            }
        }
        
        public Vector2Int CustomResolution => _customResolution;
        public float ProcessingInterval => _processingInterval;
        public bool ShowPerformanceMetrics => _showPerformanceMetrics;
        public bool AutoAdjustLighting => _autoAdjustLighting;
        public float LowLightBoost => _lowLightBoost;
        public float LowLightThreshold => _lowLightThreshold;
        public float ContrastEnhancement => _contrastEnhancement;
    }
} 