using System.Collections;
using System.Collections.Generic;
using UnityEditor;

using UnityEngine;

namespace GameEditor
{
    [CustomEditor(typeof(GameEngine.CharacterController))]
    public class CharacterControllerEditor : BaseEditor
    {
        private GameEngine.CharacterController m_target;
        private Rigidbody m_rigidbody;
        private CapsuleCollider m_capsuleCollider;

        private bool m_jumpTab;
        private bool m_wallTab;
        private bool m_groundTab;
        private bool m_movementTab;
        private bool m_stepSlopeTab;
        private bool m_infoTab;

        private GUIContent m_content;

        private void OnEnable()
        {
            m_target = (GameEngine.CharacterController)target;
            m_rigidbody = m_target.GetComponent<Rigidbody>();
            m_capsuleCollider = m_target.GetComponent<CapsuleCollider>();
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
                Separator(Color.grey);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("speed"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("speedFactor"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("threshold"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("dampSpeedUp"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("dampSpeedDown"));
                m_target.airControl = EditorGUILayout.Slider("Air Control",m_target.airControl, 0f, 1f);
            }
            EditorGUI.indentLevel = 0;
            EditorGUILayout.EndVertical();

            //Ground
            EditorGUILayout.BeginVertical("GroupBox");
            EditorGUI.indentLevel = 1;
            m_groundTab = EditorGUILayout.Foldout(m_groundTab, "Ground", true);
            if (m_groundTab)
            {
                Separator(Color.grey);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_groundMask"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("groundRadius"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("groundDistance"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("groundDirection"));

                EditorGUILayout.BeginVertical("Box");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("groundForce"));
                if (m_target.groundForce)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("groundForceMode"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("groundSpringForce"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("groundSpringDamper"));
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUI.indentLevel = 0;
            EditorGUILayout.EndVertical();

            //Step-Slope
            EditorGUILayout.BeginVertical("GroupBox");
            EditorGUI.indentLevel = 1;
            m_stepSlopeTab = EditorGUILayout.Foldout(m_stepSlopeTab, "Step & Slope", true);
            if (m_stepSlopeTab)
            {
                Separator(Color.grey);
                GUILayout.Label("Slope", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_slopeMask"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("slopeLock"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("slopeAngle"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("slopeGravityMultiplier"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("slopeDistance"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("slopeRadius"));

                GUILayout.Space(10);
                GUILayout.Label("Step", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stepOffset"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stepHeight"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stepDistance"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stepSpringForce"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stepSpringDamp"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stepSpringMin"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stepSpringMax"));
                EditorGUILayout.BeginVertical("Box");
                m_content = new GUIContent("Step Iteration", "This value controls how accurately the edges of the stairs are detected to calculate their altitude and process the vector projected at the angle of the stairs.");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stepIteration"), m_content);
                m_content = new GUIContent("","This value controls whether the force is applied if the height of the stairs is greater than this.");
                m_target.stepIterationThreshold = (uint)EditorGUILayout.IntSlider(m_content, (int)m_target.stepIterationThreshold, 1, 100);
                float ratio = m_target.stepIterationThreshold / 100f;
                float value = ratio * m_target.stepIteration;
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), ratio, ratio * 100f + " % (Percent)");
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), ratio, value + " ° (Iterations)");
                EditorGUILayout.EndVertical();


                GUILayout.Space(10f);
                EditorGUILayout.BeginHorizontal("Box");
                if (GUILayout.Button("Spring Setup"))
                {
                    float mass = m_rigidbody.mass <= 30 ? 80 : m_rigidbody.mass;
                    m_target.stepSpringForce = mass * 1.8f;
                    m_target.stepSpringDamp = mass * 0.2f;
                    m_target.stepDistance = 1f;
                    m_target.stepOffset = (m_capsuleCollider.height / -2f) * 0.9f;
                    m_target.stepIteration = 15;
                    m_target.stepIterationThreshold = 20;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel = 0;
            EditorGUILayout.EndVertical();

            //Jump
            EditorGUILayout.BeginVertical("GroupBox");
            EditorGUI.indentLevel = 1;
            m_jumpTab = EditorGUILayout.Foldout(m_jumpTab, "Jump", true);
            if (m_jumpTab)
            {
                Separator(Color.grey);
                m_content = new GUIContent("Jump", "[Enable/Disable] if the character controller can jump");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("jump"), m_content);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("jumpForce"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("jumpCount"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("jumpDelay"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("jumpMemory"));
            }
            EditorGUI.indentLevel = 0;
            EditorGUILayout.EndVertical();

            //Wall
            EditorGUILayout.BeginVertical("GroupBox");
            EditorGUI.indentLevel = 1;
            m_wallTab = EditorGUILayout.Foldout(m_wallTab, "Wall", true);
            if (m_wallTab)
            {
                Separator(Color.grey);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("wallHeight"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("wallDistance"));
            }
            EditorGUI.indentLevel = 0;
            EditorGUILayout.EndVertical();

            //Info
            EditorGUILayout.BeginVertical("GroupBox");
            EditorGUI.indentLevel = 1;
            m_infoTab = EditorGUILayout.Foldout(m_infoTab, "Information", true);
            if (m_infoTab)
            {
                string text = "";
                text += "IsGrounded = " + m_target.isGrounded + "\n";
                text += "IsSloped = " + m_target.isSloped + "\n";
                text += "IsJumping = " + m_target.isJumping + "\n";
                text += "IsTouchingStep = " + m_target.isTouchingStep + "\n";
                text += "IsTouchingWall = " + m_target.isTouchingWall;
                GUILayout.Label(text, "TextArea");
            }
            EditorGUI.indentLevel = 0;
            EditorGUILayout.EndVertical();

            if (GUILayout.Button("Basic Setup"))
            {
                m_rigidbody.mass = 80f;
                m_rigidbody.drag = 0;   
                m_rigidbody.angularDrag = 0.2f;
                m_rigidbody.useGravity = true;
                m_rigidbody.isKinematic = false;
                m_rigidbody.constraints = (RigidbodyConstraints)(64 | 32 | 16);

                m_capsuleCollider.isTrigger = false;
                m_capsuleCollider.height = 2;
                m_capsuleCollider.radius = 0.5f;
                m_capsuleCollider.direction = 1;
            }

            GUILayout.Space(10);

            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(100, 20, 400, 400));

            var speed = (int)m_rigidbody.velocity.magnitude;

            GUILayout.Label("Speed: " + speed, "Box");
            GUILayout.EndArea();
            Handles.EndGUI();
        }
    }

}
