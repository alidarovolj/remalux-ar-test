using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Unity.XR.CoreUtils;
using Remalux.AR.Utilities;

namespace Remalux.AR
{
      /// <summary>
      /// Контроллер видимости AR плоскостей
      /// </summary>
      public class ARPlaneVisibilityController : MonoBehaviour
      {
            [Header("AR компоненты")]
            [SerializeField] public ARPlaneManager planeManager;
            [SerializeField] private ARRaycastManager raycastManager;

            [Header("Внешний вид")]
            [SerializeField] public Material wallMaterial;
            [SerializeField] public Material floorMaterial;
            [SerializeField] private float planeAlpha = 0.8f;

            [Header("Настройки")]
            [SerializeField] private bool autoActivatePlanes = true;
            [SerializeField] private bool highlightVerticalPlanes = true;
            [System.NonSerialized]
            [SerializeField] private bool hideOnStart = false;

            public delegate void WallDetectedHandler(ARPlane wall);
            public delegate void WallReadyHandler(GameObject wall);
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0067:Unused event", Justification = "Used by external components")]
            public event WallDetectedHandler onWallDetected;
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0067:Unused event", Justification = "Used by external components")]
            public event WallReadyHandler onWallReady;

            private Dictionary<TrackableId, Material> originalMaterials = new Dictionary<TrackableId, Material>();
            private List<ARPlane> verticalPlanes = new List<ARPlane>();
            private List<ARPlane> horizontalPlanes = new List<ARPlane>();
            private Dictionary<TrackableId, GameObject> planeVisualizations = new Dictionary<TrackableId, GameObject>();

            private Transform _demoSurfacesParent;
            private Material _cachedWallMaterial;
            private Material _cachedFloorMaterial;
            private bool _surfacesCreated = false;
            private Dictionary<string, Vector3> _initialPositions = new Dictionary<string, Vector3>();

            /// <summary>
            /// Простой компонент для имитации ARPlane
            /// </summary>
            public class ARPlaneSimulation : MonoBehaviour
            {
                  public bool isVertical = true;
                  public string planeId = System.Guid.NewGuid().ToString();
                  public Vector2 size;
            }

            /// <summary>
            /// Компонент для отслеживания изменений AR плоскости
            /// </summary>
            public class ARPlaneTracker : MonoBehaviour
            {
                  public ARPlane AttachedPlane { get; set; }
                  private Vector3 _initialLocalPosition;
                  private Quaternion _initialLocalRotation;

                  private void Start()
                  {
                        if (AttachedPlane != null)
                        {
                              _initialLocalPosition = transform.localPosition;
                              _initialLocalRotation = transform.localRotation;
                              AttachedPlane.boundaryChanged += OnPlaneBoundaryChanged;
                        }
                  }

                  private void OnPlaneBoundaryChanged(ARPlaneBoundaryChangedEventArgs args)
                  {
                        // Обновляем позицию при изменении границ плоскости
                        transform.localPosition = _initialLocalPosition;
                        transform.localRotation = _initialLocalRotation;
                  }

                  private void OnDestroy()
                  {
                        if (AttachedPlane != null)
                        {
                              AttachedPlane.boundaryChanged -= OnPlaneBoundaryChanged;
                        }
                  }
            }

            /// <summary>
            /// Компонент для сохранения фиксированной позиции в AR пространстве
            /// </summary>
            public class FixedPositionKeeper : MonoBehaviour
            {
                  private Vector3 _fixedWorldPosition;
                  private Quaternion _fixedWorldRotation;
                  private bool _initialized = false;
                  private int _frameCount = 0;
                  private static readonly WaitForSeconds _initDelay = new WaitForSeconds(0.5f);
                  private ARAnchor _anchor;
                  private ARSession _arSession;
                  private ARPlane _attachedPlane;

                  void Start()
                  {
                        _arSession = FindFirstObjectByType<ARSession>();
                        StartCoroutine(DelayedInit());
                  }

                  private IEnumerator DelayedInit()
                  {
                        yield return _initDelay;
                        _fixedWorldPosition = transform.position;
                        _fixedWorldRotation = transform.rotation;
                        _initialized = true;
                  }

