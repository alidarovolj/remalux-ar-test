using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Mathematics;
using OpenCvSharp;

namespace Remalux.AR
{
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
            wallSegmentation = new WallSegmentation();
            
            // Добавляем и инициализируем MeshBuilder
            var meshBuilderObject = new GameObject("MeshBuilder");
            meshBuilderObject.transform.parent = transform;
            meshBuilder = meshBuilderObject.AddComponent<MeshBuilder>();
            meshBuilder.Initialize(wallHeight, wallMaterial);

            Debug.Log("ComputerVisionWallDetector: Инициализация завершена");
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
                    UpdateWalls(walls);

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

        private void UpdateWalls(List<WallSegment> newWalls)
        {
            // Обновляем существующие стены
            foreach (var wall in newWalls)
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
                if (!newWalls.Exists(w => w.id == kvp.Key))
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
    }

    public class WallSegment
    {
        public int id;
        public Vector3 worldStart;
        public Vector3 worldEnd;
        public Vector3 worldCenter => (worldStart + worldEnd) * 0.5f;
        public Vector3 normal;
        public float confidence;

        public WallSegment(Line line)
        {
            // Конвертируем из экранных координат в мировые
            this.id = line.GetHashCode();
            // TODO: Реализовать конвертацию координат
        }
    }
} 