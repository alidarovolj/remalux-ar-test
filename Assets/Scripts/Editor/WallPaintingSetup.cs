#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.UI;

namespace Remalux.AR
{
      public static class WallPaintingSetup
      {
            /// <summary>
            /// Возвращает подходящий шейдер в зависимости от используемого рендер пайплайна
            /// </summary>
            private static Shader GetAppropriateShader()
            {
                  if (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null)
                  {
                        // Для URP
                        Debug.Log("Используется URP, возвращаем URP шейдер");
                        return Shader.Find("Universal Render Pipeline/Lit");
                  }
                  else
                  {
                        // Для стандартного рендер пайплайна
                        return Shader.Find("Standard");
                  }
            }

            [MenuItem("Tools/Wall Painting/Setup System")]
            public static void SetupWallPaintingSystem()
            {
                  Debug.Log("Настройка системы покраски стен...");

                  // Создаем материалы
                  List<Material> materials = CreatePaintMaterials();

                  // Находим или создаем WallPaintingManager
                  var wallPaintingManager = FindOrCreateWallPaintingManager();

                  // Устанавливаем материалы
                  if (wallPaintingManager != null && materials.Count > 0)
                  {
                        SetMaterialsToManager(wallPaintingManager, materials.ToArray());
                  }

                  // Настраиваем компоненты
                  if (wallPaintingManager != null)
                  {
                        SetupComponents(wallPaintingManager);
                  }

                  Debug.Log("Система покраски стен настроена. Проверьте компоненты в инспекторе.");
            }

            [MenuItem("Tools/Wall Painting/Create Materials")]
            public static void CreateMaterialsMenu()
            {
                  CreatePaintMaterials();
            }

            [MenuItem("Tools/Wall Painting/Setup Components")]
            public static void SetupComponentsMenu()
            {
                  var wallPaintingManager = FindOrCreateWallPaintingManager();
                  if (wallPaintingManager != null)
                  {
                        SetupComponents(wallPaintingManager);
                  }
            }

            [MenuItem("GameObject/Setup Wall Painting", false, 10)]
            static void SetupSelectedWallPaintingManager()
            {
                  GameObject selectedObject = Selection.activeGameObject;
                  if (selectedObject == null)
                  {
                        Debug.LogError("Не выбран объект. Пожалуйста, выберите объект с компонентом WallPaintingManager.");
                        return;
                  }

                  MonoBehaviour wallPaintingManager = null;
                  var components = selectedObject.GetComponents<MonoBehaviour>();
                  foreach (var component in components)
                  {
                        if (component.GetType().Name == "WallPaintingManager")
                        {
                              wallPaintingManager = component;
                              break;
                        }
                  }

                  if (wallPaintingManager == null)
                  {
                        Debug.LogError($"На объекте {selectedObject.name} не найден компонент WallPaintingManager.");
                        return;
                  }

                  // Создаем материалы
                  List<Material> materials = CreatePaintMaterials();

                  // Устанавливаем материалы
                  if (materials.Count > 0)
                  {
                        SetMaterialsToManager(wallPaintingManager, materials.ToArray());
                  }

                  // Настраиваем компоненты
                  SetupComponents(wallPaintingManager);

                  Debug.Log($"Система покраски стен настроена для объекта {selectedObject.name}. Проверьте компоненты в инспекторе.");
            }

