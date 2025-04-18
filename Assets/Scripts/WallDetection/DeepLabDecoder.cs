using UnityEngine;
using Unity.Barracuda;
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
using System.Linq;
using System.Collections;
using Remalux.Settings;

/// <summary>
/// Декодер для обработки результатов сегментации DeepLabV3
/// Выделяет стены из результатов сегментации и формирует маску
/// </summary>
public class DeepLabDecoder : MonoBehaviour
{
    [Header("Модель и настройки")]
    [Tooltip("Модель DeepLabV3 в формате ONNX/NNModel")]
    public NNModel modelAsset;
    
    [Tooltip("Индекс класса 'стена' в модели (обычно 12 для DeepLabv3/COCO)")]
    public int wallClassIndex = 12;

    [Tooltip("Порог уверенности для сегментации стен (0-1)")]
    [Range(0, 1)]
    public float confidenceThreshold = 0.5f;

    [Tooltip("Скорость сглаживания переходов между кадрами")]
    [Range(1f, 20f)]
    public float blendSpeed = 5f;

    [Tooltip("Использовать GPU для вычислений Barracuda")]
    public bool useGPU = true;

    [Header("Размер входного изображения")]
    [Tooltip("Предустановленные размеры входного изображения")]
    public InputResolution resolutionPreset = InputResolution.Medium;
    
    [Tooltip("Пользовательский размер входного изображения (если выбран Custom)")]
    public Vector2Int customResolution = new Vector2Int(320, 320);
    
    // Перечисление для выбора разрешения
    public enum InputResolution
    {
        Low,        // 224x224 - Высокая скорость, низкое качество
        Medium,     // 320x320 - Баланс скорости и качества
        High,       // 512x512 - Высокое качество, низкая скорость
        UltraHigh,  // 768x768 - Очень высокое качество, очень низкая скорость
        Custom      // Пользовательский размер
    }

    [Header("Источник изображения")]
    [Tooltip("Компонент ARCameraManager для получения кадров")]
    public ARCameraManager cameraManager;
    
    [Tooltip("Альтернативная текстура камеры (если не используется AR)")]
    public RenderTexture alternativeCameraTexture;

    [Header("Отображение и дебаг")]
    [Tooltip("Материал для отображения результата сегментации")]
    public Material outputMaterial;
    
    [Tooltip("Цвет выделения стен")]
    public Color wallHighlightColor = new Color(0, 1, 0, 0.5f);
    
    [Tooltip("Показывать ли результат сегментации")]
    public bool showSegmentation = true;

    [Header("Выходные данные")]
    [Tooltip("Текущая маска стен")]
    [HideInInspector]
    public Texture2D wallMask;

    [Header("Условия освещения")]
    [Tooltip("Автоматическая адаптация к условиям освещения")]
    public bool autoAdjustLighting = true;

    [Tooltip("Коэффициент усиления яркости при низком освещении (1.0 = без изменений)")]
    [Range(1.0f, 3.0f)]
    public float lowLightBoost = 1.5f;

    [Tooltip("Порог уровня яркости для включения усиления (0-1)")]
    [Range(0.0f, 1.0f)]
    public float lowLightThreshold = 0.3f;

    [Tooltip("Коэффициент усиления контрастности (1.0 = без изменений)")]
    [Range(0.5f, 2.0f)]
    public float contrastEnhancement = 1.2f;

    [Header("Производительность")]
    [Tooltip("Интервал между обработками кадров (секунды, 0 = каждый кадр)")]
    [Range(0.0f, 0.5f)]
    public float processingInterval = 0.1f;

    [Tooltip("Показывать метрики производительности")]
    public bool showPerformanceMetrics = false;

    // Приватные поля
    private IWorker engine;
    private Model runtimeModel;
    // Делаем размеры публичными для доступа из ModelSwitcher
    [HideInInspector]
    public int inputWidth = 320;
    [HideInInspector]
    public int inputHeight = 320;
    private bool modelReady = false;
    private Texture2D inputTexture;
    private float[,] wallMaskData;
    
    // Статистика производительности
    private float processingTime;
    private int frameCount = 0;
    private float fps = 0;
    private float averageProcessingTime = 0;
    [HideInInspector]
    public float lastProcessingTime;
    private bool isProcessingFrame = false;
    
    // Менеджер настроек
    private SettingsManager settingsManager;

