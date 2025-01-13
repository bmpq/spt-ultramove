using EFT.Interactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ultramove
{
    public static class EFTLootUtils
    {
        public static void MakePhysicsObject(this ObservedLootItem itemObject)
        {
            Collider[] allColliders = itemObject.GetComponentsInChildren<Collider>();
            Collider mainCol = allColliders[0];
            foreach (Collider collider in allColliders)
            {
                if (collider.gameObject.GetComponent<Renderer>() != null)
                {
                    collider.enabled = false;
                    continue;
                }

                mainCol = collider;
            }

            mainCol.enabled = true;
            mainCol.gameObject.transform.localScale *= 1.15f;

            itemObject.transform.parent = null;
            itemObject.gameObject.layer = 15;

            Rigidbody rb = itemObject.GetOrAddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.mass = itemObject.Item.Weight;

            itemObject.SetItemAndRigidbody(itemObject.Item, rb);

            if (itemObject.GetComponent<Collider>() != null)
            {
                Component.Destroy(itemObject.GetComponent<Collider>());  // destroy the collider that gets added by unity when adding Rigidbody
            }
            SceneManager.MoveGameObjectToScene(itemObject.gameObject, SceneManager.GetActiveScene()); // pull back from dontdestroyonload
        }
    }
}
