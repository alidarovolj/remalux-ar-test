using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;
using UnityEngine.UI;
using System.IO;
using Remalux.Settings;

/// <summary>
/// Класс для тестирования и сравнения различных моделей DeepLabV3
/// </summary>
public class DeepLabModelTester : MonoBehaviour
{
    [System.Serializable]
    public class ModelConfig
    {
        [Tooltip("Название модели")]
        public string name;
        
        [Tooltip("Модель в формате NNModel")]
        public NNModel model;
        
        [Tooltip("Размер входного изображения")]
        public Vector2Int inputSize = new Vector2Int(320, 320);
    }
    
    [Header("Тестируемые модели")]
    [Tooltip("Список моделей для тестирования")]
    public List<ModelConfig> models = new List<ModelConfig>();
    
    [Header("Источник изображения")]
    [Tooltip("Текстура для тестирования (если не задана, будет использована камера)")]
    public Texture2D testImage;
    
    [Tooltip("Использовать камеру устройства")]
    public bool useCamera = true;
    
    [Tooltip("Индекс камеры устройства")]
    public int cameraIndex = 0;
    
    [Header("Настройки тестирования")]
    [Tooltip("Количество прогонов для усреднения результатов")]
    public int testIterations = 5;
    
    [Tooltip("Использовать GPU для инференса")]
    public bool useGPU = true;
    
    [Header("Настройки условий освещения")]
    [Tooltip("Использовать настройки освещения из SettingsManager")]
    public bool useSettingsManagerLighting = true;

    [Tooltip("Применять настройки освещения к тестовым изображениям")]
    public bool applyLightingAdjustments = true;
    
    [Header("Вывод результатов")]
    [Tooltip("Текстовое поле для вывода результатов")]
    public Text resultsText;
    
    [Tooltip("Изображение для отображения результатов")]
    public RawImage outputImage;
    
    [Tooltip("Выводить результаты в лог")]
    public bool logResults = true;

    [Tooltip("Сохранять результаты в файл")]
    public bool saveResultsToFile = false;

    [Tooltip("Путь для сохранения результатов (относительно Application.persistentDataPath)")]
    public string resultsPath = "ModelTestResults";
    
    // Приватные поля
    private WebCamTexture webCamTexture;
    private Texture2D inputTexture;
    private RenderTexture tempRT;
    private bool isTesting = false;
    private SettingsManager settingsManager;
    
    void Start()
    {
        // Получаем ссылку на менеджер настроек
        settingsManager = SettingsManager.Instance;
        
        // Инициализация камеры если нужно
        if (useCamera)
        {
            InitializeCamera();
        }
        
        // Если есть кнопка запуска теста, подключаем к ней обработчик
        Button testButton = GetComponentInChildren<Button>();
        if (testButton != null)
        {
            testButton.onClick.AddListener(RunTests);
        }

        // Создаем директорию для результатов если нужно
        if (saveResultsToFile)
        {
            string fullPath = Path.Combine(Application.persistentDataPath, resultsPath);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
        }
    }
    
