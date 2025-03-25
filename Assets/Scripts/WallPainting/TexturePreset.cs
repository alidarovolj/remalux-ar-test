using UnityEngine;

namespace Remalux.WallPainting
{
      [System.Serializable]
      public class TexturePreset
      {
            public string name;
            public Material material;
            public Sprite preview;
            public Color tintColor = Color.white;
            public float glossiness = 0.5f;
            public float metallic = 0.0f;
            public float bumpScale = 1.0f;

            public TexturePreset(string name, Material material, Sprite preview)
            {
                  this.name = name;
                  this.material = material;
                  this.preview = preview;
            }

            public void ApplyToMaterial(Material targetMaterial)
            {
                  if (targetMaterial == null) return;

                  targetMaterial.mainTexture = material.mainTexture;
                  targetMaterial.color = tintColor;
                  targetMaterial.SetFloat("_Glossiness", glossiness);
                  targetMaterial.SetFloat("_Metallic", metallic);
                  targetMaterial.SetFloat("_BumpScale", bumpScale);
            }
      }
}