using UnityEngine;

namespace Remalux.WallPainting
{
      public class WallMaterialInstanceTracker : MonoBehaviour
      {
            public Material OriginalSharedMaterial { get; set; }
            public Material InstancedMaterial { get; set; }

            private void Awake()
            {
                  // Сохраняем оригинальный материал
                  Renderer renderer = GetComponent<Renderer>();
                  if (renderer != null)
                  {
                        OriginalSharedMaterial = renderer.sharedMaterial;
                        // Создаем инстанс материала
                        InstancedMaterial = new Material(OriginalSharedMaterial);
                        renderer.material = InstancedMaterial;
                  }
            }

            public void ApplyMaterial(Material material)
            {
                  if (material == null)
                        return;

                  Renderer renderer = GetComponent<Renderer>();
                  if (renderer == null)
                        return;

                  // Create a new instance of the material
                  Material instanceMaterial = new Material(material);
                  instanceMaterial.name = $"{material.name}_Instance_{gameObject.name}";

                  // Apply the instance material
                  renderer.material = instanceMaterial;
                  InstancedMaterial = instanceMaterial;
            }

            public void ResetToOriginalMaterial()
            {
                  if (OriginalSharedMaterial == null)
                        return;

                  Renderer renderer = GetComponent<Renderer>();
                  if (renderer == null)
                        return;

                  // Create a new instance of the original material
                  Material instanceMaterial = new Material(OriginalSharedMaterial);
                  instanceMaterial.name = $"{OriginalSharedMaterial.name}_Instance_{gameObject.name}";

                  // Apply the instance material
                  renderer.material = instanceMaterial;
                  InstancedMaterial = instanceMaterial;
            }

            public void UpdateMaterialInstance()
            {
                  if (OriginalSharedMaterial == null)
                        return;

                  Renderer renderer = GetComponent<Renderer>();
                  if (renderer == null)
                        return;

                  // Create a new instance of the original material
                  Material instanceMaterial = new Material(OriginalSharedMaterial);
                  instanceMaterial.name = $"{OriginalSharedMaterial.name}_Instance_{gameObject.name}";

                  // Apply the instance material
                  renderer.material = instanceMaterial;
                  InstancedMaterial = instanceMaterial;
            }

            private void OnDestroy()
            {
                  // Очищаем инстанс материала
                  if (InstancedMaterial != null)
                  {
                        Destroy(InstancedMaterial);
                  }
            }
      }
}