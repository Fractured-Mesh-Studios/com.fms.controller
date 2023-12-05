using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace GameEngine
{
    [RequireComponent(typeof(CharacterController))]
    public class CharacterControllerEvent : MonoBehaviour
    {
        [Header("Events")]
        [Space(10)] public UnityEvent<Vector3> OnMovement = new UnityEvent<Vector3>();
        [Space(10)] public UnityEvent OnJumpStart = new UnityEvent();
        [Space(10)] public UnityEvent OnJumpEnd = new UnityEvent();

        private CharacterController m_character;
        private bool m_lastJump = false;
        private bool m_lastMovement = false;

        private void OnEnable()
        {
            OnMovement.AddListener(OnMovementCallback);
        }

        private void OnDisable()
        {
            OnMovement.RemoveListener(OnMovementCallback);
        }

        private void Awake()
        {
            m_character = GetComponent<CharacterController>();
        }

        private void Update()
        {
            float magnitude = m_character.direction.magnitude;
            if (magnitude > 0f && !m_lastMovement)
            {
                OnMovement.Invoke(m_character.direction);
            }

            if(magnitude <= 0f && m_lastMovement)
            {
                OnMovement.Invoke(Vector3.zero);
            }

            if (m_character.isJumping != m_lastJump && m_character.isGrounded)
            {
                OnJumpEnd.Invoke();
            }

            if (m_character.isJumping != m_lastJump && !m_character.isGrounded)
            {
                OnJumpStart.Invoke();
            }

            m_lastJump = m_character.isJumping;
            m_lastMovement = magnitude == 0f ? false : m_lastMovement;
        }

        private void OnMovementCallback(Vector3 axis)
        {
            m_lastMovement = true;
        }
    }
}
