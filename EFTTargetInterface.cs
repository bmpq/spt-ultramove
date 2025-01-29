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

        static void UpdateEFTJumpHeight()
        {
            AnimationCurve newCurve = new AnimationCurve(new Keyframe(0, Plugin.GroundSlamInfluence.Value), new Keyframe(1f, Plugin.GroundSlamInfluence.Value));
            newCurve.postWrapMode = WrapMode.Loop;
            newCurve.preWrapMode = WrapMode.Loop;
            EFTHardSettings.Instance.LIFT_VELOCITY_BY_SPEED = newCurve;

            AnimationCurve curveZero = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1f, 0));
            newCurve.postWrapMode = WrapMode.Loop;
            newCurve.preWrapMode = WrapMode.Loop;
            EFTHardSettings.Instance.JUMP_DELAY_BY_SPEED = curveZero;
        }

        public static void Slam(Vector3 pos)
        {
            UpdateEFTJumpHeight();

            float maxDist = 10f;

            foreach (Player player in Singleton<GameWorld>.Instance.AllAlivePlayersList)
            {
                if (player.IsYourPlayer)
                    continue;

                if (Vector3.Distance(player.Position, pos) > maxDist)
                    continue;

                player.MovementContext.SetPoseLevel(1f, true);
                player.MovementContext.PlayerAnimatorEnableJump(true);
            }
        }

        public static Transform GetAutoAimTarget(Vector3 source, Vector3 dir, float autoAimAngle)
        {
            Transform bestTarget = null;

            float closestDistance = Mathf.Infinity;
            foreach (BotOwner bot in GetAliveBots())
            {
                if (bot.BotsGroup.InitialBotType == WildSpawnType.shooterBTR)
                    continue;

                Transform tr = bot.PlayerBones.Spine3.Original;

                Vector3 directionToTarget = (tr.position - source).normalized;
                float angleToTarget = Vector3.Angle(dir, directionToTarget);

                if (angleToTarget < autoAimAngle)
                {
                    if (!LineOfSight(source, tr.position, out RaycastHit hit))
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

        static List<BotOwner> GetAliveBots()
        {
            IEnumerable<BotOwner> bots = ((IBotGame)Singleton<AbstractGame>.Instance).BotsController.Bots.BotOwners;
            return bots.Where(bot => bot.HealthController.IsAlive).ToList();
        }
    }
}