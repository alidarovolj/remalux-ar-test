using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Remalux.AR
{
      /// <summary>
      /// Упрощенная версия селектора цвета краски
      /// </summary>
      public class SimplePaintColorSelector : MonoBehaviour
      {
            public WallPainter wallPainter;
            public Material[] paintMaterials;
            public Button resetButton;
            public RectTransform colorButtonsContainer;
            public GameObject buttonPrefab; // Простая кнопка UI

            private List<SimpleColorButton> colorButtons = new List<SimpleColorButton>();
            private int selectedColorIndex = -1;

            private void Awake()
            {
                  if (resetButton != null)
                        resetButton.onClick.AddListener(ResetWallColors);
            }

            private void Start()
            {
                  // Проверяем, есть ли у нас материалы
                  if (paintMaterials == null || paintMaterials.Length == 0)
                  {
                        // Пытаемся получить материалы из WallPaintingManager
                        WallPaintingManager manager = FindObjectOfType<WallPaintingManager>();
                        if (manager != null && manager.paintMaterials != null && manager.paintMaterials.Length > 0)
                        {
                              Debug.Log("Получаем материалы из WallPaintingManager");
                              paintMaterials = manager.paintMaterials;
                        }
                        else
                        {
                              Debug.LogWarning("Не заданы материалы для покраски в SimplePaintColorSelector и не найдены в WallPaintingManager");

                              // Создаем хотя бы один материал, чтобы система не падала
                              paintMaterials = new Material[1];
                              paintMaterials[0] = new Material(Shader.Find("Standard"));
                              paintMaterials[0].name = "DefaultPaint";
                              paintMaterials[0].color = Color.white;
                              Debug.Log("SimplePaintColorSelector: создан стандартный материал для покраски");
                        }
                  }

                  // Проверяем, есть ли у нас WallPainter
                  if (wallPainter == null)
                  {
                        // Пытаемся получить WallPainter из WallPaintingManager
                        WallPaintingManager manager = FindObjectOfType<WallPaintingManager>();
                        if (manager != null && manager.wallPainter != null)
                        {
                              Debug.Log("Получаем WallPainter из WallPaintingManager");
                              wallPainter = manager.wallPainter;
                        }
                        else
                        {
                              // Пытаемся найти WallPainter в сцене
                              wallPainter = FindObjectOfType<WallPainter>();
                              if (wallPainter == null)
                              {
                                    Debug.LogWarning("Не задан WallPainter в SimplePaintColorSelector и не найден в сцене");

                                    // Создаем WallPainter, если его нет
                                    GameObject wallPainterObj = new GameObject("WallPainter");
                                    wallPainter = wallPainterObj.AddComponent<WallPainter>();
                                    Debug.Log("SimplePaintColorSelector: создан новый WallPainter");
                              }
                        }
                  }

                  // Проверяем наличие контейнера и префаба заранее
                  CheckContainerAndPrefab();

                  // Инициализируем селектор
                  Initialize();
            }

            /// <summary>
            /// Проверяет наличие контейнера и префаба кнопки, создает их при необходимости
            /// </summary>
            private void CheckContainerAndPrefab()
            {
                  // Проверяем наличие контейнера
                  if (colorButtonsContainer == null)
                  {
                        Debug.LogWarning("Не задан контейнер для кнопок в SimplePaintColorSelector");

                        // Пытаемся найти контейнер
                        Transform parent = transform;
                        colorButtonsContainer = parent.Find("ColorButtonsContainer") as RectTransform;
                        if (colorButtonsContainer == null && parent.childCount > 0)
                        {
                              // Ищем в дочерних объектах
                              for (int i = 0; i < parent.childCount; i++)
                              {
                                    Transform child = parent.GetChild(i);
                                    if (child.name.Contains("ColorButtons") || child.name.Contains("Container"))
                                    {
                                          colorButtonsContainer = child as RectTransform;
                                          if (colorButtonsContainer != null)
                                          {
                                                Debug.Log($"Найден контейнер для кнопок: {colorButtonsContainer.name}");
                                                break;
                                          }
                                    }
                              }
                        }

                        // Если контейнер все еще не найден, создаем его
                        if (colorButtonsContainer == null)
                        {
                              Debug.LogWarning("Контейнер для кнопок не найден. Создаем новый контейнер.");
                              GameObject containerObj = new GameObject("ColorButtonsContainer");
                              containerObj.transform.SetParent(transform, false);
                              colorButtonsContainer = containerObj.AddComponent<RectTransform>();
                              colorButtonsContainer.anchorMin = new Vector2(0, 0);
                              colorButtonsContainer.anchorMax = new Vector2(1, 0.2f);
                              colorButtonsContainer.offsetMin = new Vector2(10, 10);
                              colorButtonsContainer.offsetMax = new Vector2(-10, -10);

                              // Добавляем компонент горизонтальной группы
                              HorizontalLayoutGroup layout = containerObj.AddComponent<HorizontalLayoutGroup>();
                              layout.spacing = 10;
                              layout.childAlignment = TextAnchor.MiddleCenter;
                              layout.childForceExpandWidth = false;
                              layout.childForceExpandHeight = false;

                              Debug.Log("SimplePaintColorSelector: создан новый контейнер для кнопок");
                        }
                  }

                  // Проверяем наличие префаба кнопки
                  if (buttonPrefab == null)
                  {
                        Debug.LogWarning("Префаб кнопки не задан. Попытка создать временный префаб.");
                        buttonPrefab = CreateTemporaryButtonPrefab();

                        if (buttonPrefab == null)
                        {
                              Debug.LogError("Не удалось создать временный префаб кнопки. Кнопки не будут созданы.");
                        }
                        else
                        {
                              Debug.Log("Создан временный префаб кнопки для SimplePaintColorSelector");
                        }
                  }
            }

            /// <summary>
            /// Инициализирует селектор цветов, создавая кнопки для каждого материала
            /// </summary>
            public void Initialize()
            {
                  if (paintMaterials == null || paintMaterials.Length == 0)
                  {
                        Debug.LogWarning("Не заданы материалы для покраски в SimplePaintColorSelector");
                        return;
                  }

                  // Проверяем наличие контейнера и префаба
                  if (colorButtonsContainer == null || buttonPrefab == null)
                  {
                        Debug.LogError("Не задан контейнер для кнопок или префаб кнопки в SimplePaintColorSelector");
                        return;
                  }

                  Debug.Log($"SimplePaintColorSelector: начало инициализации. Материалов: {paintMaterials.Length}, Контейнер: {(colorButtonsContainer != null ? "Задан" : "Не задан")}, Префаб: {(buttonPrefab != null ? "Задан" : "Не задан")}");

                  // Очищаем существующие кнопки, если они есть
                  foreach (var button in colorButtons)
                  {
                        if (button != null && button.gameObject != null)
                        {
#if UNITY_EDITOR
                              if (!Application.isPlaying)
                                    DestroyImmediate(button.gameObject);
                              else
                                    Destroy(button.gameObject);
#else
                              Destroy(button.gameObject);
#endif
                        }
                  }
                  colorButtons.Clear();

                  // Создаем кнопки для каждого материала
                  CreateColorButtons();

                  // Выбираем первый цвет по умолчанию
                  if (colorButtons.Count > 0)
                  {
                        SelectColor(0);
                        Debug.Log($"SimplePaintColorSelector: инициализация завершена. Создано {colorButtons.Count} кнопок.");
                  }
                  else
                  {
                        Debug.LogWarning("SimplePaintColorSelector: не удалось создать кнопки для материалов.");
                  }
            }

            /// <summary>
            /// Создает кнопки для каждого материала краски
            /// </summary>
            private void CreateColorButtons()
            {
                  if (colorButtonsContainer == null || buttonPrefab == null)
                  {
                        Debug.LogError("Не задан контейнер для кнопок или префаб кнопки в SimplePaintColorSelector");
                        return;
                  }

                  Debug.Log($"SimplePaintColorSelector: создание кнопок для {paintMaterials.Length} материалов");

                  for (int i = 0; i < paintMaterials.Length; i++)
                  {
                        Material material = paintMaterials[i];
                        if (material == null)
                        {
                              Debug.LogWarning($"Материал с индексом {i} равен null. Пропускаем создание кнопки.");
                              continue;
                        }

                        GameObject buttonObj = null;
                        try
                        {
#if UNITY_EDITOR
                              if (!UnityEditor.EditorApplication.isPlaying)
                              {
                                    // В режиме редактирования используем PrefabUtility
                                    buttonObj = UnityEditor.PrefabUtility.InstantiatePrefab(buttonPrefab) as GameObject;
                                    if (buttonObj != null)
                                    {
                                          buttonObj.transform.SetParent(colorButtonsContainer, false);
                                    }
                              }
                              else
                              {
                                    buttonObj = Instantiate(buttonPrefab, colorButtonsContainer);
                              }
#else
                              buttonObj = Instantiate(buttonPrefab, colorButtonsContainer);
#endif
                        }
                        catch (System.Exception e)
                        {
                              Debug.LogError($"Ошибка при создании кнопки: {e.Message}");
                              continue;
                        }

                        if (buttonObj == null)
                        {
                              Debug.LogError("Не удалось создать кнопку. Объект равен null.");
                              continue;
                        }

                        SimpleColorButton colorButton = buttonObj.GetComponent<SimpleColorButton>();

                        if (colorButton == null)
                        {
                              colorButton = buttonObj.AddComponent<SimpleColorButton>();
                              Debug.Log($"Добавлен компонент SimpleColorButton к кнопке {i}");
                        }

                        // Настраиваем кнопку
                        colorButton.Setup(material.color, i, SelectColor);
                        colorButtons.Add(colorButton);
                        Debug.Log($"Создана кнопка для материала {material.name} с цветом {material.color}");
                  }
            }

            /// <summary>
            /// Выбирает цвет по индексу
            /// </summary>
            /// <param name="colorIndex">Индекс цвета в массиве материалов</param>
            public void SelectColor(int colorIndex)
            {
                  if (colorIndex < 0 || colorIndex >= paintMaterials.Length)
                        return;

                  selectedColorIndex = colorIndex;

                  // Обновляем визуальное состояние кнопок
                  for (int i = 0; i < colorButtons.Count; i++)
                  {
                        if (colorButtons[i] != null)
                        {
                              // Здесь можно добавить визуальное выделение выбранной кнопки
                              // например, изменить размер или добавить рамку
                        }
                  }

                  // Сообщаем WallPainter о выбранном цвете
                  if (wallPainter != null)
                  {
                        wallPainter.SelectPaintMaterial(colorIndex);
                  }
            }

            /// <summary>
            /// Сбрасывает все материалы стен к исходным
            /// </summary>
            public void ResetWallColors()
            {
                  if (wallPainter != null)
                  {
                        wallPainter.ResetWallMaterials();
                  }
            }

            /// <summary>
            /// Создает временный префаб кнопки для использования в селекторе цветов
            /// </summary>
            private GameObject CreateTemporaryButtonPrefab()
            {
                  GameObject buttonObj = new GameObject("ColorButtonPrefab");

                  // Добавляем RectTransform
                  RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
                  rectTransform.sizeDelta = new Vector2(60, 60);

                  // Добавляем Image
                  Image image = buttonObj.AddComponent<Image>();
                  image.color = Color.white;

                  // Добавляем Button
                  Button button = buttonObj.AddComponent<Button>();
                  ColorBlock colors = button.colors;
                  colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
                  colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
                  button.colors = colors;

                  // Добавляем SimpleColorButton
                  SimpleColorButton colorButton = buttonObj.AddComponent<SimpleColorButton>();

                  return buttonObj;
            }
      }
}