using UnityEngine;

namespace Remalux.AR
{
    /// <summary>
    /// Автоматически добавляет компонент UIInteractionFix в сцену при запуске
    /// </summary>
    [DefaultExecutionOrder(-100)] // Выполняется раньше других скриптов
    public class UIFixApplier : MonoBehaviour
    {
        private static UIFixApplier instance;
        
        private void Awake()
        {
            // Singleton pattern
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Добавляем компонент UIInteractionFix, если его еще нет
            if (FindObjectOfType<UIInteractionFix>() == null)
            {
                Debug.Log("UIFixApplier: Добавляем UIInteractionFix в сцену");
                GameObject fixObj = new GameObject("UIInteractionFix");
                fixObj.AddComponent<UIInteractionFix>();
                DontDestroyOnLoad(fixObj);
            }
        }
    }
} 