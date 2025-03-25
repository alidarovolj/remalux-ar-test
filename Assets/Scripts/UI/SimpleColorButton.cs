using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Remalux.AR
{
      [RequireComponent(typeof(Button), typeof(Image))]
      public class SimpleColorButton : MonoBehaviour
      {
            [Header("Настройки цвета")]
            [SerializeField] private Color buttonColor = Color.white;

            [Header("События")]
            public UnityEvent<Color> onColorSelected;

            private Button button;
            private Image image;

            private void Awake()
            {
                  // Получаем компоненты
                  button = GetComponent<Button>();
                  image = GetComponent<Image>();

                  // Настраиваем кнопку
                  if (button != null)
                  {
                        button.onClick.AddListener(OnButtonClick);
                  }

                  // Применяем цвет
                  UpdateButtonColor();
            }

            private void OnValidate()
            {
                  UpdateButtonColor();
            }

            private void UpdateButtonColor()
            {
                  if (image != null)
                  {
                        image.color = buttonColor;
                  }
            }

            public void SetColor(Color newColor)
            {
                  buttonColor = newColor;
                  UpdateButtonColor();
            }

            private void OnButtonClick()
            {
                  onColorSelected?.Invoke(buttonColor);
            }

            private void OnDestroy()
            {
                  if (button != null)
                  {
                        button.onClick.RemoveListener(OnButtonClick);
                  }
            }
      }
}