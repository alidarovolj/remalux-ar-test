using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;
using System.IO;
using UnityEngine.XR.Management;
using Unity.XR.CoreUtils;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

namespace Remalux.AR.Editor
{
      /// <summary>
      /// Класс для создания редакторских функций меню Remalux
      /// </summary>
      public static class RemaluxMenu
      {
            [MenuItem("Window/Remalux/Wall Paint", false, 10)]
            public static void CreateWallPaintScene()
            {
                  // Спрашиваем пользователя, хочет ли он сохранить текущую сцену
                  if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        return;

                  // Создаем новую сцену
                  Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                  scene.name = "WallPaintScene";

                  // Удаляем стандартную камеру, если она существует
                  Camera mainCamera = Object.FindFirstObjectByType<Camera>();
                  if (mainCamera != null)
                  {
                        Object.DestroyImmediate(mainCamera.gameObject);
                  }

                  // Создаем AR объекты и настраиваем их
                  CreateARSetup();

                  // Создаем UI
                  CreateUI();

                  // Создаем материалы для покраски стен
                  CreatePaintMaterials();

                  // Сохраняем сцену
                  string scenePath = "Assets/WallPaintScene.unity";
                  EditorSceneManager.SaveScene(scene, scenePath);

                  Debug.Log($"Сцена Wall Paint создана и сохранена по пути: {scenePath}");

                  // Открываем сцену
                  EditorSceneManager.OpenScene(scenePath);
            }

            /// <summary>
            /// Создает необходимые AR объекты и добавляет компоненты для работы с AR
            /// </summary>
            private static void CreateARSetup()
            {
                  // Создаем AR сессию
                  GameObject arSessionObject = new GameObject("AR Session");
                  arSessionObject.AddComponent<ARSession>();
                  arSessionObject.AddComponent<ARInputManager>();

                  // Создаем XR Origin вместо AR Session Origin для совместимости с новыми версиями
                  GameObject xrOriginObject = new GameObject("XR Origin");
                  XROrigin xrOrigin = xrOriginObject.AddComponent<XROrigin>();

                  // Создаем объект Camera Offset как потомок XR Origin
                  GameObject cameraOffsetObject = new GameObject("Camera Offset");
                  cameraOffsetObject.transform.SetParent(xrOriginObject.transform, false);

                  // Создаем AR камеру как потомок Camera Offset
                  GameObject arCameraObject = new GameObject("AR Camera");
                  arCameraObject.transform.SetParent(cameraOffsetObject.transform, false);

                  // Добавляем компоненты камеры
                  Camera arCamera = arCameraObject.AddComponent<Camera>();
                  arCamera.clearFlags = CameraClearFlags.SolidColor;
                  arCamera.backgroundColor = Color.black;
                  arCamera.nearClipPlane = 0.1f;
                  arCamera.farClipPlane = 30f;

                  arCameraObject.AddComponent<AudioListener>();
                  arCameraObject.AddComponent<ARCameraManager>();
                  arCameraObject.AddComponent<ARCameraBackground>();

                  // Добавляем TrackedPoseDriver вместо устаревшего ARPoseDriver
                  TrackedPoseDriver poseDriver = arCameraObject.AddComponent<TrackedPoseDriver>();
                  poseDriver.positionAction = new InputAction("Position", binding: "<XRHMD>/centerEyePosition", expectedControlType: "Vector3");
                  poseDriver.rotationAction = new InputAction("Rotation", binding: "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion");
                  poseDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
                  poseDriver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
                  poseDriver.positionAction.Enable();
                  poseDriver.rotationAction.Enable();

                  // Устанавливаем необходимые ссылки для XROrigin
                  xrOrigin.Camera = arCamera;
                  xrOrigin.CameraFloorOffsetObject = cameraOffsetObject;
                  // Не устанавливаем TrackablesParent, так как это свойство только для чтения
                  // XROrigin сам создаст TrackablesParent при инициализации

                  // Создаем объект для отслеживания плоскостей
                  GameObject trackablesObject = new GameObject("Trackables");
                  trackablesObject.transform.SetParent(xrOriginObject.transform, false);

                  // Добавляем компоненты AR плоскостей на XR Origin
                  ARPlaneManager planeManager = xrOriginObject.AddComponent<ARPlaneManager>();
                  xrOriginObject.AddComponent<ARRaycastManager>();
                  xrOriginObject.AddComponent<ARPointCloudManager>();

                  // Создаем объект для визуализации плоскостей
                  GameObject arPlanePrefab = new GameObject("AR Plane Visualizer");
                  MeshRenderer meshRenderer = arPlanePrefab.AddComponent<MeshRenderer>();
                  MeshFilter meshFilter = arPlanePrefab.AddComponent<MeshFilter>();

                  // Скрываем префаб плоскости в сцене
                  arPlanePrefab.SetActive(false);

                  // Устанавливаем префаб для визуализации плоскостей
                  planeManager.planePrefab = arPlanePrefab;

                  // Добавляем основные компоненты для покраски стен на XR Origin
                  WallPainter wallPainter = xrOriginObject.AddComponent<WallPainter>();

                  // Устанавливаем ссылки на компоненты в WallPainter
                  SerializedObject serializedObject = new SerializedObject(wallPainter);
                  serializedObject.FindProperty("raycastManager").objectReferenceValue = xrOriginObject.GetComponent<ARRaycastManager>();
                  serializedObject.FindProperty("planeManager").objectReferenceValue = planeManager;
                  serializedObject.FindProperty("arCamera").objectReferenceValue = arCamera;
                  serializedObject.ApplyModifiedProperties();
            }

