using UnityEngine;
using UnityEngine.UI;

namespace Remalux.AR
{
      /// <summary>
      /// Компонент для управления настройками OpenCV через UI
      /// </summary>
      public class OpenCVSettingsPanel : MonoBehaviour
      {
            [Header("UI References")]
            [SerializeField] private Slider cannyThreshold1Slider;
            [SerializeField] private Slider cannyThreshold2Slider;
            [SerializeField] private Slider minWallAreaSlider;
            [SerializeField] private Slider maxWallAreaSlider;
            [SerializeField] private Slider approxPolyEpsilonSlider;

            [SerializeField] private Text cannyThreshold1Text;
            [SerializeField] private Text cannyThreshold2Text;
            [SerializeField] private Text minWallAreaText;
            [SerializeField] private Text maxWallAreaText;
            [SerializeField] private Text approxPolyEpsilonText;

            [Header("References")]
            [SerializeField] private OpenCVWallDetector wallDetector;

            [Header("Default Values")]
            [SerializeField] private int defaultCannyThreshold1 = 50;
            [SerializeField] private int defaultCannyThreshold2 = 150;
            [SerializeField] private float defaultMinWallArea = 5000f;
            [SerializeField] private float defaultMaxWallArea = 100000f;
            [SerializeField] private float defaultApproxPolyEpsilon = 15f;

            [Header("Settings")]
            [SerializeField] private bool applyOnStart = true;

            private void Awake()
            {
                  if (wallDetector == null)
                        wallDetector = UnityEngine.Object.FindFirstObjectByType<OpenCVWallDetector>();
            }

            private void Start()
            {
                  InitializeUI();

                  if (applyOnStart)
                  {
                        ResetToDefaults();
                  }
            }

            /// <summary>
            /// Инициализирует UI элементы и подписывается на события
            /// </summary>
            private void InitializeUI()
            {
                  if (cannyThreshold1Slider != null)
                        cannyThreshold1Slider.onValueChanged.AddListener(OnCannyThreshold1Changed);

                  if (cannyThreshold2Slider != null)
                        cannyThreshold2Slider.onValueChanged.AddListener(OnCannyThreshold2Changed);

                  if (minWallAreaSlider != null)
                        minWallAreaSlider.onValueChanged.AddListener(OnMinWallAreaChanged);

                  if (maxWallAreaSlider != null)
                        maxWallAreaSlider.onValueChanged.AddListener(OnMaxWallAreaChanged);

                  if (approxPolyEpsilonSlider != null)
                        approxPolyEpsilonSlider.onValueChanged.AddListener(OnApproxPolyEpsilonChanged);
            }

            /// <summary>
            /// Сбрасывает настройки к значениям по умолчанию
            /// </summary>
            public void ResetToDefaults()
            {
                  if (cannyThreshold1Slider != null)
                        cannyThreshold1Slider.value = defaultCannyThreshold1;

                  if (cannyThreshold2Slider != null)
                        cannyThreshold2Slider.value = defaultCannyThreshold2;

                  if (minWallAreaSlider != null)
                        minWallAreaSlider.value = defaultMinWallArea;

                  if (maxWallAreaSlider != null)
                        maxWallAreaSlider.value = defaultMaxWallArea;

                  if (approxPolyEpsilonSlider != null)
                        approxPolyEpsilonSlider.value = defaultApproxPolyEpsilon;

                  ApplySettingsToDetector();
            }

            /// <summary>
            /// Обработчик изменения нижнего порога Canny
            /// </summary>
            private void OnCannyThreshold1Changed(float value)
            {
                  int intValue = Mathf.RoundToInt(value);
                  if (cannyThreshold1Text != null)
                        cannyThreshold1Text.text = $"Нижний порог: {intValue}";

                  ApplySettingsToDetector();
            }

            /// <summary>
            /// Обработчик изменения верхнего порога Canny
            /// </summary>
            private void OnCannyThreshold2Changed(float value)
            {
                  int intValue = Mathf.RoundToInt(value);
                  if (cannyThreshold2Text != null)
                        cannyThreshold2Text.text = $"Верхний порог: {intValue}";

                  ApplySettingsToDetector();
            }

            /// <summary>
            /// Обработчик изменения минимальной площади стены
            /// </summary>
            private void OnMinWallAreaChanged(float value)
            {
                  if (minWallAreaText != null)
                        minWallAreaText.text = $"Мин. площадь: {value:F0}";

                  ApplySettingsToDetector();
            }

            /// <summary>
            /// Обработчик изменения максимальной площади стены
            /// </summary>
            private void OnMaxWallAreaChanged(float value)
            {
                  if (maxWallAreaText != null)
                        maxWallAreaText.text = $"Макс. площадь: {value:F0}";

                  ApplySettingsToDetector();
            }

            /// <summary>
            /// Обработчик изменения точности аппроксимации контура
            /// </summary>
            private void OnApproxPolyEpsilonChanged(float value)
            {
                  if (approxPolyEpsilonText != null)
                        approxPolyEpsilonText.text = $"Точность: {value:F1}";

                  ApplySettingsToDetector();
            }

            /// <summary>
            /// Применяет текущие настройки к детектору стен
            /// </summary>
            private void ApplySettingsToDetector()
            {
                  if (wallDetector == null)
                        return;

                  // Передаем настройки в детектор через публичный метод
                  int cannyThreshold1 = cannyThreshold1Slider != null ? Mathf.RoundToInt(cannyThreshold1Slider.value) : defaultCannyThreshold1;
                  int cannyThreshold2 = cannyThreshold2Slider != null ? Mathf.RoundToInt(cannyThreshold2Slider.value) : defaultCannyThreshold2;
                  float minWallArea = minWallAreaSlider != null ? minWallAreaSlider.value : defaultMinWallArea;
                  float maxWallArea = maxWallAreaSlider != null ? maxWallAreaSlider.value : defaultMaxWallArea;
                  float approxPolyEpsilon = approxPolyEpsilonSlider != null ? approxPolyEpsilonSlider.value : defaultApproxPolyEpsilon;

                  // Вызываем метод в детекторе для установки параметров
                  wallDetector.SendMessage(
                      "SetDetectionParameters",
                      new DetectionParameters
                      {
                            CannyThreshold1 = cannyThreshold1,
                            CannyThreshold2 = cannyThreshold2,
                            MinWallArea = minWallArea,
                            MaxWallArea = maxWallArea,
                            ApproxPolyEpsilon = approxPolyEpsilon
                      },
                      SendMessageOptions.DontRequireReceiver);
            }
      }

      /// <summary>
      /// Структура для хранения параметров обнаружения стен
      /// </summary>
      [System.Serializable]
      public struct DetectionParameters
      {
            public int CannyThreshold1;
            public int CannyThreshold2;
            public float MinWallArea;
            public float MaxWallArea;
            public float ApproxPolyEpsilon;
      }
}