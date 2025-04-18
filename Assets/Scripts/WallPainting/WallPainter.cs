using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;
using WallDetection;
using System.IO;
using System;

namespace Remalux.WallPainting
{
    /// <summary>
    /// Компонент для покраски стен в AR с использованием результатов сегментации DeepLabV3
    /// </summary>
    public class WallPainter : MonoBehaviour
    {
        [Header("AR Components")]
        [SerializeField] private ARSession arSession;
        [SerializeField] private ARCameraManager arCameraManager;
        [SerializeField] private ARRaycastManager arRaycastManager;
        
        [Header("Wall Detection")]
        [SerializeField] private DeepLabDecoder wallDetector;
        
        [Header("UI References")]
        [SerializeField] private RawImage previewImage;
        
        [Header("Brush Settings")]
        [SerializeField, Range(1, 50)] private float _brushSize = 10f;
        [SerializeField, Range(0f, 1f)] private float _brushOpacity = 0.8f;
        [SerializeField] private Texture2D brushTexture;
        [SerializeField] private Color brushColor = Color.white;
        
        [Header("Performance")]
        [SerializeField] private bool useCoroutines = true;
        [SerializeField] private float paintingInterval = 0.1f;
        
        [Header("Debug")]
        [SerializeField] private bool debugMode = false;
        [SerializeField] private bool showRaycastHit = false;
        
        // Properties
        public float brushSize 
        { 
            get => _brushSize; 
            set => _brushSize = Mathf.Clamp(value, 1, 50); 
        }
        
        public float brushOpacity 
        { 
            get => _brushOpacity; 
            set => _brushOpacity = Mathf.Clamp01(value); 
        }
        
        // Private fields
        private Texture2D paintTexture;
        private Color32[] paintPixels;
        private bool isPainting = false;
        private Vector2 lastPaintPosition;
        
        // History for undo/redo
        private Stack<Color32[]> undoHistory = new Stack<Color32[]>();
        private Stack<Color32[]> redoHistory = new Stack<Color32[]>();
        private int maxHistorySteps = 10;
        
        // Wall data for saving
        private List<WallData> paintedWalls = new List<WallData>();
        
        private void Start()
        {
            InitializePaintTexture();
            
            // Subscribe to events
            if (arCameraManager != null && wallDetector != null)
            {
                arCameraManager.frameReceived += OnFrameReceived;
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (arCameraManager != null)
            {
                arCameraManager.frameReceived -= OnFrameReceived;
            }
        }
        
        private void InitializePaintTexture()
        {
            if (wallDetector == null || wallDetector.outputWidth <= 0 || wallDetector.outputHeight <= 0)
            {
                Debug.LogError("Wall detector not initialized or invalid dimensions");
                return;
            }
            
            int width = wallDetector.outputWidth;
            int height = wallDetector.outputHeight;
            
            paintTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            paintPixels = new Color32[width * height];
            
            // Initialize all pixels to transparent
            for (int i = 0; i < paintPixels.Length; i++)
            {
                paintPixels[i] = new Color32(0, 0, 0, 0);
            }
            
            paintTexture.SetPixels32(paintPixels);
            paintTexture.Apply();
            
            if (previewImage != null)
            {
                previewImage.texture = paintTexture;
            }
            
            // Save initial state for undo
            SaveState();
        }
        
        private void Update()
        {
            if (wallDetector == null || paintTexture == null || arRaycastManager == null)
                return;
                
            HandleInput();
            
            // Show paint texture on preview if available
            if (previewImage != null && paintTexture != null)
            {
                previewImage.texture = paintTexture;
            }
        }
        
        private void HandleInput()
        {
            // Check for touch input
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        BeginPainting(touch.position);
                        break;
                        
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        ContinuePainting(touch.position);
                        break;
                        
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        EndPainting();
                        break;
                }
            }
            
            // For testing in editor
            #if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0))
            {
                BeginPainting(Input.mousePosition);
            }
            else if (Input.GetMouseButton(0))
            {
                ContinuePainting(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                EndPainting();
            }
            #endif
        }
        
