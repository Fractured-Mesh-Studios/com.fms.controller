using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameEditor
{
    public class BaseEditor : Editor
    {
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
    }
}
