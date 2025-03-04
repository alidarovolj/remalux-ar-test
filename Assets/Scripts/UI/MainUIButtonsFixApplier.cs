using UnityEngine;

namespace Remalux.AR
{
    /// <summary>
    /// Автоматически добавляет компонент MainUIButtonsFix в сцену
    /// </summary>
    public static class MainUIButtonsFixApplier
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            Debug.Log("MainUIButtonsFixApplier: Инициализация исправления кнопок UI");
            
            // Ждем один кадр, чтобы все компоненты успели инициализироваться
            GameObject delayedInitObj = new GameObject("DelayedButtonsFixInitializer");
            DelayedInitializer initializer = delayedInitObj.AddComponent<DelayedInitializer>();
            Object.DontDestroyOnLoad(delayedInitObj);
        }
        
        /// <summary>
        /// Вспомогательный класс для отложенной инициализации
        /// </summary>
        private class DelayedInitializer : MonoBehaviour
        {
            private void Start()
            {
                // Запускаем корутину для отложенной инициализации
                StartCoroutine(DelayedInit());
            }
            
            private System.Collections.IEnumerator DelayedInit()
            {
                // Ждем один кадр
                yield return null;
                
                // Добавляем компонент MainUIButtonsFix, если его еще нет
                if (FindObjectOfType<MainUIButtonsFix>() == null)
                {
                    Debug.Log("MainUIButtonsFixApplier: Добавляем MainUIButtonsFix в сцену");
                    GameObject fixObj = new GameObject("MainUIButtonsFix");
                    fixObj.AddComponent<MainUIButtonsFix>();
                }
                
                // Удаляем этот объект, так как он больше не нужен
                Destroy(gameObject);
            }
        }
    }
} 