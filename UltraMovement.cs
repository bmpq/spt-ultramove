using EFT.Ballistics;
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
        Vector3 slideDir;

        bool toSlam;
        bool slamming;

        float shakeIntensity = 0f;

        Vector3 dashDir;
        float dashTime;

        Vector3 prevVelocity;
        Vector3 lastCollisionImpulse;

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
            capsule.center = new Vector3(0, capsule.height / 2f, 0);
            capsule.radius = 0.36f;

            PhysicMaterial physmat = new PhysicMaterial();
            physmat.staticFriction = 0;
            physmat.dynamicFriction = 0;
            physmat.frictionCombine = PhysicMaterialCombine.Minimum;
            capsule.material = physmat;

            groundCheck = new GameObject("GroundCheck").AddComponent<GroundCheck>();
            groundCheck.gameObject.transform.SetParent(transform, false);
            SphereCollider sphere = groundCheck.gameObject.AddComponent<SphereCollider>();
            sphere.radius = capsule.radius - 0.05f;
            sphere.isTrigger = true;
            groundCheck.gameObject.transform.localPosition = new Vector3(0, sphere.radius / 2f, 0);
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
            updownRotation = Mathf.Clamp(updownRotation - mouseY, -85f, 85f);

            camLevel = Mathf.Lerp(camLevel, capsule.height - 0.1f, Time.deltaTime * 20f);
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

            if (Input.GetKeyDown(KeyCode.LeftShift) && dashTime <= 0f)
            {
                dashDir = transform.TransformDirection(vectorInput == Vector3.zero ? new Vector3(0, 0, 1f) : vectorInput.normalized);

                dashTime = 0.3f;

                PlayerAudio.Instance.Play("Dodge3");
            }

            coyoteTime -= Time.deltaTime;

            if (!sliding)
            {
                sliding = (groundCheck.isGrounded || coyoteTime > 0f) && Input.GetKeyDown(KeyCode.C) && jumpCooldown <= 0f;
                if (sliding)
                {
                    slideDir = transform.forward;
                }
            }
            else
            {
                if (Input.GetKeyUp(KeyCode.C))
                {
                    sliding = false;
                }
            }

            PlayerAudio.Instance.Sliding(sliding);

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

        void Slam()
        {
            shakeIntensity = 1f;
            EFTBallisticsInterface.Instance.Effect("big_round_impact", transform.position);
            EFTTargetInterface.Slam(transform.position);
        }

        void FixedUpdate()
        {
            Vector3 curVelocity = rb.velocity;

            bool grounded = groundCheck.isGrounded;
            rb.useGravity = !grounded;

            rb.angularVelocity = Vector3.zero;
            rb.MoveRotation(Quaternion.Euler(0, horizontalRotation, 0));

            Vector3 inputRelativeDirection = transform.TransformDirection(vectorInput.normalized);

            if (dashTime > 0f)
            {
                dashTime -= Time.fixedDeltaTime;
                rb.velocity = Vector3.Lerp(rb.velocity, dashDir * moveSpeed * 3f, Time.fixedDeltaTime * 20f);
            }
            else if (grounded)
            {
                if (slamming)
                {
                    Slam();
                }
                slamming = false;

                if (jumpCooldown <= 0f && !sliding)
                {
                    Vector3 targetWalkVelocity = new Vector3(inputRelativeDirection.x * moveSpeed, 0, inputRelativeDirection.z * moveSpeed);
                    rb.velocity = Vector3.Lerp(rb.velocity, targetWalkVelocity, Time.fixedDeltaTime * 20f);

                    if (rb.velocity.magnitude > 0.1f)
                        PlayerAudio.Instance.PlayWalk();
                }
            }
            else if (!sliding && !slamming)
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

                if (DetectWall(out Vector3 wallNormal))
                {
                    rb.velocity = new Vector3(rb.velocity.x * 0.9f, Mathf.Max(-1f, rb.velocity.y), rb.velocity.z * 0.9f);

                    if (toJump && jumpCooldown <= 0f)
                    {
                        toJump = false;
                        jumpCooldown = 0.1f;

                        Vector3 jumpDirection = wallNormal + Vector3.up;
                        rb.AddForce(jumpDirection.normalized * 14f, ForceMode.Impulse);
                    }
                }
            }

            if (grounded && !groundedPrevTick)
            {
                if (Mathf.Min(prevVelocity.y, -lastCollisionImpulse.y) < -7f)
                    PlayerAudio.Instance.Play("Landing");
            }

            if (!grounded && groundedPrevTick && jumpCooldown <= 0)
            {
                coyoteTime = 0.3f;
            }
            groundedPrevTick = grounded;

            if (!grounded && coyoteTime < 0f)
            {
                sliding = false;
            }

            if (toJump)
            {
                toJump = false;

                if (coyoteTime > 0f || groundedPrevTick)
                {
                    dashTime = -1f;

                    coyoteTime = -1f;

                    rb.velocity = new Vector3(rb.velocity.x, Mathf.Max(rb.velocity.y + 10f, 10f), rb.velocity.z);

                    jumpCooldown = 0.1f;

                    sliding = false;

                    PlayerAudio.Instance.Play("Bluezone-Autobots-footstep-013");
                }
            }
            else if (toSlam)
            {
                dashTime = -1f;
                slamming = true;
                toSlam = false;
                rb.velocity = new Vector3(0, -40f, 0);
            }

            if (sliding)
            {
                capsule.height = capsule.radius * 2f;
                capsule.center = new Vector3(0, capsule.height / 2f, 0);

                Vector3 slideVel = slideDir * moveSpeed * 1.5f;
                slideVel.y = -1f;
                rb.velocity = slideVel;
            }
            else
            {
                capsule.height = 1.7f;
                capsule.center = new Vector3(0, capsule.height / 2f, 0);
            }

            toJump = false;

            prevVelocity = curVelocity;
        }

        private void OnCollisionEnter(Collision collision)
        {
            lastCollisionImpulse = collision.impulse;
        }

        private bool DetectWall(out Vector3 wallNormal)
        {
            wallNormal = Vector3.zero;

            if (vectorInput == Vector3.zero)
                return false;

            Vector3 inputRelativeDirection = transform.TransformDirection(vectorInput.normalized);

            int layer1 = 1 << 18; // LowPolyCollider
            int layer2 = 1 << 11; // Terrain
            int layerMask = layer1 | layer2;

            if (Physics.SphereCast(transform.position + new Vector3(0, capsule.height / 2f, 0), capsule.radius / 2f, inputRelativeDirection, out RaycastHit hit, capsule.radius, layerMask))
            {
                wallNormal = hit.normal;
                return true;
            }

            return false;
        }
    }
}