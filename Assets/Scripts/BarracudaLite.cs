using UnityEngine;

#if !USE_BARRACUDA_INDIRECTLY
using Unity.Barracuda;
#endif

namespace Remalux
{
    /// <summary>
    /// Light wrapper around Barracuda to avoid circular references in the build system.
    /// Use this class instead of direct Barracuda references when possible.
    /// </summary>
    public class BarracudaLite : MonoBehaviour
    {
        /// <summary>
        /// Load a model from a NNModel asset
        /// </summary>
        /// <param name="modelAsset">The model asset to load</param>
        /// <returns>True if successful</returns>
        public static bool TryLoadModel(Object modelAsset, out object model)
        {
            model = null;
            
#if !USE_BARRACUDA_INDIRECTLY
            try
            {
                if (modelAsset is NNModel nnModel)
                {
                    model = ModelLoader.Load(nnModel);
                    return model != null;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error loading model: {e.Message}");
            }
#endif
            
            return false;
        }
        
        /// <summary>
        /// Check if Barracuda is properly referenced
        /// </summary>
        public static bool IsBarracudaAvailable()
        {
#if !USE_BARRACUDA_INDIRECTLY
            return true;
#else
            return false;
#endif
        }
    }
} 