using System.Collections;
using System.Collections.Generic;
using ultramove;
using UnityEngine;

namespace ultramove
{
    internal class Maurice : MonoBehaviour
    {
        Rigidbody rb;
        GameObject prefabProjectile;

        int projectileShotThisCycle;

        float cooldown;

        private readonly Queue<Projectile> projectilePool = new Queue<Projectile>();

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
        }

        void Update()
        {
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
        }

        void FixedUpdate()
        {
            float distToPlayer = Vector3.Distance(EFTTargetInterface.GetPlayerPosition(), transform.position);
            Vector3 targetVel = Vector3.zero;
            if (distToPlayer > 15f)
                targetVel = (EFTTargetInterface.GetPlayerPosition() - transform.position).normalized;

            if (DistanceToFloor() < 5f)
                targetVel.y = Mathf.Max(targetVel.y, 0);

            Debug.Log(DistanceToFloor());

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
