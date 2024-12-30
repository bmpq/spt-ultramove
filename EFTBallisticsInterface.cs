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

        public EFTBallisticsInterface(GameWorld gameWorld)
        {
            player = gameWorld.GetAlivePlayerBridgeByProfileID(gameWorld.MainPlayer.ProfileId);
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
    }
}