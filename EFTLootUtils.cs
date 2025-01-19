using EFT.AssetsManager;
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
            if (itemObject.TryGetComponent<AssetPoolObject>(out AssetPoolObject assetPoolObject))
            {
                foreach (var col in assetPoolObject.GetColliders(false))
                {
                    col.enabled = true;
                    col.gameObject.layer = 15;
                }
            }

            itemObject.gameObject.layer = 15;

            Rigidbody rb = itemObject.GetOrAddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.isKinematic = true;

            if (itemObject.Item != null)
            {
                rb.mass = itemObject.Item.Weight;
            }
            itemObject.SetItemAndRigidbody(itemObject.Item, rb);

            if (itemObject.GetComponent<Collider>() != null)
            {
                Component.Destroy(itemObject.GetComponent<Collider>());  // destroy the collider that gets added by unity when adding Rigidbody
            }
            SceneManager.MoveGameObjectToScene(itemObject.gameObject, SceneManager.GetActiveScene()); // pull back from dontdestroyonload
        }
    }
}
