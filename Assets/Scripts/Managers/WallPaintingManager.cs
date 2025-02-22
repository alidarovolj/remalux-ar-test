using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using System.Linq;
using Remalux.AR;

public class WallPaintingManager : MonoBehaviour
{
    [SerializeField] private ColorManager colorManager;
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private Material wallMaterial;
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private float previewAlpha = 0.5f; // Прозрачность предпросмотра
    [SerializeField] private float colorTransitionSpeed = 5f; // Скорость перехода цвета
    [SerializeField] private WallDetectionManager wallDetectionManager; // Добавляем ссылку
    
    // Добавляем настройки размеров стены
    [Header("Настройки размеров стены")]
    [SerializeField] private float wallHeight = 4.0f; // Увеличиваем высоту
    [SerializeField] private float wallWidthMultiplier = 1.5f; // Увеличиваем множитель ширины
    [SerializeField] private float bottomOffset = -0.2f; // Увеличиваем смещение вниз
    [SerializeField] private float forwardOffset = 0.005f; // Увеличиваем смещение вперед

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

        // Попытка найти WallDetectionManager, если он не назначен
        if (wallDetectionManager == null)
        {
            wallDetectionManager = FindFirstObjectByType<WallDetectionManager>();
        }

        // Проверяем, используется ли тот же ARPlaneManager
        if (wallDetectionManager != null && planeManager != wallDetectionManager.GetComponent<ARPlaneManager>())
        {
            Debug.LogWarning("WallPaintingManager: Используется другой экземпляр ARPlaneManager!");
            planeManager = wallDetectionManager.GetComponent<ARPlaneManager>();
        }

        // Проверка компонентов
        Debug.Log($"ColorManager: {(colorManager != null ? "найден" : "не найден")}");
        Debug.Log($"RaycastManager: {(raycastManager != null ? "найден" : "не найден")}");
        Debug.Log($"PlaneManager: {(planeManager != null ? "найден" : "не найден")}");
        Debug.Log($"WallMaterial: {(wallMaterial != null ? "найден" : "не найден")}");
        Debug.Log($"HighlightMaterial: {(highlightMaterial != null ? "найден" : "не найден")}");
        Debug.Log($"WallDetectionManager: {(wallDetectionManager != null ? "найден" : "не найден")}");

        if (colorManager == null || raycastManager == null || planeManager == null ||
            wallMaterial == null || highlightMaterial == null || wallDetectionManager == null)
        {
            Debug.LogError("WallPaintingManager: Отсутствуют необходимые компоненты!");
            enabled = false;
            return;
        }

        // Подписываемся на события обнаружения стен
        wallDetectionManager.OnWallDetected += OnWallDetected;

        // Создаем объект предпросмотра
        previewObject = new GameObject("PaintPreview");
        previewObject.AddComponent<MeshFilter>();
        var previewRenderer = previewObject.AddComponent<MeshRenderer>();
        previewRenderer.material = new Material(highlightMaterial);
        previewRenderer.material.color = new Color(1f, 1f, 1f, previewAlpha);
        previewObject.SetActive(false);

        // Получаем уже обнаруженные стены
        var detectedWalls = wallDetectionManager.GetDetectedWalls();
        foreach (var wall in detectedWalls)
        {
            var plane = planeManager.GetPlane(wall.Key);
            if (plane != null)
            {
                OnWallDetected(plane, wall.Value);
            }
        }
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
        Debug.Log($"Preview update - Plane position: {plane.transform.position}, rotation: {plane.transform.rotation}");

        var previewMeshFilter = previewObject.GetComponent<MeshFilter>();
        var previewRenderer = previewObject.GetComponent<MeshRenderer>();

        // Создаем меш для предпросмотра
        Mesh previewMesh = new Mesh();
        Vector3[] vertices = new Vector3[4];
        Vector2[] uvs = new Vector2[4];
        int[] triangles = new int[] { 0, 1, 2, 2, 1, 3 };

        // Находим крайние точки в мировых координатах
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float lowestY = float.MaxValue;

        // Получаем нормаль и направление вправо для плоскости
        Vector3 planeNormal = plane.normal.normalized;
        Vector3 planeRight = Vector3.Cross(Vector3.up, planeNormal).normalized;

