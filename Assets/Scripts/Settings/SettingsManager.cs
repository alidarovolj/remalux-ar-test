using System;
using System.Collections.Generic;
using UnityEngine;

namespace Remalux.Settings
{
    /// <summary>
    /// Класс для управления настройками приложения
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        // Синглтон
        private static SettingsManager _instance;
        public static SettingsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SettingsManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SettingsManager");
                        _instance = go.AddComponent<SettingsManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        // Константы для ключей PlayerPrefs
        private const string KEY_RESOLUTION_PRESET = "ResolutionPreset";
        private const string KEY_CUSTOM_RESOLUTION_WIDTH = "CustomResolutionWidth";
        private const string KEY_CUSTOM_RESOLUTION_HEIGHT = "CustomResolutionHeight";
        private const string KEY_PROCESSING_INTERVAL = "ProcessingInterval";
        private const string KEY_SHOW_PERFORMANCE_METRICS = "ShowPerformanceMetrics";
        private const string KEY_AUTO_ADJUST_LIGHTING = "AutoAdjustLighting";
        private const string KEY_LOW_LIGHT_BOOST = "LowLightBoost";
        private const string KEY_LOW_LIGHT_THRESHOLD = "LowLightThreshold";
        private const string KEY_CONTRAST_ENHANCEMENT = "ContrastEnhancement";
        private const string KEY_CURRENT_WALL_COLOR = "CurrentWallColor";
        private const string KEY_RECENT_COLORS = "RecentColors";
        private const string KEY_MAX_RECENT_COLORS = "MaxRecentColors";

        [Header("Настройки разрешения")]
        [SerializeField] private int _resolutionPreset = 1; // 0: 224x224, 1: 320x320, 2: 512x512, 3: 768x768, 4: Пользовательское
        [SerializeField] private Vector2Int _customResolution = new Vector2Int(320, 320);

        [Header("Настройки производительности")]
        [SerializeField] private float _processingInterval = 0.1f; // Интервал между обработкой кадров
        [SerializeField] private bool _showPerformanceMetrics = false;

        [Header("Настройки освещения")]
        [SerializeField] private bool _autoAdjustLighting = true;
        [SerializeField] private float _lowLightBoost = 1.5f;
        [SerializeField] private float _lowLightThreshold = 0.3f;
        [SerializeField] private float _contrastEnhancement = 1.2f;

        [Header("Настройки цвета")]
        [SerializeField] private Color _currentWallColor = Color.white;
        [SerializeField] private List<Color> _recentColors = new List<Color>();
        [SerializeField] private int _maxRecentColors = 10;

        [Header("События")]
        public event Action OnSettingsChanged;

        private void Awake()
        {
            // Проверка синглтона
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Загрузка настроек
            LoadSettings();
        }

        #region Свойства

        public int ResolutionPreset
        {
            get => _resolutionPreset;
            set
            {
                if (_resolutionPreset != value)
                {
                    _resolutionPreset = value;
                    SaveSetting(KEY_RESOLUTION_PRESET, value);
                    OnSettingsChanged?.Invoke();
                }
            }
        }

        public Vector2Int CustomResolution
        {
            get => _customResolution;
            set
            {
                if (_customResolution != value)
                {
                    _customResolution = value;
                    SaveSetting(KEY_CUSTOM_RESOLUTION_WIDTH, value.x);
                    SaveSetting(KEY_CUSTOM_RESOLUTION_HEIGHT, value.y);
                    OnSettingsChanged?.Invoke();
                }
            }
        }

        public float ProcessingInterval
        {
            get => _processingInterval;
            set
            {
                if (_processingInterval != value)
                {
                    _processingInterval = value;
                    SaveSetting(KEY_PROCESSING_INTERVAL, value);
                    OnSettingsChanged?.Invoke();
                }
            }
        }

        public bool ShowPerformanceMetrics
        {
            get => _showPerformanceMetrics;
            set
            {
                if (_showPerformanceMetrics != value)
                {
                    _showPerformanceMetrics = value;
                    SaveSetting(KEY_SHOW_PERFORMANCE_METRICS, value ? 1 : 0);
                    OnSettingsChanged?.Invoke();
                }
            }
        }

