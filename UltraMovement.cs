using UnityEngine;

namespace ultramove
{
    public class UltraMovement : MonoBehaviour
    {
        private Transform camParent;
        private Camera cam;
        private Rigidbody rb;
        private float horizontalRotation = 0f;

        private float updownRotation = 0f;

        private float mouseSensitivity = 100f;

        private float coyoteTime;

        private float moveSpeed = 7f;

        CapsuleCollider capsule;
        private LayerMask groundLayer;

        GroundCheck groundCheck;

        bool toJump;
        float jumpCooldown;

        Vector3 vectorInput = Vector3.zero;

        float camZ = 0f;
        float camLevel;

        bool groundedPrevTick = false;

        bool sliding;

        bool toSlam;
        bool slamming;

        float shakeIntensity = 0f;

        void Start()
        {
            cam = Camera.main;

            camParent = new GameObject().transform;
            cam.transform.SetParent(camParent, false);
            cam.transform.localRotation = Quaternion.identity;
            cam.transform.localPosition = Vector3.zero;
            //Cursor.lockState = CursorLockMode.Locked;

            groundLayer = LayerMask.NameToLayer("LowPolyCollider");

            Physics.gravity = new Vector3(0, -14f, 0);

            rb = GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            rb.solverIterations = 20;
            rb.solverVelocityIterations = 20;

            capsule = GetComponent<CapsuleCollider>();
            capsule.height = 1.7f;
            capsule.center = Vector3.zero;
            capsule.radius = 0.36f;

            PhysicMaterial physmat = new PhysicMaterial();
            physmat.staticFriction = 0;
            physmat.dynamicFriction = 0;
            physmat.frictionCombine = PhysicMaterialCombine.Minimum;
            capsule.material = physmat;

            rb.position += new Vector3(0, 2f, 0);

            groundCheck = new GameObject("GroundCheck").AddComponent<GroundCheck>();
            groundCheck.gameObject.transform.SetParent(transform, false);
            groundCheck.gameObject.transform.localPosition = new Vector3(0, -capsule.height / 2f + capsule.radius - 0.2f, 0);
            SphereCollider sphere = groundCheck.gameObject.AddComponent<SphereCollider>();
            sphere.radius = capsule.radius - 0.05f;
            sphere.isTrigger = true;
        }

        void Update()
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            vectorInput = Vector3.zero;
            if (Input.GetKey(KeyCode.A))
                vectorInput.x = -1f;
            if (Input.GetKey(KeyCode.D))
                vectorInput.x = 1f;
            if (Input.GetKey(KeyCode.W))
                vectorInput.z = 1f;
            if (Input.GetKey(KeyCode.S))
                vectorInput.z = -1f;

            horizontalRotation += mouseX;
            updownRotation = Mathf.Clamp(updownRotation - mouseY, -90f, 90f);

            camLevel = Mathf.Lerp(camLevel, capsule.height / 2f - 0.1f, Time.deltaTime * 20f);
            camParent.position = transform.position + new Vector3(0, camLevel, 0);
            camParent.rotation = Quaternion.Euler(updownRotation, horizontalRotation, 0);

            if (Input.GetKeyDown(KeyCode.Space) && jumpCooldown <= 0f)
            {
                toJump = true;
            }

            if (Input.GetKeyDown(KeyCode.C) && !groundCheck.isGrounded && coyoteTime <= 0f && !slamming)
            {
                toSlam = true;
            }

            coyoteTime -= Time.deltaTime;

            if (!sliding)
            {
                sliding = (groundCheck.isGrounded || coyoteTime > 0f) && Input.GetKeyDown(KeyCode.C) && jumpCooldown <= 0f;
            }
            else
            {
                if (Input.GetKeyUp(KeyCode.C))
                {
                    sliding = false;
                }
            }

            jumpCooldown -= Time.deltaTime;

            AnimateCamera();
        }

        void AnimateCamera()
        {
            float targetZ = -vectorInput.x * 2f;

            if (!groundCheck.isGrounded)
                targetZ *= 2f;

            camZ = Mathf.Lerp(camZ, targetZ, Time.deltaTime * 10f);
            cam.transform.localEulerAngles = new Vector3(0, 0, camZ);

            shakeIntensity -= Time.deltaTime * 2f;

            if (sliding)
                shakeIntensity = 0.3f;

            if (shakeIntensity > 0)
            {
                Vector3 randomOffset = Random.insideUnitSphere * shakeIntensity;
                cam.transform.localPosition = Vector3.Lerp(Vector3.zero, randomOffset, Time.deltaTime * 10f);
            }
        }

        void FixedUpdate()
        {
            bool grounded = groundCheck.isGrounded;
            rb.useGravity = !grounded;

            rb.MoveRotation(Quaternion.Euler(0, horizontalRotation, 0));

            Vector3 inputRelativeDirection = transform.TransformDirection(vectorInput.normalized);

            if (grounded)
            {
                if (slamming)
                    shakeIntensity = 1f;
                slamming = false;

                if (jumpCooldown <= 0f && !sliding)
                {
                    Vector3 targetWalkVelocity = new Vector3(inputRelativeDirection.x * moveSpeed, 0, inputRelativeDirection.z * moveSpeed);
                    rb.velocity = Vector3.Lerp(rb.velocity, targetWalkVelocity, Time.fixedDeltaTime * 20f);
                }
            }
            else if (!sliding)
            {
                Vector3 relativeVelocity = transform.InverseTransformDirection(rb.velocity);

                float airVelocityLimit = moveSpeed;
                Vector3 airForce = vectorInput.normalized;
                airForce.y = 0f;

                if (vectorInput.x > 0f && relativeVelocity.x > airVelocityLimit)
                    airForce.x = 0f;
                else if (vectorInput.x < 0f && relativeVelocity.x < -airVelocityLimit)
                    airForce.x = 0f;

                if (vectorInput.z > 0f && relativeVelocity.z > airVelocityLimit)
                    airForce.z = 0f;
                else if (vectorInput.z < 0f && relativeVelocity.z < -airVelocityLimit)
                    airForce.z = 0f;

                rb.AddRelativeForce(airForce * Time.fixedDeltaTime * 1500f);
            }

            if (!grounded && coyoteTime < 0f)
            {
                sliding = false;
            }

            if (toJump)
            {
                toJump = false;

                if (coyoteTime > 0f || groundedPrevTick)
                {
                    coyoteTime = -1f;

                    rb.velocity = new Vector3(rb.velocity.x, Mathf.Max(rb.velocity.y + 10f, 10f), rb.velocity.z);

                    jumpCooldown = 0.1f;

                    sliding = false;
                }
            }
            else if (toSlam)
            {
                slamming = true;
                toSlam = false;
                rb.velocity = new Vector3(0, -40f, 0);
            }

            if (sliding)
            {
                capsule.height = 1.7f / 4f;
                capsule.center = new Vector3(0, -capsule.height / 2f, 0);

                Vector3 slideVel = transform.forward * moveSpeed * 1.5f;
                slideVel.y = -1f;
                rb.velocity = slideVel;
            }
            else
            {
                capsule.height = 1.7f;
                capsule.center = Vector3.zero;
            }

            if (!grounded && groundedPrevTick && jumpCooldown <= 0)
            {
                coyoteTime = 0.3f;

                if (sliding)
                    coyoteTime *= 3f;
            }
            groundedPrevTick = grounded;
        }
    }
}