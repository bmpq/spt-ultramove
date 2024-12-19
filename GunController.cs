using System.Collections;
using UnityEngine;
using EFT.Ballistics;

namespace ultramove
{
    public class GunController : MonoBehaviour
    {
        Camera cam;

        Transform muzzle;
        ParticleEffectManager particles;

        private void Start()
        {
            cam = Camera.main;
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

            Vector3 rayDir = cam.transform.forward;
            Ray ray = new Ray(cam.transform.position, rayDir);

            int layer12 = 1 << 12; // HighPolyCollider
            int layer16 = 1 << 16; // HitCollider (body parts)
            int layer11 = 1 << 11; // Terrain
            int layerMask = layer12 | layer16 | layer11;

            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, layerMask))
            {
                MaterialType matHit = MaterialType.None;

                if (hit.transform.tag == "DynamicCollider")
                {
                    if (hit.rigidbody.TryGetComponent<Coin>(out Coin coin))
                    {
                        hit.point = coin.transform.position;
                        coin.Hit(hit);
                    }
                }
                else
                {
                    matHit = EFTBallisticsInterface.Instance.Hit(hit);
                }

                TrailRendererManager.Instance.Trail(muzzle.position, hit.point);

                if (matHit == MaterialType.Body || matHit == MaterialType.BodyArmor)
                    particles.PlayBloodEffect(hit.point, hit.normal);
            }
        }
    }
}