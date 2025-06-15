using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Manages benchmarking for the Pipeline system, logging performance metrics to file
/// 
/// Author: J-Britten
/// </summary>
public class BenchmarkManager : MonoBehaviour
{
    [System.Serializable]
    public class BenchmarkData
    {
        public float timestamp;
        public float segmentationTime;
        public float depthEstimationTime;
        public float inpaintingTime;
        public float postProcessingTime;
        public float totalIterationTime;
        public float unityFPS;
        public int frameCount;
    }

    [Header("Benchmark Settings")]
    public bool isBenchmarking = false;
    public float maxBenchmarkDuration = 60f; // Default 60 seconds
    public bool saveToFile = true; // Option to disable file saving
    public string logFileName = "benchmark_log";
      private float benchmarkStartTime;
    private float lastIterationStartTime;
    private float currentIterationStartTime;
    private List<BenchmarkData> benchmarkResults = new List<BenchmarkData>();
    private int frameCounter = 0;
    private string logFilePath;    // Individual stage performance tracking
    private List<float> segmentationTimes = new List<float>();
    private List<float> depthEstimationTimes = new List<float>();
    private List<float> inpaintingTimes = new List<float>();
    private List<float> postProcessingTimes = new List<float>();
    private List<float> unityFpsSamples = new List<float>();
    
    // Timing data for current iteration
    private float segmentationStartTime;
    private float segmentationEndTime;
    private float depthStartTime;
    private float depthEndTime;
    private float inpaintingStartTime;
    private float inpaintingEndTime;
    private float postProcessingStartTime;
    private float postProcessingEndTime;    // FPS tracking for Unity stats consistency
    private float fpsAccumulator = 0f;
    private int fpsFrameCount = 0;
    private float lastFpsUpdateTime = 0f;
    private const float FPS_UPDATE_INTERVAL = 0.25f; // Update every 250ms like Unity stats
    
    // Timing state tracking to prevent overlaps
    private bool segmentationTiming = false;
    private bool depthTiming = false;
    private bool inpaintingTiming = false;
    private bool postProcessingTiming = false;
    
    // Events for Pipeline integration
    public event Action OnBenchmarkStarted;
    public event Action OnBenchmarkStopped;
    
