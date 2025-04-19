using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using UnityEngine.Networking;
using System.Collections;

namespace Remalux.WallDetection
{
    /// <summary>
    /// Generates reports from wall detection test results, including 
    /// performance metrics, image comparisons, and statistical analysis.
    /// </summary>
    public class TestResultReporter : MonoBehaviour
    {
        [Serializable]
        public class TestResult
        {
            public string modelName;
            public string imageName;
            public Vector2Int inputSize;
            public float confidenceThreshold;
            public bool postProcessingApplied;
            public float processingTimeMs;
            public float wallCoverage; // Percentage of wall pixels in the image
            public string originalImagePath;
            public string maskImagePath;
            public string processedImagePath;
            public DateTime testTime;
            
            // Additional metrics
            public float edgeAccuracy; // Quality of edge detection (0-1)
            public float noiseLevel; // Amount of noise in the mask (0-1)
            public Dictionary<string, float> customMetrics = new Dictionary<string, float>();
        }

        [Serializable]
        public class TestSession
        {
            public string sessionId;
            public DateTime startTime;
            public DateTime endTime;
            public string deviceModel;
            public string deviceName;
            public string systemInfo;
            public List<TestResult> results = new List<TestResult>();
            
            // Summary statistics
            public float averageProcessingTime;
            public float minProcessingTime;
            public float maxProcessingTime;
            public float averageWallCoverage;
            public int totalImagesProcessed;
            
            public Dictionary<string, object> metadata = new Dictionary<string, object>();
        }

        [Header("Report Settings")]
        public string reportFolderPath = "TestReports";
        public bool generateHtmlReport = true;
        public bool generateJsonReport = true;
        public bool includeImages = true;
        
        private TestSession currentSession;
        
        private void Start()
        {
            InitializeSession();
        }
        
        /// <summary>
        /// Initialize a new test session
        /// </summary>
        public void InitializeSession()
        {
            currentSession = new TestSession
            {
                sessionId = Guid.NewGuid().ToString(),
                startTime = DateTime.Now,
                deviceModel = SystemInfo.deviceModel,
                deviceName = SystemInfo.deviceName,
                systemInfo = $"{SystemInfo.operatingSystem}, {SystemInfo.processorType}, {SystemInfo.graphicsDeviceName}"
            };
            
            // Create reports directory if it doesn't exist
            string path = Path.Combine(Application.persistentDataPath, reportFolderPath);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Debug.Log($"Created report directory at {path}");
            }
        }
        
        /// <summary>
        /// Add a test result to the current session
        /// </summary>
        public void AddResult(TestResult result)
        {
            if (currentSession == null)
                InitializeSession();
                
            currentSession.results.Add(result);
            Debug.Log($"Added test result for {result.imageName} using {result.modelName}");
        }
        
        /// <summary>
        /// Finish the current session and generate reports
        /// </summary>
        public void FinalizeSession()
        {
            if (currentSession == null || currentSession.results.Count == 0)
                return;
                
            currentSession.endTime = DateTime.Now;
            
            // Calculate summary statistics
            CalculateSessionStatistics();
            
            // Generate reports
            if (generateJsonReport)
                GenerateJsonReport();
                
            if (generateHtmlReport)
                StartCoroutine(GenerateHtmlReport());
                
            Debug.Log($"Finalized test session with {currentSession.totalImagesProcessed} results");
        }
        
        /// <summary>
        /// Calculate summary statistics for the session
        /// </summary>
        private void CalculateSessionStatistics()
        {
            var results = currentSession.results;
            currentSession.totalImagesProcessed = results.Count;
            
            if (results.Count > 0)
            {
                currentSession.averageProcessingTime = results.Average(r => r.processingTimeMs);
                currentSession.minProcessingTime = results.Min(r => r.processingTimeMs);
                currentSession.maxProcessingTime = results.Max(r => r.processingTimeMs);
                currentSession.averageWallCoverage = results.Average(r => r.wallCoverage);
            }
        }
        
