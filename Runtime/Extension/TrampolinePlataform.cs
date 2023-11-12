using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{
    [RequireComponent(typeof(Collider))]
    public class TrampolinePlataform : MonoBehaviour
    {
        [HideInInspector] public bool useMass = true;
        [HideInInspector] public float bounceStrength = 25f;

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.rigidbody != null) 
            {
                Rigidbody rb = collision.rigidbody;
                float mass = useMass ? rb.mass : 1f;

                rb.AddForce(bounceStrength * transform.up * mass, ForceMode.Impulse);
            }
        }
    }
}