        public bool AutoAdjustLighting
        {
            get => _autoAdjustLighting;
            set
            {
                if (_autoAdjustLighting != value)
                {
                    _autoAdjustLighting = value;
                    SaveSetting(KEY_AUTO_ADJUST_LIGHTING, value ? 1 : 0);
                    OnSettingsChanged?.Invoke();
                }
            }
        }

        public float LowLightBoost
        {
            get => _lowLightBoost;
            set
            {
                if (_lowLightBoost != value)
                {
                    _lowLightBoost = value;
                    SaveSetting(KEY_LOW_LIGHT_BOOST, value);
                    OnSettingsChanged?.Invoke();
                }
            }
        }

        public float LowLightThreshold
        {
            get => _lowLightThreshold;
            set
            {
                if (_lowLightThreshold != value)
                {
                    _lowLightThreshold = value;
                    SaveSetting(KEY_LOW_LIGHT_THRESHOLD, value);
                    OnSettingsChanged?.Invoke();
                }
            }
        }

        public float ContrastEnhancement
        {
            get => _contrastEnhancement;
            set
            {
                if (_contrastEnhancement != value)
                {
                    _contrastEnhancement = value;
                    SaveSetting(KEY_CONTRAST_ENHANCEMENT, value);
                    OnSettingsChanged?.Invoke();
                }
            }
        }

        public Color CurrentWallColor
        {
            get => _currentWallColor;
            set
            {
                if (_currentWallColor != value)
                {
                    _currentWallColor = value;
                    SaveColorSetting(KEY_CURRENT_WALL_COLOR, value);
                    OnSettingsChanged?.Invoke();
                }
            }
        }

        public List<Color> RecentColors => _recentColors;

        #endregion

        #region Загрузка и сохранение настроек

        /// <summary>
        /// Загрузка всех настроек
        /// </summary>
        private void LoadSettings()
        {
            // Загрузка настроек разрешения
            _resolutionPreset = PlayerPrefs.GetInt(KEY_RESOLUTION_PRESET, 1);
            _customResolution.x = PlayerPrefs.GetInt(KEY_CUSTOM_RESOLUTION_WIDTH, 320);
            _customResolution.y = PlayerPrefs.GetInt(KEY_CUSTOM_RESOLUTION_HEIGHT, 320);

            // Загрузка настроек производительности
            _processingInterval = PlayerPrefs.GetFloat(KEY_PROCESSING_INTERVAL, 0.1f);
            _showPerformanceMetrics = PlayerPrefs.GetInt(KEY_SHOW_PERFORMANCE_METRICS, 0) == 1;

            // Загрузка настроек освещения
            _autoAdjustLighting = PlayerPrefs.GetInt(KEY_AUTO_ADJUST_LIGHTING, 1) == 1;
            _lowLightBoost = PlayerPrefs.GetFloat(KEY_LOW_LIGHT_BOOST, 1.5f);
            _lowLightThreshold = PlayerPrefs.GetFloat(KEY_LOW_LIGHT_THRESHOLD, 0.3f);
            _contrastEnhancement = PlayerPrefs.GetFloat(KEY_CONTRAST_ENHANCEMENT, 1.2f);

            // Загрузка настроек цвета
            string colorString = PlayerPrefs.GetString(KEY_CURRENT_WALL_COLOR, "1,1,1,1");
            _currentWallColor = ParseColor(colorString);

            // Загрузка недавних цветов
            _recentColors.Clear();
            string recentColorsJson = PlayerPrefs.GetString(KEY_RECENT_COLORS, "");
            if (!string.IsNullOrEmpty(recentColorsJson))
            {
                string[] colorStrings = recentColorsJson.Split('|');
                foreach (string colStr in colorStrings)
                {
                    if (!string.IsNullOrEmpty(colStr))
                    {
                        _recentColors.Add(ParseColor(colStr));
                    }
                }
            }

            _maxRecentColors = PlayerPrefs.GetInt(KEY_MAX_RECENT_COLORS, 10);
        }

