using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(MonoBehaviour), true)]
public class IEffectHandlerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        // Check if the target object implements IEffectHandler
        if (target is IEffectHandler effectHandler)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("IEffectHandler Controls", EditorStyles.boldLabel);

            if (GUILayout.Button("Update Classes"))
            {
                // Example: Call UpdateClasses with a dummy list of EffectSetting
                effectHandler.UpdateClasses();

                Debug.Log("UpdateClasses called on " + target.name);
            }
        }
    }
}
