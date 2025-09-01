using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor window for controlling pipeline benchmarking
/// 
/// Author: J-Britten
/// </summary>
public class BenchmarkWindow : EditorWindow
{
    private float benchmarkDuration = 60f;
    private BenchmarkManager benchmarkManager;
    private Vector2 scrollPosition;
    
    [MenuItem("MIRAGE/Benchmark Controller")]
    public static void ShowWindow()
    {
        GetWindow<BenchmarkWindow>("Benchmark Controller");
    }

    void OnEnable()
    {
        // Subscribe to play mode state changes
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        Repaint();
    }

    void OnGUI()
    {
        GUILayout.Label("Pipeline Benchmark Controller", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Benchmarking is only available in Play Mode", MessageType.Info);
            return;
        }

        // Find benchmark manager if not cached
        if (benchmarkManager == null)
        {
            benchmarkManager = BenchmarkManager.Instance;
        }

        if (benchmarkManager == null)
        {
            EditorGUILayout.HelpBox("BenchmarkManager not found in scene", MessageType.Warning);
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Benchmark Settings
        EditorGUILayout.LabelField("Benchmark Settings", EditorStyles.boldLabel);
        benchmarkDuration = EditorGUILayout.FloatField("Duration (seconds):", benchmarkDuration);
        
        if (benchmarkDuration <= 0)
        {
            benchmarkDuration = 1f;
        }

        EditorGUILayout.Space();

        // Control Buttons
        EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);
        
        EditorGUI.BeginDisabledGroup(benchmarkManager.isBenchmarking);
        if (GUILayout.Button("Start Benchmark", GUILayout.Height(30)))
        {
            benchmarkManager.StartBenchmark(benchmarkDuration);
        }
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(!benchmarkManager.isBenchmarking);
        if (GUILayout.Button("Stop Benchmark", GUILayout.Height(30)))
        {
            benchmarkManager.StopBenchmark();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(benchmarkManager.isBenchmarking || benchmarkManager.IterationCount == 0);
        if (GUILayout.Button("Print Current Results"))
        {
            benchmarkManager.PrintBenchmarkResults();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();

        // Status Display
        EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
        
        if (benchmarkManager.isBenchmarking)
        {
            EditorGUILayout.HelpBox("Benchmark is running...", MessageType.Info);
            
            float elapsed = benchmarkManager.BenchmarkDuration;
            float remaining = benchmarkManager.maxBenchmarkDuration - elapsed;
            
            EditorGUILayout.LabelField($"Elapsed Time: {elapsed:F1}s");
            EditorGUILayout.LabelField($"Remaining Time: {remaining:F1}s");
            EditorGUILayout.LabelField($"Iterations Completed: {benchmarkManager.IterationCount}");
            
            // Progress bar
            float progress = elapsed / benchmarkManager.maxBenchmarkDuration;
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), progress, $"{progress * 100:F1}%");
        }
        else
        {
            EditorGUILayout.HelpBox("Benchmark is not running", MessageType.None);
        }

        EditorGUILayout.Space();

        // Instructions
        EditorGUILayout.LabelField("Instructions", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1. Set the desired benchmark duration\n" +
            "2. Click 'Start Benchmark' to begin logging\n" +
            "3. The benchmark will automatically stop after the specified duration\n" +
            "4. Use 'Stop Benchmark' to manually stop before completion\n" +
            "5. Use 'Print Current Results' to see detailed statistics in the console\n" +
            "6. Results include min/max/average/std dev for all timing metrics\n" +
            "7. All benchmark data is available in memory for real-time analysis",
            MessageType.Info);

        EditorGUILayout.EndScrollView();

        // Auto-repaint while benchmarking
        if (benchmarkManager != null && benchmarkManager.isBenchmarking)
        {
            Repaint();
        }
    }

    void Update()
    {
        // Repaint the window while benchmarking to show live updates
        if (benchmarkManager != null && benchmarkManager.isBenchmarking)
        {
            Repaint();
        }
    }
}
