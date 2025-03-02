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
        [Header("References")]
        public WallPainter wallPainter;
        public MonoBehaviour colorSelector;
        public GameObject paintingUI;

        [Header("Materials")]
        public Material[] paintMaterials;
        public Material defaultWallMaterial;

        private void Awake()
        {
            // Проверяем наличие материалов
            if (paintMaterials == null || paintMaterials.Length == 0)
            {
                Debug.LogWarning("Не заданы материалы для покраски в WallPaintingManager. Система покраски может работать некорректно.");

                // Попытка получить материалы из ColorManager
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
                    paintMaterials[0] = new Material(Shader.Find("Standard"));
                    paintMaterials[0].name = "DefaultPaint";
                    paintMaterials[0].color = Color.white;
                    Debug.Log("WallPaintingManager: создан стандартный материал для покраски");
                }
            }
            else
            {
                Debug.Log($"WallPaintingManager: загружено {paintMaterials.Length} материалов для покраски");
            }

            // Проверяем наличие материала по умолчанию
            if (defaultWallMaterial == null && paintMaterials.Length > 0)
            {
                defaultWallMaterial = paintMaterials[0];
                Debug.Log($"WallPaintingManager: установлен материал по умолчанию: {defaultWallMaterial.name}");
            }
            else if (defaultWallMaterial == null)
            {
                Debug.LogWarning("Не задан материал по умолчанию в WallPaintingManager. Система покраски может работать некорректно.");
            }

            // Проверяем наличие WallPainter
            if (wallPainter == null)
            {
                wallPainter = FindObjectOfType<WallPainter>();
                if (wallPainter != null)
                {
                    Debug.Log("WallPaintingManager: найден WallPainter в сцене");
                }
                else
                {
                    Debug.LogWarning("Не найден WallPainter в сцене. Система покраски не будет работать.");

                    // Создаем WallPainter, если его нет
                    GameObject wallPainterObj = new GameObject("WallPainter");
                    wallPainter = wallPainterObj.AddComponent<WallPainter>();
                    Debug.Log("WallPaintingManager: создан новый WallPainter");
                }
            }

            // Проверяем наличие SimplePaintColorSelector
            if (colorSelector == null)
            {
                // Ищем объект по имени типа
                var allMonoBehaviours = FindObjectsOfType<MonoBehaviour>();
                foreach (var mb in allMonoBehaviours)
                {
                    if (mb.GetType().Name == "SimplePaintColorSelector")
                    {
                        colorSelector = mb;
                        Debug.Log("WallPaintingManager: найден SimplePaintColorSelector в сцене");
                        break;
                    }
                }

                if (colorSelector == null)
                {
                    Debug.LogError("Не удалось найти SimplePaintColorSelector в сцене");
                }
            }

            // Настраиваем связи между компонентами сразу в Awake
            SetupComponents();
        }

        private void SetupComponents()
        {
            // Настраиваем WallPainter
            if (wallPainter != null)
            {
                // Устанавливаем материал по умолчанию
                var defaultMaterialField = wallPainter.GetType().GetField("defaultMaterial",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (defaultMaterialField != null)
                {
                    defaultMaterialField.SetValue(wallPainter, defaultWallMaterial);
                    Debug.Log($"WallPaintingManager: установлен материал по умолчанию для WallPainter: {(defaultWallMaterial != null ? defaultWallMaterial.name : "null")}");
                }

                // Устанавливаем доступные материалы
                var availablePaintsField = wallPainter.GetType().GetField("availablePaints",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (availablePaintsField != null)
                {
                    availablePaintsField.SetValue(wallPainter, paintMaterials);
                    Debug.Log($"WallPaintingManager: установлено {paintMaterials.Length} материалов для WallPainter");
                }
            }

            // Настраиваем SimplePaintColorSelector
            if (colorSelector != null)
            {
                // Устанавливаем ссылку на WallPainter через рефлексию
                var wallPainterField = colorSelector.GetType().GetField("wallPainter",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (wallPainterField != null)
                {
                    wallPainterField.SetValue(colorSelector, wallPainter);
                    Debug.Log("WallPaintingManager: установлена ссылка на WallPainter для SimplePaintColorSelector");
                }

                // Устанавливаем материалы через рефлексию
                var paintMaterialsField = colorSelector.GetType().GetField("paintMaterials",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (paintMaterialsField != null)
                {
                    paintMaterialsField.SetValue(colorSelector, paintMaterials);
                    Debug.Log($"WallPaintingManager: установлено {paintMaterials.Length} материалов для SimplePaintColorSelector");
                }
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
            // Создаем новый объект стены
            GameObject wallObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallObject.transform.position = position;
            wallObject.transform.rotation = rotation;
            wallObject.transform.localScale = scale;

            // Устанавливаем слой Wall
            wallObject.layer = LayerMask.NameToLayer("Wall");

            // Создаем материал с заданным цветом
            Material material = new Material(Shader.Find("Standard"));
            material.color = color;

            // Применяем материал
            Renderer renderer = wallObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = material;
            }

            // Добавляем стену в WallPainter
            CreateWall(wallObject);
        }
    }
}