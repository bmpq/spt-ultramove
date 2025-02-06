using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.Interactive;
using EFT.InventoryLogic;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ultramove
{
    public class Coin : MonoBehaviour, IParryable
    {
        Rigidbody rb;

        TrailRenderer trailRenderer;
        Collider[] colliders;

        MeshRenderer meshRenderer;

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

        bool parried;

        public bool animVending;

        void Init()
        {
            meshRenderer = GetComponentInChildren<MeshRenderer>();

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
            gameObject.SetActive(true);

            if (!init)
                Init();

            meshRenderer.enabled = true;
            rb.isKinematic = false;
            activeCoins.Add(this);
            active = true;
            timeActive = 0f;
            lightGlint.intensity = 0f;
            trailRenderer.emitting = true;
            trailRenderer.Clear();
            parried = false;
        }

        public void Freeze()
        {
            meshRenderer.enabled = false;
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        public void Hit(float dmg, bool split = false, bool rail = false)
        {
            Freeze();

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

                if (!split || rail)
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
                        EFTBallisticsInterface.Instance.Hit(transform.position, hit, dmg);
                        Trail(transform.position, hit.point, rail);
                    }
                }
                else
                {
                    RaycastHit[] hits = EFTBallisticsInterface.Instance.Shoot(transform.position, (target.Item2.point - transform.position).normalized, dmg, rail);

                    CheckIfKilled(target.Item1);

                    Trail(transform.position, target.Item2.point, rail);

                    alreadyHit = target.Item1;
                }
            }
        }

        void CheckIfKilled(BallisticCollider ballisticCollider)
        {
            if (ballisticCollider is BodyPartCollider bpc)
            {
                bool kill = false;
                if (bpc.playerBridge is UltraPlayerBridge upb)
                {
                    if (!upb.UltraEnemy.alive)
                        kill = true;
                }
                else if (!bpc.playerBridge.iPlayer.HealthController.IsAlive)
                {
                    kill = true;
                }

                if (kill)
                    Singleton<UltraTime>.Instance.Freeze(0.1f, 0.2f);
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
            yield return new WaitForSeconds(0.1f);
            hitCoin.Hit(dmg, split, rail);

            Trail(transform.position, hitCoin.transform.position, rail);
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

        void FixedUpdate()
        {
            if (rb.useGravity)
            {
                rb.AddForce(Physics.gravity * 3f, ForceMode.Acceleration);
            }
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

            return timeActive > SPLITWINDOWSTART && timeActive < (SPLITWINDOWSTART + SPLITWINDOWSIZE);
        }

        public void Parry(Transform source)
        {
            rb.velocity = source.forward * 100f;
            parried = true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (animVending)
                return;

            if (parried)
            {
                EFTBallisticsInterface.Instance.Hit(transform.position, collision, 300f);
                EFTBallisticsInterface.Instance.Explosion(transform.position);
                parried = false;
                Disable();
            }

            if (collision.transform.parent != null && collision.transform.parent.name == "Coke_automate_update")
            {
                StartCoroutine(AnimVending(collision.transform.parent));
                return;
            }

            if (timeActive < 0.2f)
                return;

            Disable();
            gameObject.SetActive(false);
        }

        IEnumerator AnimVending(Transform trVendingMachine)
        {
            animVending = true;

            trailRenderer.emitting = false;
            trailRenderer.Clear();
            rb.isKinematic = true;

            float t = 0f;

            Vector3 startPos = trVendingMachine.position + new Vector3(0.69f, 1.16f, 0.54f);
            Vector3 endPos = startPos + new Vector3(-0.20f, 0, 0);

            Quaternion initRot = transform.rotation;
            Vector3 initPos = transform.position;
            while (t < 1f)
            {
                t = Mathf.Clamp01(t + Time.deltaTime * 6f);

                float e = EasingFunction.EaseOutElastic(0, 1f, t);

                transform.position = Vector3.Lerp(initPos, startPos, e);
                transform.rotation = Quaternion.Lerp(initRot, Quaternion.Euler(90f, 0, 0), e);

                yield return null;
            }

            t = 0f;
            while (t < 1f)
            {
                t = Mathf.Clamp01(t + Time.deltaTime * 2f);

                float e = EasingFunction.EaseInCubic(t);

                transform.position = Vector3.Lerp(startPos, endPos, e);

                lightGlint.intensity = 0;

                yield return null;
            }

            yield return new WaitForSeconds(0.5f);

            Item newItem = Singleton<ItemFactoryClass>.Instance.CreateItem(MongoID.Generate(false), "57514643245977207f2c2d09", null);
            GameObject itemObject = Singleton<PoolManager>.Instance.CreateItem(newItem, false);
            itemObject.transform.parent = null;
            itemObject.transform.position = trVendingMachine.position + new Vector3(0.6f, 0.4f, 0.05f);
            itemObject.transform.rotation = Quaternion.Euler(90f, 0, 0);
            itemObject.GetComponent<ObservedLootItem>().MakePhysicsObject(true);
            itemObject.layer = 12;
            itemObject.SetActive(true);

            Rigidbody newItemRb = itemObject.GetComponent<Rigidbody>();

            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 8f;

                newItemRb.position += new Vector3(4f, 0, 0) * Time.deltaTime;

                yield return null;
            }
            newItemRb.isKinematic = false;
            newItemRb.AddForce(new Vector3(5f, 0, 0), ForceMode.VelocityChange);
            newItemRb.AddRelativeTorque(new Vector3(Mathf.Lerp(-1f, 1f, Random.value) * 1000f, 0, 1000f), ForceMode.VelocityChange);

            animVending = false;
            Disable();
            gameObject.SetActive(false);
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