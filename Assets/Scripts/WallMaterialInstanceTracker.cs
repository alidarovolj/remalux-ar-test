using UnityEngine;

namespace Remalux.AR
{
      /// <summary>
      /// Компонент для отслеживания экземпляров материалов на стенах.
      /// Добавляется к объектам стен для управления материалами.
      /// </summary>
      public class WallMaterialInstanceTracker : MonoBehaviour
      {
            public Material originalSharedMaterial;
            public Material instancedMaterial;

            private Renderer _renderer;

            private void Awake()
            {
                  _renderer = GetComponent<Renderer>();
                  if (_renderer == null)
                  {
                        Debug.LogError($"WallMaterialInstanceTracker на объекте {gameObject.name} не может найти компонент Renderer");
                        return;
                  }

                  // Сохраняем оригинальный материал, если он еще не сохранен
                  if (originalSharedMaterial == null)
                  {
                        originalSharedMaterial = _renderer.sharedMaterial;
                        Debug.Log($"Сохранен оригинальный материал для {gameObject.name}: {(originalSharedMaterial != null ? originalSharedMaterial.name : "null")}");
                  }
            }

            /// <summary>
            /// Применяет материал к объекту, создавая его экземпляр
            /// </summary>
            /// <param name="material">Материал для применения</param>
            public void ApplyMaterial(Material material)
            {
                  if (_renderer == null)
                  {
                        _renderer = GetComponent<Renderer>();
                        if (_renderer == null)
                        {
                              Debug.LogError($"WallMaterialInstanceTracker на объекте {gameObject.name} не может найти компонент Renderer");
                              return;
                        }
                  }

                  if (material == null)
                  {
                        Debug.LogWarning($"Попытка применить null материал к объекту {gameObject.name}");
                        return;
                  }

                  // Сохраняем оригинальный материал, если он еще не сохранен
                  if (originalSharedMaterial == null)
                  {
                        originalSharedMaterial = _renderer.sharedMaterial;
                  }

                  // Создаем экземпляр материала
                  instancedMaterial = new Material(material);
                  instancedMaterial.name = $"{material.name}_Instance_{gameObject.name}";

                  // Применяем экземпляр материала
                  // Используем sharedMaterial в режиме редактора, чтобы избежать утечек
                  if (Application.isPlaying)
                  {
                        _renderer.material = instancedMaterial;
                  }
                  else
                  {
                        _renderer.sharedMaterial = instancedMaterial;
                  }

                  Debug.Log($"Применен материал {instancedMaterial.name} к объекту {gameObject.name}");
            }

            /// <summary>
            /// Восстанавливает оригинальный материал
            /// </summary>
            public void RestoreOriginalMaterial()
            {
                  if (_renderer == null)
                  {
                        _renderer = GetComponent<Renderer>();
                        if (_renderer == null)
                        {
                              Debug.LogError($"WallMaterialInstanceTracker на объекте {gameObject.name} не может найти компонент Renderer");
                              return;
                        }
                  }

                  if (originalSharedMaterial != null)
                  {
                        _renderer.sharedMaterial = originalSharedMaterial;
                        Debug.Log($"Восстановлен оригинальный материал {originalSharedMaterial.name} для объекта {gameObject.name}");
                  }
                  else
                  {
                        Debug.LogWarning($"Не удалось восстановить оригинальный материал для объекта {gameObject.name}, так как он не был сохранен");
                  }
            }

            /// <summary>
            /// Обновляет экземпляр материала, если он существует
            /// </summary>
            public void UpdateMaterialInstance()
            {
                  if (_renderer == null || instancedMaterial == null)
                        return;

                  // Проверяем, использует ли рендерер экземпляр материала
                  if (Application.isPlaying)
                  {
                        // В режиме игры используем material
                        if (_renderer.material != instancedMaterial)
                        {
                              _renderer.material = instancedMaterial;
                              Debug.Log($"Обновлен экземпляр материала для объекта {gameObject.name}");
                        }
                  }
                  else
                  {
                        // В режиме редактора используем sharedMaterial, чтобы избежать утечек
                        if (_renderer.sharedMaterial != instancedMaterial)
                        {
                              _renderer.sharedMaterial = instancedMaterial;
                              Debug.Log($"Обновлен экземпляр материала для объекта {gameObject.name} (режим редактора)");
                        }
                  }
            }
      }
}