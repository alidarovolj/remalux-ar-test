#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace Remalux.AR
{
    public static class WallSetupFixer
    {
        [MenuItem("Tools/Wall Painting/Fix/Complete Wall Setup")]
        public static void FixWallSetup()
        {
            Debug.Log("=== Starting Complete Wall Setup Fix ===");

            // Find all wall objects
            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
            List<GameObject> wallObjects = new List<GameObject>();
            
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.Contains("Wall_"))
                {
                    wallObjects.Add(obj);
                }
            }

            if (wallObjects.Count == 0)
            {
                Debug.LogError("No wall objects found in scene!");
                return;
            }

            Debug.Log($"Found {wallObjects.Count} wall objects");

            // Fix each wall
            foreach (GameObject wall in wallObjects)
            {
                Debug.Log($"Fixing wall: {wall.name}");

                // Set layer
                if (wall.layer != 8)
                {
                    wall.layer = 8; // Wall layer
                    Debug.Log($"- Set layer to Wall for {wall.name}");
                }

                // Set tag
                if (!wall.CompareTag("Wall"))
                {
                    wall.tag = "Wall";
                    Debug.Log($"- Set tag to Wall for {wall.name}");
                }

                // Check/fix MeshCollider
                MeshCollider meshCollider = wall.GetComponent<MeshCollider>();
                if (meshCollider == null)
                {
                    meshCollider = wall.AddComponent<MeshCollider>();
                    Debug.Log($"- Added MeshCollider to {wall.name}");
                }

                // Ensure collider settings are correct
                if (meshCollider != null)
                {
                    if (meshCollider.isTrigger)
                    {
                        meshCollider.isTrigger = false;
                        Debug.Log($"- Fixed trigger setting on {wall.name}");
                    }

                    // Make sure the collider uses the correct mesh
                    MeshFilter meshFilter = wall.GetComponent<MeshFilter>();
                    if (meshFilter != null && meshFilter.sharedMesh != null)
                    {
                        meshCollider.sharedMesh = meshFilter.sharedMesh;
                        Debug.Log($"- Updated collider mesh for {wall.name}");
                    }
                }

                // Check/fix MeshRenderer
                MeshRenderer renderer = wall.GetComponent<MeshRenderer>();
                if (renderer == null)
                {
                    renderer = wall.AddComponent<MeshRenderer>();
                    Debug.Log($"- Added MeshRenderer to {wall.name}");
                }

                // Make sure the renderer is enabled
                if (!renderer.enabled)
                {
                    renderer.enabled = true;
                    Debug.Log($"- Enabled renderer for {wall.name}");
                }

                // Mark object as dirty
                EditorUtility.SetDirty(wall);
            }

            // Save changes
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("=== Wall Setup Fix Complete ===");
        }
    }
}
#endif