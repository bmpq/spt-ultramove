using EFT.Ballistics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ultramove
{
    public class Coin : MonoBehaviour
    {
        Rigidbody rb;

        TrailRenderer trailRenderer;

        Collider[] colliders;

        float delayColliderActivation;

        public bool active { get; private set; }

        bool init;

        RaycastHit[] hits = new RaycastHit[4];

        void Init()
        {
            rb = GetComponent<Rigidbody>();
            init = true;
            trailRenderer = GetComponent<TrailRenderer>();

            List<Collider> list = new List<Collider>();
            foreach (Collider collider in GetComponentsInChildren<Collider>())
            {
                if (!collider.isTrigger)
                    list.Add(collider);
            }
            colliders = list.ToArray();
        }

        public void Activate()
        {
            if (!init)
                Init();

            active = true;
            delayColliderActivation = 0.2f;
            trailRenderer.emitting = true;
            trailRenderer.Clear();
        }

        public void Hit(RaycastHit incomingHit)
        {
            active = false;

            float rayDistance = 500f;

            Vector3 rayDir = transform.up;

            Vector3 trailEndPoint = transform.position + rayDir * 10f;

            Ray ray = new Ray(transform.position, rayDir);

            int layer12 = 1 << 12; // HighPolyCollider
            int layer16 = 1 << 16; // HitCollider (body parts)
            int layer11 = 1 << 11; // Terrain
            int layerMask = layer12 | layer16 | layer11;

            int hitsAmount = Physics.RaycastNonAlloc(ray, hits, rayDistance, layerMask);
            for (int i = 0; i < hitsAmount; i++)
            {
                RaycastHit hit = hits[i];

                MaterialType matHit = MaterialType.None;

                if (hit.rigidbody == rb)
                    continue;

                trailEndPoint = hit.point;

                if (hit.transform.tag == "DynamicCollider")
                {
                    if (hit.rigidbody.TryGetComponent<Coin>(out Coin coin))
                    {
                        coin.Hit(hit);
                        trailEndPoint = coin.transform.position;
                    }
                }
                else
                {
                    matHit = EFTBallisticsInterface.Instance.Hit(hit);
                }

                break;

                //if (matHit == MaterialType.Body || matHit == MaterialType.BodyArmor)
                //    particles.PlayBloodEffect(hit.point, hit.normal);
            }

            TrailRendererManager.Instance.Trail(transform.position, trailEndPoint);
        }

        private void Update()
        {
            if (delayColliderActivation > 0f)
                delayColliderActivation -= Time.deltaTime;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (delayColliderActivation > 0f)
                return;
            Disable();
        }

        void Disable()
        {
            active = false;
            trailRenderer.emitting = false;
            trailRenderer.Clear();
        }
    }
}