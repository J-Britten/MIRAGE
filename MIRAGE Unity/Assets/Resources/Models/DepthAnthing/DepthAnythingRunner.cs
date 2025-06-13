using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.InferenceEngine;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

/// <summary>
/// Depth Anything Depth Estimation
/// using Depth Anything v2: https://github.com/DepthAnything/Depth-Anything-V2
/// 
/// ONNX Models taken from: https://github.com/fabio-sim/Depth-Anything-ONNX
/// 
/// 
/// Author: J-Britten
/// </summary>
public class DepthAnythingRunner : DepthEstimationRunner
{   
#region Variables
    /// <summary>
    /// Default parameters for a Logitech C920 Pro Webcam
    /// </summary>
    public float SensorWidthPX = 1280f; //this should match the OutputWidth 
    public float FocalLengthMM = 3.67f;

    public float SensorWidthMM = 5.7f;

    private float focalLengthPX;
    public override ComputeBuffer ObjectDepthBuffer {get => objectDepthBuffer;}

    private ComputeBuffer objectDepthBuffer;
    public override float[] DepthData => objectDepths;

    private float[] objectDepths;
    
    public RawImage InputImage;
    private Unity.InferenceEngine.TextureTransform toTensor, toTexture;

    private Unity.InferenceEngine.Tensor<float> inputTensor;

    private Unity.InferenceEngine.Tensor<float> outputTensor, textureTensor;

    private ComputeShader objectDepthCompute;
    private int objectDepthKernel;

    private int threadGroupsX, threadGroupsY;

#endregion

#region Model Preparation
    protected override void PrepareModel()
    {
        /*float scaleW = (float) InputWidth / (float) ImageWidth; //Whatever your IDE claims, these casts are necessary
        float scaleH = (float) InputHeight / (float)ImageHeight;
        float scale = Mathf.Max(scaleW, scaleH);
*/

        //Focal length scaling (experimental)
        focalLengthPX = SensorWidthPX* FocalLengthMM / SensorWidthMM;
      
        float focalLengthScale = focalLengthPX  / 1000f;

        toTensor = new TextureTransform().SetDimensions(InputWidth,InputHeight,3);//.SetDimensions(InputWidth, InputHeight, 3);//.SetDimensions(scaledWidth,scaledHeight,3);
        
        //Model needs the input to be divisible by 14, we thus pad the input to the nearest larger multiple of 4
        pad_w = Mathf.CeilToInt(InputWidth / 14.0f) * 14 - InputWidth;
        pad_h = Mathf.CeilToInt(InputHeight / 14.0f) * 14 - InputHeight;
        
        int[] padding = new int[] {0, pad_w, 0, pad_h}; //width left, width right, height top, height bottom (since origin is top left)
        var model = Unity.InferenceEngine.ModelLoader.Load(ModelAsset);

        var graph = new Unity.InferenceEngine.FunctionalGraph();

        var input = graph.AddInput(model, 0); //this is dynamic but set to the scaledWidth and Height using toTensor Texture transform

        input = Unity.InferenceEngine.Functional.Pad(input, padding, paddingValue);

        var outputs = Unity.InferenceEngine.Functional.Forward(model, input);
        var output = outputs[0];
        var slicedOutput = output[.., 0..InputHeight, 0..InputWidth]; //remove the pad
        slicedOutput= Unity.InferenceEngine.Functional.Unsqueeze(slicedOutput, 0);
  
       // slicedOutput = Functional.Interpolate(slicedOutput, new int[] {OutputHeight, OutputWidth}, mode: "nearest"); //this causes issues sometimes
        var depthTexture = slicedOutput / 80f;        
        depthTexture = depthTexture * focalLengthScale;

              
        runtimeModel = graph.Compile(slicedOutput, depthTexture);

        objectDepthBuffer = new ComputeBuffer(MaxObjects, sizeof(float));

        objectDepths = new float[MaxObjects];
        objectDepthBuffer.SetData(objectDepths);

        objectDepthCompute = Resources.Load<ComputeShader>("Models/Depth/ObjectDepthCalculator");
        objectDepthKernel = objectDepthCompute.FindKernel("ObjectDepth");
        objectDepthCompute.SetInt("ImageWidth", OutputWidth);
        objectDepthCompute.SetInt("ImageHeight", OutputHeight);
        objectDepthCompute.SetFloat("FocalLengthScale", focalLengthScale);

                // Calculate physical FOV in radians (for debugging/comparison)
        float fovRadians = 2.0f * Mathf.Atan((SensorWidthMM/2.0f) / FocalLengthMM);
        
        // Pass the focal length in pixels to the compute shader instead of FOV
        objectDepthCompute.SetFloat("FOVRadians", fovRadians);

                // Dispatch the shader
        threadGroupsX = Mathf.CeilToInt(OutputWidth / 8.0f);
        threadGroupsY = Mathf.CeilToInt(OutputHeight / 8.0f);
    }
#endregion
#region Model Execution
    public override IEnumerator RunModel(params Texture[] inputs)
    {
        modelRunning = true;
        if(InputImage != null) InputImage.texture = inputs[0];
        inputTensor = Unity.InferenceEngine.TextureConverter.ToTensor(inputs[0], toTensor);

        schedule = worker.ScheduleIterable(inputTensor);

        yield return RunInference();
    }

