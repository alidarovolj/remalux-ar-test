using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using System;
using UnityEngine.EventSystems;
using System.Collections;
using Remalux.AR;
using Unity.XR.CoreUtils;

namespace Remalux.AR
{
      /// <summary>
      /// Класс для покраски стен в дополненной реальности
      /// </summary>
      public class WallPainter : MonoBehaviour
      {
            [Header("AR References")]
            [SerializeField] private ARRaycastManager raycastManager;
            [SerializeField] private ARPlaneManager planeManager;
            [SerializeField] private Camera arCamera;
            [SerializeField] private ARPlaneVisibilityController planeVisibilityController;

            [Header("Materials")]
            [SerializeField] private Material defaultWallMaterial;
            [SerializeField] private Material wallHighlightMaterial;
            [SerializeField] private List<Material> paintMaterials;

            [Header("Paint Settings")]
            [SerializeField] private float brushSize = 0.05f;
            [SerializeField] private float minBrushSize = 0.01f;
            [SerializeField] private float maxBrushSize = 0.2f;

            [Header("UI элементы")]
            [SerializeField] private GameObject colorPalettePanel;
            [SerializeField] private Button[] colorButtons;
            [SerializeField] private Slider brushSizeSlider;
            [SerializeField] private Button resetButton;
            [SerializeField] private Button screenshotButton;

            // Приватные переменные
            private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();
            private GameObject highlightedWall;
            private GameObject activeWall;
            private int selectedColorIndex = 0;
            private Dictionary<GameObject, Material> originalMaterials = new Dictionary<GameObject, Material>();
            private Dictionary<GameObject, Material> paintedWalls = new Dictionary<GameObject, Material>();
            private ARPlane currentHighlightedWall;
            private ARPlane lastPaintedWall;
            private Material currentPaintMaterial;
            private List<ARPlane> verticalPlanes = new List<ARPlane>();
            private bool isInitialized = false;
            private bool isHighlighting = true;
            private WallPaintingUIManager uiManager;
            private List<GameObject> _trackedWalls = new List<GameObject>();
            private bool _autoSelectFirstWall = true;

            public float BrushSize
            {
                  get { return brushSize; }
                  set
                  {
                        brushSize = Mathf.Clamp(value, minBrushSize, maxBrushSize);
                  }
            }

            private void Awake()
            {
                  // Получаем компоненты, если они не были установлены в инспекторе
                  if (raycastManager == null)
                        raycastManager = UnityEngine.Object.FindFirstObjectByType<ARRaycastManager>();

                  if (planeManager == null)
                        planeManager = UnityEngine.Object.FindFirstObjectByType<ARPlaneManager>();

                  if (arCamera == null)
                        arCamera = UnityEngine.Object.FindFirstObjectByType<Camera>();

                  // Установка начального материала
                  if (paintMaterials != null && paintMaterials.Count > 0)
                  {
                        currentPaintMaterial = paintMaterials[0];
                  }
                  else
                  {
                        // Создаем временный материал, если материалы не заданы
                        currentPaintMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        if (currentPaintMaterial == null)
                        {
                              currentPaintMaterial = new Material(Shader.Find("Standard"));
                        }
                        currentPaintMaterial.color = Color.red;

                        // Сохраняем созданный материал в проекте
#if UNITY_EDITOR
                        if (!System.IO.Directory.Exists("Assets/Materials"))
                        {
                              System.IO.Directory.CreateDirectory("Assets/Materials");
                        }
                        UnityEditor.AssetDatabase.CreateAsset(currentPaintMaterial, "Assets/Materials/DefaultPaintMaterial.mat");
                        UnityEditor.AssetDatabase.SaveAssets();
#endif
                  }

                  // Находим UI менеджер
                  if (uiManager == null)
                  {
                        uiManager = UnityEngine.Object.FindFirstObjectByType<WallPaintingUIManager>();
                  }
            }