        private void BeginPainting(Vector2 screenPosition)
        {
            if (isPainting)
                return;
                
            isPainting = true;
            
            // Clear redo history when beginning a new paint stroke
            redoHistory.Clear();
            
            // Raycast to find where to paint
            if (RaycastToWall(screenPosition, out Vector2 texturePosition))
            {
                lastPaintPosition = texturePosition;
                
                if (useCoroutines)
                {
                    StartCoroutine(PaintCoroutine());
                }
                else
                {
                    ApplyBrush(texturePosition);
                }
            }
            else
            {
                isPainting = false;
            }
        }
        
        private void ContinuePainting(Vector2 screenPosition)
        {
            if (!isPainting)
                return;
                
            // Raycast to find where to paint
            if (RaycastToWall(screenPosition, out Vector2 texturePosition))
            {
                if (!useCoroutines)
                {
                    // If not using coroutines, paint line between last position and current
                    PaintLine(lastPaintPosition, texturePosition);
                    lastPaintPosition = texturePosition;
                }
            }
        }
        
        private void EndPainting()
        {
            if (!isPainting)
                return;
                
            isPainting = false;
            
            // Save wall data for the painted area
            SaveWallData();
            
            // Save state for undo
            SaveState();
        }
        
        private IEnumerator PaintCoroutine()
        {
            while (isPainting)
            {
                if (Input.touchCount > 0)
                {
                    Touch touch = Input.GetTouch(0);
                    if (RaycastToWall(touch.position, out Vector2 texturePosition))
                    {
                        PaintLine(lastPaintPosition, texturePosition);
                        lastPaintPosition = texturePosition;
                    }
                }
                #if UNITY_EDITOR
                else if (Input.GetMouseButton(0))
                {
                    if (RaycastToWall(Input.mousePosition, out Vector2 texturePosition))
                    {
                        PaintLine(lastPaintPosition, texturePosition);
                        lastPaintPosition = texturePosition;
                    }
                }
                #endif
                
                yield return new WaitForSeconds(paintingInterval);
            }
        }
        
        private bool RaycastToWall(Vector2 screenPosition, out Vector2 texturePosition)
        {
            texturePosition = Vector2.zero;
            
            // If wall detector doesn't have a valid mask, return false
            if (wallDetector == null || wallDetector.wallMaskTexture == null)
                return false;
                
            // Get wall mask and output dimensions
            Texture2D wallMask = wallDetector.wallMaskTexture;
            int width = wallDetector.outputWidth;
            int height = wallDetector.outputHeight;
            
            // Convert screen position to normalized coordinates
            float normalizedX = screenPosition.x / Screen.width;
            float normalizedY = screenPosition.y / Screen.height;
            
            // Convert normalized coordinates to texture coordinates
            int texX = Mathf.Clamp(Mathf.RoundToInt(normalizedX * width), 0, width - 1);
            int texY = Mathf.Clamp(Mathf.RoundToInt((1f - normalizedY) * height), 0, height - 1);
            
            // Check if this position is within a wall using the wall mask
            if (IsWallPixel(texX, texY))
            {
                texturePosition = new Vector2(texX, texY);
                return true;
            }
            
            // For non-AR testing, always return true in debug mode
            if (debugMode)
            {
                texturePosition = new Vector2(texX, texY);
                return true;
            }
            
            return false;
        }
        
        private bool IsWallPixel(int x, int y)
        {
            if (wallDetector == null || wallDetector.wallMaskTexture == null)
                return false;
                
            Texture2D wallMask = wallDetector.wallMaskTexture;
            
            // Get pixel at specified position
            Color pixelColor = wallMask.GetPixel(x, y);
            
            // Wall pixels are typically white or close to white in the mask
            return pixelColor.r > 0.5f;
        }
        
        private void ApplyBrush(Vector2 position)
        {
            if (paintTexture == null)
                return;
                
            int centerX = Mathf.RoundToInt(position.x);
            int centerY = Mathf.RoundToInt(position.y);
            int brushRadius = Mathf.RoundToInt(brushSize / 2f);
            
            int width = paintTexture.width;
            int height = paintTexture.height;
            
            // Define brush area
            int startX = Mathf.Max(0, centerX - brushRadius);
            int endX = Mathf.Min(width - 1, centerX + brushRadius);
            int startY = Mathf.Max(0, centerY - brushRadius);
            int endY = Mathf.Min(height - 1, centerY + brushRadius);
            
            // Apply brush texture if available, otherwise use a simple circle
            if (brushTexture != null)
            {
                ApplyBrushTexture(startX, endX, startY, endY, centerX, centerY);
            }
            else
            {
                ApplyCircleBrush(startX, endX, startY, endY, centerX, centerY, brushRadius);
            }
            
            // Apply changes
            paintTexture.SetPixels32(paintPixels);
            paintTexture.Apply();
        }
        
