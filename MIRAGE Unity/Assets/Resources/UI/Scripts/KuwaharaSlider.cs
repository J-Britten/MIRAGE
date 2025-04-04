using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KuwaharaSlider : MonoBehaviour
{
    public EffectPanel effectPanel;

    public Slider RadiusSlider;
    public Slider SectorSlider;

    public Slider StrengthSlider;
    public TMP_Text RadiusText;
    public TMP_Text SectorText;
    public TMP_Text StrengthText;

    void Start()
    {
        OnRadiusSliderChange(RadiusSlider.value);
        OnSectorSliderChange(SectorSlider.value);
        OnStrengthSliderChange(StrengthSlider.value);
    }

    public void OnRadiusSliderChange(float value) {
    
       effectPanel.color.r = value/255f;
        RadiusText.text = value.ToString();
    }

    public void OnSectorSliderChange(float value) {
        effectPanel.color.g = value/255f;
        SectorText.text = value.ToString();
    }

    public void OnStrengthSliderChange(float value) {
        effectPanel.color.b = value/255f;
        StrengthText.text = value.ToString("0.00");
    }
}
