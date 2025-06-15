using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main Script that initializes and executes the pipeline
/// 
/// Author: J-Britten
/// </summary>
public class Pipeline : MonoBehaviour
{
    public bool RunAsSequence = false;

    public RawImage OutputImage;
    public RawImage PostProcessingOutputImage;
    public RawImage ImagePostprocessingOutputImage;

    public RectTransform CPUPostProcessingContainer;
    public TMP_Text segmentationTimeText;
    public TMP_Text depthEstimationTimeText;
    public TMP_Text inpaintingTimeText;
    public TMP_Text postProcessingTimeText;

    private SegmentationRunner segmentationModel;
    private DepthEstimationRunner depthModel;
    private InpaintingRunner inpaintingModel;

    private CPUPostProcessor[] cpuPostProcessors; 
    
    private PostProcessor[] gpuPostProcessors;
    private RenderTexture postProcessingOverlay;
    private RenderTexture imagePostProcessingOverlay;
    private RenderTexture inputTexture { get => CameraInput.Instance.CurrentFrame; }

    // Benchmark integration
    private BenchmarkManager benchmarkManager;

#region Initialization
    void Awake()
    {     
       InitializePipeline();
    }

    void Start()
    {
        if (RunAsSequence)
        {
            StartCoroutine(RunSequentialPipeline());
        } else {
            StartCoroutine(RunSegmentation());
            StartCoroutine(RunDepthEstimation());
            StartCoroutine(RunInpainting());
            StartCoroutine(RunPostProcessing());
        }
    }

    /// <summary>
    /// Initialize the pipeline
    /// </summary>
    private void InitializePipeline() {
        //Find the models
        segmentationModel = FindAnyObjectByType<SegmentationRunner>();
        inpaintingModel = FindAnyObjectByType<InpaintingRunner>();
        depthModel = FindAnyObjectByType<DepthEstimationRunner>();

        //Setup render textures
        postProcessingOverlay = new RenderTexture(segmentationModel.OutputWidth, segmentationModel.OutputHeight, 0,UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm);
        postProcessingOverlay.enableRandomWrite = true;
        postProcessingOverlay.Create();

        imagePostProcessingOverlay = new RenderTexture(segmentationModel.ImageWidth, segmentationModel.ImageHeight, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm);
        imagePostProcessingOverlay.enableRandomWrite = true;
        imagePostProcessingOverlay.Create();

        //Initialize postprocessors
        if (segmentationModel is YOLOSegmentationRunner)
        {

            cpuPostProcessors = FindObjectsByType<CPUPostProcessor>(FindObjectsSortMode.None);
            foreach (var processor in cpuPostProcessors)
            {
                processor.Initialize(segmentationModel as YOLOSegmentationRunner, depthModel, CPUPostProcessingContainer);
            }
        }

        gpuPostProcessors = FindObjectsByType<PostProcessor>(FindObjectsSortMode.None);
        foreach(var processor in FindObjectsByType<PostProcessor>(FindObjectsSortMode.None)) {

            if (processor is ImagePostProcessor) {
                var imageProcessor = processor as ImagePostProcessor;
                imageProcessor.Initialize(segmentationModel, depthModel, imagePostProcessingOverlay);
            }
            else {
                processor.Initialize(segmentationModel, depthModel, postProcessingOverlay);
            }
        }

        // Initialize Benchmark Manager
        benchmarkManager = FindObjectOfType<BenchmarkManager>(true);
    }
#endregion

#region Parallel Execution
    private IEnumerator RunSegmentation()
    {
        while (true)
        {
            // Start iteration timing for benchmark
            if (benchmarkManager != null)
                benchmarkManager.StartIteration();

            // Start segmentation timing
            if (benchmarkManager != null)
                benchmarkManager.StartSegmentation();

            float startTime = Time.realtimeSinceStartup;
            yield return segmentationModel.RunModel(inputTexture);
            float endTime = Time.realtimeSinceStartup;
            float deltaTime = endTime - startTime;

            // End segmentation timing
            if (benchmarkManager != null)
                benchmarkManager.EndSegmentation();

            segmentationTimeText.text = "Segmentation Time: " + deltaTime.ToString("F4") + "s\nFPS: " + (1.0f / deltaTime).ToString("F2");
        }
    }

    private IEnumerator RunDepthEstimation()
    {
        while (true)
        {
            if (depthModel.IsEnabled)
            {
                // Start depth estimation timing
                if (benchmarkManager != null)
                    benchmarkManager.StartDepthEstimation();

                float startTime = Time.realtimeSinceStartup;
                yield return depthModel.RunModel(inputTexture);
                depthModel.CalculateObjectDepths(segmentationModel.OutputBuffer, segmentationModel.NumObjDetected);
                float endTime = Time.realtimeSinceStartup;
                float deltaTime = endTime - startTime;

                // End depth estimation timing
                if (benchmarkManager != null)
                    benchmarkManager.EndDepthEstimation();

                depthEstimationTimeText.text = "Depth Estimation Time: " + deltaTime.ToString("F4") + "s\nFPS: " + (1.0f / deltaTime).ToString("F2");
            }
            else
            {
                yield return null;
            }
        }
    }

