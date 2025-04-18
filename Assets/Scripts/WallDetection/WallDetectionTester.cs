using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

/// <summary>
/// Класс для анализа и отладки результатов распознавания стен
/// </summary>
public class WallDetectionTester : MonoBehaviour
{
    [Header("DeepLabV3 компоненты")]
    [SerializeField] private DeepLabDecoder deeplabDecoder;
    
    [Header("UI элементы")]
    [SerializeField] private RawImage originalImage;
    [SerializeField] private RawImage processedMask;
    [SerializeField] private RawImage finalResult;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private Button captureButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Toggle showEdgesToggle;
    [SerializeField] private Toggle showOverlayToggle;
    
    [Header("Настройки тестирования")]
    [SerializeField] private bool autoCapture = false;
    [SerializeField] private float autoCaptureInterval = 5f;
    [SerializeField] private bool saveResults = false;
    [SerializeField] private string savePath = "WallDetectionTests";
    
    // Внутренние переменные
    private Texture2D capturedOriginal;
    private Texture2D capturedMask;
    private Texture2D capturedResult;
    private bool showEdges = false;
    private bool showOverlay = true;
    private float nextCaptureTime = 0f;
    private int captureCount = 0;
    private Dictionary<string, List<float>> performanceStats = new Dictionary<string, List<float>>();
    
    // Аналитические данные
    private int totalFrames = 0;
    private int successfulDetections = 0;
    private float averageDetectionTime = 0f;
    
    private void Start()
    {
        InitializeComponents();
        InitializeUI();
        
        // Создаем папку для сохранения результатов, если включено
        if (saveResults && !Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }
    }
    
    private void Update()
    {
        if (deeplabDecoder == null) return;
        
        // Обновляем статистику
        UpdateStatistics();
        
        // Автоматический захват кадра через заданный интервал
        if (autoCapture && Time.time >= nextCaptureTime)
        {
            CaptureFrame();
            nextCaptureTime = Time.time + autoCaptureInterval;
        }
        
        // Обновляем итоговое изображение если нужно отобразить края или наложение
        if (capturedOriginal != null && capturedMask != null)
        {
            UpdateFinalResult();
        }
    }
    
    private void InitializeComponents()
    {
        if (deeplabDecoder == null)
        {
            deeplabDecoder = FindObjectOfType<DeepLabDecoder>();
            if (deeplabDecoder == null)
            {
                Debug.LogError("DeepLabDecoder не найден! Тестирование не возможно.");
                enabled = false;
            }
        }
    }
    
    private void InitializeUI()
    {
        if (captureButton != null)
        {
            captureButton.onClick.AddListener(CaptureFrame);
        }
        
        if (saveButton != null)
        {
            saveButton.onClick.AddListener(SaveTestResults);
            saveButton.interactable = false; // Активируем после первого захвата
        }
        
        if (showEdgesToggle != null)
        {
            showEdgesToggle.isOn = showEdges;
            showEdgesToggle.onValueChanged.AddListener((value) => {
                showEdges = value;
                UpdateFinalResult();
            });
        }
        
        if (showOverlayToggle != null)
        {
            showOverlayToggle.isOn = showOverlay;
            showOverlayToggle.onValueChanged.AddListener((value) => {
                showOverlay = value;
                UpdateFinalResult();
            });
        }
    }
    
    /// <summary>
    /// Захват текущего кадра для анализа
    /// </summary>
    public void CaptureFrame()
    {
        if (deeplabDecoder == null) return;
        
        captureCount++;
        
        // Копируем оригинальное изображение
        if (deeplabDecoder.cameraTexture != null)
        {
            if (capturedOriginal == null || 
                capturedOriginal.width != deeplabDecoder.cameraTexture.width || 
                capturedOriginal.height != deeplabDecoder.cameraTexture.height)
            {
                capturedOriginal = new Texture2D(
                    deeplabDecoder.cameraTexture.width, 
                    deeplabDecoder.cameraTexture.height, 
                    TextureFormat.RGBA32, false);
            }
            
            Graphics.CopyTexture(deeplabDecoder.cameraTexture, capturedOriginal);
            if (originalImage != null)
            {
                originalImage.texture = capturedOriginal;
            }
        }
        
        // Копируем маску
        if (deeplabDecoder.maskTexture != null)
        {
            if (capturedMask == null || 
                capturedMask.width != deeplabDecoder.maskTexture.width || 
                capturedMask.height != deeplabDecoder.maskTexture.height)
            {
                capturedMask = new Texture2D(
                    deeplabDecoder.maskTexture.width, 
                    deeplabDecoder.maskTexture.height, 
                    TextureFormat.RGBA32, false);
            }
            
            Graphics.CopyTexture(deeplabDecoder.maskTexture, capturedMask);
            if (processedMask != null)
            {
                processedMask.texture = capturedMask;
            }
        }
        
        // Активируем кнопку сохранения
        if (saveButton != null)
        {
            saveButton.interactable = true;
        }
        
        // Обновляем итоговое изображение
        UpdateFinalResult();
        
        // Регистрируем статистические данные
        totalFrames++;
        if (deeplabDecoder.lastProcessingTime > 0)
        {
            if (!performanceStats.ContainsKey("processingTime"))
            {
                performanceStats["processingTime"] = new List<float>();
            }
            performanceStats["processingTime"].Add(deeplabDecoder.lastProcessingTime);
            
            // Считаем успешным распознавание, если на маске есть пиксели стены
            bool hasWallPixels = HasWallPixels(capturedMask);
            if (hasWallPixels)
            {
                successfulDetections++;
            }
        }
        
        Debug.Log($"Захвачен кадр #{captureCount}");
    }
    
