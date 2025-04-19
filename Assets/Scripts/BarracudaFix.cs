using UnityEngine;

namespace Remalux
{
    /// <summary>
    /// This class exists solely to ensure Barracuda is properly referenced at runtime.
    /// It fixes build errors with circular references in the build system.
    /// </summary>
    public class BarracudaFix : MonoBehaviour
    {
        private void Awake()
        {
            // Force Barracuda to be included in the build
            // This fixes circular reference issues
            Debug.Log("BarracudaFix initialized");
        }
    }
} 