    protected override void PeekOutput()
    {
        DisposeOutput();

        outputTensor = worker.PeekOutput(0) as Unity.InferenceEngine.Tensor<float>;
        textureTensor = worker.PeekOutput(1) as Unity.InferenceEngine.Tensor<float>;
        outputTensor.ReadbackRequest();
        textureTensor.ReadbackRequest();
 
        inputTensor.Dispose();
    }

    protected override void ReadOutput()
    {
        
        outputTexture = Unity.InferenceEngine.TextureConverter.ToTexture(textureTensor);
        UpdateOutputImage();
    }

    protected override bool RequestsDone()
    {
        return outputTensor.IsReadbackRequestDone() && textureTensor.IsReadbackRequestDone();
    }


    /// <summary>
    /// Calculate the depths of the objects in the scene
    /// </summary>
    /// <param name="segmentationBuffer"></param>
    /// <param name="numObjects"></param>
    public override void CalculateObjectDepths(ComputeBuffer segmentationBuffer, int numObjects)
    {
        if(outputTensor == null) return;
        objectDepthCompute.SetBuffer(objectDepthKernel, "SegmentationBuffer", segmentationBuffer);
        objectDepthCompute.SetBuffer(objectDepthKernel, "DepthBuffer", Unity.InferenceEngine.ComputeTensorData.Pin(outputTensor).buffer);
        objectDepthCompute.SetBuffer(objectDepthKernel, "ObjectDepthBuffer", objectDepthBuffer);

        objectDepthCompute.SetInt("MaxObjectID", numObjects);
        objectDepthCompute.Dispatch(objectDepthKernel, threadGroupsX, threadGroupsY, 1);
        objectDepthBuffer.GetData(objectDepths);

    }
#endregion
#region Disposing
    public override void ResetOutput()
    {
        if(objectDepthBuffer != null) objectDepthBuffer.Release();
        objectDepthBuffer = new ComputeBuffer(MaxObjects, sizeof(float));

        objectDepths = new float[MaxObjects];
        objectDepthBuffer.SetData(objectDepths);
    }

    public override void DisposeOutput()
    {
        if(outputTensor != null) outputTensor.Dispose();
        if(textureTensor != null) textureTensor.Dispose();
      //  if(outputTexture != null) outputTexture.Release();
    }

    void OnDestroy()
    {
        if (outputTexture != null)
            outputTexture.Release();
        if (worker != null) worker.Dispose();
        DisposeOutput();
        if (objectDepthBuffer != null)
            objectDepthBuffer.Release();
        if(inputTensor != null) inputTensor.Dispose();
        
    }

#endregion    



}
