using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using System.Linq;

public class WallDetectionManager : MonoBehaviour
{
    [Header("Настройки обнаружения")]
    [SerializeField] private float minPlaneArea = 0.01f;  // Сильно уменьшаем минимальную площадь
    [SerializeField] private float maxWallGap = 1.0f;     // Увеличиваем допустимый промежуток для лучшего объединения
    [SerializeField] private float planeMergeAngleThreshold = 35f; // Увеличиваем угол слияния
    [SerializeField] private float updateInterval = 0.05f;  // Чаще обновляем
    [SerializeField] private float edgeDistanceThreshold = 1.0f; // Увеличиваем порог расстояния краёв
    
    [Header("Настройки углов")]
    [SerializeField] private float cornerAngleThreshold = 85f; // Угол для определения углов (близко к 90)
    [SerializeField] private float cornerDistanceThreshold = 1.5f; // Увеличенное расстояние для углов
    [SerializeField] private float cornerHeightTolerance = 0.3f; // Допуск по высоте для углов
    [SerializeField] private bool prioritizeCorners = true; // Приоритет обработки углов

    [Header("Дополнительные настройки")]
    [SerializeField] private float confidenceThreshold = 0.5f; // Порог уверенности в плоскости
    [SerializeField] private float accumulationTime = 0.5f; // Время накопления данных
    [SerializeField] private int minPointsForPlane = 4; // Минимальное количество точек для плоскости

    [Header("Компоненты")]
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private Material wallMaterial;

    private Dictionary<TrackableId, WallInfo> detectedWalls = new Dictionary<TrackableId, WallInfo>();
    private float lastUpdateTime;
    private const float MIN_WALL_HEIGHT = 0.2f; // Уменьшаем минимальную высоту
    private const float MIN_WALL_WIDTH = 0.05f;  // Уменьшаем минимальную ширину

    private void Start()
    {
        if (planeManager != null)
        {
            ConfigurePlaneDetection();
        }
        else
        {
            Debug.LogError("WallDetectionManager: AR Plane Manager не назначен!");
        }

        if (wallMaterial == null)
        {
            Debug.LogError("WallDetectionManager: Wall Material не назначен!");
        }
    }

    private void ConfigurePlaneDetection()
    {
        if (planeManager == null) return;

        // Базовая настройка
        planeManager.requestedDetectionMode = PlaneDetectionMode.Vertical;
        
        // Настройка через subsystem если доступен
        if (planeManager.subsystem != null)
        {
            planeManager.subsystem.requestedPlaneDetectionMode = PlaneDetectionMode.Vertical;
            Debug.Log("Настройка параметров AR плоскостей выполнена успешно");
        }

        EnablePlaneDetection();
    }

    private void OnEnable()
    {
        EnablePlaneDetection();
    }

    private void OnDisable()
    {
        DisablePlaneDetection();
        detectedWalls.Clear();
    }

    private void EnablePlaneDetection()
    {
        if (planeManager != null)
        {
            planeManager.enabled = true;
            planeManager.planesChanged += OnPlanesChanged;
        }
    }

    private void DisablePlaneDetection()
    {
        if (planeManager != null)
        {
            planeManager.planesChanged -= OnPlanesChanged;
            planeManager.enabled = false;
        }
    }

    private void OnPlanesChanged(ARPlanesChangedEventArgs eventArgs)
    {
        // Обработка добавленных плоскостей
        foreach (ARPlane plane in eventArgs.added)
        {
            if (plane != null)
            {
                plane.boundaryChanged += OnPlaneBoundaryChanged;
            }
        }

        // Обработка удаленных плоскостей
        foreach (ARPlane plane in eventArgs.removed)
        {
            if (plane != null)
            {
                plane.boundaryChanged -= OnPlaneBoundaryChanged;
                if (detectedWalls.ContainsKey(plane.trackableId))
                {
                    detectedWalls.Remove(plane.trackableId);
                }
            }
        }

        // Обработка обновленных плоскостей
        foreach (ARPlane plane in eventArgs.updated)
        {
            if (plane != null && plane.gameObject != null)
            {
                ProcessPlane(plane);
            }
        }
    }

    private void OnPlaneBoundaryChanged(ARPlaneBoundaryChangedEventArgs args)
    {
        if (Time.time - lastUpdateTime < updateInterval) return;
        lastUpdateTime = Time.time;

        var plane = args.plane;
        ProcessPlane(plane);
        MergeWalls();
    }

