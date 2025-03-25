using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Remalux.AR
{
      public class PaintingUI : MonoBehaviour
      {
            [SerializeField] private Transform colorButtonContainer;
            [SerializeField] private GameObject colorButtonPrefab;
            [SerializeField] private WallPainter wallPainter;

            private List<ColorButton> colorButtons = new List<ColorButton>();

            // Dulux-like color presets
            private readonly Color[] availableColors = new Color[]
            {
                  new Color(0.95f, 0.95f, 0.95f), // Pure White
                  new Color(0.90f, 0.90f, 0.90f), // Antique White
                  new Color(0.85f, 0.85f, 0.85f), // Warm White
                  new Color(0.80f, 0.80f, 0.80f), // Cool White
                  new Color(0.95f, 0.90f, 0.85f), // Ivory
                  new Color(0.95f, 0.85f, 0.75f), // Cream
                  new Color(0.90f, 0.80f, 0.70f), // Beige
                  new Color(0.85f, 0.75f, 0.65f), // Light Beige
                  new Color(0.80f, 0.70f, 0.60f), // Warm Beige
                  new Color(0.75f, 0.65f, 0.55f), // Cool Beige
                  new Color(0.70f, 0.60f, 0.50f), // Taupe
                  new Color(0.65f, 0.55f, 0.45f), // Light Gray
                  new Color(0.60f, 0.50f, 0.40f), // Medium Gray
                  new Color(0.55f, 0.45f, 0.35f), // Dark Gray
                  new Color(0.50f, 0.40f, 0.30f)  // Charcoal
            };

            private void Start()
            {
                  if (wallPainter == null)
                        wallPainter = FindObjectOfType<WallPainter>();

                  if (wallPainter == null)
                  {
                        Debug.LogError("WallPainter not found in scene!");
                        return;
                  }

                  InitializeColorButtons();
            }

            private void InitializeColorButtons()
            {
                  if (colorButtonPrefab == null || colorButtonContainer == null)
                  {
                        Debug.LogError("ColorButton prefab or container not assigned!");
                        return;
                  }

                  // Create a grid layout for the buttons
                  GridLayoutGroup grid = colorButtonContainer.GetComponent<GridLayoutGroup>();
                  if (grid == null)
                  {
                        grid = colorButtonContainer.gameObject.AddComponent<GridLayoutGroup>();
                  }

                  // Configure the grid
                  grid.cellSize = new Vector2(60, 60);
                  grid.spacing = new Vector2(10, 10);
                  grid.padding = new RectOffset(10, 10, 10, 10);
                  grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                  grid.constraintCount = 5;

                  foreach (Color color in availableColors)
                  {
                        GameObject buttonObj = Instantiate(colorButtonPrefab, colorButtonContainer);
                        ColorButton colorButton = buttonObj.GetComponent<ColorButton>();

                        if (colorButton != null)
                        {
                              colorButton.SetColor(color);
                              colorButton.onColorSelected.AddListener(OnColorSelected);
                              colorButtons.Add(colorButton);
                        }
                  }
            }

            private void OnColorSelected(Color color)
            {
                  if (wallPainter != null)
                  {
                        Material paintMaterial = new Material(wallPainter.CurrentPaintMaterial);
                        paintMaterial.color = color;
                        wallPainter.SetCurrentPaintMaterial(paintMaterial);
                  }
            }

            public void Show()
            {
                  gameObject.SetActive(true);
            }

            public void Hide()
            {
                  gameObject.SetActive(false);
            }
      }
}