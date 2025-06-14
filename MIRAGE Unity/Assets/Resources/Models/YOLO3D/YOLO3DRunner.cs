using System.Collections;
using Unity.Collections;
using Unity.InferenceEngine;
using UnityEngine;

/// <summary>
/// YOLO3D Runner that processes cropped bounding boxes from YOLO detection. 
/// Model from: https://huggingface.co/qualcomm/3D-Deep-BOX
/// Original repository: https://github.com/skhadem/3D-BoundingBox/ 
/// 
/// Simple implementation that takes the first detected bounding box from a YOLO runner,
/// crops the input image based on that bounding box, and runs it through a YOLO3D model
/// that expects input shape 1x3x224x224.
/// 
/// 
/// TODO: While this does somewhat work, it is not optimized for multiple detections. Pre-And post-processing are also slow.
/// This should be improved through using better cropping techniques, e.g. shaders or grind sampling.
/// Author: J-Britten
/// </summary>
public class YOLO3DRunner : ModelRunner
{
    #region Variables
    
    [Header("YOLO3D Settings")]
    [Tooltip("The YOLO segmentation runner that provides bounding boxes")]
    public YOLOSegmentationRunner yoloRunner;
    
    [Tooltip("Minimum confidence threshold for processing a bounding box")]
    public float confidenceThreshold = 0.5f;
    
    // Input/Output tensors
    private Tensor<float> inputTensor;
    private Tensor<float> bboxTensor;
    private Tensor<float> outputTensor;
    
    // Texture transform for input
    private TextureTransform inputTextureTransform;
    
    // Combined graph for cropping and YOLO3D inference
    private FunctionalGraph combinedGraph;
    private FunctionalTensor croppedAndScaledTensor;
    
    // Output data
    private NativeArray<float> outputData;
    private bool outputReadRequest = false;
    
    // Timing measurements
    [Header("Performance Metrics")]
    [SerializeField] private float lastExecutionTime = 0f;
    [SerializeField] private float averageExecutionTime = 0f;
    [SerializeField] private int executionCount = 0;
    private System.Diagnostics.Stopwatch executionTimer;
    
    // Current detection tracking
    private int currentDetectionIndex = 0;
    private System.Diagnostics.Stopwatch detectionTimer;
    private float lastDetectionTime = 0f;
    
    public float LastExecutionTime => lastExecutionTime;
    public float AverageExecutionTime => averageExecutionTime;
    public int ExecutionCount => executionCount;
    
    #endregion
    
    #region Model Preparation
    
    protected override void PrepareModel()
    {
        // Create texture transform for input processing
        inputTextureTransform = new TextureTransform()
            .SetChannelSwizzle(ChannelSwizzle.RGBA)
            .SetTensorLayout(TensorLayout.NCHW);
        
        // Create combined functional graph that handles cropping and YOLO3D inference
        CreateCombinedGraph();
        
        // Initialize timing
        executionTimer = new System.Diagnostics.Stopwatch();
        detectionTimer = new System.Diagnostics.Stopwatch();
    }
    
    private void CreateCombinedGraph()
    {
        // Load YOLO3D model first
        var yolo3dModel = ModelLoader.Load(ModelAsset);
        
        // Create functional graph for combined cropping and inference
        combinedGraph = new FunctionalGraph();
        
        // Input texture tensor (full image)
        var inputImage = combinedGraph.AddInput(DataType.Float, new DynamicTensorShape(1, 3, -1, -1));
        
        // Bounding box tensor input (4,) - we'll use the first bbox [centerX, centerY, width, height]
        var bboxInput = combinedGraph.AddInput(DataType.Float, new DynamicTensorShape(4));
        
        // Extract bbox components (each is a scalar)
        var centerX = bboxInput[0];
        var centerY = bboxInput[1]; 
        var width = bboxInput[2];
        var height = bboxInput[3];
        
        // Calculate crop bounds in pixel coordinates
        var halfWidth = width / 2.0f;
        var halfHeight = height / 2.0f;
        var cropLeft = centerX - halfWidth;
        var cropTop = centerY - halfHeight;
        var cropRight = centerX + halfWidth;
        var cropBottom = centerY + halfHeight;
        
        // Clamp to image bounds using Max/Min operations
        cropLeft = Functional.Max(cropLeft, Functional.Constant(0.0f));
        cropTop = Functional.Max(cropTop, Functional.Constant(0.0f));
        cropRight = Functional.Min(cropRight, Functional.Constant((float)ImageWidth));
        cropBottom = Functional.Min(cropBottom, Functional.Constant((float)ImageHeight));
        
        // Convert to normalized coordinates for grid sampling [-1, 1]
        var normLeft = (cropLeft / ImageWidth) * 2.0f - 1.0f;
        var normTop = (cropTop / ImageHeight) * 2.0f - 1.0f;
        var normRight = (cropRight / ImageWidth) * 2.0f - 1.0f;
        var normBottom = (cropBottom / ImageHeight) * 2.0f - 1.0f;
        
        // Create sampling grid for 224x224 output
        var gridCoords = CreateSamplingGrid(224, 224, normLeft, normTop, normRight, normBottom);
        
        // Apply grid sampling to crop the image
        var croppedImage = Functional.GridSample(inputImage, gridCoords, "bilinear", "zeros");
        
        // Apply YOLO3D model to the cropped image
        var yolo3dOutputs = Functional.Forward(yolo3dModel, croppedImage);
        croppedAndScaledTensor = yolo3dOutputs[0];
        
        // Compile the combined model
        runtimeModel = combinedGraph.Compile(croppedAndScaledTensor, croppedImage);
    }
    
