using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Remalux.AR
{
      /// <summary>
      /// Скрипт для автоматической настройки OpenCV компонентов в сцене
      /// </summary>
      public class OpenCVSetup : MonoBehaviour
      {
            [Header("Компоненты")]
            [SerializeField] private GameObject openCVDetectorPrefab;
            [SerializeField] private bool createDetectorOnStart = true;

            private OpenCVWallDetector detector;

            void Awake()
            {
                  Debug.Log("[OpenCVSetup] Инициализация");
            }

            void Start()
            {
                  if (createDetectorOnStart)
                  {
                        SetupOpenCVDetector();
                  }
            }

            /// <summary>
            /// Настраивает OpenCV детектор в сцене
            /// </summary>
            public void SetupOpenCVDetector()
            {
                  // Проверяем, есть ли уже детектор в сцене
                  detector = Object.FindFirstObjectByType<OpenCVWallDetector>();

                  if (detector != null)
                  {
                        Debug.Log("[OpenCVSetup] OpenCVWallDetector уже существует в сцене");
                        return;
                  }

                  // Создаем детектор
                  if (openCVDetectorPrefab != null)
                  {
                        GameObject detectorObj = Instantiate(openCVDetectorPrefab);
                        detectorObj.name = "OpenCVWallDetector";
                        detector = detectorObj.GetComponent<OpenCVWallDetector>();
                        Debug.Log("[OpenCVSetup] Создан OpenCVWallDetector из префаба");
                  }
                  else
                  {
                        GameObject detectorObj = new GameObject("OpenCVWallDetector");
                        detector = detectorObj.AddComponent<OpenCVWallDetector>();

                        // Находим и связываем необходимые компоненты
                        ARCameraManager cameraManager = Object.FindFirstObjectByType<ARCameraManager>();
                        ARRaycastManager raycastManager = Object.FindFirstObjectByType<ARRaycastManager>();
                        WallPainter wallPainter = Object.FindFirstObjectByType<WallPainter>();

                        // Устанавливаем значения через рефлексию (т.к. поля приватные)
                        System.Type type = typeof(OpenCVWallDetector);

                        if (cameraManager != null)
                        {
                              type.GetField("cameraManager", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                                  ?.SetValue(detector, cameraManager);
                        }

                        if (raycastManager != null)
                        {
                              type.GetField("raycastManager", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                                  ?.SetValue(detector, raycastManager);
                        }

                        if (wallPainter != null)
                        {
                              type.GetField("wallPainter", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                                  ?.SetValue(detector, wallPainter);
                        }

                        // Устанавливаем флаг для тестового режима
                        type.GetField("createTestWallOnStart", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                            ?.SetValue(detector, true);

                        Debug.Log("[OpenCVSetup] Создан OpenCVWallDetector программно");
                  }

                  // Создаем тестовую стену через метод
                  if (detector != null)
                  {
                        detector.SendMessage("CreateTestWall", null, SendMessageOptions.DontRequireReceiver);
                        Debug.Log("[OpenCVSetup] Запрошено создание тестовой стены");
                  }
            }
      }
}