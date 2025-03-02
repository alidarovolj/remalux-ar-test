using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Mathematics;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;

namespace Remalux.AR
{
    [System.Serializable]
    public class WallSegmentationSettings
    {
        [Header("Параметры сегментации")]
        [Range(0.1f, 0.9f)]
        public float confidenceThreshold = 0.4f;

        [Range(1, 10)]
        public int morphologySize = 4;

        [Range(1, 15)]
        public int blurSize = 7;

        public bool useAdaptiveThreshold = true;

        [Range(3, 21)]
        public int adaptiveBlockSize = 13;

        [Range(0.5f, 5f)]
        public double adaptiveC = 2.0;
    }

    [System.Serializable]
    public class ImageProcessingSettings
    {
        [Header("Параметры обработки изображения")]
        [Range(1, 15)]
        public int blurSize = 7;

        [Range(10, 200)]
        public int cannyThreshold1 = 30;

        [Range(50, 300)]
        public int cannyThreshold2 = 120;

        [Range(10, 200)]
        public double houghThreshold = 40;

        [Range(10, 200)]
        public double houghMinLineLength = 50;

        [Range(1, 50)]
        public double houghMaxLineGap = 10;

        [Range(1, 10)]
        public int dilationSize = 3;

        [Range(1, 10)]
        public int erosionSize = 2;

        [Header("Параметры фильтрации линий")]
        [Range(10, 100)]
        public float minLineLength = 30f;

        [Range(5, 50)]
        public float maxLineGap = 20f;

        [Range(1, 30)]
        public float angleThreshold = 5f;

        [Range(0.1f, 0.9f)]
        public float minConfidence = 0.7f;
    }

    public class ComputerVisionWallDetector : MonoBehaviour
    {
        [Header("Настройки камеры")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private int frameWidth = 1280;
        [SerializeField] private int frameHeight = 720;
        [SerializeField] private float processingInterval = 0.1f;

        [Header("Настройки определения стен")]
        [SerializeField] private float minWallLength = 0.5f;
        [SerializeField] private float maxWallGap = 0.3f;
        [SerializeField] private float angleThreshold = 5f;
        [SerializeField] private float minConfidence = 0.7f;

        [Header("Настройки визуализации")]
        [SerializeField] private Material wallMaterial;
        [SerializeField] private float wallHeight = 2.5f;
        [SerializeField] private bool showDebug = true;

        [Header("Расширенные настройки")]
        [SerializeField] private WallSegmentationSettings segmentationSettings = new WallSegmentationSettings();
        [SerializeField] private ImageProcessingSettings imageProcessingSettings = new ImageProcessingSettings();

        [SerializeField] private WallSegmentationSettings settings = new WallSegmentationSettings();
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private WallPaintingManager wallPaintingManager;

        // Компоненты обработки
        private ImageProcessor imageProcessor;
        private WallSegmentation wallSegmentation;
        private MeshBuilder meshBuilder;

        // Состояние
        private Dictionary<int, GameObject> detectedWalls = new Dictionary<int, GameObject>();
        private GameObject currentRoom;
        private float lastProcessTime;
        private RenderTexture cameraTexture;
        private Texture2D processedFrame;

        private void Start()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            // Создаем текстуры для обработки
            cameraTexture = new RenderTexture(frameWidth, frameHeight, 24);
            processedFrame = new Texture2D(frameWidth, frameHeight, TextureFormat.RGB24, false);

            // Инициализируем компоненты
            imageProcessor = new ImageProcessor(frameWidth, frameHeight);

            // Применяем настройки к ImageProcessor
            ApplyImageProcessingSettings();

            wallSegmentation = new WallSegmentation();

            // Применяем настройки к WallSegmentation
            ApplySegmentationSettings();

            // Добавляем и инициализируем MeshBuilder
            var meshBuilderObject = new GameObject("MeshBuilder");
            meshBuilderObject.transform.parent = transform;
            meshBuilder = meshBuilderObject.AddComponent<MeshBuilder>();
            meshBuilder.Initialize(wallHeight, wallMaterial);

            Debug.Log("ComputerVisionWallDetector: Инициализация завершена");
        }

