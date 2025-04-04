using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InpaintingToggle : MonoBehaviour
{
    InpaintingRunner inpaintingRunner;

    void Start()
    {
        inpaintingRunner = FindObjectOfType<InpaintingRunner>();
    }
    
    public void OnToggle(bool value)
    {
        if (value)
        {
            inpaintingRunner.IsEnabled = true;
        }
        else
        {
            inpaintingRunner.IsEnabled = false;
        }

    }
}
