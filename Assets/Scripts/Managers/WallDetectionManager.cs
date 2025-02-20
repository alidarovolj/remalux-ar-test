using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using System.Linq;

public class WallDetectionManager : MonoBehaviour
{
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private Material wallMaterial;
    [SerializeField] private float minPlaneArea = 0.25f;
    [SerializeField] private float maxWallGap = 0.15f;
    [SerializeField] private float planeMergeAngleThreshold = 8f;
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField] private float edgeDistanceThreshold = 0.2f;
    [SerializeField] private float heightMatchThreshold = 0.3f;

    private Dictionary<TrackableId, WallInfo> detectedWalls = new Dictionary<TrackableId, WallInfo>();
    private float lastUpdateTime;

    private void Start()
    {
        if (planeManager != null)
        {
            planeManager.requestedDetectionMode = PlaneDetectionMode.Vertical;
            EnablePlaneDetection();
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
            planeManager.planesChanged += HandlePlanesChanged;
        }
    }

    private void DisablePlaneDetection()
    {
        if (planeManager != null)
        {
            planeManager.planesChanged -= HandlePlanesChanged;
            planeManager.enabled = false;
        }
    }

    private void HandlePlanesChanged(ARPlanesChangedEventArgs eventArgs)
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
        if (!plane.gameObject.activeInHierarchy || plane.alignment != PlaneAlignment.Vertical)
            return;

        Vector2 size = plane.size;
        float area = size.x * size.y;

        if (area < minPlaneArea)
        {
            return;
        }

        if (!detectedWalls.ContainsKey(plane.trackableId))
        {
            detectedWalls[plane.trackableId] = new WallInfo(plane, planeManager);

            var meshRenderer = plane.GetComponent<MeshRenderer>();
            if (meshRenderer != null && wallMaterial != null)
            {
                meshRenderer.material = wallMaterial;
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

            foreach (var wall1 in detectedWalls)
            {
                if (!wall1.Value.isActive) continue;

                foreach (var wall2 in detectedWalls)
                {
                    if (!wall2.Value.isActive || wall1.Key == wall2.Key) continue;

                    if (planeManager.trackables.TryGetTrackable(wall1.Key, out ARPlane plane1) &&
                        planeManager.trackables.TryGetTrackable(wall2.Key, out ARPlane plane2))
                    {
                        if (ShouldMergePlanes(plane1, plane2))
                        {
                            wallsToMerge.Add((wall1.Key, wall2.Key));
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
        // Проверяем расстояние между центрами
        float distance = Vector3.Distance(plane1.center, plane2.center);
        if (distance > maxWallGap) return false;

        // Проверяем угол между нормалями
        float angle = Vector3.Angle(plane1.normal, plane2.normal);
        if (angle > planeMergeAngleThreshold) return false;

        // Проверяем высоту
        float heightDiff = Mathf.Abs(plane1.center.y - plane2.center.y);
        if (heightDiff > heightMatchThreshold) return false;

        // Проверяем наличие общего края
        return HasSharedEdge(plane1, plane2);
    }

    private bool HasSharedEdge(ARPlane plane1, ARPlane plane2)
    {
        var bounds1 = detectedWalls[plane1.trackableId].boundaries;
        var bounds2 = detectedWalls[plane2.trackableId].boundaries;

        for (int i = 0; i < bounds1.Length; i++)
        {
            Vector3 edge1Start = bounds1[i];
            Vector3 edge1End = bounds1[(i + 1) % bounds1.Length];

            for (int j = 0; j < bounds2.Length; j++)
            {
                Vector3 edge2Start = bounds2[j];
                Vector3 edge2End = bounds2[(j + 1) % bounds2.Length];

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
        return distStart < edgeDistanceThreshold && distEnd < edgeDistanceThreshold;
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
            // Объединяем границы
            wall1.MergeWith(wall2);

            // Обновляем меш и материал
            var meshFilter1 = plane1.GetComponent<MeshFilter>();
            if (meshFilter1 != null && meshFilter1.mesh != null)
            {
                // Деактивируем вторую стену
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

            // Добавляем уникальные точки границ
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