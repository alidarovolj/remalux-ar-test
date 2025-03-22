using UnityEngine;

namespace Remalux.AR
{
    /// <summary>
    /// Автоматически добавляет компонент ModalDialogFix в сцену
    /// </summary>
    public static class ModalDialogFixApplier
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            Debug.Log("ModalDialogFixApplier: Инициализация исправления модального диалога");
            
            // Ждем один кадр, чтобы все компоненты успели инициализироваться
            GameObject delayedInitObj = new GameObject("DelayedModalFixInitializer");
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
                // Ждем два кадра, чтобы все остальные компоненты успели инициализироваться
                yield return null;
                yield return null;
                
                // Добавляем компонент ModalDialogFix, если его еще нет
                if (FindObjectOfType<ModalDialogFix>() == null)
                {
                    Debug.Log("ModalDialogFixApplier: Добавляем ModalDialogFix в сцену");
                    GameObject fixObj = new GameObject("ModalDialogFix");
                    fixObj.AddComponent<ModalDialogFix>();
                }
                
                // Удаляем этот объект, так как он больше не нужен
                Destroy(gameObject);
            }
        }
    }
} 