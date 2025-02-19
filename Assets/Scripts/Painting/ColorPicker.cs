// ColorPickerManager.cs
using UnityEngine;
using UnityEngine.UI;

public class ColorPickerManager : MonoBehaviour
{
    [SerializeField] private WallPainter wallPainter;
    [SerializeField] private Button[] colorButtons;
    [SerializeField] private Color[] colors;

    private void Start()
    {
        SetupColorButtons();
    }

    private void SetupColorButtons()
    {
        for (int i = 0; i < colorButtons.Length; i++)
        {
            int index = i; // Для использования в лямбда-выражении
            colorButtons[i].GetComponent<Image>().color = colors[i];
            colorButtons[i].onClick.AddListener(() => SelectColor(index));
        }
    }

    private void SelectColor(int colorIndex)
    {
        wallPainter.SetPaintColor(colors[colorIndex]);
    }
}