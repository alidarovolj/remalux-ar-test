#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using UnityEngine.UI;

namespace Remalux.AR
{
      /// <summary>
      /// Окно редактора для настройки системы покраски стен
      /// </summary>
      public class WallPaintingSetupWindow : EditorWindow
      {
            // Префабы
            private GameObject buttonPrefab;
            private GameObject colorPreviewPrefab;

            // Материалы
            private Material defaultWallMaterial;
            private List<Material> paintMaterials = new List<Material>();

            // Ссылки на сцену
            private Camera mainCamera;
            private Canvas mainCanvas;

            // Настройки слоя стен
            private int wallLayerIndex = 8; // По умолчанию слой 8 (можно изменить)
            private string wallLayerName = "Wall";

            // Состояние настройки
            private bool prefabsValid = false;
            private bool materialsValid = false;
            private bool sceneReferencesValid = false;
            private bool layerSetupValid = false;

            // Пути к директориям
            private string prefabsPath = "Assets/Prefabs/UI";
            private string materialsPath = "Assets/Materials/Paints";

            // Прокрутка
            private Vector2 materialsScrollPosition;

            [MenuItem("Remalux/Настройка покраски стен")]
            public static void ShowWindow()
            {
                  WallPaintingSetupWindow window = GetWindow<WallPaintingSetupWindow>("Настройка покраски стен");
                  window.minSize = new Vector2(400, 600);
                  window.Show();
            }

            private void OnEnable()
            {
                  // Находим камеру и канвас
                  mainCamera = Camera.main;
                  mainCanvas = FindObjectOfType<Canvas>();

                  // Проверяем наличие слоя Wall
                  CheckWallLayer();

                  // Загружаем материалы
                  LoadMaterials();

                  // Загружаем префабы
                  LoadPrefabs();
            }

            private void OnGUI()
            {
                  GUILayout.Label("Настройка системы покраски стен", EditorStyles.boldLabel);
                  EditorGUILayout.Space();

                  DrawPrefabsSection();
                  EditorGUILayout.Space();

                  DrawMaterialsSection();
                  EditorGUILayout.Space();

                  DrawSceneReferencesSection();
                  EditorGUILayout.Space();

                  DrawLayerSetupSection();
                  EditorGUILayout.Space();

                  DrawSetupButton();
            }

            private void DrawPrefabsSection()
            {
                  GUILayout.Label("1. Префабы", EditorStyles.boldLabel);

                  EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                  buttonPrefab = EditorGUILayout.ObjectField("Префаб кнопки цвета", buttonPrefab, typeof(GameObject), false) as GameObject;
                  colorPreviewPrefab = EditorGUILayout.ObjectField("Префаб превью цвета", colorPreviewPrefab, typeof(GameObject), false) as GameObject;

                  EditorGUILayout.Space();

                  if (GUILayout.Button("Создать базовые префабы"))
                  {
                        CreateBasicPrefabs();
                  }

                  prefabsValid = buttonPrefab != null && colorPreviewPrefab != null;

                  EditorGUILayout.EndVertical();
            }

            private void DrawMaterialsSection()
            {
                  GUILayout.Label("2. Материалы", EditorStyles.boldLabel);

                  EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                  defaultWallMaterial = EditorGUILayout.ObjectField("Материал стены по умолчанию", defaultWallMaterial, typeof(Material), false) as Material;

                  EditorGUILayout.Space();
                  EditorGUILayout.LabelField("Материалы для покраски:");

                  // Отображаем информацию о количестве материалов
                  EditorGUILayout.HelpBox($"Загружено материалов: {paintMaterials.Count}", MessageType.Info);

                  materialsScrollPosition = EditorGUILayout.BeginScrollView(materialsScrollPosition, GUILayout.Height(150));

                  for (int i = 0; i < paintMaterials.Count; i++)
                  {
                        EditorGUILayout.BeginHorizontal();
                        paintMaterials[i] = EditorGUILayout.ObjectField($"Материал {i + 1}", paintMaterials[i], typeof(Material), false) as Material;

                        if (GUILayout.Button("X", GUILayout.Width(25)))
                        {
                              paintMaterials.RemoveAt(i);
                              GUIUtility.ExitGUI();
                        }

                        EditorGUILayout.EndHorizontal();
                  }

                  EditorGUILayout.EndScrollView();

                  if (GUILayout.Button("Добавить материал"))
                  {
                        paintMaterials.Add(null);
                  }

                  EditorGUILayout.Space();

                  if (GUILayout.Button("Создать базовые материалы"))
                  {
                        CreateBasicMaterials();
                  }

                  materialsValid = defaultWallMaterial != null && paintMaterials.Count > 0 && !paintMaterials.Contains(null);

                  EditorGUILayout.EndVertical();
            }

