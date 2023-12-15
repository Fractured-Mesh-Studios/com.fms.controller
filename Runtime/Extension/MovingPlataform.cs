using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEngine;
using UnityEngine.Experimental.AI;

namespace GameEngine
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class MovingPlataform : MonoBehaviour
    {
        public enum PlataformMode
        {
            None,
            Forward,
            Backward,
            Random,
        }

        [HideInInspector] public PlataformMode mode; 
        [HideInInspector] public Vector3[] waypoints;
        [HideInInspector] public float timeCycle = 1f;
        [HideInInspector] public float timeDelay = 1f;
        [HideInInspector] public float speedDamp = 100f;
        [HideInInspector] public float distanceThreshold = 0.01f;
        [HideInInspector] public bool canRotate = true;
        [HideInInspector] public bool canTranslate = true;
        [HideInInspector] public Vector3 rotationSpeed;

        private Vector3 m_targetWaypoint;
        private Vector3 m_velocity;

        //Last
        private Vector3 m_lastPosition;
        private Vector3 m_lastEulerAngles;

        private int m_currentWaypoint;
        private Rigidbody m_rigidbody;
        private bool m_canMove;
        private List<Rigidbody> m_rigidbodies = new List<Rigidbody>();

        private void Awake()
        {
            m_rigidbody = GetComponent<Rigidbody>();
            if (m_rigidbody)
            {
                m_rigidbody.isKinematic = true;
                m_rigidbody.useGravity = false;
            }

            Begin();
        }

        private void FixedUpdate()
        {
            UpdateWaypoint();
            UpdatePositionAndRotation();
            UpdateRigidbodies();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if(!m_rigidbodies.Contains(collision.rigidbody))
                m_rigidbodies.Add(collision.rigidbody);
        }

        private void OnCollisionExit(Collision collision)
        {
            if(m_rigidbodies.Contains(collision.rigidbody))
                m_rigidbodies.Remove(collision.rigidbody);
        }

        #region PRIVATE
        private void UpdateWaypoint()
        {
            if (Vector3.Distance(transform.position, m_targetWaypoint) <= distanceThreshold)
            {
                m_rigidbody.position = m_targetWaypoint;

                if ((m_currentWaypoint == 0 || m_currentWaypoint == waypoints.Length - 1) && m_canMove)
                    StartCoroutine(WaitTime(timeCycle));
                else if (m_canMove)
                    StartCoroutine(WaitTime(timeDelay));

                switch (mode)
                {
                    case PlataformMode.Forward: SetNextWaypoint(); break;
                    case PlataformMode.Backward: SetPrevWaypoint(); break;
                    case PlataformMode.Random: SetRandomWaypoint(); break;
                    default: break;
                }
            }
        }

        private void UpdateRigidbodies()
        {
            if(m_rigidbodies.Count > 0)
            {
                Vector3 velocity = transform.position - m_lastPosition;
                Vector3 angularVelocity = transform.eulerAngles - m_lastEulerAngles;
                for(int i = 0;  i < m_rigidbodies.Count; i++)
                {
                    Rigidbody rb = m_rigidbodies[i];

                    if(angularVelocity.y > 0)
                    {
                        rb.transform.RotateAround(transform.position, Vector3.up, angularVelocity.y);
                        try { rb.GetComponent<CharacterController>(); }
                        catch { Debug.LogError("No Player In Plataform"); }
                    }

                    if (m_rigidbody.velocity.magnitude > 0) 
                        rb.velocity += m_rigidbody.velocity;

                    rb.position += velocity;
                }
            }
            
            m_lastPosition = transform.position;
            m_lastEulerAngles = transform.eulerAngles;
        }

        private void UpdatePositionAndRotation()
        {
            if (m_canMove)
            {
                if (canTranslate)
                {
                    m_rigidbody.position = Vector3.SmoothDamp(
                        m_rigidbody.position,
                        m_targetWaypoint, 
                        ref m_velocity, 
                        speedDamp * Time.fixedDeltaTime
                    );
                }

                if (canRotate)
                {
                    m_rigidbody.rotation *= Quaternion.Euler(rotationSpeed * Time.fixedDeltaTime);
                }
            }
        }

        private void SetNextWaypoint()
        {
            m_currentWaypoint++;
            if (m_currentWaypoint > waypoints.Length - 1)
                m_currentWaypoint = 0;

            m_targetWaypoint = waypoints[m_currentWaypoint];
        }

        private void SetPrevWaypoint()
        {
            m_currentWaypoint--;
            if(m_currentWaypoint < 0)
                m_currentWaypoint = waypoints.Length - 1;

            m_targetWaypoint = waypoints[m_currentWaypoint];
        }

        private void SetRandomWaypoint()
        {
            List<int> index = new List<int>();
            for(int i = 0; i < waypoints.Length; i++) { 
                if(i != m_currentWaypoint)
                {
                    index.Add(i);
                }
            }

            int value = Random.Range(0, index.Count);
            m_currentWaypoint = value;
            m_targetWaypoint = waypoints[value];
        }

        private IEnumerator WaitTime(float time)
        {
            m_canMove = false;
            yield return new WaitForSeconds(time);
            m_canMove = true;
        }
        #endregion

        #region CONTROLS

        public void Begin()
        {
            if (canTranslate)
                transform.position = waypoints[0];
            m_targetWaypoint = transform.position;
            m_currentWaypoint = 0;
            m_canMove = true;
        }

        public void End()
        {
            if(canTranslate)
                transform.position = waypoints[waypoints.Length - 1];
            m_targetWaypoint = transform.position;
            m_currentWaypoint = waypoints.Length - 1;
            m_canMove = true;
        }

        public void Next()
        {
            SetNextWaypoint();
        }

        public void Previous()
        {
            SetPrevWaypoint();
        }

        public void Play() { m_canMove = true; }

        public void Stop() { m_canMove = false; }

        #endregion

        #region GIZMOS
        private void OnDrawGizmosSelected()
        {
            if(waypoints != null)
            {
                for (int i = 0; i < waypoints.Length; i++)
                {
                    if(i == 0) Gizmos.color = Color.green;
                    else if(i ==  waypoints.Length-1) Gizmos.color = Color.red;
                    else Gizmos.color = Color.white;

                    Gizmos.DrawSphere(waypoints[i], 0.2f);
                    if (i >= 0 && i < waypoints.Length-1)
                    {
                        Gizmos.color = Color.white;
                        Gizmos.DrawLine(waypoints[i], waypoints[i+1]);
                    }
                }
            }

            for(int i = 0; i < m_rigidbodies.Count; i++)
            {
                var m = m_rigidbodies[i].transform.root.GetComponentInChildren<MeshFilter>().mesh;

                Gizmos.DrawWireMesh(m, m_rigidbodies[i].position);
            }
        }
        #endregion

    }
}
