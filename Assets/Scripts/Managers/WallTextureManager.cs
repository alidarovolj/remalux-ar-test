using UnityEngine;
using System.Collections.Generic;

namespace Remalux.AR
{
    [System.Serializable]
    public class WallTextureSettings
    {
        public float roughness = 0.5f;
        public float metallic = 0.0f;
        public float normalStrength = 1.0f;
        public Vector2 tiling = Vector2.one;
        public Vector2 offset = Vector2.zero;
    }

    public class WallTextureManager : MonoBehaviour
    {
        [Header("Материалы")]
        [SerializeField] private Material baseMaterial;
        [SerializeField] private Shader wallShader;
        
        [Header("Текстуры")]
        [SerializeField] private Texture2D defaultNormalMap;
        [SerializeField] private Texture2D defaultRoughnessMap;
        
        [Header("Настройки")]
        [SerializeField] private float defaultRoughness = 0.5f;
        [SerializeField] private float defaultMetallic = 0.0f;
        [SerializeField] private float defaultNormalStrength = 1.0f;
        
        private Dictionary<GameObject, Material> wallMaterials = new Dictionary<GameObject, Material>();
        private Dictionary<GameObject, WallTextureSettings> wallSettings = new Dictionary<GameObject, WallTextureSettings>();

        public void ApplyColor(GameObject wall, Color color, bool withTransition = true)
        {
            if (!wallMaterials.TryGetValue(wall, out Material material))
            {
                material = CreateWallMaterial();
                wallMaterials[wall] = material;
                wall.GetComponent<Renderer>().material = material;
            }

            if (withTransition)
            {
                StartCoroutine(TransitionColor(material, color));
            }
            else
            {
                material.SetColor("_BaseColor", color);
            }
        }

        public void UpdateTextureSettings(GameObject wall, WallTextureSettings settings)
        {
            if (!wallMaterials.TryGetValue(wall, out Material material))
            {
                material = CreateWallMaterial();
                wallMaterials[wall] = material;
                wall.GetComponent<Renderer>().material = material;
            }

            material.SetFloat("_Roughness", settings.roughness);
            material.SetFloat("_Metallic", settings.metallic);
            material.SetFloat("_NormalStrength", settings.normalStrength);
            material.SetTextureScale("_BaseMap", settings.tiling);
            material.SetTextureOffset("_BaseMap", settings.offset);

            wallSettings[wall] = settings;
        }

        private Material CreateWallMaterial()
        {
            Material material = new Material(wallShader);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICGLOSSMAP");
            
            // Устанавливаем базовые текстуры
            if (defaultNormalMap != null)
                material.SetTexture("_BumpMap", defaultNormalMap);
            
            if (defaultRoughnessMap != null)
                material.SetTexture("_MetallicGlossMap", defaultRoughnessMap);
            
            // Устанавливаем базовые значения
            material.SetFloat("_Roughness", defaultRoughness);
            material.SetFloat("_Metallic", defaultMetallic);
            material.SetFloat("_NormalStrength", defaultNormalStrength);
            
            return material;
        }

        private System.Collections.IEnumerator TransitionColor(Material material, Color targetColor)
        {
            Color startColor = material.GetColor("_BaseColor");
            float elapsedTime = 0;
            float transitionDuration = 0.5f;

            while (elapsedTime < transitionDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / transitionDuration;
                material.SetColor("_BaseColor", Color.Lerp(startColor, targetColor, t));
                yield return null;
            }

            material.SetColor("_BaseColor", targetColor);
        }

        public void ApplyTexture(GameObject wall, Texture2D albedoMap, Texture2D normalMap = null, Texture2D roughnessMap = null)
        {
            if (!wallMaterials.TryGetValue(wall, out Material material))
            {
                material = CreateWallMaterial();
                wallMaterials[wall] = material;
                wall.GetComponent<Renderer>().material = material;
            }

            if (albedoMap != null)
                material.SetTexture("_BaseMap", albedoMap);
            
            if (normalMap != null)
                material.SetTexture("_BumpMap", normalMap);
            
            if (roughnessMap != null)
                material.SetTexture("_MetallicGlossMap", roughnessMap);
        }

        public void ApplyPaintEffect(GameObject wall, float wetness = 0.5f, float glossiness = 0.7f)
        {
            if (!wallMaterials.TryGetValue(wall, out Material material))
                return;

            material.SetFloat("_Wetness", wetness);
            material.SetFloat("_Glossiness", glossiness);

            // Запускаем эффект высыхания
            StartCoroutine(DryPaintEffect(material));
        }

        private System.Collections.IEnumerator DryPaintEffect(Material material)
        {
            float elapsedTime = 0;
            float dryingDuration = 5f;
            float startWetness = material.GetFloat("_Wetness");
            float startGlossiness = material.GetFloat("_Glossiness");

            while (elapsedTime < dryingDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / dryingDuration;
                
                material.SetFloat("_Wetness", Mathf.Lerp(startWetness, 0f, t));
                material.SetFloat("_Glossiness", Mathf.Lerp(startGlossiness, defaultRoughness, t));
                
                yield return null;
            }

            material.SetFloat("_Wetness", 0f);
            material.SetFloat("_Glossiness", defaultRoughness);
        }

        public WallTextureSettings GetWallSettings(GameObject wall)
        {
            if (wallSettings.TryGetValue(wall, out WallTextureSettings settings))
                return settings;

            return new WallTextureSettings
            {
                roughness = defaultRoughness,
                metallic = defaultMetallic,
                normalStrength = defaultNormalStrength,
                tiling = Vector2.one,
                offset = Vector2.zero
            };
        }

        private void OnDestroy()
        {
            foreach (var material in wallMaterials.Values)
            {
                if (material != null)
                    Destroy(material);
            }
        }
    }
} 