    private FunctionalTensor CreateSamplingGrid(int height, int width, 
                                               FunctionalTensor normLeft, FunctionalTensor normTop,
                                               FunctionalTensor normRight, FunctionalTensor normBottom)
    {
        // Create a base grid for the output dimensions
        var gridCoords = new float[height * width * 2];
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Normalize pixel coordinates to [0, 1] within output dimensions
                float u = (float)x / (width - 1);   // [0, 1]
                float v = (float)y / (height - 1);  // [0, 1]
                
                int idx = (y * width + x) * 2;
                gridCoords[idx] = u;     // X coordinate
                gridCoords[idx + 1] = v; // Y coordinate
            }
        }
        
        // Create constant tensor with base grid coordinates
        var baseGrid = Functional.Constant(new TensorShape(1, height, width, 2), gridCoords);
        
        // Extract U and V coordinates
        var gridU = baseGrid[.., .., .., 0];
        var gridV = baseGrid[.., .., .., 1];
        
        // Map from [0,1] to crop region in normalized space [-1,1]
        var cropWidth = normRight - normLeft;
        var cropHeight = normBottom - normTop;
        
        var mappedX = normLeft + gridU * cropWidth;
        var mappedY = normTop + gridV * cropHeight;
        
        // Stack X and Y coordinates back together
        mappedX = Functional.Unsqueeze(mappedX, -1);
        mappedY = Functional.Unsqueeze(mappedY, -1);
        return Functional.Concat(new[] { mappedX, mappedY }, -1);
    }
    
    #endregion
    
    #region Model Execution
    
    public override IEnumerator RunModel(params Texture[] inputs)
    {
        // Start timing
        executionTimer.Restart();
        
        if (inputs.Length == 0 || yoloRunner == null)
        {
            yield break;
        }
        
        Texture inputTexture = inputs[0];
        
        // Check if YOLO has detected any objects
        if (yoloRunner.NumObjDetected == 0)
        {
            yield break;
        }
        
        // Create input image tensor once (reused for all detections)
        if (inputTensor != null) inputTensor.Dispose();
        inputTensor = new Tensor<float>(new TensorShape(1, 3, ImageHeight, ImageWidth));
        TextureConverter.ToTensor(inputTexture, inputTensor, inputTextureTransform);
        
        // Get bounding box data from YOLO tensor
        var bboxTensorData = ComputeTensorData.Pin(yoloRunner.BBoxTensor);
        var bboxBuffer = bboxTensorData.buffer;
        
        // Debug tensor shape and buffer size
        Debug.Log($"BBoxTensor shape: {yoloRunner.BBoxTensor.shape}");
        Debug.Log($"Buffer count: {bboxBuffer.count}, NumDetections: {yoloRunner.NumObjDetected}");
        
        // Iterate over each detected object
        for (int i = 0; i < yoloRunner.NumObjDetected; i++)
        {
            currentDetectionIndex = i;
            detectionTimer.Restart();
            Debug.Log($"Processing detection {i + 1}/{yoloRunner.NumObjDetected}");
            
            // Extract bounding box for current detection (each bbox is 4 floats: centerX, centerY, width, height)
            // The tensor is shaped (N, 4), so we need to read 4 elements starting at offset i*4
            float[] currentBbox = new float[4];
            
            // Check bounds before accessing
            int startIndex = i * 4;
            if (startIndex + 4 > bboxBuffer.count)
            {
                Debug.LogError($"Buffer access out of bounds: trying to access {startIndex}-{startIndex + 3} but buffer size is {bboxBuffer.count}");
                break;
            }
            
            bboxBuffer.GetData(currentBbox, 0, startIndex, 4);
            
            float centerX = currentBbox[0];
            float centerY = currentBbox[1];
            float width = currentBbox[2];
            float height = currentBbox[3];
            
            Debug.Log($"Detection {i}: bbox({centerX:F2}, {centerY:F2}, {width:F2}, {height:F2})");
            
            // Create bounding box tensor for current detection
            if (bboxTensor != null) bboxTensor.Dispose();
            bboxTensor = new Tensor<float>(new TensorShape(4), new[] { centerX, centerY, width, height });
            
            // Run the combined inference for this detection
            schedule = worker.ScheduleIterable(inputTensor, bboxTensor);
            yield return StartCoroutine(RunInference());
            
            // Stop detection timing
            detectionTimer.Stop();
            lastDetectionTime = (float)detectionTimer.Elapsed.TotalMilliseconds;
            
            // Process results for this detection
            Debug.Log($"Completed processing detection {i + 1} in {lastDetectionTime:F2}ms");
        }
        
        Debug.Log($"Finished processing all {yoloRunner.NumObjDetected} detections");
    }
    
    #endregion
    
    #region Inference Processing
    
    protected override void PeekOutput()
    {
        outputTensor = worker.PeekOutput(0) as Tensor<float>;
        var croppedImage = worker.PeekOutput(1) as Tensor<float>;
        Debug.Log(outputTensor.shape);
        Debug.Log(croppedImage.shape);
        if (outputTensor != null)
        {
            outputTensor.ReadbackRequest();
            outputReadRequest = true;
        }
    }
    
    protected override bool RequestsDone()
    {
        return outputReadRequest && outputTensor != null && outputTensor.IsReadbackRequestDone();
    }
    
    protected override void ReadOutput()
    {
        if (outputTensor != null && outputReadRequest)
        {
            //outputData = outputTensor.DownloadToNativeArray();
            //ProcessYOLO3DOutput();
            
            // Stop timing and update metrics
            executionTimer.Stop();
            UpdateExecutionMetrics();
        }
    }
    
    private void UpdateExecutionMetrics()
    {
        lastExecutionTime = (float)executionTimer.Elapsed.TotalMilliseconds;
        executionCount++;
        
        // Calculate running average
        if (executionCount == 1)
        {
            averageExecutionTime = lastExecutionTime;
        }
        else
        {
            averageExecutionTime = ((averageExecutionTime * (executionCount - 1)) + lastExecutionTime) / executionCount;
        }
        
        Debug.Log($"YOLO3D Execution Time: {lastExecutionTime:F2}ms | Average: {averageExecutionTime:F2}ms | Count: {executionCount}");
    }
    
    private void ProcessYOLO3DOutput()
    {
        // Process the YOLO3D output here
        // This will depend on your specific YOLO3D model's output format
        Debug.Log($"YOLO3D processed detection {currentDetectionIndex + 1} with {outputData.Length} output values");
        
        // Example: Extract 3D bounding box information
        // The exact processing will depend on your model's output format
        // Typically YOLO3D outputs: class, confidence, 3D bbox coords, rotation, etc.
        
        // You can access the output data for the current detection here
        // For example, if your model outputs:
        // - First 7 values: 3D bounding box center (x,y,z) + dimensions (w,h,l) + rotation
        // - Next values: class probabilities
        // - etc.
        
        if (outputData.Length >= 7)
        {
            Debug.Log($"Detection {currentDetectionIndex + 1} - 3D bbox center: ({outputData[0]:F3}, {outputData[1]:F3}, {outputData[2]:F3})");
            Debug.Log($"Detection {currentDetectionIndex + 1} - 3D bbox size: ({outputData[3]:F3}, {outputData[4]:F3}, {outputData[5]:F3})");
            Debug.Log($"Detection {currentDetectionIndex + 1} - Rotation: {outputData[6]:F3}");
        }
    }
    

    #endregion
    
    #region Cleanup
    
    public override void DisposeOutput()
    {
        if (inputTensor != null)
        {
            inputTensor.Dispose();
            inputTensor = null;
        }
        
        if (bboxTensor != null)
        {
            bboxTensor.Dispose();
            bboxTensor = null;
        }
        
        if (outputTensor != null)
        {
            outputTensor.Dispose();
            outputTensor = null;
        }
        
        if (outputData.IsCreated)
        {
            outputData.Dispose();
        }
    }
    
    private void OnDestroy()
    {
        DisposeOutput();
        
        if (worker != null)
        {
            worker.Dispose();
        }
    }
    
    #endregion
}
