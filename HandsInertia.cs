using UnityEngine;

namespace ultramove
{
    public class HandsInertia : MonoBehaviour
    {
        Camera cam;
        Transform ribcage;

        Rigidbody rb;

        Vector3 inertia;

        private void Start()
        {
            cam = Camera.main;
            ribcage = transform.FindInChildrenExact("Base HumanRibcage");

            ribcage.localScale = new Vector3(1f, 1f, 0.8f);

            rb = GetComponent<Rigidbody>();
        }

        private void LateUpdate()
        {
            inertia = Vector3.Lerp(inertia, -rb.velocity * Time.fixedDeltaTime * 0.5f, Time.deltaTime * 6f);

            ribcage.position = cam.transform.TransformPoint(new Vector3(0, -0.1f, 0)) + inertia;
            ribcage.rotation = Quaternion.Lerp(ribcage.rotation, cam.transform.rotation, Time.deltaTime * 10f);
        }
    }
}