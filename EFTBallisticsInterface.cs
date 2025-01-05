using Comfort.Common;
using EFT;
using EFT.Ballistics;
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

            RaycastHit[] hits = Physics.RaycastAll(ray, rayDistance, layerMask);
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
                    matHit = EFTBallisticsInterface.Instance.Hit(hit, dmg);
                }

                if (!piercing)
                    break;
            }

            return hits;
        }

        public MaterialType Hit(Collision collision)
        {
            BallisticCollider ballisticCollider = collision.collider.gameObject.GetComponent<BallisticCollider>();

            float damage = collision.impulse.magnitude / 100f;

            MaterialType mat = MaterialType.None;
            for (int i = 0; i < collision.contactCount; i++)
            {
                RaycastHit fakeHit = new RaycastHit();
                fakeHit.point = collision.contacts[i].point;
                fakeHit.normal = collision.contacts[i].normal;
                mat = Hit(ballisticCollider, fakeHit, damage);
            }

            return mat;
        }

        public MaterialType Hit(RaycastHit hit, float dmg)
        {
            BallisticCollider ballisticCollider = hit.collider.gameObject.GetComponent<BallisticCollider>();
            return Hit(ballisticCollider, hit, dmg);
        }

        public MaterialType Hit(Collider col, RaycastHit hit, float dmg)
        {
            BallisticCollider ballisticCollider = col.gameObject.GetComponent<BallisticCollider>();
            return Hit(ballisticCollider, hit, dmg);
        }

        public MaterialType Hit(BallisticCollider ballisticCollider, RaycastHit hit, float dmg)
        {
            if (ballisticCollider == null)
                return MaterialType.None;

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
                Player = player,
                IsForwardHit = true,
                HittedBallisticCollider = ballisticCollider
            };

            ballisticCollider.ApplyHit(damageInfo, ShotIdStruct.EMPTY_SHOT_ID);

            Singleton<Effects>.Instance.Emit(ballisticCollider.TypeOfMaterial, ballisticCollider, hit.point, hit.normal, 1f);

            return ballisticCollider.TypeOfMaterial;
        }

        public void Explosion(Vector3 pos)
        {
            foreach (var enemy in Singleton<GameWorld>.Instance.AllAlivePlayersList)
            {
                if (Vector3.Distance(enemy.Position, pos) < 5f)
                {
                    float dmg = 9999;

                    DamageInfoStruct damageInfo = new DamageInfoStruct
                    {
                        DamageType = EDamageType.Bullet,
                        Damage = dmg,
                        ArmorDamage = dmg,
                        StaminaBurnRate = dmg,
                        PenetrationPower = dmg,
                        Player = player,
                        HitPoint = enemy.MainParts[BodyPartType.head].Position,
                        HitNormal = (pos - enemy.MainParts[BodyPartType.head].Position).normalized,
                        IsForwardHit = true
                    };

                    enemy.ApplyDamageInfo(damageInfo, EBodyPart.Head, EBodyPartColliderType.HeadCommon, 0);
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
            if (hit.rigidbody.TryGetComponent<Projectile>(out Projectile projectile))
            {
                projectile.Parry(source);
                return true;
            }

            BodyPartCollider bodyPartCollider = hit.transform.GetComponent<BodyPartCollider>();

            if (bodyPartCollider == null)
                return false;

            if (bodyPartCollider.Player.IsYourPlayer)
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