            private void DrawSceneReferencesSection()
            {
                  GUILayout.Label("3. Ссылки на сцену", EditorStyles.boldLabel);

                  EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                  mainCamera = EditorGUILayout.ObjectField("Основная камера", mainCamera, typeof(Camera), true) as Camera;
                  mainCanvas = EditorGUILayout.ObjectField("Основной Canvas", mainCanvas, typeof(Canvas), true) as Canvas;

                  EditorGUILayout.Space();

                  EditorGUILayout.BeginHorizontal();
                  if (GUILayout.Button("Найти камеру"))
                  {
                        mainCamera = Camera.main;
                        if (mainCamera == null)
                              mainCamera = FindObjectOfType<Camera>();
                  }

                  if (GUILayout.Button("Найти Canvas"))
                  {
                        mainCanvas = FindObjectOfType<Canvas>();
                        if (mainCanvas == null)
                        {
                              // Создаем Canvas, если не найден
                              GameObject canvasObj = new GameObject("Canvas");
                              mainCanvas = canvasObj.AddComponent<Canvas>();
                              mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                              canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                              canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                        }
                  }
                  EditorGUILayout.EndHorizontal();

                  sceneReferencesValid = mainCamera != null && mainCanvas != null;

                  EditorGUILayout.EndVertical();
            }

            private void DrawLayerSetupSection()
            {
                  GUILayout.Label("4. Настройка слоя стен", EditorStyles.boldLabel);

                  EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                  wallLayerName = EditorGUILayout.TextField("Имя слоя стен", wallLayerName);
                  wallLayerIndex = EditorGUILayout.IntSlider("Индекс слоя стен", wallLayerIndex, 8, 31);

                  EditorGUILayout.Space();

                  if (GUILayout.Button("Настроить слой стен"))
                  {
                        SetupWallLayer();
                  }

                  layerSetupValid = !string.IsNullOrEmpty(wallLayerName) && IsLayerDefined(wallLayerName);

                  EditorGUILayout.EndVertical();
            }

            private void DrawSetupButton()
            {
                  EditorGUILayout.Space();

                  // Обновляем состояние валидации
                  prefabsValid = buttonPrefab != null && colorPreviewPrefab != null;
                  materialsValid = defaultWallMaterial != null && paintMaterials.Count > 0;
                  sceneReferencesValid = mainCamera != null && mainCanvas != null;
                  layerSetupValid = !string.IsNullOrEmpty(wallLayerName) && IsLayerDefined(wallLayerName);

                  // Кнопка активна только если все условия выполнены
                  GUI.enabled = prefabsValid && materialsValid && sceneReferencesValid && layerSetupValid;

                  EditorGUILayout.BeginHorizontal();
                  GUILayout.FlexibleSpace();

                  if (GUILayout.Button("Настроить систему покраски стен", GUILayout.Height(40), GUILayout.Width(300)))
                  {
                        try
                        {
                              SetupWallPaintingSystem();
                              Debug.Log("Система покраски стен успешно настроена!");
                        }
                        catch (System.Exception e)
                        {
                              Debug.LogError($"Ошибка при настройке системы покраски стен: {e.Message}");
                        }
                  }

                  GUILayout.FlexibleSpace();
                  EditorGUILayout.EndHorizontal();

                  GUI.enabled = true;

                  EditorGUILayout.Space();

                  if (!prefabsValid || !materialsValid || !sceneReferencesValid || !layerSetupValid)
                  {
                        EditorGUILayout.HelpBox("Для настройки системы необходимо выполнить все шаги выше.", MessageType.Warning);
                  }
            }

