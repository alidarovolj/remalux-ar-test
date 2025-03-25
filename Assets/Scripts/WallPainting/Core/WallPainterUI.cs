using UnityEngine;
using UnityEngine.UI;

namespace Remalux.AR
{
      [RequireComponent(typeof(WallPainter))]
      public class WallPainterUI : MonoBehaviour
      {
            [Header("UI элементы")]
            public RawImage cameraFeedImage;
            public RawImage processedImage;
            public GameObject debugPanel;
            public Text fpsText;
            public Text resolutionText;

            private WallPainter wallPainter;
            private float deltaTime = 0.0f;

            private void Start()
            {
                  wallPainter = GetComponent<WallPainter>();
                  if (wallPainter == null)
                  {
                        Debug.LogError("WallPainterUI: Не найден компонент WallPainter");
                        enabled = false;
                        return;
                  }

                  // Настраиваем отображение UI элементов
                  if (debugPanel != null)
                        debugPanel.SetActive(wallPainter.showDebugInfo);

                  if (processedImage != null)
                        processedImage.gameObject.SetActive(wallPainter.showProcessedImage);
            }

            private void Update()
            {
                  // Обновляем FPS
                  deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
                  float fps = 1.0f / deltaTime;

                  if (fpsText != null)
                        fpsText.text = $"FPS: {Mathf.Round(fps)}";

                  // Обновляем информацию о разрешении
                  if (resolutionText != null && wallPainter.webCamTexture != null)
                  {
                        resolutionText.text = $"Разрешение: {wallPainter.webCamTexture.width}x{wallPainter.webCamTexture.height}";
                  }

                  // Обновляем изображение с камеры
                  if (cameraFeedImage != null && wallPainter.webCamTexture != null)
                  {
                        cameraFeedImage.texture = wallPainter.webCamTexture;
                  }

                  // Обновляем обработанное изображение
                  if (processedImage != null && wallPainter.processedTexture != null)
                  {
                        processedImage.texture = wallPainter.processedTexture;
                        processedImage.gameObject.SetActive(wallPainter.showProcessedImage);
                  }

                  // Обновляем видимость отладочной панели
                  if (debugPanel != null)
                        debugPanel.SetActive(wallPainter.showDebugInfo);
            }
      }
}