using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

namespace Remalux.Settings
{
    /// <summary>
    /// Класс для управления UI настроек приложения
    /// </summary>
    public class SettingsUI : MonoBehaviour
    {
        [Header("Общие элементы")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button resetButton;
        
        [Header("Навигация")]
        [SerializeField] private Button generalTabButton;
        [SerializeField] private Button performanceTabButton;
        [SerializeField] private Button lightingTabButton;
        [SerializeField] private Button aboutTabButton;
        
        [Header("Вкладки")]
        [SerializeField] private GameObject generalTab;
        [SerializeField] private GameObject performanceTab;
        [SerializeField] private GameObject lightingTab;
        [SerializeField] private GameObject aboutTab;
        
        [Header("Разрешение")]
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private GameObject customResolutionPanel;
        [SerializeField] private TMP_InputField customWidthInput;
        [SerializeField] private TMP_InputField customHeightInput;

        [Header("Производительность")]
        [SerializeField] private Slider processingIntervalSlider;
        [SerializeField] private TextMeshProUGUI processingIntervalValueText;
        [SerializeField] private Toggle showPerformanceMetricsToggle;
        
        [Header("Освещение")]
        [SerializeField] private Toggle autoAdjustLightingToggle;
        [SerializeField] private Slider lowLightBoostSlider;
        [SerializeField] private TextMeshProUGUI lowLightBoostValueText;
        [SerializeField] private Slider lowLightThresholdSlider;
        [SerializeField] private TextMeshProUGUI lowLightThresholdValueText;
        [SerializeField] private Slider contrastEnhancementSlider;
        [SerializeField] private TextMeshProUGUI contrastEnhancementValueText;
        
        [Header("О приложении")]
        [SerializeField] private TextMeshProUGUI versionText;
        [SerializeField] private TextMeshProUGUI buildDateText;

        [Header("События")]
        public UnityEvent OnSettingsClosed;

        private SettingsManager _settingsManager;
        private GameObject _currentTab;
        private bool _isInitializing = false;

        private void Awake()
        {
            _settingsManager = SettingsManager.Instance;
            
            // Скрываем панель настроек при старте
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        private void Start()
        {
            InitializeUI();
            SetupEventListeners();
            
            // Установка информации о версии
            if (versionText != null)
            {
                versionText.text = "Версия: " + Application.version;
            }
            
            if (buildDateText != null)
            {
                buildDateText.text = "Дата сборки: " + System.DateTime.Now.ToString("dd.MM.yyyy");
            }
        }

        #region Инициализация UI

        /// <summary>
        /// Инициализация элементов UI
        /// </summary>
        private void InitializeUI()
        {
            _isInitializing = true;
            
            // Установка обработчиков кнопок навигации
            if (generalTabButton != null && generalTab != null)
            {
                generalTabButton.onClick.AddListener(() => SwitchTab(generalTab));
            }
            
            if (performanceTabButton != null && performanceTab != null)
            {
                performanceTabButton.onClick.AddListener(() => SwitchTab(performanceTab));
            }
            
            if (lightingTabButton != null && lightingTab != null)
            {
                lightingTabButton.onClick.AddListener(() => SwitchTab(lightingTab));
            }
            
            if (aboutTabButton != null && aboutTab != null)
            {
                aboutTabButton.onClick.AddListener(() => SwitchTab(aboutTab));
            }

            // Инициализация выпадающего списка разрешений
            if (resolutionDropdown != null)
            {
                resolutionDropdown.ClearOptions();
                resolutionDropdown.AddOptions(new List<string>
                {
                    "224x224 (Быстрее)",
                    "320x320 (Стандартно)",
                    "512x512 (Лучше)",
                    "768x768 (Детальнее)",
                    "Пользовательское"
                });
                resolutionDropdown.value = _settingsManager.ResolutionPreset;
                resolutionDropdown.RefreshShownValue();
                
                // Показываем панель пользовательского разрешения, если выбрано пользовательское
                UpdateCustomResolutionPanelVisibility();
            }

            // Инициализация полей пользовательского разрешения
            if (customWidthInput != null && customHeightInput != null)
            {
                Vector2Int customRes = _settingsManager.CustomResolution;
                customWidthInput.text = customRes.x.ToString();
                customHeightInput.text = customRes.y.ToString();
            }

            // Инициализация слайдера интервала обработки
            if (processingIntervalSlider != null)
            {
                processingIntervalSlider.value = _settingsManager.ProcessingInterval;
                UpdateProcessingIntervalValueText();
            }

            // Инициализация переключателя метрик производительности
            if (showPerformanceMetricsToggle != null)
            {
                showPerformanceMetricsToggle.isOn = _settingsManager.ShowPerformanceMetrics;
            }

            // Инициализация переключателя автоматической регулировки освещения
            if (autoAdjustLightingToggle != null)
            {
                autoAdjustLightingToggle.isOn = _settingsManager.AutoAdjustLighting;
            }

            // Инициализация слайдера усиления при слабом освещении
            if (lowLightBoostSlider != null)
            {
                lowLightBoostSlider.value = _settingsManager.LowLightBoost;
                UpdateLowLightBoostValueText();
            }

            // Инициализация слайдера порога слабого освещения
            if (lowLightThresholdSlider != null)
            {
                lowLightThresholdSlider.value = _settingsManager.LowLightThreshold;
                UpdateLowLightThresholdValueText();
            }

            // Инициализация слайдера усиления контраста
            if (contrastEnhancementSlider != null)
            {
                contrastEnhancementSlider.value = _settingsManager.ContrastEnhancement;
                UpdateContrastEnhancementValueText();
            }
            
            // Показ вкладки по умолчанию
            if (generalTab != null)
            {
                SwitchTab(generalTab);
            }

            _isInitializing = false;
        }

        /// <summary>
        /// Настройка обработчиков событий
        /// </summary>
        private void SetupEventListeners()
        {
            // Обработчик кнопки закрытия
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseSettings);
            }

            // Обработчик кнопки сброса
            if (resetButton != null)
            {
                resetButton.onClick.AddListener(ResetSettings);
            }

            // Обработчик выпадающего списка разрешений
            if (resolutionDropdown != null)
            {
                resolutionDropdown.onValueChanged.AddListener(OnResolutionPresetChanged);
            }

            // Обработчики полей пользовательского разрешения
            if (customWidthInput != null)
            {
                customWidthInput.onEndEdit.AddListener(OnCustomResolutionWidthChanged);
            }

            if (customHeightInput != null)
            {
                customHeightInput.onEndEdit.AddListener(OnCustomResolutionHeightChanged);
            }

            // Обработчик слайдера интервала обработки
            if (processingIntervalSlider != null)
            {
                processingIntervalSlider.onValueChanged.AddListener(OnProcessingIntervalChanged);
            }

            // Обработчик переключателя метрик производительности
            if (showPerformanceMetricsToggle != null)
            {
                showPerformanceMetricsToggle.onValueChanged.AddListener(OnShowPerformanceMetricsChanged);
            }

            // Обработчик переключателя автоматической регулировки освещения
            if (autoAdjustLightingToggle != null)
            {
                autoAdjustLightingToggle.onValueChanged.AddListener(OnAutoAdjustLightingChanged);
            }

            // Обработчик слайдера усиления при слабом освещении
            if (lowLightBoostSlider != null)
            {
                lowLightBoostSlider.onValueChanged.AddListener(OnLowLightBoostChanged);
            }

            // Обработчик слайдера порога слабого освещения
            if (lowLightThresholdSlider != null)
            {
                lowLightThresholdSlider.onValueChanged.AddListener(OnLowLightThresholdChanged);
            }

            // Обработчик слайдера усиления контраста
            if (contrastEnhancementSlider != null)
            {
                contrastEnhancementSlider.onValueChanged.AddListener(OnContrastEnhancementChanged);
            }

            // Обработчик события изменения настроек
            _settingsManager.OnSettingsChanged += RefreshUI;
        }

