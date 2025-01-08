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

        private void Start()
        {
            cam = Camera.main;
            ribcage = transform.FindInChildrenExact("Base HumanRibcage");

            ribcage.localScale = new Vector3(1f, 1f, 0.8f);

            rb = GetComponent<Rigidbody>();
        }

        private void LateUpdate()
        {
            inertia = Vector3.Lerp(inertia, Vector3.zero, Time.deltaTime * 5f);
            inertiaSmoothed = Vector3.Lerp(inertiaSmoothed, inertia, Time.deltaTime * 10f);

            ribcage.position = cam.transform.TransformPoint(new Vector3(0, -0.1f, 0)) + inertiaSmoothed * 0.01f;
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