            /// <summary>
            /// Инициализация
            /// </summary>
            private void Start()
            {
                  // Создаем тег "Painted", если он не существует
                  if (!Array.Exists(UnityEditorInternal.InternalEditorUtility.tags, tag => tag == "Painted"))
                  {
                        // В редакторе нужно добавить тег через сериализованный объект
#if UNITY_EDITOR
                        UnityEditor.SerializedObject tagManager = new UnityEditor.SerializedObject(UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                        UnityEditor.SerializedProperty tagsProp = tagManager.FindProperty("tags");

                        // Проверяем, есть ли уже тег
                        bool found = false;
                        for (int i = 0; i < tagsProp.arraySize; i++)
                        {
                              UnityEditor.SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
                              if (t.stringValue.Equals("Painted"))
                              {
                                    found = true;
                                    break;
                              }
                        }

                        // Если тега нет, добавляем его
                        if (!found)
                        {
                              tagsProp.arraySize++;
                              UnityEditor.SerializedProperty tag = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
                              tag.stringValue = "Painted";
                              tagManager.ApplyModifiedProperties();
                        }
#endif
                        Debug.Log("Тег 'Painted' добавлен в проект");
                  }

                  // Получаем ссылки на необходимые компоненты
                  arCamera = UnityEngine.Object.FindFirstObjectByType<Camera>();
                  planeManager = UnityEngine.Object.FindFirstObjectByType<ARPlaneManager>();
                  raycastManager = UnityEngine.Object.FindFirstObjectByType<ARRaycastManager>();
                  planeVisibilityController = UnityEngine.Object.FindFirstObjectByType<ARPlaneVisibilityController>();

                  if (planeManager == null)
                  {
                        Debug.LogError("ARPlaneManager не найден в сцене");
                        return;
                  }
                  if (raycastManager == null)
                  {
                        Debug.LogError("ARRaycastManager не найден в сцене");
                        return;
                  }

                  // Инициализация массивов
                  raycastHits = new List<ARRaycastHit>();
                  verticalPlanes = new List<ARPlane>();
                  originalMaterials = new Dictionary<GameObject, Material>();
                  paintedWalls = new Dictionary<GameObject, Material>();

                  // Подписываемся на событие изменения плоскостей
                  planeManager.trackablesChanged.AddListener(OnPlanesChanged);

                  // Инициализируем контроллер видимости плоскостей AR
                  if (planeVisibilityController == null)
                  {
                        planeVisibilityController = gameObject.AddComponent<ARPlaneVisibilityController>();
                        Debug.Log("ARPlaneManager найден в сцене");
                  }

                  // Включаем подсветку вертикальных плоскостей по умолчанию
                  if (planeVisibilityController != null)
                  {
                        planeVisibilityController.SetPlaneVisibility(true);
                        planeVisibilityController.SetHighlightWalls(true);
                  }

                  // Инициализация UI
                  InitUI();

                  isInitialized = true;

                  // Устанавливаем материал покраски по умолчанию
                  if (paintMaterials != null && paintMaterials.Count > 0)
                  {
                        currentPaintMaterial = paintMaterials[0];
                  }

                  // Автоматически выделяем все доступные стены при запуске
                  StartCoroutine(HighlightAllWallsOnStart());

                  Debug.Log("WallPainter инициализирован успешно.");
            }

            /// <summary>
            /// Выделяет все стены при запуске приложения для повышения наглядности
            /// </summary>
            private IEnumerator HighlightAllWallsOnStart()
            {
                  // Ждем немного, чтобы AR успел обнаружить плоскости
                  yield return new WaitForSeconds(1.5f);

                  int wallCount = 0;

                  // Подсвечиваем все вертикальные плоскости
                  if (planeManager != null)
                  {
                        foreach (ARPlane plane in planeManager.trackables)
                        {
                              if (IsWall(plane))
                              {
                                    // Вызываем метод настройки стены
                                    SetupWall(plane);

                                    // Автоматически выделяем стену, чтобы пользователю было понятно, что её можно красить
                                    HighlightWallMaterial(plane);

                                    wallCount++;
                              }
                        }
                  }

                  Debug.Log($"Автоматически выделено {wallCount} стен при запуске");

                  // Обновляем визуальное отображение плоскостей
                  if (planeVisibilityController != null)
                  {
                        planeVisibilityController.UpdateAllPlanesVisibility();
                        planeVisibilityController.ForceShowAllPlanes();
                  }
            }

            private void Update()
            {
                  if (!isInitialized || EventSystem.current.IsPointerOverGameObject())
                        return;

                  // Выделение стены
                  if (isHighlighting)
                  {
                        HighlightWall();
                  }

                  // Проверка касания экрана для покраски
                  if (Input.touchCount > 0)
                  {
                        Touch touch = Input.GetTouch(0);

                        if (touch.phase == TouchPhase.Began && !EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                        {
                              PaintWall(touch.position);
                        }
                  }

                  // Проверка нажатия мыши для покраски (для тестирования в редакторе)
                  if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
                  {
                        PaintWall(Input.mousePosition);
                  }
            }

            /// <summary>
            /// Инициализирует UI элементы и добавляет обработчики событий
            /// </summary>
            private void InitializeUI()
            {
                  // Настраиваем кнопки выбора цвета
                  if (colorButtons != null && colorButtons.Length > 0)
                  {
                        for (int i = 0; i < colorButtons.Length; i++)
                        {
                              int colorIndex = i; // Создаем локальную копию для замыкания
                              if (colorButtons[i] != null)
                              {
                                    colorButtons[i].onClick.AddListener(() => SelectColor(colorIndex));

                                    // Устанавливаем цвет кнопки, если есть соответствующий материал
                                    if (i < paintMaterials.Count)
                                    {
                                          Image buttonImage = colorButtons[i].GetComponent<Image>();
                                          if (buttonImage != null)
                                          {
                                                buttonImage.color = paintMaterials[i].color;
                                          }
                                    }
                              }
                        }
                  }

                  // Настраиваем слайдер размера кисти
                  if (brushSizeSlider != null)
                  {
                        brushSizeSlider.value = brushSize;
                        brushSizeSlider.onValueChanged.AddListener(SetBrushSize);
                  }

                  // Настраиваем кнопку сброса
                  if (resetButton != null)
                  {
                        resetButton.onClick.AddListener(ResetWalls);
                  }

                  // Настраиваем кнопку скриншота
                  if (screenshotButton != null)
                  {
                        screenshotButton.onClick.AddListener(TakeScreenshot);
                  }
            }

            /// <summary>
            /// Инициализирует UI элементы и добавляет обработчики событий
            /// </summary>
            private void InitUI()
            {
                  // Просто вызываем основной метод инициализации UI
                  InitializeUI();
            }

            /// <summary>
            /// Настраивает обработчики событий для AR плоскостей
            /// </summary>
            private void SetupARPlaneEvents()
            {
                  if (planeManager != null)
                  {
                        // Подписываемся на событие изменения плоскостей
                        planeManager.trackablesChanged.AddListener(OnPlanesChanged);
                  }
            }

            /// <summary>
            /// Обработчик события изменения обнаруженных плоскостей
            /// </summary>
            private void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
            {
                  // Обрабатываем добавленные плоскости
                  if (args.added != null && args.added.Count > 0)
                  {
                        foreach (ARPlane plane in args.added)
                        {
                              // Проверяем, является ли плоскость стеной
                              if (IsWall(plane))
                              {
                                    // Настраиваем стену для взаимодействия
                                    SetupWall(plane);
                                    Debug.Log($"Обнаружена стена: {plane.trackableId}");
                              }
                        }
                  }

                  // Обрабатываем обновленные плоскости
                  if (args.updated != null && args.updated.Count > 0)
                  {
                        // ... existing code ...
                  }

                  // Обрабатываем удаленные плоскости
                  if (args.removed != null && args.removed.Count > 0)
                  {
                        foreach (var kvp in args.removed)
                        {
                              ARPlane plane = kvp.Value;
                              if (plane != null)
                              {
                                    // Удаляем плоскость из списка вертикальных плоскостей
                                    verticalPlanes.Remove(plane);

                                    // Если это была выделенная стена, сбрасываем выделение
                                    if (currentHighlightedWall == plane)
                                    {
                                          currentHighlightedWall = null;
                                    }
                              }
                        }
                  }
            }

            /// <summary>
            /// Проверяет, является ли объект поверхностью для рисования
            /// </summary>
            public bool IsWall(GameObject obj)
            {
                  if (obj == null)
                        return false;

                  // Проверяем наличие компонента ARPlane (стена от AR Foundation)
                  ARPlane plane = obj.GetComponent<ARPlane>();
                  if (plane != null)
                  {
                        return planeVisibilityController.IsVerticalPlane(plane);
                  }

                  // Проверяем наличие компонента PaintableSurface (наша кастомная поверхность)
                  PaintableSurface paintableSurface = obj.GetComponent<PaintableSurface>();
                  if (paintableSurface != null)
                  {
                        return true; // Все PaintableSurface по умолчанию подходят для рисования
                  }

                  // Проверяем наличие компонента WallIdentifier (стена от OpenCV)
                  ARPlaneVisibilityController.WallIdentifier wallIdentifier = obj.GetComponent<ARPlaneVisibilityController.WallIdentifier>();
                  if (wallIdentifier != null)
                  {
                        return true; // Все WallIdentifier представляют стены
                  }

                  // Стандартная проверка по имени объекта
                  return obj.name.Contains("Wall") || obj.name.Contains("Стена") || obj.name.Contains("PaintableSurface");
            }

            /// <summary>
            /// Проверяет, является ли ARPlane стеной (вертикальной поверхностью)
            /// </summary>
            private bool IsWall(ARPlane plane)
            {
                  if (plane == null)
                        return false;

                  return planeVisibilityController.IsVerticalPlane(plane);
            }

            /// <summary>
            /// Настраивает стену из ARPlane
            /// </summary>
            public void SetupWall(ARPlane plane)
            {
                  if (plane == null || !IsWall(plane))
                        return;

                  GameObject wallObj = plane.gameObject;
                  SetupWall(wallObj);
            }

            /// <summary>
            /// Настраивает обнаруженную стену из GameObject
            /// </summary>
            private void SetupWall(GameObject wallObject)
            {
                  if (wallObject == null) return;

                  // Добавляем Mesh Collider для определения касаний
                  MeshCollider collider = wallObject.GetComponent<MeshCollider>();
                  if (collider == null)
                  {
                        collider = wallObject.AddComponent<MeshCollider>();
                  }

                  // Убеждаемся, что MeshCollider использует меш визуализатора
                  MeshFilter meshFilter = wallObject.GetComponent<MeshFilter>();
                  if (meshFilter != null)
                  {
                        collider.sharedMesh = meshFilter.sharedMesh;
                  }

                  // Сохраняем оригинальный материал
                  Renderer renderer = wallObject.GetComponent<Renderer>();
                  if (renderer != null && !originalMaterials.ContainsKey(wallObject))
                  {
                        originalMaterials[wallObject] = renderer.material;

                        // Если указан материал по умолчанию, применяем его
                        if (defaultWallMaterial != null)
                        {
                              renderer.material = defaultWallMaterial;
                        }
                  }
            }

            /// <summary>
            /// Выделяет стену, на которую в данный момент направлена камера
            /// </summary>
            private void HighlightWall()
            {
                  // Проверяем, включен ли режим выделения
                  if (!isHighlighting)
                        return;

                  // Центр экрана
                  Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);

                  // Проверяем, есть ли у нас камера
                  if (arCamera == null)
                  {
                        arCamera = UnityEngine.Object.FindFirstObjectByType<Camera>();
                        if (arCamera == null)
                        {
                              Debug.LogWarning("AR Camera не найдена в HighlightWall");
                              return;
                        }
                  }

                  // Проверяем доступность raycastManager
                  if (raycastManager == null)
                  {
                        raycastManager = UnityEngine.Object.FindFirstObjectByType<ARRaycastManager>();
                        if (raycastManager == null)
                        {
                              Debug.LogError("ARRaycastManager не найден");
                              return;
                        }
                  }

                  // Проверяем доступность planeManager
                  if (planeManager == null)
                  {
                        planeManager = UnityEngine.Object.FindFirstObjectByType<ARPlaneManager>();
                        if (planeManager == null)
                        {
                              Debug.LogError("ARPlaneManager не найден");
                              return;
                        }
                  }

                  // Выполняем raycast для определения, смотрим ли мы на плоскость
                  if (raycastManager.Raycast(screenCenter, raycastHits, TrackableType.PlaneWithinPolygon))
                  {
                        // Берем первый результат
                        ARRaycastHit hit = raycastHits[0];

                        // Находим ближайшую плоскость
                        ARPlane plane = planeManager.GetPlane(hit.trackableId);

                        // Проверяем, действительно ли это стена
                        if (plane != null && IsWall(plane))
                        {
                              // Если это новая стена, и она отличается от текущей выделенной
                              if (currentHighlightedWall != plane)
                              {
                                    // Возвращаем предыдущей стене исходный материал
                                    if (currentHighlightedWall != null)
                                    {
                                          RestoreWallMaterial(currentHighlightedWall);
                                    }

                                    // Выделяем новую стену
                                    HighlightWallMaterial(plane);
                                    currentHighlightedWall = plane;

                              }
                        }
                        else
                        {
                              // Если raycast не попал на вертикальную плоскость, убираем выделение
                              if (currentHighlightedWall != null)
                              {
                                    RestoreWallMaterial(currentHighlightedWall);
                                    currentHighlightedWall = null;
                              }
                        }
                  }
                  else
                  {
                        // Если raycast не попал ни на какую плоскость, убираем выделение
                        if (currentHighlightedWall != null)
                        {
                              RestoreWallMaterial(currentHighlightedWall);
                              currentHighlightedWall = null;
                        }
                  }
            }

            /// <summary>
            /// Изменяет материал стены для отображения выделения
            /// </summary>
            private void HighlightWallMaterial(ARPlane plane)
            {
                  if (plane != null && plane.GetComponent<MeshRenderer>() != null && wallHighlightMaterial != null)
                  {
                        // Проверяем, что стена еще не покрашена
                        if (!paintedWalls.ContainsKey(plane.gameObject))
                        {
                              if (plane.gameObject.TryGetComponent<MeshRenderer>(out MeshRenderer meshRenderer))
                              {
                                    // Сохраняем оригинальный материал, если еще не сохранен
                                    if (!originalMaterials.ContainsKey(plane.gameObject))
                                    {
                                          originalMaterials[plane.gameObject] = meshRenderer.material;
                                    }

                                    // Создаем и настраиваем специальный материал для выделения
                                    Material highlightMat = new Material(wallHighlightMaterial);

                                    // Делаем материал более заметным - яркий желтый цвет
                                    highlightMat.color = new Color(1.0f, 0.9f, 0.0f, 0.9f);

                                    // Добавляем свечение для более заметного эффекта
                                    if (highlightMat.HasProperty("_EmissionColor"))
                                    {
                                          highlightMat.EnableKeyword("_EMISSION");
                                          highlightMat.SetColor("_EmissionColor", new Color(1.0f, 0.9f, 0.0f, 1.0f));
                                          highlightMat.SetFloat("_EmissionIntensity", 1.5f);
                                    }

                                    // Применяем материал с эффектом выделения
                                    meshRenderer.material = highlightMat;

                                    // Отключаем тени для улучшения видимости
                                    meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                                    meshRenderer.receiveShadows = false;

                                    // Для отслеживания выделенных стен создаем отдельную переменную
                                    currentHighlightedWall = plane;
                                    Debug.Log($"Стена выделена: {plane.trackableId}");

                                    // Показываем более заметный индикатор выделения
                                    ShowWallHighlightEffect(plane);
                              }
                        }
                        else
                        {
                              Debug.Log($"Стена уже покрашена, пропускаем выделение: {plane.trackableId}");
                        }
                  }
            }

            /// <summary>
            /// Показывает визуальный эффект для выделенной стены
            /// </summary>
            private void ShowWallHighlightEffect(ARPlane plane)
            {
                  if (plane == null) return;

                  // Находим или создаем объект эффекта контура
                  GameObject highlightEffect = null;
                  string effectName = $"HighlightEffect_{plane.trackableId}";

                  Transform existingEffect = plane.transform.Find(effectName);
                  if (existingEffect != null)
                  {
                        highlightEffect = existingEffect.gameObject;
                  }
                  else
                  {
                        highlightEffect = new GameObject(effectName);
                        highlightEffect.transform.SetParent(plane.transform, false);
                        highlightEffect.transform.localPosition = new Vector3(0, 0.002f, 0); // Немного перед стеной

                        // Добавляем компоненты для отображения эффекта
                        MeshFilter meshFilter = highlightEffect.AddComponent<MeshFilter>();
                        MeshRenderer meshRenderer = highlightEffect.AddComponent<MeshRenderer>();

                        // Копируем меш из родительского объекта
                        MeshFilter planeMeshFilter = plane.GetComponent<MeshFilter>();
                        if (planeMeshFilter != null && planeMeshFilter.sharedMesh != null)
                        {
                              meshFilter.mesh = planeMeshFilter.sharedMesh;
                        }

                        // Создаем материал для контура
                        Material outlineMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                        outlineMaterial.color = new Color(1.0f, 0.9f, 0.0f, 0.8f); // Яркий желтый

                        // Включаем эмиссию для лучшей видимости
                        if (outlineMaterial.HasProperty("_EmissionColor"))
                        {
                              outlineMaterial.EnableKeyword("_EMISSION");
                              outlineMaterial.SetColor("_EmissionColor", new Color(1.0f, 0.9f, 0.0f, 1.0f));
                        }

                        // Сохраняем материал как ресурс проекта
#if UNITY_EDITOR
                        if (!System.IO.Directory.Exists("Assets/Materials"))
                        {
                              System.IO.Directory.CreateDirectory("Assets/Materials");
                        }
                        string matPath = $"Assets/Materials/OutlineMaterial_{System.Guid.NewGuid().ToString().Substring(0, 8)}.mat";
                        UnityEditor.AssetDatabase.CreateAsset(outlineMaterial, matPath);
                        UnityEditor.AssetDatabase.SaveAssets();
#endif

                        meshRenderer.material = outlineMaterial;
                        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        meshRenderer.receiveShadows = false;
                  }

                  // Активируем эффект
                  highlightEffect.SetActive(true);
            }

            /// <summary>
            /// Восстанавливает исходный материал стены
            /// </summary>
            private void RestoreWallMaterial(ARPlane plane)
            {
                  if (plane != null && plane.GetComponent<MeshRenderer>() != null)
                  {
                        // Восстанавливаем исходный материал, если стена не была покрашена
                        if (!paintedWalls.ContainsKey(plane.gameObject))
                        {
                              if (plane.gameObject.TryGetComponent<MeshRenderer>(out MeshRenderer meshRenderer))
                              {
                                    // Используем сохраненный оригинальный материал или устанавливаем стандартный
                                    Material material = originalMaterials.ContainsKey(plane.gameObject)
                                          ? originalMaterials[plane.gameObject]
                                          : (defaultWallMaterial != null ? defaultWallMaterial : meshRenderer.material);

                                    meshRenderer.material = material;

                                    // Скрываем эффект выделения
                                    string effectName = $"HighlightEffect_{plane.trackableId}";
                                    Transform highlightEffect = plane.transform.Find(effectName);
                                    if (highlightEffect != null)
                                    {
                                          highlightEffect.gameObject.SetActive(false);
                                    }

                                    // Очищаем ссылку на текущую выделенную стену, если это она
                                    if (currentHighlightedWall == plane)
                                    {
                                          currentHighlightedWall = null;
                                          Debug.Log($"Выделение стены снято: {plane.trackableId}");
                                    }
                              }
                        }
                        else
                        {
                              Debug.Log($"Стена покрашена, не восстанавливаем материал: {plane.trackableId}");
                        }
                  }
            }

            /// <summary>
            /// Покраска стены в выбранный цвет
            /// </summary>
            private void PaintWall(Vector2 screenPosition)
            {
                  if (!isInitialized || currentPaintMaterial == null)
                        return;

                  // Проверяем, можно ли взаимодействовать с точкой (не UI)
                  if (planeVisibilityController != null && !planeVisibilityController.CanInteractWithPoint(screenPosition))
                  {
                        Debug.Log("Взаимодействие с точкой невозможно - попадание в UI");
                        return;
                  }

                  Debug.Log($"Попытка покрасить стену по позиции экрана: {screenPosition}");

                  // Используем улучшенный метод поиска плоскости
                  ARPlane plane = planeVisibilityController != null
                        ? planeVisibilityController.GetPlaneByScreenPosition(screenPosition, raycastManager, arCamera)
                        : null;

                  if (plane != null)
                  {
                        Debug.Log($"Найдена плоскость: {plane.trackableId}");

                        // Проверяем, является ли плоскость стеной
                        if (IsWall(plane))
                        {
                              Debug.Log($"Плоскость {plane.trackableId} является стеной, покраска...");

                              // Получаем компонент рендерера стены
                              MeshRenderer meshRenderer = plane.GetComponent<MeshRenderer>();
                              if (meshRenderer != null)
                              {
                                    // Применяем материал покраски
                                    meshRenderer.material = currentPaintMaterial;
                                    // Сохраняем в словаре
                                    paintedWalls[plane.gameObject] = currentPaintMaterial;
                                    lastPaintedWall = plane;

                                    // Если эта стена была выделена, обновляем статус
                                    if (currentHighlightedWall == plane)
                                    {
                                          currentHighlightedWall = null;
                                    }

                                    Debug.Log($"Стена покрашена в выбранный цвет: {currentPaintMaterial.name}");

                                    // Создаем визуальный эффект клика
                                    ShowTapEffect(screenPosition);
                              }

                              // Также покрасим кастомную визуализацию, если она есть
                              string visualName = $"CustomVisual_{plane.trackableId}";
                              Transform visualTrans = plane.transform.Find(visualName);
                              if (visualTrans != null)
                              {
                                    MeshRenderer visualRenderer = visualTrans.GetComponent<MeshRenderer>();
                                    if (visualRenderer != null)
                                    {
                                          visualRenderer.material = currentPaintMaterial;
                                          Debug.Log($"Кастомная визуализация стены также покрашена");
                                    }
                              }
                              else
                              {
                                    // Попробуем найти с другим именем (используется в некоторых местах)
                                    visualName = $"CustomPlaneVisual_{plane.trackableId}";
                                    visualTrans = plane.transform.Find(visualName);
                                    if (visualTrans != null)
                                    {
                                          MeshRenderer visualRenderer = visualTrans.GetComponent<MeshRenderer>();
                                          if (visualRenderer != null)
                                          {
                                                visualRenderer.material = currentPaintMaterial;
                                                Debug.Log($"Кастомная визуализация стены также покрашена");
                                          }
                                    }
                                    else
                                    {
                                          Debug.LogWarning($"Кастомная визуализация для плоскости {plane.trackableId} не найдена");
                                    }
                              }
                        }
                        else
                        {
                              Debug.Log($"Плоскость {plane.trackableId} не является стеной, игнорируем");
                        }
                  }
                  else
                  {
                        Debug.Log("Не удалось найти подходящую плоскость для покраски");
                  }
            }

            /// <summary>
            /// Создает визуальный эффект касания для обратной связи
            /// </summary>
            private void ShowTapEffect(Vector2 position)
            {
                  // Можно создать простой эффект касания здесь для обратной связи
                  // Например, создать временную частицу или спрайт

                  // Для теста просто выводим сообщение
                  Debug.Log("Создан эффект касания на экране");

                  // TODO: Добавить визуальный эффект в будущем
                  // Например:
                  /*
                  GameObject tapEffect = Instantiate(tapEffectPrefab, Vector3.zero, Quaternion.identity);
                  tapEffect.transform.position = arCamera.ScreenToWorldPoint(new Vector3(position.x, position.y, 0.5f));
                  Destroy(tapEffect, 0.5f);
                  */
            }

            /// <summary>
            /// Устанавливает размер кисти
            /// </summary>
            public void SetBrushSize(float size)
            {
                  brushSize = size;
            }

            /// <summary>
            /// Выбирает цвет для покраски по индексу
            /// </summary>
            public void SelectColor(int colorIndex)
            {
                  if (paintMaterials != null && colorIndex >= 0 && colorIndex < paintMaterials.Count)
                  {
                        selectedColorIndex = colorIndex;
                        currentPaintMaterial = paintMaterials[colorIndex];
                        Debug.Log($"Выбран цвет покраски: {colorIndex}");
                  }
            }

            /// <summary>
            /// Сбрасывает все окрашенные стены к исходным материалам
            /// </summary>
            private void ResetWalls()
            {
                  foreach (var wall in paintedWalls.Keys)
                  {
                        if (wall != null)
                        {
                              Renderer renderer = wall.GetComponent<Renderer>();
                              if (renderer != null && originalMaterials.ContainsKey(wall))
                              {
                                    renderer.material = originalMaterials[wall];
                              }
                        }
                  }

                  // Очищаем список окрашенных стен
                  paintedWalls.Clear();
                  Debug.Log("Все стены сброшены к исходному состоянию");
            }

            /// <summary>
            /// Делает скриншот текущего окрашенного помещения
            /// </summary>
            private void TakeScreenshot()
            {
                  ScreenshotManager screenshotManager = UnityEngine.Object.FindFirstObjectByType<ScreenshotManager>();
                  if (screenshotManager != null)
                  {
                        screenshotManager.CaptureScreenshot();
                        Debug.Log("Скриншот сохранен");
                  }
                  else
                  {
                        Debug.LogWarning("ScreenshotManager не найден в сцене");
                  }
            }

            /// <summary>
            /// Установка текущего материала для покраски
            /// </summary>
            public void SetPaintMaterial(Material material)
            {
                  if (material != null)
                  {
                        currentPaintMaterial = material;
                        Debug.Log($"Выбран материал для покраски: {material.name}");
                  }
            }

            /// <summary>
            /// Установка текущего материала для покраски по индексу
            /// </summary>
            public void SetPaintMaterialByIndex(int index)
            {
                  if (paintMaterials != null && index >= 0 && index < paintMaterials.Count)
                  {
                        currentPaintMaterial = paintMaterials[index];
                        Debug.Log($"Выбран материал для покраски: {currentPaintMaterial.name}");
                  }
            }

            /// <summary>
            /// Включает или отключает режим выделения стен
            /// </summary>
            public void ToggleHighlighting(bool enable)
            {
                  isHighlighting = enable;

                  // Если выделение отключено, убираем текущее выделение
                  if (!isHighlighting && currentHighlightedWall != null)
                  {
                        RestoreWallMaterial(currentHighlightedWall);
                        currentHighlightedWall = null;
                  }
            }

            /// <summary>
            /// Устанавливает видимость AR плоскостей
            /// </summary>
            /// <param name="visible">Отображать или скрыть плоскости</param>
            public void SetPlaneVisibility(bool visible)
            {
                  if (planeVisibilityController != null)
                  {
                        planeVisibilityController.SetPlaneVisibility(visible);
                  }
            }

            /// <summary>
            /// Включает или отключает подсветку стен
            /// </summary>
            /// <param name="enable">Включить или отключить подсветку</param>
            public void SetWallHighlighting(bool enable)
            {
                  if (planeVisibilityController != null)
                  {
                        planeVisibilityController.SetHighlightWalls(enable);
                  }
            }

            /// <summary>
            /// Сбрасывает все плоскости в состояние по умолчанию
            /// </summary>
            public void ResetPlanes()
            {
                  if (planeVisibilityController != null)
                  {
                        planeVisibilityController.ResetAllPlanes();
                  }
            }

            /// <summary>
            /// Освобождает ресурсы при уничтожении объекта
            /// </summary>
            private void OnDestroy()
            {
                  if (planeManager != null)
                  {
                        // Отписываемся от событий
                        planeManager.trackablesChanged.RemoveListener(OnPlanesChanged);

                        // Явно очищаем коллекцию плоскостей
                        verticalPlanes.Clear();
                  }

                  // Устраняем утечку памяти от NativeArray
                  Resources.UnloadUnusedAssets();
                  System.GC.Collect();
            }

            /// <summary>
            /// Обрабатывает стену, обнаруженную через ARPlaneVisibilityController
            /// </summary>
            public void OnWallDetected(GameObject wall)
            {
                  Debug.Log($"WallPainter: Получена стена {wall.name} через сообщение");

                  if (wall == null)
                        return;

                  // Настраиваем стену для взаимодействия
                  SetupWall(wall);

                  // Автоматически выделяем стену, чтобы пользователю было понятно, что её можно красить
                  MeshRenderer renderer = wall.GetComponent<MeshRenderer>();
                  if (renderer != null)
                  {
                        // Сохраняем оригинальный материал, если еще не сохранен
                        if (!originalMaterials.ContainsKey(wall))
                        {
                              originalMaterials[wall] = renderer.material;
                        }

                        // Создаем материал для выделения
                        Material highlightMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                        highlightMat.color = new Color(1.0f, 0.9f, 0.0f, 1.0f); // Яркий желтый

                        // Включаем эмиссию для лучшей видимости
                        if (highlightMat.HasProperty("_EmissionColor"))
                        {
                              highlightMat.EnableKeyword("_EMISSION");
                              highlightMat.SetColor("_EmissionColor", new Color(1.0f, 0.9f, 0.0f, 1.0f));
                              highlightMat.SetFloat("_EmissionIntensity", 1.5f);
                        }

                        // Применяем материал с эффектом выделения
                        renderer.material = highlightMat;

                        // Отключаем тени для улучшения видимости
                        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        renderer.receiveShadows = false;

                        // Добавляем стену в список обрабатываемых
                        ARPlane plane = wall.GetComponent<ARPlane>();
                        if (plane != null && IsWall(plane) && !verticalPlanes.Contains(plane))
                        {
                              verticalPlanes.Add(plane);
                              Debug.Log($"Добавлена новая стена в список вертикальных плоскостей: {plane.trackableId}");
                        }

                        Debug.Log($"Стена успешно настроена и выделена: {wall.name}");
                  }
                  else
                  {
                        Debug.LogWarning($"У стены {wall.name} отсутствует компонент MeshRenderer");
                  }
            }

            /// <summary>
            /// Обрабатывает создание демонстрационной стены из ARPlaneVisibilityController
            /// </summary>
            /// <param name="wall">GameObject стены</param>
            public void OnDemoWallCreated(GameObject wall)
            {
                  if (wall == null)
                  {
                        Debug.LogError("OnDemoWallCreated: wall is null");
                        return;
                  }

                  // Проверяем, что стена имеет компонент PaintableSurface
                  var paintableSurface = wall.GetComponent<PaintableSurface>();
                  if (paintableSurface == null)
                  {
                        Debug.LogWarning($"Wall {wall.name} doesn't have PaintableSurface component, adding it");
                        paintableSurface = wall.AddComponent<PaintableSurface>();
                  }

                  // Проверяем наличие необходимых компонентов
                  if (!wall.TryGetComponent<MeshRenderer>(out var renderer))
                  {
                        Debug.LogError($"Wall {wall.name} doesn't have MeshRenderer");
                        return;
                  }

                  if (!wall.TryGetComponent<MeshCollider>(out var collider))
                  {
                        Debug.LogWarning($"Wall {wall.name} doesn't have MeshCollider, adding it");
                        collider = wall.AddComponent<MeshCollider>();
                  }

                  // Добавляем стену в список доступных для рисования
                  AddWallToTrackedWalls(wall);

                  Debug.Log($"Demo wall {wall.name} added to tracked walls");
            }

            /// <summary>
            /// Добавляет стену в список отслеживаемых стен
            /// </summary>
            private void AddWallToTrackedWalls(GameObject wall)
            {
                  if (wall == null || _trackedWalls.Contains(wall))
                        return;

                  _trackedWalls.Add(wall);

                  // Если это первая стена и автоматический выбор включен, выбираем её
                  if (_trackedWalls.Count == 1 && _autoSelectFirstWall)
                  {
                        SetCurrentWall(wall);
                  }
            }

            /// <summary>
            /// Устанавливает указанную стену как текущую для рисования
            /// </summary>
            private void SetCurrentWall(GameObject wall)
            {
                  if (wall == null)
                        return;

                  // Сбрасываем подсветку с предыдущей стены, если она была
                  if (currentHighlightedWall != null)
                  {
                        RestoreWallMaterial(currentHighlightedWall);
                  }

                  // Устанавливаем новую текущую стену
                  currentHighlightedWall = wall.GetComponent<ARPlane>();

                  // Подсвечиваем новую выбранную стену
                  HighlightWallMaterial(currentHighlightedWall);

                  // Обновляем UI и другие компоненты, если необходимо
                  UpdateUIForSelectedWall();
            }

            /// <summary>
            /// Обновляет UI для выбранной стены
            /// </summary>
            private void UpdateUIForSelectedWall()
            {
                  // Логика обновления UI и других компонентов, если необходимо
                  Debug.Log($"Выбрана стена: {(currentHighlightedWall != null ? currentHighlightedWall.trackableId : "нет")}");
            }
      }

