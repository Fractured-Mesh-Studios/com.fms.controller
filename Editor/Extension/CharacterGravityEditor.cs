using GameEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameEditor
{
    [CustomEditor(typeof(CharacterGravity))]
    public class CharacterGravityEditor : BaseEditor
    {
        private CharacterGravity m_target;
        private Rigidbody m_rigidbody;

        private void OnEnable()
        {
            m_target = (CharacterGravity)target;
            m_rigidbody = m_target.GetComponent<Rigidbody>();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            GUILayout.Space(10f);

            TitleBox("Character Gravity");

            GUILayout.Space(10f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("target"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetVector"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("force"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("scale"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("mass"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("forceMode"));
            GUILayout.Space(5f);

            if (GUILayout.Button("Setup"))
            {
                m_rigidbody.useGravity = false;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
