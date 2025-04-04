using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OpacitySlider : MonoBehaviour
{
    public EffectPanel effectPanel;

    public TMP_Text sliderText;

    void Start()
    {
        OnSliderChange(GetComponent<Slider>().value);
    }
    public void OnSliderChange(float value) {
        effectPanel.color.a = value;

        sliderText.text = value.ToString("0.00");
    }
}
