using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class WallPaintingManager : MonoBehaviour
{
    [SerializeField] private ColorManager colorManager;
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private Material wallMaterial;
    [SerializeField] private Material highlightMaterial;

    private Dictionary<ARPlane, GameObject> paintedWalls = new Dictionary<ARPlane, GameObject>();

    void Awake()
    {
        Debug.Log("WallPaintingManager: Awake вызван");
    }

    void OnEnable()
    {
        Debug.Log("WallPaintingManager: OnEnable вызван");
    }

    void Start()
    {
        Debug.Log("WallPaintingManager: Start вызван");
        
        // Проверка компонентов
        Debug.Log($"ColorManager: {(colorManager != null ? "найден" : "не найден")}");
        Debug.Log($"RaycastManager: {(raycastManager != null ? "найден" : "не найден")}");
        Debug.Log($"PlaneManager: {(planeManager != null ? "найден" : "не найден")}");
        Debug.Log($"WallMaterial: {(wallMaterial != null ? "найден" : "не найден")}");
        Debug.Log($"HighlightMaterial: {(highlightMaterial != null ? "найден" : "не найден")}");
    }

    void Update()
    {
        // Проверка через GetMouseButton
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("WallPaintingManager: Клик мыши обнаружен");
            HandleTouch(Input.mousePosition);
        }

        // Проверка через Input.touches
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                Debug.Log($"WallPaintingManager: Касание обнаружено в позиции {touch.position}");
                HandleTouch(touch.position);
            }
        }

        // Обновляем позиции покрашенных стен
        foreach (var kvp in new Dictionary<ARPlane, GameObject>(paintedWalls))
        {
            if (kvp.Key != null && kvp.Value != null)
            {
                kvp.Value.transform.position = kvp.Key.transform.position + kvp.Key.transform.forward * 0.0005f;
                kvp.Value.transform.rotation = kvp.Key.transform.rotation;
                kvp.Value.transform.localScale = new Vector3(1.02f, 1.02f, 1.0f);

                var planeMeshFilter = kvp.Key.GetComponent<MeshFilter>();
                var wallMeshFilter = kvp.Value.GetComponent<MeshFilter>();
                if (planeMeshFilter != null && wallMeshFilter != null && planeMeshFilter.mesh != null)
                {
                    wallMeshFilter.mesh = planeMeshFilter.mesh;
                }
            }
        }
    }

    private void HandleTouch(Vector2 touchPosition)
    {
        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        Debug.Log($"WallPaintingManager: Пытаемся сделать raycast из позиции {touchPosition}");
        
        if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            foreach (var hit in hits)
            {
                ARPlane plane = planeManager.GetPlane(hit.trackableId);
                if (plane != null && plane.alignment == PlaneAlignment.Vertical)
                {
                    if (paintedWalls.ContainsKey(plane))
                    {
                        // Обновляем цвет существующей стены
                        var existingWall = paintedWalls[plane];
                        var renderer = existingWall.GetComponent<MeshRenderer>();
                        renderer.material.color = colorManager.GetCurrentColor();
                        Debug.Log($"WallPaintingManager: Обновлен цвет существующей стены: {colorManager.GetCurrentColor()}");
                    }
                    else
                    {
                        // Создаем новый объект для покраски
                        GameObject coloredWall = new GameObject($"PaintedWall_{plane.trackableId}");
                        
                        // Располагаем чуть впереди и делаем немного больше
                        coloredWall.transform.position = plane.transform.position + plane.transform.forward * 0.0005f;
                        coloredWall.transform.rotation = plane.transform.rotation;
                        coloredWall.transform.localScale = new Vector3(1.02f, 1.02f, 1.0f);

                        MeshFilter meshFilter = coloredWall.AddComponent<MeshFilter>();
                        MeshRenderer meshRenderer = coloredWall.AddComponent<MeshRenderer>();

                        var planeMeshFilter = plane.GetComponent<MeshFilter>();
                        if (planeMeshFilter != null && planeMeshFilter.mesh != null)
                        {
                            meshFilter.mesh = planeMeshFilter.mesh;
                            Material newMaterial = new Material(wallMaterial);
                            newMaterial.color = colorManager.GetCurrentColor();
                            
                            meshRenderer.material = newMaterial;
                            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                            meshRenderer.receiveShadows = false;

                            paintedWalls.Add(plane, coloredWall);
                            Debug.Log($"WallPaintingManager: Создана новая покрашенная стена: {colorManager.GetCurrentColor()}");
                        }
                    }
                    return;
                }
            }
        }
        else
        {
            Debug.Log("WallPaintingManager: Raycast не попал ни в одну плоскость");
        }
    }

    void OnDisable()
    {
        foreach (var wall in paintedWalls.Values)
        {
            if (wall != null)
            {
                Destroy(wall);
            }
        }
        paintedWalls.Clear();
    }
}