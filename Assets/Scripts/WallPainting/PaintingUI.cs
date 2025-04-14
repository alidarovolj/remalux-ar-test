using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Remalux.WallPainting;

namespace Remalux.WallPainting
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
                  new Color(0.95f, 0.95f, 0.85f), // Cream
                  new Color(0.95f, 0.90f, 0.80f), // Beige
                  new Color(0.90f, 0.85f, 0.75f), // Warm Beige
                  new Color(0.85f, 0.80f, 0.70f), // Cool Beige
                  new Color(0.95f, 0.90f, 0.85f), // Light Pink
                  new Color(0.90f, 0.85f, 0.80f), // Warm Pink
                  new Color(0.85f, 0.80f, 0.75f), // Cool Pink
                  new Color(0.95f, 0.85f, 0.75f), // Light Peach
                  new Color(0.90f, 0.80f, 0.70f), // Warm Peach
                  new Color(0.85f, 0.75f, 0.65f), // Cool Peach
                  new Color(0.95f, 0.80f, 0.65f), // Light Orange
                  new Color(0.90f, 0.75f, 0.60f), // Warm Orange
                  new Color(0.85f, 0.70f, 0.55f), // Cool Orange
                  new Color(0.95f, 0.75f, 0.55f), // Light Brown
                  new Color(0.90f, 0.70f, 0.50f), // Warm Brown
                  new Color(0.85f, 0.65f, 0.45f)  // Cool Brown
            };

            private void Start()
            {
                  InitializeColorButtons();
            }

            private void InitializeColorButtons()
            {
                  foreach (Color color in availableColors)
                  {
                        GameObject buttonObj = Instantiate(colorButtonPrefab, colorButtonContainer);
                        ColorButton colorButton = buttonObj.GetComponent<ColorButton>();
                        if (colorButton != null)
                        {
                              colorButton.Initialize(color, OnColorSelected);
                              colorButtons.Add(colorButton);
                        }
                  }
            }

            private void OnColorSelected(Color color)
            {
                  if (wallPainter != null)
                  {
                        wallPainter.SetPaintColor(color);
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