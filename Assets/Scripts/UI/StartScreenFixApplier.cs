using UnityEngine;

namespace Remalux.AR
{
    /// <summary>
    /// Автоматически добавляет компонент StartScreenFix в сцену
    /// </summary>
    public static class StartScreenFixApplier
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            Debug.Log("StartScreenFixApplier: Инициализация исправления начального экрана");
            
            // Создаем объект с компонентом StartScreenFix
            GameObject fixObj = new GameObject("StartScreenFix");
            fixObj.AddComponent<StartScreenFix>();
            Object.DontDestroyOnLoad(fixObj);
        }
    }
} 