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

            BodyPartCollider ballistic = gameObject.transform.GetChild(1).gameObject.AddComponent<BodyPartCollider>();
            PlayerBridge bridge = new PlayerBridge();
            bridge.OnHitAction += Hit;
            ballistic.playerBridge = bridge;

            health = 400;
            currentAlive.Add(this);
        }

        void Hit(DamageInfoStruct damageInfo)
        {
            Plugin.Log.LogInfo(damageInfo.Damage);

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

            if (cooldown < 0)
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
                }
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
