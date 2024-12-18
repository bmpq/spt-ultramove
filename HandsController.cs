using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ultramove
{
    public class HandsController : MonoBehaviour
    {
        Transform target;
        Camera cam;
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
            cam = Camera.main;
            target = transform.FindInChildrenExact("Base HumanRibcage");
            animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            target.localScale = new Vector3(1f, 1f, 0.8f);
            target.position = cam.transform.TransformPoint(new Vector3(0, -0.1f, 0));
            target.rotation = cam.transform.rotation;

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