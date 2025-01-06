﻿using EFT;
using EFT.Ballistics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ultramove
{
    public class PlayerBridge : BodyPartCollider.IPlayerBridge
    {
        public event Action<DamageInfoStruct> OnHitAction;

        IPlayer BodyPartCollider.IPlayerBridge.iPlayer => null;

        float BodyPartCollider.IPlayerBridge.WorldTime => 0;

        bool BodyPartCollider.IPlayerBridge.UsingSimplifiedSkeleton => false;

        void BodyPartCollider.IPlayerBridge.ApplyDamageInfo(DamageInfoStruct damageInfo, EBodyPart bodyPartType, EBodyPartColliderType bodyPartCollider, float absorbed)
        {
            OnHitAction?.Invoke(damageInfo);
        }

        ShotInfoClass BodyPartCollider.IPlayerBridge.ApplyShot(DamageInfoStruct damageInfo, EBodyPart bodyPart, EBodyPartColliderType bodyPartCollider, EArmorPlateCollider armorPlateCollider, ShotIdStruct shotId)
        {
            OnHitAction?.Invoke(damageInfo);
            ShotInfoClass shot = new ShotInfoClass();
            shot.Penetrated = true;
            return shot;
        }

        bool BodyPartCollider.IPlayerBridge.CheckArmorHitByDirection(BodyPartCollider bodypart, Vector3 hitpoint, Vector3 shotNormal, Vector3 shotDirection)
        {
            return true;
        }

        bool BodyPartCollider.IPlayerBridge.IsShotDeflectedByHeavyArmor(EBodyPartColliderType colliderType, EArmorPlateCollider armorPlateCollider, int shotSeed)
        {
            return false;
        }

        bool BodyPartCollider.IPlayerBridge.SetShotStatus(BodyPartCollider bodypart, EftBulletClass shot, Vector3 hitpoint, Vector3 shotNormal, Vector3 shotDirection)
        {
            return false;
        }

        bool BodyPartCollider.IPlayerBridge.TryGetArmorResistData(BodyPartCollider bodyPart, float penetrationPower, out ArmorResistanceStruct armorResistanceData)
        {
            armorResistanceData = new ArmorResistanceStruct();
            return false;
        }
    }

    internal abstract class UltraEnemy : MonoBehaviour
    {
        public BodyPartCollider[] ballisticColliders { get; private set; }

        public static HashSet<UltraEnemy> currentAlive = new HashSet<UltraEnemy>();

        private float health;
        public bool alive => health > 0;

        protected virtual void Start()
        {
            PlayerBridge bridge = new PlayerBridge();
            bridge.OnHitAction += Hit;

            Collider[] cols = GetComponentsInChildren<Collider>();
            List<BodyPartCollider> bpcsToAdd = new List<BodyPartCollider>();
            foreach (Collider col in cols)
            {
                if (col.gameObject.layer != 16)
                    continue;

                if (!col.gameObject.name.StartsWith("BodyPartCollider"))
                    continue;

                BodyPartCollider bodyPartCollider = col.gameObject.GetOrAddComponent<BodyPartCollider>();
                bodyPartCollider.playerBridge = bridge;
                bodyPartCollider.Collider = col;

                bpcsToAdd.Add(bodyPartCollider);
            }

            // weak points are tagged with 'AimPoint', and they are sorted first in the array
            ballisticColliders = bpcsToAdd.OrderByDescending(x => x.gameObject.tag == "AimPoint").ToArray();

            health = GetStartingHealth();
            currentAlive.Add(this);
        }

        protected abstract float GetStartingHealth();

        public void Hit(DamageInfoStruct damageInfo)
        {
            if (!alive)
                return;

            health -= damageInfo.Damage;

            if (health <= 0)
                Die();
        }

        protected virtual void Die()
        {
            health = -1f;

            currentAlive.Remove(this);
        }
    }
}