            [MenuItem("Tools/Wall Painting/Copy Materials Between Managers")]
            public static void CopyMaterialsBetweenManagers()
            {
                  // Находим все WallPaintingManager в сцене
                  List<MonoBehaviour> managers = new List<MonoBehaviour>();

                  var allMonoBehaviours = Object.FindObjectsOfType<MonoBehaviour>();
                  foreach (var mb in allMonoBehaviours)
                  {
                        if (mb.GetType().Name == "WallPaintingManager")
                        {
                              managers.Add(mb);
                        }
                  }

                  if (managers.Count < 2)
                  {
                        Debug.LogWarning("Найдено менее двух WallPaintingManager в сцене. Нечего копировать.");
                        return;
                  }

                  // Находим менеджер с материалами
                  MonoBehaviour sourceManager = null;
                  Material[] materials = null;

                  foreach (var manager in managers)
                  {
                        var sourcePaintMaterialsField = manager.GetType().GetField("paintMaterials");
                        if (sourcePaintMaterialsField != null)
                        {
                              var mats = sourcePaintMaterialsField.GetValue(manager) as Material[];
                              if (mats != null && mats.Length > 0)
                              {
                                    sourceManager = manager;
                                    materials = mats;
                                    break;
                              }
                        }
                  }

                  if (sourceManager == null || materials == null || materials.Length == 0)
                  {
                        Debug.LogWarning("Не найден WallPaintingManager с материалами.");
                        return;
                  }

                  // Получаем материал по умолчанию
                  Material defaultMaterial = null;
                  var sourceDefaultMaterialField = sourceManager.GetType().GetField("defaultWallMaterial");
                  if (sourceDefaultMaterialField != null)
                  {
                        defaultMaterial = sourceDefaultMaterialField.GetValue(sourceManager) as Material;
                  }

                  // Копируем материалы в другие менеджеры
                  int count = 0;
                  foreach (var manager in managers)
                  {
                        if (manager == sourceManager)
                              continue;

                        var targetPaintMaterialsField = manager.GetType().GetField("paintMaterials");
                        if (targetPaintMaterialsField != null)
                        {
                              targetPaintMaterialsField.SetValue(manager, materials);

                              if (defaultMaterial != null)
                              {
                                    var targetDefaultMaterialField = manager.GetType().GetField("defaultWallMaterial");
                                    if (targetDefaultMaterialField != null)
                                    {
                                          targetDefaultMaterialField.SetValue(manager, defaultMaterial);
                                    }
                              }

                              EditorUtility.SetDirty(manager);
                              count++;
                        }
                  }

                  Debug.Log($"Материалы скопированы из {sourceManager.gameObject.name} в {count} других WallPaintingManager.");

                  // Настраиваем компоненты для всех менеджеров
                  foreach (var manager in managers)
                  {
                        SetupComponents(manager);
                  }
            }

            // Добавляем пункт контекстного меню для быстрого копирования материалов
            [MenuItem("GameObject/Copy Materials To This WallPaintingManager", true)]
            static bool ValidateCopyMaterialsToSelected()
            {
                  GameObject selectedObject = Selection.activeGameObject;
                  if (selectedObject == null)
                        return false;

                  var components = selectedObject.GetComponents<MonoBehaviour>();
                  foreach (var component in components)
                  {
                        if (component.GetType().Name == "WallPaintingManager")
                              return true;
                  }

                  return false;
            }

            [MenuItem("GameObject/Copy Materials To This WallPaintingManager", false, 11)]
            static void CopyMaterialsToSelected()
            {
                  GameObject selectedObject = Selection.activeGameObject;
                  if (selectedObject == null)
                        return;

                  MonoBehaviour targetManager = null;
                  var components = selectedObject.GetComponents<MonoBehaviour>();
                  foreach (var component in components)
                  {
                        if (component.GetType().Name == "WallPaintingManager")
                        {
                              targetManager = component;
                              break;
                        }
                  }

                  if (targetManager == null)
                        return;

                  // Находим другой WallPaintingManager с материалами
                  MonoBehaviour sourceManager = null;
                  Material[] materials = null;

                  var allMonoBehaviours = Object.FindObjectsOfType<MonoBehaviour>();
                  foreach (var mb in allMonoBehaviours)
                  {
                        if (mb == targetManager)
                              continue;

                        if (mb.GetType().Name == "WallPaintingManager")
                        {
                              var sourcePaintMaterialsField = mb.GetType().GetField("paintMaterials");
                              if (sourcePaintMaterialsField != null)
                              {
                                    var mats = sourcePaintMaterialsField.GetValue(mb) as Material[];
                                    if (mats != null && mats.Length > 0)
                                    {
                                          sourceManager = mb;
                                          materials = mats;
                                          break;
                                    }
                              }
                        }
                  }

                  if (sourceManager == null || materials == null || materials.Length == 0)
                  {
                        Debug.LogWarning("Не найден другой WallPaintingManager с материалами.");
                        return;
                  }

                  // Копируем материалы
                  var targetPaintMaterialsField = targetManager.GetType().GetField("paintMaterials");
                  if (targetPaintMaterialsField != null)
                  {
                        targetPaintMaterialsField.SetValue(targetManager, materials);

                        // Копируем материал по умолчанию
                        var sourceDefaultMaterialField = sourceManager.GetType().GetField("defaultWallMaterial");
                        if (sourceDefaultMaterialField != null)
                        {
                              var defaultMaterial = sourceDefaultMaterialField.GetValue(sourceManager) as Material;
                              if (defaultMaterial != null)
                              {
                                    var targetDefaultMaterialField = targetManager.GetType().GetField("defaultWallMaterial");
                                    if (targetDefaultMaterialField != null)
                                    {
                                          targetDefaultMaterialField.SetValue(targetManager, defaultMaterial);
                                    }
                              }
                        }

                        EditorUtility.SetDirty(targetManager);
                        Debug.Log($"Материалы скопированы из {sourceManager.gameObject.name} в {targetManager.gameObject.name}.");

                        // Настраиваем компоненты
                        SetupComponents(targetManager);
                  }
            }

