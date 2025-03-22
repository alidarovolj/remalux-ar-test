#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace Remalux.AR
{
    public static class WallSetup
    {
        [MenuItem("Tools/Wall Painting/Setup/Setup Selected Walls")]
        public static void SetupSelectedWalls()
        {
            GameObject[] selectedObjects = Selection.gameObjects;
            if (selectedObjects.Length == 0)
            {
                Debug.LogWarning("No objects selected. Please select walls to setup.");
                return;
            }

            int wallLayer = 8; // Layer "Wall"
            int setupCount = 0;

            foreach (GameObject obj in selectedObjects)
            {
                // Skip objects without MeshRenderer
                MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
                if (renderer == null)
                {
                    Debug.LogWarning($"Skipping {obj.name} - no MeshRenderer component.");
                    continue;
                }

                // Устанавливаем слой Wall
                if (obj.layer != wallLayer)
                {
                    obj.layer = wallLayer;
                    setupCount++;
                }

                // Добавляем тег Wall если его нет
                if (!obj.CompareTag("Wall"))
                {
                    obj.tag = "Wall";
                }

                EditorUtility.SetDirty(obj);
            }

            if (setupCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                Debug.Log($"Successfully setup {setupCount} walls.");
            }
        }

        [MenuItem("Tools/Wall Painting/Setup/Fix WallPainter Settings")]
        public static void FixWallPainterSettings()
        {
            // Находим все WallPainter в сцене
            var wallPainter = Object.FindObjectOfType<WallPainter>();
            if (wallPainter == null)
            {
                Debug.LogError("WallPainter not found in scene!");
                return;
            }

            // Исправляем маску слоя
            var wallLayerMaskField = wallPainter.GetType().GetField("wallLayerMask", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            
            if (wallLayerMaskField != null)
            {
                LayerMask newMask = 1 << 8; // Layer "Wall"
                wallLayerMaskField.SetValue(wallPainter, newMask);
                Debug.Log($"Fixed WallPainter layer mask to {newMask.value}");
                
                // Добавляем DirectInputHandler если его нет
                var inputHandler = wallPainter.gameObject.GetComponent<DirectInputHandler>();
                if (inputHandler == null)
                {
                    inputHandler = wallPainter.gameObject.AddComponent<DirectInputHandler>();
                    Debug.Log("Added DirectInputHandler to WallPainter object");
                }
                
                EditorUtility.SetDirty(wallPainter.gameObject);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
        }

        [MenuItem("Tools/Wall Painting/Setup/Fix WallPainter Layer")]
        public static void FixWallPainterLayer()
        {
            var wallPainter = Object.FindObjectOfType<WallPainter>();
            if (wallPainter == null)
            {
                Debug.LogError("WallPainter not found in scene!");
                return;
            }

            // Move WallPainter to Default layer
            if (wallPainter.gameObject.layer == LayerMask.NameToLayer("Wall"))
            {
                wallPainter.gameObject.layer = LayerMask.NameToLayer("Default");
                Debug.Log("Moved WallPainter to Default layer");
                EditorUtility.SetDirty(wallPainter.gameObject);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
        }

        [MenuItem("Tools/Wall Painting/Setup/Check Wall Settings")]
        public static void CheckWallSettings()
        {
            Debug.Log("=== Checking Wall Settings ===");

            // Check Wall layer
            int wallLayer = LayerMask.NameToLayer("Wall");
            if (wallLayer != 8)
            {
                Debug.LogError("Wall layer is not set to layer 8! Please set up the Wall layer in Edit > Project Settings > Tags and Layers");
                return;
            }
            Debug.Log("Wall layer is correctly set to layer 8");

            // Find walls
            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
            List<GameObject> wallObjects = new List<GameObject>();
            
            // Find WallPainter first to exclude it
            var wallPainter = Object.FindObjectOfType<WallPainter>();
            
            foreach (GameObject obj in allObjects)
            {
                if (obj.layer == wallLayer && (wallPainter == null || obj != wallPainter.gameObject))
                {
                    wallObjects.Add(obj);
                }
            }

            if (wallObjects.Count == 0)
            {
                Debug.LogError("No wall objects found on Wall layer!");
                return;
            }
            Debug.Log($"Found {wallObjects.Count} wall objects on Wall layer");

            // Check walls
            foreach (GameObject wall in wallObjects)
            {
                Debug.Log($"Checking wall: {wall.name}");
                
                // Check components
                MeshRenderer renderer = wall.GetComponent<MeshRenderer>();
                MeshCollider collider = wall.GetComponent<MeshCollider>();
                
                if (renderer == null)
                    Debug.LogError($"Wall {wall.name} is missing MeshRenderer!");
                if (collider == null)
                    Debug.LogError($"Wall {wall.name} is missing MeshCollider!");
                
                // Check tag
                if (!wall.CompareTag("Wall"))
                    Debug.LogError($"Wall {wall.name} doesn't have Wall tag!");
            }

            // Check WallPainter
            if (wallPainter == null)
            {
                Debug.LogError("WallPainter not found in scene!");
                return;
            }
            Debug.Log($"Found WallPainter on {wallPainter.gameObject.name}");

            // Check WallPainter is not on Wall layer
            if (wallPainter.gameObject.layer == wallLayer)
            {
                Debug.LogError("WallPainter should not be on Wall layer! Use 'Fix WallPainter Layer' to fix this.");
            }

            // Check layer mask
            var wallLayerMaskField = wallPainter.GetType().GetField("wallLayerMask", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            
            if (wallLayerMaskField != null)
            {
                LayerMask mask = (LayerMask)wallLayerMaskField.GetValue(wallPainter);
                if (mask.value != (1 << 8))
                {
                    Debug.LogError($"WallPainter layer mask is incorrect! Current: {mask.value}, Should be: {1 << 8}");
                }
                else
                {
                    Debug.Log("WallPainter layer mask is correct (256)");
                }
            }

            // Check DirectInputHandler
            var inputHandler = wallPainter.gameObject.GetComponent<DirectInputHandler>();
            if (inputHandler == null)
            {
                Debug.LogError("DirectInputHandler not found on WallPainter object!");
            }
            else
            {
                Debug.Log("DirectInputHandler is present on WallPainter object");
            }

            Debug.Log("=== Wall Settings Check Complete ===");
        }
    }
}
#endif