using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ultramove
{
    public class HandsController : MonoBehaviour
    {
        Animator animator;

        float coinCooldown;

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
            animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            coinCooldown -= Time.deltaTime;
            if (Input.GetMouseButtonDown(1))
            {
                if (coinCooldown <= 0f)
                    Coin();
            }
        }

        void Coin()
        {
            coinCooldown = 0.3f;

            animator.SetTrigger("Coin");
        }
    }
}