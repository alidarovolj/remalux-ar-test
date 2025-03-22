// ColorPickerManager.cs
using UnityEngine;
using UnityEngine.UI;
using Remalux.AR;

public class ColorPickerManager : MonoBehaviour
{
    [SerializeField] private WallPainter wallPainter;
    [SerializeField] private Button[] colorButtons;
    [SerializeField] private Material[] paintMaterials;

    private void Start()
    {
        if (wallPainter == null)
        {
            wallPainter = FindObjectOfType<WallPainter>();
            if (wallPainter == null)
            {
                Debug.LogError("ColorPickerManager: WallPainter reference not set!");
                return;
            }
        }

        SetupColorButtons();
    }

    private void SetupColorButtons()
    {
        if (colorButtons == null || paintMaterials == null ||
            colorButtons.Length == 0 || paintMaterials.Length == 0)
        {
            Debug.LogError("ColorPickerManager: Buttons or materials not set!");
            return;
        }

        int maxButtons = Mathf.Min(colorButtons.Length, paintMaterials.Length);

        for (int i = 0; i < maxButtons; i++)
        {
            int index = i; // Для использования в лямбда-выражении
            if (colorButtons[i] != null && paintMaterials[i] != null)
            {
                var image = colorButtons[i].GetComponent<Image>();
                if (image != null)
                {
                    image.color = paintMaterials[i].color;
                }
                colorButtons[i].onClick.AddListener(() => SelectPaint(index));
            }
        }
    }

    private void SelectPaint(int materialIndex)
    {
        if (wallPainter != null && materialIndex >= 0 && materialIndex < paintMaterials.Length)
        {
            wallPainter.SelectPaintMaterial(materialIndex);
        }
    }
}