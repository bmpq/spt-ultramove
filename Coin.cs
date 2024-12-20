using EFT.Ballistics;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ultramove
{
    public class Coin : MonoBehaviour
    {
        Rigidbody rb;

        TrailRenderer trailRenderer;

        Collider[] colliders;

        float timeActive;

        public bool active { get; private set; }

        bool init;

        public static HashSet<Coin> activeCoins = new HashSet<Coin>();

        RaycastHit[] hits = new RaycastHit[4];

        Color colorTrail = new Color(1, 0.9f, 0.4f);

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

            activeCoins.Add(this);
            active = true;
            timeActive = 0f;
            trailRenderer.emitting = true;
            trailRenderer.Clear();
        }

        public void Hit(float dmg, bool split = false)
        {
            dmg *= 2f;

            split = IsOnApex();

            Disable();

            if (activeCoins.Count > 0)
            {
                Coin hitCoin = activeCoins.FirstOrDefault();
                hitCoin.Hit(dmg);

                TrailRendererManager.Instance.Trail(transform.position, hitCoin.transform.position, colorTrail);

                if (!split)
                    return;

                split = false;
            }

            for (int i = 0; i < (split ? 2 : 1); i++)
            {
                (BallisticCollider, RaycastHit) target = EFTTargetInterface.GetCoinTarget(transform);

                if (target.Item1 == null)
                {
                    if (Raycast(transform, out RaycastHit hit))
                    {
                        EFTBallisticsInterface.Instance.Hit(hit, dmg);
                        TrailRendererManager.Instance.Trail(transform.position, hit.point, colorTrail);
                    }
                }
                else
                {
                    MaterialType matHit = EFTBallisticsInterface.Instance.Hit(target.Item1, target.Item2, dmg);
                    TrailRendererManager.Instance.Trail(transform.position, target.Item2.point, colorTrail);
                    if (matHit == MaterialType.Body || matHit == MaterialType.BodyArmor)
                        ParticleEffectManager.Instance.PlayBloodEffect(target.Item2.point, target.Item2.normal);
                }
            }
        }

        bool Raycast(Transform transform, out RaycastHit hit)
        {
            float rayDistance = 500f;

            Vector3 rayDir = Random.onUnitSphere;

            Vector3 trailEndPoint = transform.position + rayDir * 10f;

            Ray ray = new Ray(transform.position, rayDir);

            int layer12 = 1 << 12; // HighPolyCollider
            int layer16 = 1 << 16; // HitCollider (body parts)
            int layer11 = 1 << 11; // Terrain
            int layerMask = layer12 | layer16 | layer11;

            int hitsAmount = Physics.RaycastNonAlloc(ray, hits, rayDistance, layerMask);
            for (int i = 0; i < hitsAmount; i++)
            {
                if (hits[i].rigidbody == rb)
                    continue;

                hit = hits[i];
                return true;
            }

            hit = new RaycastHit();
            return false;
        }

        private void Update()
        {
            timeActive += Time.deltaTime;
        }

        public bool IsOnApex()
        {
            return timeActive > 0.33f && timeActive < 0.38f;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (timeActive < 0.2f)
                return;

            Disable();
        }

        void Disable()
        {
            active = false;
            trailRenderer.emitting = false;
            trailRenderer.Clear();

            if (activeCoins.Contains(this))
                activeCoins.Remove(this);
        }

        private void OnDisable()
        {
            Disable();
        }
    }
}