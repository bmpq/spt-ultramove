using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ultramove
{
    public class GroundCheck : MonoBehaviour
    {
        public bool isGrounded => currentlyColliding.Count > 0;

        HashSet<Collider> currentlyColliding = new HashSet<Collider>();

        void Start()
        {
            gameObject.layer = LayerMask.NameToLayer("PlayerCollisionTest");
        }

        private void OnTriggerEnter(Collider other)
        {
            currentlyColliding.Add(other);
        }

        private void OnTriggerExit(Collider other)
        {
            currentlyColliding.Remove(other);
        }
    }
}