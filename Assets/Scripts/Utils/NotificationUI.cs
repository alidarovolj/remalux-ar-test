using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Remalux.AR
{
      /// <summary>
      /// Управляет отображением всплывающих уведомлений
      /// </summary>
      public class NotificationUI : MonoBehaviour
      {
            [Header("Настройки анимации")]
            [SerializeField] private float fadeInDuration = 0.3f;
            [SerializeField] private float showDuration = 2.0f;
            [SerializeField] private float fadeOutDuration = 0.5f;

            [Header("Компоненты")]
            [SerializeField] private CanvasGroup canvasGroup;
            [SerializeField] private Text messageText;
            [SerializeField] private Image backgroundImage;

            private void Awake()
            {
                  // Получаем компоненты, если они не были установлены
                  if (canvasGroup == null)
                        canvasGroup = GetComponent<CanvasGroup>();

                  if (messageText == null)
                        messageText = GetComponentInChildren<Text>();

                  if (backgroundImage == null)
                        backgroundImage = GetComponent<Image>();

                  // Инициализируем начальное состояние
                  if (canvasGroup != null)
                        canvasGroup.alpha = 0f;
            }

            private void Start()
            {
                  // Запускаем анимацию отображения
                  StartCoroutine(ShowNotificationSequence());
            }

            /// <summary>
            /// Устанавливает текст уведомления
            /// </summary>
            public void SetMessage(string message)
            {
                  if (messageText != null)
                  {
                        messageText.text = message;
                  }
            }

            /// <summary>
            /// Устанавливает цвет фона уведомления
            /// </summary>
            public void SetBackgroundColor(Color color)
            {
                  if (backgroundImage != null)
                  {
                        backgroundImage.color = color;
                  }
            }

            /// <summary>
            /// Последовательность анимации для отображения уведомления
            /// </summary>
            private IEnumerator ShowNotificationSequence()
            {
                  // Фейд-ин
                  yield return FadeIn();

                  // Отображение
                  yield return new WaitForSeconds(showDuration);

                  // Фейд-аут
                  yield return FadeOut();

                  // Удаляем объект после завершения анимации
                  Destroy(gameObject);
            }

            /// <summary>
            /// Анимация появления уведомления
            /// </summary>
            private IEnumerator FadeIn()
            {
                  float elapsedTime = 0f;

                  while (elapsedTime < fadeInDuration)
                  {
                        if (canvasGroup != null)
                        {
                              canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInDuration);
                        }

                        elapsedTime += Time.deltaTime;
                        yield return null;
                  }

                  if (canvasGroup != null)
                  {
                        canvasGroup.alpha = 1f;
                  }
            }

            /// <summary>
            /// Анимация исчезновения уведомления
            /// </summary>
            private IEnumerator FadeOut()
            {
                  float elapsedTime = 0f;

                  while (elapsedTime < fadeOutDuration)
                  {
                        if (canvasGroup != null)
                        {
                              canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);
                        }

                        elapsedTime += Time.deltaTime;
                        yield return null;
                  }

                  if (canvasGroup != null)
                  {
                        canvasGroup.alpha = 0f;
                  }
            }
      }
}