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
    }
}