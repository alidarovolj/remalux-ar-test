using UnityEngine;

namespace Remalux.AR
{
      /// <summary>
      /// Простой компонент для предпросмотра цвета
      /// </summary>
      public class SimpleColorPreview : MonoBehaviour
      {
            [SerializeField] private Renderer previewRenderer;

            private void Awake()
            {
                  if (previewRenderer == null)
                        previewRenderer = GetComponent<Renderer>();
            }

            public void SetColor(Color color)
            {
                  if (previewRenderer != null && previewRenderer.material != null)
                        previewRenderer.material.color = color;
            }

            public void SetMaterial(Material material)
            {
                  if (previewRenderer != null)
                        previewRenderer.material = material;
            }
      }
}