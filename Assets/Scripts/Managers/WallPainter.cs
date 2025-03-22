using UnityEngine;
using System.Collections.Generic;
using Remalux.AR.Input;

namespace Remalux.AR
{
      public class WallPainter : MonoBehaviour
      {
            [Header("Paint Settings")]
            [SerializeField] public Material[] availablePaints;
            [SerializeField] public Material defaultMaterial;
            [SerializeField] public Camera mainCamera;
            [SerializeField] public LayerMask wallLayerMask;

            [Header("Preview Settings")]
            [SerializeField] public bool showColorPreview = true;
            [SerializeField] public GameObject colorPreviewPrefab;

            // Список стен для покраски
            private List<GameObject> walls = new List<GameObject>();

            // Текущий выбранный материал для покраски
            private Material currentPaintMaterial;

            // Словарь для хранения оригинальных материалов стен
            private Dictionary<GameObject, Material> originalMaterials = new Dictionary<GameObject, Material>();

            // Объект предпросмотра цвета
            private GameObject colorPreview;
            private SimpleColorPreview previewComponent;

            // Flag to control input handling
            private bool handleInputInternally = false;
            private bool _isInitialized = false;

            /// <summary>
            /// Gets whether the WallPainter is initialized
            /// </summary>
            public bool IsInitialized => _isInitialized;

            private void Awake()
            {
                  Debug.Log("WallPainter: Awake called");
                  if (!_isInitialized)
                  {
                        Initialize();
                  }
            }

            private void Start()
            {
                  Debug.Log("WallPainter: Start called");
                  if (!_isInitialized)
                  {
                        Initialize();
                  }

                  if (!_isInitialized)
                  {
                        Debug.LogError("WallPainter: Failed to initialize in both Awake and Start!");
                        return;
                  }

                  SetupInputHandlers();
            }

            private void SetupInputHandlers()
            {
                  var enhancedInput = GetComponent<EnhancedWallPainterInput>();
                  var directInput = GetComponent<DirectInputHandler>();

                  if (enhancedInput != null && directInput != null)
                  {
                        Debug.LogWarning("WallPainter: Multiple input handlers detected. Disabling DirectInputHandler.");
                        directInput.enabled = false;
                        Destroy(directInput);
                        handleInputInternally = false;
                  }
                  else if (enhancedInput != null)
                  {
                        Debug.Log("WallPainter: Using EnhancedWallPainterInput");
                        handleInputInternally = false;
                  }
                  else if (directInput != null)
                  {
                        Debug.Log("WallPainter: Using DirectInputHandler");
                        handleInputInternally = false;
                  }
                  else
                  {
                        Debug.Log("WallPainter: No external input handlers found. Using internal input handling.");
                        handleInputInternally = true;
                  }
            }

            private void OnEnable()
            {
                  // Re-check input handlers when component is enabled
                  var enhancedInput = GetComponent<EnhancedWallPainterInput>();
                  var directInput = GetComponent<DirectInputHandler>();

                  if (enhancedInput != null && directInput != null)
                  {
                        directInput.enabled = false;
                        Destroy(directInput);
                        handleInputInternally = false;
                  }
                  else if (enhancedInput != null || directInput != null)
                  {
                        handleInputInternally = false;
                  }
                  else
                  {
                        handleInputInternally = true;
                  }
            }

            private void OnDisable()
            {
                  // Disable input handling when component is disabled
                  handleInputInternally = false;
            }

            /// <summary>
            /// Возвращает подходящий шейдер в зависимости от используемого рендер пайплайна
            /// </summary>
            private Shader GetAppropriateShader()
            {
                  if (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null)
                  {
                        // Для URP
                        Debug.Log("Используется URP, возвращаем URP шейдер");
                        return Shader.Find("Universal Render Pipeline/Lit");
                  }
                  else
                  {
                        // Для стандартного рендер пайплайна
                        return Shader.Find("Standard");
                  }
            }

