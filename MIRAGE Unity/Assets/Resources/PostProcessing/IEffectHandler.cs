using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.EventSystems;


#region Interfaces
/// <summary>
/// Interface for classes that handle effects.
/// </summary>
public interface IEffectHandler
{
    public EffectType EffectType { get;}

    /// <summary>
    /// Update the effect handler based on new classes
    /// </summary>
    /// <param name="classes"></param>
    void UpdateClasses(List<EffectSetting> classes);

    void UpdateClasses();
    
}

/// <summary>
/// Interface for effect settings.
/// Different effects may requrire additional settings, but for the <see cref="PipelineUIHandler"/> to handle them, they implement this interface
/// </summary>
public class EffectSetting {
    public int ClassID;// { get; set; }
    public float MinRange = 0;// { get; set; }
    public float MaxRange = 100;// { get; set; }
}

#endregion
#region Setting Structs
/// <summary>
/// Settings for Inpainted
/// </summary>
[System.Serializable]
public class InpaintingSetting : EffectSetting  {
   // public int ClassID;// { get; set; }
   // public float MinRange;// { get; set; }
   // public float MaxRange;// { get; set; }

    public InpaintingSetting(int classID, float minRange, float maxRange) {
        ClassID = classID;
        MinRange = minRange;
        MaxRange = maxRange;
    }

    public InpaintingSettingStruct ToStruct() {
        return new InpaintingSettingStruct(ClassID, MinRange, MaxRange);
    }
}

public struct InpaintingSettingStruct {
    public int ClassID;
    public float MinRange;
    public float MaxRange;

    public InpaintingSettingStruct(int classID, float minRange, float maxRange) {
        ClassID = classID;
        MinRange = minRange;
        MaxRange = maxRange;
    }
}


/// <summary>
/// Class that holds postprocessor settings.
/// 
/// Important: Unity decided that when adding a new item to a list in the editor UI, the color by default is black and transparent (0,0,0,0).
/// Keep this in mind when setting colors!
/// </summary>
[System.Serializable]
public class PostProcessorSetting : EffectSetting
{
/*    public int ClassID { get; set; }
    public float MinRange { get; set; }
    public float MaxRange { get; set; }
*/
    [JsonConverter(typeof(ColorJsonConverter))]
    public Color color = Color.white;// { get; set; }

    public PostProcessorSetting(int id, Color color, float min = 0, float max = 0)
    {
        ClassID = id;
        this.color = color;
        MinRange = min;
        MaxRange = max;
    }

    public PostProcessorSettingStruct ToStruct()
    {
        return new PostProcessorSettingStruct(ClassID, color, MinRange, MaxRange);
    }
}


public struct PostProcessorSettingStruct {
    public int ClassID;
    public float MinRange;
    public float MaxRange;
    [JsonConverter(typeof(ColorJsonConverter))]
    public Color color;

    public PostProcessorSettingStruct(int id, Color color, float min = 0, float max = 0)
    {
        ClassID = id;
        this.color = color;
        MinRange = min;
        MaxRange = max;
    }
}

#endregion
#region Utility

/// <summary>
/// Enum for different effect types.
/// </summary>
public enum EffectType {
    ColorArea,
    Outline,
    Opacity,
    Inpainting,
    Icon,
    BBox,
    Info,
    GaussianBlur,
    Kuwahara,
    Replace,
    Transform,
    Utility
}

/// <summary>
/// Newtonsoft.Json converter for Unity Color
/// </summary>
public class ColorJsonConverter : JsonConverter<Color>
{
    public override Color ReadJson(JsonReader reader, System.Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var obj = JObject.Load(reader);
        return new Color(
            obj["r"].Value<float>(),
            obj["g"].Value<float>(),
            obj["b"].Value<float>(),
            obj["a"].Value<float>()
        );
    }

    public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("r");
        writer.WriteValue(value.r);
        writer.WritePropertyName("g");
        writer.WriteValue(value.g);
        writer.WritePropertyName("b");
        writer.WriteValue(value.b);
        writer.WritePropertyName("a");
        writer.WriteValue(value.a);
        writer.WriteEndObject();
    }
}

#endregion


