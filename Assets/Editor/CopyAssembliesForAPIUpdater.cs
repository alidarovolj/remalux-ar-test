using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Remalux.Editor
{
    public class CopyAssembliesForAPIUpdater
    {
        private static readonly string[] DllNames = new string[]
        {
            "Unity.XR.ARSubsystems.dll",
            "Unity.Barracuda.dll",
            "Unity.XR.ARFoundation.dll",
            "Unity.XR.CoreUtils.dll"
        };
        
        [MenuItem("Tools/Fix API Updater Missing DLLs")]
        public static void CopyAssemblies()
        {
            Debug.Log("Copying assemblies for API Updater...");
            
            // Find DLLs in PackageCache first to avoid circular reference issues
            string packageCachePath = Path.Combine(Application.dataPath, "..", "Library", "PackageCache");
            string projectRoot = Path.Combine(Application.dataPath, "..");
            
            foreach (string dll in DllNames)
            {
                // First attempt - find in package cache
                string sourcePath = FindDllInPackageCache(packageCachePath, dll);
                
                // If not found in package cache, try to find in artifacts folders
                if (string.IsNullOrEmpty(sourcePath))
                {
                    string artifactsPath = Path.Combine(Application.dataPath, "..", "Library", "Bee", "artifacts");
                    sourcePath = FindDllInArtifacts(artifactsPath, dll);
                }
                
                // Fallback - script assemblies (might cause circular references)
                if (string.IsNullOrEmpty(sourcePath))
                {
                    string scriptAssembliesPath = Path.Combine(Application.dataPath, "..", "Library", "ScriptAssemblies");
                    sourcePath = Path.Combine(scriptAssembliesPath, dll);
                }
                
                if (!string.IsNullOrEmpty(sourcePath) && File.Exists(sourcePath))
                {
                    try 
                    {
                        string destPath = Path.Combine(projectRoot, dll);
                        File.Copy(sourcePath, destPath, true);
                        Debug.Log($"Copied {dll} to project root from {sourcePath}");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error copying {dll}: {e.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning($"Could not find {dll} in any location");
                }
            }
            
            AssetDatabase.Refresh();
        }
        
        [MenuItem("Tools/Clean Up API Updater DLLs")]
        public static void CleanupAssemblies()
        {
            Debug.Log("Cleaning up assemblies from project root...");
            
            string projectRoot = Path.Combine(Application.dataPath, "..");
            int removedCount = 0;
            
            foreach (string dll in DllNames)
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
            
            Debug.Log($"Cleanup complete. Removed {removedCount} DLL files.");
        }
        
        private static string FindDllInPackageCache(string packageCachePath, string dllName)
        {
            if (!Directory.Exists(packageCachePath))
                return null;
                
            try
            {
                string lowerDllName = dllName.ToLower();
                
                // Package naming conventions can vary, so we check each directory
                foreach (string packageDir in Directory.GetDirectories(packageCachePath))
                {
                    string packageName = new DirectoryInfo(packageDir).Name;
                    
                    // Only check relevant package directories
                    if (lowerDllName.Contains("barracuda") && packageName.Contains("barracuda") ||
                        lowerDllName.Contains("arfoundation") && packageName.Contains("arfoundation") ||
                        lowerDllName.Contains("arsubsystems") && packageName.Contains("arsubsystems") ||
                        lowerDllName.Contains("coreutils") && packageName.Contains("core-utils"))
                    {
                        string[] foundFiles = Directory.GetFiles(packageDir, dllName, SearchOption.AllDirectories);
                        if (foundFiles.Length > 0)
                            return foundFiles[0];
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error searching package cache: {e.Message}");
            }
            
            return null;
        }
        
        private static string FindDllInArtifacts(string artifactsPath, string dllName)
        {
            if (!Directory.Exists(artifactsPath))
                return null;
                
            try
            {
                string[] foundFiles = Directory.GetFiles(artifactsPath, dllName, SearchOption.AllDirectories)
                    .Where(f => !f.Contains("post-processed"))  // Skip post-processed files to avoid circular references
                    .ToArray();
                    
                if (foundFiles.Length > 0)
                    return foundFiles[0];
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error searching artifacts: {e.Message}");
            }
            
            return null;
        }
    }
} 