using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

[CustomEditor(typeof(WeaponBehaviour))]
public class WeaponBehaviourEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(10);

        var weapon = (WeaponBehaviour)target;
        var components = weapon.GetComponents<MonoBehaviour>();

        foreach (var comp in components)
        {
            if (comp == weapon) continue; // skip itself

            var type = comp.GetType();
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            bool hasMarkedFields = false;

            foreach (var field in fields)
            {
                if (Attribute.IsDefined(field, typeof(WeaponPropertyAttribute)))
                {
                    hasMarkedFields = true;
                    break;
                }
            }

            if (!hasMarkedFields) continue;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(type.Name, EditorStyles.boldLabel);

            foreach (var field in fields)
            {
                if (!Attribute.IsDefined(field, typeof(WeaponPropertyAttribute)))
                    continue;

                SerializedObject so = new SerializedObject(comp);
                SerializedProperty prop = so.FindProperty(field.Name);

                if (prop != null)
                {
                    so.Update();
                    EditorGUILayout.PropertyField(prop, new GUIContent(ObjectNames.NicifyVariableName(field.Name)));
					so.ApplyModifiedProperties();
                }
            }
        }
    }
}
