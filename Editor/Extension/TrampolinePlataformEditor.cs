using GameEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameEditor
{
    [CustomEditor(typeof(TrampolinePlataform))]
    public class TrampolinePlataformEditor : BaseEditor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10);
            TitleBox("Trampoline Plataform");

            GUILayout.Space(10);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useMass"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bounceStrength"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
