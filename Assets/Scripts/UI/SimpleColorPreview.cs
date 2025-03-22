using UnityEngine;

namespace Remalux.AR
{
    /// <summary>
    /// Компонент для предпросмотра цвета на стене
    /// </summary>
    public class SimpleColorPreview : MonoBehaviour
    {
        [SerializeField] private Renderer previewRenderer;
        [SerializeField] private float previewSize = 0.2f;

        private Material previewMaterial;
        private bool initialized = false;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (initialized)
                return;

            if (previewRenderer == null)
            {
                previewRenderer = GetComponent<Renderer>();
                if (previewRenderer == null)
                {
                    // Создаем простой объект для предпросмотра
                    GameObject previewObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    previewObj.transform.SetParent(transform, false);
                    previewObj.transform.localPosition = Vector3.zero;
                    previewObj.transform.localScale = new Vector3(previewSize, previewSize, previewSize);
                    
                    // Удаляем коллайдер, чтобы он не мешал
                    Destroy(previewObj.GetComponent<Collider>());
                    
                    previewRenderer = previewObj.GetComponent<Renderer>();
                }
            }

            // Создаем материал для предпросмотра
            previewMaterial = new Material(Shader.Find("Standard"));
            previewMaterial.name = "PreviewMaterial";
            previewRenderer.material = previewMaterial;

            initialized = true;
        }

        private void Update()
        {
            // Плавное вращение для визуального эффекта
            transform.Rotate(Vector3.up, Time.deltaTime * 15f);
        }

        /// <summary>
        /// Установить материал для предпросмотра
        /// </summary>
        public void SetMaterial(Material material)
        {
            if (!initialized)
                Initialize();

            if (material != null && previewMaterial != null)
            {
                // Копируем свойства материала
                previewMaterial.CopyPropertiesFromMaterial(material);
                
                // Делаем материал немного прозрачным для эффекта предпросмотра
                if (previewMaterial.HasProperty("_Color"))
                {
                    Color color = previewMaterial.color;
                    color.a = 0.8f;
                    previewMaterial.color = color;
                }
                
                // Включаем прозрачность
                previewMaterial.SetFloat("_Mode", 3); // Transparent mode
                previewMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                previewMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                previewMaterial.SetInt("_ZWrite", 0);
                previewMaterial.DisableKeyword("_ALPHATEST_ON");
                previewMaterial.EnableKeyword("_ALPHABLEND_ON");
                previewMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                previewMaterial.renderQueue = 3000;
            }
        }

        /// <summary>
        /// Установить размер предпросмотра
        /// </summary>
        public void SetPreviewSize(float size)
        {
            previewSize = size;
            
            if (previewRenderer != null && previewRenderer.transform != transform)
            {
                previewRenderer.transform.localScale = new Vector3(previewSize, previewSize, previewSize);
            }
        }
        
        /// <summary>
        /// Устанавливает рендерер для предпросмотра
        /// </summary>
        public void SetPreviewRenderer(Renderer renderer)
        {
            previewRenderer = renderer;
            
            if (previewRenderer != null && previewMaterial != null)
            {
                previewRenderer.material = previewMaterial;
            }
        }
    }
}