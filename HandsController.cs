using EFT.Ballistics;
using System.Collections.Generic;
using UnityEngine;

namespace ultramove
{
    public class HandsController : MonoBehaviour
    {
        Animator animator;

        float coinCooldown;

        Transform palm;
        Transform recoilPivot;
        Vector3 recoilHighPos;
        Quaternion recoilHighRot;
        Quaternion recoilPivotOriginalLocalQuaternion;
        Vector3 recoilPivotOriginalLocalPosition;
        float recoilTime;

        float parryPause;

        GunController gunController;
        MuzzleManager muzzleManager;

        CoinTosser coinTosser;

        Camera cam;


        public void SetWeapon(GameObject weapon)
        {
            Destroy(weapon.GetComponentInChildren<Animator>());

            muzzleManager = weapon.GetComponent<MuzzleManager>();

            Transform container = weapon.transform.FindInChildrenExact("weapon");
            container.SetParent(null);
            container.position = Vector3.zero;
            container.rotation = Quaternion.identity;

            Transform handMarker = container.transform.FindInChildrenExact("weapon_R_hand_marker");

            Transform palm = transform.FindInChildrenExact("Base HumanRPalm");

            Vector3 offset = handMarker.position;
            for (int i = 0; i < container.childCount; i++)
            {
                container.GetChild(i).position -= offset;
            }

            container.SetParent(palm, false);
            container.localPosition = new Vector3(0, -0.025f, 0);
            container.localEulerAngles = new Vector3(0, 180, 90f);

            weapon.transform.SetParent(palm, false);
            weapon.transform.localPosition = Vector3.zero;
        }

        void Start()
        {
            cam = Camera.main;

            gunController = gameObject.GetComponent<GunController>();
            coinTosser = gameObject.GetOrAddComponent<CoinTosser>();

            animator = GetComponentInChildren<Animator>();

            recoilPivot = transform.FindInChildrenExact("Base HumanRCollarbone");
            recoilPivotOriginalLocalQuaternion = recoilPivot.localRotation;
            recoilPivotOriginalLocalPosition = recoilPivot.localPosition;

            recoilHighRot = recoilPivotOriginalLocalQuaternion * Quaternion.Euler(40f, 0, 0f);
            recoilHighPos = recoilPivotOriginalLocalPosition - new Vector3(0.1f, 0, 0.5f);

            palm = transform.FindInChildrenExact("Base HumanRPalm");
        }

        private void Update()
        {
            if (parryPause > 0f)
            {
                parryPause -= Time.unscaledDeltaTime;

                if (parryPause < 0.25f)
                {
                    Time.timeScale = 0;
                }
            }
            else
            {
                Time.timeScale = 1f;
            }

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
                bool shot = gunController.Shoot();

                if (shot)
                {
                    recoilTime = 0f;
                    if (muzzleManager != null)
                        muzzleManager.Shot();

                    PlayerAudio.Instance.PlayShoot();
                }
            }

            recoilTime += Time.deltaTime;

            float recoil = CalculateRecoil(recoilTime, 1f, 40f, 5f);
            recoilPivot.localRotation = Quaternion.Lerp(recoilPivotOriginalLocalQuaternion, recoilHighRot, recoil);
            recoilPivot.localPosition = Vector3.Lerp(recoilPivotOriginalLocalPosition, recoilHighPos, recoil);
        }

        float CalculateRecoil(float time, float intensity, float riseSpeed, float recoveryRate)
        {
            riseSpeed = Mathf.Max(0.01f, riseSpeed); // Prevent division issues.
            recoveryRate = Mathf.Max(0.01f, recoveryRate);

            float rise = 1f - Mathf.Exp(-riseSpeed * time);
            float fall = Mathf.Exp(-recoveryRate * time);

            float recoil = intensity * rise * fall;

            return Mathf.Max(0f, recoil);
        }

        void Coin()
        {
            coinCooldown = 0.15f;

            animator.SetTrigger("Coin");

            coinTosser.Toss();

            PlayerAudio.Instance.Play("coinflip");
        }

        void Parry()
        {
            bool parried = false;

            int layerMask = 1 << 16;
            RaycastHit[] hits = Physics.SphereCastAll(cam.transform.position, 0.6f, cam.transform.forward, 2f, layerMask);

            Plugin.Log.LogInfo("raycast result: " + hits.Length);

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];

                parried = EFTBallisticsInterface.Instance.Parry(hit);

                if (parried)
                    break;
            }

            if (parried)
            {
                animator.SetTrigger("Parry");
                parryPause = 0.3f;

                CameraShaker.Shake(1f);
                PlayerAudio.Instance.Play("Ricochet");
            }
        }
    }
}