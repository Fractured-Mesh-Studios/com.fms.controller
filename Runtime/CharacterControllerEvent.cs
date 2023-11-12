using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GameEngine
{
    public class CharacterControllerEvent : MonoBehaviour
    {
        [Header("Events")]
        public UnityEvent<Vector3> OnMovement = new UnityEvent<Vector3>();
        public UnityEvent OnJumpStart = new UnityEvent();
        public UnityEvent OnJumpEnd = new UnityEvent();
    }
}
