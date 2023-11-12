using log4net.Filter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameEngine
{

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class CharacterController : MonoBehaviour
    {
        //movement
        [HideInInspector] public float speed = 10f;
        [HideInInspector] public float speedFactor = 1f;
        [HideInInspector] public float threshold = 0.15f;
        [HideInInspector] public float airControl = 0f;
        [HideInInspector] public float dampSpeedUp = 0.2f;
        [HideInInspector] public float dampSpeedDown = 0.1f;
        //ground          
        [HideInInspector] public bool groundForce;
        [HideInInspector] public float groundSpringForce = 1f;
        [HideInInspector] public float groundSpringDamper = 1;
        [HideInInspector] public float groundRadius = 0.15f;
        [HideInInspector] public float groundDistance = 1f;
        [HideInInspector] public Vector3 groundDirection = Vector3.down;
        [HideInInspector] public ForceMode groundForceMode = ForceMode.Force;
        //slope           
        [HideInInspector] public float slopeAngle = 55f;
        [HideInInspector] public float slopeGravityMultiplier = 6f;
        [HideInInspector] public float slopeDistance = 1.0f;
        [HideInInspector] public float slopeRadius = 0.1f;
        [HideInInspector] public bool slopeLock = true;
        //step            
        [HideInInspector] public float stepOffset = 0.1f;
        [HideInInspector] public float stepHeight = 0.2f;
        [HideInInspector] public float stepDistance = 0.15f;
        [HideInInspector] public float stepSpringForce = 10f;
        [HideInInspector] public float stepSpringDamp = 1f;
        [HideInInspector] public float stepSpringMin = 0f;
        [HideInInspector] public float stepSpringMax = 2f;
        [HideInInspector] public uint stepIteration = 10;
        [HideInInspector] public uint stepIterationThreshold = 20;
        //jump
        [HideInInspector] public bool jump = true;
        [HideInInspector] public float jumpForce = 10f;
        [HideInInspector] public int jumpCount = 1;
        [HideInInspector] public float jumpDelay = 0.1f;
        [HideInInspector] public float jumpMemory = 0f;
        //wall            
        [HideInInspector] public float wallHeight = 0f;
        [HideInInspector] public float wallDistance = 1f;
        [HideInInspector] public new Transform camera = null;

        //getters & setters
        public bool isGrounded { get; private set; }
        public bool isSloped { get; private set; }
        public bool isJumping { get; private set; }
        public bool isTouchingWall { get; private set; }
        public bool isTouchingStep { get; private set; }
        public Vector3 direction { get { return m_direction.normalized; } }
        public Vector3 velocity { get { return m_velocity; } }

        //private
        [SerializeField][HideInInspector] private LayerMask m_slopeMask;
        [SerializeField][HideInInspector] private LayerMask m_groundMask;
        private Rigidbody m_rigidbody;
        private CapsuleCollider m_collider;
        private Transform m_camera;
        private Vector3 m_velocity;
        private Vector3 m_lastVelocity;
        private Vector3 m_direction;
        private Vector3 m_lastDirection;
        private Vector3 m_movementDirection;
        private Vector3 m_stepHit;
        private Vector3 m_slopeNormal;
        private Vector3 m_stepProyectedDirection;
        private float m_speed;
        private float m_slopeAngle;
        private float m_staticFriction;
        private float m_dynamicFriction;
        private float m_groundDistance;
        private float m_stepHitDistance;
        private bool m_isStepUpGroundForce;
        private bool m_isColliding;
        private int m_jumpCount;
        private int m_jumpMemoryState = -1;
        private int m_stepUpState = -1;
        private int m_stepHitCount = 0;
        private int m_stepIterationCount = 0;
        private PhysicMaterialCombine m_frictionCombine;

        protected virtual void Awake()
        {
            m_rigidbody = GetComponent<Rigidbody>();
            m_collider = GetComponent<CapsuleCollider>();
            if (m_collider)
            {
                m_staticFriction = m_collider.material.staticFriction;
                m_dynamicFriction = m_collider.material.dynamicFriction;
                m_frictionCombine = m_collider.material.frictionCombine;
            }
            m_camera = camera ? camera : Camera.main.transform;
            m_groundDistance = groundDistance;
            m_rigidbody.interpolation = RigidbodyInterpolation.None;
        }

        protected virtual void Update()
        {
            Vector3 wallNormal;

            //Checks
            isGrounded = CheckGround();
            isSloped = CheckSlopeAngle();
            isTouchingWall = CheckWall(out wallNormal);
            isTouchingStep = CheckStep(50);
        }

        protected virtual void FixedUpdate()
        {
            CalculateMovement();
            CalculateGroundForce();
            CalculateStepUpForce();
        }

        #region COLLISION
        protected virtual void OnCollisionEnter(Collision collision)
        {
            m_isColliding = true;

            if (m_stepUpState > 1)
            {
                m_isStepUpGroundForce = true;
            }
        }

        protected virtual void OnCollisionStay(Collision collision)
        {
            if (m_stepUpState > 1)
            {
                m_isStepUpGroundForce = true;
            }

            if(m_isColliding && isGrounded && airControl > 0)
            {

            }
        }

        protected virtual void OnCollisionExit(Collision collision)
        {
            m_isColliding = false;
            m_isStepUpGroundForce = false;
            /*
            if (m_stepUpState < 0)
            {
                m_isStepUpGroundForce = false;
            }*/
        }
        #endregion

        #region CHECKS
        private bool CheckStep(int stepAngle)
        {
            Vector3 position, direction;
            Vector3 forward = transform.forward;

            m_stepHitCount = 0;
            m_stepUpState = -1;

            RaycastHit stepHit;
            for (int i = -1; i < 2; i++)
            {
                position = transform.position + Vector3.up * stepOffset;
                direction = Quaternion.AngleAxis(stepAngle * i, transform.up) * forward;

                stepDistance = stepDistance != 0 ? stepDistance : 0.1f;
                if (Physics.Raycast(position, direction, out stepHit, stepDistance, m_groundMask))
                {
                    RaycastHit hit;
                    m_stepProyectedDirection = GetStepUpDirection();
                    m_stepHitDistance = stepHit.distance;

                    if (RoundValue(stepHit.normal.y) == 0 && !Physics.Raycast(position + Vector3.up * stepHeight, direction, out hit, stepDistance, m_groundMask))
                    {
                        float angle = Vector3.Angle(forward, m_movementDirection);

                        if (m_movementDirection.magnitude != 0 && angle <= stepAngle)
                        {
                            //m_rigidbody.position -= new Vector3(0, -stepSmooth * Time.deltaTime, 0);
                            m_stepUpState = 2;
                        }
                        else
                        {
                            m_stepUpState = 1;
                        }

                        m_stepHitCount++;
                    }
                }
            }



            return m_stepHitCount > 0;
        }

        private bool CheckGround()
        {
            RaycastHit groundHit;
            Ray groundRay = new Ray(transform.position, groundDirection);
            if (Physics.SphereCast(groundRay, groundRadius, out groundHit, m_groundDistance, m_groundMask))
            {
                m_jumpCount = 0;
                m_jumpMemoryState = 1;
                if (isJumping)
                {
                    isJumping = false;
                }

                /*if(IsInvoking("JumpMemory"))
                {
                    CancelInvoke("JumpMemory");
                }*/
                return true;
            }
            else
            {
                Invoke("JumpMemory", jumpMemory);
                return false;
            }
        }

        private bool CheckSlopeAngle()
        {
            RaycastHit slopeHit, moveHit;
            if (Physics.SphereCast(transform.position, slopeRadius, groundDirection, out slopeHit, slopeDistance, m_slopeMask))
            {
                m_slopeAngle = Vector3.Angle(Vector3.up, slopeHit.normal);
                m_slopeNormal = slopeHit.normal;
                if (m_slopeAngle > slopeAngle)
                {
                    Ray directionRay = new Ray(transform.position, m_direction.normalized);
                    if (Physics.Raycast(directionRay, out moveHit, speed, m_groundMask))
                    {
                        if (moveHit.collider == slopeHit.collider)
                        {
                            //revisar y mejorar esta parte
                            //codigo anterior funcional m_movementDirection = Vector3.zero;
                            m_direction = Vector3.zero;
                        }
                    }
                }

                return m_slopeAngle < slopeAngle && m_slopeAngle != 0;
            }

            m_slopeNormal = Vector3.zero;
            return false;
        }

        private bool CheckWall(out Vector3 wallNormal)
        {
            bool tmpWall = false;
            wallNormal = Vector3.zero;
            Vector3 position = transform.position + (Vector3.up * wallHeight);
            RaycastHit hit;

            for (int i = 0; i < 8; i++)
            {
                if (Physics.Raycast(position, Quaternion.AngleAxis(i * 45, transform.up) * Vector3.forward, out hit, wallDistance, m_groundMask))
                {
                    tmpWall = true;
                    wallNormal = hit.normal;

                    if(!isGrounded && airControl > 0 && m_isColliding)
                    {
                        
                    }

                }
            }

            return tmpWall;
        }
        #endregion

        #region PUBLIC
        public void Move(Vector3 value)
        {
            m_movementDirection = new Vector3(value.x, 0, value.y);
            m_movementDirection.Normalize();

            if (m_movementDirection.magnitude <= 0f)
            {
                Task.Delay(400).ContinueWith(t => ResetDirection());
            }
            else
            { 
                m_lastDirection = m_movementDirection;
            }

        }

        public void Jump(float scale = 1f)
        {
            if (!jump) return;
            if (m_jumpCount < jumpCount && m_jumpMemoryState > 0)
            {
                m_groundDistance = 0;
                StartCoroutine(JumpApplyForce(scale));
            }
        }
        #endregion

        #region PRIVATE
        private void CalculateMovement()
        {
            if (isGrounded /*|| isTouchingStep*/)
            {
                m_collider.material.staticFriction = m_staticFriction;
                m_collider.material.dynamicFriction = m_dynamicFriction;
                m_collider.material.frictionCombine = m_frictionCombine;
                m_rigidbody.maxLinearVelocity = Mathf.Infinity;

                m_speed = speed * Mathf.Clamp01(speedFactor);
                m_direction = RelativeTo(m_movementDirection, m_camera, true);
                m_direction.Normalize();
                m_direction = isSloped ? Vector3.ProjectOnPlane(m_direction, m_slopeNormal) : m_direction;
                m_direction *= m_speed;

                if (isTouchingStep && m_stepUpState > 1) //funciona con m_stepUpState > 0
                {
                    m_movementDirection.Normalize();
                    m_stepProyectedDirection.Normalize();
                    m_direction = m_movementDirection + m_stepProyectedDirection;
                    m_direction *= speed;

                }
                m_direction = Vector3.ClampMagnitude(m_direction, speed);

                if (m_direction.magnitude > threshold)
                {
                    m_rigidbody.velocity = Vector3.SmoothDamp(m_rigidbody.velocity, m_direction, ref m_velocity, dampSpeedUp);
                }
                else
                {
                    m_rigidbody.velocity = Vector3.SmoothDamp(m_rigidbody.velocity, Vector3.zero, ref m_velocity, dampSpeedDown);

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

                m_lastVelocity = Vector3.ClampMagnitude(m_lastVelocity, speed);
            }

            if (!isGrounded && airControl > 0)
            {
                m_speed = speed * Mathf.Clamp01(speedFactor);
                m_direction = RelativeTo(m_movementDirection, m_camera, true);
                m_direction.Normalize();
                m_direction *= m_speed * airControl;
                float damp = m_direction.magnitude > threshold ? 
                    dampSpeedUp : dampSpeedDown;
                m_direction = Vector3.ClampMagnitude(m_direction, speed);

                if (m_direction.magnitude > threshold) 
                {
                    m_direction = m_lastVelocity + m_direction;
                    Vector3 acceleration = (m_direction - m_rigidbody.velocity) / Time.fixedDeltaTime;
                    acceleration = Vector3.Scale(acceleration, Vector3.one + Physics.gravity.normalized);
                    m_rigidbody.AddForce(acceleration, ForceMode.Acceleration);
                }
            }

            
        }

        private void CalculateGroundForce()
        {
            if (isGrounded && groundForce)
            {
                RaycastHit groundHit;
                Vector3 otherVel = Vector3.zero;
                if (Physics.SphereCast(transform.position, groundRadius, groundDirection, out groundHit, m_groundMask))
                {
                    if (groundHit.rigidbody) {
                        otherVel = groundHit.rigidbody.velocity;
                    }
                }

                //calculate the dot product between ground ray and the this object velocity 
                float rayDirVel = Vector3.Dot(groundDirection, m_rigidbody.velocity);
                //calculate the dot product between ground ray and the other object velocity
                float otherDirVel = Vector3.Dot(groundDirection, otherVel);
                //set the relative distance 
                float relVel = rayDirVel - otherDirVel;
                //set the relative force
                float x = groundHit.distance - groundDistance;

                //Calculate damping force to set the capsule in the air
                float SpringForce = (x * groundSpringForce) - (relVel * groundSpringDamper);

                //add the force to the body object
                m_rigidbody.AddForce(groundDirection * SpringForce, groundForceMode);
                /*if (HitBody != null && JumpCount <= 0)
                {
                    HitBody.AddForceAtPosition(RayDir * -SpringForce, GroundHit.point);
                }*/

            }
        }

        private void CalculateStepUpForce()
        {
            if (m_isStepUpGroundForce)
            {
                float iterRatio = ((float)m_stepIterationCount / stepIteration);
                float threshold = Mathf.Clamp01(stepIterationThreshold / 100f);

                if(m_stepHitCount <= 0 || m_stepUpState < 2 || iterRatio <= threshold)
                {
                    m_isStepUpGroundForce = false;
                    return;
                }

                RaycastHit hit;
                if (Physics.Raycast(transform.position, groundDirection, out hit, groundDistance + stepDistance, m_groundMask))
                {
                    Vector3 otherVel = Vector3.zero;
                    if (hit.rigidbody)
                    {
                        otherVel = hit.rigidbody.velocity;
                    }

                    float rayDot = Vector3.Dot(groundDirection, m_rigidbody.velocity);
                    float bodyDot = Vector3.Dot(groundDirection, otherVel);
                    float relVel = rayDot - bodyDot;
                    float x = (hit.distance - (groundDistance + stepHeight));
                    float ff = Mathf.Lerp(stepSpringMax, stepSpringMin, m_stepHitDistance / stepDistance);
                    float springForce = (x * stepSpringForce * ff) - (relVel * stepSpringDamp);

                    m_rigidbody.AddForce(groundDirection * springForce * m_rigidbody.mass, ForceMode.Force);

                }
            }
        }

        private IEnumerator JumpApplyForce(float scale)
        {
            isJumping = true;
            Vector3 direction = transform.up * m_rigidbody.mass;
            float force = jumpForce * scale;
            m_rigidbody.AddForce(direction * force, ForceMode.Impulse);
            m_jumpCount++;
            yield return new WaitForSeconds(jumpDelay);
            StartCoroutine(JumpReset());
        }

        private IEnumerator JumpReset()
        {
            yield return new WaitForEndOfFrame();
            m_groundDistance = groundDistance;
        }

        private void JumpMemory()
        {
            m_jumpMemoryState = -1;
            m_jumpCount = jumpCount;
        }
        #endregion

        #region INTERNAL
        internal Vector3 RelativeTo(Vector3 vector3, Transform relativeToThis, bool isPlanar = true)
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

        internal float RoundValue(float _value)
        {
            float unit = (float)Mathf.Round(_value);

            if (_value - unit < 0.000001f && _value - unit > -0.000001f) return unit;
            else return _value;
        }

        internal Vector3 GetStepUpDirection()
        {
            RaycastHit hit;
            Vector3 world = GetStepHitPoint(transform.forward, (int)stepIteration).Item2;

            if(Physics.Raycast(transform.position, groundDirection, out hit, Mathf.Infinity, m_groundMask))
            {
                return world - hit.point;
            }

            return Vector3.zero;
        }

        internal Tuple<Vector3, Vector3> GetStepHitPoint(Vector3 direction, int subdivision)
        {
            //los valores aca tienen que ser iguales a los valores en CheckStepUp()
            RaycastHit hit;
            int count = 0;
            var offset = Vector3.up * stepOffset;
            var height = Vector3.up * stepHeight;
            var lerp = Vector3.Distance(height, offset) / subdivision;

            Vector3 world = Vector3.zero;
            float minDistance = Mathf.Infinity;

            for(int i = 0; i < subdivision; i++)
            {
                if(Physics.Raycast(transform.position + offset + height * (i * lerp), direction, out hit, stepDistance, m_groundMask))
                {
                    Vector3 localPoint = transform.InverseTransformPoint(hit.point);
                    if(Vector3.Distance(height, localPoint) < minDistance)
                    {
                        minDistance = Vector3.Distance(height, localPoint);
                        world = hit.point;
                    }
                    count++;
                }
            }

            var value = stepHeight * Mathf.Clamp01((float)count / subdivision);

            m_stepIterationCount = count;

            var local = Vector3.up * (value + lerp);

            return new Tuple<Vector3, Vector3>(local, world);
        }

        internal void ResetDirection()
        {
            m_lastDirection = Vector3.zero;
        }
        #endregion

        #region GIZMOS
        private void DrawWireCapsule(Vector3 pos, Quaternion rot, float radius, float height, Color color = default(Color))
        {
            if (color != default(Color))
                Handles.color = color;
            Matrix4x4 angleMatrix = Matrix4x4.TRS(pos, rot, Handles.matrix.lossyScale);
            using (new Handles.DrawingScope(angleMatrix))
            {
                var pointOffset = (height - (radius * 2)) / 2;

                //Draw sideways
                Handles.DrawWireArc(Vector3.up * pointOffset, Vector3.left, Vector3.back, -180, radius);
                Handles.DrawLine(new Vector3(0, pointOffset, -radius), new Vector3(0, -pointOffset, -radius));
                Handles.DrawLine(new Vector3(0, pointOffset, radius), new Vector3(0, -pointOffset, radius));
                Handles.DrawWireArc(Vector3.down * pointOffset, Vector3.left, Vector3.back, 180, radius);
                //Draw frontways
                Handles.DrawWireArc(Vector3.up * pointOffset, Vector3.back, Vector3.left, 180, radius);
                Handles.DrawLine(new Vector3(-radius, pointOffset, 0), new Vector3(-radius, -pointOffset, 0));
                Handles.DrawLine(new Vector3(radius, pointOffset, 0), new Vector3(radius, -pointOffset, 0));
                Handles.DrawWireArc(Vector3.down * pointOffset, Vector3.back, Vector3.left, -180, radius);
                //Draw center   
                Handles.DrawWireDisc(Vector3.up * pointOffset, Vector3.up, radius);
                Handles.DrawWireDisc(Vector3.down * pointOffset, Vector3.up, radius);
            }
        }

        private void DrawWireArrow(Vector3 a, Vector3 b, float arrowheadAngle, float arrowheadDistance, float arrowheadLength)
        {
            // Get the Direction of the Vector
            Vector3 dir = b - a;

            // Get the Position of the Arrowhead along the length of the line.
            Vector3 arrowPos = a + (dir * arrowheadDistance);

            // Get the Arrowhead Lines using the direction from earlier multiplied by a vector representing half of the full angle of the arrowhead (y)
            // and -1 for going backwards instead of forwards (z), which is then multiplied by the desired length of the arrowhead lines coming from the point.

            Vector3 up = Quaternion.LookRotation(dir) * new Vector3(0f, Mathf.Sin(arrowheadAngle * Mathf.Deg2Rad), -1f) * arrowheadLength;
            Vector3 down = Quaternion.LookRotation(dir) * new Vector3(0f, -Mathf.Sin(arrowheadAngle * Mathf.Deg2Rad), -1f) * arrowheadLength;
            Vector3 left = Quaternion.LookRotation(dir) * new Vector3(Mathf.Sin(arrowheadAngle * Mathf.Deg2Rad), 0f, -1f) * arrowheadLength;
            Vector3 right = Quaternion.LookRotation(dir) * new Vector3(-Mathf.Sin(arrowheadAngle * Mathf.Deg2Rad), 0f, -1f) * arrowheadLength;

            // Get the End Locations of all points for connecting arrowhead lines.
            Vector3 upPos = arrowPos + up;
            Vector3 downPos = arrowPos + down;
            Vector3 leftPos = arrowPos + left;
            Vector3 rightPos = arrowPos + right;

            // Draw the line from A to B
            Gizmos.DrawLine(a, b);

            // Draw the rays representing the arrowhead.
            Gizmos.DrawRay(arrowPos, up);
            Gizmos.DrawRay(arrowPos, down);
            Gizmos.DrawRay(arrowPos, left);
            Gizmos.DrawRay(arrowPos, right);

            // Draw Connections between rays representing the arrowhead
            Gizmos.DrawLine(upPos, leftPos);
            Gizmos.DrawLine(leftPos, downPos);
            Gizmos.DrawLine(downPos, rightPos);
            Gizmos.DrawLine(rightPos, upPos);

        }

        private void DrawStepUpGizmo(Vector3 direction, int subdivision) 
        {
            RaycastHit hit;
            var offset = Vector3.up * stepOffset;
            var height = Vector3.up * stepHeight;
            var part = subdivision / (1 - stepHeight);
            var lerp = Vector3.Distance(height, offset) / subdivision;

            for (int i = 0; i < subdivision; i++)
            {
                float ratio = stepIterationThreshold / 100f;
                float value = ratio * stepIteration;
                Gizmos.color = i <= value ? Color.red : Color.green;
                if (Physics.Raycast(transform.position + offset + height * (i * lerp), direction, out hit, stepDistance, m_groundMask))
                {
                    Gizmos.DrawSphere(hit.point, 0.05f);
                    Gizmos.DrawRay(transform.position + offset + height * (i * lerp), direction * hit.distance);
                }
                else
                {
                    Gizmos.DrawRay(transform.position + offset + height * (i * lerp), direction * stepDistance);
                }
            }

            Gizmos.DrawSphere(GetStepHitPoint(direction, (int)stepIteration).Item2, 0.05f);

        }

        private void OnDrawGizmosSelected()
        {
            DrawWireCapsule(transform.position, transform.rotation, 0.2f, 0.8f);

            //Movement
            if(m_movementDirection.magnitude > 0)
            {
                Vector3 start = transform.position;
                Vector3 end = transform.position + RelativeTo(m_movementDirection, m_camera);

                Gizmos.color = Color.blue;
                DrawWireArrow(start, end, 20, 1, 0.2f);

                float val = Mathf.Lerp(0.001f, speed, m_rigidbody.velocity.magnitude / speed);
                end = transform.position + ((RelativeTo(m_movementDirection, m_camera)*val)/speed);
                
                Gizmos.color = Color.cyan;
                DrawWireArrow(start, end, 10, 1, 0.2f);
            }

            //Wall
            Gizmos.color = Color.white;
            Vector3 position = transform.position + (Vector3.up * wallHeight);
            RaycastHit wallHit;
            for (int i = 0; i < 8; i++)
            {
                Vector3 angle = Quaternion.AngleAxis(i * 45, transform.up) * Vector3.forward;
                if (Physics.Raycast(position, angle, out wallHit, wallDistance, m_groundMask))
                {
                    Gizmos.DrawCube(wallHit.point, Vector3.one * 0.15f);
                    Gizmos.DrawLine(position, position + angle * wallHit.distance);
                }
                else
                {
                    Gizmos.DrawLine(position, position + angle * wallDistance);
                }
            }

            //Ground
            RaycastHit groundHit;
            if (Physics.SphereCast(transform.position, groundRadius, groundDirection, out groundHit, m_groundDistance, m_groundMask))
            {
                Gizmos.DrawSphere(groundHit.point, groundRadius);
            }

            //Slope
            RaycastHit slopeHit;
            if(Physics.SphereCast(transform.position, slopeRadius, groundDirection, out slopeHit, slopeDistance, m_slopeMask))
            {
                Gizmos.color = isSloped ? Color.green : Color.red;
                Gizmos.DrawWireSphere(slopeHit.point, slopeRadius);
            }

            //Step
            //Gizmos.color = isTouchingStep ? Color.green : Color.red;
            Gizmos.DrawRay(transform.position + Vector3.up * stepOffset, Vector3.up * stepHeight);
            RaycastHit stepHit;
            for (int i = -1; i < 2; i++)
            {
                Vector3 btnPosition = transform.position + Vector3.up * stepOffset;
                Vector3 direction = Quaternion.AngleAxis(45*i, transform.up) * (transform.forward);
                Gizmos.DrawRay(btnPosition, direction * stepDistance);
                if (Physics.Raycast(new Ray(btnPosition, direction), out stepHit, stepDistance, m_groundMask))
                {
                    RaycastHit hit;
                    if (RoundValue(stepHit.normal.y) == 0 && !Physics.Raycast(btnPosition + Vector3.up * stepHeight, direction, out hit, stepDistance, m_groundMask))
                    {
                        DrawStepUpGizmo(direction, (int)stepIteration);
                    }
                    else
                    {
                        Gizmos.DrawRay(btnPosition + Vector3.up * stepHeight, direction * stepDistance);
                    }
                }
            }
            Gizmos.DrawWireSphere(m_stepHit, 0.3f);

        }
        #endregion

    }
}
