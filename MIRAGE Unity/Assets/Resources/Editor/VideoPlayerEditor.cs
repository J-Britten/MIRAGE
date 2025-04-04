using UnityEngine;
using UnityEngine.Video;
using UnityEditor;

[CustomEditor(typeof(VideoPlayer))]
public class VideoPlayerEditor : Editor
{
    private VideoPlayer videoPlayer;
    private bool isPlaying = false;
    private float currentTime = 0f;

    private void OnEnable()
    {
        videoPlayer = (VideoPlayer)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        DrawVideoControls();
        DrawTimeline();
        DrawTimeInfo();

        // Auto-repaint the inspector to update time
        if (videoPlayer.isPlaying)
            Repaint();
    }

    private void DrawVideoControls()
    {
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button(videoPlayer.isPlaying ? "Pause" : "Play", GUILayout.Width(60)))
        {
            if (videoPlayer.isPlaying)
                videoPlayer.Pause();
            else
                videoPlayer.Play();
        }

        if (GUILayout.Button("Stop", GUILayout.Width(60)))
        {
            videoPlayer.Stop();
            videoPlayer.time = 0;
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawTimeline()
    {
        if (videoPlayer.length > 0)
        {
            EditorGUI.BeginChangeCheck();
            float newTime = EditorGUILayout.Slider((float)videoPlayer.time, 0f, (float)videoPlayer.length);
            
            if (EditorGUI.EndChangeCheck())
            {
                videoPlayer.time = newTime;
            }
        }
    }

    private void DrawTimeInfo()
    {
        string currentTimeStr = FormatTime(videoPlayer.time);
        string totalTimeStr = FormatTime(videoPlayer.length);
        
        EditorGUILayout.LabelField($"Time: {currentTimeStr} / {totalTimeStr}");
    }

    private string FormatTime(double timeInSeconds)
    {
        int minutes = (int)(timeInSeconds / 60);
        int seconds = (int)(timeInSeconds % 60);
        return $"{minutes:00}:{seconds:00}";
    }
}
