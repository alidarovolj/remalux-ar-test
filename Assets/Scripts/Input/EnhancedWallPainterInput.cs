using UnityEngine;

namespace Remalux.AR.Input
{
      /// <summary>
      /// Enhanced input handler for wall painting with additional features
      /// </summary>
      public class EnhancedWallPainterInput : MonoBehaviour
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
                        Debug.LogError("EnhancedWallPainterInput: WallPainter component not found!");
                        enabled = false;
                        return;
                  }

                  // Get paint method via reflection
                  paintMethod = wallPainter.GetType().GetMethod("PaintWallAtPosition");
                  if (paintMethod == null)
                  {
                        Debug.LogError("EnhancedWallPainterInput: PaintWallAtPosition method not found!");
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
                              Debug.Log($"EnhancedWallPainterInput: Mouse click at {mousePos}");
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
                                    Debug.Log($"EnhancedWallPainterInput: Touch at {touch.position}");
                              }
                              paintMethod.Invoke(wallPainter, new object[] { touch.position });
                        }
                  }
            }
      }
}