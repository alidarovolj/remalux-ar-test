using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Remalux.AR.Vision;

/// <summary>
/// Демонстрационный скрипт для системы покраски реальных стен.
/// Добавляет инструкции и демонстрационные элементы для пользователя.
/// </summary>
public class RealWallPaintingDemo : MonoBehaviour
{
    [Header("Ссылки на компоненты")]
    [SerializeField] private RealWallPaintingController controller;
    [SerializeField] private WallDetector wallDetector;
    [SerializeField] private Canvas mainCanvas;
    
    [Header("UI элементы")]
    [SerializeField] private Text instructionsText;
    [SerializeField] private Text statusText;
    [SerializeField] private GameObject welcomePanel;
    [SerializeField] private Button startButton;
    [SerializeField] private GameObject helpButton;
    [SerializeField] private GameObject helpPanel;
    [SerializeField] private Text fpsText;
    
    [Header("Настройки демонстрации")]
    [SerializeField] private float initialDelay = 2f;
    [SerializeField] private float instructionDuration = 5f;
    
    // Состояния демонстрации
    private enum DemoState
    {
        Welcome,
        ScanWalls,
        SelectColor,
        PaintWalls,
        Completed
    }
    
    private DemoState currentState = DemoState.Welcome;
    private int detectedWallsCount = 0;
    private int paintedWallsCount = 0;
    
    private bool isDetectionRunning = false;
    private bool isPaintingEnabled = false;
    
    private void Start()
    {
        // Инициализация компонентов, если они не назначены
        if (controller == null)
            controller = Object.FindAnyObjectByType<RealWallPaintingController>();
            
        if (wallDetector == null)
            wallDetector = Object.FindAnyObjectByType<WallDetector>();
            
        if (mainCanvas == null && controller != null)
            mainCanvas = controller.GetMainCanvas();
            
        // Настройка UI элементов
        SetupUI();
        
        // Отключаем покраску до начала демонстрации
        if (controller != null)
            controller.EnablePaintingMode(false);
            
        // Отключаем детектор стен до начала демонстрации
        if (wallDetector != null)
            wallDetector.enabled = false;
            
        // Подписываемся на события
        if (wallDetector != null)
            wallDetector.OnWallsDetected += HandleWallsDetected;
            
        // Начальное состояние - приветственный экран
        UpdateState(DemoState.Welcome);
        
        // Создаем кнопки управления
        CreateControlButtons();
        
        // Запускаем детектор стен автоматически
        StartWallDetection();
    }
    
