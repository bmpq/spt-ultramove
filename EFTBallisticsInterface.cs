﻿using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.Interactive;
using System;
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
                hits = Physics.SphereCastAll(ray, 0.3f, rayDistance, layerMask);
            else
                hits = Physics.RaycastAll(ray, rayDistance, layerMask);

            hits = hits.OrderBy(hit => Vector3.Distance(ray.origin, hit.point)).ToArray();
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];

                MaterialType matHit = MaterialType.None;

                if (hit.transform.tag == "DynamicCollider")
                {
                    if (hit.rigidbody.TryGetComponent<Coin>(out Coin coin))
                    {
                        if (coin.active)
                        {
                            hit.point = coin.transform.position;
                            coin.Hit(dmg, false, piercing);

                            hits[0].point = coin.transform.position;

                            PlayerAudio.Instance.Play("Ricochet");

                            return hits;
                        }
                    }
                }
                else
                {
                    Hit(hit, dmg);
                }

                if (!piercing)
                    break;
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

        public void Hit(BallisticCollider ballisticCollider, RaycastHit hit, float dmg)
        {
            if (ballisticCollider == null)
                return;

            DamageInfoStruct damageInfo = new DamageInfoStruct
            {
                DamageType = EDamageType.Bullet,
                Damage = dmg,
                ArmorDamage = dmg,
                StaminaBurnRate = dmg,
                PenetrationPower = dmg,
                Direction = UnityEngine.Random.onUnitSphere,
                HitNormal = hit.normal,
                HitPoint = hit.point,
                MasterOrigin = hit.point,
                Player = player,
                IsForwardHit = true,
                HittedBallisticCollider = ballisticCollider,

                BlockedBy = null,
                DeflectedBy = null
            };

            ballisticCollider.ApplyHit(damageInfo, ShotIdStruct.EMPTY_SHOT_ID);

            Singleton<Effects>.Instance.Emit(ballisticCollider.TypeOfMaterial, ballisticCollider, hit.point, hit.normal, 1f);
        }

        Collider[] explosionOverlap = new Collider[50];
        public void Explosion(Vector3 pos)
        {
            int overlapCount = Physics.OverlapSphereNonAlloc(pos, 2.5f, explosionOverlap, 1 << 16);

            int limitBodyPartsPerPlayer = 3;

            Dictionary<IPlayer, int> limit = new Dictionary<IPlayer, int>();

            for (int i = 0; i < overlapCount; i++)
            {
                if (explosionOverlap[i].TryGetComponent(out BodyPartCollider bodyPart))
                {
                    if (!limit.ContainsKey(bodyPart.playerBridge.iPlayer))
                        limit[bodyPart.playerBridge.iPlayer] = 0;
                    else if (limit[bodyPart.playerBridge.iPlayer] == limitBodyPartsPerPlayer)
                        continue;

                    float dmg = 300f;

                    DamageInfoStruct damageInfo = new DamageInfoStruct
                    {
                        DamageType = EDamageType.Explosion,
                        Damage = dmg,
                        ArmorDamage = dmg,
                        StaminaBurnRate = dmg,
                        PenetrationPower = dmg,
                        Player = player,
                        HitPoint = bodyPart.gameObject.transform.position,
                        HitNormal = (bodyPart.gameObject.transform.position - pos).normalized,
                        HittedBallisticCollider = bodyPart,
                        IsForwardHit = true,
                        MasterOrigin = pos
                    };
                    bodyPart.ApplyHit(damageInfo, ShotIdStruct.EMPTY_SHOT_ID);

                    limit[bodyPart.playerBridge.iPlayer]++;
                }
            }

            Effect("big_explosion", pos);


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