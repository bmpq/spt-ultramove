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

        void Start()
        {
            cam = Camera.main;
            target = transform.FindInChildrenExact("Base HumanRibcage");
            animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            target.localScale = new Vector3(1f, 1f, 0.9f);
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