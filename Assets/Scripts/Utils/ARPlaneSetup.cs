using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System.Collections;

namespace Remalux.AR
{
      /// <summary>
      /// Класс для настройки и инициализации системы AR плоскостей
      /// </summary>
      [RequireComponent(typeof(ARPlaneMaterialManager))]
      public class ARPlaneSetup : MonoBehaviour
      {
            [Header("AR компоненты")]
            [Tooltip("Ссылка на AR Plane Manager")]
            public ARPlaneManager planeManager;

            [Tooltip("Ссылка на AR Plane Visibility Controller")]
            public ARPlaneVisibilityController planeVisibilityController;

            [Header("Настройки")]
            [Tooltip("Автоматически инициализировать компоненты при старте")]
            public bool autoInitialize = true;

            [Tooltip("Интервал проверки плоскостей (секунды)")]
            public float checkInterval = 0.5f;

            private ARPlaneMaterialManager materialManager;

            private void Awake()
            {
                  // Получаем компонент менеджера материалов
                  materialManager = GetComponent<ARPlaneMaterialManager>();
                  if (materialManager == null)
                  {
                        materialManager = gameObject.AddComponent<ARPlaneMaterialManager>();
                  }

                  // Находим компоненты, если не заданы
                  if (planeManager == null)
                  {
                        planeManager = UnityEngine.Object.FindFirstObjectByType<ARPlaneManager>();
                  }

                  if (planeVisibilityController == null)
                  {
                        planeVisibilityController = UnityEngine.Object.FindFirstObjectByType<ARPlaneVisibilityController>();
                        if (planeVisibilityController == null && planeManager != null)
                        {
                              // Создаем контроллер видимости, если он не существует
                              GameObject visController = new GameObject("AR Plane Visibility Controller");
                              planeVisibilityController = visController.AddComponent<ARPlaneVisibilityController>();
                              Debug.Log("Создан AR Plane Visibility Controller");
                        }
                  }
            }

            private void Start()
            {
                  if (autoInitialize)
                  {
                        Initialize();
                  }
            }

            /// <summary>
            /// Инициализирует все компоненты системы AR плоскостей
            /// </summary>
            public void Initialize()
            {
                  // Инициализируем менеджер материалов
                  if (materialManager != null)
                  {
                        materialManager.planeManager = planeManager;
                        materialManager.CreateMaterials();
                        materialManager.ApplyMaterialsToPlaneManager();
                  }

                  // Инициализируем контроллер видимости
                  if (planeVisibilityController != null && planeManager != null)
                  {
                        // Устанавливаем ссылки и материалы
                        planeVisibilityController.planeManager = planeManager;

                        if (materialManager != null)
                        {
                              planeVisibilityController.wallMaterial = materialManager.wallMaterial;
                              planeVisibilityController.floorMaterial = materialManager.floorMaterial;
                        }

                        // Запускаем проверку видимости плоскостей
                        StartCoroutine(CheckPlanesCoroutine());
                  }

                  // Конфигурируем ARPlaneManager
                  if (planeManager != null)
                  {
                        // Убеждаемся, что плоскости настроены на обнаружение вертикальных и горизонтальных поверхностей
                        planeManager.requestedDetectionMode = UnityEngine.XR.ARSubsystems.PlaneDetectionMode.Vertical |
                                                              UnityEngine.XR.ARSubsystems.PlaneDetectionMode.Horizontal;

                        // Убеждаемся, что компонент включен
                        if (!planeManager.enabled)
                        {
                              planeManager.enabled = true;
                              Debug.Log("ARPlaneManager был включен");
                        }

                        // Исправляем проблему с материалом из пакета
                        if (planeManager.planePrefab != null)
                        {
                              MeshRenderer renderer = planeManager.planePrefab.GetComponent<MeshRenderer>();
                              if (renderer != null && materialManager != null)
                              {
                                    // Просто заменяем материал на наш локальный, если он существует
                                    if (renderer.sharedMaterial != null && materialManager.floorMaterial != null)
                                    {
                                          Debug.Log("Заменяем материал AR плоскости на локальный материал");
                                          renderer.sharedMaterial = materialManager.floorMaterial;
                                    }
                              }
                        }
                  }

                  Debug.Log("Система AR плоскостей инициализирована");
            }

            /// <summary>
            /// Корутина для периодической проверки состояния плоскостей
            /// </summary>
            private IEnumerator CheckPlanesCoroutine()
            {
                  yield return new WaitForSeconds(1f); // Начальная задержка для инициализации AR

                  while (true)
                  {
                        if (planeManager != null && planeManager.enabled && planeManager.trackables.count > 0)
                        {
                              int activatedCount = 0;

                              // Проверяем все плоскости
                              foreach (ARPlane plane in planeManager.trackables)
                              {
                                    if (plane != null)
                                    {
                                          // Активируем неактивные плоскости
                                          if (!plane.gameObject.activeInHierarchy)
                                          {
                                                try
                                                {
                                                      plane.gameObject.SetActive(true);
                                                      activatedCount++;
                                                }
                                                catch (System.Exception e)
                                                {
                                                      Debug.LogError($"Ошибка при активации плоскости: {e.Message}");
                                                }
                                          }

                                          // Если есть контроллер видимости, обновляем плоскость через него
                                          if (planeVisibilityController != null)
                                          {
                                                planeVisibilityController.UpdatePlaneVisibility(plane);
                                          }
                                    }
                              }

                              if (activatedCount > 0)
                              {
                                    Debug.Log($"Активировано {activatedCount} плоскостей");
                              }
                        }

                        yield return new WaitForSeconds(checkInterval);
                  }
            }
      }
}