            public void Initialize()
            {
                  Debug.Log("WallPainter: Starting initialization...");

                  // Check if already initialized
                  if (_isInitialized)
                  {
                        Debug.Log("WallPainter: Already initialized");
                        return;
                  }

                  bool initSuccess = true;

                  // Initialize camera
                  if (mainCamera == null)
                  {
                        mainCamera = Camera.main;
                        if (mainCamera == null)
                        {
                              Debug.LogError("WallPainter: No camera found!");
                              initSuccess = false;
                        }
                        else
                        {
                              Debug.Log($"WallPainter: Using main camera: {mainCamera.name}");
                        }
                  }

                  // Initialize wall layer mask
                  if (wallLayerMask == 0)
                  {
                        int wallLayer = LayerMask.NameToLayer("Wall");
                        if (wallLayer != -1)
                        {
                              wallLayerMask = 1 << wallLayer;
                              Debug.Log($"WallPainter: Set wall layer mask: {wallLayerMask.value}");
                        }
                        else
                        {
                              Debug.LogError("WallPainter: 'Wall' layer not found!");
                              initSuccess = false;
                        }
                  }

                  // Initialize paint materials
                  if (availablePaints == null || availablePaints.Length == 0)
                  {
                        Debug.LogWarning("WallPainter: No paint materials assigned, attempting to get from WallPaintingManager");
                        var manager = FindObjectOfType<WallPaintingManager>();
                        if (manager != null && manager.paintMaterials != null && manager.paintMaterials.Length > 0)
                        {
                              availablePaints = manager.paintMaterials;
                              Debug.Log($"WallPainter: Got {availablePaints.Length} materials from WallPaintingManager");
                        }
                        else
                        {
                              Debug.LogError("WallPainter: No paint materials available!");
                              initSuccess = false;
                        }
                  }

                  // Initialize default material
                  if (defaultMaterial == null)
                  {
                        if (availablePaints != null && availablePaints.Length > 0)
                        {
                              defaultMaterial = availablePaints[0];
                              Debug.Log($"WallPainter: Using first paint as default: {defaultMaterial.name}");
                        }
                        else
                        {
                              Debug.LogError("WallPainter: No default material and no available paints!");
                              initSuccess = false;
                        }
                  }

                  // Initialize current paint material
                  if (initSuccess)
                  {
                        currentPaintMaterial = defaultMaterial;
                        Debug.Log($"WallPainter: Current paint material set to: {currentPaintMaterial.name}");

                        // Initialize color preview
                        if (showColorPreview)
                        {
                              if (colorPreviewPrefab != null)
                              {
                                    colorPreview = Instantiate(colorPreviewPrefab);
                                    Debug.Log("WallPainter: Created color preview from prefab");
                              }
                              else
                              {
                                    colorPreview = CreateColorPreviewObject();
                                    Debug.Log("WallPainter: Created default color preview object");
                              }

                              previewComponent = colorPreview.GetComponent<SimpleColorPreview>();
                              if (previewComponent == null)
                              {
                                    previewComponent = colorPreview.AddComponent<SimpleColorPreview>();
                              }
                              colorPreview.SetActive(false);
                              Debug.Log("WallPainter: Color preview initialized");
                        }

                        _isInitialized = true;
                        Debug.Log("WallPainter: Initialization complete");
                  }
                  else
                  {
                        Debug.LogError("WallPainter: Initialization failed!");
                  }
            }

            private void Update()
            {
                  if (!_isInitialized)
                  {
                        return;
                  }

                  if (handleInputInternally && UnityEngine.Input.GetMouseButtonDown(0))
                  {
                        PaintWallAtPosition(UnityEngine.Input.mousePosition);
                  }

                  UpdateColorPreview();
            }

            private void UpdateColorPreview()
            {
                  if (!showColorPreview || colorPreview == null || mainCamera == null) return;

                  Ray ray = mainCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
                  if (Physics.Raycast(ray, out RaycastHit hit, 100f, wallLayerMask))
                  {
                        colorPreview.SetActive(true);
                        colorPreview.transform.position = hit.point + hit.normal * 0.01f;
                        colorPreview.transform.forward = hit.normal;

                        if (previewComponent != null && currentPaintMaterial != null)
                        {
                              previewComponent.SetMaterial(currentPaintMaterial);
                        }
                  }
                  else
                  {
                        colorPreview.SetActive(false);
                  }
            }