            /// <summary>
            /// Создает UI интерфейс для функции покраски стен
            /// </summary>
            private static void CreateUI()
            {
                  // Создаем Canvas
                  GameObject canvasObject = new GameObject("Canvas");
                  Canvas canvas = canvasObject.AddComponent<Canvas>();
                  canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                  canvasObject.AddComponent<CanvasScaler>();
                  canvasObject.AddComponent<GraphicRaycaster>();

                  // Создаем EventSystem
                  if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                  {
                        GameObject eventSystemObject = new GameObject("EventSystem");
                        eventSystemObject.AddComponent<UnityEngine.EventSystems.EventSystem>();
                        eventSystemObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                  }

                  // Создаем основную панель UI
                  GameObject mainPanelObject = new GameObject("MainPanel");
                  mainPanelObject.transform.SetParent(canvasObject.transform, false);
                  RectTransform mainPanelRect = mainPanelObject.AddComponent<RectTransform>();
                  mainPanelRect.anchorMin = new Vector2(0, 0);
                  mainPanelRect.anchorMax = new Vector2(1, 1);
                  mainPanelRect.offsetMin = Vector2.zero;
                  mainPanelRect.offsetMax = Vector2.zero;

                  // Создаем панель цветов
                  GameObject colorPaletteObject = new GameObject("ColorPalettePanel");
                  colorPaletteObject.transform.SetParent(canvasObject.transform, false);
                  RectTransform colorPaletteRect = colorPaletteObject.AddComponent<RectTransform>();
                  colorPaletteRect.anchorMin = new Vector2(0, 0);
                  colorPaletteRect.anchorMax = new Vector2(1, 1);
                  colorPaletteRect.offsetMin = Vector2.zero;
                  colorPaletteRect.offsetMax = Vector2.zero;
                  Image colorPaletteImage = colorPaletteObject.AddComponent<Image>();
                  colorPaletteImage.color = new Color(0, 0, 0, 0.8f);
                  colorPaletteObject.SetActive(false);

                  // Создаем контейнер для кнопок цветов
                  GameObject colorButtonsContainer = new GameObject("ColorButtonsContainer");
                  colorButtonsContainer.transform.SetParent(colorPaletteObject.transform, false);
                  RectTransform colorButtonsContainerRect = colorButtonsContainer.AddComponent<RectTransform>();
                  colorButtonsContainerRect.anchorMin = new Vector2(0.1f, 0.2f);
                  colorButtonsContainerRect.anchorMax = new Vector2(0.9f, 0.8f);
                  colorButtonsContainerRect.offsetMin = Vector2.zero;
                  colorButtonsContainerRect.offsetMax = Vector2.zero;
                  GridLayoutGroup gridLayout = colorButtonsContainer.AddComponent<GridLayoutGroup>();
                  gridLayout.cellSize = new Vector2(80, 80);
                  gridLayout.spacing = new Vector2(20, 20);
                  gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
                  gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
                  gridLayout.childAlignment = TextAnchor.MiddleCenter;
                  gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                  gridLayout.constraintCount = 3;

                  // Создаем панель настроек кисти
                  GameObject brushSettingsObject = new GameObject("BrushSettingsPanel");
                  brushSettingsObject.transform.SetParent(canvasObject.transform, false);
                  RectTransform brushSettingsRect = brushSettingsObject.AddComponent<RectTransform>();
                  brushSettingsRect.anchorMin = new Vector2(0, 0);
                  brushSettingsRect.anchorMax = new Vector2(1, 1);
                  brushSettingsRect.offsetMin = Vector2.zero;
                  brushSettingsRect.offsetMax = Vector2.zero;
                  Image brushSettingsImage = brushSettingsObject.AddComponent<Image>();
                  brushSettingsImage.color = new Color(0, 0, 0, 0.8f);
                  brushSettingsObject.SetActive(false);

                  // Создаем слайдер размера кисти
                  GameObject sliderObject = new GameObject("BrushSizeSlider");
                  sliderObject.transform.SetParent(brushSettingsObject.transform, false);
                  RectTransform sliderRect = sliderObject.AddComponent<RectTransform>();
                  sliderRect.anchorMin = new Vector2(0.2f, 0.5f);
                  sliderRect.anchorMax = new Vector2(0.8f, 0.5f);
                  sliderRect.sizeDelta = new Vector2(0, 20);
                  Slider slider = sliderObject.AddComponent<Slider>();
                  slider.minValue = 0.01f;
                  slider.maxValue = 0.2f;
                  slider.value = 0.05f;

                  // Создаем дочерние объекты для слайдера
                  GameObject sliderBackground = new GameObject("Background");
                  sliderBackground.transform.SetParent(sliderObject.transform, false);
                  RectTransform sliderBackgroundRect = sliderBackground.AddComponent<RectTransform>();
                  sliderBackgroundRect.anchorMin = Vector2.zero;
                  sliderBackgroundRect.anchorMax = Vector2.one;
                  sliderBackgroundRect.offsetMin = Vector2.zero;
                  sliderBackgroundRect.offsetMax = Vector2.zero;
                  Image sliderBackgroundImage = sliderBackground.AddComponent<Image>();
                  sliderBackgroundImage.color = new Color(0.2f, 0.2f, 0.2f);

                  GameObject sliderFillArea = new GameObject("Fill Area");
                  sliderFillArea.transform.SetParent(sliderObject.transform, false);
                  RectTransform sliderFillAreaRect = sliderFillArea.AddComponent<RectTransform>();
                  sliderFillAreaRect.anchorMin = new Vector2(0, 0.5f);
                  sliderFillAreaRect.anchorMax = new Vector2(1, 0.5f);
                  sliderFillAreaRect.offsetMin = new Vector2(5, -5);
                  sliderFillAreaRect.offsetMax = new Vector2(-5, 5);

                  GameObject sliderFill = new GameObject("Fill");
                  sliderFill.transform.SetParent(sliderFillArea.transform, false);
                  RectTransform sliderFillRect = sliderFill.AddComponent<RectTransform>();
                  sliderFillRect.anchorMin = Vector2.zero;
                  sliderFillRect.anchorMax = new Vector2(0.5f, 1);
                  sliderFillRect.pivot = new Vector2(0, 0.5f);
                  sliderFillRect.offsetMin = Vector2.zero;
                  sliderFillRect.offsetMax = Vector2.zero;
                  Image sliderFillImage = sliderFill.AddComponent<Image>();
                  sliderFillImage.color = new Color(0, 0.7f, 1);

                  GameObject sliderHandle = new GameObject("Handle Slide Area");
                  sliderHandle.transform.SetParent(sliderObject.transform, false);
                  RectTransform sliderHandleRect = sliderHandle.AddComponent<RectTransform>();
                  sliderHandleRect.anchorMin = Vector2.zero;
                  sliderHandleRect.anchorMax = Vector2.one;
                  sliderHandleRect.offsetMin = new Vector2(10, 0);
                  sliderHandleRect.offsetMax = new Vector2(-10, 0);

                  GameObject sliderHandleObject = new GameObject("Handle");
                  sliderHandleObject.transform.SetParent(sliderHandle.transform, false);
                  RectTransform sliderHandleObjectRect = sliderHandleObject.AddComponent<RectTransform>();
                  sliderHandleObjectRect.anchorMin = new Vector2(0.5f, 0.5f);
                  sliderHandleObjectRect.anchorMax = new Vector2(0.5f, 0.5f);
                  sliderHandleObjectRect.sizeDelta = new Vector2(20, 20);
                  sliderHandleObjectRect.anchoredPosition = Vector2.zero;
                  Image sliderHandleImage = sliderHandleObject.AddComponent<Image>();
                  sliderHandleImage.color = Color.white;

                  // Настраиваем слайдер
                  slider.fillRect = sliderFillRect;
                  slider.handleRect = sliderHandleObjectRect;
                  slider.targetGraphic = sliderHandleImage;

                  // Создаем текст размера кисти
                  GameObject brushSizeTextObject = new GameObject("BrushSizeText");
                  brushSizeTextObject.transform.SetParent(brushSettingsObject.transform, false);
                  RectTransform brushSizeTextRect = brushSizeTextObject.AddComponent<RectTransform>();
                  brushSizeTextRect.anchorMin = new Vector2(0.5f, 0.6f);
                  brushSizeTextRect.anchorMax = new Vector2(0.5f, 0.6f);
                  brushSizeTextRect.sizeDelta = new Vector2(200, 30);
                  Text brushSizeText = brushSizeTextObject.AddComponent<Text>();
                  brushSizeText.text = "Размер: 0.05";
                  brushSizeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                  brushSizeText.fontSize = 20;
                  brushSizeText.alignment = TextAnchor.MiddleCenter;
                  brushSizeText.color = Color.white;

                  // Создаем кнопки управления
                  // Кнопка палитры цветов
                  GameObject colorButton = CreateUIButton(mainPanelObject.transform, "ColorPaletteButton", "Цвета", new Vector2(0.1f, 0.1f), new Vector2(150, 50));

                  // Кнопка настройки кисти
                  GameObject brushButton = CreateUIButton(mainPanelObject.transform, "BrushSettingsButton", "Кисть", new Vector2(0.3f, 0.1f), new Vector2(150, 50));

                  // Кнопка сброса стен
                  GameObject resetButton = CreateUIButton(mainPanelObject.transform, "ResetButton", "Сброс", new Vector2(0.5f, 0.1f), new Vector2(150, 50));

                  // Кнопка скриншота
                  GameObject screenshotButton = CreateUIButton(mainPanelObject.transform, "ScreenshotButton", "Снимок", new Vector2(0.7f, 0.1f), new Vector2(150, 50));

                  // Кнопка Назад для вложенных панелей
                  GameObject backButtonColor = CreateUIButton(colorPaletteObject.transform, "BackButton", "Назад", new Vector2(0.1f, 0.9f), new Vector2(100, 40));
                  GameObject backButtonBrush = CreateUIButton(brushSettingsObject.transform, "BackButton", "Назад", new Vector2(0.1f, 0.9f), new Vector2(100, 40));

                  // Добавляем менеджер UI
                  WallPaintingUIManager uiManager = canvasObject.AddComponent<WallPaintingUIManager>();

                  // Устанавливаем ссылки на UI элементы
                  SerializedObject serializedObject = new SerializedObject(uiManager);
                  serializedObject.FindProperty("mainPanel").objectReferenceValue = mainPanelObject;
                  serializedObject.FindProperty("colorPalettePanel").objectReferenceValue = colorPaletteObject;
                  serializedObject.FindProperty("brushSettingsPanel").objectReferenceValue = brushSettingsObject;
                  serializedObject.FindProperty("colorPaletteButton").objectReferenceValue = colorButton.GetComponent<Button>();
                  serializedObject.FindProperty("brushSettingsButton").objectReferenceValue = brushButton.GetComponent<Button>();
                  serializedObject.FindProperty("resetButton").objectReferenceValue = resetButton.GetComponent<Button>();
                  serializedObject.FindProperty("screenshotButton").objectReferenceValue = screenshotButton.GetComponent<Button>();
                  serializedObject.FindProperty("backButton").objectReferenceValue = backButtonColor.GetComponent<Button>();
                  serializedObject.FindProperty("brushSizeSlider").objectReferenceValue = slider;
                  serializedObject.FindProperty("brushSizeText").objectReferenceValue = brushSizeText;
                  serializedObject.FindProperty("colorButtonsContainer").objectReferenceValue = colorButtonsContainer.transform;
                  serializedObject.ApplyModifiedProperties();

                  // Создаем менеджер скриншотов
                  GameObject screenshotManager = new GameObject("ScreenshotManager");
                  screenshotManager.AddComponent<ScreenshotManager>();
            }

