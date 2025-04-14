using UnityEngine;
using System.Collections.Generic;

namespace Remalux.WallPainting
{
      public class TextureManager : MonoBehaviour, IWallTextureApplier
      {
            [System.Serializable]
            public struct TexturePreset
            {
                  public string name;
                  public Material material;
                  public Texture2D preview;
                  public Color tintColor;
                  public float glossiness;
                  public float metallic;
                  public float bumpScale;
            }

            [Header("Texture Presets")]
            [SerializeField] private List<TexturePreset> texturePresets = new List<TexturePreset>();
            [SerializeField] private int currentPresetIndex = 0;

            private WallPainter wallPainter;

            private void Start()
            {
                  wallPainter = FindFirstObjectByType<WallPainter>();
                  ApplyCurrentPreset();
            }

            public void ApplyTextureToAllWalls(Material material)
            {
                  if (wallPainter != null)
                  {
                        wallPainter.SetCurrentPaintMaterial(material);
                  }
            }

            public void UpdateWallColors(Color color)
            {
                  if (wallPainter != null)
                  {
                        wallPainter.SetPaintColor(color);
                  }
            }

            public void NextTexture()
            {
                  currentPresetIndex = (currentPresetIndex + 1) % texturePresets.Count;
                  ApplyCurrentPreset();
            }

            public void PreviousTexture()
            {
                  currentPresetIndex = (currentPresetIndex - 1 + texturePresets.Count) % texturePresets.Count;
                  ApplyCurrentPreset();
            }

            public void SetTextureByIndex(int index)
            {
                  if (index >= 0 && index < texturePresets.Count)
                  {
                        currentPresetIndex = index;
                        ApplyCurrentPreset();
                  }
            }

            private void ApplyCurrentPreset()
            {
                  if (texturePresets.Count == 0) return;

                  var preset = texturePresets[currentPresetIndex];
                  if (preset.material != null)
                  {
                        ApplyTextureToAllWalls(preset.material);
                        UpdateWallColors(preset.tintColor);
                  }
            }

            public void UpdateCurrentColor(Color color)
            {
                  if (texturePresets.Count == 0) return;

                  var preset = texturePresets[currentPresetIndex];
                  preset.tintColor = color;
                  texturePresets[currentPresetIndex] = preset;

                  UpdateWallColors(color);
            }

            public TexturePreset GetCurrentPreset()
            {
                  if (texturePresets.Count == 0)
                        return new TexturePreset();

                  return texturePresets[currentPresetIndex];
            }

            public int GetCurrentPresetIndex()
            {
                  return currentPresetIndex;
            }

            public int GetPresetCount()
            {
                  return texturePresets.Count;
            }
      }
}