        // Находим реальные границы стены
        foreach (var point in plane.boundary)
        {
            Vector3 localPoint = new Vector3(point.x, 0, point.y);
            Vector3 worldPoint = plane.transform.TransformPoint(localPoint);
            
            // Проецируем точку на направление вправо для определения ширины
            float projectedX = Vector3.Dot(worldPoint - plane.center, planeRight);
            minX = Mathf.Min(minX, projectedX);
            maxX = Mathf.Max(maxX, projectedX);
            
            lowestY = Mathf.Min(lowestY, worldPoint.y);
        }

        // Вычисляем реальную ширину стены
        float width = (maxX - minX) * WallDetectionManager.WALL_EXTENSION;

        // Создаем вершины
        vertices[0] = new Vector3(-width/2, WallDetectionManager.WALL_BOTTOM_OFFSET, 0);
        vertices[1] = new Vector3(-width/2, WallDetectionManager.WALL_HEIGHT, 0);
        vertices[2] = new Vector3(width/2, WallDetectionManager.WALL_BOTTOM_OFFSET, 0);
        vertices[3] = new Vector3(width/2, WallDetectionManager.WALL_HEIGHT, 0);

        // UV координаты
        float tileScale = 1f;
        uvs[0] = new Vector2(0, 0);
        uvs[1] = new Vector2(0, WallDetectionManager.WALL_HEIGHT * tileScale);
        uvs[2] = new Vector2(width * tileScale, 0);
        uvs[3] = new Vector2(width * tileScale, WallDetectionManager.WALL_HEIGHT * tileScale);

        previewMesh.vertices = vertices;
        previewMesh.uv = uvs;
        previewMesh.triangles = triangles;
        previewMesh.RecalculateNormals();
        previewMesh.RecalculateBounds();

        previewMeshFilter.mesh = previewMesh;

        // Позиционируем предпросмотр
        previewObject.transform.position = plane.center;
        previewObject.transform.rotation = Quaternion.LookRotation(-planeNormal, Vector3.up);
        previewObject.transform.position = new Vector3(previewObject.transform.position.x, lowestY, previewObject.transform.position.z);
        previewObject.transform.position += planeNormal * forwardOffset;

        // Устанавливаем цвет с прозрачностью
        Color previewColor = colorManager.GetCurrentColor();
        previewColor.a = previewAlpha;
        previewRenderer.material.color = previewColor;

        Debug.Log($"Preview dimensions - Width: {width}m, Height: {WallDetectionManager.WALL_HEIGHT}m, Position: {previewObject.transform.position}");
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
        Debug.Log($"Попытка покрасить стену. ID плоскости: {plane.trackableId}, Выбранный цвет: {newColor}");

