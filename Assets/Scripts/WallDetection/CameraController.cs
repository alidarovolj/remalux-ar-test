using UnityEngine;
using UnityEngine.UI;

namespace Remalux.WallDetection
{
      public class CameraController : MonoBehaviour
      {
            [Header("Components")]
            [SerializeField] private WallDetector wallDetector;
            [SerializeField] private RawImage displayImage;
            [SerializeField] private AspectRatioFitter aspectRatioFitter;

            private WebCamTexture webCamTexture;
            private Texture2D processedFrame;
            private bool isCameraInitialized = false;

            private void Start()
            {
                  InitializeCamera();
            }

            private void InitializeCamera()
            {
                  // Получаем список доступных камер
                  WebCamDevice[] devices = WebCamTexture.devices;
                  if (devices.Length == 0)
                  {
                        Debug.LogError("Камера не найдена!");
                        return;
                  }

                  // Используем заднюю камеру на мобильных устройствах
                  string deviceName = "";
                  for (int i = 0; i < devices.Length; i++)
                  {
                        if (!devices[i].isFrontFacing)
                        {
                              deviceName = devices[i].name;
                              break;
                        }
                  }

                  // Если задняя камера не найдена, используем первую доступную
                  if (string.IsNullOrEmpty(deviceName))
                  {
                        deviceName = devices[0].name;
                  }

                  // Создаем текстуру для камеры
                  webCamTexture = new WebCamTexture(deviceName, 1280, 720, 30);
                  webCamTexture.Play();

                  // Ждем инициализации камеры
                  while (webCamTexture.width <= 16)
                  {
                        Debug.Log("Waiting for camera to initialize...");
                        System.Threading.Thread.Sleep(100);
                  }

                  // Настраиваем отображение
                  if (displayImage != null)
                  {
                        displayImage.texture = webCamTexture;

                        if (aspectRatioFitter != null)
                        {
                              aspectRatioFitter.aspectRatio = (float)webCamTexture.width / webCamTexture.height;
                        }
                  }

                  // Создаем текстуру для обработанного кадра
                  processedFrame = new Texture2D(webCamTexture.width, webCamTexture.height,
                      TextureFormat.RGBA32, false);

                  isCameraInitialized = true;
                  Debug.Log($"Camera initialized: {webCamTexture.width}x{webCamTexture.height} @ {webCamTexture.requestedFPS}fps");
            }

            private void Update()
            {
                  if (!isCameraInitialized || webCamTexture == null || !webCamTexture.isPlaying)
                        return;

                  // Копируем кадр из камеры
                  processedFrame.SetPixels(webCamTexture.GetPixels());
                  processedFrame.Apply();

                  // Отправляем кадр на обработку
                  if (wallDetector != null)
                  {
                        wallDetector.ProcessFrame(processedFrame);
                  }
            }

            private void OnDestroy()
            {
                  if (webCamTexture != null)
                  {
                        webCamTexture.Stop();
                  }

                  if (processedFrame != null)
                  {
                        Destroy(processedFrame);
                  }
            }
      }
}