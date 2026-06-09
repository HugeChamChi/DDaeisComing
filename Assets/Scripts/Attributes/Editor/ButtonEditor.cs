using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using DDaeisComing.Attributes;

namespace DDaeisComing.Attributes.Editor
{
    /// <summary>
    /// Custom editor to draw buttons in the Inspector for methods marked with [Button].
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true)]
    [CanEditMultipleObjects]
    public class ButtonEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var monoBehaviour = target as MonoBehaviour;
            if (monoBehaviour == null) return;

            var type = monoBehaviour.GetType();
            
            // Get all methods including private ones
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.GetCustomAttributes(typeof(ButtonAttribute), true).Length > 0);

            if (methods.Any())
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Buttons", EditorStyles.boldLabel);

                foreach (var method in methods)
                {
                    var buttonAttribute = (ButtonAttribute)method.GetCustomAttributes(typeof(ButtonAttribute), true)[0];
                    string buttonName = string.IsNullOrEmpty(buttonAttribute.ButtonName) ? method.Name : buttonAttribute.ButtonName;

                    if (GUILayout.Button(buttonName))
                    {
                        foreach (var targetObj in targets)
                        {
                            method.Invoke(targetObj, null);
                        }
                    }
                }
            }
        }
    }
}