            /// <summary>
            /// Создает материалы для покраски стен
            /// </summary>
            private static void CreatePaintMaterials()
            {
                  // Проверяем, существует ли папка для материалов
                  string materialsFolder = "Assets/Materials";
                  string paintMaterialsFolder = "Assets/Materials/Paints";

                  if (!AssetDatabase.IsValidFolder(materialsFolder))
                  {
                        AssetDatabase.CreateFolder("Assets", "Materials");
                  }

                  if (!AssetDatabase.IsValidFolder(paintMaterialsFolder))
                  {
                        AssetDatabase.CreateFolder(materialsFolder, "Paints");
                  }

                  // Находим или создаем шейдер в зависимости от текущего рендерера
                  Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                  if (shader == null)
                  {
                        shader = Shader.Find("Standard");
                  }

                  // Определяем цвета для материалов
                  Color[] colors = new Color[]
                  {
                new Color(0.9f, 0.1f, 0.1f), // Красный
                new Color(0.1f, 0.6f, 0.9f), // Синий
                new Color(0.1f, 0.8f, 0.2f), // Зеленый
                new Color(0.9f, 0.8f, 0.1f), // Желтый
                new Color(0.8f, 0.2f, 0.8f), // Фиолетовый
                new Color(1.0f, 0.5f, 0.0f), // Оранжевый
                new Color(0.5f, 0.3f, 0.1f), // Коричневый
                new Color(0.9f, 0.9f, 0.9f), // Белый
                new Color(0.3f, 0.3f, 0.3f)  // Серый
                  };

                  string[] materialNames = new string[]
                  {
                "RedPaint",
                "BluePaint",
                "GreenPaint",
                "YellowPaint",
                "PurplePaint",
                "OrangePaint",
                "BrownPaint",
                "WhitePaint",
                "GreyPaint"
                  };

                  // Создаем материал по умолчанию для стен
                  string defaultMaterialPath = $"{paintMaterialsFolder}/DefaultWallMaterial.mat";
                  Material defaultMaterial = null;

                  if (AssetDatabase.LoadAssetAtPath<Material>(defaultMaterialPath) == null)
                  {
                        defaultMaterial = new Material(shader);
                        defaultMaterial.color = new Color(0.9f, 0.9f, 0.9f);
                        defaultMaterial.SetFloat("_Glossiness", 0.1f);
                        defaultMaterial.SetFloat("_Metallic", 0.0f);
                        AssetDatabase.CreateAsset(defaultMaterial, defaultMaterialPath);
                  }
                  else
                  {
                        defaultMaterial = AssetDatabase.LoadAssetAtPath<Material>(defaultMaterialPath);
                  }

                  // Создаем материал для выделения стен
                  string highlightMaterialPath = $"{paintMaterialsFolder}/WallHighlightMaterial.mat";
                  Material highlightMaterial = null;

                  if (AssetDatabase.LoadAssetAtPath<Material>(highlightMaterialPath) == null)
                  {
                        highlightMaterial = new Material(shader);
                        highlightMaterial.color = new Color(1.0f, 1.0f, 0.5f, 0.3f);
                        highlightMaterial.SetFloat("_Glossiness", 0.1f);
                        highlightMaterial.SetFloat("_Metallic", 0.0f);
                        AssetDatabase.CreateAsset(highlightMaterial, highlightMaterialPath);
                  }
                  else
                  {
                        highlightMaterial = AssetDatabase.LoadAssetAtPath<Material>(highlightMaterialPath);
                  }

                  // Создаем материалы для различных цветов краски
                  for (int i = 0; i < colors.Length; i++)
                  {
                        if (i < materialNames.Length)
                        {
                              string materialPath = $"{paintMaterialsFolder}/{materialNames[i]}.mat";

                              if (AssetDatabase.LoadAssetAtPath<Material>(materialPath) == null)
                              {
                                    Material paintMaterial = new Material(shader);
                                    paintMaterial.color = colors[i];
                                    paintMaterial.SetFloat("_Glossiness", 0.2f);
                                    paintMaterial.SetFloat("_Metallic", 0.0f);
                                    AssetDatabase.CreateAsset(paintMaterial, materialPath);
                              }
                        }
                  }

                  AssetDatabase.SaveAssets();
                  AssetDatabase.Refresh();

                  // Находим WallPainter и устанавливаем материалы
                  WallPainter wallPainter = Object.FindFirstObjectByType<WallPainter>();
                  if (wallPainter != null)
                  {
                        SerializedObject serializedObject = new SerializedObject(wallPainter);
                        serializedObject.FindProperty("defaultWallMaterial").objectReferenceValue = defaultMaterial;
                        serializedObject.FindProperty("wallHighlightMaterial").objectReferenceValue = highlightMaterial;

                        // Находим список материалов и заполняем его
                        SerializedProperty paintMaterialsProperty = serializedObject.FindProperty("paintMaterials");
                        paintMaterialsProperty.ClearArray();

                        for (int i = 0; i < materialNames.Length; i++)
                        {
                              string materialPath = $"{paintMaterialsFolder}/{materialNames[i]}.mat";
                              Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

                              if (material != null)
                              {
                                    paintMaterialsProperty.arraySize++;
                                    paintMaterialsProperty.GetArrayElementAtIndex(paintMaterialsProperty.arraySize - 1).objectReferenceValue = material;
                              }
                        }

                        serializedObject.ApplyModifiedProperties();
                  }

                  // Находим WallPaintingUIManager и устанавливаем материалы
                  WallPaintingUIManager uiManager = Object.FindFirstObjectByType<WallPaintingUIManager>();
                  if (uiManager != null)
                  {
                        SerializedObject serializedObject = new SerializedObject(uiManager);

                        // Находим список материалов и заполняем его
                        SerializedProperty paintMaterialsProperty = serializedObject.FindProperty("paintMaterials");
                        paintMaterialsProperty.ClearArray();

                        for (int i = 0; i < materialNames.Length; i++)
                        {
                              string materialPath = $"{paintMaterialsFolder}/{materialNames[i]}.mat";
                              Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

                              if (material != null)
                              {
                                    paintMaterialsProperty.arraySize++;
                                    paintMaterialsProperty.GetArrayElementAtIndex(paintMaterialsProperty.arraySize - 1).objectReferenceValue = material;
                              }
                        }

                        serializedObject.ApplyModifiedProperties();
                  }
            }

