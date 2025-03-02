using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Remalux.AR
{
      /// <summary>
      /// Простой компонент для кнопки выбора цвета
      /// </summary>
      public class SimpleColorButton : MonoBehaviour
      {
            [SerializeField] private Image colorImage;
            [SerializeField] private Button button;

            private int colorIndex;

            public void Setup(Color color, int index, UnityAction<int> onClickCallback)
            {
                  if (colorImage == null)
                        colorImage = GetComponent<Image>();

                  if (button == null)
                        button = GetComponent<Button>();

                  colorIndex = index;

                  if (colorImage != null)
                        colorImage.color = color;

                  if (button != null)
                  {
                        button.onClick.RemoveAllListeners();
                        button.onClick.AddListener(() => onClickCallback?.Invoke(colorIndex));
                  }
            }
      }
}