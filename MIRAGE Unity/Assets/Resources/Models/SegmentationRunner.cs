using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Sentis;


/// <summary>
/// Abstract class for Segmentation Models, e.g. YOLO
/// </summary>
public abstract class SegmentationRunner : ModelRunner
{

    /// <summary>
    /// TextAsset containing the class names
    /// </summary>
    public TextAsset TextAsset;

    public abstract int NumObjDetected {get;}

    /// <summary>
    /// The output buffer for Segmentation Models in this pipeline
    /// is in the shape of <int2>
    /// 
    /// Each int2 contains the mask index and Class ID
    /// The Mask index describes an individual object, the Class ID is used to identify the class of the object
    /// 
    /// E.g.
    /// Mask: 0, Class: 1 -> Object 0 of Class 1
    /// Mask: 1, Class: 2 -> Object 1 of Class 2
    /// Mask: 2, Class 1 -> Object 2 of Class 1
    /// 
    /// This allows us to apply different post-processing effects to individual objects of the same class
    /// </summary>
    public ComputeBuffer OutputBuffer;

}