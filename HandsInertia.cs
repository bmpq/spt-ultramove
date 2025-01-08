using UnityEngine;

namespace ultramove
{
    public class HandsInertia : MonoBehaviour
    {
        Camera cam;
        Transform ribcage;

        Rigidbody rb;

        Vector3 inertia;

        Vector3 prevVelocity;
        Vector3 velocityDelta;

        Vector3 inertiaSmoothed;

        Vector2 sway;

        GroundCheck groundCheck;
        UltraMovement ultraMovement;

        private void Start()
        {
            cam = Camera.main;
            ribcage = transform.FindInChildrenExact("Base HumanRibcage");

            ribcage.localScale = new Vector3(1f, 1f, 0.8f);

            rb = GetComponent<Rigidbody>();
        }

        void GetRefs()
        {
            groundCheck = GetComponentInChildren<GroundCheck>();
            ultraMovement = GetComponent<UltraMovement>();
        }

        private void LateUpdate()
        {
            if (groundCheck == null || ultraMovement == null)
            {
                GetRefs();
                if (groundCheck == null || ultraMovement == null)
                    return;
            }

            inertia = Vector3.Lerp(inertia, Vector3.zero, Time.deltaTime * 5f);
            inertiaSmoothed = Vector3.Lerp(inertiaSmoothed, inertia, Time.deltaTime * 10f);

            sway.y = Mathf.Cos(Time.unscaledTime * 2f) * 0.01f;

            if (groundCheck.isGrounded && !ultraMovement.sliding)
                sway.x = Mathf.Lerp(sway.x, Mathf.Sin(Time.unscaledTime * 8f) * 0.03f * rb.velocity.magnitude, Time.deltaTime);
            else
                sway.x = Mathf.Lerp(sway.x, 0f, Time.deltaTime);

            ribcage.position = cam.transform.TransformPoint(new Vector3(0, -0.1f, 0)) + inertiaSmoothed * 0.01f;
            ribcage.position = cam.transform.TransformPoint(new Vector3(sway.x, sway.y, 0) + cam.transform.InverseTransformPoint(ribcage.position));
            ribcage.rotation = cam.transform.rotation;
        }

        void FixedUpdate()
        {
            velocityDelta = rb.velocity - prevVelocity;
            inertia -= velocityDelta;
            prevVelocity = rb.velocity;
        }
    }
}