using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UtilsModule;

namespace Remalux.AR.Vision
{
    /// <summary>
    /// Компонент для обнаружения стен с использованием OpenCV
    /// </summary>
    public class WallDetector : MonoBehaviour
    {
        [Header("Настройки камеры")]
        [SerializeField] private bool useWebcam = true;
        [SerializeField] private int webcamDeviceIndex = 0;
        [SerializeField] private Vector2Int webcamResolution = new Vector2Int(1280, 720);
        [SerializeField] private Texture2D inputTexture;
        
        [Header("Настройки обнаружения")]
        [SerializeField] private int cannyThreshold1 = 50;
        [SerializeField] private int cannyThreshold2 = 150;
        [SerializeField] private float minWallWidth = 0.5f;
        [SerializeField] private float minWallHeight = 1.0f;
        [SerializeField] private float verticalAngleThreshold = 10f; // Угол отклонения от вертикали (в градусах)
        [SerializeField] private float horizontalAngleThreshold = 10f; // Угол отклонения от горизонтали (в градусах)
        [SerializeField] private float lineGroupingThreshold = 20f; // Порог для группировки линий (в пикселях)
        
        [Header("Отладка")]
        [SerializeField] private bool showDebugVisuals = true;
        
        private WebCamTexture webcamTexture;
        private Mat rgbaMat;
        private Mat grayMat;
        private Mat cannyMat;
        private Mat linesMat;
        private MatOfInt4 linesHough;
        private Texture2D processedTexture;
        
        private RawImage debugImageDisplay;
        
        private bool isDetectionRunning = false;
        private bool isInitialized = false;
        
        /// <summary>
        /// Структура для хранения данных о стене
        /// </summary>
        public struct WallData
        {
            public Vector2 topLeft;
            public Vector2 topRight;
            public Vector2 bottomLeft;
            public Vector2 bottomRight;
            public float width;
            public float height;
            public string id; // Уникальный идентификатор стены
            
            public WallData(Vector2 tl, Vector2 tr, Vector2 bl, Vector2 br)
            {
                topLeft = tl;
                topRight = tr;
                bottomLeft = bl;
                bottomRight = br;
                width = Vector2.Distance(topLeft, topRight);
                height = Vector2.Distance(topLeft, bottomLeft);
                id = Guid.NewGuid().ToString();
            }
        }
        
        public delegate void WallDetectionEvent(List<WallData> walls);
        public event WallDetectionEvent OnWallsDetected;
        
        private void Start()
        {
            InitializeOpenCV();
        }
        
        private void OnDestroy()
        {
            ReleaseResources();
        }
        
        /// <summary>
        /// Устанавливает компонент для отображения отладочного изображения
        /// </summary>
        /// <param name="display">RawImage компонент для отображения отладочного изображения</param>
        public void SetDebugImageDisplay(RawImage display)
        {
            debugImageDisplay = display;
        }
        
        /// <summary>
        /// Запускает процесс обнаружения стен
        /// </summary>
        public void StartDetection()
        {
            if (!isInitialized)
            {
                InitializeOpenCV();
            }
            
            if (useWebcam)
            {
                // Пытаемся получить доступ к веб-камере
                WebCamDevice[] devices = WebCamTexture.devices;
                if (devices.Length > 0)
                {
                    webcamTexture = new WebCamTexture(devices[0].name, 1280, 720);
                    webcamTexture.Play();
                }
                else
                {
                    Debug.LogWarning("Веб-камера не обнаружена. Используем тестовое изображение.");
                    useWebcam = false;
                    inputTexture = CreateTestImage(1280, 720);
                }
            }
            else if (inputTexture == null)
            {
                // Если нет входного изображения, создаем тестовое
                inputTexture = CreateTestImage(1280, 720);
            }
            
            isDetectionRunning = true;
        }
        
        /// <summary>
        /// Останавливает процесс обнаружения стен
        /// </summary>
        public void StopDetection()
        {
            isDetectionRunning = false;
            
            if (useWebcam && webcamTexture != null && webcamTexture.isPlaying)
            {
                webcamTexture.Stop();
            }
            
            Debug.Log("Остановлен процесс обнаружения стен");
        }
        
