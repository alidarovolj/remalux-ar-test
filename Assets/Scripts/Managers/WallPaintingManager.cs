using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using System.Linq;
using Remalux.AR;
using UnityEngine.UI;
using System.Collections;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Remalux.AR
{
    /// <summary>
    /// Менеджер системы покраски стен
    /// </summary>
    public class WallPaintingManager : MonoBehaviour
    {
        private static WallPaintingManager instance;

        [Header("References")]
        public WallPainter wallPainter;
        public MonoBehaviour colorSelector;
        public GameObject paintingUI;

        [Header("Materials")]
        public Material[] paintMaterials;
        public Material defaultWallMaterial;

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

        private void Awake()
        {
            // Singleton pattern to prevent multiple instances
            if (instance != null && instance != this)
            {
                Debug.LogWarning("Found duplicate WallPaintingManager. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            instance = this;

            // Load materials if not assigned
            if (paintMaterials == null || paintMaterials.Length == 0)
            {
                LoadMaterialsFromColorManager();
            }
            else
            {
                Debug.Log($"WallPaintingManager: loaded {paintMaterials.Length} paint materials");
            }

            // Check for default material
            if (defaultWallMaterial == null && paintMaterials != null && paintMaterials.Length > 0)
            {
                defaultWallMaterial = paintMaterials[0];
                Debug.Log($"WallPaintingManager: set default material: {defaultWallMaterial.name}");
            }

            // Find or create WallPainter
            if (wallPainter == null)
            {
                wallPainter = FindObjectOfType<WallPainter>();
                if (wallPainter == null)
                {
                    GameObject wallPainterObj = new GameObject("WallPainter");
                    wallPainter = wallPainterObj.AddComponent<WallPainter>();
                    Debug.Log("WallPaintingManager: created new WallPainter");
                }
                else
                {
                    Debug.Log("WallPaintingManager: found existing WallPainter");
                }
            }

            // Find or create SimplePaintColorSelector
            if (colorSelector == null)
            {
                var allMonoBehaviours = FindObjectsOfType<MonoBehaviour>();
                var colorSelectors = allMonoBehaviours.Where(mb => mb.GetType().Name == "SimplePaintColorSelector").ToList();

                if (colorSelectors.Count > 0)
                {
                    colorSelector = colorSelectors[0];
                    Debug.Log("WallPaintingManager: found SimplePaintColorSelector in scene");

                    // Clean up duplicates
                    if (colorSelectors.Count > 1)
                    {
                        Debug.LogWarning($"Found {colorSelectors.Count} SimplePaintColorSelectors. Keeping only one instance.");
                        for (int i = 1; i < colorSelectors.Count; i++)
                        {
                            Destroy(colorSelectors[i].gameObject);
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("Could not find SimplePaintColorSelector in scene");
                }
            }

            // Setup components with a delay to ensure proper initialization
            StartCoroutine(DelayedInitialization());
        }

        private void SetupComponents()
        {
            if (wallPainter == null || colorSelector == null)
            {
                Debug.LogError("Cannot setup components: WallPainter or ColorSelector is missing");
                return;
            }

            // Setup WallPainter
            var defaultMaterialField = wallPainter.GetType().GetField("defaultMaterial",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (defaultMaterialField != null)
            {
                defaultMaterialField.SetValue(wallPainter, defaultWallMaterial);
                Debug.Log($"WallPaintingManager: set default material for WallPainter: {(defaultWallMaterial != null ? defaultWallMaterial.name : "null")}");
            }

            // Setup available paints
            var availablePaintsField = wallPainter.GetType().GetField("availablePaints",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (availablePaintsField != null && paintMaterials != null && paintMaterials.Length > 0)
            {
                availablePaintsField.SetValue(wallPainter, paintMaterials);
                Debug.Log($"WallPaintingManager: set {paintMaterials.Length} materials for WallPainter");
            }

            // Setup SimplePaintColorSelector
            var wallPainterField = colorSelector.GetType().GetField("wallPainter",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (wallPainterField != null)
            {
                wallPainterField.SetValue(colorSelector, wallPainter);
                Debug.Log("WallPaintingManager: set WallPainter reference for SimplePaintColorSelector");
            }

            var paintMaterialsField = colorSelector.GetType().GetField("paintMaterials",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (paintMaterialsField != null && paintMaterials != null && paintMaterials.Length > 0)
            {
                paintMaterialsField.SetValue(colorSelector, paintMaterials);
                Debug.Log($"WallPaintingManager: set {paintMaterials.Length} materials for SimplePaintColorSelector");
            }

            // Initialize the color selector
            var initializeMethod = colorSelector.GetType().GetMethod("Initialize",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (initializeMethod != null)
            {
                initializeMethod.Invoke(colorSelector, null);
                Debug.Log("WallPaintingManager: initialized SimplePaintColorSelector");
            }
        }

        private void Start()
        {
            // Вызываем инициализацию после того, как все компоненты прошли свои Awake и Start
            StartCoroutine(DelayedInitialization());
        }

        private IEnumerator DelayedInitialization()
        {
            // Ждем один кадр, чтобы все компоненты успели инициализироваться
            yield return null;

            // Инициализируем компоненты
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // Initialize WallPainter with materials
            if (wallPainter != null)
            {
                // Set available paints and default material via reflection or direct reference
                SetWallPainterMaterials();
            }

            // Initialize SimplePaintColorSelector with materials
            if (colorSelector != null)
            {
                // Set paint materials directly since they are public
                var paintMaterialsField = colorSelector.GetType().GetField("paintMaterials",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (paintMaterialsField != null)
                    paintMaterialsField.SetValue(colorSelector, paintMaterials);

                // Установка ссылки на WallPainter
                var wallPainterField = colorSelector.GetType().GetField("wallPainter",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (wallPainterField != null)
                    wallPainterField.SetValue(colorSelector, wallPainter);

                // Инициализация кнопок
                var initializeMethod = colorSelector.GetType().GetMethod("Initialize",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (initializeMethod != null)
                    initializeMethod.Invoke(colorSelector, null);
            }

            // Show UI
            if (paintingUI != null)
                paintingUI.SetActive(true);
        }

        private void SetWallPainterMaterials()
        {
            // Use reflection to set fields
            var availablePaintsField = wallPainter.GetType().GetField("availablePaints",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

            var defaultMaterialField = wallPainter.GetType().GetField("defaultMaterial",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

            if (availablePaintsField != null && paintMaterials != null)
                availablePaintsField.SetValue(wallPainter, paintMaterials);

            if (defaultMaterialField != null && defaultWallMaterial != null)
                defaultMaterialField.SetValue(wallPainter, defaultWallMaterial);
        }

        // Public methods for external control
        public void EnableWallPainting(bool enable)
        {
            if (wallPainter != null)
                wallPainter.enabled = enable;

            if (paintingUI != null)
                paintingUI.SetActive(enable);
        }

        public void ResetAllWalls()
        {
            if (wallPainter != null)
            {
                // Вызываем метод ResetWallMaterials через рефлексию
                var resetMethod = wallPainter.GetType().GetMethod("ResetWallMaterials",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

                if (resetMethod != null)
                    resetMethod.Invoke(wallPainter, null);
            }
        }

        // Добавляем недостающие методы
        public void StartPainting()
        {
            EnableWallPainting(true);
        }

        public void ResetPainting()
        {
            ResetAllWalls();
        }

        public void RestartDetection()
        {
            // Перезапуск обнаружения стен
            ResetAllWalls();
            // Дополнительная логика перезапуска обнаружения, если необходимо
        }

        public void OnWallsDetected(List<GameObject> walls)
        {
            // Обработка обнаруженных стен
            if (wallPainter != null)
            {
                foreach (var wall in walls)
                {
                    // Вызываем метод AddWall через рефлексию
                    var addWallMethod = wallPainter.GetType().GetMethod("AddWall",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public,
                        null, new System.Type[] { typeof(GameObject) }, null);

                    if (addWallMethod != null)
                        addWallMethod.Invoke(wallPainter, new object[] { wall });
                }
            }
        }

        public List<GameObject> GetPaintedWalls()
        {
            if (wallPainter != null)
            {
                // Получаем стены через рефлексию, так как поле может быть приватным
                var wallsField = wallPainter.GetType().GetField("walls",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                if (wallsField != null)
                {
                    return wallsField.GetValue(wallPainter) as List<GameObject>;
                }
            }

            return new List<GameObject>();
        }

        public void ClearWalls()
        {
            ResetAllWalls();

            // Очищаем список стен через рефлексию
            if (wallPainter != null)
            {
                var wallsField = wallPainter.GetType().GetField("walls",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                if (wallsField != null)
                {
                    var walls = wallsField.GetValue(wallPainter) as List<GameObject>;
                    if (walls != null)
                    {
                        walls.Clear();
                    }
                }
            }
        }

        public void CreateWall(GameObject wallObject)
        {
            if (wallPainter != null)
            {
                // Вызываем метод AddWall через рефлексию
                var addWallMethod = wallPainter.GetType().GetMethod("AddWall",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public,
                    null, new System.Type[] { typeof(GameObject) }, null);

                if (addWallMethod != null)
                    addWallMethod.Invoke(wallPainter, new object[] { wallObject });
            }
        }

        // Перегрузка метода CreateWall для создания стены с заданными параметрами
        public void CreateWall(Vector3 position, Quaternion rotation, Vector3 scale, Color color)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = position;
            wall.transform.rotation = rotation;
            wall.transform.localScale = scale;
            wall.tag = "Wall";
            wall.layer = LayerMask.NameToLayer("Wall");

            // Создаем материал
            Material material = new Material(GetAppropriateShader());
            material.color = color;

            // Применяем материал
            Renderer renderer = wall.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = material;
            }

            // Добавляем стену в WallPainter
            CreateWall(wall);
        }

        private void LoadMaterialsFromColorManager()
        {
            // Try to get materials from ColorManager
            ColorManager colorManager = FindObjectOfType<ColorManager>();
            if (colorManager != null)
            {
                // Пытаемся получить материалы через рефлексию, так как метод GetAllMaterials не существует
                try
                {
                    // Пытаемся получить материалы из свойства или поля
                    var materialsField = colorManager.GetType().GetField("materials",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

                    if (materialsField != null)
                    {
                        var materials = materialsField.GetValue(colorManager) as Material[];
                        if (materials != null && materials.Length > 0)
                        {
                            paintMaterials = materials;
                            Debug.Log($"WallPaintingManager: получено {paintMaterials.Length} материалов из ColorManager через рефлексию");
                        }
                    }
                    else
                    {
                        // Пытаемся найти другие поля с материалами
                        var allFields = colorManager.GetType().GetFields(
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

                        foreach (var field in allFields)
                        {
                            if (field.FieldType == typeof(Material[]))
                            {
                                var materials = field.GetValue(colorManager) as Material[];
                                if (materials != null && materials.Length > 0)
                                {
                                    paintMaterials = materials;
                                    Debug.Log($"WallPaintingManager: получено {paintMaterials.Length} материалов из ColorManager через поле {field.Name}");
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Ошибка при получении материалов из ColorManager: {e.Message}");
                }
            }

            // Если материалы все еще не получены, создаем стандартные
            if (paintMaterials == null || paintMaterials.Length == 0)
            {
                // Создаем хотя бы один материал, чтобы система не падала
                paintMaterials = new Material[1];
                paintMaterials[0] = new Material(GetAppropriateShader());
                paintMaterials[0].name = "DefaultPaint";
                paintMaterials[0].color = Color.white;
                Debug.Log("WallPaintingManager: создан стандартный материал для покраски");
            }
        }
    }
}