                  void LateUpdate()
                  {
                        if (!_initialized) return;

                        // Сохраняем позицию и поворот
                        transform.position = _fixedWorldPosition;
                        transform.rotation = _fixedWorldRotation;
                  }
            }

            /// <summary>
            /// Данные стены, полученные от OpenCV
            /// </summary>
            [System.Serializable]
            public class OpenCVWallData
            {
                  public Vector3 position;
                  public Vector3 normal;
                  public Vector2 size;
                  public float confidence;
                  public int wallId;

                  public OpenCVWallData(Vector3 position, Vector3 normal, Vector2 size, float confidence, int wallId = -1)
                  {
                        this.position = position;
                        this.normal = normal;
                        this.size = size;
                        this.confidence = confidence;
                        this.wallId = wallId;
                  }
            }

            /// <summary>
            /// Идентификатор стены
            /// </summary>
            public class WallIdentifier : MonoBehaviour
            {
                  public OpenCVWallData wallData;
                  public bool isBeingPainted = false;

                  public void SetWallData(OpenCVWallData data)
                  {
                        wallData = data;
                  }
            }

            /// <summary>
            /// Метод для привязки объекта к AR плоскости
            /// </summary>
            private void AttachToARPlane(GameObject obj, ARPlane plane, Vector3 position, Quaternion rotation)
            {
                  if (obj == null || plane == null) return;

                  try
                  {
                        // Находим ARAnchorManager
                        ARAnchorManager anchorManager = FindFirstObjectByType<ARAnchorManager>();
                        if (anchorManager != null)
                        {
                              // Создаем якорь, привязанный к AR плоскости
                              Pose anchorPose = new Pose(position, rotation);
                              ARAnchor anchor = anchorManager.AttachAnchor(plane, anchorPose);

                              if (anchor != null)
                              {
                                    // Привязываем объект к якорю
                                    obj.transform.parent = anchor.transform;
                                    obj.transform.localPosition = Vector3.zero;
                                    obj.transform.localRotation = Quaternion.identity;
                              }
                        }
                        else
                        {
                              // Fallback: создаем простой AR якорь, если не найден ARAnchorManager
                              var anchor = obj.AddComponent<ARAnchor>();
                              anchor.transform.position = position;
                              anchor.transform.rotation = rotation;
                              obj.transform.parent = anchor.transform;
                              obj.transform.localPosition = Vector3.zero;
                        }

                        // Добавляем компонент для отслеживания изменений плоскости
                        var planeTracker = obj.AddComponent<ARPlaneTracker>();
                        planeTracker.AttachedPlane = plane;

                        // Добавляем FixedPositionKeeper для сохранения позиции
                        if (!obj.TryGetComponent<FixedPositionKeeper>(out _))
                        {
                              var keeper = obj.AddComponent<FixedPositionKeeper>();
                        }

                        Debug.Log($"Объект {obj.name} успешно привязан к AR плоскости {plane.trackableId}");
                  }
                  catch (System.Exception ex)
                  {
                        Debug.LogError($"Ошибка при привязке объекта к AR плоскости: {ex.Message}\n{ex.StackTrace}");
                  }
            }

            public void UpdatePlaneVisibility(ARPlane plane)
            {
                  if (plane == null) return;
                  UpdatePlaneVisualization(plane);
            }

            public void SetPlaneVisibility(bool visible)
            {
                  if (planeManager == null)
                  {
                        Debug.LogWarning("ARPlaneManager is null in SetPlaneVisibility");
                        return;
                  }

                  if (planeManager.trackables == null)
                  {
                        Debug.LogWarning("Trackables is null in SetPlaneVisibility");
                        return;
                  }

                  foreach (var plane in planeManager.trackables)
                  {
                        if (plane != null)
                        {
                              UpdatePlaneVisibility(plane);
                        }
                  }
            }

            public void SetHighlightWalls(bool highlight)
            {
                  highlightVerticalPlanes = highlight;
                  UpdateAllPlanesVisibility();
            }

            public void ResetAllPlanes()
            {
                  foreach (var plane in planeManager.trackables)
                  {
                        if (plane.gameObject != null)
                        {
                              Destroy(plane.gameObject);
                        }
                  }
            }

