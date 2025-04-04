using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GaussianBlurSlider : MonoBehaviour
{
    public EffectPanel effectPanel;

    public Slider RadiusSlider;
    public Slider SigmaSlider;

    public TMP_Text RadiusText;
    public TMP_Text SigmaText;

    void Start()
    {
        OnRadiusSliderChange(RadiusSlider.value);
        OnSigmaSliderchange(SigmaSlider.value);
    }

    public void OnRadiusSliderChange(float value) {
    
       effectPanel.color.r = value/255f;
        RadiusText.text = value.ToString();
    }

    public void OnSigmaSliderchange(float value) {
        effectPanel.color.g = value/255f;
        SigmaText.text = value.ToString();
    }
}
