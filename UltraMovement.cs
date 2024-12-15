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

        private float moveSpeed = 4f;

        CapsuleCollider capsule;
        private LayerMask groundLayer;

        Collider[] groundHits = new Collider[3];

        bool toJump = false;

        void Start()
        {
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

            rb.position += new Vector3(0, 2f, 0);

            cameraTransform = Camera.main.transform;
            Cursor.lockState = CursorLockMode.Locked;

            groundLayer = LayerMask.NameToLayer("LowPolyCollider");
        }

        void Update()
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            horizontalRotation += mouseX;
            updownRotation = Mathf.Clamp(updownRotation - mouseY, -90f, 90f);

            cameraTransform.position = transform.position + new Vector3(0, 0.8f, 0);
            cameraTransform.rotation = Quaternion.Euler(updownRotation, horizontalRotation, 0);

            if (Input.GetKeyDown(KeyCode.Space))
            {
                toJump = true;
            }

            if (Input.GetKey(KeyCode.LeftControl))
            {
                transform.position += cameraTransform.forward * moveSpeed * Time.deltaTime * 10f;
            }

            coyoteTime -= Time.deltaTime;
        }

        void FixedUpdate()
        {
            rb.MoveRotation(Quaternion.Euler(0, horizontalRotation, 0));

            Vector3 vectorInput = Vector3.zero;

            if (Input.GetKey(KeyCode.A))
                vectorInput.x = -1f;
            if (Input.GetKey(KeyCode.D))
                vectorInput.x = 1f;
            if (Input.GetKey(KeyCode.W))
                vectorInput.z = 1f;
            if (Input.GetKey(KeyCode.S))
                vectorInput.z = -1f;

            Vector3 velocity = transform.TransformDirection(vectorInput.normalized) * moveSpeed;

            rb.velocity = new Vector3(velocity.x, rb.velocity.y, velocity.z);

            if (toJump)
            {
                toJump = false;

                if (coyoteTime > 0f || IsGrounded())
                {
                    rb.AddForce(new Vector3(0, 10f, 0), ForceMode.Impulse);

                    coyoteTime = 0f;
                }
            }
        }

        bool IsGrounded()
        {
            Vector3 spherePosition = transform.position + (Vector3.down * (capsule.height / 2));
            bool grounded = Physics.OverlapSphereNonAlloc(spherePosition, capsule.radius, groundHits, 1 << groundLayer) > 0;

            if (grounded)
            {
                coyoteTime = 0.2f;
            }

            return grounded;
        }

        bool IsTouchingCeiling()
        {
            Vector3 spherePosition = transform.position + (Vector3.up * (capsule.height / 2 + 0.1f));
            float sphereRadius = capsule.radius;
            bool touchingCeiling = Physics.OverlapSphere(spherePosition, sphereRadius, 1 << groundLayer).Length > 0;

            return touchingCeiling;
        }

        bool GoingDownHill(Vector2 inputHorizontal)
        {
            int layerMask = 1 << 18;
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
            int layerMask = 1 << 18;
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