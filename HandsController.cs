using System.Collections;
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

        GunController gunController;
        CoinTosser coinTosser;

        public void SetWeapon(GameObject weapon)
        {
            Destroy(weapon.GetComponentInChildren<Animator>());

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
        }

        void Start()
        {
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
            coinCooldown -= Time.deltaTime;
            if (Input.GetMouseButtonDown(1))
            {
                if (coinCooldown <= 0f)
                    Coin();
            }

            if (Input.GetMouseButtonDown(0))
            {
                recoilTime = 0f;

                gunController.Shoot();
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
            coinCooldown = 0.3f;

            animator.SetTrigger("Coin");

            coinTosser.Toss();
        }
    }
}