    private void SetupUI()
    {
        // Создаем UI элементы, если они не существуют
        if (welcomePanel == null)
        {
            welcomePanel = CreatePanel("WelcomePanel", new Vector2(0, 0), new Vector2(600, 400));
            
            // Добавляем заголовок
            Text titleText = CreateText("TitleText", welcomePanel.transform, "Система покраски реальных стен", 
                                       new Vector2(0, 150), 30);
            titleText.fontStyle = FontStyle.Bold;
            
            // Добавляем описание
            Text descriptionText = CreateText("DescriptionText", welcomePanel.transform, 
                                             "Эта демонстрация покажет, как использовать систему покраски реальных стен с помощью компьютерного зрения.\n\n" +
                                             "1. Направьте камеру на стены\n" +
                                             "2. Выберите цвет из палитры\n" +
                                             "3. Нажмите на стену, чтобы покрасить ее", 
                                             new Vector2(0, 0), 20);
            
            // Добавляем кнопку "Начать"
            startButton = CreateButton("StartButton", welcomePanel.transform, "Начать", 
                                      new Vector2(0, -150), new Vector2(200, 60));
            startButton.onClick.AddListener(StartDemo);
        }
        
        if (instructionsText == null)
        {
            instructionsText = CreateText("InstructionsText", mainCanvas.transform, "", 
                                         new Vector2(0, Screen.height * 0.4f), 24);
            instructionsText.gameObject.SetActive(false);
        }
        
        if (statusText == null)
        {
            statusText = CreateText("StatusText", mainCanvas.transform, "", 
                                   new Vector2(0, Screen.height * 0.45f), 20);
            statusText.gameObject.SetActive(false);
        }
        
        if (helpButton == null)
        {
            helpButton = CreateButton("HelpButton", mainCanvas.transform, "?", 
                                     new Vector2(Screen.width * 0.45f, -Screen.height * 0.45f), 
                                     new Vector2(60, 60)).gameObject;
            helpButton.GetComponent<Button>().onClick.AddListener(ToggleHelpPanel);
            helpButton.SetActive(false);
        }
        
        if (helpPanel == null)
        {
            helpPanel = CreatePanel("HelpPanel", new Vector2(0, 0), new Vector2(500, 400));
            
            Text helpTitle = CreateText("HelpTitle", helpPanel.transform, "Помощь", 
                                       new Vector2(0, 150), 28);
            helpTitle.fontStyle = FontStyle.Bold;
            
            Text helpContent = CreateText("HelpContent", helpPanel.transform, 
                                         "• Направьте камеру на хорошо освещенные стены\n" +
                                         "• Держите камеру стабильно для лучшего обнаружения\n" +
                                         "• Выберите цвет из палитры внизу экрана\n" +
                                         "• Нажмите на стену, чтобы покрасить ее\n" +
                                         "• Используйте кнопку 'Сбросить' для возврата к исходным цветам\n\n" +
                                         "Если стены не обнаруживаются, попробуйте:\n" +
                                         "- Улучшить освещение\n" +
                                         "- Выбрать стену с четкими границами\n" +
                                         "- Держать камеру параллельно стене", 
                                         new Vector2(0, 0), 18);
            
            Button closeButton = CreateButton("CloseHelpButton", helpPanel.transform, "Закрыть", 
                                            new Vector2(0, -150), new Vector2(200, 60));
            closeButton.onClick.AddListener(ToggleHelpPanel);
            
            helpPanel.SetActive(false);
        }
    }
    
    private void StartDemo()
    {
        // Скрываем приветственный экран
        welcomePanel.SetActive(false);
        
        // Показываем кнопку помощи
        helpButton.SetActive(true);
        
        // Включаем детектор стен
        if (wallDetector != null)
            wallDetector.enabled = true;
            
        // Запускаем последовательность демонстрации
        StartCoroutine(DemoSequence());
    }
    
    private IEnumerator DemoSequence()
    {
        // Небольшая задержка перед началом
        yield return new WaitForSeconds(initialDelay);
        
        // Шаг 1: Сканирование стен
        UpdateState(DemoState.ScanWalls);
        ShowInstruction("Направьте камеру на стены в комнате.\nДержите камеру стабильно для лучшего обнаружения.");
        
        // Ждем, пока будут обнаружены стены или истечет время
        float scanTimeout = 30f; // 30 секунд на обнаружение стен
        float scanTimer = 0f;
        
        while (detectedWallsCount == 0 && scanTimer < scanTimeout)
        {
            scanTimer += Time.deltaTime;
            UpdateStatusText($"Поиск стен... {Mathf.RoundToInt(scanTimeout - scanTimer)} сек");
            yield return null;
        }
        
        if (detectedWallsCount == 0)
        {
            // Если стены не обнаружены, показываем подсказку
            ShowInstruction("Стены не обнаружены. Попробуйте улучшить освещение или выбрать стену с четкими границами.");
            yield return new WaitForSeconds(instructionDuration);
        }
        
        // Шаг 2: Выбор цвета
        UpdateState(DemoState.SelectColor);
        ShowInstruction("Отлично! Теперь выберите цвет из палитры внизу экрана.");
        
        // Включаем режим покраски
        if (controller != null)
            controller.EnablePaintingMode(true);
            
        yield return new WaitForSeconds(instructionDuration);
        
        // Шаг 3: Покраска стен
        UpdateState(DemoState.PaintWalls);
        ShowInstruction("Теперь нажмите на стену, чтобы покрасить ее выбранным цветом.");
        
        // Ждем, пока пользователь покрасит хотя бы одну стену
        float paintTimeout = 60f; // 60 секунд на покраску
        float paintTimer = 0f;
        
        while (paintedWallsCount == 0 && paintTimer < paintTimeout)
        {
            paintTimer += Time.deltaTime;
            UpdateStatusText($"Попробуйте покрасить стену... {Mathf.RoundToInt(paintTimeout - paintTimer)} сек");
            yield return null;
        }
        
        // Шаг 4: Завершение демонстрации
        UpdateState(DemoState.Completed);
        ShowInstruction("Поздравляем! Вы успешно использовали систему покраски реальных стен.\n" +
                       "Продолжайте экспериментировать с разными цветами и стенами.");
        
        yield return new WaitForSeconds(instructionDuration);
        
        // Скрываем инструкции, оставляем только интерфейс покраски
        instructionsText.gameObject.SetActive(false);
        statusText.gameObject.SetActive(false);
    }
    
