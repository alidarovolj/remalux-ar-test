using UnityEngine;
using UnityEngine.UI;

namespace Remalux.AR
{
      /// <summary>
      /// Компонент для отображения отладочной информации от OpenCV
      /// </summary>
      public class OpenCVDebugView : MonoBehaviour
      {
            [Header("UI References")]
            [SerializeField] private RawImage debugImage;
            [SerializeField] private GameObject debugPanel;
            [SerializeField] private Toggle showDebugToggle;

            [Header("References")]
            [SerializeField] private OpenCVWallDetector wallDetector;

            private Texture2D currentDebugTexture;

            private void Awake()
            {
                  if (wallDetector == null)
                        wallDetector = UnityEngine.Object.FindFirstObjectByType<OpenCVWallDetector>();

                  if (debugPanel != null)
                        debugPanel.SetActive(false);
            }

            private void Start()
            {
                  if (showDebugToggle != null)
                  {
                        showDebugToggle.onValueChanged.AddListener(OnToggleDebugView);
                  }
            }

            /// <summary>
            /// Переключает видимость отладочной панели
            /// </summary>
            public void OnToggleDebugView(bool isVisible)
            {
                  if (debugPanel != null)
                        debugPanel.SetActive(isVisible);
            }

            /// <summary>
            /// Устанавливает отладочную текстуру для отображения
            /// </summary>
            public void SetDebugTexture(Texture2D texture)
            {
                  if (debugImage != null && texture != null)
                  {
                        currentDebugTexture = texture;
                        debugImage.texture = currentDebugTexture;

                        // Настраиваем соотношение сторон
                        if (debugImage.rectTransform != null)
                        {
                              float aspect = (float)texture.width / texture.height;
                              Vector2 sizeDelta = debugImage.rectTransform.sizeDelta;
                              sizeDelta.x = sizeDelta.y * aspect;
                              debugImage.rectTransform.sizeDelta = sizeDelta;
                        }
                  }
            }

            /// <summary>
            /// Получает текущую отладочную текстуру
            /// </summary>
            public Texture2D GetDebugTexture()
            {
                  return currentDebugTexture;
            }
      }
}