            public void ForceShowAllPlanes()
            {
                  foreach (var plane in planeManager.trackables)
                  {
                        UpdatePlaneVisibility(plane);
                  }
            }

            public bool IsVerticalPlane(ARPlane plane)
            {
                  if (plane == null) return false;
                  return plane.alignment == PlaneAlignment.Vertical;
            }

            public bool CanInteractWithPoint(Vector2 screenPosition)
            {
                  List<ARRaycastHit> hits = new List<ARRaycastHit>();
                  if (raycastManager.Raycast(screenPosition, hits, TrackableType.Planes))
                  {
                        foreach (var hit in hits)
                        {
                              var plane = planeManager.GetPlane(hit.trackableId);
                              if (plane != null && IsVerticalPlane(plane))
                              {
                                    return true;
                              }
                        }
                  }
                  return false;
            }

            public ARPlane GetPlaneByScreenPosition(Vector2 screenPosition, ARRaycastManager raycastManager, Camera arCamera)
            {
                  List<ARRaycastHit> hits = new List<ARRaycastHit>();
                  if (raycastManager.Raycast(screenPosition, hits, TrackableType.Planes))
                  {
                        foreach (var hit in hits)
                        {
                              var plane = planeManager.GetPlane(hit.trackableId);
                              if (plane != null && IsVerticalPlane(plane))
                              {
                                    return plane;
                              }
                        }
                  }
                  return null;
            }

            public void ProcessOpenCVWalls(List<OpenCVWallData> walls)
            {
                  foreach (var wallData in walls)
                  {
                        CreateWallFromOpenCVData(wallData.position, wallData.normal, wallData.size, wallData.confidence);
                  }
            }

            private GameObject CreateWallFromOpenCVData(Vector3 position, Vector3 normal, Vector2 size, float confidence)
            {
                  GameObject wall = CreateDefaultPaintableSurface(size.x, size.y, position, Color.white, $"OpenCV_Wall_{System.Guid.NewGuid()}");
                  if (wall != null)
                  {
                        var wallIdentifier = wall.AddComponent<WallIdentifier>();
                        wallIdentifier.SetWallData(new OpenCVWallData(position, normal, size, confidence));
                  }
                  return wall;
            }

            public void CreateDemoPaintableSurfaces()
            {
                  if (_surfacesCreated) return;
                  _surfacesCreated = true;

                  // Создаем демо-стены
                  Vector3[] positions = new Vector3[]
                  {
                        new Vector3(0, 0, 2),
                        new Vector3(2, 0, 0),
                        new Vector3(-2, 0, 0)
                  };

                  foreach (var pos in positions)
                  {
                        CreateWallAtPosition(pos, Quaternion.LookRotation(pos), new Vector3(2, 2, 1), Color.white, $"Demo_Wall_{System.Guid.NewGuid()}");
                  }
            }

            public GameObject CreatePaintableSurfaceAtScreenPoint(Vector2 screenPosition, float width = 2.0f, float height = 1.5f)
            {
                  List<ARRaycastHit> hits = new List<ARRaycastHit>();
                  if (raycastManager.Raycast(screenPosition, hits, TrackableType.Planes))
                  {
                        foreach (var hit in hits)
                        {
                              var plane = planeManager.GetPlane(hit.trackableId);
                              if (plane != null && IsVerticalPlane(plane))
                              {
                                    Vector3 position = hit.pose.position;
                                    Quaternion rotation = hit.pose.rotation;
                                    return CreateDefaultPaintableSurface(width, height, position, Color.white, $"Surface_{System.Guid.NewGuid()}");
                              }
                        }
                  }
                  return null;
            }

            public GameObject GetPlaneVisualization(TrackableId planeId)
            {
                  if (planeVisualizations.TryGetValue(planeId, out GameObject visualization))
                  {
                        return visualization;
                  }
                  return null;
            }

            public GameObject GetPlaneVisualizationByName(ARPlane plane)
            {
                  if (plane == null) return null;
                  string visualName = $"CustomVisual_{plane.trackableId}";
                  return plane.transform.Find(visualName)?.gameObject;
            }

