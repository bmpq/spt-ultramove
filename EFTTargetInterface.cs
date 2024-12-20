using Comfort.Common;
using EFT;
using EFT.Ballistics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ultramove
{
    public static class EFTTargetInterface
    {
        public static (BallisticCollider, RaycastHit) GetCoinTarget(Transform source)
        {
            float distLimit = 100f;
            float closestDist = Mathf.Infinity;
            BallisticCollider closestTarget = null;
            RaycastHit closestHit = new RaycastHit();

            foreach (Player player in Singleton<GameWorld>.Instance.AllAlivePlayersList)
            {
                if (player.IsYourPlayer)
                    continue;

                float distance = Vector3.Distance(source.position, player.Position);
                if (distance > distLimit)
                    continue;

                if (player.MainParts.TryGetValue(BodyPartType.head, out var headPart))
                {
                    BallisticCollider collider = headPart.Collider;

                    if (!LineOfSight(source.position, collider.transform.position, out RaycastHit hit))
                        continue;

                    if (distance < closestDist)
                    {
                        closestDist = distance;
                        closestTarget = collider;
                        closestHit = hit;
                    }
                }
            }

            return (closestTarget, closestHit);
        }

        static RaycastHit[] hits = new RaycastHit[5];

        static bool LineOfSight(Vector3 source, Vector3 target, out RaycastHit hitTarget)
        {
            hitTarget = new RaycastHit();

            const float margin = 0.5f;
            Vector3 direction = (target - source).normalized;
            float distance = Vector3.Distance(source, target);

            Ray ray = new Ray(source, direction.normalized);

            int layer12 = 1 << 12; // HighPolyCollider
            int layer16 = 1 << 16; // HitCollider (body parts)
            int layer11 = 1 << 11; // Terrain
            int layerMask = layer12 | layer16 | layer11;

            int hitsAmount = Physics.RaycastNonAlloc(source, direction, hits, distance, layerMask);

            for (int i = 0; i < hitsAmount; i++)
            {
                RaycastHit hit = hits[i];
                // Ignore hits too close to the source
                if (hit.distance <= margin) continue;

                // Ignore hits too close to the target
                if (hit.distance >= distance - margin)
                {
                    hitTarget = hit;
                    continue;
                }

                return false;
            }

            return true;
        }
    }
}