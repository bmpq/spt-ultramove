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

        TrailRendererManager trails;
        Transform muzzle;
        ParticleEffectManager particles;

        private void Start()
        {
            cam = Camera.main;
            trails = gameObject.GetOrAddComponent<TrailRendererManager>();
            particles = gameObject.GetOrAddComponent<ParticleEffectManager>();

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
                MaterialType matHit = EFTBallisticsInterface.Instance.Hit(hit);

                Trail(hit.point);

                if (matHit == MaterialType.Body || matHit == MaterialType.BodyArmor)
                    particles.PlayBloodEffect(hit.point, hit.normal);
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