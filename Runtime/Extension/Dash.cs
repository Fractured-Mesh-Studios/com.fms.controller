using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace GameEngine
{
    [RequireComponent(typeof(Rigidbody))]
    public class Dash : MonoBehaviour
    {
        [HideInInspector] public bool directional;
        [HideInInspector] public float massFactor = 1;
        [HideInInspector] public bool useGrounded;
        [HideInInspector] public float impulse = 1000f;
        [HideInInspector] public float coldown = 5f;
        [HideInInspector] public float drain = 200f;

        [HideInInspector] public UnityEvent<Vector3> onDash = new UnityEvent<Vector3>();

        public bool canDash { 
            get {
                bool ground = useGrounded ? m_controller.isGrounded : true;
                return ground && m_canDash;
            } 
        }

        private Rigidbody m_rigidbody;
        private CharacterController m_controller;
        private Stamina m_stamina;
        private bool m_canDash;

        private void Awake()
        {
            m_rigidbody = GetComponent<Rigidbody>();
            m_controller = GetComponent<CharacterController>();
            m_stamina = GetComponent<Stamina>();
            m_canDash = true;
        }

        #region INPUT
        private void OnDash(InputValue value)
        {
            if (canDash && m_stamina && !m_stamina.Use(drain)) return;

            if (value.isPressed && m_canDash)
            {
                if (useGrounded)
                {
                    if (m_controller.isGrounded)
                    {
                        m_canDash = false;
                        Vector3 dashDirection = directional ? m_controller.direction : transform.forward;
                        float mass = massFactor > 0 ? m_rigidbody.mass * massFactor : 1f;
                        m_rigidbody.AddForce(dashDirection * impulse * mass, ForceMode.Impulse);
                        Invoke("OnDashColdown", coldown);
                        onDash.Invoke(dashDirection);
                    }
                }
                else
                {
                    m_canDash = false;
                    Vector3 dashDirection = directional ? m_controller.direction : transform.forward;
                    float mass = massFactor > 0 ? m_rigidbody.mass * massFactor : 1f;
                    m_rigidbody.AddForce(dashDirection * impulse * mass, ForceMode.Impulse);
                    Invoke("OnDashColdown", coldown);
                    onDash.Invoke(dashDirection);
                }
            }
        }

        private void OnDashColdown()
        {
            m_canDash = true;
        }
        #endregion
    }
}

