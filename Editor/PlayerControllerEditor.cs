using GameEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameEditor
{
    [CustomEditor(typeof(PlayerController))]
    public class PlayerControllerEditor : Editor
    {
        private PlayerController m_target;
        private GUIContent m_content;

        private void OnEnable()
        {
            m_target = (PlayerController)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10);
            GUILayout.Label("Input Control Rotation", EditorStyles.boldLabel);
            m_content = new GUIContent("Target", "Transform for the output control rotation generated by the input event system. \n(The control object will be followed by a camera.)");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("target"), m_content);
            m_content = new GUIContent("Input Roll Scale", "Input roll control rotation scale");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("inputRollScale"), m_content);
            m_content = new GUIContent("Input Pitch Scale", "Input pitch control rotation scale");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("inputPitchScale"), m_content);
            m_content = new GUIContent("Input Yaw Scale", "Input yaw control rotation scale");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("inputYawScale"), m_content);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("inputRotationRate"));

            GUILayout.Space(5);
            EditorGUILayout.LabelField("Jump", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("jumpHold"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("jumpHoldSmooth"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("jumpHoldMin"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("jumpHoldMax"));

            GUILayout.Space(5);
            EditorGUI.indentLevel = 1;
            LimitControl(serializedObject.FindProperty("rollControlLimit"));
            LimitControl(serializedObject.FindProperty("pitchControlLimit"));
            LimitControl(serializedObject.FindProperty("yawControlLimit"));
            EditorGUI.indentLevel = 0;

            GUILayout.Space(10);
            GUILayout.Label("Cursor", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cursorVisible"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cursorLockMode"));

            GUILayout.Space(10);
            EditorGUILayout.HelpBox("Requiered Input Action:\n OnMovement,OnJump,OnLook", MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }

        private void LimitControl(SerializedProperty prop)
        {
            EditorGUILayout.BeginVertical("Box");
            prop.isExpanded = EditorGUILayout.Foldout(prop.isExpanded, prop.displayName, true);
            if(prop.isExpanded ) {
                EditorGUILayout.PropertyField(prop.FindPropertyRelative("min"));
                EditorGUILayout.PropertyField(prop.FindPropertyRelative("max"));
            }
            EditorGUILayout.EndVertical();
        }
    }
}
