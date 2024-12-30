using EFT.Ballistics;
using System;
using UnityEngine;

namespace ultramove
{
    internal class Projectile : MonoBehaviour
    {
        public Rigidbody rb
        {
            get
            {
                if (_rb == null)
                    _rb = GetComponent<Rigidbody>();
                return _rb;
            }
        }
        private Rigidbody _rb;

        Collider col
        {
            get
            {
                if (_col == null)
                    _col = GetComponent<Collider>();
                return _col;
            }
        }
        private Collider _col;

        public TrailRenderer trail
        {
            get
            {
                if (_trail == null)
                    _trail = GetComponent<TrailRenderer>();
                return _trail;
            }
        }
        private TrailRenderer _trail;

        bool primed;

        public Action<Projectile> OnProjectileDone;

        float timeSinceSpawned;
        float lifetime;

        public void Initialize(Vector3 position, Vector3 velocity)
        {
            rb.useGravity = false;

            rb.isKinematic = false;
            col.enabled = true;
            col.isTrigger = true;

            transform.position = position;
            rb.position = position;
            rb.velocity = velocity;
            rb.transform.rotation = Quaternion.LookRotation(velocity.normalized);

            trail.Clear();
            trail.emitting = true;
            gameObject.SetActive(true);

            timeSinceSpawned = 0f;
            lifetime = 20f;
        }

        public void Disable()
        {
            trail.emitting = false;
            rb.isKinematic = true;
            col.enabled = false;
            this.enabled = false;
        }

        private void Update()
        {
            timeSinceSpawned += Time.deltaTime;

            if (timeSinceSpawned >= lifetime)
            {
                OnProjectileDone?.Invoke(this);
            }
        }

        public void Parry(Transform source)
        {
            this.enabled = true;

            Initialize(transform.position, source.forward * 100f);
            rb.useGravity = true;
            primed = true;
        }

        void OnTriggerEnter(Collider col)
        {
            if (!enabled)
                return;

            RaycastHit hit = new RaycastHit();
            hit.point = transform.position;
            hit.normal = Vector3.up;

            if (primed)
            {
                primed = false;
                EFTBallisticsInterface.Instance.Explosion(transform.position);
            }
            else
                EFTBallisticsInterface.Instance.Hit(col, hit, 30f);

            OnProjectileDone?.Invoke(this);
        }
    }
}
