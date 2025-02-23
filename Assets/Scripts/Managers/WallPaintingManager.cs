using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using System.Linq;
using Remalux.AR;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
    [SerializeField] private bool showDebugButtons = true; // Показывать ли отладочные кнопки
    
    private const float FORWARD_OFFSET = 0.005f; // Смещение вперед для предотвращения z-fighting
    private const int AR_LAYER = 10; // Номер слоя для AR объектов

    #if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void CreateARLayer()
    {
        // Проверяем, существует ли уже слой AR
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        SerializedProperty tags = tagManager.FindProperty("tags");

        // Проверяем и создаем слой AR
        bool layerExists = false;
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layerSP = layers.GetArrayElementAtIndex(i);
            if (layerSP.stringValue == "AR")
            {
                layerExists = true;
                break;
            }
        }

        // Если слой не существует, создаем его
        if (!layerExists)
        {
            SerializedProperty layerSP = layers.GetArrayElementAtIndex(AR_LAYER);
            if (string.IsNullOrEmpty(layerSP.stringValue))
            {
                layerSP.stringValue = "AR";
                Debug.Log("Слой AR успешно создан");
            }
        }

        // Проверяем и создаем тег Wall
        bool tagExists = false;
        for (int i = 0; i < tags.arraySize; i++)
        {
            SerializedProperty tagSP = tags.GetArrayElementAtIndex(i);
            if (tagSP.stringValue == "Wall")
            {
                tagExists = true;
                break;
            }
        }

        // Если тег не существует, добавляем его
        if (!tagExists)
        {
            tags.InsertArrayElementAtIndex(tags.arraySize);
            SerializedProperty newTag = tags.GetArrayElementAtIndex(tags.arraySize - 1);
            newTag.stringValue = "Wall";
            Debug.Log("Тег Wall успешно создан");
        }

        tagManager.ApplyModifiedProperties();
    }

    [CustomEditor(typeof(WallPaintingManager))]
    public class WallPaintingManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            WallPaintingManager manager = (WallPaintingManager)target;
            
            if (manager.showDebugButtons)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Отладочные функции", EditorStyles.boldLabel);
                
                if (GUILayout.Button("Создать тестовую комнату"))
                {
                    manager.CreateTestRoom();
                }
                
                if (GUILayout.Button("Очистить все стены"))
                {
                    manager.ClearWalls();
                }
            }
        }
    }
    #endif

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

        // Настраиваем AR Plane Manager для определения вертикальных поверхностей
        if (planeManager != null)
        {
            planeManager.requestedDetectionMode = PlaneDetectionMode.Vertical | PlaneDetectionMode.Horizontal;
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
        // Проверяем, есть ли вообще стены
        if (paintedWalls.Count == 0)
        {
            Debug.Log("UpdatePreview: Нет стен для определения");
            previewObject.SetActive(false);
            return;
        }

        Vector2 screenPosition = Input.mousePosition;
        if (Input.touchCount > 0)
        {
            screenPosition = Input.GetTouch(0).position;
        }

        // Пробуем найти стену через Physics.Raycast
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        RaycastHit hit;
        int layerMask = 1 << AR_LAYER;

        if (Physics.Raycast(ray, out hit, 100f, layerMask))
        {
            foreach (var pair in paintedWalls)
            {
                if (pair.Value.gameObject == hit.collider.gameObject)
                {
                    // Обновляем предпросмотр
                    previewObject.transform.position = hit.point;
                    previewObject.transform.rotation = hit.collider.gameObject.transform.rotation;
                    previewObject.transform.localScale = hit.collider.gameObject.transform.localScale;

                    // Устанавливаем цвет с прозрачностью
                    var previewRenderer = previewObject.GetComponent<MeshRenderer>();
                    Color previewColor = colorManager.GetCurrentColor();
                    previewColor.a = previewAlpha;
                    previewRenderer.material.color = previewColor;

                    previewObject.SetActive(true);
                    return;
                }
            }
        }

        previewObject.SetActive(false);
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
            Debug.Log($"HandleInput: Попытка взаимодействия. Позиция на экране: {screenPosition}");

            // Пробуем найти стену через Physics.Raycast
            Ray ray = Camera.main.ScreenPointToRay(screenPosition);
            RaycastHit[] hits = Physics.RaycastAll(ray);
            
            Debug.Log($"HandleInput: Отправка луча из {ray.origin} в направлении {ray.direction}");
            Debug.Log($"HandleInput: Найдено попаданий: {hits.Length}");

            // Сортируем попадания по расстоянию
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            // Ищем ближайшую стену
            foreach (var hit in hits)
            {
                Debug.Log($"HandleInput: Проверка попадания:");
                Debug.Log($"- Объект: {hit.collider.gameObject.name}");
                Debug.Log($"- Расстояние: {hit.distance}");
                Debug.Log($"- Layer: {hit.collider.gameObject.layer}");
                Debug.Log($"- Tag: {hit.collider.gameObject.tag}");

                if (hit.collider.gameObject.CompareTag("Wall"))
                {
                    Debug.Log($"HandleInput: Найдена ближайшая стена {hit.collider.gameObject.name} на расстоянии {hit.distance}");
                    
                    var meshRenderer = hit.collider.gameObject.GetComponent<MeshRenderer>();
                    if (meshRenderer != null)
                    {
                        Debug.Log($"HandleInput: MeshRenderer найден, текущий материал: {(meshRenderer.material != null ? meshRenderer.material.name : "null")}");
                        PaintWall(hit.collider.gameObject);
                        return; // Выходим после покраски ближайшей стены
                    }
                    else
                    {
                        Debug.LogError("HandleInput: MeshRenderer не найден на стене!");
                    }
                }
            }

            Debug.Log("HandleInput: Подходящих стен не найдено");
            // Выводим все объекты с тегом Wall для отладки
            GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
            Debug.Log($"HandleInput: Найдено стен в сцене: {walls.Length}");
            foreach (var wall in walls)
            {
                Debug.Log($"HandleInput: Стена {wall.name}: position={wall.transform.position}, rotation={wall.transform.rotation.eulerAngles}");
                var collider = wall.GetComponent<BoxCollider>();
                if (collider != null)
                {
                    Debug.Log($"HandleInput: Коллайдер стены {wall.name}: size={collider.size}, center={collider.center}, enabled={collider.enabled}");
                }
                var renderer = wall.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    Debug.Log($"HandleInput: Renderer стены {wall.name}: enabled={renderer.enabled}, материал={renderer.material.name}");
                }
            }
        }

        // Отмена последнего действия
        if (Input.GetKeyDown(KeyCode.Z) && (Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand)))
        {
            UndoLastAction();
        }
    }

    private void PaintWall(GameObject wall)
    {
        if (wall == null)
        {
            Debug.LogError("PaintWall: Передан null объект стены");
            return;
        }

        if (colorManager == null)
        {
            Debug.LogError("PaintWall: ColorManager не назначен");
            return;
        }

        Debug.Log($"PaintWall: Начало покраски стены {wall.name}");
        
        var renderer = wall.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            Debug.LogError($"PaintWall: MeshRenderer не найден на стене {wall.name}");
            return;
        }

        Color newColor = colorManager.GetCurrentColor();
        Debug.Log($"PaintWall: Новый цвет: {newColor}");

        // Создаем новый материал на основе wallMaterial
        Material newMaterial = new Material(wallMaterial);
        newMaterial.color = newColor;

        // Применяем материал
        renderer.material = newMaterial;

        // Сохраняем информацию о покраске в словаре
        foreach (var pair in paintedWalls)
        {
            if (pair.Value.gameObject == wall)
            {
                Debug.Log($"PaintWall: Обновляем информацию о стене в словаре {pair.Key}");
                pair.Value.targetColor = newColor;
                pair.Value.currentColor = newColor;

                // Добавляем действие в стек отмены
                undoStack.Push(new PaintAction(pair.Key, pair.Value.currentColor, newColor));
                if (undoStack.Count > MAX_UNDO_STEPS)
                {
                    undoStack = new Stack<PaintAction>(undoStack.Take(MAX_UNDO_STEPS));
                }
                break;
            }
        }

        Debug.Log($"PaintWall: Стена успешно покрашена. Текущий цвет материала: {renderer.material.color}");
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

        Debug.Log($"GetHitPlane: Проверка точки {screenPosition}");
        Debug.Log($"GetHitPlane: Количество стен в словаре: {paintedWalls.Count}");

        // Пробуем найти стену через Physics.Raycast
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        RaycastHit hit;
        int layerMask = 1 << AR_LAYER;

        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 1f);

        if (Physics.Raycast(ray, out hit, 100f, layerMask))
        {
            Debug.Log($"GetHitPlane: Найдено попадание в объект {hit.collider.gameObject.name}");
            Debug.Log($"GetHitPlane: Слой: {hit.collider.gameObject.layer}, Тег: {hit.collider.gameObject.tag}");
            
            // Ищем стену в словаре по GameObject
            foreach (var pair in paintedWalls)
            {
                if (pair.Value.gameObject == hit.collider.gameObject)
                {
                    Debug.Log($"GetHitPlane: Найдена стена в словаре с ID {pair.Key}");
                    currentHighlightedPlane = null; // Сбрасываем текущую подсвеченную плоскость
                    return null; // Возвращаем null, так как нам не нужен ARPlane для тестовой комнаты
                }
            }
        }
        else
        {
            Debug.Log("GetHitPlane: Physics.Raycast не нашел попаданий");
            // Выводим все объекты на слое AR для отладки
            GameObject[] arObjects = GameObject.FindGameObjectsWithTag("Wall");
            Debug.Log($"GetHitPlane: Найдено объектов с тегом Wall: {arObjects.Length}");
            foreach (var obj in arObjects)
            {
                Debug.Log($"GetHitPlane: Wall объект: {obj.name}, слой: {obj.layer}, тег: {obj.tag}");
            }
        }

        Debug.Log("GetHitPlane: Стена не найдена");
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
                
                // Создаем и настраиваем материал для прозрачности
                Material newMaterial = new Material(wallMaterial);
                newMaterial.SetFloat("_Surface", 1); // 0 = Opaque, 1 = Transparent
                newMaterial.SetFloat("_Blend", 0); // 0 = Alpha, 1 = Premultiply
                newMaterial.SetFloat("_ZWrite", 1); // Включаем запись в z-буфер
                newMaterial.SetFloat("_DstBlend", 10); // Blend One OneMinusSrcAlpha
                newMaterial.SetFloat("_SrcBlend", 1); // One
                newMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                newMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                newMaterial.renderQueue = 3000; // Transparent queue

                // Устанавливаем начальный цвет - полупрозрачный белый
                Color initialColor = new Color(1f, 1f, 1f, 0.5f);
                newMaterial.color = initialColor;

                // Применяем материал в зависимости от режима
                if (Application.isPlaying)
                {
                    renderer.material = newMaterial;
                }
                else
                {
                    renderer.sharedMaterial = newMaterial;
                }

                // Добавляем или обновляем MeshCollider
                var meshCollider = wall.GetComponent<MeshCollider>();
                if (meshCollider == null)
                {
                    meshCollider = wall.AddComponent<MeshCollider>();
                }
                meshCollider.sharedMesh = meshFilter.mesh;
                meshCollider.convex = true;
                meshCollider.isTrigger = false;

                // Устанавливаем слой AR и тег
                wall.layer = AR_LAYER;
                wall.tag = "Wall";
                
                Debug.Log($"Материал создан. Цвет: {(Application.isPlaying ? renderer.material.color : renderer.sharedMaterial.color)}, " +
                         $"слой: {wall.layer}, тег: {wall.tag}");
            }
            else
            {
                Debug.LogError("MeshRenderer или MeshFilter не найден на новой стене");
            }

            var wallInfo = new PaintedWallInfo(wall, new Color(1f, 1f, 1f, 0.5f));
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
        Debug.Log($"ClearWalls: Начало очистки стен. Количество стен: {paintedWalls.Count}");
        
        // Сначала найдем все стены в сцене по тегу
        GameObject[] wallsInScene = GameObject.FindGameObjectsWithTag("Wall");
        Debug.Log($"ClearWalls: Найдено стен в сцене: {wallsInScene.Length}");
        
        // Удаляем все стены из сцены
        foreach (var wall in wallsInScene)
        {
            if (wall != null)
            {
                Debug.Log($"ClearWalls: Удаление стены из сцены: {wall.name}");
                if (Application.isPlaying)
                {
                    Destroy(wall);
                }
                else
                {
                    DestroyImmediate(wall);
                }
            }
        }

        // Очищаем словарь и стек
        paintedWalls.Clear();
        undoStack.Clear();
        
        Debug.Log("ClearWalls: Все стены очищены");

        // Дополнительная проверка
        wallsInScene = GameObject.FindGameObjectsWithTag("Wall");
        if (wallsInScene.Length > 0)
        {
            Debug.LogWarning($"ClearWalls: После очистки все еще остались стены: {wallsInScene.Length}");
        }
    }

    public void CreateWall(Vector3 position, Quaternion rotation, Vector3 scale, Color color)
    {
        Debug.Log($"Создание новой стены: позиция={position}, размер={scale}, поворот={rotation.eulerAngles}");
        
        var wall = new GameObject("PaintedWall");
        wall.transform.position = position;
        wall.transform.rotation = rotation;
        wall.transform.localScale = scale;

        var meshFilter = wall.AddComponent<MeshFilter>();
        var meshRenderer = wall.AddComponent<MeshRenderer>();
        
        // Создаем меш для стены
        var mesh = new Mesh();
        var vertices = new Vector3[4];
        var triangles = new int[] { 0, 1, 2, 2, 1, 3 };
        var uvs = new Vector2[4];

        // Создаем вершины для прямоугольной стены (в локальных координатах)
        vertices[0] = new Vector3(-0.5f, -0.5f, 0);
        vertices[1] = new Vector3(-0.5f, 0.5f, 0);
        vertices[2] = new Vector3(0.5f, -0.5f, 0);
        vertices[3] = new Vector3(0.5f, 0.5f, 0);

        // UV координаты
        uvs[0] = new Vector2(0, 0);
        uvs[1] = new Vector2(0, 1);
        uvs[2] = new Vector2(1, 0);
        uvs[3] = new Vector2(1, 1);

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;
        
        // Устанавливаем слой AR и тег
        wall.layer = AR_LAYER;
        wall.tag = "Wall";

        Debug.Log($"Стена настроена: Layer={wall.layer}, Tag={wall.tag}");

        // Добавляем BoxCollider с правильными размерами
        var boxCollider = wall.AddComponent<BoxCollider>();
        boxCollider.size = new Vector3(1f, 1f, 0.1f);
        boxCollider.center = Vector3.zero;
        boxCollider.isTrigger = false;
        
        Debug.Log($"Коллайдер настроен: Size={boxCollider.size}, Center={boxCollider.center}, IsTrigger={boxCollider.isTrigger}");

        // Создаем и настраиваем материал
        Material newMaterial = new Material(wallMaterial);
        newMaterial.SetFloat("_Surface", 1); // 0 = Opaque, 1 = Transparent
        newMaterial.SetFloat("_Blend", 0); // 0 = Alpha, 1 = Premultiply
        newMaterial.SetFloat("_ZWrite", 1); // Включаем запись в z-буфер
        newMaterial.SetFloat("_DstBlend", 10); // Blend One OneMinusSrcAlpha
        newMaterial.SetFloat("_SrcBlend", 1); // One
        newMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        newMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        newMaterial.renderQueue = 3000; // Transparent queue
        newMaterial.color = color;

        meshRenderer.material = newMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        
        var wallInfo = new PaintedWallInfo(wall, color);
        var trackableId = new TrackableId(
            (ulong)Random.Range(1, long.MaxValue), 
            (ulong)Random.Range(1, long.MaxValue)
        );
        
        paintedWalls.Add(trackableId, wallInfo);

        Debug.Log($"Стена создана: ID={trackableId}, Transform=(pos={wall.transform.position}, rot={wall.transform.rotation.eulerAngles}, scale={wall.transform.localScale})");
        Debug.Log($"Стена добавлена в словарь paintedWalls. Текущее количество стен: {paintedWalls.Count}");
    }

    [ContextMenu("Создать тестовую комнату")]
    public void CreateTestRoom()
    {
        Debug.Log("Начало создания тестовой комнаты");
        
        // Очищаем существующие стены
        ClearWalls();

        float roomWidth = 4f;
        float roomLength = 6f;
        float wallHeight = 2.5f;
        float wallThickness = 0.1f;
        Color initialColor = new Color(1f, 1f, 1f, 0.5f);

        Debug.Log($"Параметры комнаты: ширина={roomWidth}, длина={roomLength}, высота={wallHeight}");

        // Передняя стена
        CreateWall(
            new Vector3(0, wallHeight/2, roomLength/2),
            Quaternion.Euler(0, 180, 0),
            new Vector3(roomWidth, wallHeight, wallThickness),
            initialColor
        );

        // Задняя стена
        CreateWall(
            new Vector3(0, wallHeight/2, -roomLength/2),
            Quaternion.Euler(0, 0, 0),
            new Vector3(roomWidth, wallHeight, wallThickness),
            initialColor
        );

        // Левая стена
        CreateWall(
            new Vector3(-roomWidth/2, wallHeight/2, 0),
            Quaternion.Euler(0, 90, 0),
            new Vector3(roomLength, wallHeight, wallThickness),
            initialColor
        );

        // Правая стена
        CreateWall(
            new Vector3(roomWidth/2, wallHeight/2, 0),
            Quaternion.Euler(0, -90, 0),
            new Vector3(roomLength, wallHeight, wallThickness),
            initialColor
        );

        Debug.Log($"Тестовая комната создана. Количество стен: {paintedWalls.Count}");

        // Проверяем созданные стены
        GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
        Debug.Log($"Найдено стен в сцене: {walls.Length}");
        
        foreach (var wall in walls)
        {
            var collider = wall.GetComponent<BoxCollider>();
            var renderer = wall.GetComponent<MeshRenderer>();
            Debug.Log($"Проверка стены {wall.name}:");
            Debug.Log($"- Position: {wall.transform.position}");
            Debug.Log($"- Rotation: {wall.transform.rotation.eulerAngles}");
            Debug.Log($"- Scale: {wall.transform.localScale}");
            Debug.Log($"- Layer: {wall.layer}");
            Debug.Log($"- Tag: {wall.tag}");
            if (collider != null)
            {
                Debug.Log($"- Collider: size={collider.size}, center={collider.center}, enabled={collider.enabled}");
            }
            if (renderer != null)
            {
                Debug.Log($"- Renderer: enabled={renderer.enabled}, material={renderer.material.name}, color={renderer.material.color}");
            }
        }

        // Проверяем словарь стен
        Debug.Log("Проверка словаря paintedWalls:");
        foreach (var pair in paintedWalls)
        {
            Debug.Log($"- ID: {pair.Key}");
            Debug.Log($"  GameObject: {pair.Value.gameObject.name}");
            Debug.Log($"  Current Color: {pair.Value.currentColor}");
            Debug.Log($"  Target Color: {pair.Value.targetColor}");
        }
    }
}