using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.XR.ARSubsystems;
using Unity.XR.CoreUtils;  // This contains XROrigin
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Management;
using Unity.XR.CoreUtils.Bindings.Variables;  // For TrackedPoseDriver
using UnityEngine.InputSystem;  // For InputSystem types
using UnityEngine.InputSystem.XR;

namespace Remalux.WallPainting.Vision
{
      [RequireComponent(typeof(Unity.XR.CoreUtils.XROrigin))]
      public class XRSetupManager : MonoBehaviour
      {
            [Header("XR Components")]
            [SerializeField] public Camera mainCamera;
            [SerializeField] private float cameraYOffset = 1.6f;
            [SerializeField] private GameObject cameraOffset;

            private Unity.XR.CoreUtils.XROrigin xrOrigin;

            private void Awake()
            {
                  SetupXROrigin();
            }

            private void SetupXROrigin()
            {
                  // Check if XROrigin already exists
                  xrOrigin = GetComponent<Unity.XR.CoreUtils.XROrigin>();
                  if (xrOrigin == null)
                  {
                        xrOrigin = gameObject.AddComponent<Unity.XR.CoreUtils.XROrigin>();
                  }

                  // Find or create camera
                  if (mainCamera == null)
                        mainCamera = Camera.main;

                  if (mainCamera == null)
                  {
                        Debug.LogError("XRSetupManager: Main camera not found!");
                        return;
                  }

                  // Set up camera offset if needed
                  if (cameraOffset == null)
                  {
                        // Create camera offset
                        cameraOffset = new GameObject("Camera Offset");
                        cameraOffset.transform.SetParent(transform, false);

                        // Position the offset at the right height
                        cameraOffset.transform.localPosition = new Vector3(0, cameraYOffset, 0);
                  }

                  // Configure XROrigin
                  xrOrigin.Camera = mainCamera;
                  xrOrigin.CameraYOffset = cameraYOffset;

                  // Make sure camera offset is assigned
                  xrOrigin.CameraFloorOffsetObject = cameraOffset;

                  // Make sure camera is child of offset
                  mainCamera.transform.SetParent(cameraOffset.transform, false);

                  // Add tracked pose driver if needed
                  TryAddTrackedPoseDriver();

                  Debug.Log("XRSetupManager: XR Origin setup complete");
            }

            private void TryAddTrackedPoseDriver()
            {
                  if (mainCamera == null) return;

                  var trackedPoseDriver = mainCamera.gameObject.GetComponent<TrackedPoseDriver>();
                  if (trackedPoseDriver == null)
                  {
                        trackedPoseDriver = mainCamera.gameObject.AddComponent<TrackedPoseDriver>();
                        trackedPoseDriver.positionInput = new InputActionProperty(new InputAction("Position", binding: "<XRHMD>/centerEyePosition"));
                        trackedPoseDriver.rotationInput = new InputActionProperty(new InputAction("Rotation", binding: "<XRHMD>/centerEyeRotation"));
                        trackedPoseDriver.positionInput.action.Enable();
                        trackedPoseDriver.rotationInput.action.Enable();
                        Debug.Log("Added TrackedPoseDriver to camera");
                  }
            }

            // Utility method to check if XR device is present
            private bool IsXRDevicePresent()
            {
                  var xrDisplaySubsystems = new List<XRDisplaySubsystem>();
                  SubsystemManager.GetSubsystems(xrDisplaySubsystems);

                  foreach (var xrDisplay in xrDisplaySubsystems)
                  {
                        if (xrDisplay.running)
                        {
                              return true;
                        }
                  }

                  return false;
            }
      }
}