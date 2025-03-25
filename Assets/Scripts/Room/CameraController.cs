using UnityEngine;

namespace Remalux.WallPainting
{
      public class CameraController : MonoBehaviour
      {
            [Header("Movement Settings")]
            [SerializeField] private float moveSpeed = 5f;
            [SerializeField] private float rotationSpeed = 2f;
            [SerializeField] private float smoothTime = 0.1f;

            [Header("Zoom Settings")]
            [SerializeField] private float zoomSpeed = 2f;
            [SerializeField] private float minZoom = 2f;
            [SerializeField] private float maxZoom = 10f;

            private Vector3 currentVelocity;
            private Vector3 targetPosition;
            private float currentZoom;
            private bool isRotating;

            private void Start()
            {
                  targetPosition = transform.position;
                  currentZoom = transform.position.y;
            }

            private void Update()
            {
                  HandleInput();
                  UpdatePosition();
                  UpdateZoom();
            }

            private void HandleInput()
            {
                  // Вращение камеры
                  if (Input.GetMouseButtonDown(1))
                  {
                        isRotating = true;
                  }
                  else if (Input.GetMouseButtonUp(1))
                  {
                        isRotating = false;
                  }

                  if (isRotating)
                  {
                        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
                        float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;

                        transform.RotateAround(targetPosition, Vector3.up, mouseX);
                        transform.RotateAround(targetPosition, transform.right, -mouseY);
                  }

                  // Перемещение камеры
                  float horizontal = Input.GetAxis("Horizontal");
                  float vertical = Input.GetAxis("Vertical");
                  Vector3 movement = new Vector3(horizontal, 0, vertical) * moveSpeed * Time.deltaTime;
                  targetPosition += transform.TransformDirection(movement);

                  // Зум
                  float scroll = Input.GetAxis("Mouse ScrollWheel");
                  if (scroll != 0)
                  {
                        currentZoom = Mathf.Clamp(currentZoom - scroll * zoomSpeed, minZoom, maxZoom);
                  }
            }

            private void UpdatePosition()
            {
                  Vector3 targetPos = new Vector3(targetPosition.x, currentZoom, targetPosition.z);
                  transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, smoothTime);
            }

            private void UpdateZoom()
            {
                  Vector3 pos = transform.position;
                  pos.y = Mathf.Lerp(pos.y, currentZoom, Time.deltaTime * zoomSpeed);
                  transform.position = pos;
            }

            public void FocusOnPoint(Vector3 point)
            {
                  targetPosition = point;
            }

            public void SetRotation(Vector3 rotation)
            {
                  transform.rotation = Quaternion.Euler(rotation);
            }
      }
}