using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameEngine
{
    [RequireComponent(typeof(Collider))]
    public class AreaEffector : MonoBehaviour
    {
        [Header("General")]
        public bool gravity;
        public bool massOverride;
        public float mass = 1;
        public float massFactor = 1;
       
        [Header("Linear")]
        public Vector3 linearDirection = Vector3.up;
        public float linearForce = 15f;
        public ForceMode linearForceMode = ForceMode.Force;
        public float linearDrag = 0f;

        [Header("Angular")]
        public Vector3 angularDirection = Vector3.zero;
        public float angularForce = 10f;
        public ForceMode angularForceMode = ForceMode.Force;
        public float angularDrag = 0.2f;

        private Collider[] m_collider;

        //
        private List<Rigidbody> m_rigidbodies = new List<Rigidbody>();
        private Dictionary<Rigidbody, float> m_rigidbodyMass = new Dictionary<Rigidbody, float>();
        private Dictionary<Rigidbody, bool> m_rigidbodyGravity = new Dictionary<Rigidbody, bool>();
        private Dictionary<Rigidbody, float[]> m_rigidbodyDrag = new Dictionary<Rigidbody, float[]>();

        protected virtual void Awake()
        {
            m_collider = GetComponents<Collider>();
        }

        protected virtual void FixedUpdate()
        {
            UpdateRigidbodies();
        }

        private void UpdateRigidbodies()
        {
            linearDirection.Normalize();
            angularDirection.Normalize();

            for(int i = 0; i < m_rigidbodies.Count; ++i)
            {
                m_rigidbodies[i].AddForce(linearDirection * linearForce, linearForceMode);
                m_rigidbodies[i].AddTorque(angularDirection  * angularForce, angularForceMode);
            }
        }

        #region COLLISON
        private void OnTriggerEnter(Collider other)
        {
            if(!enabled) return;
            if (!m_rigidbodies.Contains(other.attachedRigidbody))
            {
                m_rigidbodies.Add(other.attachedRigidbody);
            }

            if (!m_rigidbodyGravity.ContainsKey(other.attachedRigidbody))
            {
                m_rigidbodyGravity.Add(other.attachedRigidbody, other.attachedRigidbody.useGravity);

                other.attachedRigidbody.useGravity = gravity;
            }

            var otherDrag = new float[] {
                other.attachedRigidbody.drag,
                other.attachedRigidbody.angularDrag
            };

            if (!m_rigidbodyDrag.ContainsKey(other.attachedRigidbody))
            {
                m_rigidbodyDrag.Add(other.attachedRigidbody, otherDrag);
            }

            if(!m_rigidbodyMass.ContainsKey(other.attachedRigidbody))
            {
                m_rigidbodyMass.Add(other.attachedRigidbody, other.attachedRigidbody.mass);

                other.attachedRigidbody.mass = massOverride ? 
                    mass * massFactor : 
                    other.attachedRigidbody.mass * massFactor;
            }

            other.attachedRigidbody.drag = linearDrag;
            other.attachedRigidbody.angularDrag = angularDrag;
        }

        private void OnTriggerExit(Collider other)
        {
            if(!enabled) return;
            if (m_rigidbodies.Contains(other.attachedRigidbody))
            {
                m_rigidbodies.Remove(other.attachedRigidbody);
            }

            if (m_rigidbodyGravity.ContainsKey(other.attachedRigidbody))
            {
                other.attachedRigidbody.useGravity = m_rigidbodyGravity[other.attachedRigidbody];
                m_rigidbodyGravity.Remove(other.attachedRigidbody);
            }

            if (m_rigidbodyDrag.ContainsKey(other.attachedRigidbody))
            {
                var otherDrag = new Tuple<float, float>(
                    m_rigidbodyDrag[other.attachedRigidbody][0],
                    m_rigidbodyDrag[other.attachedRigidbody][1]
                );

                other.attachedRigidbody.drag = otherDrag.Item1;
                other.attachedRigidbody.angularDrag = otherDrag.Item2;

                m_rigidbodyDrag.Remove(other.attachedRigidbody);
            }

            if (m_rigidbodyMass.ContainsKey(other.attachedRigidbody))
            {
                other.attachedRigidbody.mass = m_rigidbodyMass[other.attachedRigidbody];

                m_rigidbodyMass.Remove(other.attachedRigidbody);
            }
        }
        #endregion

        private void OnDrawGizmos()
        {
            Vector3 position = transform.position;

            if(m_collider != null && m_collider.Length > 0)
            {
                BoxCollider box = m_collider.OfType<BoxCollider>().FirstOrDefault();
                if (box)
                {
                    Gizmos.color = Color.green;
                    position = transform.TransformPoint(box.center);
                    Gizmos.DrawWireCube(position, box.size);
                }

                SphereCollider sphere = m_collider.OfType<SphereCollider>().FirstOrDefault();
                if (sphere)
                {
                    Gizmos.color = Color.green;
                    position = transform.TransformPoint(sphere.center);
                    Gizmos.DrawWireSphere(position, sphere.radius);
                }
            }
        }
    }
}
