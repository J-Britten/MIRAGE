using UnityEngine;
using UnityEngine.UI;

public class YOLO3DExecutor : MonoBehaviour
{
    public Texture2D[] DebugInput;

    public bool LiveInput = false;
    [Header("Performance Display")]
    public Text performanceText; // Optional UI Text to display performance metrics
    
    private YOLOSegmentationRunner segRunner;
    private YOLO3DRunner yolo3DRunner;
    
    // Timing for overall pipeline
    private System.Diagnostics.Stopwatch pipelineTimer;
    private float lastPipelineTime = 0f;
    private float averagePipelineTime = 0f;
    private int pipelineExecutionCount = 0;
    // Start is called before the first frame update
    void Start()
    {
        segRunner = FindFirstObjectByType<YOLOSegmentationRunner>();
        yolo3DRunner = FindFirstObjectByType<YOLO3DRunner>();
        pipelineTimer = new System.Diagnostics.Stopwatch();
        
        if (!LiveInput)
        {
            // Debug.Log(DebugInput.width + " " + DebugInput.height);
            StartCoroutine(RunPipelineWithTiming(DebugInput));
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(LiveInput) {
            if (!segRunner.IsRunning && !yolo3DRunner.IsRunning)
            {
                StartCoroutine(RunPipelineWithTiming(DebugVideoPlayerInput.Instance.CurrentFrame));
            }
        }
        
        // Update performance display if UI text is assigned
        UpdatePerformanceDisplay();
    }
    
    private System.Collections.IEnumerator RunPipelineWithTiming(params Texture[] inputs)
    {
        pipelineTimer.Restart();
        
        // First run YOLO segmentation to get bounding boxes
        yield return StartCoroutine(segRunner.RunModel(inputs));
        
        // Then run YOLO3D with the detected bounding boxes
        yield return StartCoroutine(yolo3DRunner.RunModel(inputs));
        
        // Stop timing and update pipeline metrics
        pipelineTimer.Stop();
        UpdatePipelineMetrics();
    }
    
    private void UpdatePipelineMetrics()
    {
        lastPipelineTime = (float)pipelineTimer.Elapsed.TotalMilliseconds;
        pipelineExecutionCount++;
        
        // Calculate running average
        if (pipelineExecutionCount == 1)
        {
            averagePipelineTime = lastPipelineTime;
        }
        else
        {
            averagePipelineTime = ((averagePipelineTime * (pipelineExecutionCount - 1)) + lastPipelineTime) / pipelineExecutionCount;
        }
        
        Debug.Log($"Full Pipeline Time: {lastPipelineTime:F2}ms | Average: {averagePipelineTime:F2}ms | Count: {pipelineExecutionCount}");
    }
    
    private void UpdatePerformanceDisplay()
    {
        if (performanceText != null)
        {
            string displayText = $"YOLO3D Performance:\n";
            displayText += $"Last Execution: {yolo3DRunner.LastExecutionTime:F2}ms\n";
            displayText += $"Average: {yolo3DRunner.AverageExecutionTime:F2}ms\n";
            displayText += $"Executions: {yolo3DRunner.ExecutionCount}\n\n";
            displayText += $"Pipeline Performance:\n";
            displayText += $"Last Pipeline: {lastPipelineTime:F2}ms\n";
            displayText += $"Average Pipeline: {averagePipelineTime:F2}ms\n";
            displayText += $"Pipeline Count: {pipelineExecutionCount}";
            
            performanceText.text = displayText;
        }
    }
}
