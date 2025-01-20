using Comfort.Common;
using EFT;
using EFT.AssetsManager;
using EFT.Ballistics;
using EFT.Interactive;
using EFT.InventoryLogic;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        Transform fireport;
        Weapon currentWeapon;

        float weaponSwapAnimationTime;

        bool currentWeaponIsFullauto => currentWeapon.FireMode.AvailableEFireModes.Contains(Weapon.EFireMode.fullauto);
        float fullautoCooldown;

        CoinTosser coinTosser;

        float timeLastParry;

        Camera cam;
        Rigidbody rb;

        SpringSimulation recoil = new SpringSimulation(0, 0);
        SpringSimulation recoilHorizontal = new SpringSimulation(0, 0);
        Dictionary<Weapon, ReloadingAnimation> reloadingAnimations;
        float reloadingTime;

        enum WhiplashState
        {
            Idle,
            Throwing,
            Pulling
        }
        WhiplashState whiplashState;
        WhiplashState whiplashStatePrev;
        Transform whiplashPullingObject;
        Vector3 whiplashGrabPointOffset;
        RopeVisual ropeVisual;
        Transform palmL;
        Vector3 currentWhiplashEnd;
        Vector3 whiplashThrowVelocity;
        float whiplashStartSpeed = 100f;
        GameObject spearhead;

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

            fireport = equippedWeapons[index].Item1.transform.FindInChildrenExact("fireport");

            for (int i = 0; i < equippedWeapons.Count; i++)
            {
                equippedWeapons[i].Item1.SetActive(i == index);
            }

            recoil.OverrideCurrent(-0.8f);
            weaponSwapAnimationTime = 0f;
            reloadingTime = 0.99f;
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

            ropeVisual = gameObject.AddComponent<RopeVisual>();
            spearhead = Instantiate(AssetBundleLoader.BundleLoader.LoadAssetBundle(AssetBundleLoader.BundleLoader.GetDefaultModAssetBundlePath("ultrakill")).LoadAsset<GameObject>("SpearheadPrefab"));
        }

        private void Update()
        {
            HandleWhiplash();

            coinCooldown -= Time.deltaTime;
            if (Input.GetMouseButtonDown(1))
            {
                if (coinCooldown <= 0f && (currentWeapon is RevolverItemClass))
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

            if (Input.GetMouseButton(0) && currentWeaponIsFullauto)
            {
                fullautoCooldown += Time.deltaTime;
                if (fullautoCooldown > 60f / (currentWeapon.FireRate * 2f))
                {
                    Shoot();
                    fullautoCooldown = 0f;
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
                SwapWeapon(2);
            if (Input.GetKeyDown(KeyCode.Alpha2))
                SwapWeapon(0);
            if (Input.GetKeyDown(KeyCode.Alpha3))
                SwapWeapon(1);

            float recoil = this.recoil.Position;

            recoilPivot.localRotation = Quaternion.LerpUnclamped(recoilPivotOriginalLocalQuaternion, recoilHighRot, recoil);
            recoilPivot.localRotation *= Quaternion.Euler(0, 0, recoilHorizontal.Position);

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

            PlayerAudio.Instance.Loop("RailcannonFull", currentWeapon is SniperRifleItemClass, recoilPivot.position, 0.5f);
        }

        private void HandleWhiplash()
        {
            if (whiplashState == WhiplashState.Idle && Input.GetKeyDown(KeyCode.R))
            {
                whiplashState = WhiplashState.Throwing;
                whiplashThrowVelocity = rb.velocity + (cam.transform.forward * whiplashStartSpeed);
                currentWhiplashEnd = cam.transform.position + (cam.transform.forward * 0.8f) - (cam.transform.up * 0.2f);
                whiplashPullingObject = null;

                Transform autoAimTarget = EFTTargetInterface.GetAutoAimTarget(palmL.transform.position + cam.transform.forward, cam.transform.forward, 15f);
                if (autoAimTarget != null)
                {
                    whiplashThrowVelocity = rb.velocity + ((autoAimTarget.position - cam.transform.position).normalized * whiplashStartSpeed);
                }

                ropeVisual.RopeShoot();
                spearhead.SetActive(true);
                spearhead.transform.SetParent(null);
            }
            else if (whiplashState == WhiplashState.Throwing && Input.GetKeyUp(KeyCode.R))
            {
                whiplashState = WhiplashState.Pulling;
            }

            if (whiplashState != WhiplashState.Idle)
            {
                ropeVisual.RopeUpdate(palmL.transform.position, currentWhiplashEnd);
                spearhead.transform.position = currentWhiplashEnd;
                spearhead.transform.rotation = Quaternion.LookRotation(whiplashThrowVelocity.normalized);
                spearhead.transform.localScale = Vector3.one * 1.4f;
            }
            else
            {
                ropeVisual.RopeRelease();
                spearhead.transform.SetParent(palmL, false);
                spearhead.transform.localPosition = new Vector3(-0.029f, -0.0383f, -0.0106f);
                spearhead.transform.localRotation = Quaternion.Euler(0f, 275f, 73f);
                spearhead.transform.localScale = Vector3.one;
            }

            animator.SetBool("WhiplashThrowing", whiplashState == WhiplashState.Throwing);
            animator.SetBool("WhiplashPulling", whiplashState == WhiplashState.Pulling);
            PlayerAudio.Instance.Loop("Whiplash Pull Loop", whiplashState == WhiplashState.Pulling);
            PlayerAudio.Instance.Loop("Whiplash Throw Loop", whiplashState == WhiplashState.Throwing);
            PlayerAudio.Instance.Loop("Whiplash Whoosh", whiplashState == WhiplashState.Throwing, currentWhiplashEnd);

            if (whiplashStatePrev != whiplashState)
            {
                if (whiplashState == WhiplashState.Pulling)
                    PlayerAudio.Instance.Play("Whiplash Pull Start");
                else if (whiplashState == WhiplashState.Throwing)
                    PlayerAudio.Instance.Play("Whiplash Throw Start");
                else if (whiplashState == WhiplashState.Idle)
                    PlayerAudio.Instance.Play("Whiplash Catch");
            }
            whiplashStatePrev = whiplashState;
        }

        void FixedUpdate()
        {
            this.recoil.Tick(Time.fixedDeltaTime);
            this.recoilHorizontal.Tick(Time.fixedDeltaTime);

            if (whiplashState == WhiplashState.Throwing)
            {
                LayerMask layerMask = 1 << 12 | 1 << 16 | 1 << 15;
                if (Physics.Raycast(currentWhiplashEnd, whiplashThrowVelocity.normalized, out RaycastHit hit, whiplashThrowVelocity.magnitude * Time.fixedDeltaTime, layerMask))
                {
                    whiplashState = WhiplashState.Pulling;
                    currentWhiplashEnd = hit.point;

                    EFTBallisticsInterface.Instance.Hit(hit, 5f);

                    if (hit.collider.TryGetComponent<BodyPartCollider>(out BodyPartCollider bpc))
                    {
                        Player bot = (bpc.Player as Player);

                        if (Plugin.WhiplashItemInHand.Value && !bot.HandsIsEmpty)
                        {
                            whiplashPullingObject = Instantiate(bot.HandsController.ControllerGameObject).transform;
                            whiplashPullingObject.GetComponent<ObservedLootItem>().MakePhysicsObject();

                            Vector3 worldGrabPoint = whiplashPullingObject.GetComponent<BoxCollider>().bounds.center;
                            whiplashGrabPointOffset = whiplashPullingObject.InverseTransformPoint(worldGrabPoint);

                            bot.HandsController.ControllerGameObject.transform.FindInChildrenExact("weapon").gameObject.SetActive(false);
                            bot.SetEmptyHands(null);

                            ParticleEffectManager.Instance.PlayGlint(worldGrabPoint + (worldGrabPoint - cam.transform.position).normalized * 0.2f, Color.white, 0.2f);
                            Singleton<UltraTime>.Instance.Freeze(0.01f, 0.5f);
                        }
                        else
                        {
                            whiplashPullingObject = bpc.Player.Transform.Original;
                            whiplashGrabPointOffset = whiplashPullingObject.InverseTransformPoint(hit.point);
                        }
                    }
                    else if (hit.collider.gameObject.TryGetComponentInParent<Door>(out Door door))
                    {
                        if (door.DoorState == EDoorState.Locked ||
                            door.DoorState == EDoorState.Shut)
                            door.Interact(EFT.EInteractionType.Breach);
                        else if (door.DoorState == EDoorState.Open)
                            door.Interact(EFT.EInteractionType.Close);
                    }
                    else if (hit.collider.gameObject.TryGetComponentInParent<ObservedLootItem>(out ObservedLootItem lootItem))
                    {
                        whiplashPullingObject = lootItem.transform;
                        whiplashGrabPointOffset = whiplashPullingObject.InverseTransformPoint(hit.point);

                        if (lootItem.TryGetComponent<Rigidbody>(out Rigidbody itemrb))
                            itemrb.isKinematic = true;
                        else
                            lootItem.MakePhysicsObject();
                    }
                }

                if (whiplashState == WhiplashState.Throwing)
                {
                    currentWhiplashEnd += whiplashThrowVelocity * Time.fixedDeltaTime;
                }
            }
            else if (whiplashState == WhiplashState.Pulling)
            {
                float reelSpeed = whiplashStartSpeed / 2f;

                if (whiplashPullingObject != null)
                {
                    whiplashPullingObject.position = currentWhiplashEnd - whiplashGrabPointOffset;
                    reelSpeed = whiplashStartSpeed / 4f;
                }

                currentWhiplashEnd += (palmL.position - currentWhiplashEnd).normalized * reelSpeed * Time.fixedDeltaTime;

                if (Vector3.Distance(cam.transform.position, currentWhiplashEnd) < reelSpeed * Time.fixedDeltaTime * 1.5f)
                {
                    whiplashState = WhiplashState.Idle;

                    if (whiplashPullingObject != null && whiplashPullingObject.gameObject.TryGetComponentInParent<Rigidbody>(out Rigidbody rb))
                    {
                        rb.isKinematic = false;
                        rb.angularVelocity = Random.onUnitSphere * 4f;
                    }
                    whiplashPullingObject = null;
                }
            }
        }

        void Shoot()
        {
            Vector3 origin = fireport.position - fireport.up * 0.1f;
            Vector3 dir = -fireport.up;

            float dmg = 20f;

            bool rail = (currentWeapon is SniperRifleItemClass);
            if (rail)
                dmg = 60f;

            bool shot = false;

            bool shotgun = (currentWeapon is ShotgunItemClass);

            bool machinegun = (currentWeapon is MachineGunItemClass);

            if (shotgun)
            {
                Shotgun.ShootProjectiles(origin, dir, rb.velocity, 9, 10f, Color.yellow);

                PlayerAudio.Instance.PlayShootShotgun();
                CameraShaker.Shake(0.5f);

                shot = true;
            }
            else if (machinegun)
            {
                Shotgun.ShootProjectiles(origin, dir, rb.velocity, 1, 3f, Color.red);

                //PlayerAudio.Instance.PlayShoot();
                CameraShaker.Shake(0.3f);

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
                reloadingTime = 0f;

                float recoilForce = 40f;
                Color colorMuzzle = Color.white;
                if (rail)
                {
                    recoilForce = 70f;
                    colorMuzzle = new Color(0.1f, 1f, 1f, 1f);
                }
                else if (shotgun)
                {
                    recoilForce = 60f;
                    colorMuzzle = new Color(1f, 1f, 0.1f, 1f);
                }
                else if (currentWeaponIsFullauto)
                {
                    recoilForce = Mathf.Lerp(-5f, 10f, Random.value);
                    colorMuzzle = new Color(1f, 0.1f, 0.1f, 1f); // the light does not light with pure (1,0,0,1) red for some reason
                }

                this.recoil.AddForce(recoilForce);

                recoilHorizontal.AddForce(Mathf.Lerp(-15, 15, Random.value));

                if (muzzleManager != null)
                    muzzleManager.Shot();

                if (!currentWeaponIsFullauto || Random.value > 0.5f)
                {
                    ParticleEffectManager.Instance.PlayGlint(origin, colorMuzzle, rail ? 0.5f : 0.2f);
                }
            }
        }

        

        void Coin()
        {
            spearhead.SetActive(false);

            //coinCooldown = 0.05f;

            animator.SetTrigger("Coin");

            coinTosser.Toss();

            PlayerAudio.Instance.Play("coinflip");
        }

        void Parry()
        {
            if ((Time.time - timeLastParry) < 0.1f)
                return;

            spearhead.SetActive(false);

            bool parried = false;

            int layerMask = 1 << 16 | 1 << 15 | 1 << 13;
            RaycastHit[] hits = Physics.SphereCastAll(cam.transform.position, 1.3f, cam.transform.forward, 3f, layerMask);

            RaycastHit hit = new RaycastHit();

            for (int i = 0; i < hits.Length; i++)
            {
                hit = hits[i];

                parried = EFTBallisticsInterface.Instance.Parry(hit, cam.transform);
                if (parried)
                    break;
            }

            if (!parried && whiplashState == WhiplashState.Pulling && Vector3.Distance(cam.transform.position, currentWhiplashEnd) < 3f)
            {
                if (whiplashPullingObject != null 
                    && (whiplashPullingObject.gameObject.layer == 13 || whiplashPullingObject.gameObject.layer == 15) 
                    && whiplashPullingObject.gameObject.TryGetComponentInParent<Rigidbody>(out Rigidbody rbitem))
                {
                    rbitem.gameObject.GetOrAddComponent<Projectile>().Parry(cam.transform);

                    whiplashState = WhiplashState.Idle;
                    whiplashPullingObject = null;

                    parried = true;
                }
            }

            if (parried)
            {
                timeLastParry = Time.time;

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