        /// <summary>
        /// Инициализирует OpenCV и подготавливает ресурсы
        /// </summary>
        private void InitializeOpenCV()
        {
            if (isInitialized)
                return;
                
            try
            {
                if (useWebcam)
                {
                    // Инициализация веб-камеры
                    WebCamDevice[] devices = WebCamTexture.devices;
                    if (devices.Length == 0)
                    {
                        Debug.LogWarning("Не найдены устройства камеры. Переключаемся на тестовое изображение.");
                        useWebcam = false;
                        
                        // Если нет тестового изображения, создаем его
                        if (inputTexture == null)
                        {
                            inputTexture = CreateTestImage(webcamResolution.x, webcamResolution.y);
                        }
                    }
                    else
                    {
                        int deviceIndex = Mathf.Clamp(webcamDeviceIndex, 0, devices.Length - 1);
                        webcamTexture = new WebCamTexture(devices[deviceIndex].name, webcamResolution.x, webcamResolution.y);
                        webcamTexture.Play();
                    }
                }
                else if (inputTexture == null)
                {
                    // Если не используем веб-камеру и нет входного изображения, создаем тестовое
                    inputTexture = CreateTestImage(webcamResolution.x, webcamResolution.y);
                }
                
                // Инициализация матриц OpenCV
                rgbaMat = new Mat();
                grayMat = new Mat();
                cannyMat = new Mat();
                linesMat = new Mat();
                linesHough = new MatOfInt4();
                
                // Создаем текстуру для отображения результатов обработки
                processedTexture = new Texture2D(
                    useWebcam && webcamTexture != null ? webcamTexture.width : webcamResolution.x,
                    useWebcam && webcamTexture != null ? webcamTexture.height : webcamResolution.y,
                    TextureFormat.RGBA32, false);
                
                isInitialized = true;
            }
            catch (System.Exception e)
            {
                Debug.LogError("Ошибка инициализации OpenCV: " + e.Message);
            }
        }
        
        /// <summary>
        /// Создает тестовое изображение с простыми стенами для демонстрации
        /// </summary>
        private Texture2D CreateTestImage(int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] colors = new Color[width * height];
            
            // Заполняем фон светло-серым цветом
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = new Color(0.9f, 0.9f, 0.9f);
            }
            
            // Рисуем простую стену (вертикальные линии)
            int wallLeft = width / 4;
            int wallRight = width * 3 / 4;
            int wallTop = height / 6;
            int wallBottom = height * 5 / 6;
            
            // Рисуем границы стены
            for (int y = wallTop; y < wallBottom; y++)
            {
                // Левая граница
                colors[y * width + wallLeft] = Color.black;
                colors[y * width + wallLeft + 1] = Color.black;
                
                // Правая граница
                colors[y * width + wallRight] = Color.black;
                colors[y * width + wallRight - 1] = Color.black;
            }
            
            for (int x = wallLeft; x < wallRight; x++)
            {
                // Верхняя граница
                colors[wallTop * width + x] = Color.black;
                colors[(wallTop + 1) * width + x] = Color.black;
                
                // Нижняя граница
                colors[wallBottom * width + x] = Color.black;
                colors[(wallBottom - 1) * width + x] = Color.black;
            }
            
            texture.SetPixels(colors);
            texture.Apply();
            
