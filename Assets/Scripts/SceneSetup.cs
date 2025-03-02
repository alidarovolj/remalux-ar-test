using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Remalux.AR
{
      public class SceneSetup : MonoBehaviour
      {
            [Header("Prefabs")]
            public GameObject buttonPrefab; // Простая кнопка UI
            public GameObject colorPreviewPrefab; // Простой объект для предпросмотра

            [Header("Materials")]
            public Material defaultWallMaterial;
            public Material[] paintMaterials;

            [Header("Scene References")]
            public Camera mainCamera;
            public Canvas mainCanvas;

            [Header("Wall Layer")]
            public LayerMask wallLayerMask;

            private WallPainter wallPainter;
            private SimplePaintColorSelector colorSelector;
            private WallPaintingManager paintingManager;

            private void Awake()
            {
                  if (mainCamera == null)
                        mainCamera = Camera.main;

                  if (mainCanvas == null)
                        mainCanvas = FindObjectOfType<Canvas>();

                  SetupScene();
            }

            public void SetupScene()
            {
                  // Проверяем наличие необходимых материалов
                  if (defaultWallMaterial == null)
                  {
                        Debug.LogError("Default Wall Material не задан в SceneSetup. Система покраски не будет работать корректно.");
                  }

                  if (paintMaterials == null || paintMaterials.Length == 0)
                  {
                        Debug.LogError("Paint Materials не заданы в SceneSetup. Система покраски не будет работать корректно.");
                  }

                  // Создаем WallPainter
                  GameObject wallPainterObj = new GameObject("WallPainter");
                  wallPainter = wallPainterObj.AddComponent<WallPainter>();

                  // Настраиваем WallPainter
                  wallPainter.defaultMaterial = defaultWallMaterial;
                  wallPainter.availablePaints = paintMaterials;
                  wallPainter.mainCamera = mainCamera;
                  wallPainter.wallLayerMask = wallLayerMask;
                  wallPainter.colorPreviewPrefab = colorPreviewPrefab;

                  // Настраиваем UI
                  SetupUI();

                  // Создаем WallPaintingManager
                  GameObject managerObj = new GameObject("WallPaintingManager");
                  paintingManager = managerObj.AddComponent<WallPaintingManager>();

                  // Настраиваем WallPaintingManager
                  paintingManager.wallPainter = wallPainter;
                  paintingManager.colorSelector = colorSelector;
                  paintingManager.paintMaterials = paintMaterials;
                  paintingManager.defaultWallMaterial = defaultWallMaterial;

                  // Если есть UI для покраски, настраиваем его
                  if (colorSelector != null)
                  {
                        paintingManager.paintingUI = colorSelector.gameObject;
                  }

                  // Проверяем, что все компоненты настроены правильно
                  Debug.Log("Система покраски стен настроена. Материалы: " +
                           (paintingManager.paintMaterials != null ? paintingManager.paintMaterials.Length.ToString() : "null") +
                           ", Default: " + (paintingManager.defaultWallMaterial != null ? "Set" : "null"));
            }

            private void SetupUI()
            {
                  if (mainCanvas == null)
                  {
                        Debug.LogWarning("MainCanvas не задан в SceneSetup. Попытка найти Canvas в сцене.");
                        mainCanvas = FindObjectOfType<Canvas>();
                        if (mainCanvas == null)
                        {
                              Debug.LogWarning("Canvas не найден в сцене. Создаем новый Canvas.");
                              GameObject canvasObj = new GameObject("Canvas");
                              mainCanvas = canvasObj.AddComponent<Canvas>();
                              mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                              canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                              canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                        }
                  }

                  // Создаем панель UI
                  GameObject paintingUIObj = new GameObject("PaintingUI");
                  paintingUIObj.transform.SetParent(mainCanvas.transform, false);

                  // Добавляем компонент RectTransform
                  RectTransform rectTransform = paintingUIObj.AddComponent<RectTransform>();
                  rectTransform.anchorMin = new Vector2(0, 0);
                  rectTransform.anchorMax = new Vector2(1, 1);
                  rectTransform.offsetMin = Vector2.zero;
                  rectTransform.offsetMax = Vector2.zero;

                  // Создаем контейнер для кнопок цветов
                  GameObject colorButtonsContainer = new GameObject("ColorButtonsContainer");
                  colorButtonsContainer.transform.SetParent(paintingUIObj.transform, false);

                  RectTransform containerRect = colorButtonsContainer.AddComponent<RectTransform>();
                  containerRect.anchorMin = new Vector2(0, 0);
                  containerRect.anchorMax = new Vector2(1, 0.2f);
                  containerRect.offsetMin = new Vector2(10, 10);
                  containerRect.offsetMax = new Vector2(-10, -10);

                  // Добавляем компонент горизонтальной группы
                  HorizontalLayoutGroup layout = colorButtonsContainer.AddComponent<HorizontalLayoutGroup>();
                  layout.spacing = 10;
                  layout.childAlignment = TextAnchor.MiddleCenter;
                  layout.childForceExpandWidth = false;
                  layout.childForceExpandHeight = false;

                  // Создаем кнопку сброса
                  GameObject resetButtonObj = new GameObject("ResetButton");
                  resetButtonObj.transform.SetParent(paintingUIObj.transform, false);

                  RectTransform resetRect = resetButtonObj.AddComponent<RectTransform>();
                  resetRect.anchorMin = new Vector2(0.5f, 0.8f);
                  resetRect.anchorMax = new Vector2(0.5f, 0.9f);
                  resetRect.sizeDelta = new Vector2(160, 40);

                  Image resetImage = resetButtonObj.AddComponent<Image>();
                  resetImage.color = Color.white;

                  Button resetButton = resetButtonObj.AddComponent<Button>();

                  // Добавляем текст на кнопку
                  GameObject resetTextObj = new GameObject("Text");
                  resetTextObj.transform.SetParent(resetButtonObj.transform, false);

                  RectTransform textRect = resetTextObj.AddComponent<RectTransform>();
                  textRect.anchorMin = Vector2.zero;
                  textRect.anchorMax = Vector2.one;
                  textRect.offsetMin = Vector2.zero;
                  textRect.offsetMax = Vector2.zero;

                  Text resetText = resetTextObj.AddComponent<Text>();
                  resetText.text = "Сбросить";
                  resetText.alignment = TextAnchor.MiddleCenter;
                  resetText.color = Color.black;
                  resetText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

                  // Создаем префаб кнопки, если его нет
                  if (buttonPrefab == null)
                  {
                        Debug.LogWarning("Префаб кнопки не задан в SceneSetup. Создаем временный префаб.");
                        buttonPrefab = CreateColorButtonPrefab();
                  }

                  // Добавляем селектор цветов
                  colorSelector = paintingUIObj.AddComponent<SimplePaintColorSelector>();

                  // Проверяем, что все необходимые компоненты доступны
                  if (wallPainter == null)
                  {
                        Debug.LogWarning("WallPainter не задан в SceneSetup. Попытка найти WallPainter в сцене.");
                        wallPainter = FindObjectOfType<WallPainter>();
                  }

                  if (paintMaterials == null || paintMaterials.Length == 0)
                  {
                        Debug.LogError("PaintMaterials не заданы в SceneSetup. Система покраски не будет работать корректно.");
                  }

                  // Настраиваем SimplePaintColorSelector
                  colorSelector.paintMaterials = paintMaterials;
                  colorSelector.wallPainter = wallPainter;
                  colorSelector.resetButton = resetButton;
                  colorSelector.colorButtonsContainer = containerRect;
                  colorSelector.buttonPrefab = buttonPrefab;

                  Debug.Log($"Настройка UI завершена. Контейнер для кнопок: {(containerRect != null ? "Создан" : "Не создан")}, Префаб кнопки: {(buttonPrefab != null ? "Задан" : "Не задан")}");

                  // Вызываем Initialize() для создания кнопок
#if UNITY_EDITOR
                  // В режиме редактирования не вызываем Initialize() автоматически,
                  // так как он будет вызван из WallPaintingSetupWindow
                  if (!UnityEditor.EditorApplication.isPlaying)
                  {
                        // Просто настраиваем компоненты без создания кнопок
                        return;
                  }
#endif
                  colorSelector.Initialize();
            }

            // Метод для создания временного префаба кнопки
            private GameObject CreateColorButtonPrefab()
            {
                  GameObject buttonObj = new GameObject("ColorButton");

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
                  buttonObj.AddComponent<SimpleColorButton>();

                  return buttonObj;
            }
      }
}