    void Start()
    {
        // Получаем ссылку на менеджер настроек
        settingsManager = SettingsManager.Instance;
        if (settingsManager != null)
        {
            // Подписываемся на событие изменения настроек
            settingsManager.OnSettingsChanged += OnSettingsChanged;
        }
        
        // Устанавливаем размер входного изображения в зависимости от выбранного пресета
        ApplySettingsFromManager();
        InitializeModel();
    }
    
    /// <summary>
    /// Применяет настройки из менеджера настроек
    /// </summary>
    private void ApplySettingsFromManager()
    {
        if (settingsManager == null)
            return;
            
        // Устанавливаем размер входа на основе настроек
        Vector2Int resolution = settingsManager.GetCurrentResolution();
        inputWidth = resolution.x;
        inputHeight = resolution.y;
        
        // Применяем другие настройки
        processingInterval = settingsManager.ProcessingInterval;
        showPerformanceMetrics = settingsManager.ShowPerformanceMetrics;
        autoAdjustLighting = settingsManager.AutoAdjustLighting;
        lowLightBoost = settingsManager.LowLightBoost;
        lowLightThreshold = settingsManager.LowLightThreshold;
        contrastEnhancement = settingsManager.ContrastEnhancement;
        
        // Пересоздаем текстуры если нужно
        if (inputTexture != null && (inputTexture.width != inputWidth || inputTexture.height != inputHeight))
        {
            Destroy(inputTexture);
            inputTexture = new Texture2D(inputWidth, inputHeight, TextureFormat.RGB24, false);
        }
        
        if (wallMask != null && (wallMask.width != inputWidth || wallMask.height != inputHeight))
        {
            Destroy(wallMask);
            wallMask = new Texture2D(inputWidth, inputHeight, TextureFormat.RGBA32, false);
            wallMaskData = new float[inputHeight, inputWidth];
        }
        
        Debug.Log($"DeepLabV3: Применены настройки из менеджера, разрешение {inputWidth}x{inputHeight}");
    }
    
    /// <summary>
    /// Обработчик события изменения настроек
    /// </summary>
    private void OnSettingsChanged()
    {
        ApplySettingsFromManager();
    }
    
    /// <summary>
    /// Устанавливает размер входного изображения в зависимости от выбранного пресета
    /// </summary>
    private void SetInputResolution(InputResolution preset)
    {
        switch (preset)
        {
            case InputResolution.Low:
                inputWidth = inputHeight = 224;
                break;
            case InputResolution.Medium:
                inputWidth = inputHeight = 320;
                break;
            case InputResolution.High:
                inputWidth = inputHeight = 512;
                break;
            case InputResolution.UltraHigh:
                inputWidth = inputHeight = 768;
                break;
            case InputResolution.Custom:
                // Если доступен менеджер настроек, используем разрешение из него
                if (settingsManager != null)
                {
                    Vector2Int customRes = settingsManager.CustomResolution;
                    inputWidth = customRes.x;
                    inputHeight = customRes.y;
                }
                break;
        }
        
        Debug.Log($"DeepLabV3: Установлено разрешение входа {inputWidth}x{inputHeight}");
        
        // Если текстура уже существует, пересоздаем её
        if(inputTexture != null)
        {
            Destroy(inputTexture);
            inputTexture = new Texture2D(inputWidth, inputHeight, TextureFormat.RGB24, false);
        }
        
        // Если маска уже существует, пересоздаем её
        if(wallMask != null)
        {
            Destroy(wallMask);
            wallMask = new Texture2D(inputWidth, inputHeight, TextureFormat.RGBA32, false);
            wallMaskData = new float[inputHeight, inputWidth];
        }
    }

    // Мониторинг изменения разрешения в инспекторе
    void OnValidate()
    {
        if (Application.isPlaying && modelReady)
        {
            SetInputResolution(resolutionPreset);
        }
    }

    void OnDestroy()
    {
        CleanupModel();
        
        // Отписываемся от события изменения настроек
        if (settingsManager != null)
        {
            settingsManager.OnSettingsChanged -= OnSettingsChanged;
        }
    }