            public void PaintWallAtPosition(Vector2 screenPosition)
            {
                  if (!_isInitialized)
                  {
                        Debug.LogError("WallPainter: Not initialized!");
                        return;
                  }

                  if (currentPaintMaterial == null)
                  {
                        Debug.LogError("WallPainter: No paint material selected!");
                        return;
                  }

                  Ray ray = mainCamera.ScreenPointToRay(screenPosition);
                  Debug.Log($"WallPainter: Casting ray from {ray.origin} in direction {ray.direction}");

                  if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, wallLayerMask))
                  {
                        Debug.Log($"WallPainter: Hit object {hit.collider.gameObject.name} at point {hit.point}");
                        var wallObject = hit.collider.gameObject;
                        var wallRenderer = wallObject.GetComponent<Renderer>();

                        if (!wallRenderer)
                        {
                              Debug.LogError($"WallPainter: No Renderer on {wallObject.name}");
                              return;
                        }

                        // Get or add WallMaterialInstanceTracker
                        var tracker = wallObject.GetComponent<WallMaterialInstanceTracker>();
                        if (tracker == null)
                        {
                              tracker = wallObject.AddComponent<WallMaterialInstanceTracker>();
                              tracker.OriginalSharedMaterial = wallRenderer.sharedMaterial;
                              Debug.Log($"WallPainter: Added WallMaterialInstanceTracker to {wallObject.name}");
                        }

                        // Apply material through the tracker
                        tracker.ApplyMaterial(currentPaintMaterial);
                        Debug.Log($"WallPainter: Applied {currentPaintMaterial.name} to {wallObject.name} through tracker");

                        // Track painted wall
                        if (!walls.Contains(wallObject))
                        {
                              walls.Add(wallObject);
                        }
                  }
                  else
                  {
                        Debug.Log("WallPainter: Ray did not hit any wall");
                  }
            }

            public void SelectPaintMaterial(int index)
            {
                  if (!_isInitialized)
                  {
                        Debug.LogError("WallPainter: Not initialized!");
                        return;
                  }

                  if (availablePaints == null || availablePaints.Length == 0)
                  {
                        Debug.LogError("WallPainter: No paint materials available!");
                        return;
                  }

                  if (index < 0 || index >= availablePaints.Length)
                  {
                        Debug.LogError($"WallPainter: Invalid material index {index}!");
                        return;
                  }

                  Material selectedMaterial = availablePaints[index];
                  if (selectedMaterial != null)
                  {
                        currentPaintMaterial = selectedMaterial;
                        Debug.Log($"WallPainter: Selected material: {currentPaintMaterial.name}");

                        // Update preview if active
                        if (colorPreview != null && previewComponent != null && colorPreview.activeSelf)
                        {
                              previewComponent.SetMaterial(currentPaintMaterial);
                        }
                  }
                  else
                  {
                        Debug.LogError($"WallPainter: Material at index {index} is null!");
                  }
            }

            public void ResetWallMaterials()
            {
                  if (!_isInitialized) return;

                  foreach (var wall in walls)
                  {
                        if (wall != null)
                        {
                              var renderer = wall.GetComponent<Renderer>();
                              if (renderer != null && originalMaterials.ContainsKey(wall))
                              {
                                    renderer.material = new Material(originalMaterials[wall]);
                                    Debug.Log($"WallPainter: Reset material for {wall.name}");
                              }
                        }
                  }

                  walls.Clear();
                  originalMaterials.Clear();
                  Debug.Log("WallPainter: All walls reset to original materials");
            }

            public Material GetCurrentPaintMaterial()
            {
                  return currentPaintMaterial;
            }

            // Метод для создания простого объекта превью цвета
            private GameObject CreateColorPreviewObject()
            {
                  GameObject previewObject = new GameObject("ColorPreview");
                  MeshFilter meshFilter = previewObject.AddComponent<MeshFilter>();
                  MeshRenderer renderer = previewObject.AddComponent<MeshRenderer>();

                  // Create preview mesh
                  Mesh mesh = new Mesh();
                  int segments = 32;
                  float radius = 0.1f;

                  Vector3[] vertices = new Vector3[segments + 1];
                  int[] triangles = new int[segments * 3];
                  Vector2[] uvs = new Vector2[segments + 1];

                  vertices[0] = Vector3.zero;
                  uvs[0] = new Vector2(0.5f, 0.5f);

                  float angleStep = 360f / segments;
                  for (int i = 0; i < segments; i++)
                  {
                        float angle = angleStep * i * Mathf.Deg2Rad;
                        float x = Mathf.Sin(angle) * radius;
                        float y = Mathf.Cos(angle) * radius;

                        vertices[i + 1] = new Vector3(x, y, 0);
                        uvs[i + 1] = new Vector2((x / radius + 1) * 0.5f, (y / radius + 1) * 0.5f);

                        triangles[i * 3] = 0;
                        triangles[i * 3 + 1] = i + 1;
                        triangles[i * 3 + 2] = (i + 1) % segments + 1;
                  }

                  mesh.vertices = vertices;
                  mesh.triangles = triangles;
                  mesh.uv = uvs;
                  mesh.RecalculateNormals();

                  meshFilter.mesh = mesh;
                  renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                  {
                        color = Color.white
                  };

                  return previewObject;
            }

            // Метод для добавления стены в список стен для покраски
            public void AddWall(GameObject wallObject)
            {
                  if (wallObject != null && !walls.Contains(wallObject))
                  {
                        walls.Add(wallObject);

                        // Сохраняем оригинальный материал, если еще не сохранен
                        if (!originalMaterials.ContainsKey(wallObject))
                        {
                              Renderer renderer = wallObject.GetComponent<Renderer>();
                              if (renderer != null)
                              {
                                    originalMaterials[wallObject] = renderer.material;
                              }
                        }

                        Debug.Log($"Стена добавлена в список для покраски: {wallObject.name}");
                  }
            }
      }
}