        private void ApplyImageProcessingSettings()
        {
            if (imageProcessor == null)
                return;

            // Применяем настройки к ImageProcessor
            imageProcessor.SetBlurSize(imageProcessingSettings.blurSize);
            imageProcessor.SetCannyThresholds(imageProcessingSettings.cannyThreshold1, imageProcessingSettings.cannyThreshold2);
            imageProcessor.SetHoughParameters(
                imageProcessingSettings.houghThreshold,
                imageProcessingSettings.houghMinLineLength,
                imageProcessingSettings.houghMaxLineGap
            );
            imageProcessor.SetMorphologyParameters(
                imageProcessingSettings.dilationSize,
                imageProcessingSettings.erosionSize
            );
            imageProcessor.SetLineFilterParameters(
                imageProcessingSettings.minLineLength,
                imageProcessingSettings.maxLineGap,
                imageProcessingSettings.angleThreshold,
                imageProcessingSettings.minConfidence
            );
        }

        private void ApplySegmentationSettings()
        {
            if (wallSegmentation == null)
                return;

            // Применяем настройки к WallSegmentation
            wallSegmentation.SetConfidenceThreshold(segmentationSettings.confidenceThreshold);
            wallSegmentation.SetMorphologySize(segmentationSettings.morphologySize);
            wallSegmentation.SetBlurSize(segmentationSettings.blurSize);
            wallSegmentation.SetAdaptiveThreshold(
                segmentationSettings.useAdaptiveThreshold,
                segmentationSettings.adaptiveBlockSize,
                segmentationSettings.adaptiveC
            );
        }

        private void Update()
        {
            if (Time.time - lastProcessTime < processingInterval)
                return;

            lastProcessTime = Time.time;
            ProcessCurrentFrame();
        }

        private async void ProcessCurrentFrame()
        {
            try
            {
                // Получаем кадр с камеры
                mainCamera.targetTexture = cameraTexture;
                mainCamera.Render();
                mainCamera.targetTexture = null;

                // Конвертируем в формат для обработки
                RenderTexture.active = cameraTexture;
                processedFrame.ReadPixels(new UnityEngine.Rect(0, 0, frameWidth, frameHeight), 0, 0);
                processedFrame.Apply();
                RenderTexture.active = null;

                // Запускаем асинхронную обработку
                await ProcessFrameAsync(processedFrame);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Ошибка при захвате кадра: {e.Message}");
            }
        }

