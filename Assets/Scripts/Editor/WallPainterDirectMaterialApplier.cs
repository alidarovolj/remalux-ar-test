#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace Remalux.AR
{
      public class WallPainterDirectMaterialApplier : EditorWindow
      {
            private List<Material> availableMaterials = new List<Material>();
            private int selectedMaterialIndex = 0;
            private Vector2 scrollPosition;
            private bool showAllProjectMaterials = false;
            private string searchFilter = "";
            private bool showWallObjects = false;
            private Vector2 wallObjectsScrollPosition;
            private List<GameObject> wallObjects = new List<GameObject>();
            private List<bool> wallObjectsSelected = new List<bool>();
            private bool selectAllWalls = true;

            [MenuItem("Tools/Wall Painting/Direct Material Applier")]
            public static void ShowWindow()
            {
                  WallPainterDirectMaterialApplier window = EditorWindow.GetWindow<WallPainterDirectMaterialApplier>("Wall Painter");
                  window.minSize = new Vector2(300, 400);
                  window.LoadMaterials();
                  window.FindWallObjects();
            }

            private void OnGUI()
            {
                  EditorGUILayout.LabelField("Wall Painter Direct Material Applier", EditorStyles.boldLabel);
                  EditorGUILayout.Space();

                  // Поиск материалов
                  EditorGUILayout.BeginHorizontal();
                  searchFilter = EditorGUILayout.TextField("Поиск материалов", searchFilter);
                  if (GUILayout.Button("Обновить", GUILayout.Width(80)))
                  {
                        LoadMaterials();
                        FindWallObjects();
                  }
                  EditorGUILayout.EndHorizontal();

                  // Переключатель для отображения всех материалов проекта
                  showAllProjectMaterials = EditorGUILayout.Toggle("Показать все материалы проекта", showAllProjectMaterials);
                  if (showAllProjectMaterials)
                  {
                        EditorGUILayout.HelpBox("Отображаются все материалы проекта. Это может занять некоторое время при большом количестве материалов.", MessageType.Info);
                  }

                  // Отображение доступных материалов
                  EditorGUILayout.LabelField("Доступные материалы:", EditorStyles.boldLabel);
                  scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

                  List<Material> filteredMaterials = FilterMaterials();
                  string[] materialNames = new string[filteredMaterials.Count];
                  for (int i = 0; i < filteredMaterials.Count; i++)
                  {
                        materialNames[i] = filteredMaterials[i] != null ? filteredMaterials[i].name : "Пустой материал";
                  }

                  if (filteredMaterials.Count > 0)
                  {
                        selectedMaterialIndex = EditorGUILayout.Popup("Выбрать материал",
                            Mathf.Min(selectedMaterialIndex, filteredMaterials.Count - 1), materialNames);

                        // Отображение превью материала
                        if (selectedMaterialIndex >= 0 && selectedMaterialIndex < filteredMaterials.Count && filteredMaterials[selectedMaterialIndex] != null)
                        {
                              Material selectedMaterial = filteredMaterials[selectedMaterialIndex];
                              EditorGUILayout.Space();
                              EditorGUILayout.LabelField("Превью материала:", EditorStyles.boldLabel);

                              Rect previewRect = GUILayoutUtility.GetRect(64, 64, GUILayout.ExpandWidth(false));
                              EditorGUI.DrawPreviewTexture(previewRect, AssetPreview.GetAssetPreview(selectedMaterial));

                              EditorGUILayout.LabelField("Имя:", selectedMaterial.name);
                              EditorGUILayout.LabelField("Шейдер:", selectedMaterial.shader.name);
                        }
                  }
                  else
                  {
                        EditorGUILayout.HelpBox("Материалы не найдены", MessageType.Warning);
                  }

                  EditorGUILayout.EndScrollView();

                  // Кнопки для применения материалов
                  EditorGUILayout.Space();
                  EditorGUILayout.BeginHorizontal();
                  if (GUILayout.Button("Применить ко всем стенам"))
                  {
                        ApplyMaterialToAllWalls(filteredMaterials[selectedMaterialIndex]);
                  }
                  if (GUILayout.Button("Применить к выбранным стенам"))
                  {
                        ApplyMaterialToSelectedWalls(filteredMaterials[selectedMaterialIndex]);
                  }
                  EditorGUILayout.EndHorizontal();

                  // Отображение объектов стен
                  EditorGUILayout.Space();
                  showWallObjects = EditorGUILayout.Foldout(showWallObjects, "Объекты стен");
                  if (showWallObjects)
                  {
                        EditorGUILayout.BeginHorizontal();
                        selectAllWalls = EditorGUILayout.Toggle("Выбрать все", selectAllWalls);
                        if (GUILayout.Button("Применить выбор"))
                        {
                              for (int i = 0; i < wallObjectsSelected.Count; i++)
                              {
                                    wallObjectsSelected[i] = selectAllWalls;
                              }
                        }
                        EditorGUILayout.EndHorizontal();

                        wallObjectsScrollPosition = EditorGUILayout.BeginScrollView(wallObjectsScrollPosition, GUILayout.Height(150));
                        for (int i = 0; i < wallObjects.Count; i++)
                        {
                              if (i < wallObjectsSelected.Count)
                              {
                                    EditorGUILayout.BeginHorizontal();
                                    wallObjectsSelected[i] = EditorGUILayout.Toggle(wallObjectsSelected[i], GUILayout.Width(20));
                                    EditorGUILayout.ObjectField(wallObjects[i], typeof(GameObject), true);
                                    EditorGUILayout.EndHorizontal();
                              }
                        }
                        EditorGUILayout.EndScrollView();
                  }

                  // Кнопки для проверки и исправления материалов
                  EditorGUILayout.Space();
                  EditorGUILayout.LabelField("Инструменты для материалов:", EditorStyles.boldLabel);
                  EditorGUILayout.BeginHorizontal();
                  if (GUILayout.Button("Проверить материалы стен"))
                  {
                        CheckWallMaterials();
                  }
                  if (GUILayout.Button("Исправить общие материалы"))
                  {
                        FixSharedMaterials();
                  }
                  EditorGUILayout.EndHorizontal();
            }

            private void LoadMaterials()
            {
                  availableMaterials.Clear();

                  // Сначала пробуем получить материалы из WallPainter
                  bool foundWallPainterMaterials = false;
                  MonoBehaviour[] allComponents = FindObjectsOfType<MonoBehaviour>();
                  foreach (MonoBehaviour component in allComponents)
                  {
                        if (component.GetType().Name == "WallPainter")
                        {
                              // Пробуем получить материалы из разных возможных полей
                              string[] possibleFieldNames = { "availableMaterials", "paintMaterials", "materials" };
                              foreach (string fieldName in possibleFieldNames)
                              {
                                    System.Reflection.FieldInfo materialsField = component.GetType().GetField(fieldName);
                                    if (materialsField != null)
                                    {
                                          Material[] materials = (Material[])materialsField.GetValue(component);
                                          if (materials != null && materials.Length > 0)
                                          {
                                                foreach (Material mat in materials)
                                                {
                                                      if (mat != null && !availableMaterials.Contains(mat))
                                                      {
                                                            availableMaterials.Add(mat);
                                                      }
                                                }
                                                foundWallPainterMaterials = true;
                                                Debug.Log($"Загружено {materials.Length} материалов из поля {fieldName} компонента WallPainter");
                                          }
                                    }
                              }
                        }
                  }

                  // Если не нашли материалы в WallPainter или включен режим отображения всех материалов
                  if (!foundWallPainterMaterials || showAllProjectMaterials)
                  {
                        // Находим все материалы в проекте
                        string[] materialGuids = AssetDatabase.FindAssets("t:Material");
                        foreach (string guid in materialGuids)
                        {
                              string path = AssetDatabase.GUIDToAssetPath(guid);
                              Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                              if (material != null && !availableMaterials.Contains(material))
                              {
                                    availableMaterials.Add(material);
                              }
                        }
                        Debug.Log($"Загружено {availableMaterials.Count} материалов из проекта");
                  }
            }

            private List<Material> FilterMaterials()
            {
                  if (string.IsNullOrEmpty(searchFilter))
                        return availableMaterials;

                  List<Material> filtered = new List<Material>();
                  foreach (Material material in availableMaterials)
                  {
                        if (material != null && material.name.ToLower().Contains(searchFilter.ToLower()))
                        {
                              filtered.Add(material);
                        }
                  }
                  return filtered;
            }

            private void FindWallObjects()
            {
                  wallObjects.Clear();
                  wallObjectsSelected.Clear();

                  GameObject[] allObjects = FindObjectsOfType<GameObject>();
                  foreach (GameObject obj in allObjects)
                  {
                        if (obj.layer == 8) // Слой "Wall"
                        {
                              wallObjects.Add(obj);
                              wallObjectsSelected.Add(selectAllWalls);
                        }
                  }
                  Debug.Log($"Найдено {wallObjects.Count} объектов на слое Wall (8)");
            }

            private void ApplyMaterialToAllWalls(Material material)
            {
                  if (material == null)
                  {
                        Debug.LogError("Материал не выбран");
                        return;
                  }

                  int paintedCount = 0;
                  foreach (GameObject obj in wallObjects)
                  {
                        if (ApplyMaterialToWall(obj, material))
                        {
                              paintedCount++;
                        }
                  }

                  Debug.Log($"Материал {material.name} применен к {paintedCount} объектам стен");
            }

            private void ApplyMaterialToSelectedWalls(Material material)
            {
                  if (material == null)
                  {
                        Debug.LogError("Материал не выбран");
                        return;
                  }

                  int paintedCount = 0;
                  for (int i = 0; i < wallObjects.Count; i++)
                  {
                        if (i < wallObjectsSelected.Count && wallObjectsSelected[i])
                        {
                              if (ApplyMaterialToWall(wallObjects[i], material))
                              {
                                    paintedCount++;
                              }
                        }
                  }

                  Debug.Log($"Материал {material.name} применен к {paintedCount} выбранным объектам стен");
            }

            private bool ApplyMaterialToWall(GameObject wallObject, Material material)
            {
                  Renderer renderer = wallObject.GetComponent<Renderer>();
                  if (renderer == null)
                  {
                        Debug.LogWarning($"Объект {wallObject.name} не имеет компонента Renderer");
                        return false;
                  }

                  // Проверяем, есть ли компонент для отслеживания материалов
                  WallMaterialInstanceTracker tracker = wallObject.GetComponent<WallMaterialInstanceTracker>();
                  if (tracker == null)
                  {
                        tracker = wallObject.AddComponent<WallMaterialInstanceTracker>();
                        tracker.OriginalSharedMaterial = renderer.sharedMaterial;
                        Debug.Log($"Added WallMaterialInstanceTracker to {wallObject.name}");
                  }

                  // Apply material through the tracker
                  tracker.ApplyMaterial(material);
                  Debug.Log($"Applied material {material.name} to {wallObject.name} through WallMaterialInstanceTracker");

                  return true;
            }

            private void CheckWallMaterials()
            {
                  int sharedMaterialCount = 0;
                  int instancedMaterialCount = 0;

                  foreach (GameObject obj in wallObjects)
                  {
                        Renderer renderer = obj.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                              Material material = renderer.material;
                              Material sharedMaterial = renderer.sharedMaterial;

                              if (material == sharedMaterial)
                              {
                                    Debug.LogWarning($"Объект {obj.name} использует общий материал: {sharedMaterial.name}");
                                    sharedMaterialCount++;
                              }
                              else
                              {
                                    Debug.Log($"Объект {obj.name} использует экземпляр материала: {material.name}");
                                    instancedMaterialCount++;
                              }
                        }
                  }

                  Debug.Log($"Результаты проверки: {sharedMaterialCount} объектов с общими материалами, {instancedMaterialCount} объектов с экземплярами материалов");
                  EditorUtility.DisplayDialog("Результаты проверки материалов",
                      $"Найдено {sharedMaterialCount} объектов с общими материалами\n{instancedMaterialCount} объектов с экземплярами материалов",
                      "OK");
            }

            private void FixSharedMaterials()
            {
                  int fixedCount = 0;

                  foreach (GameObject obj in wallObjects)
                  {
                        Renderer renderer = obj.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                              Material sharedMaterial = renderer.sharedMaterial;
                              if (sharedMaterial != null)
                              {
                                    // Создаем экземпляр материала
                                    Material instancedMaterial = new Material(sharedMaterial);
                                    instancedMaterial.name = $"{sharedMaterial.name}_Instance_{obj.name}";

                                    // Применяем экземпляр материала
                                    renderer.material = instancedMaterial;

                                    // Добавляем компонент для отслеживания
                                    WallMaterialInstanceTracker tracker = obj.GetComponent<WallMaterialInstanceTracker>();
                                    if (tracker == null)
                                    {
                                          tracker = obj.AddComponent<WallMaterialInstanceTracker>();
                                          tracker.OriginalSharedMaterial = sharedMaterial;
                                    }

                                    fixedCount++;
                              }
                        }
                  }

                  Debug.Log($"Исправлено {fixedCount} объектов стен");
                  EditorUtility.DisplayDialog("Исправление общих материалов",
                      $"Исправлено {fixedCount} объектов стен",
                      "OK");
            }
      }
}
#endif