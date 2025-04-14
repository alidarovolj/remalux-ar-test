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
                  var existingCanvas = Object.FindFirstObjectByType<Canvas>();
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
                  // Create texture panel
                  texturePanel = CreatePanel("TexturePanel");
                  texturePanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(-350, 0);
                  texturePanel.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 600);

                  // Create title
                  var titleText = CreateText("TextureTitle", "Textures", texturePanel.transform);
                  titleText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 250);
                  titleText.GetComponent<Text>().fontSize = 24;

                  // Create texture preview
                  var previewImage = CreateImage("TexturePreview", texturePanel.transform);
                  previewImage.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 150);
                  previewImage.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 200);

                  // Create texture name
                  var textureName = CreateText("TextureName", "Texture 1", texturePanel.transform);
                  textureName.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 50);

                  // Create navigation buttons
                  var prevButton = CreateButton("PrevButton", "←", texturePanel.transform);
                  prevButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-70, 0);

                  var nextButton = CreateButton("NextButton", "→", texturePanel.transform);
                  nextButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(70, 0);

                  // Create color preview
                  var colorPreview = CreateImage("ColorPreview", texturePanel.transform);
                  colorPreview.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);
                  colorPreview.GetComponent<RectTransform>().sizeDelta = new Vector2(50, 50);

                  // Create color buttons
                  var colorButtonsHolder = new GameObject("ColorButtons", typeof(RectTransform));
                  colorButtonsHolder.transform.SetParent(texturePanel.transform, false);
                  colorButtonsHolder.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -150);

                  float buttonSpacing = 60f;
                  float startX = -(buttonSpacing * 2); // For 5 buttons

                  var colorButtons = new Button[5];
                  for (int i = 0; i < 5; i++)
                  {
                        var colorButton = CreateButton($"ColorButton_{i}", "", colorButtonsHolder.transform);
                        var rectTransform = colorButton.GetComponent<RectTransform>();
                        rectTransform.anchoredPosition = new Vector2(startX + (buttonSpacing * i), 0);
                        rectTransform.sizeDelta = new Vector2(50, 50);
                        colorButtons[i] = colorButton.GetComponent<Button>();
                  }

                  // Add TextureUI component
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
                  var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
                  panel.transform.SetParent(mainCanvas.transform, false);
                  panel.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                  return panel;
            }

            private GameObject CreateButton(string name, string text, Transform parent)
            {
                  var button = Instantiate(buttonPrefab, parent);
                  button.name = name;
                  button.GetComponentInChildren<Text>().text = text;
                  return button;
            }

            private GameObject CreateText(string name, string content, Transform parent)
            {
                  var text = Instantiate(textPrefab, parent);
                  text.name = name;
                  text.GetComponent<Text>().text = content;
                  return text;
            }

            private GameObject CreateImage(string name, Transform parent)
            {
                  var image = Instantiate(imagePrefab, parent);
                  image.name = name;
                  return image;
            }
      }
}