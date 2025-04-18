using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Контроллер UI для управления функционалом покраски стен
/// </summary>
public class WallPaintingUIController : MonoBehaviour
{
    [Header("Компоненты")]
    [Tooltip("Компонент WallPainter для покраски стен")]
    public WallPainter wallPainter;
    
    [Tooltip("Компонент DeepLabDecoder для сегментации стен")]
    public DeepLabDecoder deepLabDecoder;
    
    [Tooltip("Компонент ARWorldMapController для сохранения/загрузки AR карты")]
    public ARWorldMapController worldMapController;

    [Header("UI элементы")]
    [Tooltip("Панель управления")]
    public GameObject controlPanel;
    
    [Tooltip("Панель цветов")]
    public GameObject colorPanel;
    
    [Tooltip("Панель настроек")]
    public GameObject settingsPanel;
    
    [Tooltip("Текст статуса")]
    public TextMeshProUGUI statusText;
    
    [Tooltip("Переключатель автопокраски")]
    public Toggle autoPaintToggle;
    
    [Tooltip("Ползунок дистанции покраски")]
    public Slider paintDistanceSlider;
    
    [Tooltip("Кнопка показа/скрытия сегментации")]
    public Toggle showSegmentationToggle;
    
    [Tooltip("Панель индикации прогресса")]
    public GameObject progressPanel;
    
    [Tooltip("Индикатор прогресса")]
    public Slider progressSlider;
    
    [Tooltip("Текст прогресса")]
    public TextMeshProUGUI progressText;

    [Header("Префабы цветов")]
    [Tooltip("Префаб кнопки цвета")]
    public GameObject colorButtonPrefab;
    
    [Tooltip("Родительский объект для кнопок цветов")]
    public Transform colorButtonsContainer;
    
    [Tooltip("Доступные цвета покраски")]
    public Color[] availableColors = new Color[]
    {
        Color.red,
        Color.green, 
        Color.blue,
        Color.yellow,
        Color.cyan,
        Color.magenta,
        new Color(1, 0.5f, 0),  // Оранжевый
        new Color(0.5f, 0, 1),  // Фиолетовый
        new Color(0, 0.5f, 0),  // Темно-зеленый
        Color.white,
        Color.grey,
        Color.black
    };

    // Флаг инициализации UI
    private bool uiInitialized = false;

    void Start()
    {
        // Проверяем наличие необходимых компонентов
        if (wallPainter == null)
        {
            wallPainter = FindObjectOfType<WallPainter>();
        }
        
        if (deepLabDecoder == null)
        {
            deepLabDecoder = FindObjectOfType<DeepLabDecoder>();
        }
        
        if (worldMapController == null)
        {
            worldMapController = FindObjectOfType<ARWorldMapController>();
        }

        // Инициализируем UI
        InitializeUI();
        
        // Скрываем панель прогресса
        if (progressPanel != null)
        {
            progressPanel.SetActive(false);
        }
        
        // Обновляем текст статуса
        UpdateStatusText("Инициализация AR...");
    }

    void Update()
    {
        if (!uiInitialized)
            return;
            
        // Проверяем готовность DeepLabV3
        if (deepLabDecoder != null && deepLabDecoder.wallMask != null)
        {
            UpdateStatusText("Готово к покраске стен");
        }
    }

    /// <summary>
    /// Инициализация UI элементов
    /// </summary>
    private void InitializeUI()
    {
        // Инициализация переключателя автопокраски
        if (autoPaintToggle != null && wallPainter != null)
        {
            autoPaintToggle.isOn = wallPainter.autoPaint;
            autoPaintToggle.onValueChanged.AddListener((value) => 
            {
                wallPainter.autoPaint = value;
            });
        }
        
        // Инициализация слайдера дистанции
        if (paintDistanceSlider != null && wallPainter != null)
        {
            paintDistanceSlider.value = wallPainter.paintDistance;
            paintDistanceSlider.onValueChanged.AddListener((value) => 
            {
                wallPainter.paintDistance = value;
            });
        }
        
        // Инициализация переключателя отображения сегментации
        if (showSegmentationToggle != null && deepLabDecoder != null)
        {
            showSegmentationToggle.isOn = deepLabDecoder.showSegmentation;
            showSegmentationToggle.onValueChanged.AddListener((value) => 
            {
                deepLabDecoder.showSegmentation = value;
            });
        }
        
        // Создание кнопок цветов
        CreateColorButtons();
        
        // Скрываем панели по умолчанию
        if (colorPanel != null)
        {
            colorPanel.SetActive(false);
        }
        
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        
        uiInitialized = true;
    }

