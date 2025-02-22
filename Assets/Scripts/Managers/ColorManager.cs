using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class ColorManager : MonoBehaviour
{
    [System.Serializable]
    public class DuluxColor
    {
        public string name;
        public string code;
        public Color color;
        public Texture2D texture;
        public string category;
    }

    [Header("Каталог цветов")]
    [SerializeField] private List<DuluxColor> colorCatalog = new List<DuluxColor>();
    [SerializeField] private Transform colorPaletteContainer;
    [SerializeField] private GameObject colorButtonPrefab;

    [Header("Избранное")]
    [SerializeField] private Transform favoritesContainer;
    [SerializeField] private int maxFavorites = 10;

    private Color currentColor = Color.red;
    private List<DuluxColor> favorites = new List<DuluxColor>();
    private Dictionary<string, List<DuluxColor>> categorizedColors = new Dictionary<string, List<DuluxColor>>();

    private readonly Color[] defaultColors = new Color[]
    {
        Color.red,
        Color.green,
        Color.blue,
        Color.yellow,
        new Color(1f, 0.5f, 0f), // Оранжевый
        new Color(0.5f, 0f, 0.5f), // Фиолетовый
        Color.cyan,
        Color.magenta
    };

    private void Start()
    {
        // Создаем UI элементы, если они не назначены
        if (colorPaletteContainer == null || colorButtonPrefab == null)
        {
            Debug.Log("Создание UI элементов программно...");
            CreateUIElements();
        }

        InitializeColorCatalog();
        CreateColorPalette();
        LoadFavorites();
        
        Debug.Log($"Инициализация начального цвета. Количество цветов в каталоге: {colorCatalog.Count}");
        SelectColorByIndex(0);
    }

    private void CreateUIElements()
    {
        // Находим ColorPanel
        var colorPanel = GameObject.Find("ColorPanel");
        if (colorPanel == null)
        {
            Debug.LogError("ColorPanel не найден в сцене!");
            return;
        }

        // Создаем контейнер для палитры, если он не назначен
        if (colorPaletteContainer == null)
        {
            var container = new GameObject("ColorPaletteContainer");
            container.transform.SetParent(colorPanel.transform, false);
            
            var rectTransform = container.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.sizeDelta = Vector2.zero;
            
            var gridLayout = container.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(50, 50);
            gridLayout.spacing = new Vector2(5, 5);
            gridLayout.padding = new RectOffset(5, 5, 5, 5);
            
            colorPaletteContainer = container.transform;
            Debug.Log("ColorPaletteContainer создан программно");
        }

        // Создаем префаб кнопки, если он не назначен
        if (colorButtonPrefab == null)
        {
            var buttonObj = new GameObject("ColorButtonPrefab");
            var rectTransform = buttonObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(50, 50);
            
            // Настраиваем Image
            var image = buttonObj.AddComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = true;
            image.maskable = true;
            
            // Настраиваем Button
            var button = buttonObj.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            button.colors = colors;
            
            colorButtonPrefab = buttonObj;
            Debug.Log("ColorButtonPrefab создан программно");
            
            // Сохраняем как префаб
            #if UNITY_EDITOR
            if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Prefabs"))
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Prefabs");
            UnityEditor.PrefabUtility.SaveAsPrefabAsset(buttonObj, "Assets/Prefabs/ColorButtonPrefab.prefab");
            GameObject.DestroyImmediate(buttonObj);
            #endif
        }
    }

    private void InitializeColorCatalog()
    {
        Debug.Log($"InitializeColorCatalog: Начало инициализации. Текущее количество цветов: {colorCatalog.Count}");

        if (colorCatalog.Count == 0)
        {
            Debug.Log("InitializeColorCatalog: Каталог пуст, добавляем стандартные цвета");
            for (int i = 0; i < defaultColors.Length; i++)
            {
                var duluxColor = new DuluxColor
                {
                    name = $"Color {i + 1}",
                    code = $"DEFAULT_{i}",
                    color = defaultColors[i],
                    category = "Default"
                };
                colorCatalog.Add(duluxColor);
                Debug.Log($"InitializeColorCatalog: Добавлен цвет {duluxColor.name}, RGB: {duluxColor.color}");
            }
        }

        categorizedColors = colorCatalog.GroupBy(c => c.category)
                                      .ToDictionary(g => g.Key, g => g.ToList());

        Debug.Log($"InitializeColorCatalog: Завершено. Категорий: {categorizedColors.Count}, всего цветов: {colorCatalog.Count}");
        foreach (var category in categorizedColors)
        {
            Debug.Log($"InitializeColorCatalog: Категория {category.Key}: {category.Value.Count} цветов");
        }
    }

    private void CreateColorPalette()
    {
        Debug.Log("CreateColorPalette: Начало создания цветовой палитры");
        
        if (colorPaletteContainer == null)
        {
            Debug.LogError("CreateColorPalette: ColorPaletteContainer не назначен!");
            return;
        }

        if (colorButtonPrefab == null)
        {
            Debug.LogError("CreateColorPalette: ColorButtonPrefab не назначен!");
            return;
        }

        // Создаем временный список для хранения объектов, которые нужно удалить
        var childrenToDestroy = new List<GameObject>();
        foreach (Transform child in colorPaletteContainer)
        {
            if (child != null && child.gameObject != null)
            {
                childrenToDestroy.Add(child.gameObject);
            }
        }
        
        // Удаляем старые объекты
        foreach (var child in childrenToDestroy)
        {
            if (child != null)
            {
                Destroy(child);
            }
        }

        // Создаем вертикальный layout для категорий
        var verticalLayout = colorPaletteContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        verticalLayout.spacing = 10;
        verticalLayout.padding = new RectOffset(5, 5, 5, 5);
        verticalLayout.childAlignment = TextAnchor.UpperLeft;
        verticalLayout.childControlHeight = true;
        verticalLayout.childControlWidth = true;
        verticalLayout.childForceExpandHeight = false;
        verticalLayout.childForceExpandWidth = true;

        foreach (var category in categorizedColors)
        {
            if (category.Value == null || category.Value.Count == 0) continue;

            Debug.Log($"CreateColorPalette: Создание категории: {category.Key}, количество цветов: {category.Value.Count}");
            
            // Создаем контейнер категории
            var categoryContainer = new GameObject(category.Key);
            var categoryRect = categoryContainer.AddComponent<RectTransform>();
            categoryContainer.transform.SetParent(colorPaletteContainer, false);

            // Создаем заголовок категории
            var categoryHeader = new GameObject("Header");
            categoryHeader.transform.SetParent(categoryContainer.transform, false);
            var headerText = categoryHeader.AddComponent<Text>();
            headerText.text = category.Key;
            headerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            headerText.fontSize = 14;
            headerText.color = Color.black;
            headerText.alignment = TextAnchor.MiddleLeft;
            
            var headerRect = categoryHeader.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.sizeDelta = new Vector2(0, 20);
            headerRect.anchoredPosition = new Vector2(5, -10);

            // Настраиваем RectTransform для категории
            categoryRect.anchorMin = new Vector2(0, 0);
            categoryRect.anchorMax = new Vector2(1, 0);
            categoryRect.sizeDelta = new Vector2(0, 80); // Увеличиваем высоту для заголовка и кнопок
            
            // Создаем контейнер для кнопок
            var buttonsContainer = new GameObject("ButtonsContainer");
            buttonsContainer.transform.SetParent(categoryContainer.transform, false);
            var buttonsRect = buttonsContainer.AddComponent<RectTransform>();
            buttonsRect.anchorMin = new Vector2(0, 0);
            buttonsRect.anchorMax = new Vector2(1, 1);
            buttonsRect.offsetMin = new Vector2(0, 25); // Отступ для заголовка
            
            // Добавляем горизонтальный layout для кнопок
            var buttonsLayout = buttonsContainer.AddComponent<HorizontalLayoutGroup>();
            buttonsLayout.spacing = 5;
            buttonsLayout.padding = new RectOffset(5, 5, 5, 5);
            buttonsLayout.childAlignment = TextAnchor.MiddleLeft;
            buttonsLayout.childControlHeight = false;
            buttonsLayout.childControlWidth = false;
            buttonsLayout.childForceExpandHeight = false;
            buttonsLayout.childForceExpandWidth = false;

            foreach (var duluxColor in category.Value)
            {
                if (duluxColor == null) continue;

                Debug.Log($"CreateColorPalette: Создание кнопки для цвета {duluxColor.name}, RGB: {duluxColor.color}");
                var buttonObj = Instantiate(colorButtonPrefab, buttonsContainer.transform);
                var button = buttonObj.GetComponent<Button>();
                var image = buttonObj.GetComponent<Image>();

                if (image != null)
                {
                    image.color = duluxColor.color;
                    if (duluxColor.texture != null)
                    {
                        image.sprite = Sprite.Create(duluxColor.texture, 
                            new Rect(0, 0, duluxColor.texture.width, duluxColor.texture.height), 
                            Vector2.one * 0.5f);
                    }
                }

                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    var colorForButton = duluxColor;
                    button.onClick.AddListener(() => SetColor(colorForButton));
                }
            }
        }
        
        Debug.Log("CreateColorPalette: Создание цветовой палитры завершено");
    }

    private void SetColor(DuluxColor duluxColor)
    {
        Debug.Log($"SetColor: Попытка установить цвет: {duluxColor.name}, RGB: {duluxColor.color}");
        if (currentColor != duluxColor.color)
        {
            Debug.Log($"SetColor: Изменение цвета с {currentColor} на {duluxColor.color}");
            currentColor = duluxColor.color;
            OnColorSelected?.Invoke(duluxColor);
            Debug.Log($"SetColor: Цвет успешно установлен, текущий цвет: {currentColor}");
        }
        else
        {
            Debug.Log("SetColor: Цвет не изменился, так как новый цвет совпадает с текущим");
        }
    }

    public void AddToFavorites(DuluxColor color)
    {
        if (favorites.Count >= maxFavorites)
            favorites.RemoveAt(favorites.Count - 1);

        if (!favorites.Contains(color))
        {
            favorites.Insert(0, color);
            UpdateFavoritesUI();
            SaveFavorites();
        }
    }

    private void UpdateFavoritesUI()
    {
        foreach (Transform child in favoritesContainer)
            Destroy(child.gameObject);

        foreach (var favorite in favorites)
        {
            var buttonObj = Instantiate(colorButtonPrefab, favoritesContainer);
            var button = buttonObj.GetComponent<Button>();
            var image = buttonObj.GetComponent<Image>();

            image.color = favorite.color;
            if (favorite.texture != null)
            {
                image.sprite = Sprite.Create(favorite.texture, 
                    new Rect(0, 0, favorite.texture.width, favorite.texture.height), 
                    Vector2.one * 0.5f);
            }

            button.onClick.AddListener(() => SetColor(favorite));
        }
    }

    private void SaveFavorites()
    {
        var favoriteData = favorites.Select(f => f.code).ToArray();
        PlayerPrefs.SetString("FavoriteColors", string.Join(",", favoriteData));
        PlayerPrefs.Save();
    }

    private void LoadFavorites()
    {
        var savedFavorites = PlayerPrefs.GetString("FavoriteColors", "");
        if (string.IsNullOrEmpty(savedFavorites)) return;

        var favoritesCodes = savedFavorites.Split(',');
        favorites = colorCatalog.Where(c => favoritesCodes.Contains(c.code)).ToList();
        UpdateFavoritesUI();
    }

    public Color GetCurrentColor()
    {
        Debug.Log($"GetCurrentColor: Возвращаем текущий цвет: {currentColor}");
        return currentColor;
    }

    public DuluxColor GetCurrentDuluxColor()
    {
        return colorCatalog.FirstOrDefault(c => c.color == currentColor);
    }

    public DuluxColor GetColorByCode(string code)
    {
        return colorCatalog.FirstOrDefault(c => c.code == code);
    }

    public void SelectColorByIndex(int index)
    {
        Debug.Log($"SelectColorByIndex: Попытка выбрать цвет по индексу {index}. Всего цветов: {colorCatalog.Count}");
        if (index >= 0 && index < colorCatalog.Count)
        {
            Debug.Log($"SelectColorByIndex: Выбираем цвет {colorCatalog[index].name}");
            SetColor(colorCatalog[index]);
        }
        else
        {
            Debug.LogError($"SelectColorByIndex: Неверный индекс {index}. Доступно цветов: {colorCatalog.Count}");
        }
    }

    public delegate void ColorSelectedHandler(DuluxColor color);
    public event ColorSelectedHandler OnColorSelected;
}