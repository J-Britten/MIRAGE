using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Class that handles the input for the pipeline
/// 
/// It can either use a webcam or a video player as input.
/// 
/// Author: J-Britten
/// </summary>
public class CameraInput : MonoBehaviour
{
    /// <summary>
    /// Singleton
    /// </summary>
    public static CameraInput Instance;

    /** Camera Settings **/
    public int RequestedWidth = 1280;
    public int RequestedHeight = 720;
    public int FPS = 30;
    public bool startOnAwake = true;

    [SerializeField]
    private string selectedCameraDeviceName;
    private WebCamTexture webcamTexture;

    /** Debug Video Settings **/
    public bool UseDebugVideoInput;
    public VideoPlayer VideoPlayer;


    public RawImage VideoPanel; 
    public RenderTexture CurrentFrame;


    // Start is called before the first frame update
    void Awake()
    {
       
        Instance = this;

        if(UseDebugVideoInput) {StartVideo();}
        else if(startOnAwake) {StartCamera();}
    }


    public void StartVideo() {
       
        CurrentFrame = new CustomRenderTexture(RequestedWidth,RequestedHeight,RenderTextureFormat.ARGB32);

        VideoPlayer.targetTexture = CurrentFrame;
        VideoPanel.texture = CurrentFrame;
        VideoPlayer.Play();
    }
    public void StartCamera()
    {
        if(webcamTexture != null) return;
        // Use selected camera device if specified, otherwise use default resolution
        webcamTexture = string.IsNullOrEmpty(selectedCameraDeviceName) 
            ? new WebCamTexture(RequestedWidth, RequestedHeight, FPS)
            : new WebCamTexture(selectedCameraDeviceName, RequestedWidth, RequestedHeight, FPS);

        webcamTexture.Play();
        CurrentFrame = new RenderTexture(webcamTexture.width, webcamTexture.height, 0);
        CurrentFrame.Create();
        // Set the webcam texture as the texture for the RawImage
        if (VideoPanel != null)
        {
            VideoPanel.texture = webcamTexture;
        }
        else
        {
            Debug.LogWarning("No RawImage assigned to display the camera feed.");
        }

        
    }

    public void StopCamera()
    {
        if(webcamTexture == null) return;
        webcamTexture.Stop();

        if (CurrentFrame != null)
        {
            CurrentFrame.Release();
            Destroy(CurrentFrame);
        }

        // Clear the RawImage texture when stopping the camera
        if (VideoPanel != null)
        {
            VideoPanel.texture = null;
            VideoPanel.material.mainTexture = null;
        }
    }
    // Update is called once per frame
    void Update()
    {

        if (webcamTexture != null && webcamTexture.isPlaying)
        {
         
            Graphics.Blit(webcamTexture, CurrentFrame);
        }
    }

}
