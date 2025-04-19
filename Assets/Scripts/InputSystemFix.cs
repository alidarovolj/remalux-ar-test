using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Remalux
{
    /// <summary>
    /// This class ensures that InputSystem is properly referenced.
    /// </summary>
    public class InputSystemFix : MonoBehaviour
    {
        private void Awake()
        {
            Debug.Log("InputSystemFix initialized");
            
            // Force Unity to resolve InputSystem dependencies
#if ENABLE_INPUT_SYSTEM
            var actions = ScriptableObject.CreateInstance<InputActionAsset>();
            Debug.Log("InputActionAsset created: " + (actions != null));
            if (actions != null)
            {
                Destroy(actions);
            }
#endif
        }
    }
} 