        // Получаем основную стену из WallDetectionManager
        var detectedWalls = wallDetectionManager.GetDetectedWalls();
        if (detectedWalls.TryGetValue(plane.trackableId, out GameObject wallObject))
        {
            Debug.Log($"Найдена стена для покраски: {wallObject.name}");
            
            // Создаем или получаем информацию о покраске
            if (!paintedWalls.TryGetValue(plane.trackableId, out var wallInfo))
            {
                Debug.Log("Создаем новую информацию о покраске");
                wallInfo = new PaintedWallInfo(wallObject, Color.white);
                paintedWalls.Add(plane.trackableId, wallInfo);
            }

            // Сохраняем действие для отмены
            undoStack.Push(new PaintAction(plane.trackableId, wallInfo.currentColor, newColor));
            if (undoStack.Count > MAX_UNDO_STEPS)
            {
                undoStack = new Stack<PaintAction>(undoStack.Take(MAX_UNDO_STEPS));
            }

            // Применяем новый цвет
            wallInfo.targetColor = newColor;
            wallInfo.currentColor = newColor; // Мгновенно применяем цвет

            // Обновляем материал стены
            var renderer = wallObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Debug.Log($"Обновляем материал стены. Текущий материал: {renderer.material.name}");
                
                // Создаем новый материал, если еще не создан
                Material wallMat = renderer.material;
                if (wallMat.name.Contains("Default"))
                {
                    Debug.Log("Создаем новый материал для стены");
                    wallMat = new Material(wallMaterial);
                    renderer.material = wallMat;
                }

                // Устанавливаем цвет
                wallMat.color = newColor;
                Debug.Log($"Цвет материала установлен: {wallMat.color}");
            }
            else
            {
                Debug.LogError("MeshRenderer не найден на стене!");
            }
        }
        else
        {
            Debug.LogError($"Стена не найдена в WallDetectionManager для ID: {plane.trackableId}");
        }
    }

    private void UpdatePaintedWalls()
    {
        foreach (var wallInfo in paintedWalls.Values)
        {
            if (wallInfo.gameObject != null)
            {
                var renderer = wallInfo.gameObject.GetComponent<MeshRenderer>();
                if (renderer != null && renderer.material != null)
                {
                    // Проверяем, нужно ли обновлять цвет
                    if (wallInfo.currentColor != wallInfo.targetColor)
                    {
                        // Плавный переход цвета
                        wallInfo.currentColor = Color.Lerp(
                            wallInfo.currentColor,
                            wallInfo.targetColor,
                            Time.deltaTime * colorTransitionSpeed
                        );
                        renderer.material.color = wallInfo.currentColor;
                    }
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

        Debug.Log($"Screen position: {screenPosition}, Mouse position: {Input.mousePosition}");

        // Получаем список плоскостей из WallDetectionManager
        var detectedWalls = wallDetectionManager.GetDetectedWalls();
        var planesList = new List<ARPlane>();
        foreach (var wallPair in detectedWalls)
        {
            var plane = planeManager.GetPlane(wallPair.Key);
            if (plane != null && plane.gameObject.activeInHierarchy)
            {
                planesList.Add(plane);
            }
        }
        
        Debug.Log($"Available planes: {planesList.Count}");
        foreach (var p in planesList)
        {
            Debug.Log($"Plane: ID={p.trackableId}, Position={p.transform.position}, Alignment={p.alignment}");
        }

        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        if (raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Debug.Log($"Raycast hit count: {hits.Count}");
            foreach (var hit in hits)
            {
                ARPlane plane = planeManager.GetPlane(hit.trackableId);
                if (plane != null)
                {
                    Debug.Log($"Hit plane - Position: {plane.transform.position}, Alignment: {plane.alignment}");
                    if (plane.alignment == PlaneAlignment.Vertical)
                    {
                        return plane;
                    }
                }
            }
        }
        else
        {
            // Попробуем другой тип raycast
            if (raycastManager.Raycast(screenPosition, hits, TrackableType.Planes))
            {
                Debug.Log($"General plane raycast hits: {hits.Count}");
            }
            else
            {
                Debug.Log($"No raycast hits. Screen dimensions: {Screen.width}x{Screen.height}");
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
        if (wallDetectionManager != null)
        {
            wallDetectionManager.OnWallDetected -= OnWallDetected;
        }

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

    private void OnWallDetected(ARPlane plane, GameObject wall)
    {
        // Добавляем стену в список покрашенных с начальным белым цветом
        if (!paintedWalls.ContainsKey(plane.trackableId))
        {
            var wallInfo = new PaintedWallInfo(wall, Color.white);
            paintedWalls.Add(plane.trackableId, wallInfo);
        }
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

    public Dictionary<TrackableId, GameObject> GetPaintedWalls()
    {
        var result = new Dictionary<TrackableId, GameObject>();
        foreach (var pair in paintedWalls)
        {
            result.Add(pair.Key, pair.Value.gameObject);
        }
        return result;
    }

    public void ClearWalls()
    {
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

    public void CreateWall(Vector3 position, Quaternion rotation, Vector3 scale, Color color)
    {
        var wall = new GameObject("PaintedWall");
        wall.transform.position = position;
        wall.transform.rotation = rotation;
        wall.transform.localScale = scale;

        var meshFilter = wall.AddComponent<MeshFilter>();
        var meshRenderer = wall.AddComponent<MeshRenderer>();
        meshRenderer.material = new Material(wallMaterial);
        meshRenderer.material.color = color;

        var wallInfo = new PaintedWallInfo(wall, color);
        paintedWalls.Add(new TrackableId(), wallInfo);
    }
}