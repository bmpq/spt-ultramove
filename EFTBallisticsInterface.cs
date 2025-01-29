using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.Interactive;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Systems.Effects;
using UnityEngine;

namespace ultramove
{
    internal class EFTBallisticsInterface
    {
        public static EFTBallisticsInterface Instance;

        private IPlayerOwner player;

        Effects.Effect effectSlide;

        public EFTBallisticsInterface(GameWorld gameWorld)
        {
            player = gameWorld.GetAlivePlayerBridgeByProfileID(gameWorld.MainPlayer.ProfileId);
            effectSlide = Singleton<Effects>.Instance.EffectsArray.FirstOrDefault(c => c.Name == "Concrete");
        }

        public RaycastHit[] Shoot(Vector3 origin, Vector3 rayDir, float dmg, bool piercing = false)
        {
            float rayDistance = 500f;

            Ray ray = new Ray(origin, rayDir);

            int layer12 = 1 << 12; // HighPolyCollider
            int layer16 = 1 << 16; // HitCollider (body parts)
            int layer11 = 1 << 11; // Terrain
            int layer15 = 1 << 15; // Loot (Coin)
            int layer30 = 1 << 30; // TransparentCollider
            int layerMask = layer12 | layer16 | layer11 | layer15 | layer30;

            RaycastHit[] hits;
            if (piercing)
                hits = Physics.SphereCastAll(ray, 0.4f, rayDistance, layerMask);
            else
                hits = Physics.RaycastAll(ray, rayDistance, layerMask);

            hits = hits.OrderBy(hit => Vector3.Distance(ray.origin, hit.point)).ToArray();

            Dictionary<int, int> limitPerPlayer = new Dictionary<int, int>();
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].transform.TryGetComponent(out BodyPartCollider bodyPart))
                {
                    if (bodyPart.playerBridge != null && bodyPart.playerBridge.iPlayer != null)
                    {
                        if (!limitPerPlayer.ContainsKey(bodyPart.playerBridge.iPlayer.Id))
                            limitPerPlayer[bodyPart.playerBridge.iPlayer.Id] = 0;
                        else if (limitPerPlayer[bodyPart.playerBridge.iPlayer.Id] >= 3)
                            continue;

                        limitPerPlayer[bodyPart.playerBridge.iPlayer.Id]++;
                    }

                    Hit(bodyPart, hits[i], dmg);
                }
                else
                {
                    if (hits[i].transform.tag == "DynamicCollider")
                    {
                        if (hits[i].rigidbody.TryGetComponent<Coin>(out Coin coin))
                        {
                            if (coin.active)
                            {
                                hits[i].point = coin.transform.position;
                                coin.Hit(dmg, false, piercing);

                                PlayerAudio.Instance.Play("Ricochet");

                                hits = hits.Take(i + 1).ToArray();
                                return hits;
                            }
                        }
                    }
                    else if (hits[i].transform.gameObject.layer == 30)
                        continue;

                    Hit(hits[i], dmg);
                }

