using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System;
using System.IO;

namespace Remalux.WallPainting.Vision
{
      public class DeepLabSegmentation : MonoBehaviour
      {
            [Header("Model Settings")]
            [SerializeField] private string pythonServerAddress = "127.0.0.1";
            [SerializeField] private int pythonServerPort = 5000;
            [SerializeField] private float segmentationInterval = 0.1f;

            [Header("Debug")]
            [SerializeField] private bool showDebugInfo = true;
            public UnityEngine.UI.RawImage debugImage;

            private WebClient webClient;
            private bool isProcessing = false;
            private Texture2D resultTexture;

            private void Start()
            {
                  webClient = new WebClient();
                  resultTexture = new Texture2D(1, 1);
                  StartCoroutine(SegmentationLoop());
            }

            private IEnumerator SegmentationLoop()
            {
                  while (true)
                  {
                        if (!isProcessing)
                        {
                              StartSegmentation();
                        }
                        yield return new WaitForSeconds(segmentationInterval);
                  }
            }

            private async void StartSegmentation()
            {
                  if (isProcessing) return;
                  isProcessing = true;

                  try
                  {
                        // Получаем текущий кадр с камеры
                        byte[] imageBytes = CaptureScreenToBytes();

                        // Отправляем изображение на Python сервер
                        string url = $"http://{pythonServerAddress}:{pythonServerPort}/segment";
                        byte[] response = await webClient.UploadDataTaskAsync(url, imageBytes);

                        // Обрабатываем результат
                        ProcessSegmentationResult(response);
                  }
                  catch (Exception e)
                  {
                        Debug.LogError($"Segmentation error: {e.Message}");
                  }
                  finally
                  {
                        isProcessing = false;
                  }
            }

            private byte[] CaptureScreenToBytes()
            {
                  // Получаем текущий кадр с камеры
                  Camera camera = Camera.main;
                  RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24);
                  camera.targetTexture = rt;
                  Texture2D screenShot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                  camera.Render();
                  RenderTexture.active = rt;
                  screenShot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                  camera.targetTexture = null;
                  RenderTexture.active = null;
                  Destroy(rt);

                  // Конвертируем в bytes
                  return screenShot.EncodeToPNG();
            }

            private void ProcessSegmentationResult(byte[] segmentationData)
            {
                  // Конвертируем результат в текстуру
                  resultTexture.LoadImage(segmentationData);

                  if (showDebugInfo && debugImage != null)
                  {
                        debugImage.texture = resultTexture;
                  }

                  // Отправляем событие с результатами сегментации
                  OnSegmentationComplete(resultTexture);
            }

            // События для оповещения других компонентов о результатах сегментации
            public delegate void SegmentationHandler(Texture2D segmentationMask);
            public event SegmentationHandler OnSegmentationComplete;
      }
}