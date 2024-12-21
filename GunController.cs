using System.Collections;
using UnityEngine;
using EFT.Ballistics;

namespace ultramove
{
    public class GunController : MonoBehaviour
    {
        Camera cam;

        Transform muzzle;

        private void Start()
        {
            cam = Camera.main;

            muzzle = transform.FindInChildrenExact("muzzleflash_000");
            if (muzzle == null)
            {
                muzzle = transform.FindInChildrenExact("Base HumanRPalm");
            }
        }

        public bool Shoot()
        {
            float dmg = 20f;

            float rayDistance = 500f;

            Vector3 rayDir = cam.transform.forward;
            Ray ray = new Ray(cam.transform.position, rayDir);

            int layer12 = 1 << 12; // HighPolyCollider
            int layer16 = 1 << 16; // HitCollider (body parts)
            int layer11 = 1 << 11; // Terrain
            int layer15 = 1 << 15; // Loot (Coin)
            int layerMask = layer12 | layer16 | layer11 | layer15;

            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, layerMask))
            {
                MaterialType matHit = MaterialType.None;

                if (hit.transform.tag == "DynamicCollider")
                {
                    if (hit.rigidbody.TryGetComponent<Coin>(out Coin coin))
                    {
                        if (coin.active)
                        {
                            hit.point = coin.transform.position;
                            coin.Hit(dmg);
                        }
                    }
                }
                else
                {
                    matHit = EFTBallisticsInterface.Instance.Hit(hit, dmg);
                }

                TrailRendererManager.Instance.Trail(muzzle.position, hit.point, Color.white);

                if (matHit == MaterialType.Body || matHit == MaterialType.BodyArmor)
                    ParticleEffectManager.Instance.PlayBloodEffect(hit.point, hit.normal);

                return true;
            }

            return false;
        }
    }
}