    private void Update()
    {
        if (Time.time - lastUpdateTime < updateInterval) return;
        lastUpdateTime = Time.time;

        if (planeManager != null)
        {
            UpdateWalls();
            MergeWalls();
        }
    }

    private void UpdateWalls()
    {
        var invalidWalls = new List<TrackableId>();

        foreach (var wallPair in detectedWalls)
        {
            if (!planeManager.trackables.TryGetTrackable(wallPair.Key, out ARPlane _))
            {
                invalidWalls.Add(wallPair.Key);
            }
        }

        foreach (var id in invalidWalls)
        {
            detectedWalls.Remove(id);
        }

        foreach (var plane in planeManager.trackables)
        {
            if (plane != null && plane.gameObject != null)
            {
                ProcessPlane(plane);
            }
        }
    }

    private void ProcessPlane(ARPlane plane)
    {
        if (!plane.gameObject.activeInHierarchy)
            return;

        // Проверяем уверенность в плоскости
        if (plane.alignment != PlaneAlignment.Vertical || plane.trackingState != TrackingState.Tracking)
        {
            return;
        }

        Vector2 size = plane.size;
        float area = size.x * size.y;

        // Более мягкие требования к размерам
        if (area < minPlaneArea || size.y < MIN_WALL_HEIGHT || size.x < MIN_WALL_WIDTH)
        {
            return;
        }

        // Проверяем количество точек
        var mesh = plane.GetComponent<MeshFilter>()?.mesh;
        if (mesh != null && mesh.vertices.Length < minPointsForPlane)
        {
            return;
        }

        // Более мягкая проверка вертикальности
        float verticalAngle = Vector3.Angle(plane.normal, Vector3.up);
        if (Mathf.Abs(verticalAngle - 90f) > 35f) // Увеличиваем допуск к вертикальности
        {
            return;
        }

        if (!detectedWalls.ContainsKey(plane.trackableId))
        {
            detectedWalls[plane.trackableId] = new WallInfo(plane, planeManager);

            var meshRenderer = plane.GetComponent<MeshRenderer>();
            if (meshRenderer != null && wallMaterial != null)
            {
                meshRenderer.material = new Material(wallMaterial);
                Color wallColor = meshRenderer.material.color;
                wallColor.a = 1f;
                meshRenderer.material.color = wallColor;
                meshRenderer.material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            }
        }

        detectedWalls[plane.trackableId].UpdateBoundaries();
    }

    private void MergeWalls()
    {
        bool mergedAny;
        do
        {
            mergedAny = false;
            var wallsToMerge = new List<(TrackableId, TrackableId)>();
            var processedWalls = new HashSet<TrackableId>();

            // Сначала обрабатываем углы, если включен приоритет углов
            if (prioritizeCorners)
            {
                foreach (var wall1 in detectedWalls)
                {
                    if (!wall1.Value.isActive || processedWalls.Contains(wall1.Key)) continue;

                    foreach (var wall2 in detectedWalls)
                    {
                        if (!wall2.Value.isActive || wall1.Key == wall2.Key || 
                            processedWalls.Contains(wall2.Key)) continue;

                        if (planeManager.trackables.TryGetTrackable(wall1.Key, out ARPlane plane1) &&
                            planeManager.trackables.TryGetTrackable(wall2.Key, out ARPlane plane2))
                        {
                            if (IsCorner(plane1, plane2) && ShouldMergePlanes(plane1, plane2))
                            {
                                wallsToMerge.Add((wall1.Key, wall2.Key));
                                processedWalls.Add(wall1.Key);
                                processedWalls.Add(wall2.Key);
                                mergedAny = true;
                                break;
                            }
                        }
                    }
                }
            }

            // Затем обрабатываем остальные стены
            foreach (var wall1 in detectedWalls)
            {
                if (!wall1.Value.isActive || processedWalls.Contains(wall1.Key)) continue;

                foreach (var wall2 in detectedWalls)
                {
                    if (!wall2.Value.isActive || wall1.Key == wall2.Key || 
                        processedWalls.Contains(wall2.Key)) continue;

                    if (planeManager.trackables.TryGetTrackable(wall1.Key, out ARPlane plane1) &&
                        planeManager.trackables.TryGetTrackable(wall2.Key, out ARPlane plane2))
                    {
                        if (ShouldMergePlanes(plane1, plane2))
                        {
                            wallsToMerge.Add((wall1.Key, wall2.Key));
                            processedWalls.Add(wall1.Key);
                            processedWalls.Add(wall2.Key);
                            mergedAny = true;
                            break;
                        }
                    }
                }
                if (mergedAny) break;
            }

            foreach (var (wall1Id, wall2Id) in wallsToMerge)
            {
                MergeWallPair(wall1Id, wall2Id);
            }
        } while (mergedAny);
    }

