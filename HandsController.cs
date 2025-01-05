using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.InventoryLogic;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ultramove
{
    public class HandsController : MonoBehaviour
    {
        Animator animator;

        float coinCooldown;

        Transform recoilPivot;
        Vector3 recoilHighPos;
        Quaternion recoilHighRot;
        Quaternion recoilPivotOriginalLocalQuaternion;
        Vector3 recoilPivotOriginalLocalPosition;

        List<(GameObject, Weapon)> equippedWeapons;
        MuzzleManager muzzleManager;
        Weapon currentWeapon;

        CoinTosser coinTosser;

        Camera cam;

        SpringSimulation recoil = new SpringSimulation(0, 0);

        void SetWeaponHandPosition(Weapon weaponClass)
        {
            float blendPalmDist = 0;

            if (!(weaponClass is RevolverItemClass) &&
                !(weaponClass is PistolItemClass) &&
                !(weaponClass is SmgItemClass))
                blendPalmDist = 1f;

            animator = GetComponentInChildren<Animator>();
            animator.SetFloat("BlendPalmDist", blendPalmDist);
        }

        void SwapWeapon(int index)
        {
            if (index >= equippedWeapons.Count)
                return;

            if (equippedWeapons[index].Item2 == currentWeapon)
                return;

            currentWeapon = equippedWeapons[index].Item2;

            SetWeaponHandPosition(equippedWeapons[index].Item2);

            muzzleManager = equippedWeapons[index].Item1.GetComponent<MuzzleManager>();

            for (int i = 0; i < equippedWeapons.Count; i++)
            {
                equippedWeapons[i].Item1.SetActive(i == index);
            }

            recoil.OverrideCurrent(-0.8f);
        }

        public void SetWeapons(List<(GameObject, Weapon)> weapons)
        {
            equippedWeapons = weapons;

            Transform palm = GetComponentInChildren<PlayerBody>().SkeletonRootJoint.Bones["Root_Joint/Base HumanPelvis/Base HumanSpine1/Base HumanSpine2/Base HumanSpine3/Base HumanRibcage/Base HumanRCollarbone/Base HumanRUpperarm/Base HumanRForearm1/Base HumanRForearm2/Base HumanRForearm3/Base HumanRPalm"];

            foreach (var item in weapons)
            {
                Plugin.Log.LogInfo(item.Item1 + "  " + item.Item2);

                GameObject weapon = item.Item1;

                Destroy(weapon.GetComponentInChildren<Animator>());

                Transform container = weapon.transform.FindInChildrenExact("weapon");
                container.SetParent(null);
                container.position = Vector3.zero;
                container.rotation = Quaternion.identity;

                Transform handMarker = container.transform.FindInChildrenExact("weapon_R_hand_marker");

                Vector3 offset = handMarker.position;
                for (int i = 0; i < container.childCount; i++)
                {
                    container.GetChild(i).position -= offset;
                }

                container.SetParent(palm, true);
                container.localPosition = new Vector3(0, -0.025f, -0.01f);
                container.localEulerAngles = new Vector3(0, 180, 90f);

                weapon.transform.SetParent(palm, false);
                weapon.transform.localPosition = Vector3.zero;
                container.SetParent(weapon.transform, true);
            }

            SwapWeapon(0);
        }

        void Start()
        {
            cam = Camera.main;

            GetComponentInChildren<PlayerBody>().SkeletonRootJoint.Bones["Root_Joint"].localPosition = Vector3.zero;

            coinTosser = gameObject.GetOrAddComponent<CoinTosser>();


            recoilPivot = GetComponentInChildren<PlayerBody>().SkeletonRootJoint.Bones["Root_Joint/Base HumanPelvis/Base HumanSpine1/Base HumanSpine2/Base HumanSpine3/Base HumanRibcage/Base HumanRCollarbone"];
            recoilPivotOriginalLocalQuaternion = recoilPivot.localRotation;
            recoilPivotOriginalLocalPosition = recoilPivot.localPosition;

            recoilHighRot = recoilPivotOriginalLocalQuaternion * Quaternion.Euler(40f, 0, 0f);
            recoilHighPos = recoilPivotOriginalLocalPosition - new Vector3(0.1f, 0, 0.5f);
        }

        private void Update()
        {
            coinCooldown -= Time.deltaTime;
            if (Input.GetMouseButtonDown(1))
            {
                if (coinCooldown <= 0f)
                    Coin();
            }

            if (Input.GetKeyDown(KeyCode.V))
            {
                Parry();
            }

            if (Input.GetMouseButtonDown(0))
            {
                Vector3 origin = muzzleManager.MuzzleJets[0].transform.position;
                Vector3 dir = -muzzleManager.MuzzleJets[0].transform.up;

                float dmg = 20f;

                bool rail = (currentWeapon is SniperRifleItemClass);
                if (rail)
                    dmg = 60f;

                RaycastHit[] hits = EFTBallisticsInterface.Instance.Shoot(origin, dir, dmg, rail);
                if (hits.Length > 0)
                {
                    if (rail)
                    {
                        TrailRendererManager.Instance.Trail(origin, hits[hits.Length - 1].point, new Color(0.3f, 0.7f, 1f), 0.2f, true);
                        PlayerAudio.Instance.PlayShootRail();
                        CameraShaker.Shake(0.2f);
                    }
                    else
                    {
                        TrailRendererManager.Instance.Trail(origin, hits[0].point, Color.white);
                        PlayerAudio.Instance.PlayShoot();
                    }

                    this.recoil.AddForce(40f);

                    if (muzzleManager != null)
                        muzzleManager.Shot();
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
                SwapWeapon(0);
            if (Input.GetKeyDown(KeyCode.Alpha2))
                SwapWeapon(1);
            if (Input.GetKeyDown(KeyCode.Alpha3))
                SwapWeapon(2);

            this.recoil.Tick(Time.deltaTime);
            float recoil = this.recoil.Position;

            recoilPivot.localRotation = Quaternion.LerpUnclamped(recoilPivotOriginalLocalQuaternion, recoilHighRot, recoil);
            recoilPivot.localPosition = Vector3.LerpUnclamped(recoilPivotOriginalLocalPosition, recoilHighPos, recoil);

            PlayerAudio.Instance.PlayRailIdleCharged((currentWeapon is SniperRifleItemClass) ? 1f : 0);
        }

        void Coin()
        {
            if (!(currentWeapon is RevolverItemClass))
                return;

            coinCooldown = 0.05f;

            animator.SetTrigger("Coin");

            coinTosser.Toss();

            PlayerAudio.Instance.Play("coinflip");
        }

        void Parry()
        {
            bool parried = false;

            int layerMask = 1 << 16 | 1 << 15;
            RaycastHit[] hits = Physics.SphereCastAll(cam.transform.position, 0.6f, cam.transform.forward, 2f, layerMask);

            RaycastHit hit = new RaycastHit();

            for (int i = 0; i < hits.Length; i++)
            {
                hit = hits[i];

                parried = EFTBallisticsInterface.Instance.Parry(hit, cam.transform);
                if (parried)
                    break;
            }

            if (parried)
            {
                animator.SetTrigger("Parry");

                Singleton<UltraTime>.Instance.Freeze(0.05f, 0.25f);

                CameraShaker.Shake(1f);
                PlayerAudio.Instance.Play("Ricochet");
            }
        }
    }
}