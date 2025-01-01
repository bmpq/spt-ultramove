using System.Collections;
using UnityEngine;
using EFT.Ballistics;

namespace ultramove
{
    public class GunController : MonoBehaviour
    {
        Camera cam;

        Transform muzzle;

        public void SetWeapon(GameObject gameObject)
        {
            cam = Camera.main;

            MuzzleManager manager = gameObject.GetComponent<MuzzleManager>();

            muzzle = manager.MuzzleJets[0].transform;
        }
    }
}