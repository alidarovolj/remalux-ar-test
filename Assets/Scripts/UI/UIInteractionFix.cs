using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Remalux.AR
{
    /// <summary>
    /// Скрипт для исправления проблем с взаимодействием UI
    /// </summary>
    public class UIInteractionFix : MonoBehaviour
    {
        [SerializeField] private Canvas mainCanvas;
        
        private void Awake()
        {
            // Проверяем и исправляем проблемы с UI при запуске
            FixUIInteraction();
        }
        
        private void Start()
        {
            // Повторная проверка после Start для случаев, когда другие скрипты могли изменить UI
            FixUIInteraction();
        }
        
        /// <summary>
        /// Исправляет проблемы с взаимодействием UI
        /// </summary>
        public void FixUIInteraction()
        {
            // Находим основной Canvas, если не назначен
            if (mainCanvas == null)
            {
                mainCanvas = FindObjectOfType<Canvas>();
                if (mainCanvas == null)
                {
                    Debug.LogError("UIInteractionFix: Canvas не найден!");
                    return;
                }
            }
            
            // Проверяем наличие GraphicRaycaster на Canvas
            GraphicRaycaster raycaster = mainCanvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                Debug.Log("UIInteractionFix: Добавляем GraphicRaycaster на Canvas");
                raycaster = mainCanvas.gameObject.AddComponent<GraphicRaycaster>();
            }
            
            // Проверяем наличие EventSystem
            EventSystem eventSystem = FindObjectOfType<EventSystem>();
            if (eventSystem == null)
            {
                Debug.Log("UIInteractionFix: Создаем EventSystem");
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystem = eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
            }
            
            // Проверяем, что Canvas находится в режиме ScreenSpaceOverlay
            if (mainCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                Debug.Log("UIInteractionFix: Устанавливаем режим Canvas в ScreenSpaceOverlay");
                mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            
            // Проверяем, что Canvas имеет правильный порядок сортировки
            mainCanvas.sortingOrder = 100;
            
            // Проверяем все кнопки на Canvas
            Button[] buttons = mainCanvas.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                // Проверяем, что у кнопки есть обработчики событий
                if (button.onClick.GetPersistentEventCount() == 0)
                {
                    Debug.LogWarning($"UIInteractionFix: Кнопка {button.name} не имеет обработчиков событий!");
                }
                
                // Проверяем, что кнопка интерактивна
                if (!button.interactable)
                {
                    Debug.Log($"UIInteractionFix: Кнопка {button.name} не интерактивна, включаем взаимодействие");
                    button.interactable = true;
                }
            }
            
            Debug.Log("UIInteractionFix: Проверка и исправление UI завершены");
        }
    }
} 