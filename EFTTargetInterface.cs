using Comfort.Common;
using EFT;
using EFT.Ballistics;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            float closestDist = Mathf.Infinity;
            BallisticCollider closestTarget = null;
            RaycastHit closestHit  = new RaycastHit();

            foreach (Player player in Singleton<GameWorld>.Instance.AllAlivePlayersList)
            {
                if (player.IsYourPlayer)
                    continue;

                float distance = Vector3.Distance(source.position, player.Position);

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

            foreach (UltraEnemy enemy in UltraEnemy.currentAlive.OrderBy(x => Vector3.Distance(source.position, x.transform.position)))
            {
                foreach (BodyPartCollider bodyPart in enemy.ballisticColliders)
                {
                    Vector3 hitPoint = bodyPart.Collider.ClosestPoint(source.position);

                    if (!LineOfSight(source.position, hitPoint, out RaycastHit hit))
                        continue;

                    float distance = Vector3.Distance(source.position, hitPoint);

                    if (distance < closestDist)
                    {
                        closestDist = distance;
                        closestTarget = bodyPart;
                        closestHit = new RaycastHit();
                        closestHit.point = hitPoint;
                        closestHit.normal = (source.position - hitPoint).normalized;
                    }

                    if (bodyPart.gameObject.tag == "AimPoint")
                        return (closestTarget, closestHit);
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
            float maxDist = 10f;

            foreach (Player player in Singleton<GameWorld>.Instance.AllAlivePlayersList)
            {
                if (player.IsYourPlayer)
                    continue;

                if (Vector3.Distance(player.Position, pos) > maxDist)
                    continue;

                player.MovementContext.SetPoseLevel(1f, true);
                player.gameObject.GetComponent<BotOwner>().AimingData.SetTarget(player.Transform.position + new Vector3(0, 50f, 0));
                player.MovementContext.PlayerAnimatorEnableJump(true);

                if (player.MainParts.TryGetValue(BodyPartType.leftLeg, out EnemyPart part))
                {
                    int layerMask = 1 << 16;
                    RaycastHit[] hits = Physics.RaycastAll(pos, (part.Position - pos).normalized, maxDist, layerMask);

                    foreach (var hit in hits)
                    {
                        if (hit.collider.TryGetComponent<BodyPartCollider>(out BodyPartCollider bodyPartCollider))
                        {
                            if (!bodyPartCollider.Player.IsYourPlayer)
                                continue;

                            EFTBallisticsInterface.Instance.Hit(bodyPartCollider, hit, 90f);

                            break;
                        }
                    }
                }
            }
        }

        public static Transform GetAutoAimTarget(Vector3 source, Vector3 dir, float autoAimAngle)
        {
            Transform bestTarget = null;

            float closestDistance = Mathf.Infinity;
            foreach (Player player in Singleton<GameWorld>.Instance.AllAlivePlayersList)
            {
                if (player.IsYourPlayer)
                    continue;

                Transform tr = player.PlayerBones.Spine3.Original;

                Vector3 directionToTarget = (tr.position - source).normalized;
                float angleToTarget = Vector3.Angle(dir, directionToTarget);

                if (angleToTarget < autoAimAngle)
                {
                    if (!LineOfSight(source, player.PlayerBones.Spine3.position, out RaycastHit hit))
                        continue;

                    float distanceToTarget = directionToTarget.magnitude;
                    if (distanceToTarget < closestDistance)
                    {
                        closestDistance = distanceToTarget;
                        bestTarget = tr;
                    }
                }
            }

            return bestTarget;
        }
    }
}