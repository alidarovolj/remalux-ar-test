using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Profiling;

namespace WallDetection
{
    /// <summary>
    /// PerformanceProfiler отслеживает и записывает метрики производительности во время выполнения
    /// Включает мониторинг FPS, использования CPU, памяти и времени обработки
    /// </summary>
    public class PerformanceProfiler : MonoBehaviour
    {
        [Header("Общие настройки")]
        [Tooltip("Активировать профилирование")]
        public bool enableProfiler = true;
        
        [Tooltip("Интервал сбора данных (в секундах)")]
        public float samplingInterval = 0.5f;
        
        [Tooltip("Интервал записи в файл (в секундах)")]
        public float loggingInterval = 5.0f;
        
        [Tooltip("Максимальная продолжительность профилирования (в секундах, 0 = без ограничений)")]
        public float maxProfilingDuration = 0f;
        
        [Tooltip("Тег для идентификации текущей сессии профилирования")]
        public string sessionTag = "DefaultSession";
        
        [Tooltip("Имя модели (для сравнения разных моделей)")]
        public string modelName = "Unknown";
        
        [Tooltip("Разрешение входа модели")]
        public string modelResolution = "Unknown";
        
        [Header("Опции логирования")]
        [Tooltip("Логировать FPS")]
        public bool logFPS = true;
        
        [Tooltip("Логировать использование CPU")]
        public bool logCPU = true;
        
        [Tooltip("Логировать использование памяти")]
        public bool logMemory = true;
        
        [Tooltip("Логировать время обработки кадров")]
        public bool logFrameTime = true;
        
        [Tooltip("Сохранять логи в файл")]
        public bool saveToFile = true;
        
        [Tooltip("Путь для сохранения логов (относительно папки Application.persistentDataPath)")]
        public string logFolderPath = "PerformanceLogs";
        
        [Header("UI элементы (опционально)")]
        [Tooltip("Текстовое поле для отображения FPS")]
        public TextMeshProUGUI fpsText;
        
        [Tooltip("Текстовое поле для отображения использования CPU")]
        public TextMeshProUGUI cpuText;
        
        [Tooltip("Текстовое поле для отображения использования памяти")]
        public TextMeshProUGUI memoryText;
        
        [Tooltip("Текстовое поле для отображения времени обработки")]
        public TextMeshProUGUI processingTimeText;
        
        [Tooltip("Текстовое поле для отображения статистики профилирования")]
        public TextMeshProUGUI statisticsText;
        
        [Tooltip("Ссылка на компонент DeepLabDecoder для получения времени обработки")]
        public DeepLabDecoder deepLabDecoder;

        [SerializeField] private Slider memoryBar;
        [SerializeField] private Slider cpuBar;
        [SerializeField] private Button logButton;
        [SerializeField] private GameObject statsPanel;

        // Переменные для подсчета FPS и времени
        private int _frameCounter = 0;
        private float _timeCounter = 0f;
        private float _lastFrameRate = 0f;
        private float _loggingTimeCounter = 0f;
        private float _totalProfilingTime = 0f;
        private bool _isProfilingActive = false;
        private float deltaTime = 0.0f;
        private List<PerformanceSample> performanceSamples = new List<PerformanceSample>();
        private float timeSinceLastSample = 0f;
        private StreamWriter logWriter;

        // Структура для хранения метрик производительности
        [System.Serializable]
        private class PerformanceSample
        {
            public float timestamp;
            public float fps;
            public float totalMemoryMB;
            public float allocatedMemoryMB;
            public float cpuUsage; // Approximation based on deltaTime
            public float processingTimeMs;
            public string modelName;
            public int modelResolution;
        }

        private void Start()
        {
            InitializeComponents();
            InitializeLogging();
        }
        
        private void OnDestroy()
        {
            if (logWriter != null)
            {
                logWriter.Close();
                logWriter.Dispose();
            }
        }
        
