using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;


namespace GameEngine
{

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class CharacterController : MonoBehaviour
    {
        public enum MovementMode
        {
            None,
            Force,
            Velocity,
        }

        //movement
        [HideInInspector]public MovementMode mode = MovementMode.Velocity;
        [HideInInspector]public float speed = 10f;
        [HideInInspector]public float speedFactor = 1f;
        [HideInInspector]public float threshold = 0.15f;
        [HideInInspector]public float dampSpeedUp = 0.2f;
        [HideInInspector]public float dampSpeedDown = 0.1f;
        [HideInInspector]public float acceleration = 8;
        [HideInInspector]public float maxAccelerationFactor = 4;
        [HideInInspector]public AnimationCurve accelerationFactorFromDot = AnimationCurve.EaseInOut(-1f, 2f, 1f, 1f);
        [HideInInspector]public float maxAccelerationForce = 20;
        [HideInInspector]public AnimationCurve maxAccelerationForceFactorFromDot = AnimationCurve.EaseInOut(-1f, 2f, 1f, 1f);
        //ground
        [HideInInspector]public float groundRadius = 0.15f;
        [HideInInspector]public float groundDistance = 1f;
        [HideInInspector]public Vector3 groundDirection = Vector3.down;
        //slope
        [HideInInspector]public float slopeAngle = 55f;
        [HideInInspector]public float slopeGravityMultiplier = 6f;
        [HideInInspector]public float slopeDistance = 1.0f;
        [HideInInspector]public float slopeRadius = 0.1f;
        [HideInInspector]public bool slopeLock = true;
        //step
        [HideInInspector]public float stepOffset = 0.1f;
        [HideInInspector]public float stepHeight = 0.2f;
        [HideInInspector]public float stepDistance = 0.15f;
        //jump
        [HideInInspector]public float jumpForce = 10f;
        //wall
        [HideInInspector]public float wallHeight = 0f;
        [HideInInspector]public float wallDistance = 1f;
        [HideInInspector]public new Transform camera = null;
       
        public bool isGrounded {  get; private set; }
        public bool isSloped { get; private set; }
        public bool isJumping {  get; private set; }
        public bool isWall {  get; private set; }
        public bool isStep {  get; private set; }


        [SerializeField][HideInInspector]private LayerMask m_slopeMask;
        [SerializeField][HideInInspector]private LayerMask m_groundMask;
        private Rigidbody m_rigidbody;
        private Rigidbody m_groundRigidbody;
        private CapsuleCollider m_collider;
        private Transform m_camera;
        private Vector3 m_velocity;
        private Vector3 m_aceleration;
        private Vector3 m_direction;
        private Vector3 m_slopeNormal;
        private Vector3 m_movementDirection;
        private float m_speed;
        private float m_staticFriction;
        private float m_dynamicFriction;
        private bool m_onSlopeAngle;
        private PhysicMaterialCombine m_frictionCombine;

        protected virtual void Awake () 
        {
            m_rigidbody = GetComponent<Rigidbody>();
            m_collider = GetComponent<CapsuleCollider>();
            if(m_collider)
            {
                m_staticFriction = m_collider.material.staticFriction;
                m_dynamicFriction = m_collider.material.dynamicFriction;
                m_frictionCombine = m_collider.material.frictionCombine;
            }    
            m_camera = camera ? camera : Camera.main.transform;
        }

        protected virtual void Update()
        {
            //Checks
            isGrounded = CheckGround();
            isSloped = CheckSlopeAngle();
            isWall = CheckWall().Item1;
            isStep = CheckStep();
        }

        protected virtual void FixedUpdate () 
        {
            CalculateMovement();
        }

        #region CHECKS
        private bool CheckStep()
        {
            bool tmpStep = false;
            Vector3 bottomStepPos = transform.position - (groundDirection * groundDistance) + new Vector3(0f, 0.05f, 0f);

            RaycastHit stepLowerHit;
            if (Physics.Raycast(bottomStepPos, Vector3.forward, out stepLowerHit, stepOffset, m_groundMask))
            {
                RaycastHit stepUpperHit;
                if (RoundValue(stepLowerHit.normal.y) == 0 && !Physics.Raycast(bottomStepPos + new Vector3(0f, stepHeight, 0f), Vector3.forward, out stepUpperHit, stepDistance + 0.05f, m_groundMask))
                {
                    //rigidbody.position -= new Vector3(0f, -stepSmooth, 0f);
                    tmpStep = true;
                }
            }

            RaycastHit stepLowerHit45;
            if (Physics.Raycast(bottomStepPos, Quaternion.AngleAxis(45, transform.up) * Vector3.forward, out stepLowerHit45, stepOffset, m_groundMask))
            {
                RaycastHit stepUpperHit45;
                if (RoundValue(stepLowerHit45.normal.y) == 0 && !Physics.Raycast(bottomStepPos + new Vector3(0f, stepHeight, 0f), Quaternion.AngleAxis(45, Vector3.up) * Vector3.forward, out stepUpperHit45, stepDistance + 0.05f, m_groundMask))
                {
                    //rigidbody.position -= new Vector3(0f, -stepSmooth, 0f);
                    tmpStep = true;
                }
            }

            RaycastHit stepLowerHitMinus45;
            if (Physics.Raycast(bottomStepPos, Quaternion.AngleAxis(-45, transform.up) * Vector3.forward, out stepLowerHitMinus45, stepOffset, m_groundMask))
            {
                RaycastHit stepUpperHitMinus45;
                if (RoundValue(stepLowerHitMinus45.normal.y) == 0 && !Physics.Raycast(bottomStepPos + new Vector3(0f, stepHeight, 0f), Quaternion.AngleAxis(-45, Vector3.up) * Vector3.forward, out stepUpperHitMinus45, stepDistance + 0.05f, m_groundMask))
                {
                    //rigidbody.position -= new Vector3(0f, -stepSmooth, 0f);
                    tmpStep = true;
                }
            }

            return tmpStep;
        }

        private bool CheckGround()
        {
            RaycastHit groundHit;
            return Physics.SphereCast(transform.position ,groundRadius, groundDirection,out groundHit, groundDistance, m_groundMask);
        }

        private bool CheckSlopeAngle()
        {
            RaycastHit slopeHit;
            if (Physics.SphereCast(transform.position, slopeRadius, groundDirection, out slopeHit, slopeDistance, m_slopeMask))
            {
                float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
                m_slopeNormal = slopeHit.normal;
                return angle < slopeAngle && angle != 0;
            }

            m_slopeNormal = Vector3.zero;
            return false;
        }

        private Tuple<bool, Vector3> CheckWall()
        {
            bool tmpWall = false;
            Vector3 tmpWallNormal = Vector3.zero;
            Vector3 position = transform.position + (Vector3.up * wallHeight);

            //Vector3 topWallPos = new Vector3(transform.position.x, transform.position.y + wallHeight, transform.position.z);
            RaycastHit wallHit;
            for (int i = 0; i < 8; i++)
            {
                if (Physics.Raycast(position, Quaternion.AngleAxis(i * 45, transform.up) * Vector3.forward, out wallHit, wallDistance, m_groundMask))
                {
                    tmpWallNormal = wallHit.normal;
                    tmpWall = true;
                }
            }

            return new Tuple<bool, Vector3>(tmpWall, tmpWallNormal);
        }
        #endregion

        private void CalculateMovement()
        {
            if (isGrounded)
            {
                m_collider.material.staticFriction = m_staticFriction;
                m_collider.material.dynamicFriction = m_dynamicFriction;
                m_collider.material.frictionCombine = m_frictionCombine;

                m_rigidbody.maxLinearVelocity = Mathf.Infinity;
                m_speed = speed * Mathf.Clamp01(speedFactor);
                m_direction = RelativeTo(m_movementDirection, m_camera, true);
                m_direction = isSloped ? ProjectOnPlane(m_direction) : m_direction;
                m_direction *= m_speed;

                if(m_direction.magnitude > threshold)
                {
                    switch (mode)
                    {
                        case MovementMode.Velocity: 
                            m_rigidbody.velocity = Vector3.SmoothDamp(m_rigidbody.velocity, m_direction, ref m_velocity, dampSpeedUp);
                            break;
                        case MovementMode.Force:
                            Vector3 Force = Vector3.zero;
                            //normalize the current body velocity
                            Vector3 velNorm = m_rigidbody.velocity.normalized;
                            //dot of the goal velocirty vs normalized velocity
                            float velDot = Vector3.Dot(velNorm, m_movementDirection.normalized);

                            Vector3 groundVel = m_groundRigidbody ? m_groundRigidbody.velocity : Vector3.zero;

                            float accel = acceleration * accelerationFactorFromDot.Evaluate(velDot);

                            //calculate new goal vel
                            Vector3 goalVel = m_direction;

                            m_velocity = Vector3.MoveTowards(m_velocity, goalVel + groundVel, accel * Time.fixedDeltaTime);

                            //Calculate and clamp acceleration vector length
                            m_aceleration = (m_velocity - m_rigidbody.velocity) / Time.fixedDeltaTime;

                            float MaxAccel = maxAccelerationForce * maxAccelerationForceFactorFromDot.Evaluate(velDot) * maxAccelerationFactor;

                            m_aceleration = Vector3.ClampMagnitude(m_aceleration, MaxAccel);
                            m_rigidbody.AddForce(m_aceleration, ForceMode.Acceleration);
                            
                            break;
                        default: break;
                    }
                }
                else
                {
                    switch (mode)
                    {
                        case MovementMode.Velocity:
                                m_rigidbody.velocity = Vector3.SmoothDamp(m_rigidbody.velocity, Vector3.zero, ref m_velocity, dampSpeedDown);
                            break;
                        case MovementMode.Force:
                            break;
                        default: break;
                    }

                    if(mode == MovementMode.Velocity)
                    {
                        //m_rigidbody.AddForce(-m_direction, ForceMode.Force);
                    }

                    if (isSloped)
                    {
                        if (slopeLock)
                        { 
                            m_rigidbody.maxLinearVelocity = 0;
                        }
                        else
                        {
                            Vector3 gravityProject = Vector3.ProjectOnPlane(Physics.gravity, m_slopeNormal);
                            m_rigidbody.AddForce(gravityProject * slopeGravityMultiplier, ForceMode.Acceleration);
                            m_collider.material.staticFriction = 0;
                            m_collider.material.dynamicFriction = 0;
                            m_collider.material.frictionCombine = PhysicMaterialCombine.Multiply;
                        }
                    }
                }

            }
        }

        public void Move(Vector3 value)
        {
            m_movementDirection = new Vector3(value.x, 0, value.y);
            m_movementDirection.Normalize();
        }

        public void Jump()
        {
            var tmpGroundDistance = groundDistance;
            groundDistance = 0;
            StartCoroutine(StartJump(tmpGroundDistance));
        }

        private IEnumerator StartJump(float distance)
        {
            yield return new WaitForSeconds(0.1f);
            isJumping = true;
            m_rigidbody.AddForce(transform.up * jumpForce, ForceMode.Impulse);
            StartCoroutine(EndJump(distance));
        }

        private IEnumerator EndJump(float distance)
        {
            yield return new WaitForEndOfFrame();
            groundDistance = distance;
        }

        #region INTERNAL
        private Vector3 RelativeTo(Vector3 vector3, Transform relativeToThis, bool isPlanar = true)
        {
            Vector3 forward = relativeToThis.forward;
            if (isPlanar)
            {
                forward = Vector3.ProjectOnPlane(forward, Vector3.up);

                if (forward.sqrMagnitude < 9.99999943962493E-11)
                    forward = Vector3.ProjectOnPlane(relativeToThis.up, Vector3.up);
            }

            Quaternion rotation = Quaternion.LookRotation(forward);
            return rotation * vector3;
        }

        private Vector3 ProjectOnPlane(Vector3 value)
        {
            return Vector3.ProjectOnPlane(value, m_slopeNormal);
        }

        private float RoundValue(float _value)
        {
            float unit = (float)Mathf.Round(_value);

            if (_value - unit < 0.000001f && _value - unit > -0.000001f) return unit;
            else return _value;
        }

        #endregion

        #region GIZMOS
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;
            Vector3 position = transform.position + (Vector3.up * wallHeight);
            RaycastHit wallHit;
            for (int i = 0; i < 8; i++)
            {
                Gizmos.DrawLine(position, position + Quaternion.AngleAxis(i * 45, transform.up) * Vector3.forward * wallDistance);
                if (Physics.Raycast(position, Quaternion.AngleAxis(i * 45, transform.up) * Vector3.forward, out wallHit, wallDistance, m_groundMask))
                {
                    Gizmos.DrawCube(wallHit.point, Vector3.one * 0.15f);
                }
            }

            RaycastHit groundHit;
            if(Physics.SphereCast(transform.position,groundRadius, groundDirection, out groundHit, groundDistance, m_groundMask))
            {
                Gizmos.DrawSphere(groundHit.point, groundRadius);
            }

            //Step
            Gizmos.color = Color.red;
            bool tmpStep = false;
            Vector3 bottomStepPos = transform.position - (groundDirection * groundDistance) + new Vector3(0f, 0.05f, 0f);

            RaycastHit stepLowerHit;
            if (Physics.Raycast(bottomStepPos, Vector3.forward, out stepLowerHit, stepOffset, m_groundMask))
            {
                RaycastHit stepUpperHit;
                if (RoundValue(stepLowerHit.normal.y) == 0 && !Physics.Raycast(bottomStepPos + new Vector3(0f, stepHeight, 0f), Vector3.forward, out stepUpperHit, stepDistance + 0.05f, m_groundMask))
                {
                    //rigidbody.position -= new Vector3(0f, -stepSmooth, 0f);
                    Gizmos.DrawSphere(stepUpperHit.point, 0.1f);
                    tmpStep = true;
                }
            }

            RaycastHit stepLowerHit45;
            if (Physics.Raycast(bottomStepPos, Quaternion.AngleAxis(45, transform.up) * Vector3.forward, out stepLowerHit45, stepOffset, m_groundMask))
            {
                RaycastHit stepUpperHit45;
                if (RoundValue(stepLowerHit45.normal.y) == 0 && !Physics.Raycast(bottomStepPos + new Vector3(0f, stepHeight, 0f), Quaternion.AngleAxis(45, Vector3.up) * Vector3.forward, out stepUpperHit45, stepDistance + 0.05f, m_groundMask))
                {
                    //rigidbody.position -= new Vector3(0f, -stepSmooth, 0f);
                    Gizmos.DrawSphere(stepUpperHit45.point, 0.1f);
                    tmpStep = true;
                }
            }

            RaycastHit stepLowerHitMinus45;
            if (Physics.Raycast(bottomStepPos, Quaternion.AngleAxis(-45, transform.up) * Vector3.forward, out stepLowerHitMinus45, stepOffset, m_groundMask))
            {
                RaycastHit stepUpperHitMinus45;
                if (RoundValue(stepLowerHitMinus45.normal.y) == 0 && !Physics.Raycast(bottomStepPos + new Vector3(0f, stepHeight, 0f), Quaternion.AngleAxis(-45, Vector3.up) * Vector3.forward, out stepUpperHitMinus45, stepDistance + 0.05f, m_groundMask))
                {
                    Gizmos.DrawSphere(stepLowerHitMinus45.point, 0.1f);
                    //rigidbody.position -= new Vector3(0f, -stepSmooth, 0f);
                    tmpStep = true;
                }
            }
        }

