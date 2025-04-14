using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;  // This contains XROrigin
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using Unity.XR.CoreUtils.Bindings.Variables;  // For TrackedPoseDriver
using UnityEngine.InputSystem;  // For InputSystem types

namespace Remalux.WallPainting.Setup
{
      public class XRSetupManager : MonoBehaviour
      {
            [Header("XR Components")]
            public XROrigin xrOrigin;
            public Camera mainCamera;
            public ARSession arSession;
            public Canvas arCanvas;

            private void Awake()
            {
                  SetupXRComponents();
            }

            private void SetupXRComponents()
            {
                  // Create or get XR Origin
                  if (xrOrigin == null)
                  {
                        xrOrigin = FindAnyObjectByType<XROrigin>();
                        if (xrOrigin == null)
                        {
                              GameObject xrOriginObj = new GameObject("XR Origin");
                              xrOrigin = xrOriginObj.AddComponent<XROrigin>();
                        }
                  }

                  // Create Camera Floor Offset
                  GameObject cameraFloorOffset = xrOrigin.transform.Find("Camera Floor Offset")?.gameObject;
                  if (cameraFloorOffset == null)
                  {
                        cameraFloorOffset = new GameObject("Camera Floor Offset");
                        cameraFloorOffset.transform.SetParent(xrOrigin.transform, false);
                  }
                  xrOrigin.CameraFloorOffsetObject = cameraFloorOffset;

                  // Create Camera Offset
                  Transform cameraOffset = cameraFloorOffset.transform.Find("Camera Offset");
                  if (cameraOffset == null)
                  {
                        GameObject cameraOffsetObj = new GameObject("Camera Offset");
                        cameraOffsetObj.transform.SetParent(cameraFloorOffset.transform, false);
                        cameraOffset = cameraOffsetObj.transform;
                  }

                  // Setup Main Camera
                  if (mainCamera == null)
                  {
                        mainCamera = Camera.main;
                        if (mainCamera == null)
                        {
                              GameObject cameraObj = new GameObject("Main Camera");
                              mainCamera = cameraObj.AddComponent<Camera>();
                              mainCamera.tag = "MainCamera";
                        }
                  }

                  // Move camera to be child of Camera Offset
                  mainCamera.transform.SetParent(cameraOffset, false);

                  // Add required components to camera
                  if (!mainCamera.TryGetComponent(out ARCameraManager _))
                  {
                        mainCamera.gameObject.AddComponent<ARCameraManager>();
                  }

                  if (!mainCamera.TryGetComponent(out ARCameraBackground _))
                  {
                        mainCamera.gameObject.AddComponent<ARCameraBackground>();
                  }

                  // Add Tracked Pose Driver (Input System)
                  var trackedPoseDriver = mainCamera.gameObject.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
                  if (trackedPoseDriver == null)
                  {
                        trackedPoseDriver = mainCamera.gameObject.AddComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
                        trackedPoseDriver.positionAction = new InputAction("Position", binding: "<XRHMD>/centerEyePosition");
                        trackedPoseDriver.rotationAction = new InputAction("Rotation", binding: "<XRHMD>/centerEyeRotation");
                        trackedPoseDriver.positionAction.Enable();
                        trackedPoseDriver.rotationAction.Enable();
                  }

                  // Setup AR Session
                  if (arSession == null)
                  {
                        arSession = FindAnyObjectByType<ARSession>();
                        if (arSession == null)
                        {
                              GameObject arSessionObj = new GameObject("AR Session");
                              arSession = arSessionObj.AddComponent<ARSession>();
                        }
                  }

                  // Setup AR Canvas
                  if (arCanvas == null)
                  {
                        arCanvas = FindAnyObjectByType<Canvas>();
                        if (arCanvas == null)
                        {
                              GameObject canvasObj = new GameObject("AR Canvas");
                              arCanvas = canvasObj.AddComponent<Canvas>();
                              arCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                              canvasObj.AddComponent<CanvasScaler>();
                              canvasObj.AddComponent<GraphicRaycaster>();
                        }
                  }

                  // Ensure XR Origin is properly configured
                  if (xrOrigin.Camera == null)
                  {
                        xrOrigin.Camera = mainCamera;
                  }
            }
      }
}