            private void UpdatePlaneVisualization(ARPlane plane)
            {
                  if (plane == null)
                  {
                        Debug.LogWarning("UpdatePlaneVisualization: plane is null");
                        return;
                  }

                  Debug.Log($"Updating visualization for plane {plane.trackableId}");

                  // Получаем или создаем визуализацию плоскости
                  GameObject visualization;
                  if (!planeVisualizations.TryGetValue(plane.trackableId, out visualization) || visualization == null)
                  {
                        // Проверяем, не существует ли уже визуализация с таким именем
                        string visualName = $"CustomVisual_{plane.trackableId}";
                        Transform existingVisual = plane.transform.Find(visualName);
                        if (existingVisual != null)
                        {
                              visualization = existingVisual.gameObject;
                              planeVisualizations[plane.trackableId] = visualization;
                              Debug.Log($"Found existing visualization for plane {plane.trackableId}");
                        }
                        else
                        {
                              visualization = new GameObject(visualName);
                              visualization.transform.SetParent(plane.transform, false);
                              visualization.transform.localPosition = Vector3.zero;
                              visualization.transform.localRotation = Quaternion.identity;
                              visualization.transform.localScale = Vector3.one;
                              planeVisualizations[plane.trackableId] = visualization;
                              Debug.Log($"Created new visualization for plane {plane.trackableId}");
                        }
                  }

                  // Проверяем, что визуализация правильно привязана к плоскости
                  if (visualization.transform.parent != plane.transform)
                  {
                        Debug.LogWarning($"Visualization parent mismatch for plane {plane.trackableId}, fixing...");
                        visualization.transform.SetParent(plane.transform, false);
                        visualization.transform.localPosition = Vector3.zero;
                        visualization.transform.localRotation = Quaternion.identity;
                        visualization.transform.localScale = Vector3.one;
                  }

                  // Получаем компоненты плоскости
                  var planeMeshFilter = plane.GetComponent<MeshFilter>();

                  if (planeMeshFilter == null || planeMeshFilter.mesh == null)
                  {
                        Debug.LogWarning($"No mesh found for plane {plane.trackableId}, creating default mesh");

                        // Создаем простой прямоугольный меш для визуализации
                        var mesh = new Mesh();
                        var size = plane.size;
                        var vertices = new Vector3[]
                        {
                              new Vector3(-size.x/2, 0, -size.y/2),
                              new Vector3(size.x/2, 0, -size.y/2),
                              new Vector3(-size.x/2, 0, size.y/2),
                              new Vector3(size.x/2, 0, size.y/2)
                        };
                        mesh.vertices = vertices;
                        mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
                        mesh.RecalculateNormals();
                        mesh.RecalculateBounds();

                        var visualizationMeshFilter = visualization.GetComponent<MeshFilter>();
                        if (visualizationMeshFilter == null)
                        {
                              visualizationMeshFilter = visualization.AddComponent<MeshFilter>();
                        }
                        visualizationMeshFilter.mesh = mesh;
                  }
                  else
                  {
                        // Используем меш плоскости
                        var visualizationMeshFilter = visualization.GetComponent<MeshFilter>();
                        if (visualizationMeshFilter == null)
                        {
                              visualizationMeshFilter = visualization.AddComponent<MeshFilter>();
                        }
                        visualizationMeshFilter.mesh = planeMeshFilter.mesh;
                  }

                  // Обновляем материал
                  var meshRenderer = visualization.GetComponent<MeshRenderer>();
                  if (meshRenderer == null)
                  {
                        meshRenderer = visualization.AddComponent<MeshRenderer>();
                  }

                  Material material = IsVerticalPlane(plane) ? wallMaterial : floorMaterial;
                  if (material != null)
                  {
                        // Сохраняем оригинальный материал
                        if (!originalMaterials.ContainsKey(plane.trackableId))
                        {
                              originalMaterials[plane.trackableId] = new Material(material);
                              originalMaterials[plane.trackableId].renderQueue = 3000; // Ensure transparent rendering
                              Debug.Log($"Created new material for plane {plane.trackableId}");
                        }

                        // Применяем материал с прозрачностью
                        meshRenderer.material = originalMaterials[plane.trackableId];
                        Color color = meshRenderer.material.color;
                        color.a = planeAlpha;
                        meshRenderer.material.color = color;
                        meshRenderer.enabled = true;
                  }
                  else
                  {
                        Debug.LogWarning($"No material assigned for plane {plane.trackableId}");
                  }

                  // Добавляем коллайдер для взаимодействия
                  var meshCollider = visualization.GetComponent<MeshCollider>();
                  if (meshCollider == null)
                  {
                        meshCollider = visualization.AddComponent<MeshCollider>();
                  }
                  meshCollider.sharedMesh = visualization.GetComponent<MeshFilter>().mesh;

                  // Устанавливаем видимость
                  visualization.SetActive(true);

                  Debug.Log($"Visualization updated for plane {plane.trackableId}");
            }

