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

        float weaponSwapAnimationTime;

        CoinTosser coinTosser;

        Camera cam;
        Rigidbody rb;

        SpringSimulation recoil = new SpringSimulation(0, 0);
        Dictionary<Weapon, ReloadingAnimation> reloadingAnimations;
        float reloadingTime;

        MeshRenderer muzzleFlash;
        VolumetricLight muzzleFlashLight;

        Coroutine animMuzzleFlash;


        bool whiplashThrowing;
        bool whiplashPulling;
        Transform whiplashPullingObject;
        Vector3 whiplashGrabPointOffset;
        RopeVisual ropeVisual;
        Transform palmL;
        Vector3 currentWhiplashEnd;
        Vector3 whiplashThrowVelocity;
        float whiplashStartSpeed = 100f;

        void SetWeaponHandPosition(Weapon weaponClass)
        {
            float blendPalmDist = 0;

            if (!(weaponClass is RevolverItemClass) &&
                !(weaponClass is PistolItemClass) &&
                !(weaponClass is SmgItemClass))
                blendPalmDist = 1f;

            if (weaponClass is ShotgunItemClass)
                blendPalmDist = 0.5f;

            animator = GetComponentInChildren<Animator>();
            animator.SetFloat("BlendPalmDist", blendPalmDist);
        }

        void SwapWeapon(int index)
        {
            if (index >= equippedWeapons.Count)
                return;

            if (equippedWeapons[index].Item2 == currentWeapon)
                return;

            if (equippedWeapons[index].Item2 == null)
                return;

            currentWeapon = equippedWeapons[index].Item2;

            SetWeaponHandPosition(equippedWeapons[index].Item2);

            muzzleManager = equippedWeapons[index].Item1.GetComponent<MuzzleManager>();

            for (int i = 0; i < equippedWeapons.Count; i++)
            {
                equippedWeapons[i].Item1.SetActive(i == index);
            }

            recoil.OverrideCurrent(-0.8f);
            weaponSwapAnimationTime = 0f;
        }

        public void SetWeapons(List<(GameObject, Weapon)> weapons)
        {
            equippedWeapons = weapons;

            reloadingAnimations = new Dictionary<Weapon, ReloadingAnimation>();

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

                if (item.Item2.StringTemplateId == "64748cb8de82c85eaf0a273a") // sawed-off
                {
                    weapon.transform.localPosition = new Vector3(0.0484f, 0, 0.092f);
                    reloadingAnimations.Add(item.Item2, new ReloadingAnimation.SawedOff(weapon));
                }
                else if (item.Item2.StringTemplateId == "633ec7c2a6918cb895019c6c") // rsh12 revolver
                {
                    reloadingAnimations.Add(item.Item2, new ReloadingAnimation.Revolver(weapon));
                }
                else if (item.Item2.StringTemplateId == "55801eed4bdc2d89578b4588") // sv98
                {
                    reloadingAnimations.Add(item.Item2, new ReloadingAnimation.Sniper(weapon));
                }
            }

            SwapWeapon(0);
        }

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            cam = Camera.main;

            GetComponentInChildren<PlayerBody>().SkeletonRootJoint.Bones["Root_Joint"].localPosition = Vector3.zero;

            palmL = GetComponentInChildren<PlayerBody>().SkeletonRootJoint.Bones["Root_Joint/Base HumanPelvis/Base HumanSpine1/Base HumanSpine2/Base HumanSpine3/Base HumanRibcage/Base HumanLCollarbone/Base HumanLUpperarm/Base HumanLForearm1/Base HumanLForearm2/Base HumanLForearm3/Base HumanLPalm"];

            coinTosser = gameObject.GetOrAddComponent<CoinTosser>();


            recoilPivot = GetComponentInChildren<PlayerBody>().SkeletonRootJoint.Bones["Root_Joint/Base HumanPelvis/Base HumanSpine1/Base HumanSpine2/Base HumanSpine3/Base HumanRibcage/Base HumanRCollarbone"];
            recoilPivotOriginalLocalQuaternion = recoilPivot.localRotation;
            recoilPivotOriginalLocalPosition = recoilPivot.localPosition;

            recoilHighRot = recoilPivotOriginalLocalQuaternion * Quaternion.Euler(40f, -10f, 0f);
            recoilHighPos = recoilPivotOriginalLocalPosition - new Vector3(0.1f, 0, 0.5f);

            foreach (var kvp in reloadingAnimations)
            {
                kvp.Value.SetRecoilPivotTransform(recoilPivot);
            }

            muzzleFlash = Instantiate(AssetBundleLoader.BundleLoader.LoadAssetBundle(AssetBundleLoader.BundleLoader.GetDefaultModAssetBundlePath("ultrakill")).LoadAsset<GameObject>("glint")).GetComponentInChildren<MeshRenderer>();
            muzzleFlash.material = new Material(Shader.Find("Sprites/Default"));
            muzzleFlash.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            muzzleFlashLight = new GameObject("LightMuzzleFlash", typeof(Light)).AddComponent<VolumetricLight>();
            muzzleFlashLight.transform.position = new Vector3(0f, -200f, 0f);

            ropeVisual = gameObject.AddComponent<RopeVisual>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R) && !whiplashPulling)
            {
                whiplashThrowing = true;
                whiplashThrowVelocity = rb.velocity + (cam.transform.forward * whiplashStartSpeed);
                currentWhiplashEnd = cam.transform.position + (cam.transform.forward * 0.8f) - (cam.transform.up * 0.2f);
                whiplashPullingObject = null;

                ropeVisual.RopeShoot();
            }
            else if (Input.GetKeyUp(KeyCode.R))
            {
                if (whiplashThrowing)
                    whiplashPulling = true;

                whiplashThrowing = false;
            }

            if (whiplashThrowing || whiplashPulling)
            {
                ropeVisual.RopeUpdate(palmL.transform.position, currentWhiplashEnd);
            }
            else
            {
                ropeVisual.RopeRelease();
            }

            animator.SetBool("WhiplashThrowing", whiplashThrowing);
            animator.SetBool("WhiplashPulling", whiplashPulling);


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
                Shoot();
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
                SwapWeapon(2);
            if (Input.GetKeyDown(KeyCode.Alpha2))
                SwapWeapon(0);
            if (Input.GetKeyDown(KeyCode.Alpha3))
                SwapWeapon(1);

            float recoil = this.recoil.Position;

            recoilPivot.localRotation = Quaternion.LerpUnclamped(recoilPivotOriginalLocalQuaternion, recoilHighRot, recoil);

            recoilPivot.localPosition = Vector3.LerpUnclamped(recoilPivotOriginalLocalPosition, recoilHighPos, recoil);

            if (weaponSwapAnimationTime < 1f)
            {
                weaponSwapAnimationTime = Mathf.Clamp01(weaponSwapAnimationTime + Time.deltaTime * 3f);
                recoilPivot.localPosition -= new Vector3(0, Mathf.Pow(1 - weaponSwapAnimationTime, 3), 0) * 0.2f;
            }

            if (reloadingTime < 1f)
            {
                reloadingTime = Mathf.Clamp01(reloadingTime + Time.deltaTime);

                if (reloadingAnimations.ContainsKey(currentWeapon))
                {
                    reloadingAnimations[currentWeapon].Evaluate(reloadingTime);
                }
            }
        }

        void FixedUpdate()
        {
            this.recoil.Tick(Time.fixedDeltaTime);

            if (whiplashThrowing)
            {
                LayerMask layerMask = 1 << 18 | 1 << 16;
                if (Physics.Raycast(currentWhiplashEnd, whiplashThrowVelocity.normalized, out RaycastHit hit, whiplashThrowVelocity.magnitude * Time.fixedDeltaTime, layerMask))
                {
                    whiplashThrowing = false;
                    whiplashPulling = true;
                    currentWhiplashEnd = hit.point;

                    if (hit.collider.TryGetComponent<BodyPartCollider>(out BodyPartCollider bpc))
                    {
                        whiplashPullingObject = bpc.Player.Transform.Original;
                        whiplashGrabPointOffset = whiplashPullingObject.InverseTransformPoint(hit.point);
                    }
                }

                if (whiplashThrowing)
                {
                    currentWhiplashEnd += whiplashThrowVelocity * Time.fixedDeltaTime;
                }
            }
            else if (whiplashPulling)
            {
                float reelSpeed = whiplashStartSpeed;

                if (whiplashPullingObject != null)
                {
                    whiplashPullingObject.position = currentWhiplashEnd - whiplashGrabPointOffset;
                    reelSpeed = whiplashStartSpeed / 4f;
                }

                currentWhiplashEnd += (cam.transform.position - currentWhiplashEnd).normalized * reelSpeed * Time.fixedDeltaTime;

                if (Vector3.Distance(cam.transform.position, currentWhiplashEnd) < 1f)
                    whiplashPulling = false;
            }
        }

        void Shoot()
        {
            Vector3 origin = muzzleManager.MuzzleJets[0].transform.position - muzzleManager.MuzzleJets[0].transform.up * 0.2f;
            Vector3 dir = -muzzleManager.MuzzleJets[0].transform.up;

            float dmg = 20f;

            bool rail = (currentWeapon is SniperRifleItemClass);
            if (rail)
                dmg = 60f;

            bool shot = false;

            bool shotgun = (currentWeapon is ShotgunItemClass);

            if (shotgun)
            {
                Shotgun.ShootProjectiles(origin, dir, rb.velocity);

                PlayerAudio.Instance.PlayShootShotgun();
                CameraShaker.Shake(0.5f);

                shot = true;
            }
            else
            {
                RaycastHit[] hits = EFTBallisticsInterface.Instance.Shoot(origin, dir, dmg, rail);
                if (hits.Length > 0)
                {
                    if (rail)
                    {
                        TrailRendererManager.Instance.Trail(origin, hits[hits.Length - 1].point, true);
                        PlayerAudio.Instance.PlayShootRail();
                        CameraShaker.Shake(1.5f);
                    }
                    else
                    {
                        TrailRendererManager.Instance.Trail(origin, hits[0].point, false);
                        PlayerAudio.Instance.PlayShoot();
                    }
                    shot = true;
                }
            }

            if (shot)
            {
                float recoilForce = 40f;
                Color colorMuzzle = Color.white;
                if (rail)
                {
                    recoilForce = 70f;
                    colorMuzzle = Color.cyan;
                }
                else if (shotgun)
                {
                    recoilForce = 60f;
                    colorMuzzle = Color.yellow;
                }

                this.recoil.AddForce(recoilForce);

                if (muzzleManager != null)
                    muzzleManager.Shot();

                muzzleFlash.transform.position = origin;

                muzzleFlash.material.color = colorMuzzle;
                if (animMuzzleFlash != null)
                    StopCoroutine(animMuzzleFlash);
                animMuzzleFlash = StartCoroutine(AnimMuzzleFlash(rail));

                reloadingTime = 0f;
            }
        }

        IEnumerator AnimMuzzleFlash(bool rail)
        {
            float t = 0f;

            muzzleFlash.transform.rotation = Random.rotation;

            muzzleFlashLight.transform.position = muzzleFlash.transform.position;
            muzzleFlashLight.Light.shadows = LightShadows.None;
            muzzleFlashLight.Light.range = 1f;
            muzzleFlashLight.Light.color = muzzleFlash.material.color;

            while (t < 1f)
            {
                t += Time.deltaTime * (rail ? 2f : 5f);

                float e = 1f - Mathf.Pow(1f - t, 3f);

                muzzleFlash.transform.localScale = Vector3.Lerp(Vector3.one * (rail ? 0.1f : 0.05f), Vector3.zero, e);

                muzzleFlashLight.Light.intensity = Mathf.Lerp(2f, 0f, e);
                muzzleFlashLight.CheckIntensity();

                yield return null;
            }
        }

        void Coin()
        {
            if (!(currentWeapon is RevolverItemClass))
                return;

            //coinCooldown = 0.05f;

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

        public abstract class ReloadingAnimation
        {
            Transform mod_barrel;
            Transform weapon_switch;
            Transform patron_in_weapon_000;
            Transform patron_in_weapon_001;

            Transform recoilPivot;

            public ReloadingAnimation(GameObject weapon)
            {
                mod_barrel = weapon.transform.FindInChildrenExact("mod_barrel");
                weapon_switch = weapon.transform.FindInChildrenExact("weapon_switch");
                patron_in_weapon_000 = weapon.transform.FindInChildrenExact("patron_in_weapon_000");
                patron_in_weapon_001 = weapon.transform.FindInChildrenExact("patron_in_weapon_001");
            }

            public abstract void Evaluate(float t);
            public void SetRecoilPivotTransform(Transform recoilPivot)
            {
                this.recoilPivot = recoilPivot;
            }

            public class SawedOff : ReloadingAnimation
            {
                GameObject shell0;
                GameObject shell1;

                public SawedOff(GameObject weapon) : base(weapon)
                {
                    shell0 = Singleton<PoolManager>.Instance.CreateItem(Singleton<ItemFactoryClass>.Instance.CreateItem(MongoID.Generate(false), "560d5e524bdc2d25448b4571", null), true);
                    shell1 = Singleton<PoolManager>.Instance.CreateItem(Singleton<ItemFactoryClass>.Instance.CreateItem(MongoID.Generate(false), "560d5e524bdc2d25448b4571", null), true);

                    shell0.SetActive(true);
                    shell1.SetActive(true);

                    shell0.transform.SetParent(patron_in_weapon_000, false);
                    shell1.transform.SetParent(patron_in_weapon_001, false);
                }

                public override void Evaluate(float t)
                {
                    if (t > 0.25f && t < 0.6f)
                    {
                        float e = EasingFunction.Remap(t, 0.25f, 0.6f);
                        e = EasingFunction.EaseInCubic(e);

                        recoilPivot.localRotation *= Quaternion.Euler(Mathf.Lerp(0, -90, e), 0, 0);
                    }
                    else if (t > 0.6f && t < 0.75f)
                    {
                        float e = EasingFunction.Remap(t, 0.6f, 0.75f);

                        recoilPivot.localRotation *= Quaternion.Euler(Mathf.Lerp(-90, 0f, e), 0, 0);
                    }
                    else if (t > 0.75f && t < 0.85f)
                    {
                        float e = EasingFunction.Remap(t, 0.75f, 0.85f);
                        e = EasingFunction.EaseOutCubic(e);

                        recoilPivot.localRotation *= Quaternion.Lerp(Quaternion.Euler(0, 0, 0), Quaternion.Euler(20, -10, 0), e);
                    }
                    else if (t > 0.85f && t < 0.92f)
                    {
                        float e = EasingFunction.Remap(t, 0.85f, 0.92f);
                        e = EasingFunction.EaseInCubic(e);

                        recoilPivot.localRotation *= Quaternion.Lerp(Quaternion.Euler(20, -10, 0), Quaternion.Euler(10, 2, 0), e);
                    }
                    else if (t > 0.92f)
                    {
                        float e = EasingFunction.Remap(t, 0.92f, 1f);
                        e = EasingFunction.EaseOutCubic(e);

                        recoilPivot.localRotation *= Quaternion.Lerp(Quaternion.Euler(10, 2, 0), Quaternion.Euler(0, 0, 0), e);
                    }

                    if (t > 0.3f && t < 0.35f)
                    {
                        float e = EasingFunction.Remap(t, 0.3f, 0.35f);

                        shell0.transform.localRotation = Quaternion.Euler(90, 0, 0);
                        shell1.transform.localRotation = Quaternion.Euler(90, 0, 0);

                        shell0.transform.localPosition = Vector3.Lerp(new Vector3(0, 0, 0), new Vector3(0, 0.2f, 0), e);
                        shell1.transform.localPosition = Vector3.Lerp(new Vector3(0, 0, 0), new Vector3(0, 0.2f, 0), e);
                    }
                    else if (t > 0.35f && t < 0.6f)
                    {
                        float e = EasingFunction.Remap(t, 0.35f, 0.6f);

                        shell0.transform.localPosition = Vector3.Lerp(new Vector3(0, 0.2f, 0), new Vector3(0, 0.8f, -0.4f), e);
                        shell1.transform.localPosition = Vector3.Lerp(new Vector3(0, 0.2f, 0), new Vector3(0, 0.8f, -0.4f), e);

                        shell0.transform.localRotation = Quaternion.Lerp(Quaternion.Euler(90, 0, 0), Quaternion.Euler(-90f, 0, 0), e);
                        shell1.transform.localRotation = Quaternion.Lerp(Quaternion.Euler(90, 0, 0), Quaternion.Euler(-90f, 0, 0), e);
                    }
                    else if (t > 0.6f)
                    {
                        shell0.transform.localPosition = Vector3.zero;
                        shell1.transform.localPosition = Vector3.zero;

                        shell0.transform.localRotation = Quaternion.Euler(90, 0, 0);
                        shell1.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    }


                    if (t > 0.2f && t < 0.5f)
                    {
                        float e = EasingFunction.Remap(t, 0.2f, 0.5f);
                        e = EasingFunction.EaseOutCubic(e);

                        mod_barrel.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, 0), Quaternion.Euler(60, 0, 0), e);
                    }
                    else if (t > 0.5f)
                    {
                        float e = EasingFunction.Remap(t, 0.5f, 0.95f);
                        e = EasingFunction.EaseInCubic(e);

                        mod_barrel.localRotation = Quaternion.Lerp(Quaternion.Euler(60, 0, 0), Quaternion.Euler(0, 0, 0), e);
                    }

                    if (t > 0.8f && t < 0.85f)
                    {
                        float e = EasingFunction.Remap(t, 0.8f, 0.85f);
                        e = EasingFunction.EaseInCubic(e);

                        weapon_switch.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, 0), Quaternion.Euler(0, 0, -20f), e);
                    }
                    else if (t > 0.85f && t < 0.9f)
                    {
                        float e = EasingFunction.Remap(t, 0.85f, 0.9f);
                        e = EasingFunction.EaseOutCubic(e);

                        weapon_switch.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, -20f), Quaternion.Euler(0, 0, 0), e);
                    }
                }
            }

            public class Revolver : ReloadingAnimation
            {
                Transform mod_magazine;
                Transform mod_hammer;

                public Revolver(GameObject weapon) : base(weapon)
                {
                    mod_magazine = weapon.transform.FindInChildrenExact("mod_magazine");
                    mod_hammer = weapon.transform.FindInChildrenExact("mod_hammer");
                }

                public override void Evaluate(float t)
                {
                    if (t > 0f && t < 0.7f)
                    {
                        float e = EasingFunction.Remap(t, 0, 0.7f);
                        e = EasingFunction.EaseOutCubic(e);

                        mod_magazine.localRotation = Quaternion.Euler(0, Mathf.Lerp(-(360f / 5f * 4f), 0, e), 0);
                    }

                    if (t > 0f && t < 0.5f)
                    {
                        float e = EasingFunction.Remap(t, 0f, 0.5f);
                        e = EasingFunction.EaseOutBounce(0, 1f, e);

                        mod_hammer.localRotation = Quaternion.Euler(Mathf.Lerp(0, -40f, e), 0, 0);
                    }
                }
            }

            public class Sniper : ReloadingAnimation
            {
                Transform mod_scope;
                Vector3 mod_scope_localPositionOriginal;

                public Sniper(GameObject weapon) : base(weapon)
                {
                    mod_scope = weapon.transform.FindInChildrenExact("mod_scope");
                    mod_scope_localPositionOriginal = mod_scope.localPosition;
                }

                public override void Evaluate(float t)
                {
                    if (t > 0f && t < 0.4f)
                    {
                        float e = EasingFunction.Remap(t, 0, 0.4f);
                        e = EasingFunction.EaseOutCubic(e);

                        mod_scope.localPosition = Vector3.Lerp(mod_scope_localPositionOriginal, mod_scope_localPositionOriginal + new Vector3(0, 0.07f, 0), e);
                    }
                    else if (t > 0.4f && t < 0.8f)
                    {
                        float e = EasingFunction.Remap(t, 0.4f, 0.8f);
                        e = EasingFunction.EaseInCubic(e);

                        mod_scope.localPosition = Vector3.Lerp(mod_scope_localPositionOriginal + new Vector3(0, 0.07f, 0), mod_scope_localPositionOriginal, e);
                    }
                }
            }
        }
    }
}