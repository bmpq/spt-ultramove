using EFT;
using EFT.Ballistics;
using System;
using System.Collections;
using System.Collections.Generic;
using ultramove;
using UnityEngine;

namespace ultramove
{
    internal class Maurice : MonoBehaviour
    {
        public class PlayerBridge : BodyPartCollider.IPlayerBridge
        {
            public event Action<DamageInfoStruct> OnHitAction;

            IPlayer BodyPartCollider.IPlayerBridge.iPlayer => null;

            float BodyPartCollider.IPlayerBridge.WorldTime => 0;

            bool BodyPartCollider.IPlayerBridge.UsingSimplifiedSkeleton => false;

            void BodyPartCollider.IPlayerBridge.ApplyDamageInfo(DamageInfoStruct damageInfo, EBodyPart bodyPartType, EBodyPartColliderType bodyPartCollider, float absorbed)
            {
                OnHitAction?.Invoke(damageInfo);
            }

            ShotInfoClass BodyPartCollider.IPlayerBridge.ApplyShot(DamageInfoStruct damageInfo, EBodyPart bodyPart, EBodyPartColliderType bodyPartCollider, EArmorPlateCollider armorPlateCollider, ShotIdStruct shotId)
            {
                OnHitAction?.Invoke(damageInfo);
                ShotInfoClass shot = new ShotInfoClass();
                shot.Penetrated = true;
                return shot;
            }

            bool BodyPartCollider.IPlayerBridge.CheckArmorHitByDirection(BodyPartCollider bodypart, Vector3 hitpoint, Vector3 shotNormal, Vector3 shotDirection)
            {
                return true;
            }

            bool BodyPartCollider.IPlayerBridge.IsShotDeflectedByHeavyArmor(EBodyPartColliderType colliderType, EArmorPlateCollider armorPlateCollider, int shotSeed)
            {
                return false;
            }

            bool BodyPartCollider.IPlayerBridge.SetShotStatus(BodyPartCollider bodypart, EftBulletClass shot, Vector3 hitpoint, Vector3 shotNormal, Vector3 shotDirection)
            {
                return false;
            }

            bool BodyPartCollider.IPlayerBridge.TryGetArmorResistData(BodyPartCollider bodyPart, float penetrationPower, out ArmorResistanceStruct armorResistanceData)
            {
                armorResistanceData = new ArmorResistanceStruct();
                return false;
            }
        }

        Rigidbody rb;
        GameObject prefabProjectile;

        int projectileShotThisCycle;

        float cooldown;

        private readonly Queue<Projectile> projectilePool = new Queue<Projectile>();

        public static HashSet<Maurice> currentAlive = new HashSet<Maurice>();

        private float health;
        public bool alive => health > 0;

        bool chargingBeam;
        float chargingBeamProgress;

        Light lightChargeBeam;

        BetterSource audioBeam;
        AudioClip clipBeamCharge;

        public BodyPartCollider BallisticCollider { get; private set; }

        public void SetPrefabProjectile(GameObject prefabProjectile)
        {
            this.prefabProjectile = prefabProjectile;
            prefabProjectile.AddComponent<Projectile>();
            prefabProjectile.transform.GetChild(0).gameObject.AddComponent<AlwaysFaceCamera>();
        }

        private void AddNewProjectileToPool()
        {
            Projectile projectile = Instantiate(prefabProjectile).GetOrAddComponent<Projectile>();
            projectilePool.Enqueue(projectile);
        }

        private Projectile GetFromPool()
        {
            if (projectilePool.Count > 0)
            {
                return projectilePool.Dequeue();
            }

            AddNewProjectileToPool();
            return projectilePool.Dequeue();
        }

        private void ReturnToPool(Projectile projectile)
        {
            projectile.gameObject.SetActive(false);
            projectilePool.Enqueue(projectile);
        }

        void Start()
        {
            rb = GetComponent<Rigidbody>();

            rb.useGravity = false;

            BallisticCollider = gameObject.transform.GetChild(1).gameObject.AddComponent<BodyPartCollider>();
            PlayerBridge bridge = new PlayerBridge();
            bridge.OnHitAction += Hit;
            BallisticCollider.playerBridge = bridge;
            BallisticCollider.Collider = BallisticCollider.GetComponent<Collider>();

            lightChargeBeam = GetComponentInChildren<Light>();
            lightChargeBeam.intensity = 0;
            lightChargeBeam.transform.localScale = Vector3.zero;

            health = 400;
            currentAlive.Add(this);

            audioBeam = PlayerAudio.Instance.GetSource();
            clipBeamCharge = PlayerAudio.Instance.GetClip("Throat Drone High Frequency2");
        }