        private void InitializeComponents()
        {
            if (statsPanel != null)
            {
                statsPanel.SetActive(true);
            }
            
            if (logButton != null)
            {
                logButton.onClick.AddListener(SaveLogToFile);
            }
            
            if (deepLabDecoder == null)
            {
                deepLabDecoder = FindObjectOfType<DeepLabDecoder>();
                if (deepLabDecoder == null)
                {
                    Debug.LogWarning("DeepLabDecoder not found. Performance metrics for model will not be available.");
                }
            }
            
            // Initialize UI bars
            if (memoryBar != null)
            {
                memoryBar.minValue = 0;
                memoryBar.maxValue = 1;
                memoryBar.value = 0;
            }
            
            if (cpuBar != null)
            {
                cpuBar.minValue = 0;
                cpuBar.maxValue = 100;
                cpuBar.value = 0;
            }
        }
        
        private void InitializeLogging()
        {
            if (saveToFile)
            {
                try
                {
                    string fullPath = Path.Combine(Application.persistentDataPath, logFolderPath, $"PerfLog_{sessionTag}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                    string directory = Path.GetDirectoryName(fullPath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    
                    logWriter = new StreamWriter(fullPath, false);
                    logWriter.WriteLine("Timestamp,FPS,TotalMemoryMB,AllocatedMemoryMB,CPUUsage,ProcessingTimeMs,ModelName,ModelResolution");
                    logWriter.Flush();
                    
                    Debug.Log($"Performance log initialized at: {fullPath}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error initializing performance log: {e.Message}");
                    saveToFile = false;
                }
            }
        }
        
        private void Update()
        {
            if (!enableProfiler) return;
            
            // Update FPS calculation
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            
            // Sample performance metrics at the specified interval
            timeSinceLastSample += Time.deltaTime;
            if (timeSinceLastSample >= samplingInterval)
            {
                SamplePerformance();
                UpdateUI();
                timeSinceLastSample = 0f;
            }
            
            // Log performance data at logging interval
            _loggingTimeCounter += Time.deltaTime;
            if (_loggingTimeCounter >= loggingInterval && saveToFile)
            {
                WritePerformanceLog();
                _loggingTimeCounter = 0f;
            }
            
            // Check for max profiling duration
            if (maxProfilingDuration > 0)
            {
                _totalProfilingTime += Time.deltaTime;
                if (_totalProfilingTime >= maxProfilingDuration && _isProfilingActive)
                {
                    StopProfiling();
                }
            }
        }
        
        private void SamplePerformance()
        {
            // Calculate FPS
            _frameCounter++;
            _timeCounter += Time.deltaTime;
            if (_timeCounter >= 1.0f)
            {
                _lastFrameRate = _frameCounter / _timeCounter;
                _frameCounter = 0;
                _timeCounter = 0;
            }
            
            // Create a new performance sample
            PerformanceSample sample = new PerformanceSample
            {
                timestamp = Time.time,
                fps = 1.0f / deltaTime,
                totalMemoryMB = (float)GC.GetTotalMemory(false) / (1024 * 1024),
                allocatedMemoryMB = Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024),
                cpuUsage = Mathf.Clamp01(deltaTime / (1.0f / 60.0f)) * 100f, // Simple CPU usage approximation
                processingTimeMs = deepLabDecoder != null ? deepLabDecoder.lastProcessingTime * 1000f : 0,
                modelName = modelName,
                modelResolution = deepLabDecoder != null ? deepLabDecoder.inputWidth : 0
            };
            
            performanceSamples.Add(sample);
            
            // Keep the list at a reasonable size
            if (performanceSamples.Count > 300) // About 5 minutes of data at 0.5s sampling
            {
                performanceSamples.RemoveAt(0);
            }
        }
        
