using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Remalux.WallPainting
{
      public class TextureUI : MonoBehaviour
      {
            [Header("UI References")]
            [SerializeField] private Button previousButton;
            [SerializeField] private Button nextButton;
            [SerializeField] private Image previewImage;
            [SerializeField] private Text textureName;
            [SerializeField] private Image colorPreview;
            [SerializeField] private Button[] colorButtons;

            [Header("Color Presets")]
            [SerializeField]
            private Color[] colorPresets = new Color[]
            {
            Color.white,
            new Color(0.9f, 0.9f, 0.9f), // Light Gray
            new Color(0.8f, 0.8f, 0.8f), // Gray
            new Color(0.95f, 0.95f, 0.85f), // Cream
            new Color(0.95f, 0.9f, 0.8f), // Beige
            };

            private TextureManager textureManager;

            private void Start()
            {
                  textureManager = Object.FindFirstObjectByType<TextureManager>();
                  if (textureManager == null)
                  {
                        Debug.LogError("TextureManager not found in scene!");
                        return;
                  }

                  SetupButtons();
                  UpdateUI();
            }

            private void SetupButtons()
            {
                  if (previousButton != null)
                  {
                        previousButton.onClick.AddListener(OnPreviousTexture);
                  }

                  if (nextButton != null)
                  {
                        nextButton.onClick.AddListener(OnNextTexture);
                  }

                  for (int i = 0; i < colorButtons.Length; i++)
                  {
                        int index = i; // Capture the index for the lambda
                        colorButtons[i].onClick.AddListener(() => OnColorSelected(index));
                  }
            }

            private void OnPreviousTexture()
            {
                  textureManager.PreviousTexture();
                  UpdateUI();
            }

            private void OnNextTexture()
            {
                  textureManager.NextTexture();
                  UpdateUI();
            }

            private void OnColorSelected(int index)
            {
                  if (index >= 0 && index < colorPresets.Length)
                  {
                        textureManager.UpdateCurrentColor(colorPresets[index]);
                        UpdateUI();
                  }
            }

            private void UpdateUI()
            {
                  var currentPreset = textureManager.GetCurrentPreset();

                  if (previewImage != null && currentPreset.preview != null)
                  {
                        previewImage.sprite = Sprite.Create(
                            currentPreset.preview,
                            new Rect(0, 0, currentPreset.preview.width, currentPreset.preview.height),
                            new Vector2(0.5f, 0.5f)
                        );
                  }

                  if (textureName != null)
                  {
                        textureName.text = currentPreset.name;
                  }

                  if (colorPreview != null)
                  {
                        colorPreview.color = currentPreset.tintColor;
                  }
            }

            private void OnDestroy()
            {
                  if (previousButton != null)
                  {
                        previousButton.onClick.RemoveListener(OnPreviousTexture);
                  }

                  if (nextButton != null)
                  {
                        nextButton.onClick.RemoveListener(OnNextTexture);
                  }

                  for (int i = 0; i < colorButtons.Length; i++)
                  {
                        colorButtons[i].onClick.RemoveAllListeners();
                  }
            }
      }
}