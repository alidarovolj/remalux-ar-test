using UnityEngine;
using System;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UtilsModule;

namespace Remalux.AR
{
      public class WallPainter : MonoBehaviour
      {
            [Header("Основные настройки")]
            public Camera mainCamera;
            public LayerMask wallLayerMask = 1 << 8; // Layer 8 = "Wall"
            public float maxRaycastDistance = 100f;

            [Header("Материалы")]
            public Material[] availableMaterials;
            public Material currentPaintMaterial;

            [Header("OpenCV настройки")]
            public int webCamIndex = 1; // 0 = встроенная камера, 1 = внешняя камера
            public int webCamWidth = 1280;
            public int webCamHeight = 720;
            public int webCamFPS = 30;
            public bool processEveryFrame = false; // Если false, обработка только при касании

            [Header("Отладка")]
            public bool showDebugInfo = false;
            public bool logRaycastHits = false;
            public bool showProcessedImage = false;

            public WebCamTexture webCamTexture { get; private set; }
            public Texture2D processedTexture { get; private set; }

            private Mat rgbMat;
            private Mat grayMat;
            private Mat binaryMat;
            private Mat contoursMat;
            private bool isInitialized = false;

            private void Start()
            {
                  Initialize();
            }

            private void Initialize()
            {
                  // Проверяем и инициализируем камеру
                  if (mainCamera == null)
                  {
                        mainCamera = Camera.main;
                        if (mainCamera == null)
                        {
                              Debug.LogError("WallPainter: Не удалось найти камеру. Компонент будет отключен.");
                              enabled = false;
                              return;
                        }
                  }

                  // Проверяем материалы
                  if (availableMaterials == null || availableMaterials.Length == 0)
                  {
                        Debug.LogWarning("WallPainter: Не заданы доступные материалы.");
                  }
                  else if (currentPaintMaterial == null)
                  {
                        currentPaintMaterial = availableMaterials[0];
                        Debug.Log($"WallPainter: Установлен материал по умолчанию - {currentPaintMaterial.name}");
                  }

                  // Проверяем маску слоя
                  if (wallLayerMask.value == 0)
                  {
                        wallLayerMask = 1 << 8; // Устанавливаем слой "Wall" по умолчанию
                        Debug.Log("WallPainter: Установлена маска слоя по умолчанию (Wall)");
                  }

                  InitializeWebCam();
            }