      // Add PaintableSurface class
      /// <summary>
      /// Компонент, указывающий, что объект может быть раскрашен
      /// </summary>
      [RequireComponent(typeof(MeshRenderer))]
      [RequireComponent(typeof(MeshFilter))]
      [RequireComponent(typeof(MeshCollider))]
      public class PaintableSurface : MonoBehaviour
      {
            [Header("Свойства поверхности")]
            [SerializeField] private Color _defaultColor = Color.white;
            [SerializeField] private float _width = 1.0f;
            [SerializeField] private float _height = 1.0f;

            [Header("Настройки рисования")]
            [SerializeField] private bool _canDrawOn = true;
            [SerializeField] private float _brushScale = 1.0f;
            [SerializeField] private Material _defaultMaterial;

            /// <summary>
            /// Можно ли рисовать на этой поверхности
            /// </summary>
            public bool CanDrawOn => _canDrawOn;

            /// <summary>
            /// Масштаб кисти для этой поверхности
            /// </summary>
            public float BrushScale => _brushScale;

            /// <summary>
            /// Цвет поверхности по умолчанию
            /// </summary>
            public Color DefaultColor => _defaultColor;

            /// <summary>
            /// Ширина поверхности
            /// </summary>
            public float Width => _width;

            /// <summary>
            /// Высота поверхности
            /// </summary>  
            public float Height => _height;

