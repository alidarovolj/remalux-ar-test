using UnityEngine;
using System.Collections.Generic;

namespace Remalux.AR
{
      public class WallPainter : MonoBehaviour
      {
            [Header("Настройки покраски")]
            public Material[] availablePaints;
            public Material defaultMaterial;
            public Camera mainCamera;
            public LayerMask wallLayerMask;

            [Header("Настройки визуализации")]
            public bool showColorPreview = true;
            public GameObject colorPreviewPrefab;

            // Список стен для покраски
            private List<GameObject> walls = new List<GameObject>();

            // Текущий выбранный материал для покраски
            private Material currentPaintMaterial;

            // Словарь для хранения оригинальных материалов стен
            private Dictionary<GameObject, Material> originalMaterials = new Dictionary<GameObject, Material>();

            // Объект предпросмотра цвета
            private GameObject colorPreview;
            private SimpleColorPreview previewComponent;

            private void Start()
            {
                  if (mainCamera == null)
                  {
                        mainCamera = Camera.main;
                        if (mainCamera == null)
                        {
                              Debug.LogWarning("Не найдена основная камера. Система покраски может работать некорректно.");
                        }
                  }

                  // Проверяем наличие материалов
                  if (availablePaints == null || availablePaints.Length == 0)
                  {
                        Debug.LogWarning("Не заданы материалы для покраски в WallPainter. Система покраски не будет работать корректно.");

                        // Пытаемся получить материалы из WallPaintingManager
                        WallPaintingManager manager = FindObjectOfType<WallPaintingManager>();
                        if (manager != null && manager.paintMaterials != null && manager.paintMaterials.Length > 0)
                        {
                              Debug.Log($"WallPainter: получено {manager.paintMaterials.Length} материалов из WallPaintingManager");
                              availablePaints = manager.paintMaterials;
                        }
                        else
                        {
                              // Создаем пустой массив, чтобы избежать NullReferenceException
                              availablePaints = new Material[0];
                        }
                  }
                  else
                  {
                        Debug.Log($"WallPainter: доступно {availablePaints.Length} материалов для покраски");
                  }

                  // Проверяем наличие материала по умолчанию
                  if (defaultMaterial == null && availablePaints.Length > 0)
                  {
                        defaultMaterial = availablePaints[0];
                        Debug.Log($"WallPainter: установлен материал по умолчанию: {defaultMaterial.name}");
                  }
                  else if (defaultMaterial == null)
                  {
                        Debug.LogWarning("Не задан материал для покраски по умолчанию и нет доступных материалов. Система покраски не будет работать корректно.");

                        // Создаем стандартный материал, чтобы избежать ошибок
                        defaultMaterial = new Material(Shader.Find("Standard"));
                        defaultMaterial.name = "DefaultMaterial";
                        defaultMaterial.color = Color.white;
                        Debug.Log("WallPainter: создан стандартный материал по умолчанию");
                  }

                  // Устанавливаем текущий материал
                  currentPaintMaterial = defaultMaterial;
                  Debug.Log($"WallPainter: текущий материал для покраски: {currentPaintMaterial.name}");

                  // Настраиваем превью цвета
                  if (showColorPreview && colorPreviewPrefab != null)
                  {
                        colorPreview = Instantiate(colorPreviewPrefab);
                        previewComponent = colorPreview.GetComponent<SimpleColorPreview>();
                        if (previewComponent == null)
                        {
                              previewComponent = colorPreview.AddComponent<SimpleColorPreview>();
                        }
                        colorPreview.SetActive(false);
                        Debug.Log("WallPainter: создан объект предпросмотра цвета");
                  }
                  else if (showColorPreview)
                  {
                        // Создаем простой превью, если префаб не задан
                        colorPreview = CreateColorPreviewObject();
                        previewComponent = colorPreview.AddComponent<SimpleColorPreview>();
                        colorPreview.SetActive(false);
                        Debug.Log("WallPainter: создан стандартный объект предпросмотра цвета");
                  }
            }

            private void Update()
            {
                  HandleInput();
                  if (colorPreview != null)
                  {
                        UpdateColorPreview();
                  }
            }

            private void HandleInput()
            {
                  // Обработка касания/клика для покраски стены
                  if (Input.GetMouseButtonDown(0))
                  {
                        PaintWallAtPosition(Input.mousePosition);
                  }
            }

            private void UpdateColorPreview()
            {
                  if (colorPreview == null) return;
                  if (mainCamera == null) return;

                  Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                  RaycastHit hit;

                  if (Physics.Raycast(ray, out hit, 100f, wallLayerMask))
                  {
                        colorPreview.SetActive(true);
                        colorPreview.transform.position = hit.point + hit.normal * 0.01f;
                        colorPreview.transform.forward = hit.normal;

                        // Устанавливаем текущий материал на превью
                        if (previewComponent != null && currentPaintMaterial != null)
                        {
                              previewComponent.SetMaterial(currentPaintMaterial);
                        }
                  }
                  else
                  {
                        colorPreview.SetActive(false);
                  }
            }

