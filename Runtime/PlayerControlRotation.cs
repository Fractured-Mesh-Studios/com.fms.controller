using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameEngine
{
    public class PlayerControlRotation : MonoBehaviour
    {
        [Header("Control Rotation")]
        public PlayerController controller;
        public bool applyRollControl;
        public bool applyPitchControl;
        public bool applyYawControl = true;
        public float rotationRate = 10f;

        private Vector3 m_rotation;
        private Vector3 m_targetRotation;

        void Update()
        {
            m_targetRotation = controller.target.rotation.eulerAngles;

            if(applyRollControl) { m_rotation.z = m_targetRotation.z; }
            if(applyPitchControl) { m_rotation.x = m_targetRotation.x; }
            if(applyYawControl) { m_rotation.y = m_targetRotation.y; }

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(m_rotation), Time.deltaTime * rotationRate);
        }
    }
}
