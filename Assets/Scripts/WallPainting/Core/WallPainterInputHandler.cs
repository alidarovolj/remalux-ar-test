using UnityEngine;

namespace Remalux.AR
{
      public class WallPainterInputHandler : MonoBehaviour
      {
            [SerializeField] private WallPainter wallPainter;
            [SerializeField] private Camera mainCamera;
            [SerializeField] private LayerMask wallLayerMask;
            [SerializeField] private float raycastDistance = 10f;

            private void Start()
            {
                  if (wallPainter == null)
                        wallPainter = FindObjectOfType<WallPainter>();

                  if (mainCamera == null)
                        mainCamera = Camera.main;

                  if (wallPainter == null || mainCamera == null)
                  {
                        Debug.LogError("Required components not found!");
                        enabled = false;
                        return;
                  }
            }

            private void Update()
            {
                  // Handle mouse input
                  if (Input.GetMouseButtonDown(0))
                  {
                        HandleInput(Input.mousePosition);
                  }

                  // Handle touch input
                  if (Input.touchCount > 0)
                  {
                        Touch touch = Input.GetTouch(0);
                        if (touch.phase == TouchPhase.Began)
                        {
                              HandleInput(touch.position);
                        }
                  }
            }

            private void HandleInput(Vector2 screenPosition)
            {
                  Ray ray = mainCamera.ScreenPointToRay(screenPosition);
                  RaycastHit hit;

                  if (Physics.Raycast(ray, out hit, raycastDistance, wallLayerMask))
                  {
                        wallPainter.PaintWallAtPosition(screenPosition);
                  }
            }
      }
}