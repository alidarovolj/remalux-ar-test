using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;

namespace Remalux.AR.Vision
{
    /// <summary>
    /// Класс для настройки сцены с системой покраски реальных стен
    /// </summary>
    public static class RealWallPaintingSetup
    {
        /// <summary>
        /// Создает новую сцену с системой покраски реальных стен.
        /// </summary>
        public static void CreateRealWallPaintingScene()
        {
            // Создаем новую сцену
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
            
            // Создаем основной объект для системы покраски стен
            GameObject mainObject = new GameObject("RealWallPaintingSystem");
            
            // Добавляем компоненты
            RealWallPaintingController controller = mainObject.AddComponent<RealWallPaintingController>();
            WallDetector wallDetector = mainObject.AddComponent<WallDetector>();
            WallMeshBuilder wallMeshBuilder = mainObject.AddComponent<WallMeshBuilder>();
            
            // Добавляем демонстрационный компонент
            RealWallPaintingDemo demo = mainObject.AddComponent<RealWallPaintingDemo>();
            
            // Настраиваем ссылки для контроллера через публичные методы
            controller.SetMainCamera(Camera.main);
            
            // Создаем материалы для покраски
            Material defaultMaterial = CreateDefaultMaterial();
            Material[] paintMaterials = CreatePaintMaterials();
            
            // Назначаем материалы контроллеру через публичные методы
            controller.SetDefaultWallMaterial(defaultMaterial);
            controller.SetPaintMaterials(paintMaterials);
            
            // Настраиваем ссылки для WallMeshBuilder через публичные методы
            wallMeshBuilder.SetWallDetector(wallDetector);
            wallMeshBuilder.SetMainCamera(Camera.main);
            wallMeshBuilder.SetDefaultWallMaterial(defaultMaterial);
            
            // Создаем UI элементы
            Canvas canvas = CreateCanvas();
            controller.SetMainCanvas(canvas);
            
            // Создаем контейнер для кнопок цветов
            GameObject colorButtonsContainer = CreateColorButtonsContainer(canvas);
            controller.SetColorButtonsContainer(colorButtonsContainer.GetComponent<RectTransform>());
            
            // Создаем кнопку сброса
            GameObject resetButton = CreateResetButton(canvas);
            controller.SetResetButton(resetButton.GetComponent<Button>());
            
            // Создаем префаб для кнопки цвета
            GameObject colorButtonPrefab = CreateColorButtonPrefab();
            controller.SetColorButtonPrefab(colorButtonPrefab);
            
            // Создаем префаб для предпросмотра цвета
            GameObject colorPreviewPrefab = CreateColorPreviewPrefab();
            controller.SetColorPreviewPrefab(colorPreviewPrefab);
            
            // Настраиваем ссылки для демонстрационного компонента через публичные методы
            demo.SetController(controller);
            demo.SetWallDetector(wallDetector);
            demo.SetMainCanvas(canvas);
            
            // Создаем отладочное отображение для визуальной обратной связи
            GameObject debugDisplay = CreateDebugDisplay(canvas);
            wallDetector.SetDebugImageDisplay(debugDisplay.GetComponent<RawImage>());
            
            // Сохраняем сцену
            string scenePath = EditorUtility.SaveFilePanel(
                "Сохранить сцену покраски реальных стен",
                Application.dataPath,
                "RealWallPaintingScene",
                "unity"
            );
            
            if (!string.IsNullOrEmpty(scenePath))
            {
                // Преобразуем абсолютный путь в относительный путь проекта
                if (scenePath.StartsWith(Application.dataPath))
                {
                    scenePath = "Assets" + scenePath.Substring(Application.dataPath.Length);
                }
                
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), scenePath);
                Debug.Log($"Сцена для покраски реальных стен успешно создана и сохранена по пути: {scenePath}");
            }
            else
            {
                Debug.Log("Создание сцены отменено пользователем.");
            }
        }
        
        /// <summary>
        /// Создает материалы для покраски.
        /// </summary>
        private static Material[] CreatePaintMaterials()
        {
            // Создаем директорию для материалов, если она не существует
            string materialsDir = "Assets/Materials/PaintMaterials";
            if (!Directory.Exists(materialsDir))
            {
                Directory.CreateDirectory(materialsDir);
            }
            
            // Создаем массив материалов с разными цветами
            Material[] materials = new Material[8];
            Color[] colors = new Color[]
            {
                new Color(1, 0, 0), // Красный
                new Color(0, 1, 0), // Зеленый
                new Color(0, 0, 1), // Синий
                new Color(1, 1, 0), // Желтый
                new Color(1, 0, 1), // Пурпурный
                new Color(0, 1, 1), // Голубой
                new Color(1, 0.5f, 0), // Оранжевый
                new Color(0.5f, 0, 1) // Фиолетовый
            };
            
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
                    
                    // Сохраняем материал как ассет
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                else
                {
                    // Обновляем существующий материал
                    material.color = colors[i];
                    EditorUtility.SetDirty(material);
                }
                
                materials[i] = material;
            }
            
            AssetDatabase.SaveAssets();
            return materials;
        }
        
        /// <summary>
        /// Создает материал по умолчанию для стен.
        /// </summary>
        private static Material CreateDefaultMaterial()
        {
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
                material.color = new Color(0.9f, 0.9f, 0.9f); // Светло-серый
                
                // Сохраняем материал как ассет
                AssetDatabase.CreateAsset(material, materialPath);
            }
            
            return material;
        }
        
        /// <summary>
        /// Создает Canvas для UI элементов.
        /// </summary>
        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject("Canvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            // Добавляем компоненты для работы Canvas
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            
            return canvas;
        }
        
        /// <summary>
        /// Создает контейнер для кнопок цветов.
        /// </summary>
        private static GameObject CreateColorButtonsContainer(Canvas canvas)
        {
            GameObject container = new GameObject("ColorButtonsContainer");
            container.transform.SetParent(canvas.transform, false);
            
            RectTransform rectTransform = container.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(1, 0);
            rectTransform.pivot = new Vector2(0.5f, 0);
            rectTransform.anchoredPosition = new Vector2(0, 50);
            rectTransform.sizeDelta = new Vector2(0, 100);
            
            // Добавляем горизонтальный layout group
            HorizontalLayoutGroup layoutGroup = container.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = 10;
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.padding = new RectOffset(10, 10, 10, 10);
            
            return container;
        }
        
        /// <summary>
        /// Создает кнопку сброса.
        /// </summary>
        private static GameObject CreateResetButton(Canvas canvas)
        {
            GameObject buttonObject = new GameObject("ResetButton");
            buttonObject.transform.SetParent(canvas.transform, false);
            
            RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1, 0);
            rectTransform.anchorMax = new Vector2(1, 0);
            rectTransform.pivot = new Vector2(1, 0);
            rectTransform.anchoredPosition = new Vector2(-20, 170);
            rectTransform.sizeDelta = new Vector2(120, 50);
            
            // Добавляем компоненты для кнопки
            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.8f, 0.2f, 0.2f);
            
            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
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
        /// Создает префаб для кнопки цвета.
        /// </summary>
        private static GameObject CreateColorButtonPrefab()
        {
            // Создаем директорию для префабов, если она не существует
            string prefabsDir = "Assets/Prefabs";
            if (!Directory.Exists(prefabsDir))
            {
                Directory.CreateDirectory(prefabsDir);
            }
            
            string prefabPath = $"{prefabsDir}/ColorButton.prefab";
            
            // Проверяем, существует ли префаб
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (prefab == null)
            {
                // Создаем новый префаб
                GameObject buttonObject = new GameObject("ColorButton");
                
                RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(60, 60);
                
                Image image = buttonObject.AddComponent<Image>();
                image.color = Color.white;
                
                Button button = buttonObject.AddComponent<Button>();
                ColorBlock colors = button.colors;
                colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
                colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
                button.colors = colors;
                
                // Добавляем компонент SimpleColorButton
                SimpleColorButton colorButton = buttonObject.AddComponent<SimpleColorButton>();
                colorButton.SetButtonImage(image);
                
                // Сохраняем объект как префаб
                prefab = PrefabUtility.SaveAsPrefabAsset(buttonObject, prefabPath);
                
                // Удаляем временный объект
                Object.DestroyImmediate(buttonObject);
            }
            
            return prefab;
        }
        
        /// <summary>
        /// Создает префаб для предпросмотра цвета.
        /// </summary>
        private static GameObject CreateColorPreviewPrefab()
        {
            // Создаем директорию для префабов, если она не существует
            string prefabsDir = "Assets/Prefabs";
            if (!Directory.Exists(prefabsDir))
            {
                Directory.CreateDirectory(prefabsDir);
            }
            
            string prefabPath = $"{prefabsDir}/ColorPreview.prefab";
            
            // Проверяем, существует ли префаб
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (prefab == null)
            {
                // Создаем новый префаб
                GameObject previewObject = new GameObject("ColorPreview");
                
                // Добавляем компонент SimpleColorPreview
                SimpleColorPreview colorPreview = previewObject.AddComponent<SimpleColorPreview>();
                
                // Создаем дочерний объект для визуализации
                GameObject visualObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                visualObject.transform.SetParent(previewObject.transform, false);
                visualObject.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                
                // Настраиваем компонент предпросмотра
                colorPreview.SetPreviewRenderer(visualObject.GetComponent<Renderer>());
                colorPreview.SetPreviewSize(0.2f);
                
                // Сохраняем объект как префаб
                prefab = PrefabUtility.SaveAsPrefabAsset(previewObject, prefabPath);
                
                // Удаляем временный объект
                Object.DestroyImmediate(previewObject);
            }
            
            return prefab;
        }
        
        /// <summary>
        /// Создает отладочное отображение для визуальной обратной связи.
        /// </summary>
        private static GameObject CreateDebugDisplay(Canvas canvas)
        {
            GameObject displayObject = new GameObject("DebugDisplay");
            displayObject.transform.SetParent(canvas.transform, false);
            
            RectTransform rectTransform = displayObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = new Vector2(10, -10);
            rectTransform.sizeDelta = new Vector2(200, 150);
            
            RawImage rawImage = displayObject.AddComponent<RawImage>();
            rawImage.color = Color.white;
            
            return displayObject;
        }
    }
} 