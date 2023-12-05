using GameEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameEditor
{
    [CustomEditor(typeof(PlayerControlRotation))]
    public class PlayerControlRotationEditor : BaseEditor
    {
        private PlayerControlRotation m_target;

        private void OnEnable()
        {
            m_target = (PlayerControlRotation)target;
            m_title = "Player Control Rotation";
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("controlMode"));
            if (m_target.controlMode == PlayerControlRotation.ControlMode.TowardsAim)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("applyRollControl"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("applyPitchControl"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("applyYawControl"));
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("smooth"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("lerpCurve"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("updateMethod"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