        void Hit(DamageInfoStruct damageInfo)
        {
            if (!alive)
                return;

            health -= damageInfo.Damage;

            if (health <= 0)
                Die();
        }

        void Die()
        {
            health = -1f;
            rb.useGravity = true;
            rb.drag = 1f;

            audioBeam.SetBaseVolume(0f);
            audioBeam.Release();
            lightChargeBeam.gameObject.SetActive(false);

            currentAlive.Remove(this);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (collision.impulse.magnitude > 100f)
            {
                EFTBallisticsInterface.Instance.Hit(collision);
            }
        }

        void Update()
        {
            if (!alive)
                return;

            Vector3 targetDirection = EFTTargetInterface.GetPlayerPosition() - transform.position;
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 100f * Time.deltaTime);

            cooldown -= Time.deltaTime;

            if (chargingBeam)
            {
                chargingBeamProgress += Time.deltaTime;
                float t = chargingBeamProgress / 2f;
                t = Mathf.Clamp01(t);

                lightChargeBeam.intensity = Mathf.Lerp(0, 30f, t);
                lightChargeBeam.transform.localScale = Vector3.one * Mathf.Lerp(0, 2.5f, t);

                if (chargingBeamProgress >= 3f)
                {
                    ShootBeam();
                    lightChargeBeam.intensity = 0f;
                    lightChargeBeam.transform.localScale = Vector3.zero;
                    chargingBeam = false;
                    cooldown = 1f;
                }
                else
                {
                    audioBeam.SetPitch(Mathf.Lerp(0.8f, 2.5f, t));
                }
            }
            else if (cooldown < 0)
            {
                if (EFTTargetInterface.LineOfSight(transform.position, EFTTargetInterface.GetPlayerPosition(), out RaycastHit hit))
                {
                    if (projectileShotThisCycle < 6)
                    {
                        ShootProjectile();

                        projectileShotThisCycle++;

                        cooldown = 0.2f;
                    }
                    else
                    {
                        cooldown = 1f;
                        projectileShotThisCycle = 0;

                        float distToPlayer = Vector3.Distance(EFTTargetInterface.GetPlayerPosition(), transform.position);
                        if (distToPlayer < 15f)
                        {
                            chargingBeamProgress = 0f;
                            chargingBeam = true;

                            audioBeam.SetBaseVolume(1f);
                            audioBeam.Play(clipBeamCharge, null, 1f);
                        }
                    }
                }
            }

            if (!chargingBeam && audioBeam.PlayBackState == BetterSource.EPlayBackState.Playing)
                audioBeam.SetBaseVolume(0f);
        }

        void ShootBeam()
        {
            Vector3 origin = lightChargeBeam.transform.position;

            if (EFTBallisticsInterface.Instance.Shoot(origin, transform.forward, out RaycastHit hit, 100f))
            {
                TrailRendererManager.Instance.Trail(origin, hit.point, Color.red, 3f, 0.4f);

                EFTBallisticsInterface.Instance.Explosion(hit.point);
            }
        }

        void ShootProjectile()
        {
            Projectile projectile = GetFromPool();
            Vector3 startPos = transform.position + transform.forward - transform.up * 1.3f;

            projectile.enabled = true;
            projectile.Initialize(startPos, (EFTTargetInterface.GetPlayerPosition() - startPos).normalized * 40f);

            projectile.OnProjectileDone = ReturnToPool;

            PlayerAudio.Instance.PlayAtPoint("AnimeSlash2xpitch", transform.position);
        }

        void FixedUpdate()
        {
            if (!alive)
                return;

            float distToPlayer = Vector3.Distance(EFTTargetInterface.GetPlayerPosition(), transform.position);
            Vector3 targetVel = Vector3.zero;
            if (distToPlayer > 15f)
                targetVel = (EFTTargetInterface.GetPlayerPosition() - transform.position).normalized;

            if (DistanceToFloor() < 5f)
                targetVel.y = Mathf.Max(targetVel.y, 0);

            if (chargingBeam)
                targetVel = Vector3.zero;

            rb.velocity = targetVel;
        }

        float DistanceToFloor()
        {
            int layer1 = 1 << 18; // LowPolyCollider
            int layer2 = 1 << 11; // Terrain
            int layerMask = layer1 | layer2;

            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 10f, layerMask))
            {
                return Vector3.Distance(transform.position, hit.point);
            }

            return 100f;
        }
    }
}