            private void InitializeWebCam()
            {
                  try
                  {
                        // Получаем список доступных камер
                        WebCamDevice[] devices = WebCamTexture.devices;
                        if (devices.Length == 0)
                        {
                              Debug.LogError("WallPainter: Камеры не найдены");
                              return;
                        }

                        // Выводим информацию о доступных камерах
                        for (int i = 0; i < devices.Length; i++)
                        {
                              Debug.Log($"Камера {i}: {devices[i].name} (фронтальная: {devices[i].isFrontFacing})");
                        }

                        // Проверяем индекс камеры
                        if (webCamIndex >= devices.Length)
                        {
                              Debug.LogWarning($"WallPainter: Камера с индексом {webCamIndex} не найдена, использую камеру 0");
                              webCamIndex = 0;
                        }

                        // Создаем WebCamTexture
                        webCamTexture = new WebCamTexture(devices[webCamIndex].name, webCamWidth, webCamHeight, webCamFPS);
                        webCamTexture.Play();

                        // Ждем инициализации камеры
                        while (webCamTexture.width <= 16)
                        {
                              Debug.Log("Ожидание инициализации камеры...");
                              System.Threading.Thread.Sleep(100);
                        }

                        // Инициализируем матрицы OpenCV
                        rgbMat = new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC3);
                        grayMat = new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC1);
                        binaryMat = new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC1);
                        contoursMat = new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC3);

                        // Создаем текстуру для отображения результатов
                        processedTexture = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);

                        Debug.Log($"WallPainter: Камера инициализирована. Разрешение: {webCamTexture.width}x{webCamTexture.height}, FPS: {webCamFPS}");
                        isInitialized = true;
                  }
                  catch (Exception e)
                  {
                        Debug.LogError($"WallPainter: Ошибка при инициализации камеры: {e.Message}");
                        enabled = false;
                  }
            }

            private void Update()
            {
                  if (!isInitialized)
                        return;

                  // Обновляем изображение с камеры
                  Utils.webCamTextureToMat(webCamTexture, rgbMat);

                  // Если нужно обрабатывать каждый кадр или есть касание
                  if (processEveryFrame || Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                  {
                        ProcessFrame();
                  }

                  HandleInput();
            }

            private void ProcessFrame()
            {
                  try
                  {
                        // Конвертируем в оттенки серого
                        Imgproc.cvtColor(rgbMat, grayMat, Imgproc.COLOR_RGB2GRAY);

                        // Применяем пороговое значение для выделения стен
                        Imgproc.threshold(grayMat, binaryMat, 127, 255, Imgproc.THRESH_BINARY);

                        if (showProcessedImage)
                        {
                              // Копируем исходное изображение
                              rgbMat.copyTo(contoursMat);

                              // Находим контуры
                              using (Mat hierarchy = new Mat())
                              {
                                    System.Collections.Generic.List<MatOfPoint> contours = new System.Collections.Generic.List<MatOfPoint>();
                                    Imgproc.findContours(binaryMat, contours, hierarchy, Imgproc.RETR_EXTERNAL, Imgproc.CHAIN_APPROX_SIMPLE);

                                    // Рисуем контуры
                                    for (int i = 0; i < contours.Count; i++)
                                    {
                                          Scalar color = new Scalar(0, 255, 0);
                                          Imgproc.drawContours(contoursMat, contours, i, color, 2);
                                    }
                              }

                              // Обновляем текстуру
                              Utils.matToTexture2D(contoursMat, processedTexture);
                        }
                  }
                  catch (Exception e)
                  {
                        Debug.LogError($"WallPainter: Ошибка при обработке кадра: {e.Message}");
                  }
            }

            private void HandleInput()
            {
                  // Обработка касания/клика
                  bool shouldPaint = false;
                  Vector2 paintPosition = Vector2.zero;

                  // Проверяем касание на мобильных устройствах
                  if (Input.touchCount > 0)
                  {
                        Touch touch = Input.GetTouch(0);
                        if (touch.phase == TouchPhase.Began)
                        {
                              shouldPaint = true;
                              paintPosition = touch.position;
                        }
                  }
                  // Проверяем клик мыши на десктопе
                  else if (Input.GetMouseButtonDown(0))
                  {
                        shouldPaint = true;
                        paintPosition = Input.mousePosition;
                  }

                  // Если есть ввод - пытаемся покрасить
                  if (shouldPaint)
                  {
                        PaintWallAtPosition(paintPosition);
                  }
            }

            public void PaintWallAtPosition(Vector2 screenPosition)
            {
                  if (!isInitialized || mainCamera == null || currentPaintMaterial == null)
                        return;

                  Ray ray = mainCamera.ScreenPointToRay(screenPosition);
                  RaycastHit hit;

                  if (Physics.Raycast(ray, out hit, maxRaycastDistance, wallLayerMask))
                  {
                        if (logRaycastHits)
                        {
                              Debug.Log($"WallPainter: Попадание в {hit.collider.gameObject.name} на расстоянии {hit.distance}");
                        }

                        HandleWallHit(hit);
                  }
                  else if (logRaycastHits)
                  {
                        Debug.Log("WallPainter: Рейкаст не попал в стену");
                  }
            }

            private void HandleWallHit(RaycastHit hit)
            {
                  // Получаем компонент Renderer
                  Renderer renderer = hit.collider.GetComponent<Renderer>();
                  if (renderer == null)
                  {
                        Debug.LogWarning($"WallPainter: Объект {hit.collider.gameObject.name} не имеет компонента Renderer");
                        return;
                  }

                  try
                  {
                        // Проверяем, есть ли компонент для отслеживания материалов
                        WallMaterialInstanceTracker tracker = hit.collider.GetComponent<WallMaterialInstanceTracker>();
                        if (tracker != null)
                        {
                              // Создаем экземпляр материала
                              Material instancedMaterial = new Material(currentPaintMaterial);
                              // Применяем материал через новый метод
                              tracker.SetInstancedMaterial(instancedMaterial, true);
                        }
                        else
                        {
                              // Создаем экземпляр материала
                              Material instancedMaterial = new Material(currentPaintMaterial);
                              instancedMaterial.name = $"{currentPaintMaterial.name}_Instance_{hit.collider.gameObject.name}";

                              // Применяем экземпляр материала
                              renderer.material = instancedMaterial;

                              // Добавляем компонент для отслеживания
                              tracker = hit.collider.gameObject.AddComponent<WallMaterialInstanceTracker>();
                              tracker.OriginalSharedMaterial = renderer.sharedMaterial;
                              tracker.SetInstancedMaterial(instancedMaterial, true);
                        }

                        if (logRaycastHits)
                        {
                              Debug.Log($"WallPainter: Материал {currentPaintMaterial.name} применен к {hit.collider.gameObject.name}");
                        }
                  }
                  catch (Exception e)
                  {
                        Debug.LogError($"WallPainter: Ошибка при применении материала: {e.Message}");
                  }
            }

            public void SetPaintMaterial(Material material)
            {
                  if (material == null)
                  {
                        Debug.LogWarning("WallPainter: Попытка установить null в качестве материала");
                        return;
                  }

                  currentPaintMaterial = material;
                  if (logRaycastHits)
                  {
                        Debug.Log($"WallPainter: Установлен новый материал - {material.name}");
                  }
            }

            public void SetPaintMaterialByIndex(int index)
            {
                  if (availableMaterials == null || availableMaterials.Length == 0)
                  {
                        Debug.LogWarning("WallPainter: Нет доступных материалов");
                        return;
                  }

                  if (index < 0 || index >= availableMaterials.Length)
                  {
                        Debug.LogWarning($"WallPainter: Индекс {index} выходит за пределы массива материалов");
                        return;
                  }

                  SetPaintMaterial(availableMaterials[index]);
            }

            private void OnDestroy()
            {
                  if (webCamTexture != null)
                  {
                        webCamTexture.Stop();
                        webCamTexture = null;
                  }

                  if (rgbMat != null) rgbMat.Dispose();
                  if (grayMat != null) grayMat.Dispose();
                  if (binaryMat != null) binaryMat.Dispose();
                  if (contoursMat != null) contoursMat.Dispose();
            }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Проверяем камеру при изменении в инспекторе
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            // Проверяем текущий материал
            if (currentPaintMaterial == null && availableMaterials != null && availableMaterials.Length > 0)
            {
                currentPaintMaterial = availableMaterials[0];
            }

            // Проверяем маску слоя
            if (wallLayerMask.value == 0)
            {
                wallLayerMask = 1 << 8; // Wall layer
            }
        }
#endif
      }
}