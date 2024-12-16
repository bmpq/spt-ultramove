using UnityEngine;

namespace ultramove
{
    public class UltraMovement : MonoBehaviour
    {
        private Transform cameraTransform;
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

        void Start()
        {
            cam = Camera.main;

            cameraTransform = new GameObject().transform;
            cam.transform.SetParent(cameraTransform, false);
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
            capsule.radius = 0.4f;

            PhysicMaterial physmat = new PhysicMaterial();
            physmat.staticFriction = 0;
            physmat.dynamicFriction = 0;
            physmat.frictionCombine = PhysicMaterialCombine.Minimum;
            capsule.material = physmat;

            rb.position += new Vector3(0, 2f, 0);

            groundCheck = new GameObject("GroundCheck").AddComponent<GroundCheck>();
            groundCheck.gameObject.transform.SetParent(transform, false);
            groundCheck.gameObject.transform.localPosition = new Vector3(0, -capsule.height / 2f + capsule.radius - 0.3f, 0);
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
            cameraTransform.position = transform.position + new Vector3(0, camLevel, 0);
            cameraTransform.rotation = Quaternion.Euler(updownRotation, horizontalRotation, 0);

            jumpCooldown -= Time.deltaTime;

            if (Input.GetKeyDown(KeyCode.Space) && jumpCooldown <= 0f)
            {
                toJump = true;
            }

            coyoteTime -= Time.deltaTime;

            AnimateCamera();
        }

        void AnimateCamera()
        {
            float targetZ = -vectorInput.x * 2f;

            if (!groundCheck.isGrounded)
                targetZ *= 2f;

            camZ = Mathf.Lerp(camZ, targetZ, Time.deltaTime * 10f);
            cam.transform.localEulerAngles = new Vector3(0, 0, camZ);
        }


        void FixedUpdate()
        {
            bool grounded = groundCheck.isGrounded;
            rb.useGravity = !grounded;

            rb.MoveRotation(Quaternion.Euler(0, horizontalRotation, 0));

            Vector3 inputRelativeDirection = transform.TransformDirection(vectorInput.normalized);

            if (Input.GetKey(KeyCode.C) && grounded)
            {
                capsule.height = 1.7f / 2.5f;
                capsule.center = new Vector3(0, -capsule.height / 2f, 0);
            }
            else
            {
                capsule.height = 1.7f;
                capsule.center = Vector3.zero;
            }

            if (grounded)
            {
                if (jumpCooldown <= 0f)
                {
                    Vector3 targetWalkVelocity = new Vector3(inputRelativeDirection.x * moveSpeed, 0, inputRelativeDirection.z * moveSpeed);
                    rb.velocity = Vector3.Lerp(rb.velocity, targetWalkVelocity, Time.fixedDeltaTime * 20f);
                }
            }
            else
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

            if (!grounded && groundedPrevTick)
            {
                coyoteTime = 0.3f;
            }

            groundedPrevTick = grounded;

            if (toJump)
            {
                toJump = false;

                if (coyoteTime > 0f || grounded)
                {
                    coyoteTime = 0f;

                    rb.AddForce(new Vector3(0, 9f, 0), ForceMode.Impulse);

                    jumpCooldown = 0.1f;
                }
            }
        }
    }
}