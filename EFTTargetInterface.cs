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
        static Camera cam;

        public static Vector3 GetPlayerPosition()
        {
            if (cam == null)
                cam = Camera.main;

            return cam.transform.position;

            return Singleton<GameWorld>.Instance.MainPlayer.Position;
        }

        public static (BallisticCollider, RaycastHit) GetCoinTarget(Transform source, BallisticCollider exclude)
        {
            float distLimit = 500f;
            float closestDist = Mathf.Infinity;
            BallisticCollider closestTarget = null;
            RaycastHit closestHit  = new RaycastHit();

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

                    if (exclude == collider)
                        continue;

                    if (!LineOfSight(source.position, collider.transform.position, out RaycastHit hit))
                        continue;

                    if (distance < closestDist)
                    {
                        closestDist = distance;
                        closestTarget = collider;
                        closestHit = new RaycastHit();
                        closestHit.point = headPart.Collider.Collider.ClosestPoint(source.position);
                        closestHit.normal = (source.position - headPart.Position).normalized;
                    }
                }
            }

            foreach (Maurice maurice in Maurice.currentAlive)
            {
                if (!LineOfSight(source.position, maurice.transform.position, out RaycastHit hit))
                    continue;

                float distance = Vector3.Distance(source.position, maurice.transform.position);

                if (distance < closestDist)
                {
                    closestDist = distance;
                    closestTarget = maurice.BallisticCollider;
                    closestHit = new RaycastHit();
                    closestHit.point = maurice.BallisticCollider.Collider.ClosestPoint(source.position);
                    closestHit.normal = (source.position - maurice.transform.position).normalized;
                }
            }

            return (closestTarget, closestHit);
        }

        static RaycastHit[] hits = new RaycastHit[5];

        public static bool LineOfSight(Vector3 source, Vector3 target, out RaycastHit hitTarget)
        {
            hitTarget = new RaycastHit();

            const float margin = 0.5f;
            Vector3 direction = (target - source).normalized;
            float distance = Vector3.Distance(source, target);

            Ray ray = new Ray(source, direction.normalized);

            int layer12 = 1 << 12; // HighPolyCollider
            int layer11 = 1 << 11; // Terrain
            int layerMask = layer12 | layer11;

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

        public static void Slam(Vector3 pos)
        {
            float maxDist = 5f;

            foreach (Player player in Singleton<GameWorld>.Instance.AllAlivePlayersList)
            {
                if (player.IsYourPlayer)
                    continue;

                if (Vector3.Distance(player.Position, pos) > maxDist)
                    continue;

                player.Jump();

                if (player.MainParts.TryGetValue(BodyPartType.leftLeg, out EnemyPart part))
                {
                    int layerMask = 1 << 16;
                    RaycastHit[] hits = Physics.RaycastAll(pos, (part.Position - pos).normalized, maxDist, layerMask);

                    foreach (var hit in hits)
                    {
                        if (hit.collider.TryGetComponent<BodyPartCollider>(out BodyPartCollider bodyPartCollider))
                        {
                            if (bodyPartCollider.Player == Singleton<GameWorld>.Instance.MainPlayer)
                                continue;

                            EFTBallisticsInterface.Instance.Hit(bodyPartCollider, hit, 90f);

                            break;
                        }
                    }
                }
            }
        }
    }
}