    private IEnumerator RunInpainting()
    {
        while (true)
        {
            if (inpaintingModel.IsEnabled)
            {
                // Start inpainting timing
                if (benchmarkManager != null)
                    benchmarkManager.StartInpainting();

                float startTime = Time.realtimeSinceStartup;
                yield return inpaintingModel.RunModel(inputTexture, segmentationModel as YOLOSegmentationRunner, depthModel.ObjectDepthBuffer);
                float endTime = Time.realtimeSinceStartup;
                float deltaTime = endTime - startTime;

                // End inpainting timing
                if (benchmarkManager != null)
                    benchmarkManager.EndInpainting();

                inpaintingTimeText.text = "Inpainting Time: " + deltaTime.ToString("F4") + "s\nFPS: " + (1.0f / deltaTime).ToString("F2");
            }
            else
            {
                yield return null;
            }
        }
    }

    private IEnumerator RunPostProcessing()
    {
        while (true)
        {
            PostProcessing();

            // End iteration timing for benchmark (called after post-processing completes)
            if (benchmarkManager != null)
                benchmarkManager.EndIteration();

            yield return null;
        }
    }

#endregion

#region Sequential Execution
    float inferenceTime;

    private IEnumerator RunSequentialPipeline()
    {
        while (true)
        {
            // Start iteration timing for benchmark
            if (benchmarkManager != null)
                benchmarkManager.StartIteration();

            RenderTexture.active = null;  // Reset active render texture        
            inferenceTime = Time.realtimeSinceStartup;

            // Segmentation
            if (benchmarkManager != null)
                benchmarkManager.StartSegmentation();
            yield return StartCoroutine(segmentationModel.RunModel(inputTexture));
            if (benchmarkManager != null)
                benchmarkManager.EndSegmentation();

            // Depth Estimation
            if (depthModel.IsEnabled)
            {
                if (benchmarkManager != null)
                    benchmarkManager.StartDepthEstimation();
                yield return StartCoroutine(depthModel.RunModel(inputTexture));
                depthModel.CalculateObjectDepths(segmentationModel.OutputBuffer, segmentationModel.NumObjDetected);
                if (benchmarkManager != null)
                    benchmarkManager.EndDepthEstimation();
            }

            // Inpainting
            if (inpaintingModel.IsEnabled)
            {
                if (benchmarkManager != null)
                    benchmarkManager.StartInpainting();
                yield return StartCoroutine(inpaintingModel.RunModel(inputTexture, segmentationModel as YOLOSegmentationRunner, depthModel.ObjectDepthBuffer));
                if (benchmarkManager != null)
                    benchmarkManager.EndInpainting();
            }

            // Post Processing
            PostProcessing();
            
            segmentationModel.DisposeOutput();
            var speed = Time.realtimeSinceStartup - inferenceTime;
            segmentationTimeText.text = "Inference Speed: " + speed.ToString("F4") + "s\n" + "FPS: " + (1.0f / speed).ToString("F2");

            // End iteration timing for benchmark
            if (benchmarkManager != null)
                benchmarkManager.EndIteration();
        }
    }
#endregion


    /// <summary>
    /// Execute Post Processing effects
    /// </summary>
    private void PostProcessing()
    {
        // Start post-processing timing
        if (benchmarkManager != null)
            benchmarkManager.StartPostProcessing();

        float startTime = Time.realtimeSinceStartup;
        RenderTexture.active = null;
        RenderTexture.active = postProcessingOverlay;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = imagePostProcessingOverlay;
        GL.Clear(true, true, Color.clear);

        foreach (var processor in gpuPostProcessors)
        {   
            if (processor is ImagePostProcessor) {
                var imageProcessor = processor as ImagePostProcessor;
                imageProcessor.ExecuteShader(inputTexture);
            }
            else {
                processor.ExecuteShader();
            }
        }


        foreach (var processor in cpuPostProcessors)
        {
            processor.Execute();
        }


        OutputImage.texture = inputTexture;
        PostProcessingOutputImage.texture = postProcessingOverlay;
        ImagePostprocessingOutputImage.texture = imagePostProcessingOverlay;

        float endTime = Time.realtimeSinceStartup;
        float deltaTime = endTime - startTime;
        postProcessingTimeText.text = "Post Processing Time: " + deltaTime.ToString("F4") + "s\nFPS: " + (1.0f / deltaTime).ToString("F2");

        // End post-processing timing
        if (benchmarkManager != null)
            benchmarkManager.EndPostProcessing();
    }

}