    private static BenchmarkManager _instance;
    public static BenchmarkManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<BenchmarkManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("BenchmarkManager");
                    _instance = go.AddComponent<BenchmarkManager>();
                }
            }
            return _instance;
        }
    }

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        InitializeLogFile();
    }    void Update()
    {
        if (isBenchmarking)
        {
            // Use Unity's stats-like FPS calculation with periodic updates
            float currentTime = Time.realtimeSinceStartup;
            fpsAccumulator += Time.unscaledDeltaTime;
            fpsFrameCount++;
            
            // Update FPS every 250ms like Unity's stats window
            if (currentTime - lastFpsUpdateTime >= FPS_UPDATE_INTERVAL)
            {
                if (fpsFrameCount > 0 && fpsAccumulator > 0)
                {
                    float averageFPS = fpsFrameCount / fpsAccumulator;
                    unityFpsSamples.Add(averageFPS);
                }
                
                // Reset for next period
                fpsAccumulator = 0f;
                fpsFrameCount = 0;
                lastFpsUpdateTime = currentTime;
            }
            
            // Check if benchmark should auto-stop
            if (currentTime - benchmarkStartTime >= maxBenchmarkDuration)
            {
                StopBenchmark();
            }
        }
    }

    private void InitializeLogFile()
    {
        if (!saveToFile) return;
        
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        logFilePath = Path.Combine(Application.persistentDataPath, $"{logFileName}_{timestamp}.csv");
        
        // Create CSV header
        string header = "Timestamp,SegmentationTime,DepthEstimationTime,InpaintingTime,PostProcessingTime,TotalIterationTime,UnityFPS,FrameCount";
        File.WriteAllText(logFilePath, header + "\n");
        
        Debug.Log($"Benchmark log file created at: {logFilePath}");
    }    public void StartBenchmark(float duration = 60f)
    {
        if (isBenchmarking)
        {
            Debug.LogWarning("Benchmark is already running!");
            return;
        }

        maxBenchmarkDuration = duration;
        isBenchmarking = true;
        benchmarkStartTime = Time.realtimeSinceStartup;
        frameCounter = 0;
        benchmarkResults.Clear();        // Clear individual stage timing lists
        segmentationTimes.Clear();
        depthEstimationTimes.Clear();
        inpaintingTimes.Clear();
        postProcessingTimes.Clear();
        unityFpsSamples.Clear();
          // Reset timing state to prevent stale data
        segmentationStartTime = 0f;
        segmentationEndTime = 0f;
        depthStartTime = 0f;
        depthEndTime = 0f;
        inpaintingStartTime = 0f;
        inpaintingEndTime = 0f;
        postProcessingStartTime = 0f;
        postProcessingEndTime = 0f;
          // Reset timing state flags
        segmentationTiming = false;
        depthTiming = false;
        inpaintingTiming = false;
        postProcessingTiming = false;
        
        // Reset FPS tracking
        fpsAccumulator = 0f;
        fpsFrameCount = 0;
        lastFpsUpdateTime = Time.realtimeSinceStartup;
          InitializeLogFile();
        
        string logInfo = saveToFile ? $" Log file: {logFilePath}" : " (File logging disabled)";
        Debug.Log($"Benchmark started for {duration} seconds.{logInfo}");
        Debug.Log($"Benchmark timing state reset. All timing lists cleared.");
        OnBenchmarkStarted?.Invoke();
    }

    public void StopBenchmark()
    {
        if (!isBenchmarking)
        {
            Debug.LogWarning("No benchmark is currently running!");
            return;
        }

        isBenchmarking = false;
        
        // Report and write summary
        ReportBenchmarkResults();
        if (saveToFile)
        {
            WriteSummaryToLog();
        }
        
        float totalDuration = Time.realtimeSinceStartup - benchmarkStartTime;
        string fileInfo = saveToFile ? $" Results saved to: {logFilePath}" : "";
        Debug.Log($"Benchmark stopped. Total duration: {totalDuration:F2}s{fileInfo}");
        OnBenchmarkStopped?.Invoke();
    }

    public void StartIteration()
    {
        if (!isBenchmarking) return;
        
        currentIterationStartTime = Time.realtimeSinceStartup;
        frameCounter++;
    }    public void EndIteration()
    {
        if (!isBenchmarking) return;
        
        float iterationTime = Time.realtimeSinceStartup - currentIterationStartTime;
        float unityFPS = 1.0f / Time.deltaTime;
        
        // Only record iteration data - individual stage timings are tracked separately
        BenchmarkData data = new BenchmarkData
        {
            timestamp = Time.realtimeSinceStartup - benchmarkStartTime,
            segmentationTime = 0f, // Will be populated separately
            depthEstimationTime = 0f, // Will be populated separately
            inpaintingTime = 0f, // Will be populated separately
            postProcessingTime = 0f, // Will be populated separately
            totalIterationTime = iterationTime,
            unityFPS = unityFPS,
            frameCount = frameCounter
        };
        
        benchmarkResults.Add(data);
        if (saveToFile)
        {
            WriteDataToLog(data);
        }
    }    // Timing methods for each pipeline stage
    public void StartSegmentation()
    {
        if (!isBenchmarking) return;
        
        if (segmentationTiming)
        {
            Debug.LogWarning("StartSegmentation() called while segmentation timing already in progress. Ignoring.");
            return;
        }
        
        segmentationTiming = true;
        segmentationStartTime = Time.realtimeSinceStartup;
    }    public void EndSegmentation()
    {
        if (!isBenchmarking) return;
        
        if (!segmentationTiming)
        {
            Debug.LogWarning("EndSegmentation() called without corresponding StartSegmentation(). Ignoring.");
            return;
        }
        
        segmentationTiming = false;
        segmentationEndTime = Time.realtimeSinceStartup;
        float segmentationTime = segmentationEndTime - segmentationStartTime;
        
        // More strict validation - segmentation should complete in reasonable time
        float benchmarkElapsed = segmentationEndTime - benchmarkStartTime;
        if (segmentationTime < 0 || segmentationTime > 5f || segmentationTime > benchmarkElapsed)
        {
            Debug.LogWarning($"Anomalous segmentation time detected: {segmentationTime:F4}s. Start: {segmentationStartTime:F4}, End: {segmentationEndTime:F4}, Benchmark elapsed: {benchmarkElapsed:F4}s. Skipping.");
            return;
        }
        
        segmentationTimes.Add(segmentationTime);
    }    public void StartDepthEstimation()
    {
        if (!isBenchmarking) return;
        
        if (depthTiming)
        {
            Debug.LogWarning("StartDepthEstimation() called while depth timing already in progress. Ignoring.");
            return;
        }
        
        depthTiming = true;
        depthStartTime = Time.realtimeSinceStartup;
    }    public void EndDepthEstimation()
    {
        if (!isBenchmarking) return;
        
        if (!depthTiming)
        {
            Debug.LogWarning("EndDepthEstimation() called without corresponding StartDepthEstimation(). Ignoring.");
            return;
        }
        
        depthTiming = false;
        depthEndTime = Time.realtimeSinceStartup;
        float depthTime = depthEndTime - depthStartTime;
        
        // More strict validation
        float benchmarkElapsed = depthEndTime - benchmarkStartTime;
        if (depthTime < 0 || depthTime > 5f || depthTime > benchmarkElapsed)
        {
            Debug.LogWarning($"Anomalous depth estimation time detected: {depthTime:F4}s. Start: {depthStartTime:F4}, End: {depthEndTime:F4}, Benchmark elapsed: {benchmarkElapsed:F4}s. Skipping.");
            return;
        }
        
        depthEstimationTimes.Add(depthTime);
    }    public void StartInpainting()
    {
        if (!isBenchmarking) return;
        
        if (inpaintingTiming)
        {
            Debug.LogWarning("StartInpainting() called while inpainting timing already in progress. Ignoring.");
            return;
        }
        
        inpaintingTiming = true;
        inpaintingStartTime = Time.realtimeSinceStartup;
    }    public void EndInpainting()
    {
        if (!isBenchmarking) return;
        
        if (!inpaintingTiming)
        {
            Debug.LogWarning("EndInpainting() called without corresponding StartInpainting(). Ignoring.");
            return;
        }
        
        inpaintingTiming = false;
        inpaintingEndTime = Time.realtimeSinceStartup;
        float inpaintingTime = inpaintingEndTime - inpaintingStartTime;
        
        // More strict validation
        float benchmarkElapsed = inpaintingEndTime - benchmarkStartTime;
        if (inpaintingTime < 0 || inpaintingTime > 5f || inpaintingTime > benchmarkElapsed)
        {
            Debug.LogWarning($"Anomalous inpainting time detected: {inpaintingTime:F4}s. Start: {inpaintingStartTime:F4}, End: {inpaintingEndTime:F4}, Benchmark elapsed: {benchmarkElapsed:F4}s. Skipping.");
            return;
        }
        
        inpaintingTimes.Add(inpaintingTime);
    }    public void StartPostProcessing()
    {
        if (!isBenchmarking) return;
        
        if (postProcessingTiming)
        {
            Debug.LogWarning("StartPostProcessing() called while post-processing timing already in progress. Ignoring.");
            return;
        }
        
        postProcessingTiming = true;
        postProcessingStartTime = Time.realtimeSinceStartup;
    }    public void EndPostProcessing()
    {
        if (!isBenchmarking) return;
        
        if (!postProcessingTiming)
        {
            Debug.LogWarning("EndPostProcessing() called without corresponding StartPostProcessing(). Ignoring.");
            return;
        }
        
        postProcessingTiming = false;
        postProcessingEndTime = Time.realtimeSinceStartup;
        float postProcessingTime = postProcessingEndTime - postProcessingStartTime;
        
        // More strict validation - post-processing should be very fast
        float benchmarkElapsed = postProcessingEndTime - benchmarkStartTime;
        if (postProcessingTime < 0 || postProcessingTime > 1f || postProcessingTime > benchmarkElapsed)
        {
            Debug.LogWarning($"Anomalous post-processing time detected: {postProcessingTime:F4}s. Start: {postProcessingStartTime:F4}, End: {postProcessingEndTime:F4}, Benchmark elapsed: {benchmarkElapsed:F4}s. Skipping.");
            return;
        }
        
        postProcessingTimes.Add(postProcessingTime);
    }

    private void WriteDataToLog(BenchmarkData data)
    {
        if (!saveToFile) return;
        
        string line = $"{data.timestamp:F4},{data.segmentationTime:F4},{data.depthEstimationTime:F4}," +
                     $"{data.inpaintingTime:F4},{data.postProcessingTime:F4},{data.totalIterationTime:F4}," +
                     $"{data.unityFPS:F2},{data.frameCount}";
        
        File.AppendAllText(logFilePath, line + "\n");
    }

    private void WriteSummaryToLog()
    {
        if (!saveToFile || benchmarkResults.Count == 0) return;

        File.AppendAllText(logFilePath, "\n--- BENCHMARK SUMMARY ---\n");
          float totalDuration = Time.realtimeSinceStartup - benchmarkStartTime;
        int count = benchmarkResults.Count;

        // Calculate statistics for each metric using separate timing lists
        var segStats = segmentationTimes.Count > 0 ? CalculateStatistics(segmentationTimes) : new Statistics();
        var depthStats = depthEstimationTimes.Count > 0 ? CalculateStatistics(depthEstimationTimes) : new Statistics();
        var inpaintStats = inpaintingTimes.Count > 0 ? CalculateStatistics(inpaintingTimes) : new Statistics();
        var postProcStats = postProcessingTimes.Count > 0 ? CalculateStatistics(postProcessingTimes) : new Statistics();
        var iterationStats = CalculateStatistics(benchmarkResults, d => d.totalIterationTime);
        var fpsStats = unityFpsSamples.Count > 0 ? CalculateStatistics(unityFpsSamples) : new Statistics();
        var iterationFpsStats = CalculateStatistics(benchmarkResults, d => d.unityFPS);

        File.AppendAllText(logFilePath, $"Total Duration: {totalDuration:F2}s\n");
        File.AppendAllText(logFilePath, $"Total Iterations: {count}\n");
        File.AppendAllText(logFilePath, $"Average Pipeline FPS: {1.0f / iterationStats.average:F2}\n\n");

        // Segmentation stats
        File.AppendAllText(logFilePath, "SEGMENTATION TIMING:\n");
        File.AppendAllText(logFilePath, $"Average: {segStats.average:F4}s, Min: {segStats.min:F4}s, Max: {segStats.max:F4}s, StdDev: {segStats.stdDev:F4}s\n\n");

        // Depth stats
        File.AppendAllText(logFilePath, "DEPTH ESTIMATION TIMING:\n");
        File.AppendAllText(logFilePath, $"Average: {depthStats.average:F4}s, Min: {depthStats.min:F4}s, Max: {depthStats.max:F4}s, StdDev: {depthStats.stdDev:F4}s\n\n");

        // Inpainting stats
        File.AppendAllText(logFilePath, "INPAINTING TIMING:\n");
        File.AppendAllText(logFilePath, $"Average: {inpaintStats.average:F4}s, Min: {inpaintStats.min:F4}s, Max: {inpaintStats.max:F4}s, StdDev: {inpaintStats.stdDev:F4}s\n\n");

        // Post-processing stats
        File.AppendAllText(logFilePath, "POST-PROCESSING TIMING:\n");
        File.AppendAllText(logFilePath, $"Average: {postProcStats.average:F4}s, Min: {postProcStats.min:F4}s, Max: {postProcStats.max:F4}s, StdDev: {postProcStats.stdDev:F4}s\n\n");

        // Total iteration stats
        File.AppendAllText(logFilePath, "TOTAL ITERATION TIMING:\n");
        File.AppendAllText(logFilePath, $"Average: {iterationStats.average:F4}s, Min: {iterationStats.min:F4}s, Max: {iterationStats.max:F4}s, StdDev: {iterationStats.stdDev:F4}s\n\n");        // Unity FPS stats from independent sampling
        if (unityFpsSamples.Count > 0)
        {
            File.AppendAllText(logFilePath, "UNITY FPS (Independent Sampling):\n");
            File.AppendAllText(logFilePath, $"Average: {fpsStats.average:F2}, Min: {fpsStats.min:F2}, Max: {fpsStats.max:F2}, StdDev: {fpsStats.stdDev:F2}, Samples: {unityFpsSamples.Count}\n\n");
        }

        // Unity FPS stats from iterations (if available)
        if (benchmarkResults.Count > 0)
        {
            File.AppendAllText(logFilePath, "UNITY FPS (Iteration-based):\n");
            File.AppendAllText(logFilePath, $"Average: {iterationFpsStats.average:F2}, Min: {iterationFpsStats.min:F2}, Max: {iterationFpsStats.max:F2}, StdDev: {iterationFpsStats.stdDev:F2}\n");
        }
    }    private void ReportBenchmarkResults()
    {
        // Check if we have any timing data at all
        bool hasData = benchmarkResults.Count > 0 || segmentationTimes.Count > 0 || 
                      depthEstimationTimes.Count > 0 || inpaintingTimes.Count > 0 || 
                      postProcessingTimes.Count > 0;
        
        if (!hasData)
        {
            Debug.Log("No benchmark data collected.");
            return;
        }

        float totalDuration = Time.realtimeSinceStartup - benchmarkStartTime;
        int count = benchmarkResults.Count;

        // Calculate statistics for each metric using separate timing lists
        var segStats = segmentationTimes.Count > 0 ? CalculateStatistics(segmentationTimes) : new Statistics();
        var depthStats = depthEstimationTimes.Count > 0 ? CalculateStatistics(depthEstimationTimes) : new Statistics();
        var inpaintStats = inpaintingTimes.Count > 0 ? CalculateStatistics(inpaintingTimes) : new Statistics();
        var postProcStats = postProcessingTimes.Count > 0 ? CalculateStatistics(postProcessingTimes) : new Statistics();
        var iterationStats = CalculateStatistics(benchmarkResults, d => d.totalIterationTime);
        var fpsStats = unityFpsSamples.Count > 0 ? CalculateStatistics(unityFpsSamples) : new Statistics();
        var iterationFpsStats = CalculateStatistics(benchmarkResults, d => d.unityFPS);        // Build comprehensive report
        System.Text.StringBuilder report = new System.Text.StringBuilder();
        report.AppendLine("\n=== BENCHMARK RESULTS ===");
        report.AppendLine($"Total Duration: {totalDuration:F2}s");
        report.AppendLine($"Individual Stage Timing Results:");
        report.AppendLine();

        if (segmentationTimes.Count > 0)
        {
            report.AppendLine("SEGMENTATION TIMING:");
            report.AppendLine($"  Average: {segStats.average:F4}s ({1.0f / segStats.average:F1} FPS)");
            report.AppendLine($"  Minimum: {segStats.min:F4}s ({1.0f / segStats.min:F1} FPS)");
            report.AppendLine($"  Maximum: {segStats.max:F4}s ({1.0f / segStats.max:F1} FPS)");
            report.AppendLine($"  Std Dev: {segStats.stdDev:F4}s");
            report.AppendLine($"  Total Executions: {segmentationTimes.Count}");
            report.AppendLine();
        }

        if (depthEstimationTimes.Count > 0)
        {
            report.AppendLine("DEPTH ESTIMATION TIMING:");
            report.AppendLine($"  Average: {depthStats.average:F4}s ({1.0f / depthStats.average:F1} FPS)");
            report.AppendLine($"  Minimum: {depthStats.min:F4}s ({1.0f / depthStats.min:F1} FPS)");
            report.AppendLine($"  Maximum: {depthStats.max:F4}s ({1.0f / depthStats.max:F1} FPS)");
            report.AppendLine($"  Std Dev: {depthStats.stdDev:F4}s");
            report.AppendLine($"  Total Executions: {depthEstimationTimes.Count}");
            report.AppendLine();
        }

        if (inpaintingTimes.Count > 0)
        {
            report.AppendLine("INPAINTING TIMING:");
            report.AppendLine($"  Average: {inpaintStats.average:F4}s ({1.0f / inpaintStats.average:F1} FPS)");
            report.AppendLine($"  Minimum: {inpaintStats.min:F4}s ({1.0f / inpaintStats.min:F1} FPS)");
            report.AppendLine($"  Maximum: {inpaintStats.max:F4}s ({1.0f / inpaintStats.max:F1} FPS)");
            report.AppendLine($"  Std Dev: {inpaintStats.stdDev:F4}s");
            report.AppendLine($"  Total Executions: {inpaintingTimes.Count}");
            report.AppendLine();
        }

        if (postProcessingTimes.Count > 0)
        {
            report.AppendLine("POST-PROCESSING TIMING:");
            report.AppendLine($"  Average: {postProcStats.average:F4}s ({1.0f / postProcStats.average:F1} FPS)");
            report.AppendLine($"  Minimum: {postProcStats.min:F4}s ({1.0f / postProcStats.min:F1} FPS)");
            report.AppendLine($"  Maximum: {postProcStats.max:F4}s ({1.0f / postProcStats.max:F1} FPS)");
            report.AppendLine($"  Std Dev: {postProcStats.stdDev:F4}s");
            report.AppendLine($"  Total Executions: {postProcessingTimes.Count}");
            report.AppendLine();        }

        // Always show Unity FPS if we have samples
        if (unityFpsSamples.Count > 0)
        {
            report.AppendLine("UNITY FPS:");
            report.AppendLine($"  Average: {fpsStats.average:F2}");
            report.AppendLine($"  Minimum: {fpsStats.min:F2}");
            report.AppendLine($"  Maximum: {fpsStats.max:F2}");
            report.AppendLine($"  Std Dev: {fpsStats.stdDev:F2}");
            report.AppendLine($"  Total Samples: {unityFpsSamples.Count}");
            report.AppendLine();
        }

        // Only show iteration stats if we have iteration data
        if (benchmarkResults.Count > 0)
        {
            report.AppendLine($"Total Iterations: {count}");
            report.AppendLine($"Average Pipeline FPS: {1.0f / iterationStats.average:F2}");
            report.AppendLine();
            
            report.AppendLine("TOTAL ITERATION TIMING:");
            report.AppendLine($"  Average: {iterationStats.average:F4}s ({1.0f / iterationStats.average:F1} FPS)");
            report.AppendLine($"  Minimum: {iterationStats.min:F4}s ({1.0f / iterationStats.min:F1} FPS)");
            report.AppendLine($"  Maximum: {iterationStats.max:F4}s ({1.0f / iterationStats.max:F1} FPS)");
            report.AppendLine($"  Std Dev: {iterationStats.stdDev:F4}s");
            report.AppendLine();

            // Show iteration-based FPS stats if different from independent sampling
            if (benchmarkResults.Count > 0 && unityFpsSamples.Count > 0)
            {
                report.AppendLine("ITERATION-BASED UNITY FPS:");
                report.AppendLine($"  Average: {iterationFpsStats.average:F2}");
                report.AppendLine($"  Minimum: {iterationFpsStats.min:F2}");
                report.AppendLine($"  Maximum: {iterationFpsStats.max:F2}");
                report.AppendLine($"  Std Dev: {iterationFpsStats.stdDev:F2}");
                report.AppendLine();
            }
        }

        report.AppendLine("=========================");

        Debug.Log(report.ToString());
    }public struct Statistics
    {
        public float min;
        public float max;
        public float average;
        public float stdDev;
    }

    private Statistics CalculateStatistics(List<BenchmarkData> data, System.Func<BenchmarkData, float> selector)
    {
        if (data.Count == 0) return new Statistics();

        float min = float.MaxValue;
        float max = float.MinValue;
        float sum = 0f;

        foreach (var item in data)
        {
            float value = selector(item);
            if (value < min) min = value;
            if (value > max) max = value;
            sum += value;
        }

        float average = sum / data.Count;

        // Calculate standard deviation
        float sumSquaredDiff = 0f;
        foreach (var item in data)
        {
            float value = selector(item);
            float diff = value - average;
            sumSquaredDiff += diff * diff;
        }
        float stdDev = Mathf.Sqrt(sumSquaredDiff / data.Count);
        return new Statistics
        {
            min = min,
            max = max,
            average = average,
            stdDev = stdDev
        };
    }

    private Statistics CalculateStatistics(List<float> data)
    {
        if (data.Count == 0) return new Statistics();

        float min = float.MaxValue;
        float max = float.MinValue;
        float sum = 0f;

        foreach (float value in data)
        {
            if (value < min) min = value;
            if (value > max) max = value;
            sum += value;
        }

        float average = sum / data.Count;

        // Calculate standard deviation
        float sumSquaredDiff = 0f;
        foreach (float value in data)
        {
            float diff = value - average;
            sumSquaredDiff += diff * diff;
        }
        float stdDev = Mathf.Sqrt(sumSquaredDiff / data.Count);

        return new Statistics
        {
            min = min,
            max = max,
            average = average,
            stdDev = stdDev
        };
    }

    // Public properties for UI display
    public float BenchmarkDuration => isBenchmarking ? Time.realtimeSinceStartup - benchmarkStartTime : 0f;
    public int IterationCount => frameCounter;
    public string LogFilePath => logFilePath;
    public bool SaveToFile { get => saveToFile; set => saveToFile = value; }
    
    // Public method to get current results
    public BenchmarkData[] GetBenchmarkResults()
    {
        return benchmarkResults.ToArray();
    }
    
    // Public method to manually trigger results report
    public void PrintBenchmarkResults()
    {
        ReportBenchmarkResults();
    }
    
    // Methods to get individual stage statistics for debugging/UI
    public Statistics GetSegmentationStats()
    {
        return segmentationTimes.Count > 0 ? CalculateStatistics(segmentationTimes) : new Statistics();
    }
    
    public Statistics GetDepthEstimationStats()
    {
        return depthEstimationTimes.Count > 0 ? CalculateStatistics(depthEstimationTimes) : new Statistics();
    }
    
    public Statistics GetInpaintingStats()
    {
        return inpaintingTimes.Count > 0 ? CalculateStatistics(inpaintingTimes) : new Statistics();
    }
    
    public Statistics GetPostProcessingStats()
    {
        return postProcessingTimes.Count > 0 ? CalculateStatistics(postProcessingTimes) : new Statistics();
    }
    
    public Statistics GetUnityFpsStats()
    {
        return unityFpsSamples.Count > 0 ? CalculateStatistics(unityFpsSamples) : new Statistics();
    }
    
    // Get count of recorded timings for each stage
    public int SegmentationCount => segmentationTimes.Count;
    public int DepthEstimationCount => depthEstimationTimes.Count;
    public int InpaintingCount => inpaintingTimes.Count;
    public int PostProcessingCount => postProcessingTimes.Count;
    public int UnityFpsSampleCount => unityFpsSamples.Count;
}
