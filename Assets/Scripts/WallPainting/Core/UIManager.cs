using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Remalux.WallPainting.Core
{
      public class UIManager : MonoBehaviour
      {
            [Header("Debug UI")]
            public GameObject debugPanel;
            public GameObject orangeOverlayPanel;

            void Start()
            {
                  // Disable any debug panels
                  if (debugPanel != null)
                  {
                        debugPanel.SetActive(false);
                  }

                  if (orangeOverlayPanel != null)
                  {
                        orangeOverlayPanel.SetActive(false);
                  }

                  // Clear OpenCV debug panels
                  OpenCVForUnity.UnityUtils.DebugMatUtils.clear();

                  // Find and disable any UI panels with orange color
                  DisableOrangeUIElements();
            }

            void Update()
            {
                  // Periodically check and remove debug panels
                  if (Time.frameCount % 60 == 0)
                  {
                        OpenCVForUnity.UnityUtils.DebugMatUtils.clear();
                        DisableOrangeUIElements();
                  }
            }

            // Find and disable any orange UI elements
            private void DisableOrangeUIElements()
            {
                  // Find all Image components
                  Image[] images = FindObjectsOfType<Image>();
                  foreach (Image img in images)
                  {
                        // Check if the color is orange (r ~= 1, g ~= 0.5, b ~= 0)
                        Color c = img.color;
                        if (c.r > 0.9f && c.g > 0.4f && c.g < 0.6f && c.b < 0.1f)
                        {
                              // Found an orange panel, disable it
                              Debug.Log("Found orange UI panel: " + img.gameObject.name);
                              img.gameObject.SetActive(false);

                              // If it has a parent with a button, disable the parent too
                              Transform parent = img.transform.parent;
                              if (parent != null && parent.GetComponentInChildren<Button>() != null)
                              {
                                    Debug.Log("Disabling parent panel with button: " + parent.gameObject.name);
                                    parent.gameObject.SetActive(false);
                              }
                        }
                  }
            }
      }
}