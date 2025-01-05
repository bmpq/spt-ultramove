using Comfort.Common;
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

        Projectile projectile;

        Collider[] colliders;

        public float timeActive { get; private set; }

        public const float SPLITWINDOWSTART = 0.33f;
        public const float SPLITWINDOWSIZE = 0.05f;

        public bool active { get; private set; }

        bool init;

        public static HashSet<Coin> activeCoins = new HashSet<Coin>();

        RaycastHit[] hits = new RaycastHit[4];

        Color colorTrail = new Color(1, 0.4f, 0f);

        static float timeLastRicoshot;
        static int sequenceRicoshot;

        public Light lightGlint { get; private set; }

        void Init()
        {
            projectile = GetComponent<Projectile>();

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

            lightGlint = gameObject.GetOrAddComponent<Light>();
            lightGlint.color = colorTrail;
            lightGlint.shadows = LightShadows.None;
        }

        public void Activate()
        {
            if (!init)
                Init();

            rb.isKinematic = false;
            projectile.enabled = false;
            activeCoins.Add(this);
            active = true;
            timeActive = 0f;
            lightGlint.intensity = 0f;
            trailRenderer.emitting = true;
            trailRenderer.Clear();
        }

        public void Freeze()
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        public void Hit(float dmg, bool split = false, bool rail = false)
        {
            dmg *= 2f;

            if (!split)
                split = IsOnApex();

            if (Time.time - timeLastRicoshot < 0.2f)
                sequenceRicoshot++;
            else
                sequenceRicoshot = 0;
            PlayerAudio.Instance.Play("Ricochet", 1f);
            ParticleEffectManager.Instance.PlayEffectCoinRicochet(transform);
            timeLastRicoshot = Time.time;

            Disable();

            if (rail)
                Singleton<UltraTime>.Instance.Freeze(0, 0.1f);

            if (activeCoins.Count > 0)
            {
                Coin hitCoin = activeCoins.OrderBy(c => Vector3.Distance(transform.position, c.transform.position)).FirstOrDefault();
                StartCoroutine(DelayHit(hitCoin, dmg, split, rail));

                if (!split)
                    return;

                split = false;
            }

            BallisticCollider alreadyHit = null;

            for (int i = 0; i < (split ? 2 : 1); i++)
            {
                (BallisticCollider, RaycastHit) target = EFTTargetInterface.GetCoinTarget(transform, alreadyHit);

                if (target.Item1 == null)
                {
                    if (RaycastInRandomDir(transform, out RaycastHit hit))
                    {
                        EFTBallisticsInterface.Instance.Hit(hit, dmg);
                        Trail(transform.position, hit.point, rail);
                    }
                }
                else
                {
                    MaterialType matHit = EFTBallisticsInterface.Instance.Hit(target.Item1, target.Item2, dmg);
                    Trail(transform.position, target.Item2.point, rail);

                    alreadyHit = target.Item1;
                }
            }
        }

        private void Trail(Vector3 a, Vector3 b, bool rail)
        {
            if (rail)
                TrailRendererManager.Instance.Trail(a, b, true);
            else
                TrailRendererManager.Instance.Trail(a, b, colorTrail, 0.1f);
        }

        private IEnumerator DelayHit(Coin hitCoin, float dmg, bool split, bool rail)
        {
            hitCoin.Freeze();

            yield return new WaitForSeconds(0.1f);
            hitCoin.Hit(dmg, split, rail);

            Trail(transform.position, hitCoin.transform.position, rail);

            Singleton<UltraTime>.Instance.Freeze(0.05f, 0.2f);
        }

        bool RaycastInRandomDir(Transform transform, out RaycastHit hit)
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
            if (!active)
                return;

            bool prevApex = IsOnApex();

            timeActive += Time.deltaTime;

            if (IsOnApex() && !prevApex)
                PlayerAudio.Instance.Play("coinflash");
        }

        public bool IsOnApex()
        {
            if (!active)
                return false;

            return timeActive > SPLITWINDOWSTART && timeActive < SPLITWINDOWSTART + SPLITWINDOWSIZE;
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
            lightGlint.intensity = 0f;

            if (activeCoins.Contains(this))
                activeCoins.Remove(this);
        }

        private void OnDisable()
        {
            Disable();
        }
    }
}