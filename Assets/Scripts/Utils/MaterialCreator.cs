using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Remalux.AR
{
    /// <summary>
    /// Утилита для создания базовых материалов для покраски стен
    /// </summary>
    public class MaterialCreator : MonoBehaviour
    {
        [Header("Настройки материалов")]
        [SerializeField]
        private Color[] colors = new Color[]
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

        [SerializeField]
        private string[] materialNames = new string[]
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

        [SerializeField] private string outputFolder = "Assets/Materials/Paints";
        [SerializeField] private string defaultMaterialName = "DefaultWallMaterial";
        [SerializeField] private Color defaultColor = new Color(0.9f, 0.9f, 0.9f);

#if UNITY_EDITOR
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

        /// <summary>
        /// Создает все материалы для покраски стен
        /// </summary>
        public void CreateAllMaterials()
        {
            // Создаем директорию, если она не существует
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            // Создаем материал по умолчанию
            CreateDefaultMaterial();

            // Создаем материалы для покраски
            for (int i = 0; i < colors.Length; i++)
            {
                if (i < materialNames.Length)
                {
                    CreatePaintMaterial(colors[i], materialNames[i]);
                }
                else
                {
                    CreatePaintMaterial(colors[i], $"Paint_{i}");
                }
            }

            // Обновляем базу ассетов
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Создает материал по умолчанию для стен
        /// </summary>
        private void CreateDefaultMaterial()
        {
            string path = $"{outputFolder}/{defaultMaterialName}.mat";

            // Проверяем, существует ли уже материал
            Material existingMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existingMaterial != null)
            {
                Debug.Log($"Материал {defaultMaterialName} уже существует. Обновляем его.");
                existingMaterial.color = defaultColor;
                EditorUtility.SetDirty(existingMaterial);
                AssetDatabase.SaveAssets();
                return;
            }

            // Создаем новый материал
            Material material = new Material(GetAppropriateShader());
            material.color = defaultColor;
            
            // Настраиваем свойства материала
            material.SetFloat("_Glossiness", 0.1f); // Низкий глянец
            material.SetFloat("_Metallic", 0.0f);   // Не металлический

            // Сохраняем материал
            AssetDatabase.CreateAsset(material, path);
            Debug.Log($"Создан материал: {path}");
        }

        /// <summary>
        /// Создает материал для покраски стен
        /// </summary>
        /// <param name="color">Цвет материала</param>
        /// <param name="name">Имя материала</param>
        private void CreatePaintMaterial(Color color, string name)
        {
            string path = $"{outputFolder}/{name}.mat";

            // Проверяем, существует ли уже материал
            Material existingMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existingMaterial != null)
            {
                Debug.Log($"Материал {name} уже существует. Обновляем его.");
                existingMaterial.color = color;
                EditorUtility.SetDirty(existingMaterial);
                AssetDatabase.SaveAssets();
                return;
            }

            // Создаем новый материал
            Material material = new Material(GetAppropriateShader());
            material.color = color;
            
            // Настраиваем свойства материала
            material.SetFloat("_Glossiness", 0.2f); // Низкий глянец
            material.SetFloat("_Metallic", 0.0f);   // Не металлический

            // Сохраняем материал
            AssetDatabase.CreateAsset(material, path);
            Debug.Log($"Создан материал: {path}");
        }
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// Редактор для MaterialCreator
    /// </summary>
    [CustomEditor(typeof(MaterialCreator))]
    public class MaterialCreatorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MaterialCreator creator = (MaterialCreator)target;

            EditorGUILayout.Space();
            if (GUILayout.Button("Создать все материалы"))
            {
                creator.CreateAllMaterials();
            }
        }
    }
#endif
}