        private async Task ProcessFrameAsync(Texture2D frame)
        {
            try
            {
                // Сегментация стен через ML
                using (var wallMask = await wallSegmentation.ProcessFrameAsync(frame))
                {
                    var wallMaskBytes = wallMask.ToBytes();

                    // Используем упрощенную версию обработки
                    var lines = imageProcessor.ProcessFrameSimple(frame, wallMaskBytes);

                    // Находим стены и углы
                    var walls = DetectWalls(lines);
                    ProcessWalls(walls);

                    if (showDebug)
                        DrawDebugInfo(walls);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Ошибка при обработке кадра: {e.Message}");
            }
        }

        private List<WallSegment> DetectWalls(Line[] lines)
        {
            var walls = new List<WallSegment>();

            // Группируем линии по направлению
            var groupedLines = GroupLinesByDirection(lines);

            // Объединяем близкие линии
            foreach (var group in groupedLines)
            {
                var mergedLines = MergeCloseLines(group);
                foreach (var line in mergedLines)
                {
                    if (line.length >= minWallLength)
                    {
                        var wall = new WallSegment(line);

                        // Оцениваем расстояние до стены
                        // Для простоты используем фиксированное расстояние, но можно использовать
                        // данные из AR для более точной оценки
                        float estimatedDistance = 2.0f; // 2 метра от камеры

                        // Обновляем мировые координаты
                        wall.UpdateWorldPositions(mainCamera, estimatedDistance);

                        // Добавляем стену только если уверенность в ней достаточно высока
                        if (wall.confidence >= minConfidence)
                        {
                            walls.Add(wall);
                        }
                    }
                }
            }

            return walls;
        }

        private List<Line[]> GroupLinesByDirection(Line[] lines)
        {
            var groups = new List<Line[]>();
            var processedLines = new HashSet<int>();

            for (int i = 0; i < lines.Length; i++)
            {
                if (processedLines.Contains(i))
                    continue;

                var currentGroup = new List<Line> { lines[i] };
                processedLines.Add(i);

                for (int j = i + 1; j < lines.Length; j++)
                {
                    if (processedLines.Contains(j))
                        continue;

                    float angle = Vector2.Angle(lines[i].direction, lines[j].direction);
                    if (angle < angleThreshold || angle > 180 - angleThreshold)
                    {
                        currentGroup.Add(lines[j]);
                        processedLines.Add(j);
                    }
                }

                groups.Add(currentGroup.ToArray());
            }

            return groups;
        }

        private List<Line> MergeCloseLines(Line[] lines)
        {
            var mergedLines = new List<Line>();
            var processedLines = new HashSet<int>();

            for (int i = 0; i < lines.Length; i++)
            {
                if (processedLines.Contains(i))
                    continue;

                var currentLine = lines[i];
                processedLines.Add(i);

                for (int j = i + 1; j < lines.Length; j++)
                {
                    if (processedLines.Contains(j))
                        continue;

                    if (AreLinesMergeable(currentLine, lines[j]))
                    {
                        currentLine = MergeLines(currentLine, lines[j]);
                        processedLines.Add(j);
                    }
                }

                mergedLines.Add(currentLine);
            }

            return mergedLines;
        }

        private bool AreLinesMergeable(Line line1, Line line2)
        {
            float distance = Vector2.Distance(line1.center, line2.center);
            return distance < maxWallGap;
        }

        private Line MergeLines(Line line1, Line line2)
        {
            Vector2 newStart = Vector2.Lerp(line1.start, line2.start, 0.5f);
            Vector2 newEnd = Vector2.Lerp(line1.end, line2.end, 0.5f);
            return new Line(newStart, newEnd);
        }

        private void ProcessWalls(List<WallSegment> walls)
        {
            // Обновляем существующие стены
            foreach (var wall in walls)
            {
                if (detectedWalls.ContainsKey(wall.id))
                {
                    meshBuilder.UpdateWallMesh(detectedWalls[wall.id], wall);
                }
                else
                {
                    var newWallObject = meshBuilder.CreateWallMesh(wall);
                    if (newWallObject != null)
                    {
                        detectedWalls.Add(wall.id, newWallObject);
                    }
                }
            }

            // Удаляем устаревшие стены
            var wallsToRemove = new List<int>();
            foreach (var kvp in detectedWalls)
            {
                if (!walls.Exists(w => w.id == kvp.Key))
                {
                    wallsToRemove.Add(kvp.Key);
                }
            }

            foreach (var id in wallsToRemove)
            {
                if (detectedWalls.TryGetValue(id, out var wallObject))
                {
                    Destroy(wallObject);
                    detectedWalls.Remove(id);
                }
            }

            // Уведомляем WallPaintingManager о обнаружении стен
            if (wallPaintingManager != null && walls.Count > 0)
            {
                // Создаем список GameObject стен для передачи в WallPaintingManager
                List<GameObject> wallObjects = new List<GameObject>();
                foreach (var wall in walls)
                {
                    // Создаем GameObject для стены, если его еще нет
                    GameObject wallObject = CreateWallGameObject(wall);
                    if (wallObject != null)
                        wallObjects.Add(wallObject);
                }

                wallPaintingManager.OnWallsDetected(wallObjects);
            }
        }

        // Метод для создания GameObject стены из WallSegment
        private GameObject CreateWallGameObject(WallSegment wall)
        {
            // Создаем новый объект стены
            GameObject wallObject = GameObject.CreatePrimitive(PrimitiveType.Cube);

            // Вычисляем размеры и позицию
            Vector3 direction = (wall.worldEnd - wall.worldStart).normalized;
            float length = wall.length;

            // Устанавливаем позицию, поворот и масштаб
            wallObject.transform.position = wall.worldCenter;
            wallObject.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            wallObject.transform.localScale = new Vector3(0.1f, wallHeight, length);

            // Устанавливаем слой Wall
            wallObject.layer = LayerMask.NameToLayer("Wall");

            // Устанавливаем материал
            if (wallMaterial != null)
            {
                Renderer renderer = wallObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = wallMaterial;
                }
            }

            return wallObject;
        }

