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

        BetterSource betterSource;

        public static AudioClip audioTwirl;

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

            betterSource = Singleton<BetterAudio>.Instance.GetSource(BetterAudio.AudioSourceGroupType.Weaponry, true);
            betterSource.Loop = true;
            betterSource.Play(audioTwirl, null, 1f, 1f, false, false);
        }

        void OnDisable()
        {
            if (betterSource != null)
            {
                betterSource.Release();
            }
        }

        public void Disable()
        {
            trail.emitting = false;
            col.enabled = false;
            this.enabled = false;
        }

        private void Update()
        {
            if (betterSource != null)
                betterSource.Position = transform.position;

            timeSinceSpawned += Time.deltaTime;

            if (timeSinceSpawned >= lifetime)
            {
                OnProjectileDone?.Invoke(this);
            }
        }

        public void Parry(Transform source)
        {
            this.enabled = true;

            rb.velocity = source.forward * 100f;
            primed = true;
        }

        void OnCollisionEnter(Collision col)
        {
            if (!enabled)
                return;

            if (primed)
            {
                primed = false;
                EFTBallisticsInterface.Instance.Hit(col, 300f);
                EFTBallisticsInterface.Instance.Explosion(transform.position);
            }
            else
                EFTBallisticsInterface.Instance.Hit(col, 30f);

            OnProjectileDone?.Invoke(this);
            gameObject.SetActive(false);
        }
    }
}
