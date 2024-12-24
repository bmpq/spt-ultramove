using EFT.Ballistics;
using UnityEngine;

namespace ultramove
{
    internal class Projectile : MonoBehaviour
    {
        bool primed;

        public void Parry(Transform source)
        {
            GetComponent<Rigidbody>().velocity = source.forward * 100f;
            primed = true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!primed)
                return;

            primed = false;

            EFTBallisticsInterface.Instance.Explosion(collision.contacts[0].point);
        }
    }
}
