using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Abstract class for Depth Estimation Models
/// 
/// Author: J-Britten
/// </summary>
public abstract class DepthEstimationRunner : ModelRunner
{

    public abstract ComputeBuffer ObjectDepthBuffer { get;}
    public abstract float[] DepthData {get;}
    
    public abstract void CalculateObjectDepths(ComputeBuffer segmentationBuffer, int numObjects);


    public abstract void ResetOutput();
}