            public void UpdateAllPlanesVisibility()
            {
                  foreach (var plane in planeManager.trackables)
                  {
                        UpdatePlaneVisibility(plane);
                  }
            }

            private GameObject CreateDefaultPaintableSurface(float width, float height, Vector3 position, Color color, string name)
            {
                  GameObject surface = new GameObject(name);
                  surface.transform.position = position;
                  surface.transform.rotation = Quaternion.identity;

                  // Создаем меш для поверхности
                  MeshFilter meshFilter = surface.AddComponent<MeshFilter>();
                  MeshRenderer meshRenderer = surface.AddComponent<MeshRenderer>();

                  // Создаем простой прямоугольный меш
                  Mesh mesh = new Mesh();
                  Vector3[] vertices = new Vector3[4]
                  {
                        new Vector3(-width/2, -height/2, 0),
                        new Vector3(width/2, -height/2, 0),
                        new Vector3(-width/2, height/2, 0),
                        new Vector3(width/2, height/2, 0)
                  };
                  mesh.vertices = vertices;

                  int[] triangles = new int[6]
                  {
                        0, 2, 1,
                        2, 3, 1
                  };
                  mesh.triangles = triangles;

                  Vector3[] normals = new Vector3[4]
                  {
                        -Vector3.forward,
                        -Vector3.forward,
                        -Vector3.forward,
                        -Vector3.forward
                  };
                  mesh.normals = normals;

                  Vector2[] uv = new Vector2[4]
                  {
                        new Vector2(0, 0),
                        new Vector2(1, 0),
                        new Vector2(0, 1),
                        new Vector2(1, 1)
                  };
                  mesh.uv = uv;

                  meshFilter.mesh = mesh;

                  // Настраиваем материал
                  Material material = new Material(wallMaterial);
                  material.color = color;
                  meshRenderer.material = material;

                  // Добавляем компоненты для взаимодействия
                  surface.AddComponent<MeshCollider>();
                  surface.AddComponent<FixedPositionKeeper>();

                  return surface;
            }

            private GameObject CreateWallAtPosition(Vector3 position, Quaternion rotation, Vector3 size, Color color, string name)
            {
                  GameObject wall = CreateDefaultPaintableSurface(size.x, size.y, position, color, name);
                  if (wall != null)
                  {
                        wall.transform.rotation = rotation;
                        wall.transform.localScale = new Vector3(size.x, size.y, size.z);
                  }
                  return wall;
            }

            private void Awake()
            {
                  if (planeManager == null)
                  {
                        planeManager = FindFirstObjectByType<ARPlaneManager>();
                        if (planeManager == null)
                        {
                              Debug.LogError("ARPlaneManager not found in scene. Please add ARPlaneManager to the scene.");
                              enabled = false;
                              return;
                        }
                  }

                  if (raycastManager == null)
                  {
                        raycastManager = FindFirstObjectByType<ARRaycastManager>();
                        if (raycastManager == null)
                        {
                              Debug.LogError("ARRaycastManager not found in scene. Please add ARRaycastManager to the scene.");
                              enabled = false;
                              return;
                        }
                  }
            }

