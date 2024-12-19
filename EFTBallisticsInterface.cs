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

        public MaterialType Hit(RaycastHit hit)
        {
            BallisticCollider ballisticCollider = hit.collider.gameObject.GetComponent<BallisticCollider>();
            if (ballisticCollider != null)
            {
                DamageInfoStruct damageInfo = new DamageInfoStruct
                {
                    DamageType = EDamageType.Bullet,
                    Damage = 50,
                    ArmorDamage = 15,
                    StaminaBurnRate = 20,
                    PenetrationPower = 15,
                    Direction = UnityEngine.Random.onUnitSphere,
                    HitNormal = hit.normal,
                    HitPoint = hit.point,
                    Player = player,
                    IsForwardHit = true,
                    HittedBallisticCollider = ballisticCollider
                };

                ballisticCollider.ApplyHit(damageInfo, ShotIdStruct.EMPTY_SHOT_ID);

                Singleton<Effects>.Instance.Emit(ballisticCollider.TypeOfMaterial, ballisticCollider, hit.point, hit.normal, 0.1f);

                return ballisticCollider.TypeOfMaterial;
            }

            return MaterialType.None;
        }
    }
}