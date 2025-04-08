using UnityEngine;
using System.Collections.Generic;

namespace Remalux.WallPainting
{
      public class TextureManager : MonoBehaviour
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

            private RoomManager roomManager;

            private void Start()
            {
                  roomManager = FindFirstObjectByType<RoomManager>();
                  if (roomManager == null)
                  {
                        Debug.LogError("TextureManager: RoomManager не найден!");
                        enabled = false;
                        return;
                  }

                  // Create a default blue material if no presets exist
                  if (texturePresets.Count == 0)
                  {
                        Material blueMaterial = new Material(Shader.Find("Standard"));
                        blueMaterial.color = Color.blue;

                        TexturePreset bluePreset = new TexturePreset
                        {
                              name = "Default Blue",
                              material = blueMaterial,
                              tintColor = Color.blue,
                              glossiness = 0.5f,
                              metallic = 0.0f
                        };

                        texturePresets.Add(bluePreset);
                        Debug.Log("TextureManager: Created default blue material preset");
                  }

                  if (texturePresets.Count > 0)
                  {
                        ApplyCurrentPreset();
                  }
            }

            public void NextTexture()
            {
                  if (texturePresets.Count == 0) return;
                  currentPresetIndex = (currentPresetIndex + 1) % texturePresets.Count;
                  ApplyCurrentPreset();
            }

            public void PreviousTexture()
            {
                  if (texturePresets.Count == 0) return;
                  currentPresetIndex = (currentPresetIndex - 1 + texturePresets.Count) % texturePresets.Count;
                  ApplyCurrentPreset();
            }

            public void SetTextureByIndex(int index)
            {
                  if (index < 0 || index >= texturePresets.Count) return;
                  currentPresetIndex = index;
                  ApplyCurrentPreset();
            }

            private void ApplyCurrentPreset()
            {
                  if (currentPresetIndex < 0 || currentPresetIndex >= texturePresets.Count) return;

                  TexturePreset preset = texturePresets[currentPresetIndex];
                  if (preset.material != null)
                  {
                        roomManager.SetDefaultMaterial(preset.material);
                        UpdateCurrentColor(preset.tintColor);
                  }
            }

            public void UpdateCurrentColor(Color color)
            {
                  if (currentPresetIndex < 0 || currentPresetIndex >= texturePresets.Count) return;

                  TexturePreset preset = texturePresets[currentPresetIndex];
                  preset.tintColor = color;
                  texturePresets[currentPresetIndex] = preset;

                  if (preset.material != null)
                  {
                        preset.material.color = color;
                  }
            }

            public TexturePreset GetCurrentPreset()
            {
                  if (currentPresetIndex < 0 || currentPresetIndex >= texturePresets.Count)
                        return default;
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