using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Remalux.AR.Vision
{
    /// <summary>
    /// Основной контроллер для системы покраски реальных стен с использованием компьютерного зрения
    /// </summary>
    public class RealWallPaintingController : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private WallDetector wallDetector;
        [SerializeField] private WallMeshBuilder wallMeshBuilder;
        [SerializeField] private Camera mainCamera;

        [Header("UI References")]
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private GameObject colorButtonPrefab;
        [SerializeField] private Transform colorButtonsContainer;
        [SerializeField] private Button resetButton;
        [SerializeField] private GameObject colorPreviewPrefab;

        [Header("Materials")]
        [SerializeField] private Material defaultWallMaterial;
        [SerializeField] private Material[] paintMaterials;

        [Header("Settings")]
        [SerializeField] private float raycastDistance = 10f;
        [SerializeField] private bool showColorPreview = true;

        // Текущий выбранный материал
        private Material currentPaintMaterial;
        
        // Объект предпросмотра цвета
        private GameObject colorPreview;
        private SimpleColorPreview previewComponent;

        // Состояние системы
        private bool isInitialized = false;
        private bool isPaintingMode = false;

        private void Start()
        {
            InitializeComponents();
            SetupUI();
            isInitialized = true;
        }

        private void InitializeComponents()
        {
            // Инициализация камеры
            if (mainCamera == null)
                mainCamera = Camera.main;

            // Инициализация WallDetector
            if (wallDetector == null)
                wallDetector = GetComponent<WallDetector>();

            if (wallDetector == null)
            {
                wallDetector = gameObject.AddComponent<WallDetector>();
                Debug.Log("RealWallPaintingController: Added WallDetector component");
            }

            // Инициализация WallMeshBuilder
            if (wallMeshBuilder == null)
                wallMeshBuilder = GetComponent<WallMeshBuilder>();

            if (wallMeshBuilder == null)
            {
                wallMeshBuilder = gameObject.AddComponent<WallMeshBuilder>();
                Debug.Log("RealWallPaintingController: Added WallMeshBuilder component");
            }

            // Настройка компонентов
            wallMeshBuilder.defaultWallMaterial = defaultWallMaterial;
            wallMeshBuilder.mainCamera = mainCamera;
            wallMeshBuilder.wallDetector = wallDetector;

            // Установка текущего материала
            if (paintMaterials != null && paintMaterials.Length > 0)
                currentPaintMaterial = paintMaterials[0];
            else
                Debug.LogError("RealWallPaintingController: No paint materials assigned!");
        }

        private void SetupUI()
        {
            // Проверка Canvas
            if (mainCanvas == null)
            {
                mainCanvas = Object.FindAnyObjectByType<Canvas>();
                if (mainCanvas == null)
                {
                    GameObject canvasObj = new GameObject("Canvas");
                    mainCanvas = canvasObj.AddComponent<Canvas>();
                    mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvasObj.AddComponent<CanvasScaler>();
                    canvasObj.AddComponent<GraphicRaycaster>();
                    Debug.Log("RealWallPaintingController: Created new Canvas");
                }
            }

            // Создание контейнера для кнопок цветов, если его нет
            if (colorButtonsContainer == null)
            {
                GameObject containerObj = new GameObject("ColorButtonsContainer");
                containerObj.transform.SetParent(mainCanvas.transform, false);
                
                RectTransform rectTransform = containerObj.AddComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0, 0);
                rectTransform.anchorMax = new Vector2(1, 0.2f);
                rectTransform.offsetMin = new Vector2(10, 10);
                rectTransform.offsetMax = new Vector2(-10, -10);
                
                HorizontalLayoutGroup layoutGroup = containerObj.AddComponent<HorizontalLayoutGroup>();
                layoutGroup.spacing = 10;
                layoutGroup.childAlignment = TextAnchor.MiddleCenter;
                layoutGroup.childForceExpandWidth = false;
                layoutGroup.childForceExpandHeight = false;
                
                colorButtonsContainer = rectTransform;
                Debug.Log("RealWallPaintingController: Created color buttons container");
            }

            // Создание кнопки сброса, если ее нет
            if (resetButton == null)
            {
                GameObject resetButtonObj = new GameObject("ResetButton");
                resetButtonObj.transform.SetParent(mainCanvas.transform, false);
                
                RectTransform rectTransform = resetButtonObj.AddComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.5f, 0.8f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.9f);
                rectTransform.sizeDelta = new Vector2(160, 40);
                
                Image image = resetButtonObj.AddComponent<Image>();
                image.color = Color.white;
                
                resetButton = resetButtonObj.AddComponent<Button>();
                ColorBlock colors = resetButton.colors;
                colors.normalColor = new Color(0.9f, 0.9f, 0.9f);
                colors.highlightedColor = new Color(1f, 1f, 1f);
                colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
                resetButton.colors = colors;
                
                // Добавляем текст на кнопку
                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(resetButtonObj.transform, false);
                
                RectTransform textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
                
                Text text = textObj.AddComponent<Text>();
                text.text = "Сбросить";
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.black;
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                
                resetButton.onClick.AddListener(ResetAllWalls);
                Debug.Log("RealWallPaintingController: Created reset button");
            }

            // Создание кнопок цветов
            CreateColorButtons();

            // Создание объекта предпросмотра цвета
            if (showColorPreview && colorPreviewPrefab != null)
            {
                colorPreview = Instantiate(colorPreviewPrefab);
                previewComponent = colorPreview.GetComponent<SimpleColorPreview>();
                if (previewComponent == null)
                {
                    previewComponent = colorPreview.AddComponent<SimpleColorPreview>();
                }
                colorPreview.SetActive(false);
                Debug.Log("RealWallPaintingController: Created color preview");
            }
        }

        private void CreateColorButtons()
        {
            // Проверка наличия префаба кнопки
            if (colorButtonPrefab == null)
            {
                Debug.LogError("RealWallPaintingController: Color button prefab is not assigned!");
                return;
            }

            // Проверка наличия материалов
            if (paintMaterials == null || paintMaterials.Length == 0)
            {
                Debug.LogError("RealWallPaintingController: No paint materials assigned!");
                return;
            }

            // Удаление существующих кнопок
            foreach (Transform child in colorButtonsContainer)
            {
                Destroy(child.gameObject);
            }

            // Создание новых кнопок для каждого материала
            for (int i = 0; i < paintMaterials.Length; i++)
            {
                Material material = paintMaterials[i];
                GameObject buttonObj = Instantiate(colorButtonPrefab, colorButtonsContainer);
                
                // Настройка кнопки
                SimpleColorButton colorButton = buttonObj.GetComponent<SimpleColorButton>();
                if (colorButton == null)
                {
                    colorButton = buttonObj.AddComponent<SimpleColorButton>();
                }
                
                colorButton.SetMaterial(material);
                
                // Добавление обработчика нажатия
                Button button = buttonObj.GetComponent<Button>();
                if (button != null)
                {
                    int materialIndex = i; // Сохраняем индекс для использования в лямбда-выражении
                    button.onClick.AddListener(() => SelectPaintMaterial(materialIndex));
                }
            }

            Debug.Log($"RealWallPaintingController: Created {paintMaterials.Length} color buttons");
        }

        private void Update()
        {
            if (!isInitialized)
                return;

            // Обработка ввода для покраски стен
            if (isPaintingMode && UnityEngine.Input.GetMouseButtonDown(0))
            {
                PaintWallAtMousePosition();
            }

            // Обновление предпросмотра цвета
            UpdateColorPreview();
        }

        private void UpdateColorPreview()
        {
            if (!showColorPreview || colorPreview == null || currentPaintMaterial == null)
                return;

            Ray ray = mainCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, raycastDistance))
            {
                // Проверяем, является ли объект стеной
                WallMaterialInstanceTracker tracker = hit.collider.GetComponent<WallMaterialInstanceTracker>();
                if (tracker != null)
                {
                    colorPreview.SetActive(true);
                    colorPreview.transform.position = hit.point + hit.normal * 0.01f;
                    colorPreview.transform.forward = hit.normal;

                    if (previewComponent != null)
                    {
                        previewComponent.SetMaterial(currentPaintMaterial);
                    }
                }
                else
                {
                    colorPreview.SetActive(false);
                }
            }
            else
            {
                colorPreview.SetActive(false);
            }
        }

        private void PaintWallAtMousePosition()
        {
            if (currentPaintMaterial == null)
                return;

            Ray ray = mainCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, raycastDistance))
            {
                // Проверяем, является ли объект стеной
                WallMaterialInstanceTracker tracker = hit.collider.GetComponent<WallMaterialInstanceTracker>();
                if (tracker != null)
                {
                    // Применяем материал
                    tracker.ApplyMaterial(currentPaintMaterial);
                    Debug.Log($"RealWallPaintingController: Painted wall {hit.collider.gameObject.name} with {currentPaintMaterial.name}");
                }
            }
        }

        private void SelectPaintMaterial(int materialIndex)
        {
            if (materialIndex >= 0 && materialIndex < paintMaterials.Length)
            {
                currentPaintMaterial = paintMaterials[materialIndex];
                Debug.Log($"RealWallPaintingController: Selected material {currentPaintMaterial.name}");
            }
        }

        private void ResetAllWalls()
        {
            if (wallMeshBuilder != null)
            {
                wallMeshBuilder.ResetAllWalls();
                Debug.Log("RealWallPaintingController: Reset all walls to default material");
            }
        }

        /// <summary>
        /// Возвращает основной Canvas для UI
        /// </summary>
        public Canvas GetMainCanvas()
        {
            return mainCanvas;
        }

        /// <summary>
        /// Включает режим покраски
        /// </summary>
        public void EnablePaintingMode()
        {
            isPaintingMode = true;
            
            if (colorPreview != null && showColorPreview)
                colorPreview.SetActive(true);
        }
        
        /// <summary>
        /// Выключает режим покраски
        /// </summary>
        public void DisablePaintingMode()
        {
            isPaintingMode = false;
            
            if (colorPreview != null)
                colorPreview.SetActive(false);
        }
        
        /// <summary>
        /// Включает или выключает режим покраски
        /// </summary>
        public void EnablePaintingMode(bool enable)
        {
            if (enable)
                EnablePaintingMode();
            else
                DisablePaintingMode();
        }
        
        /// <summary>
        /// Переключает режим покраски
        /// </summary>
        public void TogglePaintingMode()
        {
            if (isPaintingMode)
                DisablePaintingMode();
            else
                EnablePaintingMode();
        }

        /// <summary>
        /// Устанавливает основную камеру
        /// </summary>
        public void SetMainCamera(Camera camera)
        {
            mainCamera = camera;
        }
        
        /// <summary>
        /// Устанавливает основной Canvas для UI
        /// </summary>
        public void SetMainCanvas(Canvas canvas)
        {
            mainCanvas = canvas;
        }
        
        /// <summary>
        /// Устанавливает материал по умолчанию для стен
        /// </summary>
        public void SetDefaultWallMaterial(Material material)
        {
            defaultWallMaterial = material;
        }
        
        /// <summary>
        /// Устанавливает массив материалов для покраски
        /// </summary>
        public void SetPaintMaterials(Material[] materials)
        {
            paintMaterials = materials;
        }
        
        /// <summary>
        /// Устанавливает префаб для кнопки цвета
        /// </summary>
        public void SetColorButtonPrefab(GameObject prefab)
        {
            colorButtonPrefab = prefab;
        }
        
        /// <summary>
        /// Устанавливает контейнер для кнопок цветов
        /// </summary>
        public void SetColorButtonsContainer(RectTransform container)
        {
            colorButtonsContainer = container;
        }
        
        /// <summary>
        /// Устанавливает кнопку сброса
        /// </summary>
        public void SetResetButton(Button button)
        {
            resetButton = button;
        }
        
        /// <summary>
        /// Устанавливает префаб для предпросмотра цвета
        /// </summary>
        public void SetColorPreviewPrefab(GameObject prefab)
        {
            colorPreviewPrefab = prefab;
        }

        /// <summary>
        /// Устанавливает построитель меша стен
        /// </summary>
        public void SetWallMeshBuilder(WallMeshBuilder meshBuilder)
        {
            wallMeshBuilder = meshBuilder;
        }

        /// <summary>
        /// Устанавливает детектор стен
        /// </summary>
        public void SetWallDetector(WallDetector detector)
        {
            wallDetector = detector;
        }
    }
} 