        private void UpdateUI()
        {
            if (performanceSamples.Count == 0) return;
            
            PerformanceSample latest = performanceSamples[performanceSamples.Count - 1];
            
            if (fpsText != null)
            {
                fpsText.text = $"FPS: {latest.fps:F1}";
            }
            
            if (cpuText != null)
            {
                cpuText.text = $"CPU: {latest.cpuUsage:F1}%";
            }
            
            if (memoryText != null)
            {
                memoryText.text = $"MEM: {latest.totalMemoryMB:F1} MB";
            }
            
            if (processingTimeText != null)
            {
                processingTimeText.text = $"Processing: {latest.processingTimeMs:F1} ms";
            }
            
            if (statisticsText != null)
            {
                // Calculate averages for the last 10 samples
                int sampleCount = Mathf.Min(10, performanceSamples.Count);
                float avgFps = 0, avgMem = 0, avgCpu = 0, avgProc = 0;
                
                for (int i = performanceSamples.Count - sampleCount; i < performanceSamples.Count; i++)
                {
                    avgFps += performanceSamples[i].fps;
                    avgMem += performanceSamples[i].totalMemoryMB;
                    avgCpu += performanceSamples[i].cpuUsage;
                    avgProc += performanceSamples[i].processingTimeMs;
                }
                
                avgFps /= sampleCount;
                avgMem /= sampleCount;
                avgCpu /= sampleCount;
                avgProc /= sampleCount;
                
                statisticsText.text = $"AVG (10s):\nFPS: {avgFps:F1}\nMEM: {avgMem:F1} MB\nCPU: {avgCpu:F1}%\nPROC: {avgProc:F1} ms";
            }
            
            // Update UI bars
            if (memoryBar != null)
            {
                // Assume 4GB is the max memory (adjust as needed)
                float maxMemory = 4096f;
                memoryBar.value = latest.totalMemoryMB / maxMemory;
            }
            
            if (cpuBar != null)
            {
                cpuBar.value = latest.cpuUsage;
            }
        }
        
        public void SaveLogToFile()
        {
            if (performanceSamples.Count == 0) return;
            
            try
            {
                string fullPath = Path.Combine(Application.persistentDataPath, logFolderPath, $"PerfLog_Manual_{sessionTag}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                string directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                using (StreamWriter writer = new StreamWriter(fullPath, false))
                {
                    writer.WriteLine("Timestamp,FPS,TotalMemoryMB,AllocatedMemoryMB,CPUUsage,ProcessingTimeMs,ModelName,ModelResolution");
                    
                    foreach (var sample in performanceSamples)
                    {
                        writer.WriteLine($"{sample.timestamp:F1},{sample.fps:F1},{sample.totalMemoryMB:F1},{sample.allocatedMemoryMB:F1}," +
                                        $"{sample.cpuUsage:F1},{sample.processingTimeMs:F1},{sample.modelName},{sample.modelResolution}");
                    }
                }
                
                Debug.Log($"Performance log saved to: {fullPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving performance log: {e.Message}");
            }
        }
        
        private void WritePerformanceLog()
        {
            if (logWriter == null || performanceSamples.Count == 0) return;
            
            try
            {
                // Write all new samples since last logging
                int startIdx = Mathf.Max(0, performanceSamples.Count - 10); // Last 10 samples
                
                for (int i = startIdx; i < performanceSamples.Count; i++)
                {
                    var sample = performanceSamples[i];
                    logWriter.WriteLine($"{sample.timestamp:F1},{sample.fps:F1},{sample.totalMemoryMB:F1},{sample.allocatedMemoryMB:F1}," +
                                      $"{sample.cpuUsage:F1},{sample.processingTimeMs:F1},{sample.modelName},{sample.modelResolution}");
                }
                
                logWriter.Flush();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error writing to performance log: {e.Message}");
                saveToFile = false;
            }
        }
        
        public void StartProfiling()
        {
            enableProfiler = true;
            _isProfilingActive = true;
            _totalProfilingTime = 0f;
            performanceSamples.Clear();
            Debug.Log("Performance profiling started");
        }
        
        public void StopProfiling()
        {
            _isProfilingActive = false;
            
            if (saveToFile)
            {
                SaveLogToFile();
            }
            
            Debug.Log($"Performance profiling stopped. Duration: {_totalProfilingTime:F1}s");
        }
        
        public void ResetProfilingData()
        {
            performanceSamples.Clear();
            _frameCounter = 0;
            _timeCounter = 0f;
            _lastFrameRate = 0f;
            _loggingTimeCounter = 0f;
            _totalProfilingTime = 0f;
            
            Debug.Log("Performance profiling data reset");
        }
    }
} 