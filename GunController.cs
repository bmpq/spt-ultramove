using Comfort.Common;
using EFT;
using EFT.Ballistics;
using System.Collections;
using System.Collections.Generic;
using Systems.Effects;
using UnityEngine;

namespace ultramove
{
    public class GunController : MonoBehaviour
    {
        Camera cam;

        IPlayerOwner player;

        TrailRendererManager trails;
        Transform muzzle;

        private void Start()
        {
            cam = Camera.main;
            trails = gameObject.GetOrAddComponent<TrailRendererManager>();
            player = Singleton<GameWorld>.Instance.GetAlivePlayerBridgeByProfileID(Singleton<GameWorld>.Instance.MainPlayer.ProfileId);

            muzzle = transform.FindInChildrenExact("muzzleflash_000");
            if (muzzle == null)
            {
                muzzle = transform.FindInChildrenExact("Base HumanRPalm");
            }
        }

        public void Shoot()
        {
            float rayDistance = 500f;
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);

            int layer12 = 1 << 12; // HighPolyCollider
            int layer16 = 1 << 16; // HitCollider (body parts)
            int layer11 = 1 << 11; // Terrain
            int layerMask = layer12 | layer16 | layer11;

            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, layerMask))
            {
                BallisticCollider ballisticCollider = hit.collider.gameObject.GetComponent<BallisticCollider>();
                if (ballisticCollider != null)
                {
                    DamageInfoStruct damageInfo = new DamageInfoStruct
                    {
                        DamageType = EDamageType.Bullet,
                        Damage = 50,
                        ArmorDamage = 15,
                        StaminaBurnRate = 20,
                        PenetrationPower = 15,
                        Direction = UnityEngine.Random.onUnitSphere,
                        HitNormal = hit.normal,
                        HitPoint = hit.point,
                        Player = player,
                        IsForwardHit = true,
                        HittedBallisticCollider = ballisticCollider
                    };

                    ballisticCollider.ApplyHit(damageInfo, ShotIdStruct.EMPTY_SHOT_ID);

                    Singleton<Effects>.Instance.Emit(ballisticCollider.TypeOfMaterial, ballisticCollider, hit.point, hit.normal, 0.1f);

                    Trail(hit.point);
                }
                else
                {
                    Plugin.Log.LogWarning($"{hit.collider.gameObject.name} does not contain BallisticCollider!");
                }
            }
        }

        void Trail(Vector3 hitpoint)
        {
            TrailRenderer trail = trails.GetTrail(1f);
            trail.transform.position = muzzle.position;
            trail.Clear();
            trail.emitting = true;

            StartCoroutine(WaitOneFrame(trail.transform, hitpoint));
        }

        IEnumerator WaitOneFrame(Transform tr, Vector3 point)
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            tr.position = point;
        }
    }
}