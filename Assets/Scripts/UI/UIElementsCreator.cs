using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Remalux.WallPainting
{
      public class UIElementsCreator : MonoBehaviour
      {
            [Header("UI Prefabs")]
            [SerializeField] private GameObject buttonPrefab;
            [SerializeField] private GameObject textPrefab;
            [SerializeField] private GameObject imagePrefab;

            private Canvas mainCanvas;
            private GameObject texturePanel;

            private void Start()
            {
                  CreateMainCanvas();
                  CreateTextureUI();
            }

            private void CreateMainCanvas()
            {
                  var existingCanvas = Object.FindObjectOfType<Canvas>();
                  if (existingCanvas != null)
                  {
                        mainCanvas = existingCanvas;
                        return;
                  }

                  var canvasObject = new GameObject("MainCanvas");
                  mainCanvas = canvasObject.AddComponent<Canvas>();
                  mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

                  var scaler = canvasObject.AddComponent<CanvasScaler>();
                  scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                  scaler.referenceResolution = new Vector2(1920, 1080);

                  canvasObject.AddComponent<GraphicRaycaster>();
            }

            private void CreateTextureUI()
            {
                  // Создаем панель для текстур
                  texturePanel = CreatePanel("TexturePanel");
                  texturePanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(-350, 0);
                  texturePanel.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 600);

                  // Создаем заголовок
                  var titleText = CreateText("TextureTitle", "Текстуры", texturePanel.transform);
                  titleText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 250);
                  titleText.GetComponent<Text>().fontSize = 24;

                  // Создаем превью текстуры
                  var previewImage = CreateImage("TexturePreview", texturePanel.transform);
                  previewImage.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 150);
                  previewImage.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 200);

                  // Создаем название текстуры
                  var textureName = CreateText("TextureName", "Текстура 1", texturePanel.transform);
                  textureName.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 50);

                  // Создаем кнопки навигации
                  var prevButton = CreateButton("PrevButton", "←", texturePanel.transform);
                  prevButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-70, 0);

                  var nextButton = CreateButton("NextButton", "→", texturePanel.transform);
                  nextButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(70, 0);

                  // Создаем превью цвета
                  var colorPreview = CreateImage("ColorPreview", texturePanel.transform);
                  colorPreview.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);
                  colorPreview.GetComponent<RectTransform>().sizeDelta = new Vector2(50, 50);

                  // Создаем кнопки цветов
                  var colorButtonsHolder = new GameObject("ColorButtons", typeof(RectTransform));
                  colorButtonsHolder.transform.SetParent(texturePanel.transform, false);
                  colorButtonsHolder.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -150);

                  float buttonSpacing = 60f;
                  float startX = -(buttonSpacing * 2); // Для 5 кнопок

                  var colorButtons = new Button[5];
                  for (int i = 0; i < 5; i++)
                  {
                        var colorButton = CreateButton($"ColorButton_{i}", "", colorButtonsHolder.transform);
                        var rectTransform = colorButton.GetComponent<RectTransform>();
                        rectTransform.anchoredPosition = new Vector2(startX + (buttonSpacing * i), 0);
                        rectTransform.sizeDelta = new Vector2(50, 50);
                        colorButtons[i] = colorButton.GetComponent<Button>();
                  }

                  // Добавляем компонент TextureUI
                  var textureUI = texturePanel.AddComponent<TextureUI>();
                  textureUI.GetType().GetField("previousButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                      ?.SetValue(textureUI, prevButton.GetComponent<Button>());
                  textureUI.GetType().GetField("nextButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                      ?.SetValue(textureUI, nextButton.GetComponent<Button>());
                  textureUI.GetType().GetField("previewImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                      ?.SetValue(textureUI, previewImage.GetComponent<Image>());
                  textureUI.GetType().GetField("textureName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                      ?.SetValue(textureUI, textureName.GetComponent<Text>());
                  textureUI.GetType().GetField("colorPreview", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                      ?.SetValue(textureUI, colorPreview.GetComponent<Image>());
                  textureUI.GetType().GetField("colorButtons", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                      ?.SetValue(textureUI, colorButtons);
            }

            private GameObject CreatePanel(string name)
            {
                  var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                  panel.transform.SetParent(mainCanvas.transform, false);

                  var image = panel.GetComponent<Image>();
                  image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

                  return panel;
            }

            private GameObject CreateButton(string name, string text, Transform parent)
            {
                  GameObject buttonObj;
                  if (buttonPrefab != null)
                  {
                        buttonObj = Instantiate(buttonPrefab, parent);
                  }
                  else
                  {
                        buttonObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                        buttonObj.transform.SetParent(parent, false);

                        var buttonImage = buttonObj.GetComponent<Image>();
                        buttonImage.color = Color.white;

                        var textObj = CreateText($"{name}Text", text, buttonObj.transform);
                        textObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                  }

                  buttonObj.name = name;
                  buttonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 40);

                  return buttonObj;
            }

            private GameObject CreateText(string name, string content, Transform parent)
            {
                  GameObject textObj;
                  if (textPrefab != null)
                  {
                        textObj = Instantiate(textPrefab, parent);
                  }
                  else
                  {
                        textObj = new GameObject(name, typeof(RectTransform), typeof(Text));
                        textObj.transform.SetParent(parent, false);

                        var text = textObj.GetComponent<Text>();
                        text.text = content;
                        text.color = Color.white;
                        text.alignment = TextAnchor.MiddleCenter;
                        text.fontSize = 18;
                  }

                  textObj.name = name;
                  textObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 30);

                  return textObj;
            }

            private GameObject CreateImage(string name, Transform parent)
            {
                  GameObject imageObj;
                  if (imagePrefab != null)
                  {
                        imageObj = Instantiate(imagePrefab, parent);
                  }
                  else
                  {
                        imageObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                        imageObj.transform.SetParent(parent, false);

                        var image = imageObj.GetComponent<Image>();
                        image.color = Color.white;
                  }

                  imageObj.name = name;

                  return imageObj;
            }
      }
}