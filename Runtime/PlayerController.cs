using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameEngine
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [HideInInspector] public Transform target;
        [HideInInspector][Range(-1,1)] public float inputRollScale = 0f;
        [HideInInspector][Range(-1,1)] public float inputPitchScale = 1f;
        [HideInInspector][Range(-1,1)] public float inputYawScale = 1f;
        [HideInInspector] public float inputRotationRate = 15;
        [HideInInspector] public ControlLimit rollControlLimit;
        [HideInInspector] public ControlLimit pitchControlLimit = new ControlLimit(-45,45);
        [HideInInspector] public ControlLimit yawControlLimit = new ControlLimit(-180, 180);

        //cursor
        [HideInInspector] public bool cursorVisible = true;
        [HideInInspector] public CursorLockMode cursorLockMode;

        //Jump
        [HideInInspector] public bool jumpHold = false;
        [HideInInspector] public float jumpHoldSmooth = 2f;
        [HideInInspector] public float jumpHoldMin = 1.0f;
        [HideInInspector] public float jumpHoldMax = 2.2f;
        [HideInInspector] public float jumpDrain = 25f;

        //Sprint
        [HideInInspector] public bool sprintHold = false;
        [HideInInspector] public float sprintDrain = 15f;

        //crouch
        [HideInInspector] public bool crouchHold = false;
        [HideInInspector] public float crouchDrain = 5f;

        //movement
        [HideInInspector] public bool movementRootMotion = false;
        [HideInInspector] public float movementDrain = 1f;
        [HideInInspector] public float movementScrollFactor = 0.01f;

        //prone
        [HideInInspector] public bool proneHold;
        [HideInInspector] public float proneDrain = 10f;

        public Vector3 direction { internal set; get; }


        //components
        private CharacterController m_character;
        private Rigidbody m_rigidbody;
        private Stamina m_stamina;

        //values
        private Vector3 m_look;
        private Quaternion m_axisDelta;
        private bool m_jumpHolded = false;
        private float m_jumpHoldValue = 0f;
        private bool m_sprintValue;
        private bool m_crouchValue;

        private float m_speedFactor;

        void Start()
        {
            m_stamina = GetComponent<Stamina>();
            m_character = GetComponent<CharacterController>();
            m_rigidbody = GetComponent<Rigidbody>();

            Cursor.lockState = cursorLockMode;
            Cursor.visible = cursorVisible;
        }

        private void Update()
        {
            float x = m_look.x * inputYawScale * Time.deltaTime;
            float y = m_look.y * inputPitchScale* Time.deltaTime;
            float z = m_look.z * inputRollScale * Time.deltaTime;
            
            m_axisDelta = target.rotation;
            m_axisDelta *= Quaternion.AngleAxis(x * inputRotationRate, Vector3.up);
            m_axisDelta *= Quaternion.AngleAxis(y * inputRotationRate, Vector3.right);
            m_axisDelta *= Quaternion.AngleAxis(z * inputRotationRate, Vector3.forward);
            Vector3 angles = m_axisDelta.eulerAngles;

            angles.x = ClampAngle(angles.x, pitchControlLimit.min, pitchControlLimit.max);
            angles.y = ClampAngle(angles.y, yawControlLimit.min, yawControlLimit.max);
            angles.z = ClampAngle(angles.z, rollControlLimit.min, rollControlLimit.max);

            target.localEulerAngles = angles;

            if (m_jumpHolded)
            {
                m_jumpHoldValue += Time.deltaTime * jumpHoldSmooth;
                m_jumpHoldValue = Mathf.Clamp(m_jumpHoldValue, jumpHoldMin, jumpHoldMax);
            }

            if(m_rigidbody.velocity.magnitude == 0 || !m_character.isGrounded)
            {
                m_stamina.RemoveDrain("move");
                m_stamina.RemoveDrain("sprint");
            }

        }

        #region INTERNAL
        private float ClampAngle(float current, float min, float max)
        {
            float dtAngle = Mathf.Abs(((min - max) + 180) % 360 - 180);
            float hdtAngle = dtAngle * 0.5f;
            float midAngle = min + hdtAngle;

            float offset = Mathf.Abs(Mathf.DeltaAngle(current, midAngle)) - hdtAngle;
            if (offset > 0)
                current = Mathf.MoveTowardsAngle(current, midAngle, offset);
            return current;
        }
        #endregion

        #region INPUT
        private void OnMovement(InputValue value)
        {
            Vector2 axis = value.Get<Vector2>();
            bool isPressed = axis.magnitude > 0;
            direction = new Vector3(axis.x, 0f, axis.y);

            if (movementRootMotion) 
                return;

            if (m_stamina)
            {
                if(isPressed) 
                { m_stamina.AddDrain("move", movementDrain); } 
                else 
                { m_stamina.RemoveDrain("move"); }

                if (!m_stamina.isEmpty)
                {
                    m_character.Move(axis);
                }
                else
                {
                    m_character.Move(Vector3.zero);
                    return;
                }
            }
            else
            {
                m_character.Move(axis);
            }
        }

        private void OnMovementSpeed(InputValue value) 
        {
            m_speedFactor += value.Get<float>() * movementScrollFactor;
            m_character.speedFactor = m_speedFactor;
        }

        private void OnJump(InputValue value)
        {
            if (!jumpHold)
            {
                if (m_stamina && value.isPressed)
                {
                    if (m_character.canJump && m_stamina.Use(jumpDrain))
                    {
                        m_character.Jump();
                    }
                }
            }
            else
            {
                m_jumpHolded = value.isPressed;
                if (m_stamina)
                {
                    if (m_character.canJump && !value.isPressed)
                    {
                        if (m_stamina.Use(jumpDrain))
                        {
                            float factor = m_stamina.GetStaminaFillAmount(1f);
                            m_character.Jump(m_jumpHoldValue * factor);
                            m_jumpHoldValue = 0;
                        }
                    }
                }
                else
                {
                    if (m_character.canJump && !value.isPressed)
                    {
                        m_character.Jump(m_jumpHoldValue);
                        m_jumpHoldValue = 0;
                    }
                }
            }

        }

        private void OnLook(InputValue value)
        {
            m_look = value.Get<Vector2>();
        }

        private void OnSprint(InputValue value)
        {
            if (sprintHold)
            {
                if(value.isPressed)
                {
                    if (m_stamina)
                        m_stamina.AddDrain("sprint", sprintDrain);

                    m_character.Sprint(true);
                }
                else
                {
                    if (m_stamina)
                        m_stamina.RemoveDrain("sprint");

                    m_character.Sprint(false);
                }
            }
            else
            {
                if (value.isPressed)
                {
                    m_sprintValue = !m_sprintValue;

                    if (m_stamina)
                    {
                        if (m_sprintValue)
                            m_stamina.AddDrain("sprint", sprintDrain);
                        else
                            m_stamina.RemoveDrain("sprint");
                    }

                    m_character.Sprint(m_sprintValue);
                }
            }

            
        }

        private void OnCrouch(InputValue value)
        {
            if (m_stamina)
            {
                if (m_character.isCrouch)
                    m_stamina.AddDrain("crouch", crouchDrain);
                else
                    m_stamina.RemoveDrain("crouch");
            }

            if (crouchHold)
            {
                m_character.Crouch(value.isPressed);
            }
            else
            {
                if (value.isPressed)
                {
                    m_crouchValue = !m_crouchValue;
                    m_character.Crouch(m_crouchValue);
                }
            }
        }

        private void OnProne(InputValue value)
        {
            if (m_stamina)
            {
                if (m_character.isProne)
                    m_stamina.AddDrain("prone", proneDrain);
                else
                    m_stamina.RemoveDrain("prone");

                if (!m_stamina.ContainDrain("prone"))
                    return;
            }

            if (proneHold)
            {
                m_character.Prone(value.isPressed);
            }
            else
            {
                if (value.isPressed)
                {
                    m_character.Prone();
                }
            }
        }
        #endregion

        #region PUBLIC

        public float GetJumpFillAmount(float scale = 100f)
        {
            return Mathf.Clamp01(m_jumpHoldValue / jumpHoldMax) * scale;
        }

        #endregion

    }

    [System.Serializable]
    public struct ControlLimit
    {
        public float min;
        public float max;

        public ControlLimit(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
    }
}