    private bool ShouldMergePlanes(ARPlane plane1, ARPlane plane2)
    {
        if (plane1.trackingState != TrackingState.Tracking || 
            plane2.trackingState != TrackingState.Tracking)
        {
            return false;
        }

        // Проверяем, образуют ли плоскости угол
        bool isCorner = IsCorner(plane1, plane2);
        float maxDistance = isCorner ? cornerDistanceThreshold : maxWallGap;
        
        float distance = Vector3.Distance(plane1.center, plane2.center);
        if (distance > maxDistance) return false;

        // Для углов используем другую логику
        if (isCorner)
        {
            return ValidateCorner(plane1, plane2);
        }

        // Стандартная логика для параллельных стен
        float angle = Vector3.Angle(plane1.normal, plane2.normal);
        if (angle > planeMergeAngleThreshold) return false;

        Bounds bounds1 = plane1.GetComponent<MeshRenderer>().bounds;
        Bounds bounds2 = plane2.GetComponent<MeshRenderer>().bounds;
        
        float heightOverlap = Mathf.Min(bounds1.max.y, bounds2.max.y) - Mathf.Max(bounds1.min.y, bounds2.min.y);
        if (heightOverlap < 0.01f) return false;

        if (!bounds1.Intersects(bounds2))
        {
            return HasSharedEdge(plane1, plane2);
        }

        return true;
    }

    private bool IsCorner(ARPlane plane1, ARPlane plane2)
    {
        // Получаем нормали плоскостей в мировых координатах
        Vector3 normal1 = plane1.transform.TransformDirection(plane1.normal).normalized;
        Vector3 normal2 = plane2.transform.TransformDirection(plane2.normal).normalized;

        // Проверяем угол между нормалями
        float angle = Vector3.Angle(normal1, normal2);
        return Mathf.Abs(angle - 90f) < cornerAngleThreshold;
    }

    private bool ValidateCorner(ARPlane plane1, ARPlane plane2)
    {
        Bounds bounds1 = plane1.GetComponent<MeshRenderer>().bounds;
        Bounds bounds2 = plane2.GetComponent<MeshRenderer>().bounds;

        // Проверяем перекрытие по высоте с большим допуском
        float heightDiff = Mathf.Abs(bounds1.center.y - bounds2.center.y);
        if (heightDiff > cornerHeightTolerance) return false;

        // Проверяем близость краёв
        Vector3 closestPoint1 = GetClosestEdgePoint(plane1, plane2.center);
        Vector3 closestPoint2 = GetClosestEdgePoint(plane2, plane1.center);
        
        float edgeDistance = Vector3.Distance(closestPoint1, closestPoint2);
        return edgeDistance < cornerDistanceThreshold;
    }

    private Vector3 GetClosestEdgePoint(ARPlane plane, Vector3 targetPoint)
    {
        if (!detectedWalls.TryGetValue(plane.trackableId, out WallInfo wallInfo))
            return plane.center;

        Vector3 closestPoint = plane.center;
        float minDistance = float.MaxValue;

        // Проверяем все рёбра плоскости
        for (int i = 0; i < wallInfo.boundaries.Length; i++)
        {
            Vector3 start = wallInfo.boundaries[i];
            Vector3 end = wallInfo.boundaries[(i + 1) % wallInfo.boundaries.Length];

            Vector3 point = GetClosestPointOnLine(start, end, targetPoint);
            float distance = Vector3.Distance(point, targetPoint);

            if (distance < minDistance)
            {
                minDistance = distance;
                closestPoint = point;
            }
        }

        return closestPoint;
    }

