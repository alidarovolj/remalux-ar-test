using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using WallDetection;

namespace UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Main UI References")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject debugPanel;
        [SerializeField] private GameObject colorPickerPanel;
        
        [Header("Main Controls")]
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button debugButton;
        [SerializeField] private Button colorPickerButton;
        [SerializeField] private Button captureButton;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button redoButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        
        [Header("Settings UI")]
        [SerializeField] private Slider brightnessSlider;
        [SerializeField] private Slider contrastSlider;
        [SerializeField] private Slider brushSizeSlider;
        [SerializeField] private Toggle lowLightModeToggle;
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private Button backFromSettingsButton;
        
        [Header("Debug UI")]
        [SerializeField] private Toggle showMaskToggle;
        [SerializeField] private Toggle showEdgesToggle;
        [SerializeField] private Toggle showStatisticsToggle;
        [SerializeField] private Toggle autoTestModeToggle;
        [SerializeField] private Button captureTestFrameButton;
        [SerializeField] private Button saveTestResultsButton;
        [SerializeField] private Button backFromDebugButton;
        
        [Header("Color Picker UI")]
        [SerializeField] private Image selectedColorPreview;
        [SerializeField] private Button[] colorButtons;
        [SerializeField] private Button customColorButton;
        [SerializeField] private Button backFromColorPickerButton;
        
        [Header("Text Elements")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI fpsText;
        [SerializeField] private TextMeshProUGUI wallAreaText;
        
        [Header("Component References")]
        [SerializeField] private DeepLabDecoder deepLabDecoder;
        [SerializeField] private WallDetectionTester wallDetectionTester;
        [SerializeField] private PerformanceProfiler performanceProfiler;
        
        // Wall Painter reference will be added when that class is created
        // [SerializeField] private WallPainter wallPainter;
        
        // Event for color selection
        public UnityEvent<Color> onColorSelected = new UnityEvent<Color>();
        
        // Predefined colors
        private readonly Color[] predefinedColors = new Color[]
        {
            new Color(1, 1, 1),       // White
            new Color(0.9f, 0.9f, 0.9f), // Light Gray
            new Color(0.8f, 0.8f, 0.8f), // Gray
            new Color(0.95f, 0.95f, 0.85f), // Cream
            new Color(0.96f, 0.87f, 0.7f),  // Beige
            new Color(0.76f, 0.69f, 0.57f), // Taupe
            new Color(0.55f, 0.71f, 0.73f), // Light Blue
            new Color(0.86f, 0.94f, 0.97f), // Sky Blue
            new Color(0.56f, 0.74f, 0.56f), // Sage Green
            new Color(0.98f, 0.84f, 0.65f), // Peach
            new Color(0.96f, 0.76f, 0.76f), // Light Pink
            new Color(0.8f, 0.6f, 0.4f)     // Light Brown
        };
        
        private Color currentColor = Color.white;
        
        private void Start()
        {
            InitializeUI();
            SetupEventListeners();
            ShowMainPanel();
        }
        
        private void InitializeUI()
        {
            // Initialize default UI state
            if (mainPanel) mainPanel.SetActive(true);
            if (settingsPanel) settingsPanel.SetActive(false);
            if (debugPanel) debugPanel.SetActive(false);
            if (colorPickerPanel) colorPickerPanel.SetActive(false);
            
            // Initialize color buttons
            InitializeColorButtons();
            
            // Initialize dropdowns
            if (resolutionDropdown)
            {
                resolutionDropdown.ClearOptions();
                resolutionDropdown.AddOptions(new List<string> { "224 x 224", "320 x 320", "512 x 512", "768 x 768", "Пользовательский" });
            }
            
            // Initialize sliders
            if (brightnessSlider && deepLabDecoder)
                brightnessSlider.value = deepLabDecoder.lowLightBoost;
                
            if (contrastSlider && deepLabDecoder)
                contrastSlider.value = deepLabDecoder.contrastEnhancement;
                
            if (brushSizeSlider)
                brushSizeSlider.value = 10f; // Default brush size
                
            // Initialize toggles
            if (lowLightModeToggle && deepLabDecoder)
                lowLightModeToggle.isOn = deepLabDecoder.autoAdjustLighting;
                
            if (showMaskToggle && wallDetectionTester)
                showMaskToggle.isOn = true;
                
            if (showEdgesToggle && wallDetectionTester)
                showEdgesToggle.isOn = true;
                
            if (showStatisticsToggle && performanceProfiler)
                showStatisticsToggle.isOn = true;
                
            if (autoTestModeToggle && wallDetectionTester)
                autoTestModeToggle.isOn = false;
            
            // Set selected color preview
            if (selectedColorPreview)
            {
                selectedColorPreview.color = currentColor;
            }
        }
        
        private void InitializeColorButtons()
        {
            if (colorButtons == null || colorButtons.Length == 0)
                return;
                
            int maxColors = Mathf.Min(colorButtons.Length, predefinedColors.Length);
            
            for (int i = 0; i < maxColors; i++)
            {
                if (colorButtons[i] != null)
                {
                    Image buttonImage = colorButtons[i].GetComponent<Image>();
                    if (buttonImage != null)
                    {
                        buttonImage.color = predefinedColors[i];
                    }
                    
                    int colorIndex = i;
                    colorButtons[i].onClick.AddListener(() => SelectColor(predefinedColors[colorIndex]));
                }
            }
        }
        
        private void SetupEventListeners()
        {
            // Main panel buttons
            if (settingsButton) settingsButton.onClick.AddListener(ShowSettingsPanel);
            if (debugButton) debugButton.onClick.AddListener(ShowDebugPanel);
            if (colorPickerButton) colorPickerButton.onClick.AddListener(ShowColorPickerPanel);
            if (captureButton && wallDetectionTester) captureButton.onClick.AddListener(wallDetectionTester.CaptureFrame);
            if (undoButton) undoButton.onClick.AddListener(OnUndo);
            if (redoButton) redoButton.onClick.AddListener(OnRedo);
            if (resetButton) resetButton.onClick.AddListener(OnReset);
            if (saveButton) saveButton.onClick.AddListener(OnSave);
            if (loadButton) loadButton.onClick.AddListener(OnLoad);
            
            // Settings panel
            if (brightnessSlider && deepLabDecoder) 
                brightnessSlider.onValueChanged.AddListener((val) => { deepLabDecoder.lowLightBoost = val; });
                
            if (contrastSlider && deepLabDecoder)
                contrastSlider.onValueChanged.AddListener((val) => { deepLabDecoder.contrastEnhancement = val; });
                
            if (brushSizeSlider) 
                brushSizeSlider.onValueChanged.AddListener(OnBrushSizeChanged);
                
            if (lowLightModeToggle && deepLabDecoder)
                lowLightModeToggle.onValueChanged.AddListener((val) => { deepLabDecoder.autoAdjustLighting = val; });
                
            if (resolutionDropdown && deepLabDecoder)
                resolutionDropdown.onValueChanged.AddListener((val) => { 
                    if (val < 4) deepLabDecoder.resolutionPreset = (DeepLabDecoder.InputResolution)val;
                    else deepLabDecoder.resolutionPreset = DeepLabDecoder.InputResolution.Custom;
                });
                
            if (backFromSettingsButton) backFromSettingsButton.onClick.AddListener(ShowMainPanel);
            
            // Debug panel
            if (showMaskToggle && wallDetectionTester)
                showMaskToggle.onValueChanged.AddListener((val) => { 
                    if (wallDetectionTester.originalImageDisplay) 
                        wallDetectionTester.originalImageDisplay.gameObject.SetActive(!val);
                });
                
            if (showEdgesToggle && wallDetectionTester)
                showEdgesToggle.onValueChanged.AddListener((val) => { 
                    wallDetectionTester.showEdges = val;
                });
                
            if (showStatisticsToggle && performanceProfiler)
                showStatisticsToggle.onValueChanged.AddListener((val) => { 
                    if (performanceProfiler.statsPanel)
                        performanceProfiler.statsPanel.SetActive(val);
                });
                
            if (autoTestModeToggle && wallDetectionTester)
                autoTestModeToggle.onValueChanged.AddListener((val) => { 
                    wallDetectionTester.autoCaptureEnabled = val;
                });
                
            if (captureTestFrameButton && wallDetectionTester)
                captureTestFrameButton.onClick.AddListener(wallDetectionTester.CaptureFrame);
                
            if (saveTestResultsButton && wallDetectionTester)
                saveTestResultsButton.onClick.AddListener(wallDetectionTester.SaveTestResults);
                
            if (backFromDebugButton) backFromDebugButton.onClick.AddListener(ShowMainPanel);
            
            // Color picker panel
            if (customColorButton) customColorButton.onClick.AddListener(ShowCustomColorPicker);
            if (backFromColorPickerButton) backFromColorPickerButton.onClick.AddListener(ShowMainPanel);
        }
        
        private void ShowMainPanel()
        {
            if (mainPanel) mainPanel.SetActive(true);
            if (settingsPanel) settingsPanel.SetActive(false);
            if (debugPanel) debugPanel.SetActive(false);
            if (colorPickerPanel) colorPickerPanel.SetActive(false);
        }
        
        private void ShowSettingsPanel()
        {
            if (mainPanel) mainPanel.SetActive(false);
            if (settingsPanel) settingsPanel.SetActive(true);
            if (debugPanel) debugPanel.SetActive(false);
            if (colorPickerPanel) colorPickerPanel.SetActive(false);
        }
        
        private void ShowDebugPanel()
        {
            if (mainPanel) mainPanel.SetActive(false);
            if (settingsPanel) settingsPanel.SetActive(false);
            if (debugPanel) debugPanel.SetActive(true);
            if (colorPickerPanel) colorPickerPanel.SetActive(false);
        }
        
        private void ShowColorPickerPanel()
        {
            if (mainPanel) mainPanel.SetActive(false);
            if (settingsPanel) settingsPanel.SetActive(false);
            if (debugPanel) debugPanel.SetActive(false);
            if (colorPickerPanel) colorPickerPanel.SetActive(true);
        }
        
        private void SelectColor(Color color)
        {
            currentColor = color;
            if (selectedColorPreview)
                selectedColorPreview.color = color;
                
            onColorSelected.Invoke(color);
            ShowMainPanel();
        }
        
        private void ShowCustomColorPicker()
        {
            // Implementation will depend on what color picker asset is used
            // For now, we'll just use a basic color 
            SelectColor(new Color(
                Random.Range(0.5f, 1.0f),
                Random.Range(0.5f, 1.0f),
                Random.Range(0.5f, 1.0f)
            ));
        }
        
        private void OnBrushSizeChanged(float size)
        {
            // Will be implemented when the WallPainter class is ready
            // if (wallPainter)
            //     wallPainter.brushSize = size;
        }
        
        private void OnUndo()
        {
            // Will be implemented when the WallPainter class is ready
            // if (wallPainter)
            //     wallPainter.Undo();
            if (statusText)
                statusText.text = "Отменено последнее действие";
        }
        
        private void OnRedo()
        {
            // Will be implemented when the WallPainter class is ready
            // if (wallPainter)
            //     wallPainter.Redo();
            if (statusText)
                statusText.text = "Повторено последнее действие";
        }
        
        private void OnReset()
        {
            // Will be implemented when the WallPainter class is ready
            // if (wallPainter)
            //     wallPainter.Reset();
            if (statusText)
                statusText.text = "Сброшено к исходному состоянию";
        }
        
        private void OnSave()
        {
            // Will be implemented when saving functionality is ready
            if (statusText)
                statusText.text = "Сохранено";
        }
        
        private void OnLoad()
        {
            // Will be implemented when loading functionality is ready
            if (statusText)
                statusText.text = "Загружено";
        }
        
        public void UpdateWallAreaText(float areaPercentage)
        {
            if (wallAreaText)
                wallAreaText.text = $"Площадь стены: {areaPercentage:F1}%";
        }
        
        private void Update()
        {
            // Update FPS text if available
            if (fpsText && performanceProfiler)
            {
                fpsText.text = $"FPS: {performanceProfiler.CurrentFPS:F1}";
            }
        }
    }
} 