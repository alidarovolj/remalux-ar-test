using UnityEngine;

namespace Remalux.AR
{
      public class DirectInputHandler : MonoBehaviour
      {
            [Header("Input Settings")]
            public bool enableMouseInput = true;
            public bool enableTouchInput = true;
            public bool logInputEvents = true;

            private WallPainter wallPainter;

            private void Start()
            {
                  // Find WallPainter in scene
                  wallPainter = FindObjectOfType<WallPainter>();
                  if (wallPainter == null)
                  {
                        Debug.LogError("DirectInputHandler: WallPainter component not found in scene!");
                        enabled = false;
                        return;
                  }
                  Debug.Log($"DirectInputHandler: Found WallPainter on {wallPainter.gameObject.name}");
            }

            private void Update()
            {
                  if (wallPainter == null) return;

                  // Handle mouse input
                  if (enableMouseInput && UnityEngine.Input.GetMouseButton(0))
                  {
                        Vector2 mousePos = UnityEngine.Input.mousePosition;
                        if (logInputEvents)
                        {
                              Debug.Log($"DirectInputHandler: Mouse click at {mousePos}");
                        }
                        wallPainter.PaintWallAtPosition(mousePos);
                  }

                  // Handle touch input
                  if (enableTouchInput && UnityEngine.Input.touchCount > 0)
                  {
                        Touch touch = UnityEngine.Input.GetTouch(0);
                        if (touch.phase == TouchPhase.Began)
                        {
                              if (logInputEvents)
                              {
                                    Debug.Log($"DirectInputHandler: Touch at {touch.position}");
                              }
                              wallPainter.PaintWallAtPosition(touch.position);
                        }
                  }
            }
      }
}