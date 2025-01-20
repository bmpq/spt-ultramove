using Comfort.Common;
using EFT.Ballistics;
using System;
using UnityEngine;

namespace ultramove
{
    internal class Projectile : MonoBehaviour, IParryable
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
        float timeSinceParried = 999f;

        void Start()
        {
            gameObject.layer = 13;
            lifetime = 20f;
        }

        public void Initialize(Vector3 position, Vector3 velocity)
        {
            rb.useGravity = false;

            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            col.enabled = true;
            col.isTrigger = false;

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
            col.enabled = false;
            this.enabled = false;
        }

        void OnDisable()
        {
            primed = false;
        }

        private void Update()
        {
            timeSinceSpawned += Time.deltaTime;
            timeSinceParried += Time.deltaTime;

            if (timeSinceSpawned >= lifetime)
            {
                OnProjectileDone?.Invoke(this);
                gameObject.SetActive(false);
            }
        }

        public void Parry(Transform source)
        {
            this.enabled = true;

            rb.isKinematic = false;
            rb.useGravity = true;
            rb.angularVelocity = UnityEngine.Random.onUnitSphere * 60f;
            rb.velocity = source.forward * 40f;
            //rb.velocity + (source.forward * 30f) + (Vector3.up * 24f);
            primed = true;
            timeSinceParried = 0f;
        }

        void OnCollisionEnter(Collision col)
        {
            if (!enabled)
                return;

            if (timeSinceParried < 0.05f)
                return;

            if (primed)
            {
                primed = false;
                EFTBallisticsInterface.Instance.Hit(col, 300f);
                EFTBallisticsInterface.Instance.Explosion(transform.position);
            }
            else
                EFTBallisticsInterface.Instance.Hit(col, 30f);

            gameObject.SetActive(false);
            OnProjectileDone?.Invoke(this);
        }
    }
}
