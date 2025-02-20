using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using System.Linq;

public class WallPaintingManager : MonoBehaviour
{
    [SerializeField] private ColorManager colorManager;
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private Material wallMaterial;
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private float previewAlpha = 0.5f; // Прозрачность предпросмотра
    [SerializeField] private float colorTransitionSpeed = 5f; // Скорость перехода цвета

    private Dictionary<TrackableId, PaintedWallInfo> paintedWalls = new Dictionary<TrackableId, PaintedWallInfo>();
    private ARPlane currentHighlightedPlane;
    private GameObject previewObject;
    private Stack<PaintAction> undoStack = new Stack<PaintAction>();
    private const int MAX_UNDO_STEPS = 20;

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

        if (colorManager == null || raycastManager == null || planeManager == null ||
            wallMaterial == null || highlightMaterial == null)
        {
            Debug.LogError("WallPaintingManager: Отсутствуют необходимые компоненты!");
            enabled = false;
            return;
        }

        // Создаем объект предпросмотра
        previewObject = new GameObject("PaintPreview");
        previewObject.AddComponent<MeshFilter>();
        var previewRenderer = previewObject.AddComponent<MeshRenderer>();
        previewRenderer.material = new Material(highlightMaterial);
        previewRenderer.material.color = new Color(1f, 1f, 1f, previewAlpha);
        previewObject.SetActive(false);
    }

    void Update()
    {
        UpdatePreview();
        UpdatePaintedWalls();
        HandleInput();
    }

    private void UpdatePreview()
    {
        ARPlane hitPlane = GetHitPlane();

        if (hitPlane != null && hitPlane.alignment == PlaneAlignment.Vertical)
        {
            if (currentHighlightedPlane != hitPlane)
            {
                currentHighlightedPlane = hitPlane;
                UpdatePreviewMesh(hitPlane);
            }
            previewObject.SetActive(true);
        }
        else
        {
            currentHighlightedPlane = null;
            previewObject.SetActive(false);
        }
    }

    private void UpdatePreviewMesh(ARPlane plane)
    {
        var planeMesh = plane.GetComponent<MeshFilter>()?.mesh;
        if (planeMesh == null) return;

        var previewMeshFilter = previewObject.GetComponent<MeshFilter>();
        previewMeshFilter.mesh = planeMesh;

        previewObject.transform.position = plane.transform.position + plane.transform.forward * 0.001f;
        previewObject.transform.rotation = plane.transform.rotation;
        previewObject.transform.localScale = new Vector3(1.01f, 1.01f, 1f);

        var previewRenderer = previewObject.GetComponent<MeshRenderer>();
        previewRenderer.material.color = new Color(
            colorManager.GetCurrentColor().r,
            colorManager.GetCurrentColor().g,
            colorManager.GetCurrentColor().b,
            previewAlpha
        );
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            if (currentHighlightedPlane != null)
            {
                PaintWall(currentHighlightedPlane);
            }
        }

        // Отмена последнего действия
        if (Input.GetKeyDown(KeyCode.Z) && (Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand)))
        {
            UndoLastAction();
        }
    }

    private void PaintWall(ARPlane plane)
    {
        Color newColor = colorManager.GetCurrentColor();

        if (!paintedWalls.TryGetValue(plane.trackableId, out var wallInfo))
        {
            GameObject paintedWall = CreatePaintedWall(plane);
            wallInfo = new PaintedWallInfo(paintedWall, newColor);
            paintedWalls.Add(plane.trackableId, wallInfo);
        }

        // Сохраняем действие для отмены
        undoStack.Push(new PaintAction(plane.trackableId, wallInfo.currentColor, newColor));
        if (undoStack.Count > MAX_UNDO_STEPS)
        {
            undoStack = new Stack<PaintAction>(undoStack.Take(MAX_UNDO_STEPS));
        }

        wallInfo.targetColor = newColor;
    }

    private GameObject CreatePaintedWall(ARPlane plane)
    {
        GameObject wall = new GameObject($"PaintedWall_{plane.trackableId}");

        wall.transform.position = plane.transform.position + plane.transform.forward * 0.001f;
        wall.transform.rotation = plane.transform.rotation;
        wall.transform.localScale = new Vector3(1.01f, 1.01f, 1f);

        var meshFilter = wall.AddComponent<MeshFilter>();
        var meshRenderer = wall.AddComponent<MeshRenderer>();

        var planeMesh = plane.GetComponent<MeshFilter>()?.mesh;
        if (planeMesh != null)
        {
            meshFilter.mesh = planeMesh;
        }

        meshRenderer.material = new Material(wallMaterial);
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        return wall;
    }

    private void UpdatePaintedWalls()
    {
        foreach (var wallInfo in paintedWalls.Values)
        {
            if (wallInfo.gameObject != null)
            {
                // Плавный переход цвета
                wallInfo.currentColor = Color.Lerp(
                    wallInfo.currentColor,
                    wallInfo.targetColor,
                    Time.deltaTime * colorTransitionSpeed
                );

                var renderer = wallInfo.gameObject.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.material.color = wallInfo.currentColor;
                }
            }
        }
    }

    private ARPlane GetHitPlane()
    {
        Vector2 screenPosition = Input.mousePosition;
        if (Input.touchCount > 0)
        {
            screenPosition = Input.GetTouch(0).position;
        }

        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        if (raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            foreach (var hit in hits)
            {
                ARPlane plane = planeManager.GetPlane(hit.trackableId);
                if (plane != null && plane.alignment == PlaneAlignment.Vertical)
                {
                    return plane;
                }
            }
        }
        return null;
    }

    private void UndoLastAction()
    {
        if (undoStack.Count == 0) return;

        var action = undoStack.Pop();
        if (paintedWalls.TryGetValue(action.planeId, out var wallInfo))
        {
            wallInfo.targetColor = action.previousColor;
        }
    }

    void OnDisable()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
        }

        foreach (var wallInfo in paintedWalls.Values)
        {
            if (wallInfo.gameObject != null)
            {
                Destroy(wallInfo.gameObject);
            }
        }
        paintedWalls.Clear();
        undoStack.Clear();
    }

    private class PaintedWallInfo
    {
        public GameObject gameObject;
        public Color currentColor;
        public Color targetColor;

        public PaintedWallInfo(GameObject obj, Color color)
        {
            gameObject = obj;
            currentColor = color;
            targetColor = color;
        }
    }

    private struct PaintAction
    {
        public TrackableId planeId;
        public Color previousColor;
        public Color newColor;

        public PaintAction(TrackableId id, Color prev, Color next)
        {
            planeId = id;
            previousColor = prev;
            newColor = next;
        }
    }
}