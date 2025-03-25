#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace Remalux.AR
{
    public class ColorButtonPrefabCreator
    {
        private const string PREFAB_PATH = "Assets/Prefabs/UI";
        private const string PREFAB_NAME = "ColorButton.prefab";

        [MenuItem("Tools/Remalux/Create Color Button Prefab")]
        public static void CreateColorButtonPrefab()
        {
            // Создаем базовый объект
            GameObject buttonObj = new GameObject("ColorButton");

            // Добавляем компоненты UI
            RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
            Image image = buttonObj.AddComponent<Image>();
            Button button = buttonObj.AddComponent<Button>();
            SimpleColorButton colorButton = buttonObj.AddComponent<SimpleColorButton>();

            // Настраиваем RectTransform
            rectTransform.sizeDelta = new Vector2(50, 50);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            // Настраиваем Image
            image.color = Color.white;

            // Настраиваем Button
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
            button.colors = colors;

            // Создаем директорию если её нет
            if (!AssetDatabase.IsValidFolder(PREFAB_PATH))
            {
                string[] folderNames = PREFAB_PATH.Split('/');
                string currentPath = folderNames[0];
                for (int i = 1; i < folderNames.Length; i++)
                {
                    string newPath = $"{currentPath}/{folderNames[i]}";
                    if (!AssetDatabase.IsValidFolder(newPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, folderNames[i]);
                    }
                    currentPath = newPath;
                }
            }

            // Создаем префаб
            string prefabPath = $"{PREFAB_PATH}/{PREFAB_NAME}";
            bool success = false;
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (existingPrefab != null)
            {
                // Обновляем существующий префаб
                success = PrefabUtility.SaveAsPrefabAsset(buttonObj, prefabPath);
            }
            else
            {
                // Создаем новый префаб
                success = PrefabUtility.SaveAsPrefabAsset(buttonObj, prefabPath);
            }

            // Удаляем временный объект
            Object.DestroyImmediate(buttonObj);

            if (success)
            {
                Debug.Log($"ColorButton префаб создан успешно: {prefabPath}");
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }
            else
            {
                Debug.LogError("Не удалось создать ColorButton префаб");
            }
        }
    }
}
#endif