            private static List<Material> CreatePaintMaterials()
            {
                  // Создаем папку для материалов
                  string folderPath = "Assets/Materials/PaintMaterials";
                  if (!Directory.Exists(Application.dataPath + "/Materials"))
                  {
                        Directory.CreateDirectory(Application.dataPath + "/Materials");
                  }

                  if (!Directory.Exists(Application.dataPath + "/Materials/PaintMaterials"))
                  {
                        Directory.CreateDirectory(Application.dataPath + "/Materials/PaintMaterials");
                        AssetDatabase.Refresh();
                  }

                  // Создаем базовые цвета
                  Color[] colors = new Color[]
                  {
                  Color.red,
                  Color.green,
                  Color.blue,
                  Color.yellow,
                  Color.cyan,
                  Color.magenta,
                  new Color(1.0f, 0.5f, 0.0f), // Оранжевый
                  new Color(0.5f, 0.0f, 0.5f), // Фиолетовый
                  Color.white,
                  Color.gray
                  };

                  string[] names = new string[]
                  {
                  "RedPaint",
                  "GreenPaint",
                  "BluePaint",
                  "YellowPaint",
                  "CyanPaint",
                  "MagentaPaint",
                  "OrangePaint",
                  "PurplePaint",
                  "WhitePaint",
                  "GrayPaint"
                  };

                  List<Material> materials = new List<Material>();

                  for (int i = 0; i < colors.Length && i < names.Length; i++)
                  {
                        string materialName = names[i];
                        string materialPath = $"{folderPath}/{materialName}.mat";

                        // Проверяем, существует ли уже такой материал
                        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                        if (material == null)
                        {
                              // Создаем новый материал
                              material = new Material(GetAppropriateShader());
                              material.color = colors[i];
                              AssetDatabase.CreateAsset(material, materialPath);
                        }
                        else
                        {
                              // Обновляем существующий материал
                              material.color = colors[i];
                              EditorUtility.SetDirty(material);
                        }

                        materials.Add(material);
                  }

                  AssetDatabase.SaveAssets();
                  Debug.Log($"Создано {materials.Count} материалов для покраски");

                  return materials;
            }