            public void PaintWallAtPosition(Vector2 screenPosition)
            {
                  if (currentPaintMaterial == null)
                        return;
                  if (mainCamera == null)
                        return;

                  Ray ray = mainCamera.ScreenPointToRay(screenPosition);
                  RaycastHit hit;

                  if (Physics.Raycast(ray, out hit, 100f, wallLayerMask))
                  {
                        GameObject wallObject = hit.collider.gameObject;

                        // Сохраняем оригинальный материал, если еще не сохранен
                        if (!originalMaterials.ContainsKey(wallObject))
                        {
                              Renderer renderer = wallObject.GetComponent<Renderer>();
                              if (renderer != null)
                              {
                                    originalMaterials[wallObject] = renderer.material;
                              }
                        }

                        // Применяем новый материал
                        Renderer wallRenderer = wallObject.GetComponent<Renderer>();
                        if (wallRenderer != null)
                        {
                              wallRenderer.material = currentPaintMaterial;
                        }

                        Debug.Log($"Стена покрашена материалом: {currentPaintMaterial.name}");
                  }
            }

            public void SelectPaintMaterial(int index)
            {
                  // Проверяем наличие материалов
                  if (availablePaints == null || availablePaints.Length == 0)
                  {
                        Debug.LogWarning("Не заданы материалы для покраски в WallPainter. Невозможно выбрать материал.");
                        return;
                  }

                  if (index >= 0 && index < availablePaints.Length)
                  {
                        Material selectedMaterial = availablePaints[index];
                        if (selectedMaterial != null)
                        {
                              currentPaintMaterial = selectedMaterial;
                              Debug.Log($"Выбран материал: {currentPaintMaterial.name}");

                              // Обновляем превью, если оно активно
                              if (colorPreview != null && previewComponent != null && colorPreview.activeSelf)
                              {
                                    previewComponent.SetMaterial(currentPaintMaterial);
                              }
                        }
                        else
                        {
                              Debug.LogWarning($"Материал с индексом {index} равен null.");
                        }
                  }
                  else
                  {
                        Debug.LogWarning($"Индекс материала {index} выходит за пределы доступных материалов (0-{availablePaints.Length - 1}).");
                  }
            }

            public void ResetWallMaterials()
            {
                  foreach (var kvp in originalMaterials)
                  {
                        GameObject wallObject = kvp.Key;
                        Material originalMaterial = kvp.Value;

                        if (wallObject != null)
                        {
                              Renderer renderer = wallObject.GetComponent<Renderer>();
                              if (renderer != null)
                              {
                                    renderer.material = originalMaterial;
                              }
                        }
                  }

                  originalMaterials.Clear();
                  Debug.Log("Все материалы стен сброшены к исходным");
            }

            // Метод для создания простого объекта превью цвета
            private GameObject CreateColorPreviewObject()
            {
                  GameObject previewObject = new GameObject("ColorPreview");

                  // Добавляем компоненты для отображения
                  MeshFilter meshFilter = previewObject.AddComponent<MeshFilter>();
                  MeshRenderer renderer = previewObject.AddComponent<MeshRenderer>();

                  // Создаем простой круглый меш для превью
                  Mesh mesh = new Mesh();

                  int segments = 32;
                  float radius = 0.1f;

                  Vector3[] vertices = new Vector3[segments + 1];
                  int[] triangles = new int[segments * 3];
                  Vector2[] uvs = new Vector2[segments + 1];

                  vertices[0] = Vector3.zero;
                  uvs[0] = new Vector2(0.5f, 0.5f);

                  float angleStep = 360f / segments;

                  for (int i = 0; i < segments; i++)
                  {
                        float angle = angleStep * i * Mathf.Deg2Rad;
                        float x = Mathf.Sin(angle) * radius;
                        float y = Mathf.Cos(angle) * radius;

                        vertices[i + 1] = new Vector3(x, y, 0);
                        uvs[i + 1] = new Vector2((x / radius + 1) * 0.5f, (y / radius + 1) * 0.5f);

                        triangles[i * 3] = 0;
                        triangles[i * 3 + 1] = i + 1;
                        triangles[i * 3 + 2] = (i + 1) % segments + 1;
                  }

                  mesh.vertices = vertices;
                  mesh.triangles = triangles;
                  mesh.uv = uvs;
                  mesh.RecalculateNormals();

                  meshFilter.mesh = mesh;

                  // Устанавливаем материал
                  if (defaultMaterial != null)
                        renderer.material = defaultMaterial;

                  return previewObject;
            }

            // Метод для добавления стены в список стен для покраски
            public void AddWall(GameObject wallObject)
            {
                  if (wallObject != null && !walls.Contains(wallObject))
                  {
                        walls.Add(wallObject);

                        // Сохраняем оригинальный материал, если еще не сохранен
                        if (!originalMaterials.ContainsKey(wallObject))
                        {
                              Renderer renderer = wallObject.GetComponent<Renderer>();
                              if (renderer != null)
                              {
                                    originalMaterials[wallObject] = renderer.material;
                              }
                        }

                        Debug.Log($"Стена добавлена в список для покраски: {wallObject.name}");
                  }
            }
      }
}