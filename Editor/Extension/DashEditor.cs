using UnityEditor;
using GameEngine;
using UnityEngine;

namespace GameEditor
{
    [CustomEditor(typeof(Dash))]
    public class DashEditor : BaseEditor
    {
        private Dash m_target;
        private Stamina m_stamina;

        private void OnEnable()
        {
            m_title = "Dash";
            m_target = target as Dash;
            m_stamina = m_target.GetComponent<Stamina>();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("directional"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("massFactor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useGrounded"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("impulse"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("coldown"));
            if (m_stamina)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("drain"));
            }

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onDash"));

            EditorGUILayout.HelpBox("OnDash() requiered", MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