            private static MonoBehaviour FindOrCreateWallPaintingManager()
            {
                  // Находим WallPaintingManager в сцене
                  MonoBehaviour wallPaintingManager = null;

                  var allMonoBehaviours = Object.FindObjectsOfType<MonoBehaviour>();
                  foreach (var mb in allMonoBehaviours)
                  {
                        if (mb.GetType().Name == "WallPaintingManager")
                        {
                              wallPaintingManager = mb;
                              Debug.Log("Найден WallPaintingManager в сцене");
                              break;
                        }
                  }

                  // Если не найден, создаем новый
                  if (wallPaintingManager == null)
                  {
                        // Ищем тип WallPaintingManager
                        System.Type wallPaintingManagerType = null;

                        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                        foreach (var assembly in assemblies)
                        {
                              var types = assembly.GetTypes();
                              foreach (var type in types)
                              {
                                    if (type.Name == "WallPaintingManager")
                                    {
                                          wallPaintingManagerType = type;
                                          break;
                                    }
                              }

                              if (wallPaintingManagerType != null)
                                    break;
                        }

                        if (wallPaintingManagerType != null)
                        {
                              GameObject managerObj = new GameObject("WallPaintingManager");
                              wallPaintingManager = managerObj.AddComponent(wallPaintingManagerType) as MonoBehaviour;
                              Debug.Log("Создан новый WallPaintingManager");
                        }
                        else
                        {
                              Debug.LogError("Не удалось найти тип WallPaintingManager");
                        }
                  }

                  return wallPaintingManager;
            }

            private static void SetMaterialsToManager(MonoBehaviour manager, Material[] materials)
            {
                  // Устанавливаем материалы через рефлексию
                  var paintMaterialsField = manager.GetType().GetField("paintMaterials");
                  if (paintMaterialsField != null)
                  {
                        paintMaterialsField.SetValue(manager, materials);

                        // Устанавливаем материал по умолчанию
                        var defaultMaterialField = manager.GetType().GetField("defaultWallMaterial");
                        if (defaultMaterialField != null && materials.Length > 0)
                        {
                              defaultMaterialField.SetValue(manager, materials[0]);
                        }

                        EditorUtility.SetDirty(manager);
                        Debug.Log($"Материалы установлены для WallPaintingManager");
                  }
                  else
                  {
                        Debug.LogError("Не удалось найти поле paintMaterials в WallPaintingManager");
                  }
            }

            private static void SetupComponents(MonoBehaviour manager)
            {
                  // Находим или создаем WallPainter
                  MonoBehaviour wallPainter = FindOrCreateComponent("WallPainter");

                  // Находим или создаем SimplePaintColorSelector
                  MonoBehaviour colorSelector = FindOrCreateColorSelector();

                  if (wallPainter != null && colorSelector != null)
                  {
                        // Устанавливаем ссылки в WallPaintingManager
                        var wallPainterField = manager.GetType().GetField("wallPainter");
                        if (wallPainterField != null)
                        {
                              wallPainterField.SetValue(manager, wallPainter);
                              Debug.Log("Установлена ссылка на WallPainter в WallPaintingManager");
                        }

                        var colorSelectorField = manager.GetType().GetField("colorSelector");
                        if (colorSelectorField != null)
                        {
                              colorSelectorField.SetValue(manager, colorSelector);
                              Debug.Log("Установлена ссылка на SimplePaintColorSelector в WallPaintingManager");
                        }

                        var paintingUIField = manager.GetType().GetField("paintingUI");
                        if (paintingUIField != null)
                        {
                              paintingUIField.SetValue(manager, colorSelector.gameObject);
                              Debug.Log("Установлена ссылка на UI в WallPaintingManager");
                        }

                        // Получаем материалы из WallPaintingManager
                        var paintMaterialsField = manager.GetType().GetField("paintMaterials");
                        var defaultWallMaterialField = manager.GetType().GetField("defaultWallMaterial");

                        if (paintMaterialsField != null && defaultWallMaterialField != null)
                        {
                              Material[] materials = paintMaterialsField.GetValue(manager) as Material[];
                              Material defaultMaterial = defaultWallMaterialField.GetValue(manager) as Material;

                              if (materials != null && materials.Length > 0)
                              {
                                    // Устанавливаем материалы в WallPainter
                                    var availablePaintsField = wallPainter.GetType().GetField("availablePaints");
                                    var defaultMaterialField = wallPainter.GetType().GetField("defaultMaterial");

                                    if (availablePaintsField != null)
                                    {
                                          availablePaintsField.SetValue(wallPainter, materials);
                                          Debug.Log($"Установлено {materials.Length} материалов в WallPainter");
                                    }

                                    if (defaultMaterialField != null && defaultMaterial != null)
                                    {
                                          defaultMaterialField.SetValue(wallPainter, defaultMaterial);
                                          Debug.Log($"Установлен материал по умолчанию в WallPainter: {defaultMaterial.name}");
                                    }

                                    // Устанавливаем материалы в SimplePaintColorSelector
                                    var paintMaterialsFieldSelector = colorSelector.GetType().GetField("paintMaterials");
                                    if (paintMaterialsFieldSelector != null)
                                    {
                                          paintMaterialsFieldSelector.SetValue(colorSelector, materials);
                                          Debug.Log($"Установлено {materials.Length} материалов в SimplePaintColorSelector");
                                    }

                                    // Устанавливаем ссылку на WallPainter в SimplePaintColorSelector
                                    var wallPainterFieldSelector = colorSelector.GetType().GetField("wallPainter");
                                    if (wallPainterFieldSelector != null)
                                    {
                                          wallPainterFieldSelector.SetValue(colorSelector, wallPainter);
                                          Debug.Log("Установлена ссылка на WallPainter в SimplePaintColorSelector");
                                    }

                                    // Инициализируем SimplePaintColorSelector
                                    var initializeMethod = colorSelector.GetType().GetMethod("Initialize");
                                    if (initializeMethod != null)
                                    {
                                          initializeMethod.Invoke(colorSelector, null);
                                          Debug.Log("Инициализирован SimplePaintColorSelector");
                                    }
                              }
                              else
                              {
                                    Debug.LogWarning("Нет материалов в WallPaintingManager. Сначала создайте материалы.");
                              }
                        }

                        EditorUtility.SetDirty(manager);
                        EditorUtility.SetDirty(wallPainter);
                        EditorUtility.SetDirty(colorSelector);
                  }
            }

