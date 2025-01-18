using EFT;
using System.Collections;
using UnityEngine;

namespace ultramove
{
    internal class EnemyPropeller : MonoBehaviour
    {
        Player player;
        public Vector3 addVelocity;

        void Start()
        {
            player = gameObject.GetComponent<Player>();
        }

        void LateUpdate()
        {
            if (addVelocity == Vector3.zero)
                return;

            player.Position += addVelocity * Time.deltaTime;
        }

        void FixedUpdate()
        {
            addVelocity = Vector3.Lerp(addVelocity, Vector3.zero, Time.fixedDeltaTime * 4f);
        }
    }
}