    private void InitializeCamera()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length > 0)
        {
            int index = Mathf.Clamp(cameraIndex, 0, devices.Length - 1);
            webCamTexture = new WebCamTexture(devices[index].name, 1280, 720, 30);
            webCamTexture.Play();
            
            Debug.Log($"Инициализирована камера: {devices[index].name}");
        }
        else
        {
            Debug.LogWarning("Не найдено доступных камер.");
            useCamera = false;
        }
    }
    
    /// <summary>
    /// Запускает тестирование всех моделей
    /// </summary>
    public void RunTests()
    {
        if (isTesting)
        {
            Debug.Log("Тестирование уже выполняется...");
            return;
        }
        
        if (models.Count == 0)
        {
            Debug.LogError("Не заданы модели для тестирования.");
            return;
        }
        
        StartCoroutine(TestAllModels());
    }
    
    /// <summary>
    /// Тестирование всех моделей и сбор результатов
    /// </summary>
    private IEnumerator TestAllModels()
    {
        isTesting = true;
        string results = "============= РЕЗУЛЬТАТЫ ТЕСТИРОВАНИЯ МОДЕЛЕЙ =============\n";
        results += "Модель | Размер входа | Время (мс) | FPS\n";
        results += "------------------------------------------------------\n";
        
        // Подготавливаем входное изображение
        Texture2D sourceImage = PrepareInputTexture();
        if (sourceImage == null)
        {
            Debug.LogError("Не удалось получить входное изображение для тестирования.");
            isTesting = false;
            yield break;
        }

        // Применяем настройки освещения если нужно
        if (applyLightingAdjustments && settingsManager != null && useSettingsManagerLighting)
        {
            sourceImage = AdjustForLightingConditions(sourceImage);
        }
        
        // Тестируем каждую модель
        foreach (ModelConfig config in models)
        {
            if (config.model == null)
            {
                Debug.LogWarning($"Модель {config.name} не задана, пропускаем.");
                continue;
            }
            
            // Обновляем текст с результатами
            if (resultsText != null)
            {
                resultsText.text = $"Тестирование модели {config.name}...";
            }
            
            yield return new WaitForSeconds(0.1f); // Даем время для обновления UI
            
            // Тестируем модель
            float avgTime = 0;
            float minTime = float.MaxValue;
            float maxTime = 0;
            
            // Загружаем модель
            IWorker engine = null;
            try
            {
                Model runtimeModel = ModelLoader.Load(config.model);
                WorkerFactory.Type workerType = useGPU ? WorkerFactory.Type.ComputePrecompiled : WorkerFactory.Type.CSharpBurst;
                engine = WorkerFactory.CreateWorker(workerType, runtimeModel);
                
                // Создаем масштабированное изображение для входа модели
                Texture2D scaledInput = ScaleTexture(sourceImage, config.inputSize.x, config.inputSize.y);
                
                // Прогреваем модель
                Tensor inputTensor = new Tensor(scaledInput, channels: 3);
                engine.Execute(inputTensor);
                inputTensor.Dispose();
                
                yield return null; // Даем время для выполнения
                
                // Выполняем несколько прогонов для получения среднего времени
                for (int i = 0; i < testIterations; i++)
                {
                    inputTensor = new Tensor(scaledInput, channels: 3);
                    
                    float startTime = Time.realtimeSinceStartup;
                    engine.Execute(inputTensor);
                    Tensor outputTensor = engine.PeekOutput();
                    float elapsedMs = (Time.realtimeSinceStartup - startTime) * 1000f;
                    
                    // Если это последняя итерация, показываем результат
                    if (i == testIterations - 1 && outputImage != null)
                    {
                        Texture2D resultTexture = VisualizeSegmentation(outputTensor, config.inputSize.x, config.inputSize.y);
                        outputImage.texture = resultTexture;

                        // Сохраняем результаты в файл если нужно
                        if (saveResultsToFile)
                        {
                            SaveResultTexture(resultTexture, config.name);
                        }
                    }
                    
                    inputTensor.Dispose();
                    outputTensor.Dispose();
                    
                    avgTime += elapsedMs;
                    minTime = Mathf.Min(minTime, elapsedMs);
                    maxTime = Mathf.Max(maxTime, elapsedMs);
                    
                    yield return null; // Даем время между итерациями
                }
                
                // Освобождаем ресурсы
                engine.Dispose();
                Destroy(scaledInput);
                
                // Рассчитываем среднее время
                avgTime /= testIterations;
                float fps = 1000f / avgTime;
                
                // Добавляем результаты
                string modelResults = $"{config.name} | {config.inputSize.x}x{config.inputSize.y} | {avgTime:F2} мс | {fps:F1} FPS\n";
                modelResults += $"  Min: {minTime:F2} мс, Max: {maxTime:F2} мс\n";
                results += modelResults;
                
                // Выводим в лог если нужно
                if (logResults)
                {
                    Debug.Log(modelResults);
                }
                
                // Обновляем текст с результатами
                if (resultsText != null)
                {
                    resultsText.text = results;
                }
                
                yield return new WaitForSeconds(0.5f); // Пауза между тестами моделей
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Ошибка при тестировании модели {config.name}: {e.Message}");
                results += $"{config.name} | ОШИБКА: {e.Message}\n";
                
                if (engine != null)
                {
                    engine.Dispose();
                }
            }
        }
        
        // Выводим итоговые результаты
        results += "======================================================\n";
        results += $"Тестирование завершено. Выполнено {testIterations} итераций для каждой модели.";
        
        if (resultsText != null)
        {
            resultsText.text = results;
        }
        
        if (logResults)
        {
            Debug.Log(results);
        }
        
        // Сохраняем результаты в файл если нужно
        if (saveResultsToFile)
        {
            SaveResultsToFile(results);
        }
        
        isTesting = false;
    }
    
    /// <summary>
    /// Подготовка входного изображения из камеры или тестовой текстуры
    /// </summary>
    private Texture2D PrepareInputTexture()
    {
        // Если есть тестовое изображение, используем его
        if (testImage != null)
        {
            return testImage;
        }
        
        // Иначе пытаемся получить изображение с камеры
        if (useCamera && webCamTexture != null && webCamTexture.isPlaying)
        {
            // Создаем текстуру для хранения изображения с камеры
            if (inputTexture == null || inputTexture.width != webCamTexture.width || inputTexture.height != webCamTexture.height)
            {
                if (inputTexture != null)
                    Destroy(inputTexture);
                    
                inputTexture = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGB24, false);
            }
            
            // Копируем изображение с камеры в текстуру
            if (tempRT == null || tempRT.width != webCamTexture.width || tempRT.height != webCamTexture.height)
            {
                if (tempRT != null)
                    RenderTexture.ReleaseTemporary(tempRT);
                    
                tempRT = RenderTexture.GetTemporary(webCamTexture.width, webCamTexture.height, 0, RenderTextureFormat.ARGB32);
            }
            
            Graphics.Blit(webCamTexture, tempRT);
            RenderTexture.active = tempRT;
            inputTexture.ReadPixels(new Rect(0, 0, webCamTexture.width, webCamTexture.height), 0, 0);
            inputTexture.Apply();
            RenderTexture.active = null;
            
            return inputTexture;
        }
        
        Debug.LogError("Не удалось получить входное изображение. Проверьте параметры источника.");
        return null;
    }
    
    /// <summary>
    /// Масштабирование текстуры
    /// </summary>
    private Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        Texture2D result = new Texture2D(targetWidth, targetHeight, source.format, false);
        
        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(source, rt);
        
        RenderTexture.active = rt;
        result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        result.Apply();
        RenderTexture.active = null;
        
        RenderTexture.ReleaseTemporary(rt);
        
        return result;
    }
    
    /// <summary>
    /// Визуализирует результат сегментации
    /// </summary>
    private Texture2D VisualizeSegmentation(Tensor output, int width, int height)
    {
        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] colors = new Color[width * height];
        
        // Определяем цвета для классов сегментации
        Color wallColor = new Color(0, 0.7f, 1.0f, 0.7f);
        
        // DeepLabV3 обычно имеет 21 класс (PASCAL VOC), где стены имеют индекс 12
        int wallClassIndex = 12;
        
        // Восстанавливаем изображение из выходного тензора
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int maxClassIndex = -1;
                float maxScore = float.MinValue;
                
                // Находим класс с наибольшей вероятностью
                for (int c = 0; c < output.shape.channels; c++)
                {
                    float score = output[0, y, x, c];
                    if (score > maxScore)
                    {
                        maxScore = score;
                        maxClassIndex = c;
                    }
                }
                
                // Определяем цвет пикселя в зависимости от класса
                Color pixelColor = Color.clear;
                
                if (maxClassIndex == wallClassIndex && maxScore > 0.5f)
                {
                    pixelColor = wallColor;
                }
                
                colors[y * width + x] = pixelColor;
            }
        }
        
        result.SetPixels(colors);
        result.Apply();
        
        return result;
    }

    /// <summary>
    /// Применяет настройки освещения к изображению
    /// </summary>
    private Texture2D AdjustForLightingConditions(Texture2D texture)
    {
        if (settingsManager == null || !settingsManager.AutoAdjustLighting)
            return texture;

        Texture2D adjustedTexture = new Texture2D(texture.width, texture.height, texture.format, false);
        Color[] pixels = texture.GetPixels();
        Color[] adjustedPixels = new Color[pixels.Length];

        // Получаем настройки из SettingsManager
        float lowLightBoost = settingsManager.LowLightBoost;
        float lowLightThreshold = settingsManager.LowLightThreshold;
        float contrastEnhancement = settingsManager.ContrastEnhancement;

        // Анализируем среднюю яркость изображения
        float totalBrightness = 0;
        for (int i = 0; i < pixels.Length; i++)
        {
            totalBrightness += (pixels[i].r + pixels[i].g + pixels[i].b) / 3f;
        }
        float averageBrightness = totalBrightness / pixels.Length;

        // Применяем коррекцию яркости и контраста
        float boostFactor = averageBrightness < lowLightThreshold ? lowLightBoost : 1f;

        for (int i = 0; i < pixels.Length; i++)
        {
            // Коррекция яркости
            Color pixel = pixels[i];
            pixel.r = Mathf.Clamp01(pixel.r * boostFactor);
            pixel.g = Mathf.Clamp01(pixel.g * boostFactor);
            pixel.b = Mathf.Clamp01(pixel.b * boostFactor);

            // Коррекция контраста
            if (contrastEnhancement != 1.0f)
            {
                float r = ((pixel.r - 0.5f) * contrastEnhancement) + 0.5f;
                float g = ((pixel.g - 0.5f) * contrastEnhancement) + 0.5f;
                float b = ((pixel.b - 0.5f) * contrastEnhancement) + 0.5f;

                pixel.r = Mathf.Clamp01(r);
                pixel.g = Mathf.Clamp01(g);
                pixel.b = Mathf.Clamp01(b);
            }

            adjustedPixels[i] = pixel;
        }

        adjustedTexture.SetPixels(adjustedPixels);
        adjustedTexture.Apply();

        return adjustedTexture;
    }

    /// <summary>
    /// Сохраняет результаты в текстовый файл
    /// </summary>
    private void SaveResultsToFile(string results)
    {
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string filename = $"ModelTest_{timestamp}.txt";
        string fullPath = Path.Combine(Application.persistentDataPath, resultsPath, filename);
        
        try
        {
            File.WriteAllText(fullPath, results);
            Debug.Log($"Результаты сохранены в файл: {fullPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка при сохранении результатов: {e.Message}");
        }
    }

    /// <summary>
    /// Сохраняет текстуру с результатом сегментации
    /// </summary>
    private void SaveResultTexture(Texture2D texture, string modelName)
    {
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string filename = $"{modelName}_{timestamp}.png";
        string fullPath = Path.Combine(Application.persistentDataPath, resultsPath, filename);
        
        try
        {
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(fullPath, bytes);
            Debug.Log($"Результат сегментации сохранен в файл: {fullPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка при сохранении текстуры: {e.Message}");
        }
    }
    
    void OnDestroy()
    {
        if (webCamTexture != null)
        {
            webCamTexture.Stop();
            Destroy(webCamTexture);
        }
        
        if (inputTexture != null)
        {
            Destroy(inputTexture);
        }
        
        if (tempRT != null)
        {
            RenderTexture.ReleaseTemporary(tempRT);
        }
    }
} 