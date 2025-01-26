using EFT;
using EFT.Ballistics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ultramove
{
    internal class UltraPlayerBridge : BodyPartCollider.IPlayerBridge
    {
        public event Action<DamageInfoStruct> OnHitAction;

        public UltraEnemy UltraEnemy;

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

        public static UltraEnemy[] currentAlive => _currentAlive.ToArray();
        private static HashSet<UltraEnemy> _currentAlive = new HashSet<UltraEnemy>();

        private float health;
        protected abstract float startingHealth { get; }
        public bool alive => health > 0;

        private void Start()
        {
            bool rb = TryGetComponent<Rigidbody>(out Rigidbody rigidbody);

            UltraPlayerBridge bridge = new UltraPlayerBridge();
            bridge.OnHitAction += Hit;
            bridge.UltraEnemy = this;

            Collider[] cols = GetComponentsInChildren<Collider>();
            List<BodyPartCollider> bpcsToAdd = new List<BodyPartCollider>();
            foreach (Collider col in cols)
            {
                if (!col.gameObject.name.StartsWith("BodyPartCollider"))
                    continue;

                BodyPartCollider bodyPartCollider = col.gameObject.GetOrAddComponent<BodyPartCollider>();
                bodyPartCollider.playerBridge = bridge;
                bodyPartCollider.Collider = col;

                bpcsToAdd.Add(bodyPartCollider);

                if (rb)
                    bodyPartCollider.gameObject.AddComponent<BodyPartColliderParentLink>();
            }

            // weak points are tagged with 'AimPoint', and they are sorted first in the array
            ballisticColliders = bpcsToAdd.OrderByDescending(x => x.gameObject.tag == "AimPoint").ToArray();

            Revive();
        }

        public void Hit(DamageInfoStruct damageInfo)
        {
            if (!alive)
                return;

            health -= damageInfo.Damage;

            if (Plugin.UndyingEnemies.Value)
                health = Mathf.Max(1, health);

            if (health <= 0)
                Die();
        }

        protected virtual void Revive()
        {
            health = startingHealth;
            _currentAlive.Add(this);
        }

        protected virtual void Die()
        {
            health = -1f;
            _currentAlive.Remove(this);
        }

        void OnCollisionEnter(Collision collision)
        {
            EFTBallisticsInterface.Instance.Hit(collision);
        }
    }
}
