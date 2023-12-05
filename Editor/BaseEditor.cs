using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameEditor
{
    public class BaseEditor : Editor
    {
        protected string m_title = "Title";

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(10);
            TitleBox(m_title);
            GUILayout.Space(10);

            DrawInspectorExcept(serializedObject, "m_Script");
        }

        protected virtual void TitleBox(string title, float height = 5)
        {
            EditorGUILayout.BeginVertical("SelectionRect");
            GUILayout.Space(height);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(title, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(height);
            EditorGUILayout.EndVertical();
        }

        protected virtual void Separator(Color color)
        {
            EditorGUILayout.BeginHorizontal();
            var rect = EditorGUILayout.GetControlRect();
            rect.height = 2;
            EditorGUI.DrawRect(rect, color);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawInspectorExcept(SerializedObject serializedObject, string fieldToSkip)
        {
            DrawInspectorExcept(serializedObject, new string[1] { fieldToSkip });
        }

        private void DrawInspectorExcept(SerializedObject serializedObject, string[] fieldsToSkip)
        {
            serializedObject.Update();
            SerializedProperty prop = serializedObject.GetIterator();
            if (prop.NextVisible(true))
            {
                do
                {
                    if (fieldsToSkip.Any(prop.name.Contains))
                        continue;

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(prop.name), true);
                }
                while (prop.NextVisible(false));
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