            private static MonoBehaviour FindOrCreateComponent(string componentName)
            {
                  // Находим компонент в сцене
                  MonoBehaviour component = null;

                  var allMonoBehaviours = Object.FindObjectsOfType<MonoBehaviour>();
                  foreach (var mb in allMonoBehaviours)
                  {
                        if (mb.GetType().Name == componentName)
                        {
                              component = mb;
                              Debug.Log($"Найден {componentName} в сцене");
                              break;
                        }
                  }

                  // Если не найден, создаем новый
                  if (component == null)
                  {
                        // Ищем тип компонента
                        System.Type componentType = null;

                        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                        foreach (var assembly in assemblies)
                        {
                              var types = assembly.GetTypes();
                              foreach (var type in types)
                              {
                                    if (type.Name == componentName)
                                    {
                                          componentType = type;
                                          break;
                                    }
                              }

                              if (componentType != null)
                                    break;
                        }

                        if (componentType != null)
                        {
                              GameObject obj = new GameObject(componentName);
                              component = obj.AddComponent(componentType) as MonoBehaviour;
                              Debug.Log($"Создан новый {componentName}");
                        }
                        else
                        {
                              Debug.LogError($"Не удалось найти тип {componentName}");
                        }
                  }

                  return component;
            }

            private static MonoBehaviour FindOrCreateColorSelector()
            {
                  // Находим SimplePaintColorSelector в сцене
                  MonoBehaviour colorSelector = null;

                  var allMonoBehaviours = Object.FindObjectsOfType<MonoBehaviour>();
                  foreach (var mb in allMonoBehaviours)
                  {
                        if (mb.GetType().Name == "SimplePaintColorSelector")
                        {
                              colorSelector = mb;
                              Debug.Log("Найден SimplePaintColorSelector в сцене");
                              break;
                        }
                  }

                  // Если не найден, создаем новый
                  if (colorSelector == null)
                  {
                        // Ищем тип SimplePaintColorSelector
                        System.Type colorSelectorType = null;

                        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                        foreach (var assembly in assemblies)
                        {
                              var types = assembly.GetTypes();
                              foreach (var type in types)
                              {
                                    if (type.Name == "SimplePaintColorSelector")
                                    {
                                          colorSelectorType = type;
                                          break;
                                    }
                              }

                              if (colorSelectorType != null)
                                    break;
                        }

                        if (colorSelectorType != null)
                        {
                              // Создаем или находим Canvas
                              GameObject canvasObj = GameObject.Find("Canvas");
                              if (canvasObj == null)
                              {
                                    canvasObj = new GameObject("Canvas");
                                    Canvas canvas = canvasObj.AddComponent<Canvas>();
                                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                                    canvasObj.AddComponent<CanvasScaler>();
                                    canvasObj.AddComponent<GraphicRaycaster>();
                                    Debug.Log("Создан новый Canvas");
                              }

                              // Создаем объект для SimplePaintColorSelector
                              GameObject selectorObj = new GameObject("PaintColorSelector");
                              selectorObj.transform.SetParent(canvasObj.transform, false);
                              RectTransform rectTransform = selectorObj.AddComponent<RectTransform>();
                              rectTransform.anchorMin = new Vector2(0, 0);
                              rectTransform.anchorMax = new Vector2(1, 0.2f);
                              rectTransform.offsetMin = new Vector2(0, 0);
                              rectTransform.offsetMax = new Vector2(0, 0);

                              colorSelector = selectorObj.AddComponent(colorSelectorType) as MonoBehaviour;
                              Debug.Log("Создан новый SimplePaintColorSelector");

                              // Создаем контейнер для кнопок
                              GameObject containerObj = new GameObject("ColorButtonsContainer");
                              containerObj.transform.SetParent(selectorObj.transform, false);
                              RectTransform containerRectTransform = containerObj.AddComponent<RectTransform>();
                              containerRectTransform.anchorMin = new Vector2(0, 0);
                              containerRectTransform.anchorMax = new Vector2(1, 0.7f);
                              containerRectTransform.offsetMin = new Vector2(10, 10);
                              containerRectTransform.offsetMax = new Vector2(-10, -10);

                              HorizontalLayoutGroup layout = containerObj.AddComponent<HorizontalLayoutGroup>();
                              layout.spacing = 10;
                              layout.childAlignment = TextAnchor.MiddleCenter;
                              layout.childForceExpandWidth = false;
                              layout.childForceExpandHeight = false;

                              // Устанавливаем контейнер
                              var containerField = colorSelector.GetType().GetField("colorButtonsContainer");
                              if (containerField != null)
                              {
                                    containerField.SetValue(colorSelector, containerRectTransform);
                              }

                              // Создаем кнопку сброса
                              GameObject resetButtonObj = new GameObject("ResetButton");
                              resetButtonObj.transform.SetParent(selectorObj.transform, false);
                              RectTransform resetRectTransform = resetButtonObj.AddComponent<RectTransform>();
                              resetRectTransform.anchorMin = new Vector2(0.5f, 0.8f);
                              resetRectTransform.anchorMax = new Vector2(0.5f, 1.0f);
                              resetRectTransform.sizeDelta = new Vector2(100, 40);

                              Image resetImage = resetButtonObj.AddComponent<Image>();
                              resetImage.color = Color.white;

                              Button resetButton = resetButtonObj.AddComponent<Button>();

                              // Устанавливаем кнопку сброса
                              var resetButtonField = colorSelector.GetType().GetField("resetButton");
                              if (resetButtonField != null)
                              {
                                    resetButtonField.SetValue(colorSelector, resetButton);
                              }

                              // Добавляем текст на кнопку
                              GameObject textObj = new GameObject("Text");
                              textObj.transform.SetParent(resetButtonObj.transform, false);
                              RectTransform textRectTransform = textObj.AddComponent<RectTransform>();
                              textRectTransform.anchorMin = Vector2.zero;
                              textRectTransform.anchorMax = Vector2.one;
                              textRectTransform.offsetMin = Vector2.zero;
                              textRectTransform.offsetMax = Vector2.zero;

                              Text text = textObj.AddComponent<Text>();
                              text.text = "Сброс";
                              text.alignment = TextAnchor.MiddleCenter;
                              text.color = Color.black;
                              text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                        }
                        else
                        {
                              Debug.LogError("Не удалось найти тип SimplePaintColorSelector");
                        }
                  }

                  return colorSelector;
            }
      }
}
#endif