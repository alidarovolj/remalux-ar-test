using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Remalux.AR
{
      /// <summary>
      /// Класс для управления материалами AR плоскостей
      /// </summary>
      public class ARPlaneMaterialManager : MonoBehaviour
      {
            [Header("Материалы")]
            [Tooltip("Материал для стен (вертикальных плоскостей)")]
            public Material wallMaterial;

            [Tooltip("Материал для пола (горизонтальных плоскостей)")]
            public Material floorMaterial;

            [Tooltip("Материал для выделения стен")]
            public Material wallHighlightMaterial;

            [Range(0.1f, 1.0f)]
            [Tooltip("Прозрачность материалов плоскостей")]
            public float planeAlpha = 0.7f;

            [Header("Ссылки на компоненты")]
            [Tooltip("AR Plane Manager")]
            public ARPlaneManager planeManager;

            private void Awake()
            {
                  // Создаем материалы при запуске, если они не назначены
                  CreateMaterials();
            }

            private void Start()
            {
                  // Применяем материалы к AR Plane Manager
                  ApplyMaterialsToPlaneManager();
            }

            /// <summary>
            /// Создает материалы, если они не назначены
            /// </summary>
            public void CreateMaterials()
            {
                  // Создаем материал для стен
                  if (wallMaterial == null)
                  {
                        wallMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                        wallMaterial.color = new Color(1.0f, 0.2f, 0.2f, planeAlpha);
                        wallMaterial.enableInstancing = true;
                        wallMaterial.SetFloat("_ZWrite", 1);
                        wallMaterial.SetFloat("_Surface", 1); // прозрачный

                        // Добавляем эмиссию для лучшей видимости
                        wallMaterial.EnableKeyword("_EMISSION");
                        wallMaterial.SetColor("_EmissionColor", new Color(1.0f, 0.3f, 0.3f, 1.0f));
                        wallMaterial.SetFloat("_EmissionIntensity", 0.8f);

                        // Сохраняем материал в проект
#if UNITY_EDITOR
                if (!System.IO.Directory.Exists("Assets/Materials"))
                {
                    System.IO.Directory.CreateDirectory("Assets/Materials");
                }
                UnityEditor.AssetDatabase.CreateAsset(wallMaterial, "Assets/Materials/CustomWallMaterial.mat");
                UnityEditor.AssetDatabase.SaveAssets();
#endif

                        Debug.Log("Материал стены создан и сохранен в проект");
                  }

                  // Создаем материал для пола
                  if (floorMaterial == null)
                  {
                        floorMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                        floorMaterial.color = new Color(0.2f, 1.0f, 0.2f, planeAlpha);
                        floorMaterial.enableInstancing = true;
                        floorMaterial.SetFloat("_ZWrite", 1);
                        floorMaterial.SetFloat("_Surface", 1); // прозрачный

                        // Сохраняем материал в проект
#if UNITY_EDITOR
                if (!System.IO.Directory.Exists("Assets/Materials"))
                {
                    System.IO.Directory.CreateDirectory("Assets/Materials");
                }
                UnityEditor.AssetDatabase.CreateAsset(floorMaterial, "Assets/Materials/CustomFloorMaterial.mat");
                UnityEditor.AssetDatabase.SaveAssets();
#endif

                        Debug.Log("Материал пола создан и сохранен в проект");
                  }

                  // Создаем материал для выделения стен
                  if (wallHighlightMaterial == null)
                  {
                        wallHighlightMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                        wallHighlightMaterial.color = new Color(1.0f, 0.9f, 0.0f, 0.9f);
                        wallHighlightMaterial.enableInstancing = true;
                        wallHighlightMaterial.SetFloat("_ZWrite", 1);
                        wallHighlightMaterial.SetFloat("_Surface", 1); // прозрачный

                        // Добавляем эмиссию для лучшей видимости
                        wallHighlightMaterial.EnableKeyword("_EMISSION");
                        wallHighlightMaterial.SetColor("_EmissionColor", new Color(1.0f, 0.9f, 0.0f, 1.0f));
                        wallHighlightMaterial.SetFloat("_EmissionIntensity", 1.5f);

                        // Сохраняем материал в проект
#if UNITY_EDITOR
                if (!System.IO.Directory.Exists("Assets/Materials"))
                {
                    System.IO.Directory.CreateDirectory("Assets/Materials");
                }
                UnityEditor.AssetDatabase.CreateAsset(wallHighlightMaterial, "Assets/Materials/WallHighlightMaterial.mat");
                UnityEditor.AssetDatabase.SaveAssets();
#endif

                        Debug.Log("Материал выделения стены создан и сохранен в проект");
                  }
            }

            /// <summary>
            /// Применяет материалы к ARPlaneManager
            /// </summary>
            public void ApplyMaterialsToPlaneManager()
            {
                  if (planeManager == null)
                  {
                        planeManager = UnityEngine.Object.FindFirstObjectByType<ARPlaneManager>();
                        if (planeManager == null)
                        {
                              Debug.LogError("ARPlaneManager не найден");
                              return;
                        }
                  }

                  // Если у ARPlaneManager есть planePrefab, используем его
                  if (planeManager.planePrefab != null)
                  {
                        // Получаем компонент MeshRenderer планы
                        MeshRenderer renderer = planeManager.planePrefab.GetComponent<MeshRenderer>();
                        if (renderer != null)
                        {
                              // Создаем новый материал для префаба плоскости, если нужно
                              Material customPlaneMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                              customPlaneMaterial.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);

                              // Сохраняем материал в проект
#if UNITY_EDITOR
                    if (!System.IO.Directory.Exists("Assets/Materials"))
                    {
                        System.IO.Directory.CreateDirectory("Assets/Materials");
                    }
                    UnityEditor.AssetDatabase.CreateAsset(customPlaneMaterial, "Assets/Materials/CustomARPlaneMaterial.mat");
                    UnityEditor.AssetDatabase.SaveAssets();
#endif

                              // Назначаем материал префабу плоскости
                              renderer.material = customPlaneMaterial;

                              Debug.Log("Материал применен к префабу плоскости AR");
                        }
                  }
            }

            /// <summary>
            /// Применяет материал к плоскости в зависимости от ее типа
            /// </summary>
            public Material GetMaterialForPlane(ARPlane plane)
            {
                  if (plane == null) return null;

                  bool isVertical = IsVerticalPlane(plane);
                  return isVertical ? wallMaterial : floorMaterial;
            }

            /// <summary>
            /// Определяет, является ли плоскость вертикальной (стеной)
            /// </summary>
            public static bool IsVerticalPlane(ARPlane plane)
            {
                  if (plane == null) return false;

                  // Проверяем нормаль плоскости для определения вертикальности
                  Vector3 normal = plane.normal;
                  float dotWithUp = Vector3.Dot(normal, Vector3.up);

                  // Если угол между нормалью и вектором вверх близок к 90 градусам (dotProduct близок к 0),
                  // то это вертикальная плоскость (стена)
                  return Mathf.Abs(dotWithUp) < 0.1f;
            }
      }
}