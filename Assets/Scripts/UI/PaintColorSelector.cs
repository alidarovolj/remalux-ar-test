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
                  InitializeComponents();
                  CreateColorButtons();
            }

            private void InitializeComponents()
            {
                  // Find WallPainter if not assigned
                  if (wallPainter == null)
                  {
                        wallPainter = FindObjectOfType<WallPainter>();
                        if (wallPainter == null)
                        {
                              Debug.LogError("PaintColorSelector: WallPainter component not found!");
                              return;
                        }
                  }

                  // Get materials from WallPainter if not assigned
                  if (paintMaterials == null || paintMaterials.Length == 0)
                  {
                        paintMaterials = wallPainter.availablePaints;
                        if (paintMaterials == null || paintMaterials.Length == 0)
                        {
                              Debug.LogError("PaintColorSelector: No paint materials available!");
                              return;
                        }
                  }

                  // Setup reset button
                  if (resetButton != null)
                  {
                        resetButton.onClick.RemoveAllListeners();
                        resetButton.onClick.AddListener(ResetWallColors);
                  }
            }

            private void CreateColorButtons()
            {
                  if (colorButtonPrefab == null || colorButtonsContainer == null || paintMaterials == null)
                  {
                        Debug.LogError("PaintColorSelector: Missing required components!");
                        return;
                  }

                  // Clear existing buttons
                  foreach (var button in colorButtons)
                  {
                        if (button != null)
                        {
                              Destroy(button.gameObject);
                        }
                  }
                  colorButtons.Clear();

                  // Create new buttons for each material
                  for (int i = 0; i < paintMaterials.Length; i++)
                  {
                        Material material = paintMaterials[i];
                        if (material == null) continue;

                        GameObject buttonObj = Instantiate(colorButtonPrefab, colorButtonsContainer);
                        Button button = buttonObj.GetComponent<Button>();

                        if (button != null)
                        {
                              // Set button color
                              Image buttonImage = button.GetComponent<Image>();
                              if (buttonImage != null && material.HasProperty("_Color"))
                              {
                                    buttonImage.color = material.GetColor("_Color");
                              }

                              // Add click listener
                              int index = i; // Capture index for lambda
                              button.onClick.AddListener(() => SelectColor(index));
                              colorButtons.Add(button);
                        }
                  }

                  // Select first color by default
                  if (colorButtons.Count > 0)
                  {
                        SelectColor(0);
                  }
            }

            private void SelectColor(int index)
            {
                  if (index < 0 || index >= paintMaterials.Length)
                        return;

                  selectedColorIndex = index;

                  // Update button visuals
                  for (int i = 0; i < colorButtons.Count; i++)
                  {
                        if (colorButtons[i] != null)
                        {
                              colorButtons[i].transform.localScale = (i == selectedColorIndex)
                                    ? new Vector3(1.2f, 1.2f, 1.2f)
                                    : Vector3.one;
                        }
                  }

                  // Update WallPainter
                  if (wallPainter != null)
                  {
                        wallPainter.SelectPaintMaterial(index);
                        Debug.Log($"PaintColorSelector: Selected color {index}, material: {paintMaterials[index].name}");
                  }
                  else
                  {
                        Debug.LogError("PaintColorSelector: WallPainter reference is missing!");
                  }
            }

            public void ResetWallColors()
            {
                  if (wallPainter != null)
                  {
                        wallPainter.ResetWallMaterials();
                        Debug.Log("PaintColorSelector: Reset wall colors");
                  }
            }
      }
}