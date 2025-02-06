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
        public static void MakePhysicsObject(this ObservedLootItem itemObject, bool kinematicAtStart = true)
        {
            Component.Destroy(itemObject.GetComponentInChildren<Animator>());

            Collider[] allcols = itemObject.GetComponentsInChildren<Collider>();
            for (int i = 0; i < allcols.Length; i++)
            {
                allcols[i].enabled = false;
                Component.Destroy(allcols[i]);
            }

            itemObject.gameObject.layer = 15;
            itemObject.gameObject.transform.localScale = Vector3.one;

            if (itemObject.TryGetComponent<WeaponPrefab>(out WeaponPrefab weaponPrefab))
            {
                Quaternion origRot;
                Vector3 origPos;
                Transform bone_mele = itemObject.transform.FindInChildrenExact("bone_mele");
                if (bone_mele != null)
                {
                    origRot = bone_mele.rotation;
                    origPos = bone_mele.position;
                    bone_mele.SetParent(itemObject.transform, false);
                    itemObject.transform.rotation = Quaternion.identity;
                    bone_mele.localRotation = Quaternion.identity;
                    bone_mele.localPosition = Vector3.zero;
                }
                else
                {
                    Transform weapon = itemObject.transform.FindInChildrenExact("weapon");
                    origRot = weapon.rotation;
                    origPos = weapon.position;
                    weapon.SetParent(itemObject.transform, false);
                    itemObject.transform.rotation = Quaternion.identity;
                    weapon.localRotation = Quaternion.identity;
                    weapon.localPosition = Vector3.zero;
                }

                ColliderHelper.CreateBoundingBoxColliderInChildren(itemObject.gameObject);

                itemObject.transform.position = origPos;
                itemObject.transform.rotation = origRot;
            }
            else
            {
                Quaternion origItemRot = itemObject.transform.rotation;
                itemObject.transform.rotation = Quaternion.identity;
                ColliderHelper.CreateBoundingBoxColliderInChildren(itemObject.gameObject);
                itemObject.transform.rotation = origItemRot;
            }

            Rigidbody rb = itemObject.GetOrAddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.isKinematic = kinematicAtStart;

            if (itemObject.Item != null)
            {
                rb.mass = itemObject.Item.Weight;
            }

            SceneManager.MoveGameObjectToScene(itemObject.gameObject, SceneManager.GetActiveScene()); // pull back from dontdestroyonload
        }
    }
}
