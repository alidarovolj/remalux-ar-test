using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;

public class ARPlayerController : MonoBehaviour
{
    [Header("Настройки перемещения")]
    [SerializeField] private float moveSpeed = 2.0f;
    [SerializeField] private float rotationSpeed = 100.0f;
    [SerializeField] private float pinchSpeed = 0.5f;
    
    [Header("XR компоненты")]
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField] private Camera mainCamera;

    private Vector2 touchStart;
    private float initialDistance;
    private Vector3 initialScale;
    private bool isInitialized = false;
    private bool isMoving = false;
    private Vector3 originStartPosition;
    private Quaternion originStartRotation;

    private void Awake()
    {
        SetupXRComponents();
    }

    private void SetupXRComponents()
    {
        // Находим XR Origin, если не назначен
        if (xrOrigin == null)
        {
            xrOrigin = FindObjectOfType<XROrigin>();
            if (xrOrigin == null)
            {
                Debug.LogError("ARPlayerController: Не найден XR Origin в сцене!");
                return;
            }
        }

        // Находим Main Camera
        if (mainCamera == null)
        {
            mainCamera = xrOrigin.Camera;
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogError("ARPlayerController: Не найдена Main Camera в сцене!");
                    return;
                }
            }
        }

        isInitialized = true;
        Debug.Log("ARPlayerController: XR компоненты успешно настроены");
    }

    private void Start()
    {
        // Дополнительная инициализация, если потребуется
    }

    private void Update()
    {
        if (!isInitialized)
        {
            SetupXRComponents();
            if (!isInitialized) return;
        }

        // Обработка касаний на мобильном устройстве
        if (!Application.isEditor)
        {
            HandleMobileInput();
        }
        // Обработка ввода в редакторе
        else
        {
            HandleEditorInput();
        }
    }

    private void HandleMobileInput()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            // Проверяем, находится ли касание в правой части экрана
            bool isRightSideTouch = touch.position.x > Screen.width * 0.7f;

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    if (isRightSideTouch)
                    {
                        touchStart = touch.position;
                        isMoving = true;
                        // Сохраняем начальную позицию и поворот
                        originStartPosition = mainCamera.transform.localPosition;
                        originStartRotation = mainCamera.transform.localRotation;
                    }
                    break;

                case TouchPhase.Moved:
                    if (!isMoving) return;

                    // Перемещение
                    float deltaY = (touch.position.y - touchStart.y) / Screen.height;
                    float deltaX = (touch.position.x - touchStart.x) / Screen.width;

                    // Движение вперед/назад (локальное для камеры)
                    Vector3 moveDirection = mainCamera.transform.forward;
                    moveDirection.y = 0;
                    moveDirection.Normalize();
                    
                    Vector3 newPosition = originStartPosition + moveDirection * deltaY * moveSpeed;
                    mainCamera.transform.localPosition = newPosition;

                    // Поворот (локальный для камеры)
                    Quaternion newRotation = originStartRotation * Quaternion.Euler(0, deltaX * rotationSpeed, 0);
                    mainCamera.transform.localRotation = newRotation;
                    break;

                case TouchPhase.Ended:
                    isMoving = false;
                    break;
            }
        }
        // Масштабирование щипком
        else if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            // Проверяем, что хотя бы одно касание в правой части экрана
            bool isRightSideTouch = touch0.position.x > Screen.width * 0.7f || touch1.position.x > Screen.width * 0.7f;
            if (!isRightSideTouch) return;

            if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
            {
                initialDistance = Vector2.Distance(touch0.position, touch1.position);
                initialScale = mainCamera.transform.localScale;
            }
            else if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
            {
                float currentDistance = Vector2.Distance(touch0.position, touch1.position);
                float scaleFactor = currentDistance / initialDistance;
                
                Vector3 newScale = initialScale * Mathf.Pow(scaleFactor, pinchSpeed);
                newScale = Vector3.ClampMagnitude(newScale, 2.0f);
                newScale = Vector3.Max(newScale, Vector3.one * 0.5f);
                
                mainCamera.transform.localScale = newScale;
            }
        }
    }

    private void HandleEditorInput()
    {
        // Проверяем, зажата ли правая кнопка мыши
        if (!Input.GetMouseButton(1)) return;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Локальное движение камеры
        Vector3 movement = new Vector3(horizontal, 0, vertical);
        movement = mainCamera.transform.TransformDirection(movement);
        movement.y = 0;
        movement.Normalize();
        
        Vector3 newPosition = mainCamera.transform.localPosition + movement * moveSpeed * Time.deltaTime;
        mainCamera.transform.localPosition = newPosition;

        // Локальный поворот камеры
        if (Input.GetKey(KeyCode.Q))
        {
            Quaternion newRotation = mainCamera.transform.localRotation * Quaternion.Euler(0, -rotationSpeed * Time.deltaTime, 0);
            mainCamera.transform.localRotation = newRotation;
        }
        if (Input.GetKey(KeyCode.E))
        {
            Quaternion newRotation = mainCamera.transform.localRotation * Quaternion.Euler(0, rotationSpeed * Time.deltaTime, 0);
            mainCamera.transform.localRotation = newRotation;
        }
    }
} 