using UnityEngine;
using UnityEngine.UI;

public class ColorManager : MonoBehaviour
{
    private Color currentColor = Color.red; // Текущий выбранный цвет
    
    // Ссылки на кнопки
    [SerializeField] private Button redButton;
    [SerializeField] private Button blueButton;
    [SerializeField] private Button greenButton;
    
    void Start()
    {
        // Добавляем обработчики нажатий
        redButton.onClick.AddListener(() => SetColor(Color.red));
        blueButton.onClick.AddListener(() => SetColor(Color.blue));
        greenButton.onClick.AddListener(() => SetColor(Color.green));
    }
    
    void SetColor(Color newColor)
    {
        currentColor = newColor;
        Debug.Log($"Color changed to: {newColor}");
        // Здесь позже добавим логику для рисования
    }
    
    public Color GetCurrentColor()
    {
        return currentColor;
    }
}