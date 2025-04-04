using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework.Constraints;
using Unity.Sentis;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// MI-GAN is an Inpainting models that uses a GAN to inpaint images
/// https://github.com/Picsart-AI-Research/MI-GAN/
/// 
/// To export the model to ONNX, we modified the original script. See the repository for detailed instructions.
/// 
/// 
/// Setup: 
/// The IMAGE Width & Height must match the Output Dimensions of the Segmentation Model.
/// 
/// The INPUT Width & Height need to match the model input dimensions. For MI-GAN, this is 512x512.
/// 
/// 
/// Author: J-Britten
/// </summary>
public class MIGANRunner : InpaintingRunner, IEffectHandler
{   

#region Variables


    public EffectType EffectType => EffectType.Inpainting;

    /// <summary>
    /// Classes for Inpainting
    /// </summary>
    public InpaintingSetting[] SelectedClasses;


    private InpaintingSettingStruct[] selectedClassesStructs = new InpaintingSettingStruct[] { };

    public RawImage MaskImage;
    public RawImage DebugOutputImage;


    private TextureTransform imgTransform, maskTransform;

    private Tensor<float> inputTensor;
    private Tensor<float> inputMaskTensor;
    private Tensor<float> outputTensor;

    /// <summary>
    /// Shader that validates the objects detected by YOLO
    /// </summary>
    private ComputeShader objectValidator;
    /// <summary>
    /// Shader that creates the mask based on the yolo output and verified objects
    /// </summary>
    private ComputeShader createMaskShader;
    private int objectValidatorKernel;
    private int createMaskKernel;
    private ComputeBuffer validObjectsBuffer, settingsBuffer;

    private int createMaskThreadGroups;

    private RenderTexture maskOutputTexture;

#endregion
#region Model Preparation
    protected override void PrepareModel()
    {
        imgTransform = new TextureTransform().SetDimensions(InputWidth,InputHeight,3);
        maskTransform = new TextureTransform().SetDimensions(InputWidth,InputHeight,1);

        PreparePreProcessing();

        var model = ModelLoader.Load(ModelAsset);

        var graph = new FunctionalGraph();

        var inputImg = graph.AddInput(DataType.Float, new DynamicTensorShape(1, 3, -1, -1));
        var inputMsk = graph.AddInput(DataType.Float, new DynamicTensorShape(1, 1, -1, -1));
        

        inputMsk = Functional.Interpolate(inputMsk, new int[] {InputHeight, InputWidth}, mode: "nearest");
        //var sliced = inputMsk[..,0,..,..];
        var ceil = Functional.Ceil(inputMsk);
        
        ceil = 1 - ceil;
       // ceil = Functional.MaxPool2D(ceil, 5, 1, 2);

        var outputs = Functional.Forward(model, inputImg, ceil);
        var op = outputs[0];
        
        op = op * inputMsk;

        var alpha = Functional.Concat(new[] {op, inputMsk}, 1);
        runtimeModel = graph.Compile(alpha, ceil);
    }

    private void PreparePreProcessing() {

        objectValidator = Resources.Load<ComputeShader>("Models/MI-GAN/ObjectValidator");
        createMaskShader = Resources.Load<ComputeShader>("Models/MI-GAN/CreateMask");
        objectValidatorKernel = objectValidator.FindKernel("ValidateObjects");
        validObjectsBuffer = new ComputeBuffer(256, sizeof(uint));
        objectValidator.SetBuffer(objectValidatorKernel, "ValidObjects", validObjectsBuffer);
        UpdateClasses();

        inputMaskTensor = new Tensor<float>(new TensorShape(1, 1, ImageHeight, ImageWidth));

        createMaskKernel = createMaskShader.FindKernel("CreateMask");
        
        createMaskThreadGroups = Mathf.CeilToInt((ImageWidth * ImageHeight) / 64f);
        createMaskShader.SetBuffer(createMaskKernel, "ValidObjects", validObjectsBuffer);
        createMaskShader.SetBuffer(createMaskKernel, "MaskBuffer", ComputeTensorData.Pin(inputMaskTensor).buffer);
        createMaskShader.SetInt("NumMaskPositions", ImageWidth * ImageHeight); //ensures we dont go out of bounds
    }