            /// <summary>
            /// Создает UI кнопку с заданными параметрами
            /// </summary>
            private static GameObject CreateUIButton(Transform parent, string name, string text, Vector2 positionAnchor, Vector2 size)
            {
                  GameObject buttonObject = new GameObject(name);
                  buttonObject.transform.SetParent(parent, false);
                  RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
                  buttonRect.anchorMin = positionAnchor;
                  buttonRect.anchorMax = positionAnchor;
                  buttonRect.sizeDelta = size;
                  Button button = buttonObject.AddComponent<Button>();

                  // Добавляем изображение кнопки
                  Image buttonImage = buttonObject.AddComponent<Image>();
                  buttonImage.color = new Color(0.2f, 0.2f, 0.2f);
                  button.targetGraphic = buttonImage;

                  // Добавляем текст кнопки
                  GameObject textObject = new GameObject("Text");
                  textObject.transform.SetParent(buttonObject.transform, false);
                  RectTransform textRect = textObject.AddComponent<RectTransform>();
                  textRect.anchorMin = Vector2.zero;
                  textRect.anchorMax = Vector2.one;
                  textRect.offsetMin = Vector2.zero;
                  textRect.offsetMax = Vector2.zero;
                  Text buttonText = textObject.AddComponent<Text>();
                  buttonText.text = text;
                  buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                  buttonText.fontSize = 20;
                  buttonText.alignment = TextAnchor.MiddleCenter;
                  buttonText.color = Color.white;

                  return buttonObject;
            }
      }
}