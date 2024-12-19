using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ultramove
{
    public class Coin : MonoBehaviour
    {
        TrailRenderer trailRenderer;

        Collider[] colliders;

        float delayColliderActivation;

        public bool active { get; private set; }

        bool init;

        void Init()
        {
            init = true;
            trailRenderer = GetComponent<TrailRenderer>();

            List<Collider> list = new List<Collider>();
            foreach (Collider collider in GetComponentsInChildren<Collider>())
            {
                if (!collider.isTrigger)
                    list.Add(collider);
            }
            colliders = list.ToArray();
        }

        public void Activate()
        {
            if (!init)
                Init();

            active = true;
            delayColliderActivation = 0.2f;
            trailRenderer.emitting = true;
            trailRenderer.Clear();
        }

        public void Hit(float dmg)
        {
            active = false;
        }

        private void Update()
        {
            if (delayColliderActivation > 0f)
                delayColliderActivation -= Time.deltaTime;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (delayColliderActivation > 0f)
                return;
            Disable();
        }

        void Disable()
        {
            active = false;
            trailRenderer.emitting = false;
            trailRenderer.Clear();
        }
    }
}