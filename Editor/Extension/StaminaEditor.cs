using GameEngine;
using UnityEditor;
using UnityEngine;

namespace GameEditor
{
    [CustomEditor(typeof(Stamina))]
    public class StaminaEditor : BaseEditor
    {
        private Stamina m_target;

        private void OnEnable()
        {
            m_target = (Stamina)target;
            m_title = "Stamina";
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            float stamina = m_target.stamina / m_target.maxStamina;

            float value = Mathf.Abs(m_target.gain - m_target.loss);

            int percent = Mathf.RoundToInt(m_target.GetStaminaFillAmount());

            EditorGUILayout.PropertyField(serializedObject.FindProperty("alwaysGain"));
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), stamina,"Stamina ["+value+"] - "+percent+"%");
            m_target.stamina = EditorGUILayout.Slider(m_target.stamina, m_target.minStamina, m_target.maxStamina);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("minStamina"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxStamina"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("staminaGainRate"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("staminaLossRate"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("staminaThreshold"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("staminaGainCurve"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("staminaLossCurve"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cooldown"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cooldownFactor"));
            //Events
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onStaminaChange"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onStaminaInsufficient"));

            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal("Box");
            if (GUILayout.Button("Use Random"))
            {
                m_target.Use(Random.Range(m_target.minStamina + 10, m_target.maxStamina / 2));
            }
            if (GUILayout.Button("Use Random 2 Seconds Timer"))
            {
                m_target.Use(Random.Range(m_target.minStamina + 10, m_target.maxStamina / 2), 2f);
            }
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
