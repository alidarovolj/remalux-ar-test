using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// This script forces a restart of PlayMode to clear any issues from the previous session.
/// Add it to any GameObject in the scene and it will restart PlayMode once.
/// </summary>
public class RestartPlayMode : MonoBehaviour
{
      private static bool hasRestarted = false;

      private void Start()
      {
#if UNITY_EDITOR
        if (!hasRestarted)
        {
            hasRestarted = true;
            Debug.Log("Restarting PlayMode to clear any issues from previous session...");
            EditorApplication.isPlaying = false;
        }
        else
        {
            Debug.Log("PlayMode has been restarted successfully.");
            // Remove this component after restart
            Destroy(this);
        }
#else
            Destroy(this);
#endif
      }
}