                if (!piercing)
                {
                    hits = hits.Take(1).ToArray();
                    return hits;
                }
            }

            return hits;
        }

        public void Hit(Collision collision, float damage = 1f)
        {
            BaseBallistic baseBallistic = collision.transform.GetComponent<BaseBallistic>();

            if (baseBallistic == null)
            {
                if (collision.transform.parent == null)
                    return;

                baseBallistic = collision.transform.parent.GetComponentInChildren<BaseBallistic>();
            }

            float dmg = collision.impulse.magnitude / 10f * damage;

            MaterialType mat = MaterialType.None;
            for (int i = 0; i < collision.contactCount; i++)
            {
                RaycastHit fakeHit = new RaycastHit();
                fakeHit.point = collision.contacts[i].point;
                fakeHit.normal = collision.contacts[i].normal;

                if (baseBallistic is TerrainBallistic terrainBallistic)
                    Hit(terrainBallistic.Get(fakeHit.point), fakeHit, dmg);
                else
                    Hit(baseBallistic as BallisticCollider, fakeHit, dmg);
            }
        }

        public void Hit(RaycastHit hit, float dmg)
        {
            BaseBallistic baseBallistic = hit.collider.gameObject.GetComponent<BaseBallistic>();

            if (baseBallistic is TerrainBallistic terrainBallistic)
            {
                Hit(terrainBallistic.Get(hit.point), hit, dmg);
                return;
            }

            Hit(baseBallistic as BallisticCollider, hit, dmg);
        }

        public void Hit(Collider col, RaycastHit hit, float dmg)
        {
            BaseBallistic baseBallistic = col.gameObject.GetComponent<BaseBallistic>();

            if (baseBallistic is TerrainBallistic terrainBallistic)
            {
                Hit(terrainBallistic.Get(hit.point), hit, dmg);
                return;
            }

            Hit(baseBallistic as BallisticCollider, hit, dmg);
        }

        public void Hit(BallisticCollider ballisticCollider, Vector3 hitPoint, Vector3 hitNormal, float dmg)
        {
            RaycastHit fakeHit = new RaycastHit();
            fakeHit.point = hitPoint;
            fakeHit.normal = hitNormal;

            Hit(ballisticCollider, fakeHit, dmg);
        }

        public void Hit(BallisticCollider ballisticCollider, RaycastHit hit, float dmg)
        {
            if (ballisticCollider == null)
                return;

            DamageInfoStruct damageInfo = new DamageInfoStruct
            {
                DamageType = dmg > 200f ? EDamageType.Explosion : EDamageType.Bullet,
                Damage = dmg,
                ArmorDamage = dmg,
                StaminaBurnRate = dmg,
                PenetrationPower = dmg,
                Direction = UnityEngine.Random.onUnitSphere,
                HitNormal = hit.normal,
                HitPoint = hit.point,
                Player = player,
                IsForwardHit = true,
                HittedBallisticCollider = ballisticCollider,

                BlockedBy = null,
                DeflectedBy = null
            };

            ballisticCollider.ApplyHit(damageInfo, ShotIdStruct.EMPTY_SHOT_ID);

            Singleton<Effects>.Instance.Emit(ballisticCollider.TypeOfMaterial, ballisticCollider, hit.point, hit.normal, 1f);
        }

        Collider[] explosionOverlap = new Collider[300];
        public void Explosion(Vector3 pos)
        {
            int overlapCount = Physics.OverlapSphereNonAlloc(pos, 6f, explosionOverlap, 1 << 16);

            int limitBodyPartsPerPlayer = 4;

            Dictionary<IPlayer, int> limit = new Dictionary<IPlayer, int>();

            for (int i = 0; i < overlapCount; i++)
            {
                if (explosionOverlap[i].TryGetComponent(out BodyPartCollider bodyPart))
                {
                    if (bodyPart.playerBridge != null && bodyPart.playerBridge.iPlayer != null)
                    {
                        if (!limit.ContainsKey(bodyPart.playerBridge.iPlayer))
                            limit[bodyPart.playerBridge.iPlayer] = 0;
                        else if (limit[bodyPart.playerBridge.iPlayer] >= limitBodyPartsPerPlayer)
                            continue;

                        limit[bodyPart.playerBridge.iPlayer]++;
                    }

                    float dmg = 999f;
                    Hit(bodyPart, bodyPart.transform.position, (bodyPart.gameObject.transform.position - pos).normalized, dmg);
                }
            }

            Effect("big_explosion", pos);
            ParticleEffectManager.Instance.LightExplosion(pos);

            float maxDistance = 10f; 
            float minDistance = 1f;
            float maxShakeIntensity = 4f;
            float distance = CameraClass.Instance.Distance(pos);
            if (distance < maxDistance)
            {
                float normalizedDistance = Mathf.Clamp01((maxDistance - distance) / (maxDistance - minDistance));
                float shakeIntensity = Mathf.Lerp(0f, maxShakeIntensity, normalizedDistance);
                CameraShaker.Shake(shakeIntensity);
            }
        }

        public bool Parry(RaycastHit hit, Transform source)
        {
            if (hit.transform.parent!= null && hit.transform.parent.gameObject.layer == 22 && hit.transform.parent.gameObject.TryGetComponent<Door>(out Door door))
            {
                if (door.DoorState == EDoorState.Locked ||
                    door.DoorState == EDoorState.Shut)
                {
                    door.Interact(EFT.EInteractionType.Breach);
                    return true;
                }
            }

            if (hit.rigidbody != null && hit.rigidbody.TryGetComponent<IParryable>(out IParryable parryable))
            {
                parryable.Parry(source);
                return true;
            }

            BodyPartCollider bodyPartCollider = hit.transform.GetComponent<BodyPartCollider>();

            if (bodyPartCollider == null)
                return false;

            if (bodyPartCollider.playerBridge.iPlayer != null && bodyPartCollider.playerBridge.iPlayer.IsYourPlayer)
                return false;

            Hit(hit, 9999);
            return true;
        }

        public void Effect(string effect, Vector3 pos)
        {
            Singleton<Effects>.Instance.EmitGrenade(effect, pos, Vector3.up);
        }

        public void PlaySlide(Vector3 playerPos, Vector3 slideDir)
        {
            Vector3 point = playerPos + slideDir + new Vector3(0, 0.1f, 0);

            Singleton<Effects>.Instance.AddEffectEmit(effectSlide, point, -slideDir * 1.3f, null, false, 0, false, true, false);
        }
    }
}