        /// <summary>
        /// Сохранение целочисленной настройки
        /// </summary>
        private void SaveSetting(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Сохранение настройки с плавающей точкой
        /// </summary>
        private void SaveSetting(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Сохранение строковой настройки
        /// </summary>
        private void SaveSetting(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Сохранение настройки цвета
        /// </summary>
        private void SaveColorSetting(string key, Color color)
        {
            string colorString = $"{color.r},{color.g},{color.b},{color.a}";
            PlayerPrefs.SetString(key, colorString);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Разбор строки цвета
        /// </summary>
        private Color ParseColor(string colorString)
        {
            try
            {
                string[] components = colorString.Split(',');
                if (components.Length >= 4)
                {
                    return new Color(
                        float.Parse(components[0]),
                        float.Parse(components[1]),
                        float.Parse(components[2]),
                        float.Parse(components[3])
                    );
                }
                else if (components.Length >= 3)
                {
                    return new Color(
                        float.Parse(components[0]),
                        float.Parse(components[1]),
                        float.Parse(components[2])
                    );
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parsing color: {colorString}. Exception: {e.Message}");
            }
            return Color.white;
        }

        #endregion

        #region Публичные методы

        /// <summary>
        /// Сбросить все настройки к значениям по умолчанию
        /// </summary>
        public void ResetToDefaults()
        {
            // Сброс настроек разрешения
            ResolutionPreset = 1;
            CustomResolution = new Vector2Int(320, 320);

            // Сброс настроек производительности
            ProcessingInterval = 0.1f;
            ShowPerformanceMetrics = false;

            // Сброс настроек освещения
            AutoAdjustLighting = true;
            LowLightBoost = 1.5f;
            LowLightThreshold = 0.3f;
            ContrastEnhancement = 1.2f;

            // Сброс настроек цвета
            CurrentWallColor = Color.white;
            _recentColors.Clear();
            SaveRecentColors();

            OnSettingsChanged?.Invoke();
        }

        /// <summary>
        /// Добавить цвет в список недавних
        /// </summary>
        public void AddRecentColor(Color color)
        {
            // Удаляем этот цвет, если он уже есть в списке
            _recentColors.RemoveAll(c => ColorEquals(c, color));

            // Добавляем цвет в начало списка
            _recentColors.Insert(0, color);

            // Ограничиваем количество недавних цветов
            if (_recentColors.Count > _maxRecentColors)
            {
                _recentColors.RemoveRange(_maxRecentColors, _recentColors.Count - _maxRecentColors);
            }

            // Сохраняем недавние цвета
            SaveRecentColors();
        }

        /// <summary>
        /// Очистить список недавних цветов
        /// </summary>
        public void ClearRecentColors()
        {
            _recentColors.Clear();
            SaveRecentColors();
            OnSettingsChanged?.Invoke();
        }

        /// <summary>
        /// Получить текущее разрешение в зависимости от выбранного пресета
        /// </summary>
        public Vector2Int GetCurrentResolution()
        {
            switch (_resolutionPreset)
            {
                case 0: return new Vector2Int(224, 224);
                case 1: return new Vector2Int(320, 320);
                case 2: return new Vector2Int(512, 512);
                case 3: return new Vector2Int(768, 768);
                case 4: return _customResolution;
                default: return new Vector2Int(320, 320);
            }
        }

        #endregion

        #region Вспомогательные методы

        /// <summary>
        /// Сохранение списка недавних цветов
        /// </summary>
        private void SaveRecentColors()
        {
            if (_recentColors.Count == 0)
            {
                PlayerPrefs.SetString(KEY_RECENT_COLORS, "");
                PlayerPrefs.Save();
                return;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < _recentColors.Count; i++)
            {
                if (i > 0) sb.Append("|");
                Color c = _recentColors[i];
                sb.Append($"{c.r},{c.g},{c.b},{c.a}");
            }

            PlayerPrefs.SetString(KEY_RECENT_COLORS, sb.ToString());
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Сравнение двух цветов с допустимой погрешностью
        /// </summary>
        private bool ColorEquals(Color a, Color b, float tolerance = 0.01f)
        {
            return Mathf.Abs(a.r - b.r) < tolerance &&
                   Mathf.Abs(a.g - b.g) < tolerance &&
                   Mathf.Abs(a.b - b.b) < tolerance &&
                   Mathf.Abs(a.a - b.a) < tolerance;
        }

        #endregion
    }
} 