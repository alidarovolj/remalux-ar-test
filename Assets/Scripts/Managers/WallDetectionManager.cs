using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace Remalux.AR
{
    public class RoomCorner
    {
        public Vector3 position;
        public Vector3 normal1;
        public Vector3 normal2;
        public TrackableId wall1Id;
        public TrackableId wall2Id;
        public float angle;
        public float confidence;

        public RoomCorner(Vector3 pos, Vector3 n1, Vector3 n2, TrackableId w1, TrackableId w2)
        {
            position = pos;
            normal1 = n1.normalized;
            normal2 = n2.normalized;
            wall1Id = w1;
            wall2Id = w2;
            angle = Vector3.Angle(normal1, normal2);
            
            // Вычисляем уверенность на основе угла (90 градусов - идеальный угол для комнаты)
            confidence = 1f - Mathf.Abs(angle - 90f) / 90f;
        }

        public bool IsValid()
        {
            // Угол должен быть близок к 90 градусам (допуск ±30 градусов)
            return angle >= 60f && angle <= 120f && confidence > 0.5f;
        }
    }

    public class WallDetectionManager : MonoBehaviour
    {
        public delegate void WallDetectedHandler(ARPlane plane, GameObject wall);
        public delegate void RoomCornerDetectedHandler(RoomCorner corner);
        public event WallDetectedHandler OnWallDetected;
        public event RoomCornerDetectedHandler OnCornerDetected;

        private const float MIN_WALL_HEIGHT = 1.5f; // Уменьшаем высоту стены для тестирования

        [Header("Настройки обнаружения")]
        [SerializeField] private float updateInterval = 0.05f;
        [SerializeField] private float minWallArea = 0.1f;
        [SerializeField] private float verticalAlignmentThreshold = 30f;
        
        [Header("Настройки углов")]
        [SerializeField] private float cornerAngleThreshold = 95f;
        [SerializeField] private float cornerDistanceThreshold = 3.0f;

        [Header("Компоненты")]
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private Material wallMaterial;

        private Dictionary<TrackableId, GameObject> detectedWalls = new Dictionary<TrackableId, GameObject>();
        private Dictionary<TrackableId, Vector3> wallLastPositions = new Dictionary<TrackableId, Vector3>();
        private float lastUpdateTime;
        private const float MIN_WALL_WIDTH = 0.05f;
        private const float UPDATE_THRESHOLD = 0.01f; // Минимальное изменение для обновления
        private const float POSITION_SMOOTHING = 0.3f; // Увеличиваем время сглаживания
        private const float SIZE_SMOOTHING = 0.2f; // Увеличиваем время сглаживания
        private const float MAX_POSITION_CHANGE = 0.05f; // Уменьшаем максимальное изменение за кадр
        private const float MAX_SIZE_CHANGE = 0.025f; // Уменьшаем максимальное изменение за кадр
        private const float STABILITY_THRESHOLD = 0.01f; // Порог стабильности
        private const float STABILITY_TIME = 0.5f; // Время для признания стены стабильной

        // Добавляем константы для размеров стен
        public const float WALL_HEIGHT = 2.0f;          // Стандартная высота стены (2.0 метра - типичная высота для жилых помещений)
        public const float WALL_EXTENSION = 1.01f;      // Минимальное расширение (1%)
        public const float WALL_BOTTOM_OFFSET = -0.1f; // Небольшой отступ вниз для лучшего прилегания к полу

        private float lastDebugTime = 0f;
        private const float DEBUG_LOG_INTERVAL = 0.5f; // Интервал для дебаг сообщений

        private Dictionary<string, RoomCorner> detectedCorners = new Dictionary<string, RoomCorner>();
        private const float CORNER_DETECTION_INTERVAL = 0.5f;
        private float lastCornerDetectionTime;

        private void Start()
        {
            if (planeManager == null)
            {
                Debug.LogError("WallDetectionManager: AR Plane Manager не назначен!");
                return;
            }

            Debug.Log($"Plane Detection Mode: {planeManager.requestedDetectionMode}");
            Debug.Log($"Plane Manager enabled: {planeManager.enabled}");
            Debug.Log($"Plane Manager subsystem running: {planeManager.subsystem?.running}");
            Debug.Log($"Plane Manager trackables count: {planeManager.trackables.count}");
            Debug.Log($"Plane Manager prefab: {(planeManager.planePrefab != null ? "Assigned" : "Not Assigned")}");
            
            if (wallMaterial == null)
            {
                Debug.LogError("WallDetectionManager: Wall Material не назначен!");
                return;
            }

            // Включаем отображение AR-плоскостей для отладки
            if (planeManager.planePrefab != null)
            {
                planeManager.planePrefab.SetActive(true);
            }
            
            // Включаем обнаружение плоскостей
            planeManager.enabled = true;
            planeManager.requestedDetectionMode = PlaneDetectionMode.Vertical | PlaneDetectionMode.Horizontal;

            // Активируем существующие плоскости для отладки
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(true);
                Debug.Log($"Existing plane: ID={plane.trackableId}, Alignment={plane.alignment}, Size={plane.size}");
            }

            ConfigurePlaneDetection();
        }

        private void ConfigurePlaneDetection()
        {
            if (planeManager.subsystem != null)
            {
                // Устанавливаем режим обнаружения
                planeManager.requestedDetectionMode = PlaneDetectionMode.Vertical | PlaneDetectionMode.Horizontal;
                
                // Принудительно перезапускаем подсистему
                if (!planeManager.subsystem.running)
                {
                    planeManager.subsystem.Start();
                }
                
                var subsystemDescriptor = planeManager.subsystem.subsystemDescriptor;
                if (subsystemDescriptor != null)
                {
                    Debug.Log($"Plane Subsystem capabilities:");
                    Debug.Log($"- Supports Classification: {subsystemDescriptor.supportsClassification}");
                    Debug.Log($"- Supports Bounding Box: {subsystemDescriptor.supportsBoundaryVertices}");
                    Debug.Log($"- Supports Polygons: {subsystemDescriptor.supportsArbitraryPlaneDetection}");
                    
                    // Устанавливаем режим обнаружения в подсистеме
                    planeManager.subsystem.requestedPlaneDetectionMode = PlaneDetectionMode.Vertical | PlaneDetectionMode.Horizontal;
                    
                    Debug.Log($"Plane Subsystem настроен для быстрого обнаружения: {subsystemDescriptor.id}");
                }
            }
        }

        private void OnEnable()
        {
            if (planeManager != null)
            {
                planeManager.trackablesChanged.AddListener(OnTrackablesChanged);
                Debug.Log("WallDetectionManager: Подписка на события изменения плоскостей");
            }
        }

        private void OnDisable()
        {
            if (planeManager != null)
            {
                planeManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
            }
            foreach (var wall in detectedWalls.Values)
            {
                if (wall != null)
                {
                    Destroy(wall);
                }
            }
            detectedWalls.Clear();
        }

        private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARPlane> eventArgs)
        {
            // Обработка добавленных плоскостей
            if (eventArgs.added != null)
            {
                foreach (var plane in eventArgs.added)
                {
                    Debug.Log($"OnTrackablesChanged: Обнаружена новая плоскость. ID: {plane.trackableId}, Выравнивание: {plane.alignment}, Размер: {plane.size}");
                    if (plane.alignment == PlaneAlignment.Vertical)
                    {
                        ProcessPlane(plane);
                    }
                }
            }

            // Обработка обновленных плоскостей
            if (eventArgs.updated != null)
            {
                foreach (var plane in eventArgs.updated)
                {
                    if (plane.alignment == PlaneAlignment.Vertical)
                    {
                        UpdatePlane(plane);
                    }
                }
            }

            // Обработка удаленных плоскостей
            if (eventArgs.removed != null)
            {
                foreach (var pair in eventArgs.removed)
                {
                    var trackableId = pair.Key;
                    if (detectedWalls.TryGetValue(trackableId, out var wall))
                    {
                        Destroy(wall);
                        detectedWalls.Remove(trackableId);
                    }
                }
            }
        }

        private void ProcessPlane(ARPlane plane)
        {
            if (plane.alignment == PlaneAlignment.Vertical)
            {
                // Быстрая проверка основных параметров
                float angleFromVertical = Vector3.Angle(plane.normal, Vector3.up);
                bool isNearlyVertical = Mathf.Abs(angleFromVertical - 90f) < verticalAlignmentThreshold;
                bool isLargeEnough = plane.size.x * plane.size.y > minWallArea;
                
                Debug.Log($"ProcessPlane: Проверка плоскости {plane.trackableId}:");
                Debug.Log($"- Угол от вертикали: {angleFromVertical}°");
                Debug.Log($"- Почти вертикальная: {isNearlyVertical}");
                Debug.Log($"- Размер: {plane.size}, Площадь: {plane.size.x * plane.size.y}");
                Debug.Log($"- Достаточно большая: {isLargeEnough}");
                
                if (isNearlyVertical && isLargeEnough && !detectedWalls.ContainsKey(plane.trackableId))
                {
                    Debug.Log($"ProcessPlane: Создаем стену для плоскости {plane.trackableId}");
                    CreateWall(plane);
                }
                else
                {
                    Debug.Log($"ProcessPlane: Плоскость {plane.trackableId} не подходит для создания стены");
                }
            }
            else
            {
                Debug.Log($"ProcessPlane: Плоскость {plane.trackableId} не вертикальная (alignment: {plane.alignment})");
            }
        }

        private void UpdatePlane(ARPlane plane)
        {
            if (detectedWalls.TryGetValue(plane.trackableId, out var wall))
            {
                UpdateWall(wall, plane);
            }
            else if (plane.alignment == PlaneAlignment.Vertical)
            {
                CreateWall(plane);
            }
        }

        private void RemovePlane(GameObject wall)
        {
            if (detectedWalls.TryGetValue(wall.GetComponent<ARPlane>().trackableId, out var existingWall))
            {
                Destroy(existingWall);
                detectedWalls.Remove(wall.GetComponent<ARPlane>().trackableId);
            }
        }

        private void CalculateWallDimensions(ARPlane plane, out Vector3 position, out float width, out float height)
        {
            // Находим крайние точки в мировых координатах
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float lowestY = float.MaxValue;
            float highestY = float.MinValue;

            Vector3 planeNormal = plane.normal.normalized;
            Vector3 planeRight = Vector3.Cross(Vector3.up, planeNormal).normalized;

            foreach (var point in plane.boundary)
            {
                Vector3 localPoint = new Vector3(point.x, 0, point.y);
                Vector3 worldPoint = plane.transform.TransformPoint(localPoint);
                
                float projectedX = Vector3.Dot(worldPoint - plane.center, planeRight);
                minX = Mathf.Min(minX, projectedX);
                maxX = Mathf.Max(maxX, projectedX);
                
                lowestY = Mathf.Min(lowestY, worldPoint.y);
                highestY = Mathf.Max(highestY, worldPoint.y);
            }

            width = (maxX - minX) * WALL_EXTENSION;
            
            // Вычисляем позицию
            position = plane.center;
            position.y = lowestY;
            position += planeNormal * 0.001f; // Смещение для z-fighting

            // Вычисляем высоту стены
            height = Mathf.Min(highestY - lowestY, WALL_HEIGHT);
        }

        private void CreateWall(ARPlane plane)
        {
            if (detectedWalls.ContainsKey(plane.trackableId))
            {
                Debug.Log($"CreateWall: Стена для плоскости {plane.trackableId} уже существует");
                return;
            }

            Debug.Log($"CreateWall: Начало создания стены для плоскости {plane.trackableId}");

            var wall = new GameObject($"Wall_{plane.trackableId}");
            
            // Создаем компоненты
            var meshFilter = wall.AddComponent<MeshFilter>();
            var meshRenderer = wall.AddComponent<MeshRenderer>();
            meshRenderer.material = wallMaterial;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            // Вычисляем позицию и размеры
            Vector3 targetPosition;
            float targetWidth;
            float targetHeight;
            CalculateWallDimensions(plane, out targetPosition, out targetWidth, out targetHeight);

            // Устанавливаем позицию и поворот
            wall.transform.position = targetPosition;
            wall.transform.rotation = Quaternion.LookRotation(-plane.normal.normalized, Vector3.up);

            // Создаем меш с правильными размерами
            var mesh = new Mesh();
            var vertices = new Vector3[4];
            var triangles = new int[] { 0, 1, 2, 2, 1, 3 };
            var uvs = new Vector2[4];

            // Создаем вершины для прямоугольной стены
            vertices[0] = new Vector3(-targetWidth/2, WALL_BOTTOM_OFFSET, 0);
            vertices[1] = new Vector3(-targetWidth/2, targetHeight, 0);
            vertices[2] = new Vector3(targetWidth/2, WALL_BOTTOM_OFFSET, 0);
            vertices[3] = new Vector3(targetWidth/2, targetHeight, 0);

            // UV координаты для правильного масштабирования текстуры
            float tileScale = 1f;
            uvs[0] = new Vector2(0, 0);
            uvs[1] = new Vector2(0, targetHeight * tileScale);
            uvs[2] = new Vector2(targetWidth * tileScale, 0);
            uvs[3] = new Vector2(targetWidth * tileScale, targetHeight * tileScale);

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            meshFilter.mesh = mesh;
            
            Debug.Log($"CreateWall: Стена создана успешно. Размеры: {targetWidth}x{targetHeight}");
            
            detectedWalls.Add(plane.trackableId, wall);
            wallLastPositions.Add(plane.trackableId, targetPosition);
            
            OnWallDetected?.Invoke(plane, wall);
        }

        private void UpdateWall(GameObject wall, ARPlane plane)
        {
            var meshFilter = wall.GetComponent<MeshFilter>();
            if (meshFilter == null) return;

            bool wasUpdated = false;

            // Вычисляем новую позицию и размеры
            Vector3 targetPosition;
            float targetWidth;
            float targetHeight;
            CalculateWallDimensions(plane, out targetPosition, out targetWidth, out targetHeight);

            // Проверяем, нужно ли обновлять стену
            if (!wallLastPositions.ContainsKey(plane.trackableId) ||
                Vector3.Distance(wallLastPositions[plane.trackableId], targetPosition) > UPDATE_THRESHOLD)
            {
                // Обновляем позицию
                wall.transform.position = targetPosition;
                wallLastPositions[plane.trackableId] = targetPosition;

                // Обновляем поворот
                wall.transform.rotation = Quaternion.LookRotation(-plane.normal.normalized, Vector3.up);

                wasUpdated = true;
            }

            // Обновляем меш только если размер изменился
            float currentWidth = meshFilter.mesh.bounds.size.x;
            float currentHeight = meshFilter.mesh.bounds.size.y;
            if (Mathf.Abs(currentWidth - targetWidth) > UPDATE_THRESHOLD ||
                Mathf.Abs(currentHeight - targetHeight) > UPDATE_THRESHOLD)
            {
                // Обновляем меш с новыми размерами
                var mesh = new Mesh();
                var vertices = new Vector3[4];
                var triangles = new int[] { 0, 1, 2, 2, 1, 3 };
                var uvs = new Vector2[4];

                vertices[0] = new Vector3(-targetWidth/2, WALL_BOTTOM_OFFSET, 0);
                vertices[1] = new Vector3(-targetWidth/2, targetHeight, 0);
                vertices[2] = new Vector3(targetWidth/2, WALL_BOTTOM_OFFSET, 0);
                vertices[3] = new Vector3(targetWidth/2, targetHeight, 0);

                float tileScale = 1f;
                uvs[0] = new Vector2(0, 0);
                uvs[1] = new Vector2(0, targetHeight * tileScale);
                uvs[2] = new Vector2(targetWidth * tileScale, 0);
                uvs[3] = new Vector2(targetWidth * tileScale, targetHeight * tileScale);

                mesh.vertices = vertices;
                mesh.triangles = triangles;
                mesh.uv = uvs;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();

                meshFilter.mesh = mesh;
                wasUpdated = true;
            }

            // Если стена была обновлена, вызываем событие
            if (wasUpdated)
            {
                OnWallDetected?.Invoke(plane, wall);
            }
        }

        private bool IsPlaneRectangular(ARPlane plane)
        {
            if (plane.boundary.Length < 4) return false;

            // Конвертируем boundary в массив
            Vector2[] points = new Vector2[plane.boundary.Length];
            plane.boundary.CopyTo(points);

            // Находим углы между последовательными сегментами
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 current = points[i];
                Vector2 next = points[(i + 1) % points.Length];
                Vector2 nextNext = points[(i + 2) % points.Length];

                Vector2 dir1 = (next - current).normalized;
                Vector2 dir2 = (nextNext - next).normalized;

                float angle = Vector2.Angle(dir1, dir2);
                
                // Проверяем, близок ли угол к 90 градусам
                if (Mathf.Abs(angle - 90f) > 15f) // Допуск в 15 градусов
                {
                    return false;
                }
            }

            return true;
        }

        private void Update()
        {
            // Добавляем периодическую диагностику
            if (Time.time - lastDebugTime >= DEBUG_LOG_INTERVAL)
            {
                lastDebugTime = Time.time;
                Debug.Log($"AR Debug - " +
                         $"Planes: {planeManager.trackables.count}, " +
                         $"Subsystem Running: {planeManager.subsystem?.running}, " +
                         $"Walls: {detectedWalls.Count}, " +
                         $"Corners: {detectedCorners.Count}");
            }

            // Обновляем только если прошло достаточно времени
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                lastUpdateTime = Time.time;
                UpdateWalls();
            }

            // Обновляем углы комнаты
            if (Time.time - lastCornerDetectionTime >= CORNER_DETECTION_INTERVAL)
            {
                lastCornerDetectionTime = Time.time;
                DetectRoomCorners();
            }
        }

        private void UpdateWalls()
        {
            if (!planeManager.subsystem?.running ?? true) return;

            // Используем HashSet для быстрого поиска
            var currentPlaneIds = new HashSet<TrackableId>();
            
            // Быстрое обновление существующих стен
            foreach (var plane in planeManager.trackables)
            {
                if (plane != null && plane.alignment == PlaneAlignment.Vertical)
                {
                    currentPlaneIds.Add(plane.trackableId);
                    
                    if (!detectedWalls.ContainsKey(plane.trackableId))
                    {
                        CreateWall(plane);
                    }
                    else if (Time.time - lastUpdateTime >= updateInterval)
                    {
                        UpdatePlane(plane);
                    }
                }
            }

            // Быстрое удаление недействительных стен
            var wallsToRemove = detectedWalls.Keys.Where(id => !currentPlaneIds.Contains(id)).ToList();
            foreach (var id in wallsToRemove)
            {
                if (detectedWalls.TryGetValue(id, out var wall))
                {
                    Destroy(wall);
                    detectedWalls.Remove(id);
                    wallLastPositions.Remove(id);
                }
            }
        }

        private void DetectRoomCorners()
        {
            Debug.Log("Начало поиска углов комнаты...");
            Debug.Log($"Количество обнаруженных стен: {detectedWalls.Count}");
            
            var walls = detectedWalls.ToList();
            var newCorners = new Dictionary<string, RoomCorner>();

            // Проверяем каждую пару стен
            for (int i = 0; i < walls.Count; i++)
            {
                for (int j = i + 1; j < walls.Count; j++)
                {
                    var wall1 = walls[i];
                    var wall2 = walls[j];

                    // Получаем нормали стен
                    Vector3 normal1 = wall1.Value.transform.forward;
                    Vector3 normal2 = wall2.Value.transform.forward;

                    // Вычисляем угол между стенами
                    float angle = Vector3.Angle(normal1, normal2);
                    
                    Debug.Log($"Проверка пары стен: {wall1.Key} и {wall2.Key}. Угол между ними: {angle}°");

                    // Проверяем, образуют ли стены угол, близкий к 90 градусам
                    if (Mathf.Abs(angle - 90f) <= cornerAngleThreshold)
                    {
                        Debug.Log($"Найден потенциальный угол между стенами {wall1.Key} и {wall2.Key}");
                        // Находим точку пересечения стен
                        if (TryFindIntersection(wall1.Value, wall2.Value, out Vector3 intersection))
                        {
                            var corner = new RoomCorner(intersection, normal1, normal2, wall1.Key, wall2.Key);
                            
                            if (corner.IsValid())
                            {
                                string cornerId = $"{wall1.Key}_{wall2.Key}";
                                newCorners[cornerId] = corner;
                                Debug.Log($"Обнаружен новый угол комнаты: ID={cornerId}, Позиция={intersection}, Угол={angle}°, Уверенность={corner.confidence}");

                                // Если это новый угол, вызываем событие
                                if (!detectedCorners.ContainsKey(cornerId))
                                {
                                    OnCornerDetected?.Invoke(corner);
                                }
                            }
                        }
                    }
                }
            }

            // Обновляем список углов
            detectedCorners = newCorners;
            Debug.Log($"Поиск углов завершен. Найдено углов: {detectedCorners.Count}");
        }

        private bool TryFindIntersection(GameObject wall1, GameObject wall2, out Vector3 intersection)
        {
            intersection = Vector3.zero;

            // Получаем позиции и нормали стен
            Vector3 pos1 = wall1.transform.position;
            Vector3 pos2 = wall2.transform.position;
            Vector3 normal1 = wall1.transform.forward;
            Vector3 normal2 = wall2.transform.forward;

            // Проверяем расстояние между стенами
            float distance = Vector3.Distance(pos1, pos2);
            if (distance > cornerDistanceThreshold)
                return false;

            // Находим точку пересечения плоскостей
            Vector3 direction = Vector3.Cross(normal1, normal2);
            if (direction.magnitude < 0.01f) // Стены параллельны
                return false;

            // Проецируем точки на плоскость XZ
            Vector2 pos1_2D = new Vector2(pos1.x, pos1.z);
            Vector2 pos2_2D = new Vector2(pos2.x, pos2.z);
            Vector2 normal1_2D = new Vector2(normal1.x, normal1.z).normalized;
            Vector2 normal2_2D = new Vector2(normal2.x, normal2.z).normalized;

            // Находим точку пересечения в 2D
            Vector2 intersection2D = FindIntersection2D(pos1_2D, normal1_2D, pos2_2D, normal2_2D);
            
            // Восстанавливаем Y-координату
            float y = Mathf.Max(pos1.y, pos2.y);
            intersection = new Vector3(intersection2D.x, y, intersection2D.y);

            return true;
        }

        private Vector2 FindIntersection2D(Vector2 pos1, Vector2 normal1, Vector2 pos2, Vector2 normal2)
        {
            // Находим точку пересечения двух прямых в 2D
            float d = normal1.x * normal2.y - normal1.y * normal2.x;
            
            if (Mathf.Abs(d) < 0.001f)
                return (pos1 + pos2) * 0.5f;

            float t = ((pos2.x - pos1.x) * normal2.y - (pos2.y - pos1.y) * normal2.x) / d;
            return pos1 + normal1 * t;
        }

        public Dictionary<TrackableId, GameObject> GetDetectedWalls()
        {
            return new Dictionary<TrackableId, GameObject>(detectedWalls);
        }

        public Dictionary<string, RoomCorner> GetDetectedCorners()
        {
            return new Dictionary<string, RoomCorner>(detectedCorners);
        }
    }
}