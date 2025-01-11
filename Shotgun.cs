using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ultramove
{
    internal static class Shotgun
    {
        private static int initialPoolSize = 20;
        private static float projectileSpeed = 90f;

        private static readonly Queue<Projectile> projectilePool = new Queue<Projectile>();
        private static Material matTrail;

        private static void AddNewProjectileToPool()
        {
            if (matTrail == null)
            {
                matTrail = new Material(Shader.Find("Sprites/Default"));
            }

            GameObject newProjectile = new GameObject("UltraProjectile");
            newProjectile.layer = 13;
            newProjectile.AddComponent<SphereCollider>().radius = 0.05f;
            Rigidbody newProjectileRb = newProjectile.AddComponent<Rigidbody>();
            newProjectileRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            newProjectileRb.interpolation = RigidbodyInterpolation.Interpolate;
            TrailRenderer trail = newProjectile.AddComponent<TrailRenderer>();
            trail.emitting = false;
            trail.sharedMaterial = matTrail;
            trail.time = 0.15f;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.startWidth = 0.08f;
            trail.endWidth = trail.startWidth;
            trail.startColor = new Color(1, 1, 1, 0.31f);
            trail.endColor = new Color(1, 1, 1, 0);
            Projectile projectile = newProjectile.AddComponent<Projectile>();
            projectilePool.Enqueue(projectile);
        }

        private static Projectile GetFromPool()
        {
            if (projectilePool.Count > 0)
            {
                return projectilePool.Dequeue();
            }

            AddNewProjectileToPool();
            return projectilePool.Dequeue();
        }

        private static void ReturnToPool(Projectile projectile)
        {
            projectile.Disable();
            projectilePool.Enqueue(projectile);
        }

        public static void ShootProjectiles(Vector3 origin, Vector3 dir, Vector3 addVelocity)
        {
            int shot = 9;

            for (int i = 0; i < shot; i++)
            {
                Projectile projectile = GetFromPool();

                // Determine spread direction
                Vector3 spreadDir = CalculateSpreadDirection(dir, 10f);
                Vector3 startVel = addVelocity + spreadDir * projectileSpeed;

                projectile.enabled = true;
                projectile.Initialize(origin, startVel);

                projectile.OnProjectileDone = ReturnToPool;
            }
        }

        static Vector3 CalculateSpreadDirection(Vector3 baseDirection, float maxAngle)
        {
            Quaternion randomRotation = Quaternion.Euler(
                UnityEngine.Random.Range(-maxAngle, maxAngle),
                UnityEngine.Random.Range(-maxAngle, maxAngle),
                0
            );

            return randomRotation * baseDirection;
        }
    }
}
