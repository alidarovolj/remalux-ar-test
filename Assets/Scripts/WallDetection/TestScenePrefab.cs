using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using TMPro;

/// <summary>
/// Класс для настройки тестовой сцены для отладки распознавания стен
/// </summary>
public class TestScenePrefab : MonoBehaviour
{
    [Header("AR компоненты")]
    [SerializeField] private ARSession arSession;
    [SerializeField] private ARCameraManager arCameraManager;
    
    [Header("DeepLabV3 компоненты")]
    [SerializeField] private DeepLabDecoder deeplabDecoder;
    [SerializeField] private ModelSwitcher modelSwitcher;
    
    [Header("UI элементы")]
    [SerializeField] private RawImage cameraPreview;
    [SerializeField] private RawImage maskPreview;
    [SerializeField] private Transform uiRoot;
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private TextMeshProUGUI statusText;
    
    [Header("Настройки")]
    [SerializeField] private bool startInTestMode = true;
    [SerializeField] private List<ModelSwitcher.ModelConfig> defaultModels = new List<ModelSwitcher.ModelConfig>();
    
    private bool isTestMode = false;
    private Camera mainCamera;
    
    private void Awake()
    {
        // Проверяем наличие всех необходимых компонентов
        CheckComponents();
        
        // Инициализируем камеру
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Основная камера не найдена!");
        }
    }
    
    private void Start()
    {
        // Инициализация интерфейса
        SetupUI();
        
        // Добавляем модели в modelSwitcher, если не добавлены
        if (modelSwitcher != null && defaultModels.Count > 0 && modelSwitcher.availableModels.Count == 0)
        {
            foreach (var model in defaultModels)
            {
                modelSwitcher.availableModels.Add(model);
            }
        }
        
        // Устанавливаем начальный режим
        SetTestMode(startInTestMode);
    }
    
    private void Update()
    {
        // Обновление текста статуса
        UpdateStatusText();
        
        // Update preview textures
        UpdateUITextures();
    }
    
    private void CheckComponents()
    {
        if (arSession == null)
        {
            arSession = FindFirstObjectByType<ARSession>();
            if (arSession == null)
            {
                Debug.LogWarning("ARSession не найден! AR функции будут недоступны.");
            }
        }
        
        if (arCameraManager == null)
        {
            arCameraManager = FindFirstObjectByType<ARCameraManager>();
            if (arCameraManager == null)
            {
                Debug.LogWarning("ARCameraManager не найден! AR функции будут недоступны.");
            }
        }
        
        if (deeplabDecoder == null)
        {
            deeplabDecoder = FindFirstObjectByType<DeepLabDecoder>();
            if (deeplabDecoder == null)
            {
                Debug.LogError("DeepLabDecoder не найден! Тестирование невозможно.");
                enabled = false;
                return;
            }
        }
        
        if (modelSwitcher == null)
        {
            modelSwitcher = FindFirstObjectByType<ModelSwitcher>();
            if (modelSwitcher == null)
            {
                Debug.LogWarning("ModelSwitcher не найден! Функции переключения моделей недоступны.");
            }
        }
        
        if (uiRoot == null)
        {
            Debug.LogWarning("UI корневой элемент не назначен! UI будет недоступен.");
        }
    }
    
    private void SetupUI()
    {
        if (uiRoot == null) return;
        
        // Если панели предпросмотров не заданы, ищем их
        if (cameraPreview == null)
        {
            cameraPreview = uiRoot.GetComponentInChildren<RawImage>(true);
            if (cameraPreview != null)
            {
                Debug.Log("Автоматически найден cameraPreview.");
            }
        }
        
        if (maskPreview == null)
        {
            // Ищем второй RawImage, который должен быть maskPreview
            RawImage[] rawImages = uiRoot.GetComponentsInChildren<RawImage>(true);
            if (rawImages.Length > 1 && rawImages[0] != maskPreview)
            {
                maskPreview = rawImages[1];
                Debug.Log("Автоматически найден maskPreview.");
            }
        }
        
        // Находим statusText, если не задан
        if (statusText == null)
        {
            statusText = uiRoot.GetComponentInChildren<TextMeshProUGUI>(true);
            if (statusText != null)
            {
                Debug.Log("Автоматически найден statusText.");
            }
        }
        
        // Настраиваем modelSwitcher, если он доступен
        if (modelSwitcher != null)
        {
            modelSwitcher.decoder = deeplabDecoder;
        }
    }
    
    private void UpdateUITextures()
    {
        if (deeplabDecoder != null)
        {
            if (cameraPreview != null && deeplabDecoder.cameraTexture != null)
            {
                cameraPreview.texture = deeplabDecoder.cameraTexture;
            }
            
            if (maskPreview != null && deeplabDecoder.maskTexture != null)
            {
                maskPreview.texture = deeplabDecoder.maskTexture;
            }
        }
    }
    
    /// <summary>
    /// Переключение между режимом тестирования и AR
    /// </summary>
    public void ToggleTestMode()
    {
        SetTestMode(!isTestMode);
    }
    
    /// <summary>
    /// Установка режима тестирования
    /// </summary>
    public void SetTestMode(bool enable)
    {
        isTestMode = enable;
        
        if (arSession != null)
        {
            arSession.enabled = !isTestMode;
        }
        
        if (mainCamera != null)
        {
            mainCamera.enabled = isTestMode;
        }
        
        if (arCameraManager != null)
        {
            arCameraManager.enabled = !isTestMode;
        }
        
        // Включаем/выключаем статистику в зависимости от режима
        if (statsPanel != null)
        {
            statsPanel.SetActive(isTestMode);
        }
        
        Debug.Log(isTestMode ? "Включен режим тестирования" : "Включен AR режим");
    }
    
    /// <summary>
    /// Обновление текста статуса
    /// </summary>
    private void UpdateStatusText()
    {
        if (statusText == null) return;
        
        string mode = isTestMode ? "Тестирование" : "AR";
        string modelInfo = "";
        
        if (modelSwitcher != null && modelSwitcher.availableModels.Count > 0 && modelSwitcher.modelDropdown != null)
        {
            int index = modelSwitcher.modelDropdown.value;
            if (index >= 0 && index < modelSwitcher.availableModels.Count)
            {
                modelInfo = $"Модель: {modelSwitcher.availableModels[index].name}";
            }
        }
        
        string resolution = $"Разрешение: {deeplabDecoder.inputWidth}x{deeplabDecoder.inputHeight}";
        
        statusText.text = $"Режим: {mode}\n{modelInfo}\n{resolution}";
    }

    /// <summary>
    /// Вызывается при уничтожении объекта
    /// </summary>
    private void OnDestroy()
    {
        // Освобождаем ресурсы, если необходимо
    }
} 