using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ClassPanel : MonoBehaviour
{

    public ClassSetting ClassSetting;
    public TMP_Text classNameText;

    public Transform container;

    public Transform popupPosition;

    public List<EffectPanel> effectPanels;

    public void Initialize(ClassSetting setting) {
        ClassSetting = setting;
        classNameText.text = setting.ClassName;
    }
    public void AddEffectPanel(EffectPanel effectPanel) {
        effectPanels.Add(effectPanel);
        effectPanel.Initialize(this);
    }

    public void RemoveEffectPanel(EffectPanel effectPanel) {
        effectPanels.Remove(effectPanel);
    }


    public void OnAddEffectButtonPress() {
        EffectSelectorPanel.Instance.SwitchClassPanel(this);
    }
}