        /// <summary>
        /// Generate a JSON report of the test session
        /// </summary>
        private void GenerateJsonReport()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"TestReport_{timestamp}.json";
            string filePath = Path.Combine(Application.persistentDataPath, reportFolderPath, fileName);
            
            try
            {
                string json = JsonUtility.ToJson(currentSession, true);
                File.WriteAllText(filePath, json);
                Debug.Log($"Generated JSON report at {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to generate JSON report: {e.Message}");
            }
        }
        
        /// <summary>
        /// Generate an HTML report with visualizations
        /// </summary>
        private IEnumerator GenerateHtmlReport()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"TestReport_{timestamp}.html";
            string filePath = Path.Combine(Application.persistentDataPath, reportFolderPath, fileName);
            
            try
            {
                // Create a basic HTML report structure
                string htmlContent = GenerateHtmlContent();
                File.WriteAllText(filePath, htmlContent);
                Debug.Log($"Generated HTML report at {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to generate HTML report: {e.Message}");
            }
            
            yield return null;
        }
        
        /// <summary>
        /// Generate the HTML content for the report
        /// </summary>
        private string GenerateHtmlContent()
        {
            string html = $@"
            <!DOCTYPE html>
            <html lang='en'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Wall Detection Test Report</title>
                <style>
                    body {{ font-family: Arial, sans-serif; margin: 20px; }}
                    h1, h2, h3 {{ color: #333; }}
                    .summary {{ background-color: #f5f5f5; padding: 15px; border-radius: 5px; margin-bottom: 20px; }}
                    table {{ border-collapse: collapse; width: 100%; }}
                    th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
                    th {{ background-color: #f2f2f2; }}
                    tr:nth-child(even) {{ background-color: #f9f9f9; }}
                    .image-container {{ display: flex; margin-top: 10px; }}
                    .image-box {{ margin-right: 10px; text-align: center; }}
                    .image {{ max-width: 300px; max-height: 200px; }}
                </style>
            </head>
            <body>
                <h1>Wall Detection Test Report</h1>
                <div class='summary'>
                    <h2>Summary</h2>
                    <p>Session ID: {currentSession.sessionId}</p>
                    <p>Start Time: {currentSession.startTime}</p>
                    <p>End Time: {currentSession.endTime}</p>
                    <p>Device: {currentSession.deviceName} ({currentSession.deviceModel})</p>
                    <p>System: {currentSession.systemInfo}</p>
                    <p>Total Images Processed: {currentSession.totalImagesProcessed}</p>
                    <p>Average Processing Time: {currentSession.averageProcessingTime.ToString("F2")} ms</p>
                    <p>Min Processing Time: {currentSession.minProcessingTime.ToString("F2")} ms</p>
                    <p>Max Processing Time: {currentSession.maxProcessingTime.ToString("F2")} ms</p>
                    <p>Average Wall Coverage: {(currentSession.averageWallCoverage * 100).ToString("F2")}%</p>
                </div>
                
                <h2>Detailed Results</h2>
                <table>
                    <tr>
                        <th>Image</th>
                        <th>Model</th>
                        <th>Input Size</th>
                        <th>Processing Time (ms)</th>
                        <th>Wall Coverage (%)</th>
                        <th>Confidence Threshold</th>
                    </tr>";
            
            // Add rows for each test result
            foreach (var result in currentSession.results)
            {
                html += $@"
                    <tr>
                        <td>{result.imageName}</td>
                        <td>{result.modelName}</td>
                        <td>{result.inputSize.x}x{result.inputSize.y}</td>
                        <td>{result.processingTimeMs.ToString("F2")}</td>
                        <td>{(result.wallCoverage * 100).ToString("F2")}</td>
                        <td>{result.confidenceThreshold.ToString("F2")}</td>
                    </tr>";
            }
            
            html += @"
                </table>
                
                <h2>Performance Analysis</h2>
                <p>This section would include charts and visualizations comparing different models and parameters.</p>
                
                <h2>Conclusion</h2>
                <p>This report was automatically generated by the Remalux AR Wall Detection Test System.</p>
            </body>
            </html>";
            
            return html;
        }
    }
} 