    /// <summary>
    /// Инициализация модели DeepLabV3
    /// </summary>
    private void InitializeModel()
    {
        if (modelAsset == null)
        {
            Debug.LogError("DeepLabV3 модель не задана!");
            return;
        }

        try
        {
            runtimeModel = ModelLoader.Load(modelAsset);
            
            engine = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, runtimeModel);
            
            Debug.Log($"DeepLabV3 модель инициализирована: входной размер {inputWidth}x{inputHeight}");
            
            // Создаем маску для стен
            wallMask = new Texture2D(inputWidth, inputHeight, TextureFormat.RGBA32, false);
            wallMaskData = new float[inputHeight, inputWidth];
            
            // Создаем входную текстуру
            inputTexture = new Texture2D(inputWidth, inputHeight, TextureFormat.RGB24, false);
            
            modelReady = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка инициализации модели: {e.Message}");
            modelReady = false;
        }
    }

    /// <summary>
    /// Очистка ресурсов модели
    /// </summary>
    private void CleanupModel()
    {
        if (engine != null)
        {
            engine.Dispose();
            engine = null;
        }
    }

    /// <summary>
    /// Основной цикл обработки
    /// </summary>
    void Update()
    {
        if (!modelReady)
            return;

        // Ограничиваем частоту обработки кадров для экономии ресурсов
        if (processingInterval > 0 && Time.time - lastProcessingTime < processingInterval)
            return;

        if (isProcessingFrame)
            return;

        // Метрики производительности
        if (showPerformanceMetrics)
        {
            frameCount++;
            if (Time.time - lastProcessingTime > 1.0f)
            {
                fps = frameCount / (Time.time - lastProcessingTime);
                frameCount = 0;
                lastProcessingTime = Time.time;
            }
        }

        // Запускаем асинхронную обработку
        StartCoroutine(ProcessFrameCoroutine());
    }

    /// <summary>
    /// Асинхронная обработка кадра
    /// </summary>
    private System.Collections.IEnumerator ProcessFrameCoroutine()
    {
        isProcessingFrame = true;
        
        float startTime = Time.time;
        
        // Получаем текстуру из камеры
        Texture2D cameraTexture = GetCameraTexture();
        
        if (cameraTexture != null)
        {
            // Адаптация к условиям освещения
            if (autoAdjustLighting)
            {
                AdjustForLightingConditions(cameraTexture);
            }
            
            // Обрабатываем кадр
            ProcessFrame(cameraTexture);
            
            // Обновляем материал для отображения результата
            if (outputMaterial != null && showSegmentation)
            {
                outputMaterial.SetTexture("_MainTex", wallMask);
            }
        }
        
        // Вычисляем время обработки
        lastProcessingTime = Time.time;
        processingTime = Time.time - startTime;
        
        // Обновляем среднее время обработки
        if (averageProcessingTime == 0)
            averageProcessingTime = processingTime;
        else
            averageProcessingTime = Mathf.Lerp(averageProcessingTime, processingTime, 0.1f);
        
        isProcessingFrame = false;
        
        yield return null;
    }

    /// <summary>
    /// Адаптация к условиям освещения
    /// </summary>
    private void AdjustForLightingConditions(Texture2D texture)
    {
        // Анализируем яркость изображения
        Color[] pixels = texture.GetPixels();
        float averageBrightness = 0;
        
        for (int i = 0; i < pixels.Length; i++)
        {
            // Вычисляем яркость пикселя (0-1)
            float brightness = (pixels[i].r + pixels[i].g + pixels[i].b) / 3.0f;
            averageBrightness += brightness;
        }
        
        averageBrightness /= pixels.Length;
        
        // Если освещение низкое, применяем коррекцию
        if (averageBrightness < lowLightThreshold)
        {
            float boost = 1.0f + (lowLightThreshold - averageBrightness) / lowLightThreshold * (lowLightBoost - 1.0f);
            
            for (int i = 0; i < pixels.Length; i++)
            {
                // Усиливаем яркость пикселей
                pixels[i].r = Mathf.Clamp01(pixels[i].r * boost);
                pixels[i].g = Mathf.Clamp01(pixels[i].g * boost);
                pixels[i].b = Mathf.Clamp01(pixels[i].b * boost);
            }
            
            // Улучшаем контрастность
            if (contrastEnhancement != 1.0f)
            {
                for (int i = 0; i < pixels.Length; i++)
                {
                    // Применяем контрастность (значения относительно 0.5)
                    pixels[i].r = 0.5f + (pixels[i].r - 0.5f) * contrastEnhancement;
                    pixels[i].g = 0.5f + (pixels[i].g - 0.5f) * contrastEnhancement;
                    pixels[i].b = 0.5f + (pixels[i].b - 0.5f) * contrastEnhancement;
                    
                    // Ограничиваем значения в диапазоне 0-1
                    pixels[i].r = Mathf.Clamp01(pixels[i].r);
                    pixels[i].g = Mathf.Clamp01(pixels[i].g);
                    pixels[i].b = Mathf.Clamp01(pixels[i].b);
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
        }
    }

    /// <summary>
    /// Получение текстуры из AR камеры
    /// </summary>
    private Texture2D GetCameraTexture()
    {
        if (cameraManager != null)
        {
            // Получаем текстуру из AR камеры
            Texture2D arCameraTexture = null;
            
            try
            {
                XRCpuImage image;
                if (cameraManager.TryAcquireLatestCpuImage(out image))
                {
                    using (image)
                    {
                        var conversionParams = new XRCpuImage.ConversionParams
                        {
                            inputRect = new RectInt(0, 0, image.width, image.height),
                            outputDimensions = new Vector2Int(inputWidth, inputHeight),
                            outputFormat = TextureFormat.RGB24,
                            transformation = XRCpuImage.Transformation.MirrorY
                        };

                        // Получаем размер буфера для преобразованного изображения
                        int size = image.GetConvertedDataSize(conversionParams);
                        var buffer = new byte[size];
                        
                        // Преобразуем изображение
                        image.Convert(conversionParams, buffer, buffer.Length);
                        
                        // Загружаем в текстуру
                        if (inputTexture == null || inputTexture.width != inputWidth || inputTexture.height != inputHeight)
                        {
                            inputTexture = new Texture2D(inputWidth, inputHeight, TextureFormat.RGB24, false);
                        }
                        
                        inputTexture.LoadRawTextureData(buffer);
                        inputTexture.Apply();
                        
                        return inputTexture;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Ошибка получения изображения с AR камеры: {e.Message}");
            }
        }
        
        // Если AR камера недоступна, используем альтернативный источник
        if (alternativeCameraTexture != null)
        {
            RenderTexture.active = alternativeCameraTexture;
            
            if (inputTexture == null || inputTexture.width != alternativeCameraTexture.width || inputTexture.height != alternativeCameraTexture.height)
            {
                inputTexture = new Texture2D(alternativeCameraTexture.width, alternativeCameraTexture.height, TextureFormat.RGB24, false);
            }
            
            inputTexture.ReadPixels(new Rect(0, 0, alternativeCameraTexture.width, alternativeCameraTexture.height), 0, 0);
            inputTexture.Apply();
            
            RenderTexture.active = null;
            
            return inputTexture;
        }
        
        return null;
    }

    /// <summary>
    /// Обработка кадра через модель DeepLabV3
    /// </summary>
    private void ProcessFrame(Texture2D sourceTexture)
    {
        if (engine == null || sourceTexture == null)
            return;

        try
        {
            // Масштабируем и подготавливаем входной тензор
            Texture2D resizedTexture = ScaleTexture(sourceTexture, inputWidth, inputHeight);
            Tensor inputTensor = new Tensor(resizedTexture, channels: 3);

            // Запускаем модель
            engine.Execute(inputTensor);
            Tensor outputTensor = engine.PeekOutput();

            // Получаем и обрабатываем результаты сегментации
            GenerateWallMask(outputTensor);

            // Освобождаем ресурсы
            inputTensor.Dispose();
            outputTensor.Dispose();
            
            if (resizedTexture != sourceTexture)
            {
                Destroy(resizedTexture);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка обработки кадра: {e.Message}");
        }
    }

    /// <summary>
    /// Масштабирование текстуры под нужный размер
    /// </summary>
    private Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        // Если размеры совпадают, используем исходную текстуру
        if (source.width == targetWidth && source.height == targetHeight)
            return source;

        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
        Graphics.Blit(source, rt);
        
        RenderTexture previousRT = RenderTexture.active;
        RenderTexture.active = rt;
        
        Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);
        result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        result.Apply();
        
        RenderTexture.active = previousRT;
        RenderTexture.ReleaseTemporary(rt);
        
        return result;
    }

    /// <summary>
    /// Генерация маски стен из результатов сегментации
    /// </summary>
    private void GenerateWallMask(Tensor outputTensor)
    {
        int outputWidth = outputTensor.shape.width;
        int outputHeight = outputTensor.shape.height;
        int numClasses = outputTensor.shape.channels;

        if (wallMask == null || wallMask.width != outputWidth || wallMask.height != outputHeight)
        {
            wallMask = new Texture2D(outputWidth, outputHeight, TextureFormat.RGBA32, false);
            wallMaskData = new float[outputHeight, outputWidth];
        }

        Color wallColor = wallHighlightColor;
        Color transparentColor = new Color(0, 0, 0, 0);

        for (int y = 0; y < outputHeight; y++)
        {
            for (int x = 0; x < outputWidth; x++)
            {
                int maxClassIndex = -1;
                float maxScore = float.MinValue;

                // Находим класс с наибольшей вероятностью
                for (int c = 0; c < numClasses; c++)
                {
                    float score = outputTensor[0, y, x, c];
                    if (score > maxScore)
                    {
                        maxScore = score;
                        maxClassIndex = c;
                    }
                }

                // Проверяем, является ли это стеной и превышает ли порог уверенности
                bool isWall = (maxClassIndex == wallClassIndex && maxScore >= confidenceThreshold);
                
                // Сохраняем вероятность в массив данных маски
                wallMaskData[y, x] = isWall ? maxScore : 0;
                
                // Устанавливаем цвет пикселя в текстуре маски
                wallMask.SetPixel(x, outputHeight - y - 1, isWall ? wallColor : transparentColor);
            }
        }

        // Проводим простую постобработку - удаляем шум и заполняем дырки
        PostProcessWallMask();

        wallMask.Apply();
    }

    /// <summary>
    /// Простая постобработка маски для удаления шума и заполнения дырок
    /// </summary>
    private void PostProcessWallMask()
    {
        // Улучшенная морфологическая обработка для устранения артефактов и заполнения дырок
        int width = wallMaskData.GetLength(1);
        int height = wallMaskData.GetLength(0);
        
        // Создаем временные буферы для обработки
        float[,] tempMask1 = new float[height, width];
        float[,] tempMask2 = new float[height, width];
        
        // Копируем исходную маску
        System.Array.Copy(wallMaskData, tempMask1, wallMaskData.Length);
        
        // Настройки морфологических операций
        int smallKernelSize = 3; // Для удаления мелкого шума
        int mediumKernelSize = 5; // Для операций с соседними областями
        int largeKernelSize = 7; // Для заполнения крупных дыр
        
        // 1. Удаляем мелкий шум с помощью эрозии и дилатации (открытие)
        ErodeOperation(tempMask1, tempMask2, smallKernelSize);
        DilateOperation(tempMask2, tempMask1, smallKernelSize);
        
        // 2. Заполняем дыры с помощью дилатации и эрозии (закрытие)
        DilateOperation(tempMask1, tempMask2, mediumKernelSize);
        ErodeOperation(tempMask2, tempMask1, mediumKernelSize);
        
        // 3. Повторно удаляем мелкие изолированные области
        RemoveSmallRegions(tempMask1, tempMask2, 20); // Удаляем регионы меньше заданного размера
        
        // 4. Сглаживаем края с помощью билатерального фильтра
        SmoothEdges(tempMask2, tempMask1);
        
        // 5. Усиливаем важные признаки (углы комнат, длинные стены)
        EnhanceWallFeatures(tempMask1, tempMask2);
        
        // Копируем обработанный результат обратно в основную маску
        System.Array.Copy(tempMask2, wallMaskData, wallMaskData.Length);
        
        // Обновляем текстуру маски
        UpdateMaskTexture();
    }

    /// <summary>
    /// Операция эрозии (сжатия) для маски
    /// </summary>
    private void ErodeOperation(float[,] input, float[,] output, int kernelSize)
    {
        int height = input.GetLength(0);
        int width = input.GetLength(1);
        int radius = kernelSize / 2;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool keepValue = input[y, x] > 0;
                
                if (keepValue)
                {
                    // Сокращенный алгоритм - проверяем до первого не-стенового пикселя
                    for (int ky = -radius; ky <= radius && keepValue; ky++)
                    {
                        int ny = y + ky;
                        if (ny < 0 || ny >= height) continue;
                        
                        for (int kx = -radius; kx <= radius && keepValue; kx++)
                        {
                            int nx = x + kx;
                            if (nx < 0 || nx >= width) continue;
                            
                            // Если хоть один пиксель в окрестности не стена - удаляем
                            if (input[ny, nx] <= 0)
                            {
                                keepValue = false;
                            }
                        }
                    }
                }
                
                output[y, x] = keepValue ? 1.0f : 0.0f;
            }
        }
    }

    /// <summary>
    /// Операция дилатации (расширения) для маски
    /// </summary>
    private void DilateOperation(float[,] input, float[,] output, int kernelSize)
    {
        int height = input.GetLength(0);
        int width = input.GetLength(1);
        int radius = kernelSize / 2;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool setWall = false;
                
                // Оптимизированный поиск - выходим при первом нахождении стены
                for (int ky = -radius; ky <= radius && !setWall; ky++)
                {
                    int ny = y + ky;
                    if (ny < 0 || ny >= height) continue;
                    
                    for (int kx = -radius; kx <= radius && !setWall; kx++)
                    {
                        int nx = x + kx;
                        if (nx < 0 || nx >= width) continue;
                        
                        // Если найдена хоть одна стена в окрестности - расширяем
                        if (input[ny, nx] > 0)
                        {
                            setWall = true;
                        }
                    }
                }
                
                output[y, x] = setWall ? 1.0f : 0.0f;
            }
        }
    }

    /// <summary>
    /// Удаление небольших изолированных регионов маски
    /// </summary>
    private void RemoveSmallRegions(float[,] input, float[,] output, int minRegionSize)
    {
        int height = input.GetLength(0);
        int width = input.GetLength(1);
        
        // Копируем входные данные в выходной буфер
        System.Array.Copy(input, output, input.Length);
        
        // Используем алгоритм связных компонент для определения регионов
        int[,] labelMap = new int[height, width];
        int nextLabel = 1;
        Dictionary<int, int> regionSizes = new Dictionary<int, int>();
        
        // Первый проход - присваиваем метки
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (input[y, x] > 0)
                {
                    // Проверяем соседние пиксели (верхний и левый)
                    List<int> neighborLabels = new List<int>();
                    
                    if (y > 0 && labelMap[y - 1, x] > 0)
                        neighborLabels.Add(labelMap[y - 1, x]);
                    
                    if (x > 0 && labelMap[y, x - 1] > 0)
                        neighborLabels.Add(labelMap[y, x - 1]);
                    
                    if (neighborLabels.Count == 0)
                    {
                        // Новая метка
                        labelMap[y, x] = nextLabel;
                        regionSizes[nextLabel] = 1;
                        nextLabel++;
                    }
                    else
                    {
                        // Используем наименьшую из соседних меток
                        int minLabel = neighborLabels.Min();
                        labelMap[y, x] = minLabel;
                        regionSizes[minLabel] = regionSizes.ContainsKey(minLabel) ? regionSizes[minLabel] + 1 : 1;
                    }
                }
            }
        }
        
        // Удаляем регионы меньше заданного размера
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int label = labelMap[y, x];
                if (label > 0 && regionSizes[label] < minRegionSize)
                {
                    output[y, x] = 0; // Удаляем регион
                }
            }
        }
    }

    /// <summary>
    /// Сглаживание краев маски для более естественного вида
    /// </summary>
    private void SmoothEdges(float[,] input, float[,] output)
    {
        int height = input.GetLength(0);
        int width = input.GetLength(1);
        
        // Простое размытие по Гауссу (аппроксимация)
        float[] kernel = { 0.1f, 0.2f, 0.4f, 0.2f, 0.1f }; // Простое ядро Гаусса
        int kernelRadius = kernel.Length / 2;
        
        // Сначала применяем по горизонтали
        float[,] tempBuffer = new float[height, width];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (input[y, x] > 0)
                {
                    float sum = 0;
                    float totalWeight = 0;
                    
                    for (int k = -kernelRadius; k <= kernelRadius; k++)
                    {
                        int nx = x + k;
                        if (nx < 0 || nx >= width) continue;
                        
                        float weight = kernel[k + kernelRadius];
                        sum += input[y, nx] * weight;
                        totalWeight += weight;
                    }
                    
                    tempBuffer[y, x] = sum / totalWeight;
                }
            }
        }
        
        // Затем применяем по вертикали
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (tempBuffer[y, x] > 0)
                {
                    float sum = 0;
                    float totalWeight = 0;
                    
                    for (int k = -kernelRadius; k <= kernelRadius; k++)
                    {
                        int ny = y + k;
                        if (ny < 0 || ny >= height) continue;
                        
                        float weight = kernel[k + kernelRadius];
                        sum += tempBuffer[ny, x] * weight;
                        totalWeight += weight;
                    }
                    
                    // Пороговое значение для сохранения четких границ
                    output[y, x] = (sum / totalWeight) >= 0.4f ? 1.0f : 0.0f;
                }
                else
                {
                    output[y, x] = 0;
                }
            }
        }
    }

    /// <summary>
    /// Усиление характерных признаков стен (углы, длинные стены)
    /// </summary>
    private void EnhanceWallFeatures(float[,] input, float[,] output)
    {
        int height = input.GetLength(0);
        int width = input.GetLength(1);
        
        // Копируем входные данные
        System.Array.Copy(input, output, input.Length);
        
        // Ищем горизонтальные и вертикальные линии (стены)
        int lineLength = 9; // Длина линии для проверки
        int minPixelsInLine = 7; // Минимальное количество пикселей для признания линией
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Проверяем только граничные пиксели (края стен)
                bool isEdge = false;
                if (input[y, x] > 0)
                {
                    // Проверяем соседей по 4-м направлениям
                    if ((y > 0 && input[y - 1, x] == 0) || 
                        (y < height - 1 && input[y + 1, x] == 0) ||
                        (x > 0 && input[y, x - 1] == 0) || 
                        (x < width - 1 && input[y, x + 1] == 0))
                    {
                        isEdge = true;
                    }
                }
                
                if (isEdge)
                {
                    // Проверяем горизонтальную линию
                    if (x + lineLength <= width)
                    {
                        int pixelsInLine = 0;
                        for (int i = 0; i < lineLength; i++)
                        {
                            if (input[y, x + i] > 0) pixelsInLine++;
                        }
                        
                        if (pixelsInLine >= minPixelsInLine)
                        {
                            // Заполняем горизонтальную линию
                            for (int i = 0; i < lineLength; i++)
                            {
                                output[y, x + i] = 1.0f;
                            }
                        }
                    }
                    
                    // Проверяем вертикальную линию
                    if (y + lineLength <= height)
                    {
                        int pixelsInLine = 0;
                        for (int i = 0; i < lineLength; i++)
                        {
                            if (input[y + i, x] > 0) pixelsInLine++;
                        }
                        
                        if (pixelsInLine >= minPixelsInLine)
                        {
                            // Заполняем вертикальную линию
                            for (int i = 0; i < lineLength; i++)
                            {
                                output[y + i, x] = 1.0f;
                            }
                        }
                    }
                    
                    // Проверяем углы (Г-образные структуры)
                    CheckAndEnhanceCorner(input, output, x, y, height, width);
                }
            }
        }
    }

    /// <summary>
    /// Проверка и улучшение угловых структур в маске
    /// </summary>
    private void CheckAndEnhanceCorner(float[,] input, float[,] output, int x, int y, int height, int width)
    {
        int cornerSize = 5; // Размер проверяемого угла
        
        // Проверяем 4 возможных угла (верхний-левый, верхний-правый, нижний-левый, нижний-правый)
        if (x + cornerSize <= width && y + cornerSize <= height)
        {
            // Нижний-правый угол
            int horizontalCount = 0;
            int verticalCount = 0;
            
            for (int i = 0; i < cornerSize; i++)
            {
                if (input[y, x + i] > 0) horizontalCount++;
                if (input[y + i, x] > 0) verticalCount++;
            }
            
            if (horizontalCount >= 3 && verticalCount >= 3)
            {
                // Заполняем угол
                for (int i = 0; i < cornerSize; i++)
                {
                    output[y, x + i] = 1.0f;
                    output[y + i, x] = 1.0f;
                }
            }
        }
        
        if (x - cornerSize >= 0 && y + cornerSize <= height)
        {
            // Нижний-левый угол
            int horizontalCount = 0;
            int verticalCount = 0;
            
            for (int i = 0; i < cornerSize; i++)
            {
                if (input[y, x - i] > 0) horizontalCount++;
                if (input[y + i, x] > 0) verticalCount++;
            }
            
            if (horizontalCount >= 3 && verticalCount >= 3)
            {
                // Заполняем угол
                for (int i = 0; i < cornerSize; i++)
                {
                    output[y, x - i] = 1.0f;
                    output[y + i, x] = 1.0f;
                }
            }
        }
        
        if (x + cornerSize <= width && y - cornerSize >= 0)
        {
            // Верхний-правый угол
            int horizontalCount = 0;
            int verticalCount = 0;
            
            for (int i = 0; i < cornerSize; i++)
            {
                if (input[y, x + i] > 0) horizontalCount++;
                if (input[y - i, x] > 0) verticalCount++;
            }
            
            if (horizontalCount >= 3 && verticalCount >= 3)
            {
                // Заполняем угол
                for (int i = 0; i < cornerSize; i++)
                {
                    output[y, x + i] = 1.0f;
                    output[y - i, x] = 1.0f;
                }
            }
        }
        
        if (x - cornerSize >= 0 && y - cornerSize >= 0)
        {
            // Верхний-левый угол
            int horizontalCount = 0;
            int verticalCount = 0;
            
            for (int i = 0; i < cornerSize; i++)
            {
                if (input[y, x - i] > 0) horizontalCount++;
                if (input[y - i, x] > 0) verticalCount++;
            }
            
            if (horizontalCount >= 3 && verticalCount >= 3)
            {
                // Заполняем угол
                for (int i = 0; i < cornerSize; i++)
                {
                    output[y, x - i] = 1.0f;
                    output[y - i, x] = 1.0f;
                }
            }
        }
    }

    /// <summary>
    /// Обновление текстуры маски из массива данных
    /// </summary>
    private void UpdateMaskTexture()
    {
        int height = wallMaskData.GetLength(0);
        int width = wallMaskData.GetLength(1);
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isWall = wallMaskData[y, x] > 0;
                
                // Инвертируем координату y, так как текстура в Unity начинается снизу
                wallMask.SetPixel(x, wallMask.height - y - 1, isWall ? wallHighlightColor : new Color(0, 0, 0, 0));
            }
        }
    }

    /// <summary>
    /// Возвращает текущую маску стен
    /// </summary>
    public Texture2D GetCurrentMask()
    {
        return wallMask;
    }

    /// <summary>
    /// Проверяет точку на принадлежность к стене
    /// </summary>
    public bool IsWallPoint(Vector2 normalizedPoint)
    {
        if (wallMaskData == null)
            return false;

        int x = Mathf.RoundToInt(normalizedPoint.x * (wallMaskData.GetLength(1) - 1));
        int y = Mathf.RoundToInt(normalizedPoint.y * (wallMaskData.GetLength(0) - 1));
        
        // Проверяем границы массива
        if (x >= 0 && x < wallMaskData.GetLength(1) && y >= 0 && y < wallMaskData.GetLength(0))
        {
            return wallMaskData[y, x] > 0;
        }
        
        return false;
    }

    /// <summary>
    /// Загружает новую модель DeepLabV3 во время работы приложения
    /// </summary>
    /// <param name="newModel">Новая модель для загрузки</param>
    /// <returns>true, если загрузка прошла успешно</returns>
    public bool LoadNewModel(NNModel newModel)
    {
        if (newModel == null)
        {
            Debug.LogError("Попытка загрузить пустую модель!");
            return false;
        }
        
        // Проверка совместимости модели перед загрузкой
        string errorMessage;
        bool isCompatible = BarracudaModelChecker.CheckModelCompatibilityStatic(newModel, out errorMessage);
        
        if (!isCompatible)
        {
            Debug.LogError($"Модель {newModel.name} несовместима с Barracuda: {errorMessage}");
            return false;
        }

        // Сохраняем ссылку на новую модель
        modelAsset = newModel;
        
        // Очищаем старую модель
        CleanupModel();
        
        // Инициализируем новую модель
        try
        {
            runtimeModel = ModelLoader.Load(modelAsset);
            
            WorkerFactory.Type workerType = useGPU ? 
                WorkerFactory.Type.ComputePrecompiled : 
                WorkerFactory.Type.CSharpBurst;
            
            engine = WorkerFactory.CreateWorker(workerType, runtimeModel);
            
            Debug.Log($"DeepLabV3 модель успешно переключена: входной размер {inputWidth}x{inputHeight}");
            
            // Пересоздаем буферы если нужно
            if (wallMask == null || wallMask.width != inputWidth || wallMask.height != inputHeight)
            {
                if (wallMask != null)
                    Destroy(wallMask);
                    
                wallMask = new Texture2D(inputWidth, inputHeight, TextureFormat.RGBA32, false);
                wallMaskData = new float[inputHeight, inputWidth];
            }
            
            // Пересоздаем входную текстуру если нужно
            if (inputTexture == null || inputTexture.width != inputWidth || inputTexture.height != inputHeight)
            {
                if (inputTexture != null)
                    Destroy(inputTexture);
                    
                inputTexture = new Texture2D(inputWidth, inputHeight, TextureFormat.RGB24, false);
            }
            
            modelReady = true;
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка при переключении модели: {e.Message}");
            modelReady = false;
            return false;
        }
    }
    
    /// <summary>
    /// Устанавливает пользовательское разрешение для входного изображения
    /// </summary>
    /// <param name="width">Ширина изображения</param>
    /// <param name="height">Высота изображения</param>
    public void SetCustomResolution(int width, int height)
    {
        customResolution = new Vector2Int(width, height);
        resolutionPreset = InputResolution.Custom;
        SetInputResolution(resolutionPreset);
    }
} 