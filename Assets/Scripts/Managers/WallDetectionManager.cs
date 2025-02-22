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
        [SerializeField] private float maxWallGap = 0.5f;    // Увеличиваем расстояние для объединения
        [SerializeField] private float planeMergeAngleThreshold = 10f; // Увеличиваем допуск для углов слияния
        [SerializeField] private float updateInterval = 0.2f; // Увеличиваем интервал обновления
        [SerializeField] private float edgeDistanceThreshold = 0.5f; // Увеличиваем порог для краёв
        [SerializeField] private float minWallArea = 0.3f; // Уменьшаем минимальную площадь для стены
        [SerializeField] private float verticalAlignmentThreshold = 15f; // Увеличиваем допуск отклонения от вертикали
        
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
        public const float WALL_HEIGHT = 1.8f;          // Уменьшаем стандартную высоту стены с 2.4м до 1.8м
        public const float WALL_EXTENSION = 1.01f;      // Минимальное расширение (1%)
        public const float WALL_BOTTOM_OFFSET = -0.05f; // Небольшой отступ вниз

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
            // Установим более агрессивные настройки обнаружения
            planeManager.requestedDetectionMode = PlaneDetectionMode.Vertical;
            
            if (planeManager.subsystem != null)
            {
                // Явно включаем обнаружение вертикальных поверхностей
                planeManager.subsystem.requestedPlaneDetectionMode = PlaneDetectionMode.Vertical;
                
                // Уменьшим минимальный размер обнаруживаемой плоскости
                var subsystemDescriptor = planeManager.subsystem.subsystemDescriptor;
                if (subsystemDescriptor != null)
                {
                    // Логируем возможности системы
                    Debug.Log($"Plane Subsystem: {subsystemDescriptor.id}, Supports: Vertical={subsystemDescriptor.supportsVerticalPlaneDetection}, Horizontal={subsystemDescriptor.supportsHorizontalPlaneDetection}");
                }
                
                Debug.Log("Настройка параметров AR плоскостей выполнена успешно");
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
            // Добавим более подробное логирование
            if (eventArgs.added != null && eventArgs.added.Count > 0)
            {
                Debug.Log($"Добавлено новых плоскостей: {eventArgs.added.Count}");
                foreach (var plane in eventArgs.added)
                {
                    Debug.Log($"Новая плоскость: ID={plane.trackableId}, " +
                             $"Alignment={plane.alignment}, " +
                             $"Position={plane.transform.position}");
                }
            }

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
                // Отключаем визуализацию для новой плоскости
                var meshRenderer = plane.gameObject.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.enabled = false;
                }

                var lineRenderer = plane.gameObject.GetComponent<LineRenderer>();
                if (lineRenderer != null)
                {
                    lineRenderer.enabled = false;
                }

                // Отключаем ARPlaneMeshVisualizer
                var visualizer = plane.gameObject.GetComponent<ARPlaneMeshVisualizer>();
                if (visualizer != null)
                {
                    visualizer.enabled = false;
                }

                // Фильтруем частоту дебаг сообщений
                bool shouldLog = Time.time - lastDebugTime >= DEBUG_LOG_INTERVAL;
                if (shouldLog)
                {
                    lastDebugTime = Time.time;
                    Debug.Log($"Plane Detection Mode: {planeManager.requestedDetectionMode}");
                    Debug.Log($"Plane Manager enabled: {planeManager.enabled}");
                }

                // Проверяем, действительно ли плоскость вертикальная
                float angleFromVertical = Vector3.Angle(plane.normal, Vector3.up);
                bool isNearlyVertical = Mathf.Abs(angleFromVertical - 90f) < verticalAlignmentThreshold;
                
                // Проверяем размер плоскости и соотношение сторон
                float aspectRatio = plane.size.y / plane.size.x;
                bool isLargeEnough = plane.size.x * plane.size.y > minWallArea;
                bool hasValidAspectRatio = aspectRatio < 1.2f; // Увеличиваем допустимое соотношение сторон
                
                // Проверяем, не находится ли плоскость слишком высоко
                bool isAtValidHeight = true;
                float averageHeight = 0;
                if (detectedWalls.Count > 0)
                {
                    foreach (var wall in detectedWalls.Values)
                    {
                        if (wall != null && wall.activeInHierarchy)
                        {
                            averageHeight += wall.transform.position.y;
                        }
                    }
                    averageHeight /= detectedWalls.Count;
                    
                    float heightDifference = Mathf.Abs(plane.center.y - averageHeight);
                    isAtValidHeight = heightDifference < 0.5f; // Увеличиваем допуск по высоте до 50см
                }

                // Проверяем, нет ли уже стены рядом с этой позицией и с похожей ориентацией
                bool isTooClose = false;
                foreach (var kvp in detectedWalls)
                {
                    var existingPlane = planeManager.GetPlane(kvp.Key);
                    if (existingPlane != null && kvp.Value.activeInHierarchy)
                    {
                        float distance = Vector3.Distance(kvp.Value.transform.position, plane.center);
                        float angleToExisting = Vector3.Angle(existingPlane.normal, plane.normal);
                        
                        // Проверяем расстояние и угол между плоскостями
                        if (distance < 0.3f && angleToExisting < 20f) // Уменьшаем минимальное расстояние
                        {
                            isTooClose = true;
                            break;
                        }
                    }
                }

                if (isNearlyVertical && isLargeEnough && hasValidAspectRatio && isAtValidHeight && !isTooClose)
                {
                    Debug.Log($"WallDetectionManager: Обработка вертикальной плоскости {plane.trackableId}");
                    Debug.Log($"Plane size: {plane.size}, Normal: {plane.normal}, Angle from vertical: {angleFromVertical}, Aspect ratio: {aspectRatio}");
                    
                    if (!detectedWalls.ContainsKey(plane.trackableId))
                    {
                        Debug.Log($"WallDetectionManager: Создание новой стены для плоскости {plane.trackableId}");
                        CreateWall(plane);
                    }
                    else
                    {
                        Debug.Log($"WallDetectionManager: Обновление существующей стены для плоскости {plane.trackableId}");
                        UpdatePlane(plane);
                    }
                }
                else
                {
                    Debug.Log($"WallDetectionManager: Плоскость отклонена. Угол: {angleFromVertical}, " +
                             $"Размер: {plane.size}, Соотношение сторон: {aspectRatio}, " +
                             $"Валидная высота: {isAtValidHeight}, Слишком близко: {isTooClose}");
                }
            }
            else
            {
                Debug.Log($"WallDetectionManager: Пропуск не-вертикальной плоскости {plane.trackableId}, alignment: {plane.alignment}");
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

        private void CreateWall(ARPlane plane)
        {
            if (detectedWalls.ContainsKey(plane.trackableId))
            {
                Debug.LogWarning($"WallDetectionManager: Попытка создать стену с существующим ID: {plane.trackableId}");
                return;
            }

            Debug.Log($"WallDetectionManager: Создание стены... Размер границы: {plane.boundary.Length}, Центр: {plane.center}, Нормаль: {plane.normal}");

            var wall = new GameObject($"Wall_{plane.trackableId}");
            
            // Создаем компоненты
            var meshFilter = wall.AddComponent<MeshFilter>();
            var meshRenderer = wall.AddComponent<MeshRenderer>();
            meshRenderer.material = wallMaterial; // Используем оригинальный материал вместо создания нового
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            // Обновляем меш и позицию
            UpdateWallMesh(meshFilter, plane, plane.size.x * WALL_EXTENSION);
            
            detectedWalls.Add(plane.trackableId, wall);
            
            Debug.Log($"WallDetectionManager: Стена создана успешно. Всего стен: {detectedWalls.Count}");
            
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
            CalculateWallDimensions(plane, out targetPosition, out targetWidth);

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
            if (Mathf.Abs(currentWidth - targetWidth) > UPDATE_THRESHOLD)
            {
                UpdateWallMesh(meshFilter, plane, targetWidth);
                wasUpdated = true;
            }

            // Если стена была обновлена, вызываем событие
            if (wasUpdated)
            {
                OnWallDetected?.Invoke(plane, wall);
            }
        }

        private void CalculateWallDimensions(ARPlane plane, out Vector3 position, out float width)
        {
            // Находим крайние точки в мировых координатах
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float lowestY = float.MaxValue;

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
            }

            width = (maxX - minX) * WALL_EXTENSION;
            
            // Вычисляем позицию
            position = plane.center;
            position.y = lowestY;
            position += planeNormal * 0.001f; // Смещение для z-fighting
        }

        private void UpdateWallMesh(MeshFilter meshFilter, ARPlane plane, float width)
        {
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();

            // Создаем вершины для прямоугольной стены
            vertices.Add(new Vector3(-width/2, WALL_BOTTOM_OFFSET, 0));  // Нижний левый
            vertices.Add(new Vector3(-width/2, WALL_HEIGHT, 0));         // Верхний левый
            vertices.Add(new Vector3(width/2, WALL_BOTTOM_OFFSET, 0));   // Нижний правый
            vertices.Add(new Vector3(width/2, WALL_HEIGHT, 0));          // Верхний правый

            // UV координаты для правильного масштабирования текстуры
            float tileScale = 1f;
            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(0, WALL_HEIGHT * tileScale));
            uvs.Add(new Vector2(width * tileScale, 0));
            uvs.Add(new Vector2(width * tileScale, WALL_HEIGHT * tileScale));

            // Индексы для двух треугольников
            triangles.Add(0); triangles.Add(1); triangles.Add(2);
            triangles.Add(2); triangles.Add(1); triangles.Add(3);

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            meshFilter.mesh = mesh;
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
            if (Time.time - lastUpdateTime < updateInterval) return;
            lastUpdateTime = Time.time;

            if (planeManager.subsystem != null && planeManager.subsystem.running)
            {
                UpdateWalls();
            }
        }

        private void UpdateWalls()
        {
            var invalidWalls = new List<TrackableId>();

            // Проверяем существующие стены
            foreach (var wallPair in detectedWalls)
            {
                var plane = planeManager.GetPlane(wallPair.Key);
                if (plane == null || !plane.gameObject.activeInHierarchy)
                {
                    invalidWalls.Add(wallPair.Key);
                }
            }

            // Удаляем недействительные стены
            foreach (var id in invalidWalls)
            {
                if (detectedWalls.TryGetValue(id, out var wallObject))
                {
                    Destroy(wallObject);
                    detectedWalls.Remove(id);
                    wallLastPositions.Remove(id);
                }
            }

            // Обновляем существующие и добавляем новые стены
            foreach (var plane in planeManager.trackables)
            {
                if (plane != null && plane.alignment == PlaneAlignment.Vertical)
                {
                    if (!detectedWalls.ContainsKey(plane.trackableId))
                    {
                        CreateWall(plane);
                    }
                    else
                    {
                        UpdatePlane(plane);
                    }
                }
            }
        }

        public Dictionary<TrackableId, GameObject> GetDetectedWalls()
        {
            return new Dictionary<TrackableId, GameObject>(detectedWalls);
        }
    }
}