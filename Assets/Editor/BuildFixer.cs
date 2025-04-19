using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Remalux.Editor
{
    public class BuildFixer : IPreprocessBuildWithReport
    {
        public int callbackOrder => -100; // Run before any other build processors
        
        [MenuItem("Tools/Fix All Build Issues")]
        public static void FixAllBuildIssues()
        {
            Debug.Log("Starting build fixes...");
            
            // Fix 1: Clean up any DLLs in project root
            CleanupProjectRootDlls();
            
            // Fix 2: Fix broken material references
            FixBrokenMaterials();
            
            // Fix 3: Delete problematic build artifacts
            DeleteBuildArtifacts();
            
            Debug.Log("Build fixes completed. Try building now.");
            
            // Refresh to apply all changes
            AssetDatabase.Refresh();
        }
        
        private static void CleanupProjectRootDlls()
        {
            string[] dllNames = new string[]
            {
                "Unity.XR.ARSubsystems.dll",
                "Unity.Barracuda.dll",
                "Unity.XR.ARFoundation.dll",
                "Unity.XR.CoreUtils.dll",
                "Unity.InputSystem.dll"
            };
            
            string projectRoot = Path.Combine(Application.dataPath, "..");
            int removedCount = 0;
            
            foreach (string dll in dllNames)
            {
                string dllPath = Path.Combine(projectRoot, dll);
                if (File.Exists(dllPath))
                {
                    try
                    {
                        File.Delete(dllPath);
                        removedCount++;
                        Debug.Log($"Removed {dll} from project root");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error removing {dll}: {e.Message}");
                    }
                }
            }
            
            if (removedCount > 0)
            {
                Debug.Log($"Removed {removedCount} DLL files from project root");
            }
        }
        
        private static void FixBrokenMaterials()
        {
            // Find all materials in the project
            string[] materialGuids = AssetDatabase.FindAssets("t:Material");
            int fixedCount = 0;
            
            foreach (string guid in materialGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                
                if (material != null && material.shader == null)
                {
                    // Material has a broken shader reference, fix it
                    material.shader = Shader.Find("Standard");
                    EditorUtility.SetDirty(material);
                    fixedCount++;
                    Debug.Log($"Fixed broken shader reference in material: {assetPath}");
                }
            }
            
            if (fixedCount > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"Fixed {fixedCount} materials with broken shader references");
            }
        }
        
        private static void DeleteBuildArtifacts()
        {
            string libraryPath = Path.Combine(Application.dataPath, "..", "Library");
            
            // Paths to problematic build artifacts
            string[] pathsToDelete = new string[]
            {
                Path.Combine(libraryPath, "Bee"),
                Path.Combine(libraryPath, "APIUpdater"),
                Path.Combine(libraryPath, "Artifacts"),
                Path.Combine(libraryPath, "BuildCache")
            };
            
            int deletedCount = 0;
            
            foreach (string path in pathsToDelete)
            {
                if (Directory.Exists(path))
                {
                    try
                    {
                        // Try a safe delete first (not recursive)
                        string[] files = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly);
                        foreach (string file in files)
                        {
                            File.Delete(file);
                        }
                        
                        // Try to delete the directory itself
                        if (Directory.GetFileSystemEntries(path).Length == 0)
                        {
                            Directory.Delete(path);
                            deletedCount++;
                            Debug.Log($"Deleted build artifact: {path}");
                        }
                        else
                        {
                            Debug.Log($"Couldn't fully delete {path} - some files may be in use");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Error deleting {path}: {e.Message}");
                    }
                }
            }
            
            if (deletedCount > 0)
            {
                Debug.Log($"Deleted {deletedCount} problematic build artifact directories");
            }
        }
        
        // Implement IPreprocessBuildWithReport
        public void OnPreprocessBuild(BuildReport report)
        {
            // Apply all fixes before building
            FixAllBuildIssues();
        }
    }
} 