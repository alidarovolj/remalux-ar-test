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
                  textureManager = FindObjectOfType<TextureManager>();
                  if (textureManager == null)
                  {
                        Debug.LogError("TextureUI: TextureManager не найден!");
                        enabled = false;
                        return;
                  }

                  SetupButtons();
                  UpdateUI();
            }

            private void SetupButtons()
            {
                  if (previousButton != null)
                        previousButton.onClick.AddListener(OnPreviousTexture);

                  if (nextButton != null)
                        nextButton.onClick.AddListener(OnNextTexture);

                  // Настраиваем кнопки цветов
                  for (int i = 0; i < colorButtons.Length && i < colorPresets.Length; i++)
                  {
                        int index = i; // Для замыкания
                        if (colorButtons[i] != null)
                        {
                              Image buttonImage = colorButtons[i].GetComponent<Image>();
                              if (buttonImage != null)
                                    buttonImage.color = colorPresets[i];

                              colorButtons[i].onClick.AddListener(() => OnColorSelected(index));
                        }
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
                  if (currentPreset.preview != null)
                  {
                        if (previewImage != null)
                        {
                              var texture = currentPreset.preview;
                              previewImage.sprite = Sprite.Create(
                                    texture,
                                    new Rect(0, 0, texture.width, texture.height),
                                    new Vector2(0.5f, 0.5f)
                              );
                        }

                        if (textureName != null)
                              textureName.text = currentPreset.name;

                        if (colorPreview != null)
                              colorPreview.color = currentPreset.tintColor;
                  }
            }

            private void OnDestroy()
            {
                  if (previousButton != null)
                        previousButton.onClick.RemoveListener(OnPreviousTexture);

                  if (nextButton != null)
                        nextButton.onClick.RemoveListener(OnNextTexture);

                  for (int i = 0; i < colorButtons.Length; i++)
                  {
                        if (colorButtons[i] != null)
                        {
                              int index = i;
                              colorButtons[i].onClick.RemoveListener(() => OnColorSelected(index));
                        }
                  }
            }
      }
}