    private void UpdateState(DemoState newState)
    {
        currentState = newState;
        
        switch (currentState)
        {
            case DemoState.Welcome:
                // Начальное состояние - показываем приветственный экран
                welcomePanel.SetActive(true);
                break;
                
            case DemoState.ScanWalls:
                // Сканирование стен - включаем детектор
                if (wallDetector != null)
                    wallDetector.enabled = true;
                break;
                
            case DemoState.SelectColor:
                // Выбор цвета - активируем UI выбора цвета
                if (controller != null)
                    controller.EnablePaintingMode(true);
                break;
                
            case DemoState.PaintWalls:
                // Покраска стен - все уже должно быть активировано
                break;
                
            case DemoState.Completed:
                // Завершение демонстрации
                break;
        }
    }
    
    private void ShowInstruction(string text)
    {
        if (instructionsText != null)
        {
            instructionsText.text = text;
            instructionsText.gameObject.SetActive(true);
        }
    }
    
    private void UpdateStatusText(string text)
    {
        if (statusText != null)
        {
            statusText.text = text;
            statusText.gameObject.SetActive(true);
        }
    }
    
    private void ToggleHelpPanel()
    {
        if (helpPanel != null)
            helpPanel.SetActive(!helpPanel.activeSelf);
    }
    
    /// <summary>
    /// Обрабатывает событие обнаружения стен
    /// </summary>
    private void HandleWallsDetected(List<WallDetector.WallData> walls)
    {
        detectedWallsCount = walls.Count;
        UpdateStatusText($"Обнаружено стен: {detectedWallsCount}");
    }
    
    // Вспомогательные методы для создания UI элементов
    
    private GameObject CreatePanel(string name, Vector2 position, Vector2 size)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(mainCanvas.transform, false);
        
