﻿using EFT;
using EFT.Ballistics;
using System;
using System.Collections;
using System.Collections.Generic;
using ultramove;
using UnityEngine;

namespace ultramove
{
    internal class Maurice : UltraEnemy
    {
        Rigidbody rb;
        GameObject prefabProjectile;

        int projectileShotThisCycle;

        float cooldown;

        private readonly Queue<Projectile> projectilePool = new Queue<Projectile>();

        public bool chargingBeam { get; private set; }
        public float chargingBeamProgress { get; private set; }
        public const float BeamChargeTime = 3f;

        Light lightChargeBeam;

        BetterSource audioBeam;
        AudioClip clipBeamCharge;
        protected override float startingHealth => 400f;

        public void SetPrefabProjectile(GameObject prefabProjectile)
        {
            this.prefabProjectile = prefabProjectile;
            prefabProjectile.AddComponent<Projectile>();
            prefabProjectile.transform.GetChild(0).gameObject.AddComponent<AlwaysFaceCamera>();
        }

        private void AddNewProjectileToPool()
        {
            Projectile projectile = Instantiate(prefabProjectile).GetOrAddComponent<Projectile>();
            projectilePool.Enqueue(projectile);
        }

        private Projectile GetFromPool()
        {
            if (projectilePool.Count > 0)
            {
                return projectilePool.Dequeue();
            }

            AddNewProjectileToPool();
            return projectilePool.Dequeue();
        }

        private void ReturnToPool(Projectile projectile)
        {
            projectile.gameObject.SetActive(false);
            projectilePool.Enqueue(projectile);
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody>();

            rb.useGravity = false;

            lightChargeBeam = GetComponentInChildren<Light>();
            lightChargeBeam.intensity = 0;
            lightChargeBeam.transform.localScale = Vector3.zero;

            clipBeamCharge = PlayerAudio.Instance.GetClip("Throat Drone High Frequency2");
        }

        protected override void Revive()
        {
            base.Revive();

            rb.useGravity = false;

            rb.MovePosition(transform.position + new Vector3(0, 1f, 0));
            rb.MoveRotation(Quaternion.identity);

            audioBeam = PlayerAudio.Instance.GetSource();
        }

        protected override void Die()
        {
            base.Die();

            rb.useGravity = true;
            rb.drag = 1f;

            audioBeam.Release();
            lightChargeBeam.gameObject.SetActive(false);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.L))
            {
                Revive();
            }

            if (!alive)
                return;

            Vector3 targetDirection = EFTTargetInterface.GetPlayerPosition() - transform.position;
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 100f * Time.deltaTime);

            cooldown -= Time.deltaTime;

            if (chargingBeam)
            {
                chargingBeamProgress += Time.deltaTime;
                float t = chargingBeamProgress / 2f;
                t = Mathf.Clamp01(t);

                lightChargeBeam.gameObject.SetActive(true);
                lightChargeBeam.intensity = Mathf.Lerp(0, 30f, t);
                lightChargeBeam.transform.localScale = Vector3.one * Mathf.Lerp(0, 2.5f, t);

                if (chargingBeamProgress >= BeamChargeTime)
                {
                    ShootBeam();
                    lightChargeBeam.intensity = 0f;
                    lightChargeBeam.transform.localScale = Vector3.zero;
                    chargingBeam = false;
                    cooldown = 1f;
                }
                else
                {
                    audioBeam.SetPitch(Mathf.Lerp(0.8f, 2.5f, t));
                }
            }
            else if (cooldown < 0)
            {
                if (EFTTargetInterface.LineOfSight(transform.position, EFTTargetInterface.GetPlayerPosition() + new Vector3(0, 1.5f, 0), out RaycastHit hit))
                {
                    if (projectileShotThisCycle < 6)
                    {
                        ShootProjectile();

                        projectileShotThisCycle++;

                        cooldown = 0.2f;
                    }
                    else
                    {
                        cooldown = 1f;
                        projectileShotThisCycle = 0;

                        float distToPlayer = Vector3.Distance(EFTTargetInterface.GetPlayerPosition(), transform.position);
                        if (distToPlayer < 30f)
                        {
                            chargingBeamProgress = 0f;
                            chargingBeam = true;

                            audioBeam.SetBaseVolume(1f);
                            audioBeam.Play(clipBeamCharge, null, 1f);
                        }
                    }
                }
            }

            if (!chargingBeam && audioBeam.PlayBackState == BetterSource.EPlayBackState.Playing)
                audioBeam.SetBaseVolume(0f);
        }

        void ShootBeam()
        {
            Vector3 origin = lightChargeBeam.transform.position;

            RaycastHit[] hits = EFTBallisticsInterface.Instance.Shoot(origin, transform.forward, 900f);
            if (hits.Length > 0)
            {
                RaycastHit hit = hits[0];

                TrailRendererManager.Instance.Trail(origin, hit.point, new Color(1f, 0.4f, 0), 0.5f, true);

                EFTBallisticsInterface.Instance.Explosion(hit.point);
            }
        }

        void ShootProjectile()
        {
            Projectile projectile = GetFromPool();
            Vector3 startPos = transform.position + (transform.forward * 2f) - transform.up * 1.3f;

            projectile.enabled = true;
            projectile.Initialize(startPos, (EFTTargetInterface.GetPlayerPosition() - startPos).normalized * 40f);

            projectile.OnProjectileDone = ReturnToPool;

            PlayerAudio.Instance.PlayAtPoint("AnimeSlash2xpitch", transform.position);
        }

        void FixedUpdate()
        {
            if (!alive)
                return;

            float distToPlayer = Vector3.Distance(EFTTargetInterface.GetPlayerPosition(), transform.position);
            Vector3 targetVel = Vector3.zero;
            if (distToPlayer > 15f)
                targetVel = (EFTTargetInterface.GetPlayerPosition() - transform.position).normalized;

            float distToFloor = DistanceToFloor();
            if (distToFloor < 5f)
                targetVel.y = 5f;
            else if (distToFloor > 10f)
                targetVel.y = -5f;

            if (chargingBeam)
                targetVel = Vector3.zero;

            rb.velocity = targetVel;
        }

        float DistanceToFloor()
        {
            int layer1 = 1 << 18; // LowPolyCollider
            int layer2 = 1 << 11; // Terrain
            int layerMask = layer1 | layer2;

            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 10f, layerMask))
            {
                return Vector3.Distance(transform.position, hit.point);
            }

            return 100f;
        }
    }
}