            return texture;
        }
        
        /// <summary>
        /// Освобождает ресурсы OpenCV
        /// </summary>
        private void ReleaseResources()
        {
            if (webcamTexture != null)
            {
                webcamTexture.Stop();
                webcamTexture = null;
            }
            
            if (rgbaMat != null)
                rgbaMat.Dispose();
            
            if (grayMat != null)
                grayMat.Dispose();
            
            if (cannyMat != null)
                cannyMat.Dispose();
            
            if (linesMat != null)
                linesMat.Dispose();
            
            if (linesHough != null)
                linesHough.Dispose();
            
            isInitialized = false;
        }
        
        private void Update()
        {
            if (!isDetectionRunning || !isInitialized)
                return;
            
            try
            {
                // Получаем текущий кадр
                if (useWebcam && webcamTexture != null && webcamTexture.isPlaying)
                {
                    // Обработка кадра с веб-камеры
                    Utils.webCamTextureToMat(webcamTexture, rgbaMat);
                }
                else if (inputTexture != null)
                {
                    // Обработка статического изображения
                    Utils.texture2DToMat(inputTexture, rgbaMat);
                }
                else
                {
                    // Если нет ни веб-камеры, ни входного изображения, выходим
                    return;
                }
                
                // Обработка изображения для обнаружения стен
                ProcessFrame();
                
                // Обновляем отладочное отображение
                if (showDebugVisuals && debugImageDisplay != null && processedTexture != null)
                {
                    debugImageDisplay.texture = processedTexture;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Ошибка обработки кадра: " + e.Message);
            }
        }
        
        /// <summary>
        /// Обрабатывает текущий кадр для обнаружения линий
        /// </summary>
        private void ProcessFrame()
        {
            // Конвертируем в оттенки серого
            Imgproc.cvtColor(rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
            
            // Применяем размытие для уменьшения шума
            Imgproc.GaussianBlur(grayMat, grayMat, new Size(5, 5), 0);
            
            // Применяем детектор границ Canny
            Imgproc.Canny(grayMat, cannyMat, cannyThreshold1, cannyThreshold2);
            
            // Преобразуем cannyMat в цветное изображение для визуализации
            Imgproc.cvtColor(cannyMat, linesMat, Imgproc.COLOR_GRAY2RGBA);
            
            // Обнаруживаем линии с помощью преобразования Хафа
            Imgproc.HoughLinesP(cannyMat, linesHough, 1, Math.PI / 180, 50, 50, 10);
            
            // Визуализируем обнаруженные линии
            if (showDebugVisuals)
            {
                int[] linesArray = linesHough.toArray();
                for (int i = 0; i < linesArray.Length; i += 4)
                {
                    int x1 = linesArray[i];
                    int y1 = linesArray[i + 1];
                    int x2 = linesArray[i + 2];
                    int y2 = linesArray[i + 3];
                    
                    Point pt1 = new Point(x1, y1);
                    Point pt2 = new Point(x2, y2);
                    
                    // Классифицируем линию как вертикальную или горизонтальную
                    double angle = Math.Atan2(y2 - y1, x2 - x1) * 180 / Math.PI;
                    angle = (angle < 0) ? angle + 180 : angle;
                    
                    Scalar lineColor;
                    if (IsVerticalLine(angle))
                    {
                        lineColor = new Scalar(255, 0, 0, 255); // Красный для вертикальных линий
                    }
                    else if (IsHorizontalLine(angle))
                    {
                        lineColor = new Scalar(0, 255, 0, 255); // Зеленый для горизонтальных линий
                    }
                    else
                    {
                        lineColor = new Scalar(0, 0, 255, 255); // Синий для других линий
                    }
                    
                    Imgproc.line(linesMat, pt1, pt2, lineColor, 2);
                }
            }
            
            // Обновляем текстуру для отображения
            if (showDebugVisuals)
            {
                if (processedTexture == null || processedTexture.width != linesMat.width() || processedTexture.height != linesMat.height())
                {
                    if (processedTexture != null)
                        Destroy(processedTexture);
                    
                    processedTexture = new Texture2D(linesMat.width(), linesMat.height(), TextureFormat.RGBA32, false);
                }
                
                Utils.matToTexture2D(linesMat, processedTexture);
            }
            
            // Обнаруживаем стены
            List<WallData> detectedWalls = DetectWalls();
            
            // Вызываем событие обнаружения стен
            if (detectedWalls.Count > 0 && OnWallsDetected != null)
            {
                OnWallsDetected.Invoke(detectedWalls);
            }
        }
        
        /// <summary>
        /// Обнаруживает стены на основе обнаруженных линий
        /// </summary>
        private List<WallData> DetectWalls()
        {
            List<WallData> walls = new List<WallData>();
            
            // Получаем линии из преобразования Хафа
            int[] linesArray = linesHough.toArray();
            
            // Разделяем линии на вертикальные и горизонтальные
            List<Vector4> verticalLines = new List<Vector4>();
            List<Vector4> horizontalLines = new List<Vector4>();
            
            for (int i = 0; i < linesArray.Length; i += 4)
            {
                int x1 = linesArray[i];
                int y1 = linesArray[i + 1];
                int x2 = linesArray[i + 2];
                int y2 = linesArray[i + 3];
                
                double angle = Math.Atan2(y2 - y1, x2 - x1) * 180 / Math.PI;
                angle = (angle < 0) ? angle + 180 : angle;
                
                if (IsVerticalLine(angle))
                {
                    verticalLines.Add(new Vector4(x1, y1, x2, y2));
                }
                else if (IsHorizontalLine(angle))
                {
                    horizontalLines.Add(new Vector4(x1, y1, x2, y2));
                }
            }
            
            // Группируем близкие вертикальные линии
            List<Vector4> groupedVerticalLines = GroupLines(verticalLines, true);
            
            // Группируем близкие горизонтальные линии
            List<Vector4> groupedHorizontalLines = GroupLines(horizontalLines, false);
            
            // Визуализируем сгруппированные линии
            if (showDebugVisuals)
            {
                foreach (Vector4 line in groupedVerticalLines)
                {
                    Point pt1 = new Point(line.x, line.y);
                    Point pt2 = new Point(line.z, line.w);
                    Imgproc.line(linesMat, pt1, pt2, new Scalar(255, 255, 0, 255), 3); // Желтый для сгруппированных вертикальных линий
                }
                
                foreach (Vector4 line in groupedHorizontalLines)
                {
                    Point pt1 = new Point(line.x, line.y);
                    Point pt2 = new Point(line.z, line.w);
                    Imgproc.line(linesMat, pt1, pt2, new Scalar(255, 0, 255, 255), 3); // Пурпурный для сгруппированных горизонтальных линий
                }
            }
            
            // Создаем кандидаты стен на основе пересечений линий
            for (int i = 0; i < groupedVerticalLines.Count - 1; i++)
            {
                for (int j = i + 1; j < groupedVerticalLines.Count; j++)
                {
                    Vector4 leftLine = groupedVerticalLines[i];
                    Vector4 rightLine = groupedVerticalLines[j];
                    
                    // Убедимся, что левая линия действительно левее правой
                    if (leftLine.x > rightLine.x)
                    {
                        Vector4 temp = leftLine;
                        leftLine = rightLine;
                        rightLine = temp;
                    }
                    
                    // Проверяем расстояние между линиями
                    float distance = Mathf.Abs(rightLine.x - leftLine.x);
                    if (distance < minWallWidth * 100) // Предполагаем, что 100 пикселей = 1 метр (примерно)
                        continue;
                    
                    // Ищем горизонтальные линии, которые могут быть верхней и нижней границами стены
                    Vector4? topLine = null;
                    Vector4? bottomLine = null;
                    
                    foreach (Vector4 hLine in groupedHorizontalLines)
                    {
                        // Проверяем, находится ли горизонтальная линия между вертикальными линиями
                        if (hLine.x <= leftLine.x && hLine.z >= rightLine.x)
                        {
                            if (topLine == null || hLine.y < topLine.Value.y)
                            {
                                topLine = hLine;
                            }
                            
                            if (bottomLine == null || hLine.y > bottomLine.Value.y)
                            {
                                bottomLine = hLine;
                            }
                        }
                    }
                    
                    // Если нашли верхнюю и нижнюю границы, создаем стену
                    if (topLine != null && bottomLine != null)
                    {
                        float wallHeight = Mathf.Abs(bottomLine.Value.y - topLine.Value.y);
                        if (wallHeight < minWallHeight * 100) // Предполагаем, что 100 пикселей = 1 метр (примерно)
                            continue;
                        
                        Vector2 topLeft = new Vector2(leftLine.x, topLine.Value.y);
                        Vector2 topRight = new Vector2(rightLine.x, topLine.Value.y);
                        Vector2 bottomLeft = new Vector2(leftLine.x, bottomLine.Value.y);
                        Vector2 bottomRight = new Vector2(rightLine.x, bottomLine.Value.y);
                        
                        WallData wall = new WallData(topLeft, topRight, bottomLeft, bottomRight);
                        walls.Add(wall);
                        
                        // Визуализируем обнаруженную стену
                        if (showDebugVisuals)
                        {
                            Imgproc.rectangle(
                                linesMat,
                                new Point(topLeft.x, topLeft.y),
                                new Point(bottomRight.x, bottomRight.y),
                                new Scalar(0, 255, 255, 255), // Голубой для обнаруженных стен
                                2
                            );
                        }
                    }
                }
            }
            
            return walls;
        }
        
        /// <summary>
        /// Группирует близкие линии
        /// </summary>
        private List<Vector4> GroupLines(List<Vector4> lines, bool isVertical)
        {
            if (lines.Count == 0)
                return new List<Vector4>();
            
            // Сортируем линии по X или Y в зависимости от ориентации
            lines.Sort((a, b) => 
            {
                return isVertical 
                    ? a.x.CompareTo(b.x) 
                    : a.y.CompareTo(b.y);
            });
            
            List<Vector4> groupedLines = new List<Vector4>();
            List<Vector4> currentGroup = new List<Vector4> { lines[0] };
            
            for (int i = 1; i < lines.Count; i++)
            {
                Vector4 currentLine = lines[i];
                Vector4 previousLine = lines[i - 1];
                
                float distance = isVertical 
                    ? Mathf.Abs(currentLine.x - previousLine.x) 
                    : Mathf.Abs(currentLine.y - previousLine.y);
                
                if (distance <= lineGroupingThreshold)
                {
                    // Линия близка к предыдущей, добавляем в текущую группу
                    currentGroup.Add(currentLine);
                }
                else
                {
                    // Линия далеко от предыдущей, создаем новую группу
                    // Сначала обрабатываем текущую группу
                    if (currentGroup.Count > 0)
                    {
                        Vector4 groupedLine = AverageLines(currentGroup, isVertical);
                        groupedLines.Add(groupedLine);
                    }
                    
                    // Начинаем новую группу
                    currentGroup.Clear();
                    currentGroup.Add(currentLine);
                }
            }
            
            // Обрабатываем последнюю группу
            if (currentGroup.Count > 0)
            {
                Vector4 groupedLine = AverageLines(currentGroup, isVertical);
                groupedLines.Add(groupedLine);
            }
            
            return groupedLines;
        }
        
        /// <summary>
        /// Вычисляет среднюю линию из группы линий
        /// </summary>
        private Vector4 AverageLines(List<Vector4> lines, bool isVertical)
        {
            if (lines.Count == 1)
                return lines[0];
            
            float sumX1 = 0, sumY1 = 0, sumX2 = 0, sumY2 = 0;
            
            foreach (Vector4 line in lines)
            {
                sumX1 += line.x;
                sumY1 += line.y;
                sumX2 += line.z;
                sumY2 += line.w;
            }
            
            float avgX1 = sumX1 / lines.Count;
            float avgY1 = sumY1 / lines.Count;
            float avgX2 = sumX2 / lines.Count;
            float avgY2 = sumY2 / lines.Count;
            
            if (isVertical)
            {
                // Для вертикальных линий усредняем X, но сохраняем крайние Y
                float minY = float.MaxValue;
                float maxY = float.MinValue;
                
                foreach (Vector4 line in lines)
                {
                    minY = Mathf.Min(minY, Mathf.Min(line.y, line.w));
                    maxY = Mathf.Max(maxY, Mathf.Max(line.y, line.w));
                }
                
                return new Vector4(avgX1, minY, avgX1, maxY);
            }
            else
            {
                // Для горизонтальных линий усредняем Y, но сохраняем крайние X
                float minX = float.MaxValue;
                float maxX = float.MinValue;
                
                foreach (Vector4 line in lines)
                {
                    minX = Mathf.Min(minX, Mathf.Min(line.x, line.z));
                    maxX = Mathf.Max(maxX, Mathf.Max(line.x, line.z));
                }
                
                return new Vector4(minX, avgY1, maxX, avgY1);
            }
        }
        
        /// <summary>
        /// Проверяет, является ли линия вертикальной
        /// </summary>
        private bool IsVerticalLine(double angle)
        {
            return (angle >= 180 - verticalAngleThreshold || angle <= verticalAngleThreshold);
        }
        
        /// <summary>
        /// Проверяет, является ли линия горизонтальной
        /// </summary>
        private bool IsHorizontalLine(double angle)
        {
            return (angle >= 90 - horizontalAngleThreshold && angle <= 90 + horizontalAngleThreshold);
        }
        
        /// <summary>
        /// Устанавливает входное изображение для обработки
        /// </summary>
        /// <param name="texture">Текстура для обработки</param>
        public void SetInputTexture(Texture2D texture)
        {
            inputTexture = texture;
            useWebcam = false;
        }
        
        /// <summary>
        /// Создает и возвращает тестовое изображение с простыми стенами
        /// </summary>
        /// <param name="width">Ширина изображения</param>
        /// <param name="height">Высота изображения</param>
        /// <returns>Текстура с тестовым изображением</returns>
        public Texture2D GetTestImage(int width = 1280, int height = 720)
        {
            return CreateTestImage(width, height);
        }
    }
} 