    private Vector3 GetClosestPointOnLine(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
    {
        Vector3 line = lineEnd - lineStart;
        float lineLength = line.magnitude;
        line.Normalize();

        Vector3 v = point - lineStart;
        float d = Vector3.Dot(v, line);
        d = Mathf.Clamp(d, 0f, lineLength);

        return lineStart + line * d;
    }

    private bool HasSharedEdge(ARPlane plane1, ARPlane plane2)
    {
        var bounds1 = detectedWalls[plane1.trackableId].boundaries;
        var bounds2 = detectedWalls[plane2.trackableId].boundaries;

        // Проверяем каждую пару рёбер
        for (int i = 0; i < bounds1.Length; i++)
        {
            Vector3 edge1Start = bounds1[i];
            Vector3 edge1End = bounds1[(i + 1) % bounds1.Length];

            for (int j = 0; j < bounds2.Length; j++)
            {
                Vector3 edge2Start = bounds2[j];
                Vector3 edge2End = bounds2[(j + 1) % bounds2.Length];

                // Проверяем близость рёбер и их направление
                if (EdgesAreClose(edge1Start, edge1End, edge2Start, edge2End))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool EdgesAreClose(Vector3 edge1Start, Vector3 edge1End, Vector3 edge2Start, Vector3 edge2End)
    {
        float distStart = Mathf.Min(
            Vector3.Distance(edge1Start, edge2Start),
            Vector3.Distance(edge1Start, edge2End)
        );
        float distEnd = Mathf.Min(
            Vector3.Distance(edge1End, edge2Start),
            Vector3.Distance(edge1End, edge2End)
        );

        Vector3 edge1Dir = (edge1End - edge1Start).normalized;
        Vector3 edge2Dir = (edge2End - edge2Start).normalized;
        float edgeAngle = Vector3.Angle(edge1Dir, edge2Dir);

        // Сильно смягчаем условия для объединения
        return (distStart < edgeDistanceThreshold || distEnd < edgeDistanceThreshold) &&
               (edgeAngle < 35f || Mathf.Abs(edgeAngle - 180f) < 35f);
    }

    private void MergeWallPair(TrackableId wall1Id, TrackableId wall2Id)
    {
        if (!detectedWalls.ContainsKey(wall1Id) || !detectedWalls.ContainsKey(wall2Id))
            return;

        var wall1 = detectedWalls[wall1Id];
        var wall2 = detectedWalls[wall2Id];

        if (planeManager.trackables.TryGetTrackable(wall1Id, out ARPlane plane1) &&
            planeManager.trackables.TryGetTrackable(wall2Id, out ARPlane plane2))
        {
            wall1.MergeWith(wall2);

            var meshFilter1 = plane1.GetComponent<MeshFilter>();
            var meshRenderer1 = plane1.GetComponent<MeshRenderer>();
            var meshRenderer2 = plane2.GetComponent<MeshRenderer>();

            if (meshFilter1 != null && meshFilter1.mesh != null)
            {
                if (meshRenderer1 != null && meshRenderer2 != null)
                {
                    float brightness1 = meshRenderer1.material.color.grayscale;
                    float brightness2 = meshRenderer2.material.color.grayscale;
                    
                    if (brightness2 > brightness1)
                    {
                        meshRenderer1.material.color = meshRenderer2.material.color;
                    }
                }

                wall2.isActive = false;
                plane2.gameObject.SetActive(false);
            }
        }
    }

    public class WallInfo
    {
        public TrackableId trackableId;
        public Vector3[] boundaries;
        public HashSet<TrackableId> connectedWalls;
        private ARPlaneManager planeManager;
        public List<Vector3> mergedBoundaries;
        public bool isActive = true;

        public WallInfo(ARPlane plane, ARPlaneManager manager)
        {
            this.trackableId = plane.trackableId;
            this.planeManager = manager;
            this.connectedWalls = new HashSet<TrackableId>();
            this.mergedBoundaries = new List<Vector3>();
            UpdateBoundaries();
        }

        public void UpdateBoundaries()
        {
            if (!isActive) return;

            if (planeManager != null && planeManager.trackables.TryGetTrackable(trackableId, out ARPlane plane))
            {
                var mesh = plane.GetComponent<MeshFilter>()?.mesh;
                if (mesh != null)
                {
                    boundaries = new Vector3[mesh.vertices.Length];
                    for (int i = 0; i < mesh.vertices.Length; i++)
                    {
                        boundaries[i] = plane.transform.TransformPoint(mesh.vertices[i]);
                    }
                    if (mergedBoundaries.Count == 0)
                    {
                        mergedBoundaries = boundaries.ToList();
                    }
                }
            }
        }

        public void MergeWith(WallInfo other)
        {
            if (!isActive || !other.isActive) return;

            foreach (var point in other.mergedBoundaries)
            {
                if (!mergedBoundaries.Any(p => Vector3.Distance(p, point) < 0.01f))
                {
                    mergedBoundaries.Add(point);
                }
            }

            connectedWalls.UnionWith(other.connectedWalls);
            connectedWalls.Add(other.trackableId);
        }
    }
}