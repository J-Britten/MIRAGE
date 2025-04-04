using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static UnityEngine.UI.Toggle;

public class UIToggleHandler : MonoBehaviour
{

    public bool value = true;
    [SerializeField]
    public ToggleEvent OnToggle = new ToggleEvent();
    // Start is called before the first frame update
    void Update() {
        
        if (Input.GetButtonDown("Cancel")) {
            value = !value;
            OnToggle.Invoke(value);

        }
    }


}
