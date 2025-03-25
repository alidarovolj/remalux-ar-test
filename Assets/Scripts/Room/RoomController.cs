using UnityEngine;
using System.Collections.Generic;
using Remalux.WallPainting;

namespace Remalux.Room
{
      public class RoomController : MonoBehaviour
      {
            [Header("References")]
            [SerializeField] private Camera mainCamera;
            [SerializeField] private RoomManager roomManager;
            [SerializeField] private TextureManager textureManager;

            [Header("Settings")]
            [SerializeField] private float raycastDistance = 100f;
            [SerializeField] private LayerMask wallLayer;

            private void Start()
            {
                  ValidateComponents();
                  InitializeRoom();
            }

            private void ValidateComponents()
            {
                  if (mainCamera == null)
                  {
                        mainCamera = Camera.main;
                        if (mainCamera == null)
                        {
                              Debug.LogError("RoomController: Main camera not found!");
                              enabled = false;
                              return;
                        }
                  }

                  if (roomManager == null)
                  {
                        roomManager = GetComponent<RoomManager>();
                        if (roomManager == null)
                        {
                              Debug.LogError("RoomController: RoomManager not found!");
                              enabled = false;
                              return;
                        }
                  }

                  if (textureManager == null)
                  {
                        textureManager = FindObjectOfType<TextureManager>();
                        if (textureManager == null)
                        {
                              Debug.LogError("RoomController: TextureManager not found!");
                              enabled = false;
                              return;
                        }
                  }
            }

            private void InitializeRoom()
            {
                  roomManager.CreateDefaultRoom();
            }

            private void Update()
            {
                  HandleWallPainting();
                  HandleTextureSelection();
            }

            private void HandleWallPainting()
            {
                  if (Input.GetMouseButtonDown(0))
                  {
                        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                        RaycastHit hit;

                        if (Physics.Raycast(ray, out hit, raycastDistance, wallLayer))
                        {
                              var currentPreset = textureManager.GetCurrentPreset();
                              if (currentPreset.material != null)
                              {
                                    roomManager.ApplyMaterialToWall(hit.collider.gameObject, currentPreset.material);
                              }
                        }
                  }
            }

            private void HandleTextureSelection()
            {
                  // Выбор текстуры клавишами 1-5
                  for (int i = 0; i < 5; i++)
                  {
                        if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                        {
                              textureManager.SetTextureByIndex(i);
                        }
                  }
            }

            public void ResetRoom()
            {
                  roomManager.ClearWalls();
                  roomManager.CreateDefaultRoom();
            }

            public void SetRoomSize(Vector3[] points)
            {
                  if (points == null || points.Length < 3)
                  {
                        Debug.LogError("RoomController: Недостаточно точек для создания комнаты!");
                        return;
                  }

                  roomManager.ClearWalls();
                  roomManager.CreateRoomFromPoints(points);
            }
      }
}