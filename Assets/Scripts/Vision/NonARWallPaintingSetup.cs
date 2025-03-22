using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Remalux.AR.Vision
{
    /// <summary>
    /// Класс для настройки сцены с системой покраски стен без использования AR
    /// </summary>
    public static class NonARWallPaintingSetup
    {
        /// <summary>
        /// Создает новую сцену с системой покраски стен без использования AR
        /// </summary>
        public static void CreateNonARWallPaintingScene()
        {
#if UNITY_EDITOR
            // Создаем новую сцену
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
            
            // Создаем основной объект для системы покраски стен
            GameObject wallPaintingSystem = new GameObject("WallPaintingSystem");
            
            // Добавляем компоненты
            RealWallPaintingController controller = wallPaintingSystem.AddComponent<RealWallPaintingController>();
            WallDetector wallDetector = wallPaintingSystem.AddComponent<WallDetector>();
            WallMeshBuilder wallMeshBuilder = wallPaintingSystem.AddComponent<WallMeshBuilder>();
            
            // Настраиваем ссылки для контроллера
            controller.SetMainCamera(Camera.main);
            controller.SetWallMeshBuilder(wallMeshBuilder);
            controller.SetWallDetector(wallDetector);
            
            // Создаем и настраиваем материалы
            Material defaultMaterial = CreateDefaultMaterial();
            Material[] paintMaterials = CreatePaintMaterials();
            
            controller.SetDefaultWallMaterial(defaultMaterial);
            controller.SetPaintMaterials(paintMaterials);
            
            // Настраиваем ссылки для построителя меша стен
            wallMeshBuilder.SetDefaultWallMaterial(defaultMaterial);
            wallMeshBuilder.SetMainCamera(Camera.main);
            wallMeshBuilder.SetWallDetector(wallDetector);
            
            // Создаем UI
            Canvas mainCanvas = CreateCanvas();
            controller.SetMainCanvas(mainCanvas);
            
            GameObject colorButtonsContainer = CreateColorButtonsContainer(mainCanvas);
            controller.SetColorButtonsContainer(colorButtonsContainer.GetComponent<RectTransform>());
            
            GameObject resetButton = CreateResetButton(mainCanvas);
            controller.SetResetButton(resetButton.GetComponent<Button>());
            
            GameObject colorButtonPrefab = CreateColorButtonPrefab();
            controller.SetColorButtonPrefab(colorButtonPrefab);
            
            GameObject colorPreviewPrefab = CreateColorPreviewPrefab();
            controller.SetColorPreviewPrefab(colorPreviewPrefab);
            
            // Добавляем демо-компонент для удобства использования
            RealWallPaintingDemo demo = wallPaintingSystem.AddComponent<RealWallPaintingDemo>();
            demo.SetController(controller);
            demo.SetWallDetector(wallDetector);
            demo.SetMainCanvas(mainCanvas);
            
            // Создаем дисплей для отладочной информации
            GameObject debugDisplay = CreateDebugDisplay(mainCanvas);
            wallDetector.SetDebugImageDisplay(debugDisplay.GetComponent<RawImage>());
            
            // Сохраняем сцену
            string savePath = EditorUtility.SaveFilePanel("Сохранить сцену", "Assets", "WallPaintingScene", "unity");
            if (!string.IsNullOrEmpty(savePath))
            {
                string relativePath = "Assets" + savePath.Substring(Application.dataPath.Length);
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), relativePath);
                Debug.Log($"Сцена сохранена по пути: {relativePath}");
            }
            
            Debug.Log("Сцена для покраски стен без AR успешно создана!");
#else
            Debug.LogError("Этот метод можно вызывать только в редакторе Unity");
#endif
        }
        
        /// <summary>
        /// Создает материалы для покраски стен
        /// </summary>
        private static Material[] CreatePaintMaterials()
        {
#if UNITY_EDITOR
            // Создаем директорию для материалов, если она не существует
            string materialsDir = "Assets/Materials";
            if (!Directory.Exists(materialsDir))
            {
                Directory.CreateDirectory(materialsDir);
            }
            
            // Создаем базовые цвета для покраски
            Color[] colors = new Color[]
            {
                new Color(0.9f, 0.1f, 0.1f), // Красный
                new Color(0.1f, 0.6f, 0.1f), // Зеленый
                new Color(0.1f, 0.3f, 0.9f), // Синий
                new Color(0.9f, 0.9f, 0.1f), // Желтый
                new Color(0.9f, 0.5f, 0.1f), // Оранжевый
                new Color(0.6f, 0.1f, 0.6f), // Фиолетовый
                new Color(0.1f, 0.7f, 0.7f), // Бирюзовый
                new Color(0.7f, 0.7f, 0.7f)  // Серый
            };
            
            List<Material> materials = new List<Material>();
            
            // Создаем материал для каждого цвета
            for (int i = 0; i < colors.Length; i++)
            {
                string materialPath = $"{materialsDir}/PaintMaterial_{i}.mat";
                
                // Проверяем, существует ли материал
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                
                if (material == null)
                {
                    // Создаем новый материал
                    material = new Material(Shader.Find("Standard"));
                    material.color = colors[i];
                    
                    // Сохраняем материал
                    AssetDatabase.CreateAsset(material, materialPath);
                    Debug.Log($"Создан материал: {materialPath}");
                }
                else
                {
                    // Обновляем существующий материал
                    material.color = colors[i];
                    EditorUtility.SetDirty(material);
                    Debug.Log($"Обновлен материал: {materialPath}");
                }
                
                materials.Add(material);
            }
            
            AssetDatabase.SaveAssets();
            
            return materials.ToArray();
#else
            return new Material[0];
#endif
        }
        
        /// <summary>
        /// Создает материал по умолчанию для стен
        /// </summary>
        private static Material CreateDefaultMaterial()
        {
#if UNITY_EDITOR
            // Создаем директорию для материалов, если она не существует
            string materialsDir = "Assets/Materials";
            if (!Directory.Exists(materialsDir))
            {
                Directory.CreateDirectory(materialsDir);
            }
            
            string materialPath = $"{materialsDir}/DefaultWallMaterial.mat";
            
            // Проверяем, существует ли материал
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            
            if (material == null)
            {
                // Создаем новый материал
                material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.95f, 0.95f, 0.95f); // Почти белый
                
                // Сохраняем материал
                AssetDatabase.CreateAsset(material, materialPath);
                Debug.Log($"Создан материал по умолчанию: {materialPath}");
            }
            
            return material;
