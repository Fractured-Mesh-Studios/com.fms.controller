using GameEngine;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using CharacterController = GameEngine.CharacterController;

namespace GameEditor
{
    [CustomEditor(typeof(GameEngine.CharacterController))]
    public class CharacterControllerEditor : BaseEditor
    {
        private CharacterController m_target;
        private Rigidbody m_rigidbody;
        private CapsuleCollider m_capsuleCollider;
        private Stamina m_stamina;

        private bool m_jumpTab;
        private bool m_wallTab;
        private bool m_groundTab;
        private bool m_movementTab;
        private bool m_stepSlopeTab;
        private bool m_infoTab;
        private int m_toolBar;

        private GUIContent m_content;

        private void OnEnable()
        {
            m_target = (CharacterController)target;
            m_stamina = m_target.GetComponent<Stamina>();
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

                GUILayout.Space(10);
                Separator(Color.grey);
                m_toolBar = GUILayout.Toolbar(m_toolBar, new string[] { "Crouch", "Prone", "Sprint" });


                //Stance(serializedObject.FindProperty("stances"));

                switch (m_toolBar)
                {
                    case 0:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("crouch"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("crouchSpeed"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("crouchSpeedFactor"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("crouchHeight"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("crouchRadius"));
                        break;
                    case 1: 
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("prone"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("proneSpeed"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("proneSpeedFactor"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("proneHeight"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("proneRadius"));
                        break;
                    case 2:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("sprint"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("sprintSpeed"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("sprintSpeedFactor"));
                        break;
                    case 3: break;
                    case 4: break;
                }

                StaminaBox("Stamina Movement Drain Enabled");
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
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stepSmooth"));
                m_content = new GUIContent("Step Iteration", "This value controls how accurately the edges of the stairs are detected to calculate their altitude and process the vector projected at the angle of the stairs.");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stepIteration"), m_content);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stepForceMode"));

                GUILayout.Space(10f);
                EditorGUILayout.BeginHorizontal("Box");
                if (GUILayout.Button("Spring Setup"))
                {
                    float mass = m_rigidbody.mass <= 30 ? 80 : m_rigidbody.mass;
                    m_target.stepDistance = 1f;
                    m_target.stepOffset = (m_capsuleCollider.height / -2f) * 0.9f;
                    m_target.stepIteration = 15;
                    m_target.stepSmooth = 1f;
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

                StaminaBox("Stamina Jump Drain Enabled");
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

        private void StaminaBox(string text)
        {
            if (m_stamina)
            {
                GUILayout.Space(5);
                EditorGUILayout.HelpBox(text, MessageType.Warning);
            }
        }

       /* private void Stance(SerializedProperty stance)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add"))
            {
                m_target.stances.Add(new Stance("Stance "+m_target.stances.Count, Random.Range(2,10)));
            }
            if(GUILayout.Button("Remove Last"))
            {
                m_target.stances.Remove(m_target.stances[m_target.stances.Count-1]);
            }
            EditorGUILayout.EndHorizontal();

            if(m_target.stances.Count > 0)
            {
                List<string> names = new List<string>();
                for(int i = 0; i < m_target.stances.Count; i++)
                {
                    names.Add(m_target.stances[i].name);    
                }

                m_toolBar = GUILayout.Toolbar(m_toolBar, names.ToArray());

            }


            EditorGUILayout.BeginVertical("Box");

            for(int i = 0; i < stance.arraySize; i++)
            {
                var element = stance.GetArrayElementAtIndex(i);
                if(i == m_toolBar)
                {
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("name"));
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("speed"));
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("factor"));
                }
            }

            EditorGUILayout.EndVertical ();
        }*/


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