            private void CreateBasicPrefabs()
            {
                  // Создаем директорию, если она не существует
                  if (!Directory.Exists(prefabsPath))
                  {
                        Directory.CreateDirectory(prefabsPath);
                  }

                  // Создаем префаб кнопки
                  if (buttonPrefab == null)
                  {
                        GameObject buttonObj = new GameObject("ColorButton");
                        RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
                        rectTransform.sizeDelta = new Vector2(60, 60);

                        UnityEngine.UI.Image image = buttonObj.AddComponent<UnityEngine.UI.Image>();
                        image.color = Color.white;

                        UnityEngine.UI.Button button = buttonObj.AddComponent<UnityEngine.UI.Button>();
                        ColorBlock colors = button.colors;
                        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
                        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
                        button.colors = colors;

                        buttonObj.AddComponent<SimpleColorButton>();

                        // Сохраняем префаб
                        string prefabPath = $"{prefabsPath}/ColorButton.prefab";

#if UNITY_2018_3_OR_NEWER
                buttonPrefab = PrefabUtility.SaveAsPrefabAsset(buttonObj, prefabPath);
#else
                        buttonPrefab = PrefabUtility.CreatePrefab(prefabPath, buttonObj);
#endif

                        DestroyImmediate(buttonObj);
                  }

                  // Создаем префаб превью цвета
                  if (colorPreviewPrefab == null)
                  {
                        GameObject previewObj = new GameObject("ColorPreview");

                        // Добавляем компоненты для отображения
                        MeshFilter meshFilter = previewObj.AddComponent<MeshFilter>();
                        MeshRenderer renderer = previewObj.AddComponent<MeshRenderer>();

                        // Создаем простой круглый меш для превью
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

                        // Добавляем компонент SimpleColorPreview
                        previewObj.AddComponent<SimpleColorPreview>();

                        // Сохраняем префаб
                        string prefabPath = $"{prefabsPath}/ColorPreview.prefab";

#if UNITY_2018_3_OR_NEWER
                colorPreviewPrefab = PrefabUtility.SaveAsPrefabAsset(previewObj, prefabPath);
#else
                        colorPreviewPrefab = PrefabUtility.CreatePrefab(prefabPath, previewObj);
#endif

                        DestroyImmediate(previewObj);
                  }

                  AssetDatabase.Refresh();
            }

            private void CreateBasicMaterials()
            {
                  Debug.Log("Начинаем создание базовых материалов...");

                  // Создаем директорию, если она не существует
                  if (!Directory.Exists(materialsPath))
                  {
                        Directory.CreateDirectory(materialsPath);
                        Debug.Log($"Создана директория для материалов: {materialsPath}");
                  }

                  // Создаем GameObject с компонентом MaterialCreator
                  GameObject creatorObj = new GameObject("MaterialCreator");
                  MaterialCreator creator = creatorObj.AddComponent<MaterialCreator>();

                  // Проверяем, что компонент добавлен успешно
                  if (creator == null)
                  {
                        Debug.LogError("Не удалось создать компонент MaterialCreator");
                        DestroyImmediate(creatorObj);
                        return;
                  }

                  Debug.Log("Компонент MaterialCreator создан успешно");

                  // Вызываем метод создания материалов
                  creator.CreateAllMaterials();
                  Debug.Log("Метод CreateAllMaterials() вызван");

                  // Удаляем временный GameObject
                  DestroyImmediate(creatorObj);

                  // Загружаем созданные материалы
                  LoadMaterials();
                  Debug.Log("Материалы загружены после создания");
            }