            private void Start()
            {
                  if (!planeManager || !raycastManager)
                  {
                        Debug.LogError("Required components are missing. Disabling ARPlaneVisibilityController.");
                        enabled = false;
                        return;
                  }

                  // Проверяем материалы
                  if (wallMaterial == null)
                  {
                        Debug.LogWarning("Wall material is not assigned. Creating default material.");
                        wallMaterial = new Material(Shader.Find("Standard"));
                        wallMaterial.color = new Color(1.0f, 0.0f, 0.0f, 1.0f); // Чисто красный
                        wallMaterial.EnableKeyword("_EMISSION"); // Включаем эмиссию
                        wallMaterial.SetColor("_EmissionColor", new Color(1.0f, 0.0f, 0.0f, 1.0f) * 2.0f); // Яркая эмиссия
                        wallMaterial.SetFloat("_Mode", 2); // Cutout mode вместо Transparent
                        wallMaterial.SetFloat("_Cutoff", 0.5f);
                        wallMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        wallMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        wallMaterial.SetInt("_ZWrite", 1);
                        wallMaterial.DisableKeyword("_ALPHABLEND_ON");
                        wallMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        wallMaterial.EnableKeyword("_ALPHATEST_ON");
                        wallMaterial.renderQueue = 2450; // Cutout queue
                        wallMaterial.SetFloat("_Metallic", 1.0f); // Максимальная металличность
                        wallMaterial.SetFloat("_Glossiness", 1.0f); // Максимальный блеск
                  }

                  if (floorMaterial == null)
                  {
                        Debug.LogWarning("Floor material is not assigned. Creating default material.");
                        floorMaterial = new Material(Shader.Find("Standard"));
                        floorMaterial.color = new Color(0.0f, 0.0f, 1.0f, 1.0f); // Чисто синий
                        floorMaterial.EnableKeyword("_EMISSION");
                        floorMaterial.SetColor("_EmissionColor", new Color(0.0f, 0.0f, 1.0f, 1.0f) * 2.0f);
                        floorMaterial.SetFloat("_Mode", 2);
                        floorMaterial.SetFloat("_Cutoff", 0.5f);
                        floorMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        floorMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        floorMaterial.SetInt("_ZWrite", 1);
                        floorMaterial.DisableKeyword("_ALPHABLEND_ON");
                        floorMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        floorMaterial.EnableKeyword("_ALPHATEST_ON");
                        floorMaterial.renderQueue = 2450;
                        floorMaterial.SetFloat("_Metallic", 1.0f);
                        floorMaterial.SetFloat("_Glossiness", 1.0f);
                  }

                  // Включаем обнаружение плоскостей
                  if (planeManager != null)
                  {
                        planeManager.enabled = true;
                        planeManager.requestedDetectionMode = UnityEngine.XR.ARSubsystems.PlaneDetectionMode.Vertical | UnityEngine.XR.ARSubsystems.PlaneDetectionMode.Horizontal;
                  }

                  // Подписываемся на события изменения плоскостей
                  planeManager.planesChanged += OnPlanesChanged;

                  // Инициализируем словари
                  planeVisualizations = new Dictionary<TrackableId, GameObject>();
                  originalMaterials = new Dictionary<TrackableId, Material>();

                  // Устанавливаем начальную видимость
                  if (!hideOnStart)
                  {
                        SetPlaneVisibility(true);
                  }

                  Debug.Log("ARPlaneVisibilityController initialized successfully");
            }

            private void OnPlanesChanged(ARPlanesChangedEventArgs args)
            {
                  // Обновляем добавленные плоскости
                  foreach (var plane in args.added)
                  {
                        UpdatePlaneVisualization(plane);
                  }

                  // Обновляем измененные плоскости
                  foreach (var plane in args.updated)
                  {
                        UpdatePlaneVisualization(plane);
                  }

                  // Удаляем визуализацию удаленных плоскостей
                  foreach (var plane in args.removed)
                  {
                        if (planeVisualizations.TryGetValue(plane.trackableId, out GameObject visualization))
                        {
                              Destroy(visualization);
                              planeVisualizations.Remove(plane.trackableId);
                        }
                  }
            }

            private void OnDestroy()
            {
                  if (planeManager != null)
                  {
                        planeManager.planesChanged -= OnPlanesChanged;
                  }

                  // Очищаем ресурсы
                  foreach (var material in originalMaterials.Values)
                  {
                        if (material != null)
                        {
                              Destroy(material);
                        }
                  }
            }
      }
}