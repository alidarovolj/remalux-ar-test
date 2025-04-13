using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Remalux.AR
{
      /// <summary>
      /// Управляет UI элементами для функционала покраски стен
      /// </summary>
      public class WallPaintingUIManager : MonoBehaviour
      {
            [Header("Панели UI")]
            [SerializeField] private GameObject mainPanel;
            [SerializeField] private GameObject colorPalettePanel;
            [SerializeField] private GameObject brushSettingsPanel;
            [SerializeField] private GameObject helpPanel;

            [Header("Кнопки навигации")]
            [SerializeField] private Button colorPaletteButton;
            [SerializeField] private Button brushSettingsButton;
            [SerializeField] private Button helpButton;
            [SerializeField] private Button backButton;
            [SerializeField] private Button resetButton;
            [SerializeField] private Button screenshotButton;

            [Header("Настройки цветов")]
            [SerializeField] private List<Material> paintMaterials = new List<Material>();
            [SerializeField] private GameObject colorButtonPrefab;
            [SerializeField] private Transform colorButtonsContainer;

            [Header("Настройки кисти")]
            [SerializeField] private Slider brushSizeSlider;
            [SerializeField] private Text brushSizeText;

            // Ссылка на WallPainter
            private WallPainter wallPainter;

            private void Awake()
            {
                  // Находим компонент WallPainter в сцене
                  wallPainter = UnityEngine.Object.FindFirstObjectByType<WallPainter>();

                  if (wallPainter == null)
                  {
                        Debug.LogError("WallPainter не найден в сцене! UI не будет работать корректно.");
                  }
            }

            private void Start()
            {
                  // Инициализируем UI элементы
                  InitializeUI();

                  // Создаем кнопки выбора цвета
                  CreateColorButtons();

                  // Показываем главную панель
                  ShowPanel(mainPanel);
            }

            /// <summary>
            /// Инициализирует UI элементы и добавляет обработчики событий
            /// </summary>
            private void InitializeUI()
            {
                  // Настраиваем кнопки навигации
                  if (colorPaletteButton != null)
                        colorPaletteButton.onClick.AddListener(() => ShowPanel(colorPalettePanel));

                  if (brushSettingsButton != null)
                        brushSettingsButton.onClick.AddListener(() => ShowPanel(brushSettingsPanel));

                  if (helpButton != null)
                        helpButton.onClick.AddListener(() => ShowPanel(helpPanel));

                  if (backButton != null)
                        backButton.onClick.AddListener(() => ShowPanel(mainPanel));

                  // Настраиваем кнопки функций
                  if (resetButton != null && wallPainter != null)
                        resetButton.onClick.AddListener(() => wallPainter.SendMessage("ResetWalls"));

                  if (screenshotButton != null)
                        screenshotButton.onClick.AddListener(TakeScreenshot);

                  // Настраиваем слайдер размера кисти
                  if (brushSizeSlider != null && wallPainter != null)
                  {
                        brushSizeSlider.onValueChanged.AddListener(UpdateBrushSize);
                        // Обновляем текст с размером кисти
                        UpdateBrushSizeText(brushSizeSlider.value);
                  }
            }

            /// <summary>
            /// Создает кнопки выбора цвета на основе доступных материалов
            /// </summary>
            private void CreateColorButtons()
            {
                  if (colorButtonsContainer == null || colorButtonPrefab == null)
                        return;

                  // Удаляем существующие кнопки (если есть)
                  foreach (Transform child in colorButtonsContainer)
                  {
                        Destroy(child.gameObject);
                  }

                  // Сначала находим ARPlaneSetup и инициализируем его
                  var planeSetup = UnityEngine.Object.FindFirstObjectByType<Remalux.AR.ARPlaneSetup>();
                  if (planeSetup == null)
                  {
                        // Если компонент не найден, создаем новый GameObject с этим компонентом
                        GameObject planeSetupObj = new GameObject("AR Plane Setup");
                        planeSetup = planeSetupObj.AddComponent<Remalux.AR.ARPlaneSetup>();
                        Debug.Log("Создан новый компонент ARPlaneSetup");
                  }

                  // Инициализируем компонент, что решит проблему материала из пакета
                  planeSetup.Initialize();

                  // Создаем кнопки для каждого доступного материала
                  for (int i = 0; i < paintMaterials.Count; i++)
                  {
                        GameObject buttonObj = Instantiate(colorButtonPrefab, colorButtonsContainer);
                        Button button = buttonObj.GetComponent<Button>();

                        if (button != null)
                        {
                              // Устанавливаем цвет кнопки
                              Image buttonImage = button.GetComponent<Image>();
                              if (buttonImage != null && paintMaterials[i] != null)
                              {
                                    buttonImage.color = paintMaterials[i].color;
                              }

                              // Добавляем обработчик события для выбора цвета
                              int colorIndex = i; // Создаем локальную копию для замыкания
                              button.onClick.AddListener(() => SelectColor(colorIndex));
                        }
                  }
            }

            /// <summary>
            /// Выбирает цвет для покраски
            /// </summary>
            private void SelectColor(int colorIndex)
            {
                  if (wallPainter != null)
                  {
                        // Отправляем сообщение WallPainter для выбора цвета
                        wallPainter.SendMessage("SelectColor", colorIndex, SendMessageOptions.DontRequireReceiver);

                        // Возвращаемся на главную панель
                        ShowPanel(mainPanel);
                  }
            }

            /// <summary>
            /// Обновляет размер кисти
            /// </summary>
            private void UpdateBrushSize(float size)
            {
                  if (wallPainter != null)
                  {
                        // Отправляем сообщение WallPainter для изменения размера кисти
                        wallPainter.SendMessage("SetBrushSize", size, SendMessageOptions.DontRequireReceiver);

                        // Обновляем текст с размером кисти
                        UpdateBrushSizeText(size);
                  }
            }

            /// <summary>
            /// Обновляет текст с размером кисти
            /// </summary>
            private void UpdateBrushSizeText(float size)
            {
                  if (brushSizeText != null)
                  {
                        brushSizeText.text = $"Размер: {size:F2}";
                  }
            }

            /// <summary>
            /// Делает скриншот текущего окрашенного помещения
            /// </summary>
            private void TakeScreenshot()
            {
                  if (wallPainter != null)
                  {
                        // Временно скрываем UI
                        SetUIVisibility(false);

                        // Даем время для обновления кадра без UI
                        StartCoroutine(CaptureScreenshotAfterFrame());
                  }
            }

            /// <summary>
            /// Делает скриншот после обновления кадра
            /// </summary>
            private System.Collections.IEnumerator CaptureScreenshotAfterFrame()
            {
                  // Ждем следующего кадра
                  yield return new WaitForEndOfFrame();

                  // Отправляем сообщение WallPainter для создания скриншота
                  wallPainter.SendMessage("TakeScreenshot", SendMessageOptions.DontRequireReceiver);

                  // Показываем UI снова
                  SetUIVisibility(true);
            }

            /// <summary>
            /// Устанавливает видимость всех UI элементов
            /// </summary>
            private void SetUIVisibility(bool visible)
            {
                  if (mainPanel != null) mainPanel.SetActive(visible);
                  if (colorPalettePanel != null) colorPalettePanel.SetActive(false);
                  if (brushSettingsPanel != null) brushSettingsPanel.SetActive(false);
                  if (helpPanel != null) helpPanel.SetActive(false);
            }

            /// <summary>
            /// Показывает указанную панель и скрывает другие
            /// </summary>
            private void ShowPanel(GameObject panel)
            {
                  // Скрываем все панели
                  if (mainPanel != null) mainPanel.SetActive(false);
                  if (colorPalettePanel != null) colorPalettePanel.SetActive(false);
                  if (brushSettingsPanel != null) brushSettingsPanel.SetActive(false);
                  if (helpPanel != null) helpPanel.SetActive(false);

                  // Показываем выбранную панель
                  if (panel != null)
                  {
                        panel.SetActive(true);
                  }
            }

            /// <summary>
            /// Создает панели для рисования на поверхностях
            /// </summary>
            public void CreatePaintableSurfaces()
            {
                  ARPlaneVisibilityController planeController = FindFirstObjectByType<ARPlaneVisibilityController>();
                  if (planeController != null)
                  {
                        planeController.CreateDemoPaintableSurfaces();
                        Debug.Log("Созданы панели для рисования");
                  }
                  else
                  {
                        Debug.LogError("Не найден ARPlaneVisibilityController!");
                  }
            }

            /// <summary>
            /// Создать панель для рисования в указанной точке экрана
            /// </summary>
            public void CreatePaintableSurfaceAtPoint(Vector2 screenPosition)
            {
                  ARPlaneVisibilityController planeController = FindFirstObjectByType<ARPlaneVisibilityController>();
                  if (planeController != null)
                  {
                        GameObject panel = planeController.CreatePaintableSurfaceAtScreenPoint(screenPosition);
                        if (panel != null)
                        {
                              Debug.Log($"Создана панель для рисования в точке {screenPosition}");
                        }
                        else
                        {
                              Debug.LogError("Не удалось создать панель для рисования!");
                        }
                  }
                  else
                  {
                        Debug.LogError("Не найден ARPlaneVisibilityController!");
                  }
            }
      }
}