            private void LoadMaterials()
            {
                  // Очищаем список материалов
                  paintMaterials.Clear();

                  // Проверяем существование директории
                  if (!Directory.Exists(materialsPath))
                  {
                        Debug.LogWarning($"Директория {materialsPath} не существует. Создайте базовые материалы.");
                        return;
                  }

                  Debug.Log($"Загрузка материалов из директории: {materialsPath}");

                  // Загружаем материал по умолчанию
                  string defaultPath = $"{materialsPath}/DefaultWallMaterial.mat";
                  if (File.Exists(defaultPath))
                  {
                        defaultWallMaterial = AssetDatabase.LoadAssetAtPath<Material>(defaultPath);
                        Debug.Log($"Загружен материал по умолчанию: {defaultWallMaterial.name}");
                  }
                  else
                  {
                        Debug.LogWarning("Материал DefaultWallMaterial.mat не найден. Создайте базовые материалы.");
                  }

                  // Получаем все файлы .mat в директории
                  string[] materialFiles = Directory.GetFiles(materialsPath, "*.mat");
                  Debug.Log($"Найдено {materialFiles.Length} материалов в директории");

                  foreach (string file in materialFiles)
                  {
                        // Пропускаем материал по умолчанию
                        if (file.Contains("DefaultWallMaterial"))
                              continue;

                        // Загружаем материал
                        string assetPath = file.Replace('\\', '/');
                        Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                        if (material != null)
                        {
                              paintMaterials.Add(material);
                              Debug.Log($"Добавлен материал для покраски: {material.name}");
                        }
                  }

                  // Проверяем, загружены ли материалы
                  materialsValid = defaultWallMaterial != null && paintMaterials.Count > 0;
                  Debug.Log($"Загружено материалов для покраски: {paintMaterials.Count}, материал по умолчанию: {(defaultWallMaterial != null ? "Загружен" : "Не загружен")}");
            }

            private void LoadPrefabs()
            {
                  // Проверяем существование директории
                  if (!Directory.Exists(prefabsPath))
                  {
                        Debug.LogWarning($"Директория {prefabsPath} не существует. Создайте базовые префабы.");
                        return;
                  }

                  // Загружаем префабы
                  string buttonPath = $"{prefabsPath}/ColorButton.prefab";
                  string previewPath = $"{prefabsPath}/ColorPreview.prefab";

                  if (File.Exists(buttonPath))
                  {
                        buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(buttonPath);
                  }
                  else
                  {
                        Debug.LogWarning("Префаб ColorButton.prefab не найден. Создайте базовые префабы.");
                  }

                  if (File.Exists(previewPath))
                  {
                        colorPreviewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(previewPath);
                  }
                  else
                  {
                        Debug.LogWarning("Префаб ColorPreview.prefab не найден. Создайте базовые префабы.");
                  }

                  // Проверяем, загружены ли префабы
                  prefabsValid = buttonPrefab != null && colorPreviewPrefab != null;
            }

            private void CheckWallLayer()
            {
                  // Проверяем, существует ли слой Wall
                  for (int i = 8; i < 32; i++)
                  {
                        string layerName = LayerMask.LayerToName(i);
                        if (layerName == wallLayerName)
                        {
                              wallLayerIndex = i;
                              layerSetupValid = true;
                              break;
                        }
                  }
            }

            private void SetupWallLayer()
            {
                  // Получаем путь к файлу TagManager.asset
                  SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                  SerializedProperty layers = tagManager.FindProperty("layers");

                  // Проверяем, существует ли уже слой с таким именем
                  bool layerExists = false;
                  for (int i = 8; i < 32; i++)
                  {
                        SerializedProperty layerProp = layers.GetArrayElementAtIndex(i);
                        if (layerProp.stringValue == wallLayerName)
                        {
                              layerExists = true;
                              wallLayerIndex = i;
                              break;
                        }
                  }

                  // Если слой не существует, создаем его
                  if (!layerExists)
                  {
                        SerializedProperty layerProp = layers.GetArrayElementAtIndex(wallLayerIndex);
                        layerProp.stringValue = wallLayerName;
                        tagManager.ApplyModifiedProperties();
                  }

                  layerSetupValid = true;
                  Debug.Log($"Слой '{wallLayerName}' настроен с индексом {wallLayerIndex}");
            }

            private bool IsLayerDefined(string layerName)
            {
                  // Проверяем, определен ли слой
                  for (int i = 0; i < 32; i++)
                  {
                        if (LayerMask.LayerToName(i) == layerName)
                              return true;
                  }
                  return false;
            }

