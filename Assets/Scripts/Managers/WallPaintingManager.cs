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
    
    private const float FORWARD_OFFSET = 0.005f; // Смещение вперед для предотвращения z-fighting

    private Dictionary<TrackableId, PaintedWallInfo> paintedWalls = new Dictionary<TrackableId, PaintedWallInfo>();
    private ARPlane currentHighlightedPlane;
    private GameObject previewObject;
    private Stack<PaintAction> undoStack = new Stack<PaintAction>();
    private const int MAX_UNDO_STEPS = 20;

    void Awake()
    {
        // Убираем лишние логи
    }

    void OnEnable()
    {
        // Убираем лишние логи
    }

    void Start()
    {
        Debug.Log($"ColorManager текущий цвет при старте: {(colorManager != null ? colorManager.GetCurrentColor().ToString() : "ColorManager is null")}");

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
        Debug.Log($"UpdatePreview: результат GetHitPlane: {(hitPlane != null ? hitPlane.trackableId.ToString() : "null")}");

        if (hitPlane != null && hitPlane.alignment == PlaneAlignment.Vertical)
        {
            if (currentHighlightedPlane != hitPlane)
            {
                Debug.Log($"UpdatePreview: Смена подсвеченной плоскости с {(currentHighlightedPlane != null ? currentHighlightedPlane.trackableId.ToString() : "null")} на {hitPlane.trackableId}");
                currentHighlightedPlane = hitPlane;
                UpdatePreviewMesh(hitPlane);
            }
            previewObject.SetActive(true);
        }
        else
        {
            if (currentHighlightedPlane != null)
            {
                Debug.Log("UpdatePreview: Сброс подсвеченной плоскости");
            }
            currentHighlightedPlane = null;
            previewObject.SetActive(false);
        }
    }

    private void UpdatePreviewMesh(ARPlane plane)
    {
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
        float highestY = float.MinValue;

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
            highestY = Mathf.Max(highestY, worldPoint.y);
        }

        // Вычисляем реальную ширину стены
        float width = (maxX - minX) * WallDetectionManager.WALL_EXTENSION;
        
        // Вычисляем реальную высоту стены, но не больше максимальной
        float height = Mathf.Min(highestY - lowestY, WallDetectionManager.WALL_HEIGHT);

        // Создаем вершины
        vertices[0] = new Vector3(-width/2, WallDetectionManager.WALL_BOTTOM_OFFSET, 0);
        vertices[1] = new Vector3(-width/2, height, 0);
        vertices[2] = new Vector3(width/2, WallDetectionManager.WALL_BOTTOM_OFFSET, 0);
        vertices[3] = new Vector3(width/2, height, 0);

        // UV координаты
        float tileScale = 1f;
        uvs[0] = new Vector2(0, 0);
        uvs[1] = new Vector2(0, height * tileScale);
        uvs[2] = new Vector2(width * tileScale, 0);
        uvs[3] = new Vector2(width * tileScale, height * tileScale);

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
        previewObject.transform.position += planeNormal * FORWARD_OFFSET;

        // Устанавливаем цвет с прозрачностью
        Color previewColor = colorManager.GetCurrentColor();
        previewColor.a = previewAlpha;
        previewRenderer.material.color = previewColor;
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            Vector2 screenPosition = Input.mousePosition;
            if (Input.touchCount > 0)
            {
                screenPosition = Input.GetTouch(0).position;
            }
            Debug.Log($"Попытка взаимодействия. Позиция на экране: {screenPosition}");

            var hitPlane = GetHitPlane();
            Debug.Log($"Результат GetHitPlane: {(hitPlane != null ? hitPlane.trackableId.ToString() : "null")}");
            
            if (currentHighlightedPlane != null)
            {
                Debug.Log($"Текущая подсвеченная плоскость: {currentHighlightedPlane.trackableId}");
                Debug.Log($"Текущий цвет для покраски: {colorManager.GetCurrentColor()}");
                PaintWall(currentHighlightedPlane);
            }
            else
            {
                Debug.Log("Нет подсвеченной плоскости для покраски");
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
        Debug.Log($"Попытка покраски стены {plane.trackableId}. Новый цвет: {newColor}");

        if (paintedWalls.TryGetValue(plane.trackableId, out var wallInfo))
        {
            Debug.Log($"Стена найдена. Текущий цвет: {wallInfo.currentColor}, Целевой цвет: {wallInfo.targetColor}");

            // Сохраняем действие для отмены
            undoStack.Push(new PaintAction(plane.trackableId, wallInfo.currentColor, newColor));
            if (undoStack.Count > MAX_UNDO_STEPS)
            {
                undoStack = new Stack<PaintAction>(undoStack.Take(MAX_UNDO_STEPS));
            }

            // Применяем новый цвет
            wallInfo.targetColor = newColor;
            wallInfo.currentColor = newColor;

            // Обновляем материал стены
            var renderer = wallInfo.gameObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Debug.Log($"Обновляем материал стены. Текущий цвет материала: {(renderer.material != null ? renderer.material.color.ToString() : "null")}");
                
                if (renderer.material == null || renderer.material.color != wallInfo.currentColor)
                {
                    Debug.Log("Создаем новый материал для стены");
                    renderer.material = new Material(wallMaterial);
                    renderer.material.color = newColor;
                }
                else
                {
                    Debug.Log("Обновляем цвет существующего материала");
                    renderer.material.color = newColor;
                }
                
                Debug.Log($"Новый цвет материала: {renderer.material.color}");
            }
            else
            {
                Debug.LogError("MeshRenderer не найден на стене");
            }
        }
        else
        {
            Debug.LogError($"Стена {plane.trackableId} не найдена в списке покрашенных стен");
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
                    if (!wallInfo.currentColor.Equals(wallInfo.targetColor))
                    {
                        Debug.Log($"Обновление цвета стены. Текущий: {wallInfo.currentColor}, Целевой: {wallInfo.targetColor}");
                        
                        // Плавный переход цвета
                        wallInfo.currentColor = Color.Lerp(
                            wallInfo.currentColor,
                            wallInfo.targetColor,
                            Time.deltaTime * colorTransitionSpeed
                        );
                        
                        // Принудительно обновляем цвет материала
                        if (renderer.material.color != wallInfo.currentColor)
                        {
                            renderer.material.color = wallInfo.currentColor;
                            Debug.Log($"Цвет материала обновлен: {renderer.material.color}");
                        }
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

        Debug.Log($"GetHitPlane: Проверка точки {screenPosition}, Размер экрана: {Screen.width}x{Screen.height}, " +
                  $"Ориентация: {Screen.orientation}");

        // В портретной ориентации проверяем координаты относительно повернутого экрана
        if (Screen.orientation == ScreenOrientation.Portrait || 
            Screen.orientation == ScreenOrientation.PortraitUpsideDown)
        {
            // В портретном режиме меняем местами проверку координат:
            // x проверяем относительно ширины (Screen.width)
            // y проверяем относительно высоты (Screen.height)
            bool isXValid = screenPosition.x >= 0 && screenPosition.x < Screen.width;
            bool isYValid = screenPosition.y >= 0 && screenPosition.y < Screen.height;
            
            Debug.Log($"Портретная ориентация - Проверка границ:" +
                      $"\nX координата: {screenPosition.x} {'<'} {Screen.width} = {isXValid}" +
                      $"\nY координата: {screenPosition.y} {'<'} {Screen.height} = {isYValid}");

            if (!isXValid || !isYValid)
            {
                Debug.Log($"GetHitPlane: Точка {screenPosition} находится за пределами экрана {Screen.width}x{Screen.height} " +
                         $"в портретной ориентации");
                return null;
            }
        }
        else
        {
            bool isXValid = screenPosition.x >= 0 && screenPosition.x < Screen.width;
            bool isYValid = screenPosition.y >= 0 && screenPosition.y < Screen.height;
            
            Debug.Log($"Альбомная ориентация - Проверка границ:" +
                      $"\nX координата: {screenPosition.x} {'<'} {Screen.width} = {isXValid}" +
                      $"\nY координата: {screenPosition.y} {'<'} {Screen.height} = {isYValid}");

            if (!isXValid || !isYValid)
            {
                Debug.Log($"GetHitPlane: Точка {screenPosition} находится за пределами экрана {Screen.width}x{Screen.height} " +
                         $"в альбомной ориентации");
                return null;
            }
        }

        // Преобразуем координаты для raycast в зависимости от ориентации
        Vector2 raycastPosition = screenPosition;
        if (Screen.orientation == ScreenOrientation.Portrait || 
            Screen.orientation == ScreenOrientation.PortraitUpsideDown)
        {
            raycastPosition.x = screenPosition.y;
            raycastPosition.y = Screen.width - screenPosition.x;
            Debug.Log($"GetHitPlane: Преобразованные координаты для raycast: {raycastPosition}");
        }

        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        
        // Расширяем типы отслеживаемых поверхностей
        TrackableType trackableTypes = TrackableType.PlaneWithinPolygon | 
                                     TrackableType.PlaneEstimated |
                                     TrackableType.FeaturePoint;

        bool hasHits = raycastManager.Raycast(raycastPosition, hits, trackableTypes);
        Debug.Log($"Raycast результат: {hasHits}, количество попаданий: {hits.Count}, " +
                  $"типы поиска: {trackableTypes}, " +
                  $"позиция для raycast: {raycastPosition}");

        // Проверяем состояние AR системы
        if (planeManager != null && planeManager.subsystem != null)
        {
            Debug.Log($"AR система: активна={planeManager.subsystem.running}, " +
                     $"режим определения={planeManager.requestedDetectionMode}, " +
                     $"количество плоскостей={planeManager.trackables.count}");
        }

        if (hasHits)
        {
            // Сначала ищем вертикальные плоскости
            foreach (var hit in hits)
            {
                ARPlane plane = planeManager.GetPlane(hit.trackableId);
                Debug.Log($"Проверка плоскости {hit.trackableId}, " +
                         $"plane null? {plane == null}, " +
                         $"alignment={plane?.alignment}, " +
                         $"normal={plane?.normal}, " +
                         $"classifications={plane?.classifications}, " +
                         $"размер={plane?.size}");
                
                if (plane != null)
                {
                    // Проверяем, является ли плоскость частью обнаруженной стены
                    bool isDetectedWall = wallDetectionManager != null && 
                                        wallDetectionManager.GetDetectedWalls().ContainsKey(plane.trackableId);

                    if (isDetectedWall)
                    {
                        Debug.Log($"Найдена обнаруженная стена: {plane.trackableId}");
                        return plane;
                    }

                    if (plane.alignment == PlaneAlignment.Vertical)
                    {
                        // Находим крайние точки в мировых координатах
                        float lowestY = float.MaxValue;
                        float highestY = float.MinValue;

                        foreach (var point in plane.boundary)
                        {
                            Vector3 worldPoint = plane.transform.TransformPoint(new Vector3(point.x, 0, point.y));
                            lowestY = Mathf.Min(lowestY, worldPoint.y);
                            highestY = Mathf.Max(highestY, worldPoint.y);
                        }

                        float wallHeight = Mathf.Min(highestY - lowestY, WallDetectionManager.WALL_HEIGHT);
                        float hitHeight = hit.pose.position.y - lowestY;

                        Debug.Log($"Параметры стены: lowestY={lowestY:F3}, highestY={highestY:F3}, " +
                                $"wallHeight={wallHeight:F3}, hitHeight={hitHeight:F3}, " +
                                $"расстояние до камеры={hit.distance:F3}м");
                        
                        if (hitHeight >= WallDetectionManager.WALL_BOTTOM_OFFSET && hitHeight <= wallHeight)
                        {
                            Debug.Log($"Найдена подходящая вертикальная плоскость: {plane.trackableId}");
                            return plane;
                        }
                    }
                }
            }

            // Если вертикальные плоскости не найдены, проверяем все остальные
            foreach (var hit in hits)
            {
                ARPlane plane = planeManager.GetPlane(hit.trackableId);
                if (plane != null)
                {
                    float angleFromVertical = Vector3.Angle(plane.normal, Vector3.up);
                    Debug.Log($"Проверка угла плоскости: {angleFromVertical}° от вертикали, " +
                             $"расстояние={hit.distance:F3}м");
                    
                    // Если плоскость близка к вертикальной (в пределах 30 градусов)
                    if (Mathf.Abs(angleFromVertical - 90f) <= 30f)
                    {
                        Debug.Log($"Найдена почти вертикальная плоскость: {plane.trackableId}");
                        return plane;
                    }
                }
            }
        }

        Debug.Log("GetHitPlane: Подходящая плоскость не найдена");
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
        Debug.Log($"Обнаружена новая стена: {plane.trackableId}");
        if (!paintedWalls.ContainsKey(plane.trackableId))
        {
            var meshFilter = wall.GetComponent<MeshFilter>();
            var renderer = wall.GetComponent<MeshRenderer>();
            if (renderer != null && meshFilter != null)
            {
                Debug.Log("Инициализация материала для новой стены");
                // Создаем новый материал для стены
                renderer.material = new Material(wallMaterial);
                renderer.material.color = Color.white;

                // Обновляем меш стены с правильными размерами
                UpdateWallMesh(meshFilter, plane);
                
                Debug.Log($"Материал создан. Цвет: {renderer.material.color}");
            }
            else
            {
                Debug.LogError("MeshRenderer или MeshFilter не найден на новой стене");
            }

            var wallInfo = new PaintedWallInfo(wall, Color.white);
            paintedWalls.Add(plane.trackableId, wallInfo);
            Debug.Log($"Стена добавлена в список покрашенных. ID: {plane.trackableId}");
        }
    }

    private void UpdateWallMesh(MeshFilter meshFilter, ARPlane plane)
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

        float width = (maxX - minX) * WallDetectionManager.WALL_EXTENSION;
        float height = Mathf.Min(highestY - lowestY, WallDetectionManager.WALL_HEIGHT);

        var mesh = new Mesh();
        var vertices = new Vector3[4];
        var triangles = new int[] { 0, 1, 2, 2, 1, 3 };
        var uvs = new Vector2[4];

        // Создаем вершины для прямоугольной стены
        vertices[0] = new Vector3(-width/2, WallDetectionManager.WALL_BOTTOM_OFFSET, 0);
        vertices[1] = new Vector3(-width/2, height, 0);
        vertices[2] = new Vector3(width/2, WallDetectionManager.WALL_BOTTOM_OFFSET, 0);
        vertices[3] = new Vector3(width/2, height, 0);

        // UV координаты для правильного масштабирования текстуры
        float tileScale = 1f;
        uvs[0] = new Vector2(0, 0);
        uvs[1] = new Vector2(0, height * tileScale);
        uvs[2] = new Vector2(width * tileScale, 0);
        uvs[3] = new Vector2(width * tileScale, height * tileScale);

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;
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