    /// <summary>
    /// Проверка наличия пикселей стены на маске
    /// </summary>
    private bool HasWallPixels(Texture2D maskTexture)
    {
        if (maskTexture == null) return false;
        
        // Проверяем, есть ли на маске пиксели со значением > 0
        Color[] pixels = maskTexture.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].r > 0.1f || pixels[i].g > 0.1f || pixels[i].b > 0.1f)
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Обновление итогового результата с наложением или отображением краев
    /// </summary>
    private void UpdateFinalResult()
    {
        if (capturedOriginal == null || capturedMask == null || finalResult == null) return;
        
        // Создаем текстуру для итогового результата, если нужно
        if (capturedResult == null || 
            capturedResult.width != capturedOriginal.width || 
            capturedResult.height != capturedOriginal.height)
        {
            capturedResult = new Texture2D(
                capturedOriginal.width, 
                capturedOriginal.height, 
                TextureFormat.RGBA32, false);
        }
        
        // Копируем оригинальное изображение как основу
        Graphics.CopyTexture(capturedOriginal, capturedResult);
        
        Color[] resultPixels = capturedResult.GetPixels();
        Color[] maskPixels = capturedMask.GetPixels();
        
        // Масштабируем маску до размеров оригинального изображения если нужно
        if (capturedMask.width != capturedOriginal.width || capturedMask.height != capturedOriginal.height)
        {
            // Простая билинейная интерполяция
            maskPixels = ScaleMaskPixels(maskPixels, capturedMask.width, capturedMask.height, 
                                         capturedOriginal.width, capturedOriginal.height);
        }
        
        // Применяем наложение или выделение краев
        for (int i = 0; i < resultPixels.Length; i++)
        {
            if (maskPixels[i].r > 0.1f || maskPixels[i].g > 0.1f || maskPixels[i].b > 0.1f)
            {
                if (showOverlay)
                {
                    // Полупрозрачное наложение
                    resultPixels[i] = Color.Lerp(resultPixels[i], Color.green, 0.5f);
                }
            }
        }
        
        // Если нужно отображать края, находим их на маске
        if (showEdges)
        {
            DetectAndDrawEdges(resultPixels, maskPixels, capturedOriginal.width, capturedOriginal.height);
        }
        
        // Применяем изменения и обновляем текстуру
        capturedResult.SetPixels(resultPixels);
        capturedResult.Apply();
        
        finalResult.texture = capturedResult;
    }
    
    /// <summary>
    /// Масштабирование пикселей маски до размеров оригинального изображения
    /// </summary>
    private Color[] ScaleMaskPixels(Color[] pixels, int srcWidth, int srcHeight, int dstWidth, int dstHeight)
    {
        Color[] result = new Color[dstWidth * dstHeight];
        
        float xRatio = (float)srcWidth / dstWidth;
        float yRatio = (float)srcHeight / dstHeight;
        
        for (int y = 0; y < dstHeight; y++)
        {
            for (int x = 0; x < dstWidth; x++)
            {
                int srcX = Mathf.FloorToInt(x * xRatio);
                int srcY = Mathf.FloorToInt(y * yRatio);
                result[y * dstWidth + x] = pixels[srcY * srcWidth + srcX];
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Обнаружение и отрисовка краев на маске
    /// </summary>
    private void DetectAndDrawEdges(Color[] resultPixels, Color[] maskPixels, int width, int height)
    {
        // Простой алгоритм обнаружения краев по разнице с соседними пикселями
        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                int idx = y * width + x;
                float center = maskPixels[idx].r;
                
                // Если текущий пиксель имеет значительную разницу с соседями, считаем его краем
                float left = maskPixels[idx - 1].r;
                float right = maskPixels[idx + 1].r;
                float up = maskPixels[idx - width].r;
                float down = maskPixels[idx + width].r;
                
                if (Mathf.Abs(center - left) > 0.3f || Mathf.Abs(center - right) > 0.3f ||
                    Mathf.Abs(center - up) > 0.3f || Mathf.Abs(center - down) > 0.3f)
                {
                    // Рисуем красный пиксель для отображения края
                    resultPixels[idx] = Color.red;
                }
            }
        }
    }
    
    /// <summary>
    /// Сохранение результатов тестирования
    /// </summary>
    public void SaveTestResults()
    {
        if (!saveResults || capturedOriginal == null || capturedMask == null || capturedResult == null)
        {
            return;
        }
        
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string testFolder = $"{savePath}/Test_{timestamp}";
        
        // Создаем папку для теста
        if (!Directory.Exists(testFolder))
        {
            Directory.CreateDirectory(testFolder);
        }
        
        // Сохраняем изображения
        byte[] originalBytes = capturedOriginal.EncodeToPNG();
        File.WriteAllBytes($"{testFolder}/original.png", originalBytes);
        
        byte[] maskBytes = capturedMask.EncodeToPNG();
        File.WriteAllBytes($"{testFolder}/mask.png", maskBytes);
        
        byte[] resultBytes = capturedResult.EncodeToPNG();
        File.WriteAllBytes($"{testFolder}/result.png", resultBytes);
        
        // Сохраняем статистику в JSON
        string statsJson = JsonUtility.ToJson(new TestStats
        {
            totalFrames = totalFrames,
            successfulDetections = successfulDetections,
            detectionRate = totalFrames > 0 ? (float)successfulDetections / totalFrames : 0,
            averageProcessingTime = CalculateAverage(performanceStats.ContainsKey("processingTime") ? 
                                    performanceStats["processingTime"] : new List<float>()),
            testDate = System.DateTime.Now.ToString(),
            modelName = deeplabDecoder != null ? (deeplabDecoder.modelAsset != null ? deeplabDecoder.modelAsset.name : "Unknown") : "Unknown",
            inputResolution = deeplabDecoder != null ? $"{deeplabDecoder.inputWidth}x{deeplabDecoder.inputHeight}" : "Unknown",
            deviceInfo = SystemInfo.deviceModel + " | " + SystemInfo.deviceName
        }, true);
        
        File.WriteAllText($"{testFolder}/stats.json", statsJson);
        
        Debug.Log($"Результаты тестирования сохранены в папку: {testFolder}");
    }
    
    /// <summary>
    /// Обновление статистических данных
    /// </summary>
    private void UpdateStatistics()
    {
        if (statsText == null) return;
        
        // Вычисляем среднее время обработки
        averageDetectionTime = CalculateAverage(performanceStats.ContainsKey("processingTime") ? 
                              performanceStats["processingTime"] : new List<float>());
        
        // Отображаем статистику
        statsText.text = $"Статистика тестирования:\n" +
                        $"Всего кадров: {totalFrames}\n" +
                        $"Успешных распознаваний: {successfulDetections} ({(totalFrames > 0 ? (float)successfulDetections / totalFrames * 100 : 0):F1}%)\n" +
                        $"Среднее время: {averageDetectionTime * 1000:F1} мс\n" +
                        $"Текущее FPS: {(deeplabDecoder.lastProcessingTime > 0 ? 1.0f / deeplabDecoder.lastProcessingTime : 0):F1}\n" +
                        $"Модель: {(deeplabDecoder.modelAsset != null ? deeplabDecoder.modelAsset.name : "Unknown")}\n" +
                        $"Размер входа: {deeplabDecoder.inputWidth}x{deeplabDecoder.inputHeight}";
    }
    
    /// <summary>
    /// Вычисление среднего значения из списка
    /// </summary>
    private float CalculateAverage(List<float> values)
    {
        if (values == null || values.Count == 0)
        {
            return 0;
        }
        
        float sum = 0;
        foreach (float value in values)
        {
            sum += value;
        }
        
        return sum / values.Count;
    }
    
    /// <summary>
    /// Класс для хранения статистических данных тестирования
    /// </summary>
    [System.Serializable]
    private class TestStats
    {
        public int totalFrames;
        public int successfulDetections;
        public float detectionRate;
        public float averageProcessingTime;
        public string testDate;
        public string modelName;
        public string inputResolution;
        public string deviceInfo;
    }
} 