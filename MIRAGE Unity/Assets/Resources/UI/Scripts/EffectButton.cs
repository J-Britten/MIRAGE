using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EffectButton : MonoBehaviour
{

    public TMP_Text effectName;

    public Image effectIcon;

    public EffectType effectType;

    void Start()
    {
        effectName.text = name;
        EffectSelectorPanel.Instance.RegisterEffectButton(this);
    }


}
