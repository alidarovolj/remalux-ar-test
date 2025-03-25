using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Remalux.AR
{
      public class ColorButton : MonoBehaviour
      {
            [SerializeField] private Image colorImage;
            [SerializeField] private Button button;
            [SerializeField] private Color buttonColor = Color.white;

            public UnityEvent<Color> onColorSelected = new UnityEvent<Color>();

            private void Awake()
            {
                  if (colorImage == null)
                        colorImage = GetComponent<Image>();

                  if (button == null)
                        button = GetComponent<Button>();

                  if (button != null)
                        button.onClick.AddListener(OnButtonClick);
            }

            private void Start()
            {
                  if (colorImage != null)
                        colorImage.color = buttonColor;
            }

            private void OnButtonClick()
            {
                  onColorSelected.Invoke(buttonColor);
            }

            public void SetColor(Color color)
            {
                  buttonColor = color;
                  if (colorImage != null)
                        colorImage.color = color;
            }

            public Color GetColor()
            {
                  return buttonColor;
            }
      }
}