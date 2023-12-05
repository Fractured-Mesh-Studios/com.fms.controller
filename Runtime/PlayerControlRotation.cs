using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameEngine
{
    public class PlayerControlRotation : MonoBehaviour
    {
        public enum UpdateMethod
        {
            None,
            Update,
            FixedUpdate,
            LateUpdate
        }

        public enum ControlMode
        {
            None,
            TowardsAim,
            TowardsMovement
        }

        [HideInInspector] public ControlMode controlMode;
        [HideInInspector] public bool applyRollControl;
        [HideInInspector] public bool applyPitchControl;
        [HideInInspector] public bool applyYawControl = true;
        [HideInInspector] public float smooth = 10f;
        [HideInInspector] public AnimationCurve lerpCurve;
        [HideInInspector] public UpdateMethod updateMethod;

        private Vector3 m_euler;
        private Vector3 m_targetRotation;
        private Quaternion m_rotation;
        private float m_smooth;

        private PlayerController m_controller;
        private CharacterController m_character;
        private Camera m_camera;

        private void Start()
        {
            m_controller = FindObjectOfType<PlayerController>();
            if (!m_controller)
            {
                Debug.LogError("Player controller not found in scene", gameObject);
            }

            m_character = FindAnyObjectByType<CharacterController>();
            if (!m_character)
            {
                Debug.LogError("Player character not found in scene", gameObject);
            }

            m_camera = Camera.main;
        }

        void Update()
        {
            if (updateMethod != UpdateMethod.Update) 
                return;

            CalculateMode(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (updateMethod != UpdateMethod.FixedUpdate) 
                return;

            CalculateMode(Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            if (updateMethod != UpdateMethod.LateUpdate) 
                return;
            
            CalculateMode(Time.maximumDeltaTime);
        }

        private void CalculateMode(float delta)
        {
            switch (controlMode)
            {
                //Movement
                case ControlMode.TowardsMovement:
                    Vector3 relative = RelativeTo(m_controller.direction, m_camera.transform);

                    m_rotation = Quaternion.FromToRotation(Vector3.forward, relative);

                    m_euler = new Vector3(0, m_rotation.eulerAngles.y + m_rotation.eulerAngles.x, 0);

                    m_rotation = Quaternion.Euler(m_euler);

                    float angle = Vector3.Angle(transform.forward, m_character.direction);
                    m_smooth = lerpCurve.Evaluate(angle / 180f) * smooth;

                    if (m_controller.direction.magnitude > 0)
                        transform.rotation = Quaternion.Slerp(transform.rotation, m_rotation, delta * m_smooth);
                    break;
                //Aim
                case ControlMode.TowardsAim:
                    m_targetRotation = m_controller.target.rotation.eulerAngles;

                    if (applyRollControl) { m_euler.z = m_targetRotation.z; }
                    if (applyPitchControl) { m_euler.x = m_targetRotation.x; }
                    if (applyYawControl) { m_euler.y = m_targetRotation.y; }
                
                    m_rotation = Quaternion.Euler(m_euler);

                    transform.rotation = Quaternion.Slerp(transform.rotation, m_rotation, delta * smooth);
                    break;
                default: break;
            }
        }

        private Vector3 RelativeTo(Vector3 vector3, Transform relativeToThis, bool isPlanar = true)
        {
            Vector3 forward = relativeToThis.forward;
            if (isPlanar)
            {
                forward = Vector3.ProjectOnPlane(forward, Vector3.up);

                if (forward.sqrMagnitude < 9.99999943962493E-11)
                    forward = Vector3.ProjectOnPlane(relativeToThis.up, Vector3.up);
            }

            Quaternion rotation = Quaternion.LookRotation(forward);
            return rotation * vector3;
        }
    }
}
