// ScreenshotManager.cs
using UnityEngine;
using System.IO;

public class ScreenshotManager : MonoBehaviour
{
    public void TakeScreenshot()
    {
        string fileName = $"PaintedWall_{System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}.png";
        string filePath = Path.Combine(Application.persistentDataPath, fileName);
        
        ScreenCapture.CaptureScreenshotAsTexture();
        Debug.Log($"Screenshot saved: {filePath}");
    }
}