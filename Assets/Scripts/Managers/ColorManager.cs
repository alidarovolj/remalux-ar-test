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

    private Color currentColor = Color.white;
    private List<DuluxColor> favorites = new List<DuluxColor>();
    private Dictionary<string, List<DuluxColor>> categorizedColors = new Dictionary<string, List<DuluxColor>>();

    private void Start()
    {
        InitializeColorCatalog();
        CreateColorPalette();
        LoadFavorites();
    }

    private void InitializeColorCatalog()
    {
        // Группируем цвета по категориям
        categorizedColors = colorCatalog.GroupBy(c => c.category)
                                      .ToDictionary(g => g.Key, g => g.ToList());
    }

    private void CreateColorPalette()
    {
        foreach (var category in categorizedColors)
        {
            var categoryContainer = new GameObject(category.Key).transform;
            categoryContainer.SetParent(colorPaletteContainer, false);

            foreach (var duluxColor in category.Value)
            {
                var buttonObj = Instantiate(colorButtonPrefab, categoryContainer);
                var button = buttonObj.GetComponent<Button>();
                var image = buttonObj.GetComponent<Image>();

                image.color = duluxColor.color;
                if (duluxColor.texture != null)
                {
                    image.sprite = Sprite.Create(duluxColor.texture, 
                        new Rect(0, 0, duluxColor.texture.width, duluxColor.texture.height), 
                        Vector2.one * 0.5f);
                }

                button.onClick.AddListener(() => SetColor(duluxColor));
            }
        }
    }

    private void SetColor(DuluxColor duluxColor)
    {
        currentColor = duluxColor.color;
        OnColorSelected?.Invoke(duluxColor);
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
        // Очищаем текущие избранные
        foreach (Transform child in favoritesContainer)
            Destroy(child.gameObject);

        // Создаем новые кнопки для избранных цветов
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

    // События
    public delegate void ColorSelectedHandler(DuluxColor color);
    public event ColorSelectedHandler OnColorSelected;
}