    public void UpdateClasses() {
        if(SelectedClasses.Length == 0) { //ensure we dont crash
            selectedClassesStructs = new InpaintingSettingStruct[] {new InpaintingSetting(-2, 0, 100).ToStruct()};
        } else {
            selectedClassesStructs = SelectedClasses.Select(x => x.ToStruct()).ToArray();
        }

        if(settingsBuffer != null) settingsBuffer.Release();


        
        settingsBuffer = new ComputeBuffer(selectedClassesStructs.Length, sizeof(int) + sizeof(float) * 2);
        settingsBuffer.SetData(selectedClassesStructs);
        objectValidator.SetBuffer(objectValidatorKernel, "SelectedClasses", settingsBuffer);
        objectValidator.SetInt("NumSelectedClasses", selectedClassesStructs.Length);
    }
    public void UpdateClasses(List<EffectSetting> classes)
    {
       SelectedClasses = classes.Cast<InpaintingSetting>().ToArray();

       UpdateClasses();
    }

#endregion
#region Model Execution
    public override IEnumerator RunModel(Texture input, ComputeBuffer mask, ComputeBuffer depthBuffer)
    {
        throw new NotImplementedException();
    }

    public override IEnumerator RunModel(Texture input, YOLOSegmentationRunner yolo, ComputeBuffer depthBuffer)
    {
        if(yolo.NumObjDetected == 0) yield break;
        // Run object validator first
        objectValidator.SetBuffer(objectValidatorKernel, "ClassIds", yolo.ClassIdsBuffer);
        objectValidator.SetBuffer(objectValidatorKernel, "DepthBuffer", depthBuffer);
        objectValidator.SetInt("NumObjects", yolo.NumObjDetected);
        
        int threadGroups = Mathf.CeilToInt(yolo.NumObjDetected / 64f);
        objectValidator.Dispatch(objectValidatorKernel, threadGroups, 1, 1);

        // Now run the CreateMask shader
        createMaskShader.SetBuffer(createMaskKernel, "YOLOMaskBuffer", yolo.OutputBuffer);
        createMaskShader.Dispatch(createMaskKernel, createMaskThreadGroups, 1, 1);

        inputTensor = TextureConverter.ToTensor(input, imgTransform);
        schedule = worker.ScheduleIterable(inputTensor, inputMaskTensor);
        // Run the inpainting model
        yield return RunInference();
    }


    /// <summary>
    /// Basic run model without any additional filtering
    /// </summary>
    /// <param name="inputs">Input Texture, Mask Texture</param>
    /// <returns></returns>
    public override IEnumerator RunModel(params Texture[] inputs)
    {
       // if( inputMaskTensor != null) inputMaskTensor.Dispose();
       
        inputTensor = TextureConverter.ToTensor(inputs[0], imgTransform);
        inputMaskTensor = TextureConverter.ToTensor(inputs[1], maskTransform);


        schedule = worker.ScheduleIterable(inputTensor, inputMaskTensor);

        yield return RunInference();
    }

    protected override bool RequestsDone()
    {
        return outputTensor.IsReadbackRequestDone();
    }

   protected override void PeekOutput()
    {
        outputTensor = worker.PeekOutput(0) as Tensor<float>;
        outputTensor.ReadbackRequest();
    }

    protected override void ReadOutput()
    {
        if (outputTexture != null) {
            outputTexture.Release();
        }
        if( maskOutputTexture != null) maskOutputTexture.Release();


        outputTexture = TextureConverter.ToTexture(outputTensor);
        if(MaskImage != null) {
            maskOutputTexture = TextureConverter.ToTexture(inputMaskTensor);
            MaskImage.texture = maskOutputTexture;
        }

       if(DebugOutputImage != null) {
           DebugOutputImage.texture = outputTexture;
        }
        UpdateOutputImage();
        DisposeOutput();
    }

#endregion
    public override void DisposeOutput()
    {
        if( outputTensor != null) outputTensor.Dispose();
        if( inputTensor != null) inputTensor.Dispose(); 
    }
    void OnDestroy()
    {
        DisposeOutput();
        if(worker != null) worker.Dispose();
        if ( outputTexture != null) outputTexture.Release();

        if( validObjectsBuffer != null) validObjectsBuffer.Release();
        if( settingsBuffer != null) settingsBuffer.Release();
    }    
}
