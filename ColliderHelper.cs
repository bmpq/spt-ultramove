using UnityEngine;
using System.Collections.Generic;

public static class ColliderHelper
{
    public static void CreateBoundingBoxColliderInChildren(GameObject targetGameObject)
    {
        if (targetGameObject == null)
        {
            Plugin.Log.LogWarning("Target GameObject cannot be null.");
            return;
        }

        Renderer[] renderers = targetGameObject.GetComponentsInChildren<Renderer>();

        CreateBoundingBoxColliderInChildren(targetGameObject, renderers);
    }

    public static void CreateBoundingBoxColliderInChildren(GameObject targetGameObject, Renderer[] renderers)
    {
        if (targetGameObject == null)
        {
            Plugin.Log.LogWarning("Target GameObject cannot be null.");
            return;
        }

        if (renderers.Length == 0)
        {
            Plugin.Log.LogWarning("No MeshRenderers found in children to create a bounding box.");
            return;
        }

        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            combinedBounds.Encapsulate(renderers[i].bounds);
        }

        BoxCollider boxCollider = targetGameObject.GetOrAddComponent<BoxCollider>();
        boxCollider.center = targetGameObject.transform.InverseTransformPoint(combinedBounds.center);
        boxCollider.size = combinedBounds.size;


        Plugin.Log.LogInfo($"{targetGameObject.name}: Success creating bound box collider.");
    }
}