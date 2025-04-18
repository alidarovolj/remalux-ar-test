using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Remalux.Settings
{
    /// <summary>
    /// Класс для управления интерфейсом выбора цвета
    /// </summary>
    public class ColorPickerUI : MonoBehaviour
    {
        [Header("Основные компоненты")]
        [SerializeField] private GameObject colorPickerPanel;
        [SerializeField] private Image colorPreview;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button applyButton;
        
        [Header("Слайдеры цвета")]
        [SerializeField] private Slider redSlider;
        [SerializeField] private Slider greenSlider;
        [SerializeField] private Slider blueSlider;
        [SerializeField] private TMP_Text redValueText;
        [SerializeField] private TMP_Text greenValueText;
        [SerializeField] private TMP_Text blueValueText;
        
        [Header("Предустановленные цвета")]
        [SerializeField] private Button[] presetColorButtons;
        [SerializeField] private Color[] presetColors = new Color[]
        {
            new Color(1.0f, 1.0f, 1.0f), // белый
            new Color(0.9f, 0.9f, 0.9f), // светло-серый
            new Color(0.7f, 0.7f, 0.7f), // серый
            new Color(0.96f, 0.96f, 0.86f), // бежевый
            new Color(0.98f, 0.95f, 0.82f), // кремовый
            new Color(0.94f, 0.9f, 0.55f), // песочный
            new Color(0.85f, 0.65f, 0.45f), // коричневый
            new Color(0.82f, 0.41f, 0.12f), // терракотовый
            new Color(0.8f, 0.52f, 0.25f), // коньячный
            new Color(0.55f, 0.27f, 0.07f), // шоколадный
            new Color(0.5f, 0.2f, 0.2f), // бордовый
            new Color(0.86f, 0.08f, 0.24f), // красный
            new Color(0.98f, 0.5f, 0.45f), // коралловый
            new Color(0.96f, 0.76f, 0.76f), // розовый
            new Color(0.87f, 0.63f, 0.87f), // лавандовый
            new Color(0.54f, 0.17f, 0.89f), // фиолетовый
            new Color(0.0f, 0.0f, 0.8f), // синий
            new Color(0.0f, 0.5f, 1.0f), // голубой
            new Color(0.68f, 0.85f, 0.9f), // небесный
            new Color(0.0f, 0.8f, 0.8f), // бирюзовый
            new Color(0.56f, 0.74f, 0.56f), // фисташковый
            new Color(0.13f, 0.55f, 0.13f), // зеленый
            new Color(0.42f, 0.56f, 0.14f), // оливковый
            new Color(0.85f, 0.85f, 0.1f)  // желтый
        };
        
        [Header("Недавние цвета")]
        [SerializeField] private Button[] recentColorButtons;
        [SerializeField] private Image[] recentColorImages;
        
        private Color currentColor = Color.white;
        private Action<Color> onColorSelected;
        private SettingsManager settingsManager;
        
        /// <summary>
        /// Событие, вызываемое при выборе цвета
        /// </summary>
        public event Action<Color> OnColorSelected;
        
        private void Awake()
        {
            settingsManager = SettingsManager.Instance;
            
            if (colorPickerPanel != null)
            {
                colorPickerPanel.SetActive(false);
            }
            
            InitializeUI();
        }
        
        /// <summary>
        /// Инициализация UI компонентов
        /// </summary>
        private void InitializeUI()
        {
            // Инициализация слайдеров
            if (redSlider != null) redSlider.onValueChanged.AddListener(OnRedChanged);
            if (greenSlider != null) greenSlider.onValueChanged.AddListener(OnGreenChanged);
            if (blueSlider != null) blueSlider.onValueChanged.AddListener(OnBlueChanged);
            
            // Инициализация кнопок
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (applyButton != null) applyButton.onClick.AddListener(ApplyColor);
            
            // Инициализация предустановленных цветов
            InitializePresetColors();
            
            // Инициализация недавних цветов
            UpdateRecentColors();
        }
        
        /// <summary>
        /// Инициализация предустановленных цветов
        /// </summary>
        private void InitializePresetColors()
        {
            for (int i = 0; i < presetColorButtons.Length && i < presetColors.Length; i++)
            {
                Button button = presetColorButtons[i];
                Color color = presetColors[i];
                
                // Устанавливаем цвет кнопки
                Image image = button.GetComponent<Image>();
                if (image != null)
                {
                    image.color = color;
                }
                
                // Устанавливаем обработчик нажатия
                int index = i; // Для использования в лямбда-выражении
                button.onClick.AddListener(() => OnPresetColorClicked(index));
            }
        }
        
        /// <summary>
        /// Обновление недавних цветов
        /// </summary>
        private void UpdateRecentColors()
        {
            List<Color> recentColors = settingsManager != null ? settingsManager.RecentColors : new List<Color>();
            
            for (int i = 0; i < recentColorButtons.Length; i++)
            {
                if (i < recentColors.Count)
                {
                    recentColorImages[i].color = recentColors[i];
                    recentColorButtons[i].gameObject.SetActive(true);
                    
                    // Устанавливаем обработчик нажатия
                    int index = i; // Для использования в лямбда-выражении
                    recentColorButtons[i].onClick.RemoveAllListeners();
                    recentColorButtons[i].onClick.AddListener(() => OnRecentColorClicked(index));
                }
                else
                {
                    recentColorButtons[i].gameObject.SetActive(false);
                }
            }
        }
        
        /// <summary>
        /// Открыть панель выбора цвета
        /// </summary>
        public void Open(Color initialColor)
        {
            currentColor = initialColor;
            
            if (colorPickerPanel != null)
            {
                colorPickerPanel.SetActive(true);
            }
            
            // Устанавливаем начальные значения слайдеров
            if (redSlider != null) redSlider.value = currentColor.r;
            if (greenSlider != null) greenSlider.value = currentColor.g;
            if (blueSlider != null) blueSlider.value = currentColor.b;
            
            // Обновляем предпросмотр
            UpdateColorPreview();
            UpdateColorTexts();
            
            // Обновляем недавние цвета
            UpdateRecentColors();
        }
        
        /// <summary>
        /// Закрыть панель выбора цвета
        /// </summary>
        public void Close()
        {
            if (colorPickerPanel != null)
            {
                colorPickerPanel.SetActive(false);
            }
        }
        
        /// <summary>
        /// Применить выбранный цвет
        /// </summary>
        private void ApplyColor()
        {
            if (settingsManager != null)
            {
                // Устанавливаем текущий цвет
                settingsManager.CurrentWallColor = currentColor;
                
                // Добавляем в недавние
                settingsManager.AddRecentColor(currentColor);
                
                // Обновляем UI недавних цветов
                UpdateRecentColors();
            }
            
            // Вызываем событие выбора цвета
            OnColorSelected?.Invoke(currentColor);
            
            // Закрываем панель
            Close();
        }
        
        /// <summary>
        /// Обработчик изменения значения красного компонента
        /// </summary>
        private void OnRedChanged(float value)
        {
            currentColor.r = value;
            UpdateColorPreview();
            UpdateColorTexts();
        }
        
        /// <summary>
        /// Обработчик изменения значения зеленого компонента
        /// </summary>
        private void OnGreenChanged(float value)
        {
            currentColor.g = value;
            UpdateColorPreview();
            UpdateColorTexts();
        }
        
        /// <summary>
        /// Обработчик изменения значения синего компонента
        /// </summary>
        private void OnBlueChanged(float value)
        {
            currentColor.b = value;
            UpdateColorPreview();
            UpdateColorTexts();
        }
        
        /// <summary>
        /// Обновление предпросмотра цвета
        /// </summary>
        private void UpdateColorPreview()
        {
            if (colorPreview != null)
            {
                colorPreview.color = currentColor;
            }
        }
        
        /// <summary>
        /// Обновление текстовых полей со значениями RGB
        /// </summary>
        private void UpdateColorTexts()
        {
            if (redValueText != null)
            {
                redValueText.text = $"{Mathf.RoundToInt(currentColor.r * 255)}";
            }
            
            if (greenValueText != null)
            {
                greenValueText.text = $"{Mathf.RoundToInt(currentColor.g * 255)}";
            }
            
            if (blueValueText != null)
            {
                blueValueText.text = $"{Mathf.RoundToInt(currentColor.b * 255)}";
            }
        }
        
        /// <summary>
        /// Обработчик нажатия на предустановленный цвет
        /// </summary>
        private void OnPresetColorClicked(int index)
        {
            if (index >= 0 && index < presetColors.Length)
            {
                // Устанавливаем выбранный цвет
                currentColor = presetColors[index];
                
                // Обновляем слайдеры
                if (redSlider != null) redSlider.value = currentColor.r;
                if (greenSlider != null) greenSlider.value = currentColor.g;
                if (blueSlider != null) blueSlider.value = currentColor.b;
                
                // Обновляем предпросмотр
                UpdateColorPreview();
                UpdateColorTexts();
            }
        }
        
        /// <summary>
        /// Обработчик нажатия на недавний цвет
        /// </summary>
        private void OnRecentColorClicked(int index)
        {
            if (settingsManager != null && index >= 0 && index < settingsManager.RecentColors.Count)
            {
                // Устанавливаем выбранный цвет
                currentColor = settingsManager.RecentColors[index];
                
                // Обновляем слайдеры
                if (redSlider != null) redSlider.value = currentColor.r;
                if (greenSlider != null) greenSlider.value = currentColor.g;
                if (blueSlider != null) blueSlider.value = currentColor.b;
                
                // Обновляем предпросмотр
                UpdateColorPreview();
                UpdateColorTexts();
            }
        }
    }
} 