    /// <summary>
    /// Создание кнопок выбора цвета
    /// </summary>
    private void CreateColorButtons()
    {
        if (colorButtonPrefab == null || colorButtonsContainer == null)
            return;
        
        // Удаляем существующие кнопки
        foreach (Transform child in colorButtonsContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Создаем кнопки для каждого доступного цвета
        foreach (Color color in availableColors)
        {
            GameObject buttonObj = Instantiate(colorButtonPrefab, colorButtonsContainer);
            Button button = buttonObj.GetComponent<Button>();
            
            if (button != null)
            {
                // Устанавливаем цвет кнопки
                ColorBlock colorBlock = button.colors;
                colorBlock.normalColor = color;
                colorBlock.highlightedColor = color * 1.2f;
                colorBlock.pressedColor = color * 0.8f;
                button.colors = colorBlock;
                
                // Иначе устанавливаем цвет через Image если он есть
                Image image = button.GetComponent<Image>();
                if (image != null)
                {
                    image.color = color;
                }
                
                // Добавляем обработчик нажатия
                button.onClick.AddListener(() => SetPaintColor(color));
            }
        }
    }

    /// <summary>
    /// Устанавливает выбранный цвет покраски
    /// </summary>
    public void SetPaintColor(Color color)
    {
        if (wallPainter != null)
        {
            wallPainter.paintColor = color;
            
            // Также обновляем цвет в DeepLabDecoder если нужно
            if (deepLabDecoder != null)
            {
                deepLabDecoder.wallHighlightColor = new Color(color.r, color.g, color.b, 0.5f);
            }
            
            // Скрываем панель цветов
            if (colorPanel != null)
            {
                colorPanel.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Показывает/скрывает панель выбора цвета
    /// </summary>
    public void ToggleColorPanel()
    {
        if (colorPanel != null)
        {
            colorPanel.SetActive(!colorPanel.activeSelf);
            
            // Скрываем другие панели
            if (settingsPanel != null && colorPanel.activeSelf)
            {
                settingsPanel.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Показывает/скрывает панель настроек
    /// </summary>
    public void ToggleSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
            
            // Скрываем другие панели
            if (colorPanel != null && settingsPanel.activeSelf)
            {
                colorPanel.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Обновляет текст статуса
    /// </summary>
    public void UpdateStatusText(string text)
    {
        if (statusText != null)
        {
            statusText.text = text;
        }
    }

    /// <summary>
    /// Показывает панель прогресса с указанным текстом
    /// </summary>
    public void ShowProgress(string text, float progress = 0f)
    {
        if (progressPanel != null)
        {
            progressPanel.SetActive(true);
            
            if (progressText != null)
            {
                progressText.text = text;
            }
            
            if (progressSlider != null)
            {
                progressSlider.value = progress;
            }
        }
    }

    /// <summary>
    /// Скрывает панель прогресса
    /// </summary>
    public void HideProgress()
    {
        if (progressPanel != null)
        {
            progressPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Очищает все покрашенные стены
    /// </summary>
    public void ClearAllPaintedWalls()
    {
        if (wallPainter != null)
        {
            wallPainter.ClearAllPaintedWalls();
            UpdateStatusText("Все покрашенные стены удалены");
        }
    }

    /// <summary>
    /// Сохраняет AR World Map
    /// </summary>
    public void SaveARWorldMap()
    {
        if (worldMapController != null)
        {
            ShowProgress("Сохранение карты мира...");
            
            // Запускаем сохранение
            worldMapController.SaveWorldMap();
            
            // Скрываем прогресс через 2 секунды
            Invoke("HideProgress", 2f);
            
            UpdateStatusText("Карта мира сохранена");
        }
    }

    /// <summary>
    /// Загружает AR World Map
    /// </summary>
    public void LoadARWorldMap()
    {
        if (worldMapController != null)
        {
            ShowProgress("Загрузка карты мира...");
            
            // Запускаем загрузку
            worldMapController.LoadWorldMap();
            
            // Скрываем прогресс через 3 секунды
            Invoke("HideProgress", 3f);
            
            UpdateStatusText("Карта мира загружена");
        }
    }
} 