            private void OnValidate()
            {
                  // Обновляем размеры и материал при изменении в инспекторе
                  UpdateSizeAndMaterial();
            }

            private void Awake()
            {
                  // Убеждаемся, что у нас есть все необходимые компоненты
                  if (GetComponent<MeshRenderer>() == null || GetComponent<MeshFilter>() == null || GetComponent<MeshCollider>() == null)
                  {
                        Debug.LogError($"На объекте {gameObject.name} отсутствует один из необходимых компонентов: MeshRenderer, MeshFilter или MeshCollider");
                  }

                  // Обновляем размеры и материал при создании
                  UpdateSizeAndMaterial();
            }

            /// <summary>
            /// Обновляет размеры и материал поверхности
            /// </summary>
            private void UpdateSizeAndMaterial()
            {
                  try
                  {
                        // Обновляем размеры меша, если он есть
                        MeshFilter meshFilter = GetComponent<MeshFilter>();
                        if (meshFilter != null && meshFilter.sharedMesh != null)
                        {
                              // Или если нужно, создаем новый меш
                        }

                        // Обновляем материал, если он есть
                        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
                        if (meshRenderer != null)
                        {
                              if (_defaultMaterial != null)
                              {
                                    meshRenderer.material = _defaultMaterial;
                              }
                              else
                              {
                                    // Создаем простой материал
                                    Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                                    material.color = _defaultColor;
                                    meshRenderer.material = material;
                              }
                        }

                        // Обновляем коллайдер
                        MeshCollider meshCollider = GetComponent<MeshCollider>();
                        if (meshCollider != null && meshFilter != null && meshFilter.sharedMesh != null)
                        {
                              meshCollider.sharedMesh = meshFilter.sharedMesh;
                        }
                  }
                  catch (System.Exception ex)
                  {
                        Debug.LogError($"Ошибка при обновлении размеров и материала: {ex.Message}");
                  }
            }

            /// <summary>
            /// Устанавливает новый цвет поверхности
            /// </summary>
            public void SetColor(Color newColor)
            {
                  _defaultColor = newColor;
                  MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
                  if (meshRenderer != null && meshRenderer.material != null)
                  {
                        meshRenderer.material.color = newColor;
                  }
            }

            /// <summary>
            /// Устанавливает новые размеры поверхности
            /// </summary>
            public void SetSize(float width, float height)
            {
                  _width = width;
                  _height = height;
                  UpdateSizeAndMaterial();
            }
      }
}