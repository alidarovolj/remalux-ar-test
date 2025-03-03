using UnityEngine;

namespace Remalux.AR.Input
{
      /// <summary>
      /// Handles direct input for wall painting using mouse and touch input
      /// </summary>
      public class DirectInputHandler : MonoBehaviour
      {
            [Header("Input Settings")]
            public bool enableMouseInput = true;
            public bool enableTouchInput = true;
            public bool logInputEvents = true;

            private MonoBehaviour wallPainter;
            private System.Reflection.MethodInfo paintMethod;

            private void Start()
            {
                  // Find WallPainter component
                  wallPainter = GetComponent<MonoBehaviour>();
                  if (wallPainter == null || wallPainter.GetType().Name != "WallPainter")
                  {
                        wallPainter = FindObjectOfType<MonoBehaviour>();
                        if (wallPainter == null || wallPainter.GetType().Name != "WallPainter")
                        {
                              Debug.LogError("DirectInputHandler: WallPainter component not found!");
                              enabled = false;
                              return;
                        }
                  }

                  // Get paint method via reflection
                  paintMethod = wallPainter.GetType().GetMethod("PaintWallAtPosition");
                  if (paintMethod == null)
                  {
                        Debug.LogError("DirectInputHandler: PaintWallAtPosition method not found!");
                        enabled = false;
                        return;
                  }
            }

            private void Update()
            {
                  if (wallPainter == null || paintMethod == null) return;

                  // Handle mouse input
                  if (enableMouseInput && UnityEngine.Input.GetMouseButtonDown(0))
                  {
                        Vector2 mousePos = UnityEngine.Input.mousePosition;
                        if (logInputEvents)
                        {
                              Debug.Log($"DirectInputHandler: Mouse click at {mousePos}");
                        }
                        paintMethod.Invoke(wallPainter, new object[] { mousePos });
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
                              paintMethod.Invoke(wallPainter, new object[] { touch.position });
                        }
                  }
            }
      }
}