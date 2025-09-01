using System;
using System.Collections;
using System.Collections.Generic;
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
    
    private float benchmarkStartTime;
    private float lastIterationStartTime;
    private float currentIterationStartTime;
    private List<BenchmarkData> benchmarkResults = new List<BenchmarkData>();
    private int frameCounter = 0;
    
    // Timing data for current iteration
    private float segmentationStartTime;
    private float segmentationEndTime;
    private float depthStartTime;
    private float depthEndTime;
    private float inpaintingStartTime;
    private float inpaintingEndTime;
    private float postProcessingStartTime;
    private float postProcessingEndTime;
    
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
    }

    void Update()
    {
        if (isBenchmarking)
        {
            // Check if benchmark should auto-stop
            if (Time.realtimeSinceStartup - benchmarkStartTime >= maxBenchmarkDuration)
            {
                StopBenchmark();
            }
        }
    }

    public void StartBenchmark(float duration = 60f)
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
        benchmarkResults.Clear();
        
        Debug.Log($"Benchmark started for {duration} seconds.");
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
        
        // Report summary
        ReportBenchmarkResults();
        
        float totalDuration = Time.realtimeSinceStartup - benchmarkStartTime;
        Debug.Log($"Benchmark stopped. Total duration: {totalDuration:F2}s");
        OnBenchmarkStopped?.Invoke();
    }

    public void StartIteration()
    {
        if (!isBenchmarking) return;
        
        currentIterationStartTime = Time.realtimeSinceStartup;
        frameCounter++;
    }

    public void EndIteration()
    {
        if (!isBenchmarking) return;
        
        float iterationTime = Time.realtimeSinceStartup - currentIterationStartTime;
        float unityFPS = 1.0f / Time.deltaTime;
        
        BenchmarkData data = new BenchmarkData
        {
            timestamp = Time.realtimeSinceStartup - benchmarkStartTime,
            segmentationTime = segmentationEndTime - segmentationStartTime,
            depthEstimationTime = depthEndTime - depthStartTime,
            inpaintingTime = inpaintingEndTime - inpaintingStartTime,
            postProcessingTime = postProcessingEndTime - postProcessingStartTime,
            totalIterationTime = iterationTime,
            unityFPS = unityFPS,
            frameCount = frameCounter
        };
        
        benchmarkResults.Add(data);
    }

    // Timing methods for each pipeline stage
    public void StartSegmentation()
    {
        if (!isBenchmarking) return;
        segmentationStartTime = Time.realtimeSinceStartup;
    }

    public void EndSegmentation()
    {
        if (!isBenchmarking) return;
        segmentationEndTime = Time.realtimeSinceStartup;
    }

    public void StartDepthEstimation()
    {
        if (!isBenchmarking) return;
        depthStartTime = Time.realtimeSinceStartup;
    }

    public void EndDepthEstimation()
    {
        if (!isBenchmarking) return;
        depthEndTime = Time.realtimeSinceStartup;
    }

    public void StartInpainting()
    {
        if (!isBenchmarking) return;
        inpaintingStartTime = Time.realtimeSinceStartup;
    }

    public void EndInpainting()
    {
        if (!isBenchmarking) return;
        inpaintingEndTime = Time.realtimeSinceStartup;
    }

    public void StartPostProcessing()
    {
        if (!isBenchmarking) return;
        postProcessingStartTime = Time.realtimeSinceStartup;
    }

    public void EndPostProcessing()
    {
        if (!isBenchmarking) return;
        postProcessingEndTime = Time.realtimeSinceStartup;
    }

    private void ReportBenchmarkResults()
    {
        if (benchmarkResults.Count == 0)
        {
            Debug.Log("No benchmark data collected.");
            return;
        }

        float totalDuration = Time.realtimeSinceStartup - benchmarkStartTime;
        int count = benchmarkResults.Count;

        // Calculate statistics for each metric
        var segStats = CalculateStatistics(benchmarkResults, d => d.segmentationTime);
        var depthStats = CalculateStatistics(benchmarkResults, d => d.depthEstimationTime);
        var inpaintStats = CalculateStatistics(benchmarkResults, d => d.inpaintingTime);
        var postProcStats = CalculateStatistics(benchmarkResults, d => d.postProcessingTime);
        var iterationStats = CalculateStatistics(benchmarkResults, d => d.totalIterationTime);
        var fpsStats = CalculateStatistics(benchmarkResults, d => d.unityFPS);

        // Build comprehensive report
        System.Text.StringBuilder report = new System.Text.StringBuilder();
        report.AppendLine("\n=== BENCHMARK RESULTS ===");
        report.AppendLine($"Total Duration: {totalDuration:F2}s");
        report.AppendLine($"Total Iterations: {count}");
        report.AppendLine($"Average Pipeline FPS: {1.0f / iterationStats.average:F2}");
        report.AppendLine();

        report.AppendLine("SEGMENTATION TIMING:");
        report.AppendLine($"  Average: {segStats.average:F4}s ({1.0f / segStats.average:F1} FPS)");
        report.AppendLine($"  Minimum: {segStats.min:F4}s ({1.0f / segStats.min:F1} FPS)");
        report.AppendLine($"  Maximum: {segStats.max:F4}s ({1.0f / segStats.max:F1} FPS)");
        report.AppendLine($"  Std Dev: {segStats.stdDev:F4}s");
        report.AppendLine();

        report.AppendLine("DEPTH ESTIMATION TIMING:");
        report.AppendLine($"  Average: {depthStats.average:F4}s ({1.0f / depthStats.average:F1} FPS)");
        report.AppendLine($"  Minimum: {depthStats.min:F4}s ({1.0f / depthStats.min:F1} FPS)");
        report.AppendLine($"  Maximum: {depthStats.max:F4}s ({1.0f / depthStats.max:F1} FPS)");
        report.AppendLine($"  Std Dev: {depthStats.stdDev:F4}s");
        report.AppendLine();

        report.AppendLine("INPAINTING TIMING:");
        report.AppendLine($"  Average: {inpaintStats.average:F4}s ({1.0f / inpaintStats.average:F1} FPS)");
        report.AppendLine($"  Minimum: {inpaintStats.min:F4}s ({1.0f / inpaintStats.min:F1} FPS)");
        report.AppendLine($"  Maximum: {inpaintStats.max:F4}s ({1.0f / inpaintStats.max:F1} FPS)");
        report.AppendLine($"  Std Dev: {inpaintStats.stdDev:F4}s");
        report.AppendLine();

        report.AppendLine("POST-PROCESSING TIMING:");
        report.AppendLine($"  Average: {postProcStats.average:F4}s ({1.0f / postProcStats.average:F1} FPS)");
        report.AppendLine($"  Minimum: {postProcStats.min:F4}s ({1.0f / postProcStats.min:F1} FPS)");
        report.AppendLine($"  Maximum: {postProcStats.max:F4}s ({1.0f / postProcStats.max:F1} FPS)");
        report.AppendLine($"  Std Dev: {postProcStats.stdDev:F4}s");
        report.AppendLine();

        report.AppendLine("TOTAL ITERATION TIMING:");
        report.AppendLine($"  Average: {iterationStats.average:F4}s ({1.0f / iterationStats.average:F1} FPS)");
        report.AppendLine($"  Minimum: {iterationStats.min:F4}s ({1.0f / iterationStats.min:F1} FPS)");
        report.AppendLine($"  Maximum: {iterationStats.max:F4}s ({1.0f / iterationStats.max:F1} FPS)");
        report.AppendLine($"  Std Dev: {iterationStats.stdDev:F4}s");
        report.AppendLine();

        report.AppendLine("UNITY FPS:");
        report.AppendLine($"  Average: {fpsStats.average:F2}");
        report.AppendLine($"  Minimum: {fpsStats.min:F2}");
        report.AppendLine($"  Maximum: {fpsStats.max:F2}");
        report.AppendLine($"  Std Dev: {fpsStats.stdDev:F2}");
        report.AppendLine("=========================");

        Debug.Log(report.ToString());
    }

    private struct Statistics
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

    // Public properties for UI display
    public float BenchmarkDuration => isBenchmarking ? Time.realtimeSinceStartup - benchmarkStartTime : 0f;
    public int IterationCount => frameCounter;
    
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
}
