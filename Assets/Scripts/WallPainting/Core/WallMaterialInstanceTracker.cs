using UnityEngine;

namespace Remalux.AR
{
      public class WallMaterialInstanceTracker : MonoBehaviour
      {
            public Material OriginalSharedMaterial { get; set; }
            protected Material _instancedMaterial;
            public Material InstancedMaterial => _instancedMaterial;

            private Renderer wallRenderer;

            private void Awake()
            {
                  wallRenderer = GetComponent<Renderer>();
                  if (wallRenderer == null)
                  {
                        Debug.LogError($"WallMaterialInstanceTracker: Не найден компонент Renderer на объекте {gameObject.name}");
                        enabled = false;
                        return;
                  }

                  // Сохраняем оригинальный материал, если еще не сохранен
                  if (OriginalSharedMaterial == null)
                  {
                        OriginalSharedMaterial = wallRenderer.sharedMaterial;
                  }
            }

            public void SetInstancedMaterial(Material material, bool applyToRenderer = true)
            {
                  if (material == null)
                  {
                        Debug.LogWarning("WallMaterialInstanceTracker: Попытка установить null материал");
                        return;
                  }

                  // Уничтожаем старый экземпляр материала через MemoryManager
                  if (_instancedMaterial != null)
                  {
                        MemoryManager.Instance.ReleaseMaterialInstance(_instancedMaterial);
                        _instancedMaterial = null;
                  }

                  // Создаем новый экземпляр материала через MemoryManager
                  string instanceName = $"{material.name}_Instance_{gameObject.name}";
                  _instancedMaterial = MemoryManager.Instance.CreateMaterialInstance(material, instanceName);

                  if (applyToRenderer && wallRenderer != null)
                  {
                        wallRenderer.material = _instancedMaterial;
                  }
            }

            public void ApplyMaterial(Material newMaterial)
            {
                  if (newMaterial == null)
                  {
                        Debug.LogWarning("WallMaterialInstanceTracker: Попытка применить null материал");
                        return;
                  }

                  if (wallRenderer == null)
                  {
                        Debug.LogError("WallMaterialInstanceTracker: Renderer не найден");
                        return;
                  }

                  try
                  {
                        SetInstancedMaterial(newMaterial, true);
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"WallMaterialInstanceTracker: Ошибка при применении материала: {e.Message}");
                  }
            }

            public void UpdateMaterialInstance()
            {
                  if (wallRenderer == null || _instancedMaterial == null)
                        return;

                  try
                  {
                        // Создаем новый экземпляр через MemoryManager
                        string instanceName = $"{_instancedMaterial.name}_Updated";
                        Material newInstance = MemoryManager.Instance.CreateMaterialInstance(_instancedMaterial, instanceName);

                        // Уничтожаем старый экземпляр
                        MemoryManager.Instance.ReleaseMaterialInstance(_instancedMaterial);

                        // Обновляем ссылки
                        _instancedMaterial = newInstance;
                        wallRenderer.material = _instancedMaterial;

                        Debug.Log($"WallMaterialInstanceTracker: Материал обновлен для {gameObject.name}");
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"WallMaterialInstanceTracker: Ошибка при обновлении материала: {e.Message}");
                  }
            }

            public void RestoreOriginalMaterial()
            {
                  if (wallRenderer == null || OriginalSharedMaterial == null)
                        return;

                  // Уничтожаем экземпляр материала через MemoryManager
                  if (_instancedMaterial != null)
                  {
                        MemoryManager.Instance.ReleaseMaterialInstance(_instancedMaterial);
                        _instancedMaterial = null;
                  }

                  // Восстанавливаем оригинальный материал
                  wallRenderer.sharedMaterial = OriginalSharedMaterial;
            }

            // Алиас для обратной совместимости
            public void ResetToOriginalMaterial()
            {
                  RestoreOriginalMaterial();
            }

            private void OnDestroy()
            {
                  // Уничтожаем экземпляр материала через MemoryManager
                  if (_instancedMaterial != null)
                  {
                        MemoryManager.Instance.ReleaseMaterialInstance(_instancedMaterial);
                        _instancedMaterial = null;
                  }
            }
      }
}