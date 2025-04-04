using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Sentis;
using UnityEngine;

public class ModelInferenceSpeedSlider : MonoBehaviour
{
    public ModelRunner ModelRunner;
    public TMP_Text SliderText;

    void Awake() {
        OnSliderChange(1.0f);
    }
    public void OnSliderChange(float value)
    {
        if(ModelRunner == null) return;
        ModelRunner.SetUpdateRate(value);
        SliderText.text = value.ToString("0.00");
    }
}
