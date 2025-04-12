using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System;
using System.Collections;

namespace Remalux.AR
{
      /// <summary>
      /// Вспомогательный класс для гарантированной активации AR плоскостей
      /// </summary>
      public static class ARPlaneActivator
      {
            /// <summary>
            /// Гарантирует, что плоскость активна в иерархии
            /// </summary>
            public static void EnsurePlaneIsActive(ARPlane plane, MonoBehaviour context)
            {
                  if (plane == null) return;

                  if (!plane.gameObject.activeInHierarchy)
                  {
                        // Принудительно активируем gameObject
                        try
                        {
                              plane.gameObject.SetActive(true);

                              // Запускаем корутину для повторной попытки активации
                              if (context != null)
                              {
                                    context.StartCoroutine(DelayedActivationCheck(plane));
                              }

                              Debug.Log($"ARPlaneActivator: Принудительно активирована плоскость: {plane.trackableId}");
                        }
                        catch (Exception e)
                        {
                              Debug.LogError($"ARPlaneActivator: Ошибка при активации плоскости {plane.trackableId}: {e.Message}");
                        }
                  }

                  // Проверка компонентов
                  EnsurePlaneComponents(plane);
            }

            /// <summary>
            /// Проверяет наличие необходимых компонентов на плоскости
            /// </summary>
            public static void EnsurePlaneComponents(ARPlane plane)
            {
                  if (plane == null) return;

                  // Проверка и добавление MeshRenderer
                  MeshRenderer renderer = plane.GetComponent<MeshRenderer>();
                  if (renderer == null)
                  {
                        renderer = plane.gameObject.AddComponent<MeshRenderer>();
                        Debug.Log($"ARPlaneActivator: Добавлен MeshRenderer к плоскости {plane.trackableId}");
                  }

                  if (!renderer.enabled)
                  {
                        renderer.enabled = true;
                  }

                  // Проверка и добавление MeshFilter
                  MeshFilter meshFilter = plane.GetComponent<MeshFilter>();
                  if (meshFilter == null)
                  {
                        meshFilter = plane.gameObject.AddComponent<MeshFilter>();
                        Debug.Log($"ARPlaneActivator: Добавлен MeshFilter к плоскости {plane.trackableId}");
                  }

                  // Проверка и добавление MeshCollider
                  MeshCollider collider = plane.GetComponent<MeshCollider>();
                  if (collider == null)
                  {
                        collider = plane.gameObject.AddComponent<MeshCollider>();

                        // Установка меша для коллайдера
                        if (meshFilter != null && meshFilter.sharedMesh != null)
                        {
                              collider.sharedMesh = meshFilter.sharedMesh;
                        }

                        Debug.Log($"ARPlaneActivator: Добавлен MeshCollider к плоскости {plane.trackableId}");
                  }
            }

            /// <summary>
            /// Корутина для проверки активации плоскости через задержку
            /// </summary>
            private static IEnumerator DelayedActivationCheck(ARPlane plane)
            {
                  yield return new WaitForSeconds(0.1f);

                  if (plane != null && !plane.gameObject.activeInHierarchy)
                  {
                        // Пробуем еще раз активировать
                        plane.gameObject.SetActive(true);
                        Debug.Log($"ARPlaneActivator: Повторная попытка активации плоскости: {plane.trackableId}");

                        // Проверяем родителя
                        if (plane.transform.parent != null)
                        {
                              plane.transform.parent.gameObject.SetActive(true);
                              Debug.Log($"ARPlaneActivator: Активирован родитель плоскости: {plane.transform.parent.name}");
                        }
                  }
            }
      }
}