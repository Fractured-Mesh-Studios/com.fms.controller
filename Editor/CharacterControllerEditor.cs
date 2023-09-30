using System.Collections;
using System.Collections.Generic;
using UnityEditor;

using UnityEngine;

namespace GameEditor
{
    [CustomEditor(typeof(GameEngine.CharacterController))]
    public class CharacterControllerEditor : Editor
    {
        private GameEngine.CharacterController m_target;

        private bool m_jumpTab;
        private bool m_wallTab;
        private bool m_groundTab;
        private bool m_movementTab;
        private bool m_stepSlopeTab;

        private void OnEnable()
        {
            m_target = (GameEngine.CharacterController)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10);
            TitleBox("Physics Character Controller");

            //Movement
            EditorGUILayout.BeginVertical("GroupBox");
            EditorGUI.indentLevel = 1;
            m_movementTab = EditorGUILayout.Foldout(m_movementTab, "Movement", true);
            if (m_movementTab)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("mode"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("speed"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("speedFactor"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("threshold"));
                switch (m_target.mode)
                {
                    case GameEngine.CharacterController.MovementMode.Force:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("acceleration"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxAccelerationFactor"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("accelerationFactorFromDot"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxAccelerationForce"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxAccelerationForceFactorFromDot"));
                        break;
                    case GameEngine.CharacterController.MovementMode.Velocity:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("dampSpeedUp"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("dampSpeedDown"));
                        break;
                    default: break;
                }
            }
            EditorGUI.indentLevel = 0;
            EditorGUILayout.EndVertical();

            //Ground
            EditorGUILayout.BeginVertical("GroupBox");
            EditorGUI.indentLevel = 1;
            m_groundTab = EditorGUILayout.Foldout(m_groundTab, "Ground", true);
            if (m_groundTab)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_groundMask"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("groundRadius"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("groundDistance"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("groundDirection"));
            }
            EditorGUI.indentLevel = 0;
            EditorGUILayout.EndVertical();

            //Step-Slope
            EditorGUILayout.BeginVertical("GroupBox");
            EditorGUI.indentLevel = 1;
            m_stepSlopeTab = EditorGUILayout.Foldout(m_stepSlopeTab, "Step & Slope", true);
            if (m_stepSlopeTab)
            {
                GUILayout.Space(5);
                GUILayout.Label("Slope", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_slopeMask"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("slopeLock"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("slopeAngle"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("slopeGravityMultiplier"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("slopeDistance"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("slopeRadius"));

                GUILayout.Space(5);
                GUILayout.Label("Step", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stepOffset"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stepHeight"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stepDistance"));
            }
            EditorGUI.indentLevel = 0;
            EditorGUILayout.EndVertical();

            //Jump
            EditorGUILayout.BeginVertical("GroupBox");
            EditorGUI.indentLevel = 1;
            m_jumpTab = EditorGUILayout.Foldout(m_jumpTab, "Jump", true);
            if (m_jumpTab)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("jumpForce"));
            }
            EditorGUI.indentLevel = 0;
            EditorGUILayout.EndVertical();

            //Wall
            EditorGUILayout.BeginVertical("GroupBox");
            EditorGUI.indentLevel = 1;
            m_wallTab = EditorGUILayout.Foldout(m_wallTab, "Wall", true);
            if (m_wallTab)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("wallHeight"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("wallDistance"));
            }
            EditorGUI.indentLevel = 0;
            EditorGUILayout.EndVertical();


            serializedObject.ApplyModifiedProperties();
        }


        private void TitleBox(string title)
        {
            EditorGUILayout.BeginVertical("SelectionRect");
            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(title);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }
    }

}