        #endregion

    }
    /*

            //movement
            public float speed = 14f;
            public float speedFactor = 1f;
            public AnimationCurve speedMultiplierOnAngle = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            public float threshold = 0.2f;

            public float dampSpeedUp = 0.2f;
            public float dampSpeedDown = 0.1f;

            //jump
            public bool canLongJump;
            public float jumpVelocity = 20f;
            public float jumpFallMultiplier = 1.7f;
            public float jumpHoldMultiplier = 5f;
            private float coyoteJumpMultiplier = 1f;

            //sprint
            public float sprintSpeed = 20f;

            public float crouchSpeedMultiplier;

            public float frictionWall;
            public float frictionGround;

            public Vector3 groundDirectionCheck = Vector3.down;
            public float groundDirectionLength = 1f;
            public float groundCheckerThreshold = 0.1f;
            public float slopeCheckerThreshold = 0.51f;
            public float stepCheckerThreshold = 0.6f;

            public float maxClimbableSlopeAngle = 53.6f;
            public float maxStepHeight = 0.74f;


            public float canSlideMultiplierCurve = 0.061f;
            public float cantSlideMultiplierCurve = 0.039f;
            public float climbingStairsMultiplierCurve = 0.637f;

            public float gravityMultiplier = 6f;
            public float gravityMultiplyerOnSlideChange = 3f;
            public float gravityMultiplierIfUnclimbableSlope = 30f;

            public bool lockOnSlope = false;


            public float wallCheckerThrashold = 0.8f;
            public float hightWallCheckerChecker = 0.5f;
            public float jumpFromWallMultiplier = 30f;
            public float multiplierVerticalLeap = 1f;

            public float crouchHeightMultiplier = 0.5f;
            public Vector3 POV_normalHeadHeight = new Vector3(0f, 0.5f, -0.1f);
            public Vector3 POV_crouchHeadHeight = new Vector3(0f, -0.1f, -0.1f);


            public GameObject characterCamera;
            public GameObject characterModel;
            public float characterModelRotationSmooth = 0.1f;

            public float targetAngle;

            public Vector2 axisInput;


            //Getters
            public bool isGrounded { private set; get; }
            public bool isCrouch { private set; get; }
            public bool isJumping { private set; get; }


            //private if stataments
            private bool m_isTouchingStep = false;
            private bool m_isTouchingWall = false;
            private bool m_isTouchingSlope = false;
            //private
            [SerializeField][HideInInspector] private LayerMask m_groundMask;
            private bool m_prevGround;
            private bool m_lockOnSlope;
            private bool m_lockRotation;
            private bool m_crouch;
            private bool m_sprint;
            private bool m_jump;
            private bool m_jumpHold;

            private Vector3 m_globalForward;
            private Vector3 m_wallNormal;
            private Vector3 m_prevGroundNormal;
            private Vector3 m_groundNormal;
            private Vector3 m_forward;
            private Vector3 m_reactionForward;
            private Vector3 m_down;
            private Vector3 m_globalDown;
            private Vector3 m_reactionGlobalDown;
            private Vector3 m_currVelocity;

            private float m_currentSurfaceAngle;
            private float m_originalColliderHeight;
            private float m_turnSmoothVelocity;

            //protected
            protected Rigidbody m_body;
            protected CapsuleCollider m_collider;

            protected virtual void Awake()
            {
                m_body = GetComponent<Rigidbody>();
                m_collider = GetComponent<CapsuleCollider>();
                if (m_collider)
                {
                    m_originalColliderHeight = m_collider.height;
                }

                m_lockOnSlope = lockOnSlope;


            }

            protected virtual void Start()
            {
                this.characterCamera = Camera.main.gameObject;
            }

            protected virtual void FixedUpdate()
            {
                //local
                CheckGround();
                CheckStep();
                CheckWall();
                CheckSlopeAndDirections();

                //movement
                MoveCrouch();
                MoveWalk();
                MoveRotation();
                MoveJump();

                //gravity
                ApplyGravity();

                //events
            }

            #region CHECKS

            private void CheckGround()
            {
                m_prevGround = isGrounded;
                isGrounded = Physics.CheckSphere(transform.position + groundDirectionCheck, groundCheckerThreshold, m_groundMask);
            }

            private void CheckStep()
            {
                bool tmpStep = false;
                Vector3 bottomStepPos = transform.position - (groundDirectionCheck * groundCheckerThreshold) + new Vector3(0f, 0.05f, 0f);

                RaycastHit stepLowerHit;
                if (Physics.Raycast(bottomStepPos, m_globalForward, out stepLowerHit, stepCheckerThreshold, m_groundMask))
                {
                    RaycastHit stepUpperHit;
                    if (RoundValue(stepLowerHit.normal.y) == 0 && !Physics.Raycast(bottomStepPos + new Vector3(0f, maxStepHeight, 0f), m_globalForward, out stepUpperHit, stepCheckerThreshold + 0.05f, m_groundMask))
                    {
                        //rigidbody.position -= new Vector3(0f, -stepSmooth, 0f);
                        tmpStep = true;
                    }
                }

                RaycastHit stepLowerHit45;
                if (Physics.Raycast(bottomStepPos, Quaternion.AngleAxis(45, transform.up) * m_globalForward, out stepLowerHit45, stepCheckerThreshold, m_groundMask))
                {
                    RaycastHit stepUpperHit45;
                    if (RoundValue(stepLowerHit45.normal.y) == 0 && !Physics.Raycast(bottomStepPos + new Vector3(0f, maxStepHeight, 0f), Quaternion.AngleAxis(45, Vector3.up) * m_globalForward, out stepUpperHit45, stepCheckerThreshold + 0.05f, m_groundMask))
                    {
                        //rigidbody.position -= new Vector3(0f, -stepSmooth, 0f);
                        tmpStep = true;
                    }
                }

                RaycastHit stepLowerHitMinus45;
                if (Physics.Raycast(bottomStepPos, Quaternion.AngleAxis(-45, transform.up) * m_globalForward, out stepLowerHitMinus45, stepCheckerThreshold, m_groundMask))
                {
                    RaycastHit stepUpperHitMinus45;
                    if (RoundValue(stepLowerHitMinus45.normal.y) == 0 && !Physics.Raycast(bottomStepPos + new Vector3(0f, maxStepHeight, 0f), Quaternion.AngleAxis(-45, Vector3.up) * m_globalForward, out stepUpperHitMinus45, stepCheckerThreshold + 0.05f, m_groundMask))
                    {
                        //rigidbody.position -= new Vector3(0f, -stepSmooth, 0f);
                        tmpStep = true;
                    }
                }

                m_isTouchingStep = tmpStep;
            }

            private void CheckWall()
            {
                bool tmpWall = false;
                Vector3 tmpWallNormal = Vector3.zero;
                Vector3 topWallPos = new Vector3(transform.position.x, transform.position.y + hightWallCheckerChecker, transform.position.z);

                RaycastHit wallHit;
               *//* 
                if (Physics.Raycast(topWallPos, m_globalForward, out wallHit, wallCheckerThrashold, m_groundMask))
                {
                    tmpWallNormal = wallHit.normal;
                    tmpWall = true;
                }
                else if (Physics.Raycast(topWallPos, Quaternion.AngleAxis(45, transform.up) * m_globalForward, out wallHit, wallCheckerThrashold, m_groundMask))
                {
                    tmpWallNormal = wallHit.normal;
                    tmpWall = true;
                }
                else if (Physics.Raycast(topWallPos, Quaternion.AngleAxis(90, transform.up) * m_globalForward, out wallHit, wallCheckerThrashold, m_groundMask))
                {
                    tmpWallNormal = wallHit.normal;
                    tmpWall = true;
                }
                else if (Physics.Raycast(topWallPos, Quaternion.AngleAxis(135, transform.up) * m_globalForward, out wallHit, wallCheckerThrashold, m_groundMask))
                {
                    tmpWallNormal = wallHit.normal;
                    tmpWall = true;
                }
                else if (Physics.Raycast(topWallPos, Quaternion.AngleAxis(180, transform.up) * m_globalForward, out wallHit, wallCheckerThrashold, m_groundMask))
                {
                    tmpWallNormal = wallHit.normal;
                    tmpWall = true;
                }
                else if (Physics.Raycast(topWallPos, Quaternion.AngleAxis(225, transform.up) * m_globalForward, out wallHit, wallCheckerThrashold, m_groundMask))
                {
                    tmpWallNormal = wallHit.normal;
                    tmpWall = true;
                }
                else if (Physics.Raycast(topWallPos, Quaternion.AngleAxis(270, transform.up) * m_globalForward, out wallHit, wallCheckerThrashold, m_groundMask))
                {
                    tmpWallNormal = wallHit.normal;
                    tmpWall = true;
                }
                else if (Physics.Raycast(topWallPos, Quaternion.AngleAxis(315, transform.up) * m_globalForward, out wallHit, wallCheckerThrashold, m_groundMask))
                {
                    tmpWallNormal = wallHit.normal;
                    tmpWall = true;
                }*//*

                for (int i = 0; i < 8; i++)
                {
                    if (Physics.Raycast(topWallPos, Quaternion.AngleAxis(i * 45, transform.up) * m_globalForward, out wallHit, wallCheckerThrashold, m_groundMask))
                    {
                        tmpWallNormal = wallHit.normal;
                        tmpWall = true;
                    }
                }

                m_isTouchingWall = tmpWall;
                m_wallNormal = tmpWallNormal;
            }

            private void CheckSlopeAndDirections()
            {
                m_prevGroundNormal = m_groundNormal;

                RaycastHit slopeHit;
                if (Physics.SphereCast(transform.position, slopeCheckerThreshold, Vector3.down, out slopeHit, groundDirectionLength, m_groundMask))
                {
                    m_groundNormal = slopeHit.normal;

                    if (slopeHit.normal.y == 1)
                    {

                        m_forward = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                        m_globalForward = m_forward;
                        m_reactionForward = m_forward;

                        //SetFriction(frictionAgainstFloor, true);
                        m_lockOnSlope = lockOnSlope;

                        m_currentSurfaceAngle = 0f;
                        m_isTouchingSlope = false;

                    }
                    else
                    {
                        //set forward
                        Vector3 tmpGlobalForward = transform.forward.normalized;
                        Vector3 tmpForward = new Vector3(tmpGlobalForward.x, Vector3.ProjectOnPlane(transform.forward.normalized, slopeHit.normal).normalized.y, tmpGlobalForward.z);
                        Vector3 tmpReactionForward = new Vector3(tmpForward.x, tmpGlobalForward.y - tmpForward.y, tmpForward.z);

                        if (m_currentSurfaceAngle <= maxClimbableSlopeAngle && !m_isTouchingStep)
                        {
                            //set forward
                            m_forward = tmpForward * ((speedMultiplierOnAngle.Evaluate(m_currentSurfaceAngle / 90f) * canSlideMultiplierCurve) + 1f);
                            m_globalForward = tmpGlobalForward * ((speedMultiplierOnAngle.Evaluate(m_currentSurfaceAngle / 90f) * canSlideMultiplierCurve) + 1f);
                            m_reactionForward = tmpReactionForward * ((speedMultiplierOnAngle.Evaluate(m_currentSurfaceAngle / 90f) * canSlideMultiplierCurve) + 1f);

                            //SetFriction(frictionAgainstFloor, true);
                            m_lockOnSlope = lockOnSlope;
                        }
                        else if (m_isTouchingStep)
                        {
                            //set forward
                            m_forward = tmpForward * ((speedMultiplierOnAngle.Evaluate(m_currentSurfaceAngle / 90f) * climbingStairsMultiplierCurve) + 1f);
                            m_globalForward = tmpGlobalForward * ((speedMultiplierOnAngle.Evaluate(m_currentSurfaceAngle / 90f) * climbingStairsMultiplierCurve) + 1f);
                            m_reactionForward = tmpReactionForward * ((speedMultiplierOnAngle.Evaluate(m_currentSurfaceAngle / 90f) * climbingStairsMultiplierCurve) + 1f);

                            //SetFriction(frictionAgainstFloor, true);
                            m_lockOnSlope = true;
                        }
                        else
                        {
                            //set forward
                            m_forward = tmpForward * ((speedMultiplierOnAngle.Evaluate(m_currentSurfaceAngle / 90f) * cantSlideMultiplierCurve) + 1f);
                            m_globalForward = tmpGlobalForward * ((speedMultiplierOnAngle.Evaluate(m_currentSurfaceAngle / 90f) * cantSlideMultiplierCurve) + 1f);
                            m_reactionForward = tmpReactionForward * ((speedMultiplierOnAngle.Evaluate(m_currentSurfaceAngle / 90f) * cantSlideMultiplierCurve) + 1f);

                            //SetFriction(0f, true);
                            m_lockOnSlope = lockOnSlope;
                        }

                        m_currentSurfaceAngle = Vector3.Angle(Vector3.up, slopeHit.normal);
                        m_isTouchingSlope = true;
                    }

                    //set down
                    m_down = Vector3.Project(Vector3.down, slopeHit.normal);
                    m_globalDown = Vector3.down.normalized;
                    m_reactionGlobalDown = Vector3.up.normalized;
                }
                else
                {
                    m_groundNormal = Vector3.zero;

                    m_forward = Vector3.ProjectOnPlane(transform.forward, slopeHit.normal).normalized;
                    m_globalForward = m_forward;
                    m_reactionForward = m_forward;

                    //set down
                    m_down = Vector3.down.normalized;
                    m_globalDown = Vector3.down.normalized;
                    m_reactionGlobalDown = Vector3.up.normalized;

                    //SetFriction(frictionAgainstFloor, true);
                    m_lockOnSlope = lockOnSlope;
                }
            }

            #endregion

            #region MOVEMENT

            private void MoveCrouch()
            {
                if (m_crouch && isGrounded)
                {
                    isCrouch = true;
                    *//*if (meshCharacterCrouch != null && meshCharacter != null) meshCharacter.SetActive(false);
                    if (meshCharacterCrouch != null) meshCharacterCrouch.SetActive(true);
    *//*
                    float newHeight = m_originalColliderHeight * crouchHeightMultiplier;
                    m_collider.height = newHeight;
                    m_collider.center = new Vector3(0f, -newHeight * crouchHeightMultiplier, 0f);

                    //headPoint.position = new Vector3(transform.position.x + POV_crouchHeadHeight.x, transform.position.y + POV_crouchHeadHeight.y, transform.position.z + POV_crouchHeadHeight.z);
                }
                else
                {
                    isCrouch = false;
                    *//*if (meshCharacterCrouch != null && meshCharacter != null) meshCharacter.SetActive(true);
                    if (meshCharacterCrouch != null) meshCharacterCrouch.SetActive(false);*//*

                    m_collider.height = m_originalColliderHeight;
                    m_collider.center = Vector3.zero;

                    //headPoint.position = new Vector3(transform.position.x + POV_normalHeadHeight.x, transform.position.y + POV_normalHeadHeight.y, transform.position.z + POV_normalHeadHeight.z);
                }
            }


            private void MoveWalk()
            {
                float crouchMultiplier = 1f;
                if (isCrouch) crouchMultiplier = crouchSpeedMultiplier;

                if (axisInput.magnitude > threshold)
                {
                    targetAngle = Mathf.Atan2(axisInput.x, axisInput.y) * Mathf.Rad2Deg + characterCamera.transform.eulerAngles.y;


                    if (!m_sprint)
                        m_body.velocity = Vector3.SmoothDamp(m_body.velocity, m_forward * speed * crouchMultiplier, ref m_currVelocity, dampSpeedUp);
                    else
                        m_body.velocity = Vector3.SmoothDamp(m_body.velocity, m_forward * sprintSpeed * crouchMultiplier, ref m_currVelocity, dampSpeedUp);
                }
                else
                    m_body.velocity = Vector3.SmoothDamp(m_body.velocity, Vector3.zero * crouchMultiplier, ref m_currVelocity, dampSpeedDown);
            }


            private void MoveRotation()
            {
                //charactermodel.transform.eulerAngles.z
                float angle = Mathf.SmoothDampAngle(characterModel.transform.eulerAngles.y, targetAngle, ref m_turnSmoothVelocity, characterModelRotationSmooth);
                characterModel.transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);

                if (!m_lockRotation) characterModel.transform.rotation = Quaternion.Euler(0f, angle, 0f);
                else
                {
                    var lookPos = -m_wallNormal;
                    lookPos.y = 0;
                    var rotation = Quaternion.LookRotation(lookPos);
                    characterModel.transform.rotation = rotation;
                }
            }


            private void MoveJump()
            {
                //jumped
                if (m_jump && isGrounded && ((m_isTouchingSlope && m_currentSurfaceAngle <= maxClimbableSlopeAngle) || !m_isTouchingSlope) && !m_isTouchingWall)
                {
                    m_body.velocity += Vector3.up * jumpVelocity;
                    isJumping = true;
                }
                //jumped from wall
                else if (m_jump && !isGrounded && m_isTouchingWall)
                {
                    m_body.velocity += m_wallNormal * jumpFromWallMultiplier + (Vector3.up * jumpFromWallMultiplier) * multiplierVerticalLeap;
                    isJumping = true;

                    targetAngle = Mathf.Atan2(m_wallNormal.x, m_wallNormal.z) * Mathf.Rad2Deg;

                    m_forward = m_wallNormal;
                    m_globalForward = m_forward;
                    m_reactionForward = m_forward;
                }

                //is falling
                if (m_body.velocity.y < 0 && !isGrounded) coyoteJumpMultiplier = jumpFallMultiplier;
                else if (m_body.velocity.y > 0.1f && (m_currentSurfaceAngle <= maxClimbableSlopeAngle || m_isTouchingStep))
                {
                    //is short jumping
                    if (!m_jumpHold || !canLongJump) coyoteJumpMultiplier = 1f;
                    //is long jumping
                    else coyoteJumpMultiplier = 1f / jumpHoldMultiplier;
                }
                else
                {
                    isJumping = false;
                    coyoteJumpMultiplier = 1f;
                }
            }


            #endregion

            #region GRAVITY

            private void ApplyGravity()
            {
                Vector3 gravity = Vector3.zero;

                if (m_lockOnSlope || m_isTouchingStep) gravity = m_down * gravityMultiplier * -Physics.gravity.y * coyoteJumpMultiplier;
                else gravity = m_globalDown * gravityMultiplier * -Physics.gravity.y * coyoteJumpMultiplier;

                //avoid little jump
                if (m_groundNormal.y != 1 && m_groundNormal.y != 0 && m_isTouchingSlope && m_prevGroundNormal != m_groundNormal)
                {
                    //Debug.Log("Added correction jump on slope");
                    gravity *= gravityMultiplyerOnSlideChange;
                }

                //slide if angle too big
                if (m_groundNormal.y != 1 && m_groundNormal.y != 0 && (m_currentSurfaceAngle > maxClimbableSlopeAngle && !m_isTouchingStep))
                {
                    //Debug.Log("Slope angle too high, character is sliding");
                    if (m_currentSurfaceAngle > 0f && m_currentSurfaceAngle <= 30f) 
                        gravity = m_globalDown * gravityMultiplierIfUnclimbableSlope * -Physics.gravity.y;
                    else if (m_currentSurfaceAngle > 30f && m_currentSurfaceAngle <= 89f) 
                        gravity = m_globalDown * gravityMultiplierIfUnclimbableSlope / 2f * -Physics.gravity.y;
                }

                //friction when touching wall
                if (m_isTouchingWall && m_body.velocity.y < 0) gravity *= frictionWall;

                m_body.AddForce(gravity);
            }

            #endregion

            #region FUNCTIONS

            public void Jump()
            {

            }

            public void Move(Vector3 input)
            {
                this.axisInput = input;
            }

            #endregion

            private float RoundValue(float _value)
            {
                float unit = (float)Mathf.Round(_value);

                if (_value - unit < 0.000001f && _value - unit > -0.000001f) return unit;
                else return _value;
            }

            #region DEBUG 
            [SerializeField][HideInInspector] private bool m_debug;

            private void OnDrawGizmos()
            {
                if (m_debug)
                {
                    m_body = this.GetComponent<Rigidbody>();
                    m_collider = this.GetComponent<CapsuleCollider>();

                    Vector3 bottomStepPos = transform.position - new Vector3(0f, m_originalColliderHeight / 2f, 0f) + new Vector3(0f, 0.05f, 0f);
                    Vector3 topWallPos = new Vector3(transform.position.x, transform.position.y + hightWallCheckerChecker, transform.position.z);

                    //ground and slope
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireSphere(transform.position - new Vector3(0, m_originalColliderHeight / 2f, 0), groundCheckerThreshold);

                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(transform.position - new Vector3(0, m_originalColliderHeight / 2f, 0), slopeCheckerThreshold);

                    //direction
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(transform.position, transform.position + m_forward * 2f);

                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(transform.position, transform.position + m_globalForward * 2);

                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(transform.position, transform.position + m_reactionForward * 2f);

                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(transform.position, transform.position + m_down * 2f);

                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(transform.position, transform.position + m_globalDown * 2f);

                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(transform.position, transform.position + m_reactionGlobalDown * 2f);

                    //step check
                    Gizmos.color = Color.black;
                    Gizmos.DrawLine(bottomStepPos, bottomStepPos + m_globalForward * stepCheckerThreshold);

                    Gizmos.color = Color.black;
                    Gizmos.DrawLine(bottomStepPos + new Vector3(0f, maxStepHeight, 0f), bottomStepPos + new Vector3(0f, maxStepHeight, 0f) + m_globalForward * (stepCheckerThreshold + 0.05f));

                    Gizmos.color = Color.black;
                    Gizmos.DrawLine(bottomStepPos, bottomStepPos + Quaternion.AngleAxis(45, transform.up) * (m_globalForward * stepCheckerThreshold));

                    Gizmos.color = Color.black;
                    Gizmos.DrawLine(bottomStepPos + new Vector3(0f, maxStepHeight, 0f), bottomStepPos + Quaternion.AngleAxis(45, Vector3.up) * (m_globalForward * stepCheckerThreshold) + new Vector3(0f, maxStepHeight, 0f));

                    Gizmos.color = Color.black;
                    Gizmos.DrawLine(bottomStepPos, bottomStepPos + Quaternion.AngleAxis(-45, transform.up) * (m_globalForward * stepCheckerThreshold));

                    Gizmos.color = Color.black;
                    Gizmos.DrawLine(bottomStepPos + new Vector3(0f, maxStepHeight, 0f), bottomStepPos + Quaternion.AngleAxis(-45, Vector3.up) * (m_globalForward * stepCheckerThreshold) + new Vector3(0f, maxStepHeight, 0f));

                    for (int i = 0; i < 8; i++)
                    {
                        Gizmos.DrawLine(topWallPos, topWallPos + Quaternion.AngleAxis(45*i, transform.up) * (m_globalForward * wallCheckerThrashold));
                    }

                    //wall check
                    *//*Gizmos.color = Color.black;
                    Gizmos.DrawLine(topWallPos, topWallPos + m_globalForward * wallCheckerThrashold);

                    Gizmos.color = Color.black;
                    Gizmos.DrawLine(topWallPos, topWallPos + Quaternion.AngleAxis(45, transform.up) * (m_globalForward * wallCheckerThrashold));

                    Gizmos.color = Color.black;
                    Gizmos.DrawLine(topWallPos, topWallPos + Quaternion.AngleAxis(90, transform.up) * (m_globalForward * wallCheckerThrashold));

                    Gizmos.color = Color.black;
                    Gizmos.DrawLine(topWallPos, topWallPos + Quaternion.AngleAxis(135, transform.up) * (m_globalForward * wallCheckerThrashold));

                    Gizmos.color = Color.black;
                    Gizmos.DrawLine(topWallPos, topWallPos + Quaternion.AngleAxis(180, transform.up) * (m_globalForward * wallCheckerThrashold));

                    Gizmos.color = Color.black;
                    Gizmos.DrawLine(topWallPos, topWallPos + Quaternion.AngleAxis(225, transform.up) * (m_globalForward * wallCheckerThrashold));

                    Gizmos.color = Color.black;
                    Gizmos.DrawLine(topWallPos, topWallPos + Quaternion.AngleAxis(270, transform.up) * (m_globalForward * wallCheckerThrashold));

                    Gizmos.color = Color.black;
                    Gizmos.DrawLine(topWallPos, topWallPos + Quaternion.AngleAxis(315, transform.up) * (m_globalForward * wallCheckerThrashold));*//*
                }
            }
            #endregion*/
}