#else
            return null;
#endif
        }
        
        /// <summary>
        /// Создает Canvas для UI
        /// </summary>
        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject("Canvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            
            return canvas;
        }
        
        /// <summary>
        /// Создает контейнер для кнопок цветов
        /// </summary>
        private static GameObject CreateColorButtonsContainer(Canvas canvas)
        {
            GameObject containerObject = new GameObject("ColorButtonsContainer");
            containerObject.transform.SetParent(canvas.transform, false);
            
            RectTransform rectTransform = containerObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(1, 0.2f);
            rectTransform.offsetMin = new Vector2(10, 10);
            rectTransform.offsetMax = new Vector2(-10, -10);
            
            // Добавляем горизонтальную группу для выравнивания кнопок
            HorizontalLayoutGroup layoutGroup = containerObject.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = 10;
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            
            return containerObject;
        }
        
        /// <summary>
        /// Создает кнопку сброса
        /// </summary>
        private static GameObject CreateResetButton(Canvas canvas)
        {
            GameObject buttonObject = new GameObject("ResetButton");
            buttonObject.transform.SetParent(canvas.transform, false);
            
            RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.9f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.9f);
            rectTransform.sizeDelta = new Vector2(160, 40);
            rectTransform.anchoredPosition = Vector2.zero;
            
            Image image = buttonObject.AddComponent<Image>();
            image.color = Color.white;
            
            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.8f, 0.2f, 0.2f);
            colors.highlightedColor = new Color(0.9f, 0.3f, 0.3f);
            colors.pressedColor = new Color(0.7f, 0.1f, 0.1f);
            button.colors = colors;
            
            // Добавляем текст на кнопку
            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(buttonObject.transform, false);
            
            RectTransform textRectTransform = textObject.AddComponent<RectTransform>();
            textRectTransform.anchorMin = Vector2.zero;
            textRectTransform.anchorMax = Vector2.one;
            textRectTransform.offsetMin = Vector2.zero;
            textRectTransform.offsetMax = Vector2.zero;
            
            Text text = textObject.AddComponent<Text>();
            text.text = "Сбросить";
            text.fontSize = 20;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            
            return buttonObject;
        }
        
        /// <summary>
        /// Создает префаб для кнопки цвета
        /// </summary>
        private static GameObject CreateColorButtonPrefab()
        {
#if UNITY_EDITOR
            // Создаем директорию для префабов, если она не существует
            string prefabsDir = "Assets/Prefabs";
            if (!Directory.Exists(prefabsDir))
            {
                Directory.CreateDirectory(prefabsDir);
            }
            
            string prefabPath = $"{prefabsDir}/ColorButton.prefab";
            
            // Проверяем, существует ли префаб
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (prefab != null)
            {
                return prefab;
            }
            
            // Создаем новый объект для префаба
            GameObject buttonObject = new GameObject("ColorButton");
            
            RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(60, 60);
            
            Image image = buttonObject.AddComponent<Image>();
            image.color = Color.white;
            
            Button button = buttonObject.AddComponent<Button>();
            
            // Добавляем компонент SimpleColorButton
            SimpleColorButton colorButton = buttonObject.AddComponent<SimpleColorButton>();
            colorButton.SetButtonImage(image);
            
            // Создаем префаб
            prefab = PrefabUtility.SaveAsPrefabAsset(buttonObject, prefabPath);
            
            // Удаляем временный объект
            Object.DestroyImmediate(buttonObject);
            
            Debug.Log($"Создан префаб кнопки цвета: {prefabPath}");
            
            return prefab;
#else
            return null;
#endif
        }
        
        /// <summary>
        /// Создает префаб для предпросмотра цвета
        /// </summary>
        private static GameObject CreateColorPreviewPrefab()
        {
#if UNITY_EDITOR
            // Создаем директорию для префабов, если она не существует
            string prefabsDir = "Assets/Prefabs";
            if (!Directory.Exists(prefabsDir))
            {
                Directory.CreateDirectory(prefabsDir);
            }
            
            string prefabPath = $"{prefabsDir}/ColorPreview.prefab";
            
            // Проверяем, существует ли префаб
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (prefab != null)
            {
                return prefab;
            }
            
            // Создаем новый объект для префаба
            GameObject previewObject = new GameObject("ColorPreview");
            
            // Добавляем компонент SimpleColorPreview
            SimpleColorPreview previewComponent = previewObject.AddComponent<SimpleColorPreview>();
            
            // Создаем дочерний объект для визуализации
            GameObject visualObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            visualObject.transform.SetParent(previewObject.transform, false);
            visualObject.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            
            // Настраиваем компонент предпросмотра
            previewComponent.SetPreviewRenderer(visualObject.GetComponent<Renderer>());
            previewComponent.SetPreviewSize(0.2f);
            
            // Создаем префаб
            prefab = PrefabUtility.SaveAsPrefabAsset(previewObject, prefabPath);
            
            // Удаляем временный объект
            Object.DestroyImmediate(previewObject);
            
            Debug.Log($"Создан префаб предпросмотра цвета: {prefabPath}");
            
            return prefab;
#else
            return null;
#endif
        }
        
        /// <summary>
        /// Создает дисплей для отладочной информации
        /// </summary>
        private static GameObject CreateDebugDisplay(Canvas canvas)
        {
            GameObject displayObject = new GameObject("DebugDisplay");
            displayObject.transform.SetParent(canvas.transform, false);
            
            RectTransform rectTransform = displayObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.7f, 0.7f);
            rectTransform.anchorMax = new Vector2(0.95f, 0.95f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            RawImage rawImage = displayObject.AddComponent<RawImage>();
            rawImage.color = Color.white;
            
            return displayObject;
        }
    }
} 