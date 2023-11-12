using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CharacterController))]
    public class CharacterGravity : MonoBehaviour
    {
        [HideInInspector]public Transform target;
        [HideInInspector]public Vector3 targetVector;
        [HideInInspector]public float force = 9.81f;
        [HideInInspector]public float scale = 1f;
        [HideInInspector][Range(0f, 1f)] public float mass = 0f;
        [HideInInspector]public ForceMode forceMode = ForceMode.Acceleration;

        private CharacterController m_controller;
        private Rigidbody m_rigidbody;
        private Vector3 m_direction;
        private float m_scaledForce;
        private Vector3 m_newton;

        private void Awake()
        {
            m_rigidbody = GetComponent<Rigidbody>();
            m_controller = GetComponent<CharacterController>();

            m_rigidbody.useGravity = false;
        }

        private void FixedUpdate()
        {
            m_direction = (target) ?
                target.position - transform.position :
                targetVector - transform.position;

            m_direction = m_direction.normalized;
            m_scaledForce = force * (mass > 0f ? m_rigidbody.mass : 1f);
            m_newton = m_direction * m_scaledForce * scale;
            m_rigidbody.AddForce(m_newton, forceMode);
        }
    }
}
