using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using System.Linq;
using Remalux.AR;
using UnityEngine.UI;
using System.Collections;
using System.Reflection;
using UnityEngine.SceneManagement;

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

        private bool materialsLoaded = false;
        private bool wallPainterInitialized = false;

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
            if (instance != null && instance != this)
            {
                Debug.LogWarning("Found duplicate WallPaintingManager. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            instance = this;

            // Step 1: Load materials
            LoadMaterials();
        }

        private void Start()
        {
            // Step 2: Initialize WallPainter
            InitializeWallPainter();

            // Step 3: Initialize other components with delay
            StartCoroutine(DelayedInitialization());
        }

        private void LoadMaterials()
        {
            if (paintMaterials == null || paintMaterials.Length == 0)
            {
                LoadMaterialsFromColorManager();
            }

            if (paintMaterials != null && paintMaterials.Length > 0)
            {
                Debug.Log($"WallPaintingManager: loaded {paintMaterials.Length} paint materials");

                if (defaultWallMaterial == null)
                {
                    defaultWallMaterial = paintMaterials[0];
                    Debug.Log($"WallPaintingManager: set default material: {defaultWallMaterial.name}");
                }

                materialsLoaded = true;
            }
            else
            {
                Debug.LogError("WallPaintingManager: Failed to load materials!");
            }
        }

        private void InitializeWallPainter()
        {
            if (!materialsLoaded)
            {
                Debug.LogError("WallPaintingManager: Cannot initialize WallPainter - materials not loaded!");
                return;
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

            // Set up WallPainter materials
            var availablePaintsField = wallPainter.GetType().GetField("availablePaints",
                BindingFlags.Instance | BindingFlags.Public);
            var defaultMaterialField = wallPainter.GetType().GetField("defaultMaterial",
                BindingFlags.Instance | BindingFlags.Public);

            if (availablePaintsField != null && paintMaterials != null)
            {
                availablePaintsField.SetValue(wallPainter, paintMaterials);
                Debug.Log($"WallPaintingManager: Set {paintMaterials.Length} materials for WallPainter");
            }

            if (defaultMaterialField != null && defaultWallMaterial != null)
            {
                defaultMaterialField.SetValue(wallPainter, defaultWallMaterial);
                Debug.Log($"WallPaintingManager: Set default material for WallPainter: {defaultWallMaterial.name}");
            }

            // Initialize WallPainter
            var initializeMethod = wallPainter.GetType().GetMethod("Initialize",
                BindingFlags.Instance | BindingFlags.Public);
            if (initializeMethod != null)
            {
                initializeMethod.Invoke(wallPainter, null);
                wallPainterInitialized = true;
                Debug.Log("WallPaintingManager: WallPainter initialized");
            }
        }

        private IEnumerator DelayedInitialization()
        {
            if (!wallPainterInitialized)
            {
                Debug.LogError("WallPaintingManager: Cannot proceed with initialization - WallPainter not initialized!");
                yield break;
            }

            // Wait for WallPainter to be fully initialized
            float timeout = 5f;
            float elapsed = 0f;

            while (!wallPainter.IsInitialized && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!wallPainter.IsInitialized)
            {
                Debug.LogError("WallPaintingManager: Timeout waiting for WallPainter initialization!");
                yield break;
            }

            // Initialize color selector
            if (colorSelector != null)
            {
                var paintMaterialsField = colorSelector.GetType().GetField("paintMaterials",
                    BindingFlags.Instance | BindingFlags.Public);
                if (paintMaterialsField != null)
                {
                    paintMaterialsField.SetValue(colorSelector, paintMaterials);
                    Debug.Log($"WallPaintingManager: Set {paintMaterials.Length} materials for color selector");
                }

                var wallPainterField = colorSelector.GetType().GetField("wallPainter",
                    BindingFlags.Instance | BindingFlags.Public);
                if (wallPainterField != null)
                {
                    wallPainterField.SetValue(colorSelector, wallPainter);
                    Debug.Log("WallPaintingManager: Set WallPainter reference for color selector");
                }

                var initializeMethod = colorSelector.GetType().GetMethod("Initialize",
                    BindingFlags.Instance | BindingFlags.Public);
                if (initializeMethod != null)
                {
                    initializeMethod.Invoke(colorSelector, null);
                    Debug.Log("WallPaintingManager: Color selector initialized");
                }
            }

            // Show UI
            if (paintingUI != null)
            {
                paintingUI.SetActive(true);
                Debug.Log("WallPaintingManager: Painting UI activated");
            }

            Debug.Log("WallPaintingManager: Initialization complete");
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