        RectTransform rectTransform = panel.AddComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;
        
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        
        return panel;
    }
    
    private Text CreateText(string name, Transform parent, string content, Vector2 position, int fontSize)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        
        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(500, 100);
        
        Text text = textObject.AddComponent<Text>();
        text.text = content;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        
        // Попытка найти шрифт в ресурсах
        Font arialFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (arialFont != null)
            text.font = arialFont;
        
        return text;
    }
    
    private Button CreateButton(string name, Transform parent, string text, Vector2 position, Vector2 size)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);
        
        RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;
        
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.2f, 0.6f, 1f, 1f);
        
        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.3f, 0.7f, 1f, 1f);
        colors.pressedColor = new Color(0.1f, 0.5f, 0.9f, 1f);
        button.colors = colors;
        
        // Создаем текст для кнопки
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObject.transform, false);
        Text buttonText = textObj.AddComponent<Text>();
        buttonText.text = text;
        buttonText.fontSize = Mathf.RoundToInt(size.y * 0.4f);
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        
        // Попытка найти шрифт в ресурсах
        Font arialFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (arialFont != null)
            buttonText.font = arialFont;
        
        return button;
    }
    
    private void OnDestroy()
    {
        // Отписываемся от событий
        if (wallDetector != null)
            wallDetector.OnWallsDetected -= HandleWallsDetected;
    }
    
    /// <summary>
    /// Устанавливает детектор стен
    /// </summary>
    public void SetWallDetector(WallDetector detector)
    {
        this.wallDetector = detector;
    }
    
    /// <summary>
    /// Устанавливает основной Canvas
    /// </summary>
    public void SetMainCanvas(Canvas canvas)
    {
        this.mainCanvas = canvas;
    }
    
    /// <summary>
    /// Создает кнопки управления для демонстрации
    /// </summary>
    private void CreateControlButtons()
    {
        if (mainCanvas == null)
        {
            Debug.LogError("Main Canvas не назначен для RealWallPaintingDemo");
            return;
        }
        
        // Создаем кнопку для запуска/остановки детекции стен
        GameObject detectionButtonObj = new GameObject("DetectionButton");
        detectionButtonObj.transform.SetParent(mainCanvas.transform, false);
        
        RectTransform detectionRectTransform = detectionButtonObj.AddComponent<RectTransform>();
        detectionRectTransform.anchorMin = new Vector2(0, 1);
        detectionRectTransform.anchorMax = new Vector2(0, 1);
        detectionRectTransform.pivot = new Vector2(0, 1);
        detectionRectTransform.anchoredPosition = new Vector2(20, -20);
        detectionRectTransform.sizeDelta = new Vector2(150, 50);
        
        Image detectionImage = detectionButtonObj.AddComponent<Image>();
        detectionImage.color = new Color(0.2f, 0.6f, 0.2f);
        
        Button detectionButton = detectionButtonObj.AddComponent<Button>();
        detectionButton.onClick.AddListener(ToggleWallDetection);
        
        // Добавляем текст к кнопке
        GameObject detectionTextObj = new GameObject("Text");
        detectionTextObj.transform.SetParent(detectionButtonObj.transform, false);
        
        RectTransform detectionTextRectTransform = detectionTextObj.AddComponent<RectTransform>();
        detectionTextRectTransform.anchorMin = Vector2.zero;
        detectionTextRectTransform.anchorMax = Vector2.one;
        detectionTextRectTransform.offsetMin = Vector2.zero;
        detectionTextRectTransform.offsetMax = Vector2.zero;
        
        Text detectionText = detectionTextObj.AddComponent<Text>();
        detectionText.text = "Стоп детекция";
        detectionText.fontSize = 18;
        detectionText.color = Color.white;
        detectionText.alignment = TextAnchor.MiddleCenter;
        detectionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        
        // Создаем кнопку для включения/выключения режима покраски
        GameObject paintButtonObj = new GameObject("PaintButton");
        paintButtonObj.transform.SetParent(mainCanvas.transform, false);
        
        RectTransform paintRectTransform = paintButtonObj.AddComponent<RectTransform>();
        paintRectTransform.anchorMin = new Vector2(0, 1);
        paintRectTransform.anchorMax = new Vector2(0, 1);
        paintRectTransform.pivot = new Vector2(0, 1);
        paintRectTransform.anchoredPosition = new Vector2(190, -20);
        paintRectTransform.sizeDelta = new Vector2(150, 50);
        
        Image paintImage = paintButtonObj.AddComponent<Image>();
        paintImage.color = new Color(0.2f, 0.2f, 0.8f);
        
        Button paintButton = paintButtonObj.AddComponent<Button>();
        paintButton.onClick.AddListener(TogglePaintingMode);
        
        // Добавляем текст к кнопке
        GameObject paintTextObj = new GameObject("Text");
        paintTextObj.transform.SetParent(paintButtonObj.transform, false);
        
        RectTransform paintTextRectTransform = paintTextObj.AddComponent<RectTransform>();
        paintTextRectTransform.anchorMin = Vector2.zero;
        paintTextRectTransform.anchorMax = Vector2.one;
        paintTextRectTransform.offsetMin = Vector2.zero;
        paintTextRectTransform.offsetMax = Vector2.zero;
        
        Text paintText = paintTextObj.AddComponent<Text>();
        paintText.text = "Включить покраску";
        paintText.fontSize = 18;
        paintText.color = Color.white;
        paintText.alignment = TextAnchor.MiddleCenter;
        paintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
    
    /// <summary>
    /// Запускает детекцию стен
    /// </summary>
    private void StartWallDetection()
    {
        if (wallDetector != null)
        {
            wallDetector.StartDetection();
            isDetectionRunning = true;
            Debug.Log("Детекция стен запущена");
        }
        else
        {
            Debug.LogError("WallDetector не назначен для RealWallPaintingDemo");
        }
    }
    
    /// <summary>
    /// Останавливает детекцию стен
    /// </summary>
    private void StopWallDetection()
    {
        if (wallDetector != null)
        {
            wallDetector.StopDetection();
            isDetectionRunning = false;
            Debug.Log("Детекция стен остановлена");
        }
    }
    
    /// <summary>
    /// Переключает режим детекции стен
    /// </summary>
    private void ToggleWallDetection()
    {
        if (isDetectionRunning)
        {
            StopWallDetection();
            
            // Обновляем текст кнопки
            Transform buttonTransform = mainCanvas.transform.Find("DetectionButton/Text");
            if (buttonTransform != null)
            {
                Text buttonText = buttonTransform.GetComponent<Text>();
                if (buttonText != null)
                {
                    buttonText.text = "Старт детекция";
                }
            }
        }
        else
        {
            StartWallDetection();
            
            // Обновляем текст кнопки
            Transform buttonTransform = mainCanvas.transform.Find("DetectionButton/Text");
            if (buttonTransform != null)
            {
                Text buttonText = buttonTransform.GetComponent<Text>();
                if (buttonText != null)
                {
                    buttonText.text = "Стоп детекция";
                }
            }
        }
    }
    
    /// <summary>
    /// Включает режим покраски
    /// </summary>
    private void EnablePaintingMode()
    {
        if (controller != null)
        {
            controller.EnablePaintingMode();
            isPaintingEnabled = true;
            Debug.Log("Режим покраски включен");
        }
        else
        {
            Debug.LogError("RealWallPaintingController не назначен для RealWallPaintingDemo");
        }
    }
    
    /// <summary>
    /// Выключает режим покраски
    /// </summary>
    private void DisablePaintingMode()
    {
        if (controller != null)
        {
            controller.DisablePaintingMode();
            isPaintingEnabled = false;
            Debug.Log("Режим покраски выключен");
        }
    }
    
    /// <summary>
    /// Переключает режим покраски
    /// </summary>
    private void TogglePaintingMode()
    {
        if (isPaintingEnabled)
        {
            DisablePaintingMode();
            
            // Обновляем текст кнопки
            Transform buttonTransform = mainCanvas.transform.Find("PaintButton/Text");
            if (buttonTransform != null)
            {
                Text buttonText = buttonTransform.GetComponent<Text>();
                if (buttonText != null)
                {
                    buttonText.text = "Включить покраску";
                }
            }
        }
        else
        {
            EnablePaintingMode();
            
            // Обновляем текст кнопки
            Transform buttonTransform = mainCanvas.transform.Find("PaintButton/Text");
            if (buttonTransform != null)
            {
                Text buttonText = buttonTransform.GetComponent<Text>();
                if (buttonText != null)
                {
                    buttonText.text = "Выключить покраску";
                }
            }
        }
    }
    
    /// <summary>
    /// Устанавливает контроллер покраски стен
    /// </summary>
    public void SetController(RealWallPaintingController controller)
    {
        this.controller = controller;
    }

    /// <summary>
    /// Обновляет счетчик FPS
    /// </summary>
    private void UpdateFPSCounter()
    {
        if (fpsText == null)
            return;
            
        float fps = 1.0f / Time.deltaTime;
        fpsText.text = $"FPS: {Mathf.RoundToInt(fps)}";
    }

    private void Update()
    {
        // Обработка ввода для переключения режимов
        if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
        {
            ToggleWallDetection();
        }
        
        if (UnityEngine.Input.GetKeyDown(KeyCode.P))
        {
            TogglePaintingMode();
        }
        
        if (UnityEngine.Input.GetKeyDown(KeyCode.H))
        {
            ToggleHelpPanel();
        }
        
        // Обновление FPS счетчика
        UpdateFPSCounter();
    }
} 