        #endregion

        #region Публичные методы

        /// <summary>
        /// Открыть панель настроек
        /// </summary>
        public void OpenSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
                RefreshUI();
            }
        }

        /// <summary>
        /// Закрыть панель настроек
        /// </summary>
        public void CloseSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
                OnSettingsClosed?.Invoke();
            }
        }

        /// <summary>
        /// Сбросить настройки к значениям по умолчанию
        /// </summary>
        public void ResetSettings()
        {
            _settingsManager.ResetToDefaults();
            RefreshUI();
        }

        /// <summary>
        /// Обновить элементы UI на основе текущих настроек
        /// </summary>
        public void RefreshUI()
        {
            _isInitializing = true;

            if (resolutionDropdown != null)
            {
                resolutionDropdown.value = _settingsManager.ResolutionPreset;
                resolutionDropdown.RefreshShownValue();
                UpdateCustomResolutionPanelVisibility();
            }

            if (customWidthInput != null && customHeightInput != null)
            {
                Vector2Int customRes = _settingsManager.CustomResolution;
                customWidthInput.text = customRes.x.ToString();
                customHeightInput.text = customRes.y.ToString();
            }

            if (processingIntervalSlider != null)
            {
                processingIntervalSlider.value = _settingsManager.ProcessingInterval;
                UpdateProcessingIntervalValueText();
            }

            if (showPerformanceMetricsToggle != null)
            {
                showPerformanceMetricsToggle.isOn = _settingsManager.ShowPerformanceMetrics;
            }

            if (autoAdjustLightingToggle != null)
            {
                autoAdjustLightingToggle.isOn = _settingsManager.AutoAdjustLighting;
            }

            if (lowLightBoostSlider != null)
            {
                lowLightBoostSlider.value = _settingsManager.LowLightBoost;
                UpdateLowLightBoostValueText();
            }

            if (lowLightThresholdSlider != null)
            {
                lowLightThresholdSlider.value = _settingsManager.LowLightThreshold;
                UpdateLowLightThresholdValueText();
            }

            if (contrastEnhancementSlider != null)
            {
                contrastEnhancementSlider.value = _settingsManager.ContrastEnhancement;
                UpdateContrastEnhancementValueText();
            }

            _isInitializing = false;
        }

        /// <summary>
        /// Переключение вкладки настроек
        /// </summary>
        public void SwitchTab(GameObject targetTab)
        {
            if (targetTab == null) return;
            
            // Скрываем текущую вкладку
            if (_currentTab != null)
            {
                _currentTab.SetActive(false);
            }
            
            // Показываем выбранную вкладку
            targetTab.SetActive(true);
            _currentTab = targetTab;
            
            // Обновляем внешний вид кнопок навигации
            UpdateTabButtons(targetTab);
        }

        #endregion

        #region Обработчики событий UI

        private void OnResolutionPresetChanged(int value)
        {
            if (_isInitializing) return;

            _settingsManager.ResolutionPreset = value;
            UpdateCustomResolutionPanelVisibility();
        }

        private void OnCustomResolutionWidthChanged(string value)
        {
            if (_isInitializing) return;

            if (int.TryParse(value, out int width))
            {
                // Ограничиваем минимальное и максимальное значение
                width = Mathf.Clamp(width, 128, 1024);
                
                Vector2Int customRes = _settingsManager.CustomResolution;
                customRes.x = width;
                _settingsManager.CustomResolution = customRes;
                
                // Обновляем текст, если значение было изменено
                customWidthInput.text = width.ToString();
            }
            else
            {
                // Возвращаем предыдущее значение, если ввод некорректный
                customWidthInput.text = _settingsManager.CustomResolution.x.ToString();
            }
        }

        private void OnCustomResolutionHeightChanged(string value)
        {
            if (_isInitializing) return;

            if (int.TryParse(value, out int height))
            {
                // Ограничиваем минимальное и максимальное значение
                height = Mathf.Clamp(height, 128, 1024);
                
                Vector2Int customRes = _settingsManager.CustomResolution;
                customRes.y = height;
                _settingsManager.CustomResolution = customRes;
                
                // Обновляем текст, если значение было изменено
                customHeightInput.text = height.ToString();
            }
            else
            {
                // Возвращаем предыдущее значение, если ввод некорректный
                customHeightInput.text = _settingsManager.CustomResolution.y.ToString();
            }
        }

        private void OnProcessingIntervalChanged(float value)
        {
            if (_isInitializing) return;

            _settingsManager.ProcessingInterval = value;
            UpdateProcessingIntervalValueText();
        }

        private void OnShowPerformanceMetricsChanged(bool value)
        {
            if (_isInitializing) return;

            _settingsManager.ShowPerformanceMetrics = value;
        }

        private void OnAutoAdjustLightingChanged(bool value)
        {
            if (_isInitializing) return;

            _settingsManager.AutoAdjustLighting = value;
            
            // Включаем/отключаем соответствующие слайдеры
            if (lowLightBoostSlider != null)
            {
                lowLightBoostSlider.interactable = value;
            }
            
            if (lowLightThresholdSlider != null)
            {
                lowLightThresholdSlider.interactable = value;
            }
            
            if (contrastEnhancementSlider != null)
            {
                contrastEnhancementSlider.interactable = value;
            }
        }

        private void OnLowLightBoostChanged(float value)
        {
            if (_isInitializing) return;

            _settingsManager.LowLightBoost = value;
            UpdateLowLightBoostValueText();
        }

        private void OnLowLightThresholdChanged(float value)
        {
            if (_isInitializing) return;

            _settingsManager.LowLightThreshold = value;
            UpdateLowLightThresholdValueText();
        }

        private void OnContrastEnhancementChanged(float value)
        {
            if (_isInitializing) return;

            _settingsManager.ContrastEnhancement = value;
            UpdateContrastEnhancementValueText();
        }

        #endregion

        #region Вспомогательные методы

        /// <summary>
        /// Обновить видимость панели пользовательского разрешения
        /// </summary>
        private void UpdateCustomResolutionPanelVisibility()
        {
            if (customResolutionPanel != null)
            {
                // Показываем панель только если выбрано пользовательское разрешение (индекс 4)
                customResolutionPanel.SetActive(_settingsManager.ResolutionPreset == 4);
            }
        }

        /// <summary>
        /// Обновить текст значения интервала обработки
        /// </summary>
        private void UpdateProcessingIntervalValueText()
        {
            if (processingIntervalValueText != null)
            {
                float value = _settingsManager.ProcessingInterval;
                processingIntervalValueText.text = value <= 0.05f ? "Максимум" : $"{value:F2} сек";
            }
        }

        /// <summary>
        /// Обновить текст значения усиления при слабом освещении
        /// </summary>
        private void UpdateLowLightBoostValueText()
        {
            if (lowLightBoostValueText != null)
            {
                lowLightBoostValueText.text = $"x{_settingsManager.LowLightBoost:F1}";
            }
        }

        /// <summary>
        /// Обновить текст значения порога слабого освещения
        /// </summary>
        private void UpdateLowLightThresholdValueText()
        {
            if (lowLightThresholdValueText != null)
            {
                int percent = Mathf.RoundToInt(_settingsManager.LowLightThreshold * 100);
                lowLightThresholdValueText.text = $"{percent}%";
            }
        }

        /// <summary>
        /// Обновить текст значения усиления контраста
        /// </summary>
        private void UpdateContrastEnhancementValueText()
        {
            if (contrastEnhancementValueText != null)
            {
                contrastEnhancementValueText.text = $"x{_settingsManager.ContrastEnhancement:F1}";
            }
        }
        
        /// <summary>
        /// Обновляет внешний вид кнопок навигации
        /// </summary>
        private void UpdateTabButtons(GameObject activeTab)
        {
            if (generalTabButton != null)
            {
                UpdateTabButtonState(generalTabButton, activeTab == generalTab);
            }
            
            if (performanceTabButton != null)
            {
                UpdateTabButtonState(performanceTabButton, activeTab == performanceTab);
            }
            
            if (lightingTabButton != null)
            {
                UpdateTabButtonState(lightingTabButton, activeTab == lightingTab);
            }
            
            if (aboutTabButton != null)
            {
                UpdateTabButtonState(aboutTabButton, activeTab == aboutTab);
            }
        }
        
        /// <summary>
        /// Обновляет состояние кнопки вкладки
        /// </summary>
        private void UpdateTabButtonState(Button button, bool isActive)
        {
            // Находим компонент изображения кнопки
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                // Изменяем прозрачность в зависимости от состояния
                Color color = buttonImage.color;
                color.a = isActive ? 1.0f : 0.6f;
                buttonImage.color = color;
            }
            
            // Находим текст кнопки
            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                // Изменяем жирность текста в зависимости от состояния
                buttonText.fontStyle = isActive ? FontStyles.Bold : FontStyles.Normal;
            }
        }

        #endregion

        private void OnDestroy()
        {
            // Отписываемся от события изменения настроек
            if (_settingsManager != null)
            {
                _settingsManager.OnSettingsChanged -= RefreshUI;
            }
        }
    }
} 