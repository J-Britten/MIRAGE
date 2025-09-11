using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// Simple Study Manager, holds partiicpant Id is responsible for logging
/// </summary>
public class StudyManager : MonoBehaviour
{
    public static StudyManager Instance;

    public int ParticipantID;
    public bool EnableLogging = true;

    private List<ModelRunner> modelRunners = new List<ModelRunner>();

    void Start()
    {
        Instance = this;

        modelRunners = FindObjectsOfType<ModelRunner>().ToList();
    }


    public void LogSettings(Dictionary<EffectType, List<EffectSetting>> effectSettings, List<Toggle> LayerSettings) {
        if(!EnableLogging) return;
        
        // Create combined JSON object
        var combinedSettings = new JObject();
            
        // Serialize effect settings
        var effectSettingsJson = JObject.Parse(JsonConvert.SerializeObject(effectSettings, Formatting.Indented));
        combinedSettings["effects"] = effectSettingsJson;

        // Create layers object
        var layersObject = new JObject();
        foreach (var layer in LayerSettings) {
            layersObject[layer.gameObject.name] = layer.isOn;
        }
        combinedSettings["layers"] = layersObject;

        // Create model runners object
        var runnersObject = new JObject();
        foreach (var runner in modelRunners) {
            var runnerSettings = new JObject();
            runnerSettings["isEnabled"] = runner.IsEnabled;
            runnerSettings["updateRate"] = runner.UpdateRate;
            runnersObject[runner.gameObject.name] = runnerSettings;
        }
        combinedSettings["modelRunners"] = runnersObject;

        // Save to file
        string jsonData = combinedSettings.ToString(Formatting.Indented);

        // Create directory if it doesn't exist
        string directoryPath = Path.Combine(Application.dataPath, "..", "UserLogs");
        Directory.CreateDirectory(directoryPath);

        // Generate filename with participant ID and timestamp
        long unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string fileName = $"{ParticipantID}_{unixTimestamp}.json";
        string filePath = Path.Combine(directoryPath, fileName);

        File.WriteAllText(filePath, jsonData);
        Debug.Log($"Settings saved to: {filePath}");
    }

}
