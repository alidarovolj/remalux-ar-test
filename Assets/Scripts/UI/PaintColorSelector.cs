using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Remalux.AR
{
      public class PaintColorSelector : MonoBehaviour
      {
            [SerializeField] private WallPainter wallPainter;
            [SerializeField] private GameObject colorButtonPrefab;
            [SerializeField] private Transform colorButtonsContainer;
            [SerializeField] private Material[] paintMaterials;
            [SerializeField] private Button resetButton;

            private List<Button> colorButtons = new List<Button>();
            private int selectedColorIndex = -1;

            private void Start()
            {
                  if (wallPainter == null)
                        wallPainter = FindObjectOfType<WallPainter>();

                  if (resetButton != null)
                        resetButton.onClick.AddListener(ResetWallColors);

                  CreateColorButtons();
            }

            private void CreateColorButtons()
            {
                  if (colorButtonPrefab == null || colorButtonsContainer == null || paintMaterials == null)
                        return;

                  // Clear existing buttons
                  foreach (Transform child in colorButtonsContainer)
                  {
                        Destroy(child.gameObject);
                  }
                  colorButtons.Clear();

                  // Create new buttons for each paint material
                  for (int i = 0; i < paintMaterials.Length; i++)
                  {
                        Material material = paintMaterials[i];
                        GameObject buttonObj = Instantiate(colorButtonPrefab, colorButtonsContainer);
                        Button button = buttonObj.GetComponent<Button>();

                        // Set button color to match paint material
                        Image buttonImage = button.GetComponent<Image>();
                        if (buttonImage != null && material != null)
                        {
                              buttonImage.color = material.color;
                        }

                        // Add click listener
                        int index = i; // Capture index for lambda
                        button.onClick.AddListener(() => SelectColor(index));

                        colorButtons.Add(button);
                  }

                  // Select first color by default
                  if (paintMaterials.Length > 0)
                        SelectColor(0);
            }

            public void SelectColor(int index)
            {
                  if (index < 0 || index >= paintMaterials.Length)
                        return;

                  selectedColorIndex = index;

                  // Update button visuals to show selection
                  for (int i = 0; i < colorButtons.Count; i++)
                  {
                        // Add visual indication of selection (e.g., outline or scale)
                        colorButtons[i].transform.localScale = (i == selectedColorIndex)
                            ? new Vector3(1.2f, 1.2f, 1.2f)
                            : Vector3.one;
                  }

                  // Tell the WallPainter which color is selected
                  if (wallPainter != null)
                        wallPainter.SelectPaintMaterial(index);
            }

            public void ResetWallColors()
            {
                  if (wallPainter != null)
                        wallPainter.ResetWallMaterials();
            }
      }
}