using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace Remalux.AR
{
    public class WallDetectionManager : MonoBehaviour
    {
        public delegate void WallDetectedHandler(ARPlane plane, GameObject wall);
        public event WallDetectedHandler OnWallDetected;

        private const float MIN_WALL_HEIGHT = 1.5f; // Уменьшаем высоту стены для тестирования

        [Header("Настройки обнаружения")]
        [SerializeField] private float maxWallGap = 0.3f;    // Уменьшаем расстояние для объединения
        [SerializeField] private float planeMergeAngleThreshold = 15f; // Увеличиваем допуск для углов слияния
        [SerializeField] private float updateInterval = 0.1f; // Уменьшаем интервал обновления
        [SerializeField] private float edgeDistanceThreshold = 0.3f; // Уменьшаем порог для краёв
        [SerializeField] private float minWallArea = 0.2f; // Уменьшаем минимальную площадь для стены
        [SerializeField] private float verticalAlignmentThreshold = 20f; // Увеличиваем допуск отклонения от вертикали
        
        [Header("Настройки расширения")]
        [SerializeField] private float expansionRate = 1.05f; // Уменьшаем коэффициент расширения
        
        [Header("Настройки углов")]
        [SerializeField] private float cornerAngleThreshold = 95f; // Увеличиваем допуск для углов
        [SerializeField] private float cornerDistanceThreshold = 3.0f; // Значительно увеличиваем расстояние для углов
        [SerializeField] private float cornerHeightTolerance = 0.5f; // Увеличиваем допуск по высоте для углов
        [SerializeField] private bool prioritizeCorners = true; // Приоритет обработки углов

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
            
            if (wallMaterial == null)
            {
                Debug.LogError("WallDetectionManager: Wall Material не назначен!");
                return;
            }

            // Полностью отключаем визуализацию AR-плоскостей
            if (planeManager.planePrefab != null)
            {
                planeManager.planePrefab.SetActive(false);
            }
            
            // Отключаем отображение всех плоскостей
            planeManager.enabled = true;
            planeManager.requestedDetectionMode = PlaneDetectionMode.Vertical;
            planeManager.planePrefab = null;

            // Деактивируем существующие плоскости
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(false);
            }

            ConfigurePlaneDetection();
        }

        private void ConfigurePlaneDetection()
        {
            // Устанавливаем агрессивные настройки обнаружения
            planeManager.requestedDetectionMode = PlaneDetectionMode.Vertical;
            
            if (planeManager.subsystem != null)
            {
                var subsystemDescriptor = planeManager.subsystem.subsystemDescriptor;
                if (subsystemDescriptor != null)
                {
                    // Устанавливаем минимальные значения для быстрого обнаружения
                    planeManager.subsystem.requestedPlaneDetectionMode = PlaneDetectionMode.Vertical;
                    
                    // Включаем все возможные оптимизации
                    if (subsystemDescriptor.supportsClassification)
                    {
                        planeManager.requestedDetectionMode |= PlaneDetectionMode.Vertical;
                    }
                    
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
                
                if (isNearlyVertical && isLargeEnough && !detectedWalls.ContainsKey(plane.trackableId))
                {
                    // Быстрое создание стены без дополнительных проверок
                    CreateWall(plane);
                }
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
            if (detectedWalls.ContainsKey(plane.trackableId)) return;

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
            // Обновляем только если прошло достаточно времени
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                lastUpdateTime = Time.time;
                UpdateWalls();
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

        public Dictionary<TrackableId, GameObject> GetDetectedWalls()
        {
            return new Dictionary<TrackableId, GameObject>(detectedWalls);
        }
    }
}