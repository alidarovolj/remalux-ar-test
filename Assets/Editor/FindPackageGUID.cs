using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public class FindPackageGUID : ScriptableObject
{
    [MenuItem("Tools/Find Missing Package GUIDs")]
    public static void FindMissingGUIDs()
    {
        string packageCachePath = Path.Combine(Application.dataPath, "../Library/PackageCache");
        
        // Search for package paths
        string[] barracudaPaths = Directory.GetDirectories(packageCachePath, "*barracuda*", SearchOption.TopDirectoryOnly);
        string[] arSubsystemsPaths = Directory.GetDirectories(packageCachePath, "*arfoundation*", SearchOption.TopDirectoryOnly);
        string[] coreUtilsPaths = Directory.GetDirectories(packageCachePath, "*core-utils*", SearchOption.TopDirectoryOnly);
        
        Debug.Log("=== Package Paths ===");
        foreach (var path in barracudaPaths) Debug.Log($"Barracuda: {path}");
        foreach (var path in arSubsystemsPaths) Debug.Log($"ARFoundation: {path}");
        foreach (var path in coreUtilsPaths) Debug.Log($"CoreUtils: {path}");
        
        // Search for asmdef files
        string[] barracudaAsmdefs = Directory.GetFiles(packageCachePath, "Unity.Barracuda.asmdef", SearchOption.AllDirectories);
        string[] arSubsystemsAsmdefs = Directory.GetFiles(packageCachePath, "Unity.XR.ARSubsystems.asmdef", SearchOption.AllDirectories);
        string[] coreUtilsAsmdefs = Directory.GetFiles(packageCachePath, "Unity.XR.CoreUtils.asmdef", SearchOption.AllDirectories);
        
        Debug.Log("=== Assembly Definitions ===");
        foreach (var asmdef in barracudaAsmdefs) Debug.Log($"Barracuda ASMDEF: {asmdef}");
        foreach (var asmdef in arSubsystemsAsmdefs) Debug.Log($"ARSubsystems ASMDEF: {asmdef}");
        foreach (var asmdef in coreUtilsAsmdefs) Debug.Log($"CoreUtils ASMDEF: {asmdef}");
        
        // Extract GUIDs from meta files
        if (barracudaAsmdefs.Length > 0)
        {
            var metaFile = barracudaAsmdefs[0] + ".meta";
            if (File.Exists(metaFile))
            {
                var guid = ExtractGuidFromMetaFile(metaFile);
                Debug.Log($"Unity.Barracuda GUID: {guid}");
            }
        }
        
        if (arSubsystemsAsmdefs.Length > 0)
        {
            var metaFile = arSubsystemsAsmdefs[0] + ".meta";
            if (File.Exists(metaFile))
            {
                var guid = ExtractGuidFromMetaFile(metaFile);
                Debug.Log($"Unity.XR.ARSubsystems GUID: {guid}");
            }
        }
        
        if (coreUtilsAsmdefs.Length > 0)
        {
            var metaFile = coreUtilsAsmdefs[0] + ".meta";
            if (File.Exists(metaFile))
            {
                var guid = ExtractGuidFromMetaFile(metaFile);
                Debug.Log($"Unity.XR.CoreUtils GUID: {guid}");
            }
        }
    }
    
    private static string ExtractGuidFromMetaFile(string metaFilePath)
    {
        if (!File.Exists(metaFilePath))
            return "Meta file not found";
            
        string content = File.ReadAllText(metaFilePath);
        var match = Regex.Match(content, @"guid:\s*([a-f0-9]+)");
        
        if (match.Success)
            return match.Groups[1].Value;
            
        return "GUID not found in meta file";
    }
} 