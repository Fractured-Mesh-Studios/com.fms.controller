
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace GameEngine
{

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class CharacterController : MonoBehaviour
    {
        public enum StepUpForceMode
        {
            None            = (1 << 0),
            Acceleration    = (1 << 1),
            Translation     = (1 << 2),
            Both            = (1 << 3),
        }

        //movement
        [HideInInspector] public float speed = 10f;
        [HideInInspector] public float speedFactor = 1f;
        [HideInInspector] public float threshold = 0.15f;
        [HideInInspector] public float airControl = 0f;
        [HideInInspector] public float dampSpeedUp = 0.2f;
        [HideInInspector] public float dampSpeedDown = 0.1f;

        [HideInInspector] public bool crouch = true;
        [HideInInspector] public float crouchSpeed = 5f;
        [HideInInspector] public float crouchSpeedFactor = 1f;
        [HideInInspector] public float crouchHeight = 0.60f;
        [HideInInspector] public float crouchRadius = 0.45f;

        [HideInInspector] public bool sprint = true;
        [HideInInspector] public float sprintSpeed = 15f;
        [HideInInspector] public float sprintSpeedFactor = 1f;

        [HideInInspector] public bool prone = true;
        [HideInInspector] public float proneSpeed = 3f;
        [HideInInspector] public float proneSpeedFactor = 1f;
        [HideInInspector] public float proneHeight = 1f;
        [HideInInspector] public float proneRadius = 0.45f;

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
        [HideInInspector] public float stepSmooth = 1f;
        [HideInInspector] public uint stepIteration = 10;
        [HideInInspector] public StepUpForceMode stepForceMode = StepUpForceMode.Translation;


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
        public bool isCrouch {  get; private set; }
        public bool isSprinting {  get; private set; }
        public bool isProne { get; private set; }

        public Vector3 direction { get { return m_direction.normalized; } }
        public Vector3 velocity { get { return m_velocity; } }

        //checks
        public bool canJump { get { return m_jumpCount < jumpCount && m_jumpMemoryState > 0 || !jump; } }
        public bool canMove { get { return isGrounded || airControl > 0; } }

        //components
        private Rigidbody m_rigidbody;
        private CapsuleCollider m_collider;
        private Stamina m_stamina;
        private Transform m_camera;

        //serialized private fields
        [SerializeField][HideInInspector] private LayerMask m_slopeMask;
        [SerializeField][HideInInspector] private LayerMask m_groundMask;

        //variables
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
        private float m_groundDistance;
        private float m_stepHitDistance;
        private float m_initialHeight;
        private float m_initialSpeed;
        private float m_initialRadius;
        private float[] m_friction = new float[2];

        private Vector3 m_initialColliderCenter;
        private bool m_isStepUpGroundForce;
        private bool m_isColliding;
        private bool m_rootMotion;
        private int m_jumpCount;
        private int m_jumpMemoryState = -1;
        private int m_stepUpState = -1;
        private int m_stepHitCount = 0;
        private PhysicMaterialCombine m_frictionCombine;

        private RaycastHit m_groundRay;

        protected virtual void Awake()
        {
            m_stamina = GetComponent<Stamina>();
            m_rigidbody = GetComponent<Rigidbody>();
            m_collider = GetComponent<CapsuleCollider>();
            if (m_collider)
            {
                m_friction[0] = m_collider.material.staticFriction;
                m_friction[1] = m_collider.material.dynamicFriction;
                m_frictionCombine = m_collider.material.frictionCombine;

                m_initialHeight = m_collider.height;
                m_initialRadius = m_collider.radius;
                m_initialColliderCenter = m_collider.center;
            }
            m_camera = camera ? camera : Camera.main.transform;
            m_groundDistance = groundDistance;
            m_rigidbody.interpolation = RigidbodyInterpolation.None;
            m_initialSpeed = speed;
        }

        protected virtual void Update()
        {
            Vector3 wallNormal;

            //Checks
            isGrounded = CheckGround();
            isSloped = CheckSlopeAngle();
            isTouchingWall = CheckWall(out wallNormal);
            isTouchingStep = CheckStep(50);
            CalculateStepUp();
        }

        protected virtual void FixedUpdate()
        {
            CalculateStamina();
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
                if(m_rigidbody.velocity.magnitude <= 0.1f && m_movementDirection.magnitude != 0)
                {
                
                }
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

            m_stepProyectedDirection = forward;
            RaycastHit stepHit;
            for (int i = -1; i < 2; i++) // -1 0 1
            {
                position = transform.position + Vector3.up * stepOffset;
                direction = Quaternion.AngleAxis(stepAngle * i, transform.up) * forward;

                stepDistance = stepDistance != 0 ? stepDistance : 0.1f;
                if (Physics.Raycast(position, direction, out stepHit, stepDistance, m_groundMask))
                {
                    RaycastHit hit;
                    m_stepHitDistance = stepHit.distance;
                    m_stepProyectedDirection = GetStepUpDirection();

                    if (RoundValue(stepHit.normal.y) == 0 && !Physics.Raycast(position + Vector3.up * stepHeight, direction, out hit, stepDistance, m_groundMask))
                    {
                        forward = transform.InverseTransformDirection(forward);
                        float dot = Vector3.Dot(forward, m_movementDirection);
                        float angle = Vector3.Angle(forward, m_movementDirection);

                        if (m_movementDirection.magnitude != 0 && dot > 0 && angle <= slopeAngle)
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
            Ray groundRay = new Ray(transform.position, groundDirection);
            if (Physics.SphereCast(groundRay, groundRadius, out m_groundRay, m_groundDistance, m_groundMask))
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
            if (Physics.SphereCast(transform.position, slopeRadius, groundDirection, out slopeHit, slopeDistance, m_groundMask))
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

            //Crouch Wall Check
            float height = m_initialHeight + Mathf.Abs(m_initialHeight - crouchHeight);
            Vector3 world = transform.TransformDirection(m_collider.center);
            if(Physics.SphereCast(world, m_initialRadius, transform.up, out hit, height, m_groundMask))
            {
                isCrouch = false;
            }
            

            return tmpWall;
        }
        #endregion

        #region PUBLIC
        public void Move(Vector3 value)
        {
            m_rootMotion = false;
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

        public void MoveRootMotion(Vector3 direction, Vector3 velocity)
        {
            m_rootMotion = true;
            m_movementDirection = direction;
            m_movementDirection.Normalize();

            m_velocity = velocity;
        }

        public void Sprint(bool value)
        {
            isSprinting = value;

            if (isSprinting) 
            {
                speed = sprintSpeed * sprintSpeedFactor;
            }
            else
            {
                speed = m_initialSpeed;
            }
        }

        public void Jump(float scale = 1f)
        {
            if (!jump) return;
            if (m_jumpCount < jumpCount && m_jumpMemoryState > 0)
            {
                StartCoroutine(JumpApplyForce(scale));
            }
        }

        public void Crouch()
        {
            isCrouch = !isCrouch;
            Crouch(isCrouch);
        }

        public void Crouch(bool value)
        {
            if (!crouch) return;
            isCrouch = value;

            float radius = m_collider.radius;
            float radiusDiff = radius - crouchRadius;
            float groundDistance = m_groundRay.distance;

            float newCenter = m_collider.height - crouchHeight;
            Debug.Log(newCenter);


            RaycastHit hit;
            Vector3 position = transform.TransformPoint(m_collider.center);
            if(Physics.Raycast(position, groundDirection, out hit, this.groundDistance, m_groundMask))
            {
                groundDistance = hit.distance;
            }

            if (isCrouch)
            {

                speed = crouchSpeed * crouchSpeedFactor;
                //m_collider.radius = crouchRadius;
                m_collider.height = crouchHeight;
                m_collider.center = groundDirection * (newCenter - crouchRadius);
                //m_rigidbody.AddForce(groundDirection * m_rigidbody.mass * 10f, ForceMode.Impulse);
                m_stepHitDistance = m_groundRay.distance;
            }
            else
            {
                speed = m_initialSpeed;
                //m_collider.radius = m_initialRadius;
                m_collider.height = m_initialHeight;
                m_stepHitDistance = m_groundRay.distance;
                m_collider.center = m_initialColliderCenter;
            }
        }

        public void Prone()
        {
            isProne = !isProne;
            Prone(isProne);
        }

        public void Prone(bool value)
        {
            if (!prone) return;
            isProne = value;

            if(isProne)
            {
                speed = proneSpeed * proneSpeedFactor;
                m_collider.direction = 2;
                m_collider.radius = proneRadius;
                m_collider.height = proneHeight;
                m_collider.center = groundDirection * m_groundRay.distance;
                m_stepHitDistance = m_groundRay.distance;
            }
            else
            {
                speed = m_initialSpeed;
                m_collider.direction = 1;
                m_collider.radius = m_initialRadius;
                m_collider.height = m_initialHeight;
                m_stepHitDistance = m_groundRay.distance;
                m_collider.center = m_initialColliderCenter;
            }
        }
        #endregion

        #region PRIVATE
        private void CalculateMovement()
        {
            void slope() 
            {
                if (isSloped)
                {
                    if (slopeLock)
                    {
                        m_rigidbody.maxLinearVelocity = 0;
                        //m_rigidbody.velocity = Vector3.zero;
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


            if (isGrounded /*|| isTouchingStep*/)
            {
                m_collider.material.staticFriction = m_friction[0];
                m_collider.material.dynamicFriction = m_friction[1];
                m_collider.material.frictionCombine = m_frictionCombine;
                m_rigidbody.maxLinearVelocity = Mathf.Infinity;

                if (m_rootMotion)
                {
                    m_direction = m_movementDirection;

                    m_velocity = isSloped ? Vector3.ProjectOnPlane(m_velocity, m_slopeNormal) : m_velocity;
                    m_velocity = Vector3.ClampMagnitude(m_velocity, speed);

                    if(isTouchingStep && m_stepUpState > 1)
                    {
                        m_velocity += m_stepProyectedDirection;
                    }

                    m_rigidbody.velocity = m_velocity;

                    if(m_velocity.magnitude < threshold)
                    {
                        slope();
                    }
                }
                else
                {
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

                        slope();
                    }

                    m_lastVelocity = Vector3.ClampMagnitude(m_lastVelocity, speed);
                }

            }

            if (!isGrounded && airControl > 0)
            {
                if (m_rootMotion)
                {
                    //no probado
                    if (m_direction.magnitude > threshold)
                    {
                        m_direction = m_lastVelocity + m_velocity;
                        Vector3 acceleration = (m_direction - m_rigidbody.velocity) / Time.fixedDeltaTime;
                        acceleration = Vector3.Scale(acceleration, Vector3.one + Physics.gravity.normalized);
                        m_rigidbody.AddForce(acceleration, ForceMode.Acceleration);
                    }
                }
                else
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

                m_lastVelocity = Vector3.ClampMagnitude(m_lastVelocity, speed); // <-- 
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

        private void CalculateStepUp()
        {
            Vector3 ClampMagnitude(Vector3 v, float max, float min)
            {
                double sm = v.sqrMagnitude;
                if (sm > (double)max * (double)max) return v.normalized * max;
                else if (sm < (double)min * (double)min) return v.normalized * min;
                return v;
            }

            bool valid = true;
            valid &= stepForceMode == StepUpForceMode.Translation;
            valid |= stepForceMode == StepUpForceMode.Both;

            if (m_isStepUpGroundForce && valid)
            {
                if (m_stepHitCount <= 0 || m_stepUpState < 2)
                {
                    m_isStepUpGroundForce = false;
                    return;
                }

                

                Vector3 direction = transform.up * stepSmooth;
                direction = ClampMagnitude(direction, speed, 1f);

                m_rigidbody.position += direction * Time.deltaTime;
                Debug.Log("Tran");
            }
        }

        private void CalculateStepUpForce()
        {
            bool valid = true;
            valid &= stepForceMode == StepUpForceMode.Acceleration;
            valid |= stepForceMode == StepUpForceMode.Both;

            if (m_isStepUpGroundForce && valid)
            {
                if (m_stepHitCount <= 0 || m_stepUpState < 2)
                {
                    m_isStepUpGroundForce = false;
                    return;
                }

                m_rigidbody.AddForce(transform.up * stepSmooth * 2f, ForceMode.Acceleration);
            }
        }

        private void CalculateStamina()
        {
            if (m_stamina)
            {
                if (m_stamina.isEmpty)
                {
                    m_movementDirection = Vector3.zero;
                }
            }
        }

        private IEnumerator JumpApplyForce(float scale)
        {
            yield return new WaitForSeconds(jumpDelay);
            m_groundDistance = 0;
            isJumping = true;
            Vector3 direction = transform.up * m_rigidbody.mass;
            float force = jumpForce * scale;
            m_rigidbody.AddForce(direction * force, ForceMode.Impulse);
            m_jumpCount++;
            yield return new WaitForSeconds(0.1f);
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
            Vector3 local = GetStepHitPoint(transform.forward, (int)stepIteration)[0];
            Vector3 world = GetStepHitPoint(transform.forward, (int)stepIteration)[1];

            if(Physics.Raycast(transform.position, groundDirection, out hit, Mathf.Infinity, m_groundMask))
            {
                return world - hit.point;
            }

            return local;
        }

        internal Vector3[] GetStepHitPoint(Vector3 direction, int subdivision)
        {
            //los valores aca tienen que ser iguales a los valores en CheckStepUp()
            RaycastHit hit;
            int count = 0;
            var offset = Vector3.up * stepOffset;
            var height = Vector3.up * stepHeight;
            var lerp = Vector3.Distance(height, offset) / subdivision;

            Vector3 world = transform.position + transform.forward;
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
            var local = Vector3.up * (value + lerp);

            Vector3[] points = new Vector3[2];
            points[0] = local;
            points[1] = world;
            return points;
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

            if(dir != Vector3.zero)
            {
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
                Gizmos.color = Color.green;
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

            Gizmos.DrawSphere(GetStepHitPoint(direction, (int)stepIteration)[1], 0.05f);

        }

        private void OnDrawGizmosSelected()
        {
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

                DrawWireArrow(start, transform.position + m_stepProyectedDirection, 10, 1, 0.2f);
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
            //Gizmos.DrawWireSphere(m_stepHit, 0.3f);
            
            if (m_collider)
            {


                float height = m_initialHeight ;
                
                float val = isCrouch ? height : m_initialHeight;
                
                Vector3 world = transform.TransformPoint(m_initialColliderCenter);
                DrawWireCapsule(world, transform.rotation, m_initialRadius, val, Color.blue);
            }

        }
        #endregion

    }
}
