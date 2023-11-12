using GameEngine;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using UnityEditor;
using UnityEditorInternal;

using UnityEngine;

namespace GameEditor
{
    [CustomEditor(typeof(MovingPlataform))]
    public class MovingPlataformEditor : BaseEditor
    {
        private MovingPlataform m_target;
        private ReorderableList m_list;

        private void OnEnable()
        {
            m_target = (MovingPlataform)target;
            m_list = new ReorderableList(
                serializedObject,
                serializedObject.FindProperty("waypoints"),
                true, 
                true, 
                true, 
                true
            );

            m_list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                if (m_list.serializedProperty.isExpanded) {
                    var element = m_list.serializedProperty.GetArrayElementAtIndex(index);
                    var offset = new Rect(rect.x+10, rect.y+2, rect.width-10, rect.height);
                    element.vector3Value = EditorGUI.Vector3Field(offset, string.Empty, element.vector3Value);
                }
            };

            m_list.drawHeaderCallback = (Rect rect) =>
            {
                var label = " Waypoints ["+m_target.waypoints.Length+"]";
                var offset = new Rect(rect.x + 10, rect.y, rect.width - 10, rect.height);
                m_list.serializedProperty.isExpanded = EditorGUI.Foldout(offset, m_list.serializedProperty.isExpanded, label, true);
                //EditorGUI.LabelField(rect, new GUIContent("Waypoints", ""));
            };

            m_list.elementHeightCallback = (int index) =>
            {
                return m_list.serializedProperty.isExpanded ? 20f : 0f;
            };
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10);
            TitleBox("Plataform Kinematic Controller");

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Plataform Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("mode"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("timeCycle"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("timeDelay"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("speedDamp"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("distanceThreshold"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rotationSpeed"));
            EditorGUI.indentLevel = 0;

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Plataform Transform Controls", EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("canRotate"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("canTranslate"));
            EditorGUI.indentLevel = 0;

            GUILayout.Space(10);
            m_list.DoLayoutList();

            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Begin"))
            {
                m_target.Begin();
            }
            if (GUILayout.Button("End"))
            {
                m_target.End();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Prev"))
            {
                m_target.Previous();
            }
            if (GUILayout.Button("Next"))
            {
                m_target.Next();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();


            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            if(m_target.waypoints != null)
            {
                for(int i = 0; i < m_target.waypoints.Length; i++)
                {
                    m_target.waypoints[i] = Handles.PositionHandle(m_target.waypoints[i], Quaternion.identity);
                }
            }
        }

    }
}
