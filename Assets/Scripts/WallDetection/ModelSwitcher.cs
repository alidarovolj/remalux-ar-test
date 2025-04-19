using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Barracuda;

/// <summary>
/// Класс для динамического переключения между разными моделями DeepLabV3
/// </summary>
public class ModelSwitcher : MonoBehaviour
{
    [System.Serializable]
    public class ModelConfig
    {
        public string name;
        public NNModel modelAsset;
        public int recommendedResolution = 320;
        public float recommendedThreshold = 0.5f;
    }

    [Header("Модели DeepLabV3")]
    [Tooltip("Список моделей DeepLabV3 для тестирования")]
    public List<ModelConfig> availableModels = new List<ModelConfig>();

    [Header("Целевые компоненты")]
    [Tooltip("DeepLabDecoder для работы с моделью")]
    public DeepLabDecoder decoder;

    [Header("UI элементы")]
    [Tooltip("Дропдаун для выбора модели")]
    public TMP_Dropdown modelDropdown;
    
    [Tooltip("Дропдаун для выбора разрешения")]
    public TMP_Dropdown resolutionDropdown;
    
    [Tooltip("Слайдер для настройки порога уверенности")]
    public Slider thresholdSlider;
    
    [Tooltip("Текст, отображающий текущий порог")]
    public TextMeshProUGUI thresholdText;
    
    [Tooltip("Переключатель использования GPU")]
    public Toggle useGpuToggle;
    
    [Tooltip("Текст для отображения инфо о производительности")]
    public TextMeshProUGUI performanceText;

    [Header("Настройки")]
    [Tooltip("Интервал обновления статистики производительности (секунды)")]
    public float statsUpdateInterval = 0.5f;

    // Приватные поля
    private int[] resolutionOptions = new int[] { 224, 320, 512, 768 };
    private int currentModelIndex = 0;
    private float timeSinceLastStatsUpdate = 0;
    private DeepLabDecoder _wallDetector;

    private void Start()
    {
        if (decoder == null)
        {
            decoder = GetWallDetector();
            if (decoder == null)
            {
                Debug.LogError("DeepLabDecoder не найден! ModelSwitcher не будет работать.");
                enabled = false;
                return;
            }
        }

        SetupUI();
        
        // Первичная установка модели
        if (availableModels.Count > 0)
        {
            ApplyModelChange(0);
        }
    }

    private void Update()
    {
        // Обновление статистики производительности
        timeSinceLastStatsUpdate += Time.deltaTime;
        if (timeSinceLastStatsUpdate >= statsUpdateInterval)
        {
            UpdatePerformanceStats();
            timeSinceLastStatsUpdate = 0;
        }
    }

    private void SetupUI()
    {
        // Настройка дропдауна моделей
        if (modelDropdown != null)
        {
            modelDropdown.ClearOptions();
            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
            
            foreach (var model in availableModels)
            {
                options.Add(new TMP_Dropdown.OptionData(model.name));
            }
            
            modelDropdown.AddOptions(options);
            modelDropdown.onValueChanged.AddListener(ApplyModelChange);
        }
        
        // Настройка дропдауна разрешений
        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
            
            foreach (var res in resolutionOptions)
            {
                options.Add(new TMP_Dropdown.OptionData($"{res}x{res}"));
            }
            
            resolutionDropdown.AddOptions(options);
            
            // Устанавливаем начальное значение близкое к текущему разрешению
            int bestMatchIndex = 0;
            int minDiff = int.MaxValue;
            
            for (int i = 0; i < resolutionOptions.Length; i++)
            {
                int diff = Mathf.Abs(resolutionOptions[i] - decoder.inputWidth);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    bestMatchIndex = i;
                }
            }
            
            resolutionDropdown.value = bestMatchIndex;
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        }
        
        // Настройка слайдера порога
        if (thresholdSlider != null)
        {
            thresholdSlider.value = decoder.confidenceThreshold;
            thresholdSlider.onValueChanged.AddListener(OnThresholdChanged);
            
            if (thresholdText != null)
            {
                thresholdText.text = $"Порог: {decoder.confidenceThreshold:F2}";
            }
        }
        
        // Настройка переключателя GPU
        if (useGpuToggle != null)
        {
            useGpuToggle.isOn = decoder.useGPU;
            useGpuToggle.onValueChanged.AddListener(OnGpuToggleChanged);
        }
    }

    public void ApplyModelChange(int modelIndex)
    {
        if (modelIndex < 0 || modelIndex >= availableModels.Count)
        {
            Debug.LogError($"Неверный индекс модели: {modelIndex}");
            return;
        }

        currentModelIndex = modelIndex;
        var selectedModel = availableModels[modelIndex];
        
        // Устанавливаем рекомендуемые настройки
        if (thresholdSlider != null)
        {
            thresholdSlider.value = selectedModel.recommendedThreshold;
            OnThresholdChanged(selectedModel.recommendedThreshold);
        }
        
        // Находим ближайшее разрешение в списке опций
        if (resolutionDropdown != null)
        {
            int bestMatchIndex = 0;
            int minDiff = int.MaxValue;
            
            for (int i = 0; i < resolutionOptions.Length; i++)
            {
                int diff = Mathf.Abs(resolutionOptions[i] - selectedModel.recommendedResolution);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    bestMatchIndex = i;
                }
            }
            
            resolutionDropdown.value = bestMatchIndex;
            OnResolutionChanged(bestMatchIndex);
        }
        
        // Загружаем модель
        if (decoder.LoadNewModel(selectedModel.modelAsset))
        {
            Debug.Log($"Успешно переключили на модель: {selectedModel.name}");
        }
        else
        {
            Debug.LogError($"Не удалось переключиться на модель: {selectedModel.name}");
        }
    }

    private void OnResolutionChanged(int resIndex)
    {
        if (resIndex < 0 || resIndex >= resolutionOptions.Length)
            return;
            
        int newResolution = resolutionOptions[resIndex];
        decoder.inputWidth = newResolution;
        decoder.inputHeight = newResolution;
        
        Debug.Log($"Разрешение изменено на: {newResolution}x{newResolution}");
        
        // Повторно инициализируем модель с новым разрешением
        ApplyModelChange(currentModelIndex);
    }

    private void OnThresholdChanged(float newValue)
    {
        decoder.confidenceThreshold = newValue;
        
        if (thresholdText != null)
        {
            thresholdText.text = $"Порог: {newValue:F2}";
        }
    }

    private void OnGpuToggleChanged(bool useGpu)
    {
        decoder.useGPU = useGpu;
        
        // Повторно инициализируем модель с новыми настройками
        ApplyModelChange(currentModelIndex);
    }

    private void UpdatePerformanceStats()
    {
        if (performanceText != null && decoder != null)
        {
            float ms = decoder.lastProcessingTime * 1000f;
            float fps = (decoder.lastProcessingTime > 0) ? 1.0f / decoder.lastProcessingTime : 0;
            
            performanceText.text = $"Модель: {availableModels[currentModelIndex].name}\n" +
                                  $"Разрешение: {decoder.inputWidth}x{decoder.inputHeight}\n" +
                                  $"Время: {ms:F1} мс\n" +
                                  $"FPS: {fps:F1}\n" +
                                  $"Устройство: {(decoder.useGPU ? "GPU" : "CPU")}";
        }
    }

    /// <summary>
    /// Get wall detector component
    /// </summary>
    private DeepLabDecoder GetWallDetector()
    {
        if (_wallDetector == null)
        {
            _wallDetector = FindFirstObjectByType<DeepLabDecoder>();
        }
        return _wallDetector;
    }
}