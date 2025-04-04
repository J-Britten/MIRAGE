using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ToggleClass : MonoBehaviour
{
    public ClassSetting ClassSetting;
    public TMP_Text text;
    public void OnToggle(bool value) {
        PipelineUIHandler.Instance.ToggleClass(value, ClassSetting);
    }
}