        private void ApplyBrushTexture(int startX, int endX, int startY, int endY, int centerX, int centerY)
        {
            int brushWidth = brushTexture.width;
            int brushHeight = brushTexture.height;
            float scaleFactor = brushSize / Mathf.Max(brushWidth, brushHeight);
            
            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    // Calculate normalized coordinates in the brush texture
                    float brushX = (x - centerX) / brushSize + 0.5f;
                    float brushY = (y - centerY) / brushSize + 0.5f;
                    
                    // Check if within brush bounds
                    if (brushX >= 0 && brushX <= 1 && brushY >= 0 && brushY <= 1)
                    {
                        int texX = Mathf.Clamp(Mathf.RoundToInt(brushX * brushWidth), 0, brushWidth - 1);
                        int texY = Mathf.Clamp(Mathf.RoundToInt(brushY * brushHeight), 0, brushHeight - 1);
                        
                        // Get brush color and apply it
                        Color brushPixel = brushTexture.GetPixel(texX, texY);
                        if (brushPixel.a > 0.01f)
                        {
                            Color targetColor = brushColor;
                            targetColor.a *= brushPixel.a * brushOpacity;
                            
                            int index = y * paintTexture.width + x;
                            paintPixels[index] = BlendColors(paintPixels[index], targetColor);
                        }
                    }
                }
            }
        }
        
        private void ApplyCircleBrush(int startX, int endX, int startY, int endY, int centerX, int centerY, int radius)
        {
            float radiusSq = radius * radius;
            
            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    float distSq = (x - centerX) * (x - centerX) + (y - centerY) * (y - centerY);
                    
                    // If within brush radius
                    if (distSq <= radiusSq)
                    {
                        // Calculate opacity based on distance from center (softer at edges)
                        float opacity = brushOpacity * (1.0f - Mathf.Sqrt(distSq) / radius);
                        
                        // Apply color blend
                        Color targetColor = brushColor;
                        targetColor.a *= opacity;
                        
                        int index = y * paintTexture.width + x;
                        paintPixels[index] = BlendColors(paintPixels[index], targetColor);
                    }
                }
            }
        }
        
        private void PaintLine(Vector2 from, Vector2 to)
        {
            // Use Bresenham's line algorithm to draw a line between two points
            int x0 = Mathf.RoundToInt(from.x);
            int y0 = Mathf.RoundToInt(from.y);
            int x1 = Mathf.RoundToInt(to.x);
            int y1 = Mathf.RoundToInt(to.y);
            
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            
            float brushStepSize = Mathf.Min(brushSize / 4f, 1.0f);
            float distCovered = 0f;
            float dist = Vector2.Distance(from, to);
            
            while (true)
            {
                distCovered += brushStepSize;
                if (distCovered >= dist)
                {
                    ApplyBrush(new Vector2(x1, y1));
                    break;
                }
                
                float t = dist == 0 ? 1f : distCovered / dist;
                int interpX = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
                int interpY = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
                
                ApplyBrush(new Vector2(interpX, interpY));
            }
        }
        
        private Color32 BlendColors(Color32 background, Color foreground)
        {
            // Standard alpha blending
            float alpha = foreground.a / 255.0f;
            float oneMinusAlpha = 1.0f - alpha;
            
            return new Color32(
                (byte)(foreground.r * alpha + background.r * oneMinusAlpha * (background.a / 255.0f)),
                (byte)(foreground.g * alpha + background.g * oneMinusAlpha * (background.a / 255.0f)),
                (byte)(foreground.b * alpha + background.b * oneMinusAlpha * (background.a / 255.0f)),
                (byte)(foreground.a + background.a * oneMinusAlpha)
            );
        }
        
        private void SaveState()
        {
            // Create a copy of the current pixels
            Color32[] pixelsCopy = new Color32[paintPixels.Length];
            Array.Copy(paintPixels, pixelsCopy, paintPixels.Length);
            
            // Add to undo history
            undoHistory.Push(pixelsCopy);
            
            // Limit history size
            if (undoHistory.Count > maxHistorySteps)
            {
                // Remove oldest state
                Color32[] oldestState = new Color32[undoHistory.Count];
                undoHistory.CopyTo(oldestState, 1);
                undoHistory.Clear();
                
                for (int i = 0; i < oldestState.Length - 1; i++)
                {
                    undoHistory.Push(oldestState[i]);
                }
            }
        }
        
        public void Undo()
        {
            if (undoHistory.Count <= 1) // Keep at least the initial state
                return;
                
            // Save current state to redo stack
            Color32[] currentState = new Color32[paintPixels.Length];
            Array.Copy(paintPixels, currentState, paintPixels.Length);
            redoHistory.Push(currentState);
            
            // Remove current state from undo history
            undoHistory.Pop();
            
            // Get previous state
            Color32[] previousState = undoHistory.Peek();
            
            // Apply previous state
            Array.Copy(previousState, paintPixels, paintPixels.Length);
            paintTexture.SetPixels32(paintPixels);
            paintTexture.Apply();
        }
        
        public void Redo()
        {
            if (redoHistory.Count == 0)
                return;
                
            // Get state to redo
            Color32[] stateToRedo = redoHistory.Pop();
            
            // Save current state to undo history
            Color32[] currentState = new Color32[paintPixels.Length];
            Array.Copy(paintPixels, currentState, paintPixels.Length);
            undoHistory.Push(currentState);
            
            // Apply redo state
            Array.Copy(stateToRedo, paintPixels, paintPixels.Length);
            paintTexture.SetPixels32(paintPixels);
            paintTexture.Apply();
        }
        
        public void Reset()
        {
            // Save current state for undo
            SaveState();
            
            // Clear all paint
            for (int i = 0; i < paintPixels.Length; i++)
            {
                paintPixels[i] = new Color32(0, 0, 0, 0);
            }
            
            paintTexture.SetPixels32(paintPixels);
            paintTexture.Apply();
            
            // Clear painted walls
            paintedWalls.Clear();
        }
        
        private void SaveWallData()
        {
            if (arSession == null || arCameraManager == null)
                return;
                
            // Create wall data
            WallData wallData = new WallData
            {
                position = arCameraManager.transform.position,
                rotation = arCameraManager.transform.rotation,
                color = brushColor,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            
            paintedWalls.Add(wallData);
        }
        
        public void SaveWallsToJson(string filename = "painted_walls.json")
        {
            string path = Path.Combine(Application.persistentDataPath, filename);
            WallDataList wallDataList = new WallDataList { walls = paintedWalls.ToArray() };
            string json = JsonUtility.ToJson(wallDataList, true);
            File.WriteAllText(path, json);
            
            Debug.Log($"Walls saved to {path}");
        }
        
        public void LoadWallsFromJson(string filename = "painted_walls.json")
        {
            string path = Path.Combine(Application.persistentDataPath, filename);
            
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                WallDataList wallDataList = JsonUtility.FromJson<WallDataList>(json);
                
                if (wallDataList != null && wallDataList.walls != null)
                {
                    paintedWalls = new List<WallData>(wallDataList.walls);
                    
                    Debug.Log($"Loaded {paintedWalls.Count} walls from {path}");
                }
            }
            else
            {
                Debug.LogWarning($"File not found: {path}");
            }
        }
        
        private void OnFrameReceived(ARCameraFrameEventArgs args)
        {
            // Update paint texture based on camera frame
            if (paintTexture != null && previewImage != null)
            {
                previewImage.texture = paintTexture;
            }
        }
        
        public void SetBrushColor(Color color)
        {
            brushColor = color;
        }
        
        [Serializable]
        public class WallData
        {
            public Vector3 position;
            public Quaternion rotation;
            public Color color;
            public string timestamp;
        }
        
        [Serializable]
        public class WallDataList
        {
            public WallData[] walls;
        }
    }
} 