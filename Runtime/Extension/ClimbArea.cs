using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{
    [RequireComponent(typeof(BoxCollider))]
    public class ClimbArea : MonoBehaviour
    {
        public bool canJump = false;
        public Vector3 direction = Vector3.up;
        public float speed = 10f;

        private BoxCollider m_collider;
        private CharacterController m_controller;
        private List<Rigidbody> m_rigidbodies = new List<Rigidbody>();

        //Controller Settings
        private bool m_canJump = false;
        private float m_airControl = 0f;

        private void Awake()
        {
            m_collider = GetComponent<BoxCollider>();
        }

        private void FixedUpdate()
        {
            for(int i = 0; i < m_rigidbodies.Count; i++) 
            {
                Rigidbody rb = m_rigidbodies[i];

                if (m_controller)
                {
                    rb.AddForce(direction * speed, ForceMode.Acceleration);
                }

            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!m_rigidbodies.Contains(other.attachedRigidbody))
            {
                m_rigidbodies.Add(other.attachedRigidbody);

                try
                {
                    m_controller = other.GetComponent<CharacterController>();
                    if (m_controller)
                    {
                        m_canJump = m_controller.jump;
                        m_airControl = m_controller.airControl;

                        m_controller.jump = canJump;
                        m_controller.airControl = 1f;
                    }
                }
                catch
                {
                    Debug.Log("Character Controller Found");
                }
            
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (m_rigidbodies.Contains(other.attachedRigidbody))
            {
                m_rigidbodies.Remove(other.attachedRigidbody);

                if(m_controller.gameObject == other.gameObject)
                {
                    m_controller.jump = m_canJump;
                    m_controller.airControl = m_airControl;
                    m_controller = null;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!m_collider)
            {
                m_collider = GetComponent<BoxCollider>();
            }

            Vector3 position = transform.position + m_collider.center;
            Gizmos.color = new Color(0, 0.7f, 0, 0.25f);
            Gizmos.DrawCube(position, m_collider.size);

            Gizmos.color = Color.white;
            for(int i = 0;i < m_rigidbodies.Count;i++)
            {
                var root = m_rigidbodies[i].transform.root;
                var mesh = root.GetComponentInChildren<MeshFilter>().mesh;

                Gizmos.DrawMesh(mesh, m_rigidbodies[i].position);    
            }
        }
    }
}
