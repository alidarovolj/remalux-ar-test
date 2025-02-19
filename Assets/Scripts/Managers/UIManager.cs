// UIManager.cs
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject scanningUI;
    [SerializeField] private GameObject colorPickerUI;
    [SerializeField] private GameObject paintingUI;
    
    public void ShowScanningUI()
    {
        scanningUI.SetActive(true);
        colorPickerUI.SetActive(false);
        paintingUI.SetActive(false);
    }
    
    public void ShowColorPicker()
    {
        scanningUI.SetActive(false);
        colorPickerUI.SetActive(true);
        paintingUI.SetActive(false);
    }
    
    public void ShowPaintingUI()
    {
        scanningUI.SetActive(false);
        colorPickerUI.SetActive(false);
        paintingUI.SetActive(true);
    }
}