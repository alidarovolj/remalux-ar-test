using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using System.Linq;

public class WallDetectionManager : MonoBehaviour
{
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private Material wallMaterial;
    [SerializeField] private float minPlaneArea = 0.09f;
    [SerializeField] private float maxWallGap = 1.0f;
    [SerializeField] private float angleThreshold = 20f;

    private Dictionary<TrackableId, WallInfo> detectedWalls = new Dictionary<TrackableId, WallInfo>();

    private void Start()
    {
        Debug.Log("WallDetectionManager: Start");
        
        if (planeManager != null)
        {
            planeManager.requestedDetectionMode = PlaneDetectionMode.Vertical;
            Debug.Log($"WallDetectionManager: Plane Manager найден, режим = {planeManager.requestedDetectionMode}");
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

    private void Update()
    {
        if (planeManager != null)
        {
            // Создаем временный список для удаления недействительных стен
            var invalidWalls = new List<TrackableId>();
            
            // Проверяем существующие стены
            foreach (var wallPair in detectedWalls)
            {
                if (!planeManager.trackables.TryGetTrackable(wallPair.Key, out ARPlane _))
                {
                    invalidWalls.Add(wallPair.Key);
                }
            }
            
            // Удаляем недействительные стены
            foreach (var id in invalidWalls)
            {
                detectedWalls.Remove(id);
            }

            // Обрабатываем текущие плоскости
            foreach (var plane in planeManager.trackables)
            {
                if (plane != null && plane.gameObject != null)
                {
                    ProcessPlane(plane);
                }
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
            Debug.Log($"WallDetectionManager: Плоскость {plane.trackableId} слишком мала: {size}");
            return;
        }

        // Проверяем соседние стены
        foreach (var wallPair in detectedWalls)
        {
            if (!planeManager.trackables.TryGetTrackable(wallPair.Key, out ARPlane existingPlane))
                continue;

            float distance = Vector3.Distance(plane.transform.position, existingPlane.transform.position);
            float angle = Vector3.Angle(plane.transform.up, existingPlane.transform.up);

            if (distance < maxWallGap && Mathf.Abs(angle - 90f) < angleThreshold)
            {
                float existingArea = existingPlane.size.x * existingPlane.size.y;
                if (area < existingArea)
                {
                    Debug.Log($"WallDetectionManager: Плоскость {plane.trackableId} пропущена как часть угла");
                    return;
                }
            }
        }

        // Обновляем или добавляем информацию о стене
        if (!detectedWalls.ContainsKey(plane.trackableId))
        {
            detectedWalls[plane.trackableId] = new WallInfo(plane, planeManager);
            Debug.Log($"WallDetectionManager: Добавлена новая стена {plane.trackableId}");
        }

        var meshRenderer = plane.GetComponent<MeshRenderer>();
        if (meshRenderer != null && wallMaterial != null)
        {
            meshRenderer.material = wallMaterial;
        }

        detectedWalls[plane.trackableId].UpdateBoundaries();
    }

    public class WallInfo
    {
        public TrackableId trackableId;
        public Vector3[] boundaries;
        public HashSet<TrackableId> connectedWalls;
        private ARPlaneManager planeManager;

        public WallInfo(ARPlane plane, ARPlaneManager manager)
        {
            this.trackableId = plane.trackableId;
            this.planeManager = manager;
            this.connectedWalls = new HashSet<TrackableId>();
            UpdateBoundaries();
        }

        public void UpdateBoundaries()
        {
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
                }
            }
        }
    }

    private void OnDisable()
    {
        detectedWalls.Clear();
    }
}