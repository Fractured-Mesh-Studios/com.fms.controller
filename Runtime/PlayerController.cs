using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static PlasticPipe.PlasticProtocol.Messages.Serialization.ItemHandlerMessagesSerialization;

namespace GameEngine
{
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

        [HideInInspector] public bool cursorVisible = true;
        [HideInInspector] public CursorLockMode cursorLockMode;

        private Vector3 m_look;
        private Quaternion m_axisDelta;
        private CharacterController characterController;

        void Start()
        {
            characterController = GetComponent<CharacterController>();
            Cursor.lockState = cursorLockMode;
            Cursor.visible = cursorVisible;
        }

        private void Update()
        {
            float x = m_look.x * inputPitchScale * Time.deltaTime;
            float y = m_look.y * inputYawScale * Time.deltaTime;
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
            characterController.Move(value.Get<Vector2>());
        }

        private void OnJump(InputValue value)
        {
            characterController.Jump();
        }

        private void OnLook(InputValue value)
        {
            m_look = value.Get<Vector2>();
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
