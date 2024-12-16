using UnityEngine;

namespace ultramove
{
    public class UltraMovement : MonoBehaviour
    {
        private Transform cameraTransform;
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

        void Start()
        {
            cameraTransform = Camera.main.transform;
            Cursor.lockState = CursorLockMode.Locked;

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
            sphere.radius = capsule.radius;
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

            cameraTransform.position = transform.position + new Vector3(0, 0.5f, 0);
            cameraTransform.rotation = Quaternion.Euler(updownRotation, horizontalRotation, 0);

            jumpCooldown -= Time.deltaTime;

            if (Input.GetKeyDown(KeyCode.Space) && jumpCooldown <= 0f)
            {
                toJump = true;
            }

            coyoteTime -= Time.deltaTime;
        }


        void FixedUpdate()
        {
            bool grounded = groundCheck.isGrounded;
            rb.useGravity = !grounded;

            rb.MoveRotation(Quaternion.Euler(0, horizontalRotation, 0));

            Vector3 inputRelativeDirection = transform.TransformDirection(vectorInput.normalized);

            if (grounded)
            {
                Vector3 targetWalkVelocity = new Vector3(inputRelativeDirection.x * moveSpeed, Mathf.Max(0, rb.velocity.y), inputRelativeDirection.z * moveSpeed);
                rb.velocity = Vector3.Lerp(rb.velocity, targetWalkVelocity, Time.fixedDeltaTime * 20f);
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

        bool IsTouchingCeiling()
        {
            Vector3 spherePosition = transform.position + (Vector3.up * (capsule.height / 2 + 0.1f));
            float sphereRadius = capsule.radius;
            bool touchingCeiling = Physics.OverlapSphere(spherePosition, sphereRadius, groundLayer).Length > 0;

            return touchingCeiling;
        }

        bool GoingDownHill(Vector2 inputHorizontal)
        {
            int layerMask = 1 << groundLayer;
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, capsule.height, layerMask))
            {
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                if (slopeAngle > 10f)
                {
                    Vector3 moveDirection = new Vector3(inputHorizontal.x, 0f, inputHorizontal.y);
                    moveDirection = transform.TransformDirection(moveDirection);
                    float downhillCheck = Vector3.Dot(moveDirection, hit.normal);
                    if (downhillCheck > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        void GroundClamp()
        {
            int layerMask = 1 << groundLayer;
            float targetDistanceToGround = capsule.height / 2f + capsule.radius / 2f;
            targetDistanceToGround -= 0.1f; // yeah i dont know

            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, targetDistanceToGround + 0.3f, layerMask))
            {
                Vector3 newPosition = transform.position;
                newPosition.y = hit.point.y + targetDistanceToGround;
                transform.position = newPosition;
            }
        }
    }
}