            private void SetupWallPaintingSystem()
            {
                  // Проверяем наличие материалов перед настройкой
                  if (defaultWallMaterial == null)
                  {
                        Debug.LogError("Default Wall Material не задан. Пожалуйста, создайте базовые материалы перед настройкой системы.");
                        return;
                  }

                  if (paintMaterials.Count == 0)
                  {
                        Debug.LogError("Не заданы материалы для покраски. Пожалуйста, создайте базовые материалы перед настройкой системы.");
                        return;
                  }

                  Debug.Log($"Настройка системы покраски стен с {paintMaterials.Count} материалами и материалом по умолчанию {defaultWallMaterial.name}");

                  // Удаляем существующие объекты, если они есть
                  GameObject existingSystem = GameObject.Find("WallPaintingSystem");
                  if (existingSystem != null)
                  {
                        DestroyImmediate(existingSystem);
                  }

                  GameObject existingWallPainter = GameObject.Find("WallPainter");
                  if (existingWallPainter != null)
                  {
                        DestroyImmediate(existingWallPainter);
                  }

                  GameObject existingManager = GameObject.Find("WallPaintingManager");
                  if (existingManager != null)
                  {
                        DestroyImmediate(existingManager);
                  }

                  // Создаем GameObject для SceneSetup
                  GameObject setupObj = new GameObject("WallPaintingSystem");
                  SceneSetup sceneSetup = setupObj.AddComponent<SceneSetup>();

                  // Устанавливаем префабы через публичные поля
                  sceneSetup.buttonPrefab = buttonPrefab;
                  sceneSetup.colorPreviewPrefab = colorPreviewPrefab;

                  // Устанавливаем материалы через публичные поля
                  sceneSetup.defaultWallMaterial = defaultWallMaterial;
                  sceneSetup.paintMaterials = paintMaterials.ToArray();

                  // Устанавливаем ссылки на сцену через публичные поля
                  sceneSetup.mainCamera = mainCamera;
                  sceneSetup.mainCanvas = mainCanvas;

                  // Настраиваем слой стен для WallPainter
                  // Создаем LayerMask для слоя стен
                  LayerMask wallLayerMask = 1 << wallLayerIndex;

                  // Сохраняем LayerMask в SceneSetup
                  sceneSetup.wallLayerMask = wallLayerMask;

                  // Вызываем метод SetupScene() вручную, чтобы убедиться, что все компоненты созданы
                  sceneSetup.SetupScene();

                  // Проверяем, что все компоненты созданы и настроены
                  GameObject wallPainterObj = GameObject.Find("WallPainter");
                  GameObject managerObj = GameObject.Find("WallPaintingManager");

                  if (wallPainterObj != null && managerObj != null)
                  {
                        WallPainter wallPainter = wallPainterObj.GetComponent<WallPainter>();
                        WallPaintingManager manager = managerObj.GetComponent<WallPaintingManager>();

                        if (wallPainter != null && manager != null)
                        {
                              if (manager.wallPainter == null)
                                    manager.wallPainter = wallPainter;

                              // Явно назначаем материалы
                              manager.defaultWallMaterial = defaultWallMaterial;
                              manager.paintMaterials = paintMaterials.ToArray();

                              // Находим SimplePaintColorSelector
                              SimplePaintColorSelector colorSelector = FindObjectOfType<SimplePaintColorSelector>();
                              if (colorSelector != null)
                              {
                                    if (manager.colorSelector == null)
                                          manager.colorSelector = colorSelector;

                                    if (colorSelector.wallPainter == null)
                                          colorSelector.wallPainter = wallPainter;

                                    // Явно назначаем материалы селектору цветов
                                    colorSelector.paintMaterials = paintMaterials.ToArray();

                                    // Настраиваем UI
                                    if (manager.paintingUI == null)
                                          manager.paintingUI = colorSelector.gameObject;

                                    // Вызываем Initialize() для создания кнопок
                                    try
                                    {
                                          colorSelector.Initialize();
                                    }
                                    catch (System.Exception e)
                                    {
                                          Debug.LogError($"Ошибка при инициализации SimplePaintColorSelector: {e.Message}");
                                    }
                              }

                              // Проверяем, что материалы были правильно назначены
                              WallPaintingManager finalManager = FindObjectOfType<WallPaintingManager>();
                              if (finalManager != null)
                              {
                                    string materialsInfo = finalManager.paintMaterials != null ?
                                          $"Количество: {finalManager.paintMaterials.Length}" : "null";

                                    Debug.Log($"Проверка настройки WallPaintingManager: " +
                                          $"Default Material: {(finalManager.defaultWallMaterial != null ? finalManager.defaultWallMaterial.name : "null")}, " +
                                          $"Paint Materials: {materialsInfo}");
                              }
                        }
                  }

                  Debug.Log("Система покраски стен успешно настроена!");

                  // Закрываем окно
                  Close();
            }
      }
}
#endif