        private void DrawDebugInfo(List<WallSegment> walls)
        {
            foreach (var wall in walls)
            {
                Debug.DrawLine(wall.worldStart, wall.worldEnd, Color.green, processingInterval);
                Debug.DrawRay(wall.worldCenter, wall.normal, Color.blue, processingInterval);
            }
        }

        private void OnDestroy()
        {
            if (cameraTexture != null)
                Destroy(cameraTexture);

            if (processedFrame != null)
                Destroy(processedFrame);

            if (meshBuilder != null)
                Destroy(meshBuilder.gameObject);

            imageProcessor?.Dispose();
            wallSegmentation?.Dispose();
        }

        // Добавляем метод для обновления настроек во время выполнения
        public void UpdateSettings()
        {
            ApplyImageProcessingSettings();
            ApplySegmentationSettings();
            Debug.Log("ComputerVisionWallDetector: Настройки обновлены");
        }

        // Добавляем метод для сброса настроек к значениям по умолчанию
        public void ResetSettings()
        {
            segmentationSettings = new WallSegmentationSettings();
            imageProcessingSettings = new ImageProcessingSettings();
            UpdateSettings();
            Debug.Log("ComputerVisionWallDetector: Настройки сброшены к значениям по умолчанию");
        }
    }

#if UNITY_EDITOR
    // Добавляем кнопки в инспектор для обновления и сброса настроек
    [UnityEditor.CustomEditor(typeof(ComputerVisionWallDetector))]
    public class ComputerVisionWallDetectorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ComputerVisionWallDetector detector = (ComputerVisionWallDetector)target;

            UnityEditor.EditorGUILayout.Space();
            UnityEditor.EditorGUILayout.LabelField("Управление настройками", UnityEditor.EditorStyles.boldLabel);

            if (GUILayout.Button("Обновить настройки"))
            {
                detector.UpdateSettings();
            }

            if (GUILayout.Button("Сбросить настройки"))
            {
                detector.ResetSettings();
            }
        }
    }
#endif

    public class WallSegment
    {
        public int id;
        public Vector2 start; // Экранные координаты начала
        public Vector2 end;   // Экранные координаты конца
        public Vector3 worldStart;
        public Vector3 worldEnd;
        public Vector3 worldCenter => (worldStart + worldEnd) * 0.5f;
        public Vector3 normal;
        public float confidence;
        public float length => Vector3.Distance(worldStart, worldEnd);

        public WallSegment(Line line)
        {
            // Генерируем уникальный ID на основе координат
            this.id = line.GetHashCode();
            this.confidence = line.confidence;

            // Сохраняем экранные координаты
            this.start = line.start;
            this.end = line.end;

            // Конвертация будет реализована в методе ScreenToWorldPoint
            this.worldStart = Vector3.zero;
            this.worldEnd = Vector3.zero;
            this.normal = Vector3.zero;
        }

        public void UpdateWorldPositions(Camera camera, float distance)
        {
            // Конвертируем экранные координаты в мировые
            worldStart = ScreenToWorldPoint(camera, new Vector2(start.x, start.y), distance);
            worldEnd = ScreenToWorldPoint(camera, new Vector2(end.x, end.y), distance);

            // Вычисляем нормаль к стене (перпендикулярно к направлению стены и вверх)
            Vector3 direction = (worldEnd - worldStart).normalized;
            normal = Vector3.Cross(direction, Vector3.up).normalized;

            // Если нормаль направлена от камеры, инвертируем её
            if (Vector3.Dot(normal, camera.transform.forward) < 0)
            {
                normal = -normal;
            }
        }

        private Vector3 ScreenToWorldPoint(Camera camera, Vector2 screenPoint, float distance)
        {
            // Конвертируем экранные координаты в нормализованные координаты (0-1)
            float normalizedX = screenPoint.x / camera.pixelWidth;
            float normalizedY = screenPoint.y / camera.pixelHeight;

            // Создаем луч из камеры через точку на экране
            Ray ray = camera.ViewportPointToRay(new Vector3(normalizedX, normalizedY, 0));

            // Вычисляем точку на расстоянии distance от камеры
            return ray.origin + ray.direction * distance;
        }
    }
}