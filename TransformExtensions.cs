using System.Collections.Generic;
using UnityEngine;

internal static class TransformExtensions
{
    public static Transform FindInChildrenExact(this Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) 
                return child;

            Transform result = child.FindInChildrenExact(name);
            if (result != null) 
                return result;
        }
        return null;
    }

    public static List<GameObject> FindAllChildrenContaining(this Transform parent, string nameSubstring)
    {
        List<GameObject> matchingChildren = new List<GameObject>();

        // Recursive method to search the children and add them to the list if they contain the substring
        void SearchChildren(Transform current)
        {
            foreach (Transform child in current)
            {
                if (child.name.Contains(nameSubstring))
                {
                    matchingChildren.Add(child.gameObject);
                }
                SearchChildren(child); // Recursively search deeper in the hierarchy
            }
        }

        SearchChildren(parent); // Initiate the recursive search from the parent
        return matchingChildren;
    }

    public static bool TryGetComponentInParent<T>(this GameObject gameObject, out T component) where T : Component
    {
        component = gameObject.GetComponent<T>();

        if (component != null)
        {
            return true;
        }

        Transform currentTransform = gameObject.transform.parent;
        while (currentTransform != null)
        {
            component = currentTransform.GetComponent<T>();
            if (component != null)
            {
                return true;
            }

            currentTransform = currentTransform.parent;
        }

        component = null;
        return false;
    }

    public static bool IsLookingAt(this Transform looker, Transform target, float angleMargin)
    {
        Vector3 directionToTarget = target.position - looker.position;
        Vector3 forward = looker.forward;
        float angle = Vector3.Angle(forward, directionToTarget);
        return angle <= angleMargin;
    }

    public static bool IsLookingAt(this Transform looker, Vector3 targetPosition, float angleMargin)
    {
        Vector3 directionToTarget = targetPosition - looker.position;
        Vector3 forward = looker.forward;
        float angle = Vector3.Angle(forward, directionToTarget);
        return angle <= angleMargin;
    }

    public static GameObject FindGameObjectWithComponentAtPosition<T>(Vector3 position, float tolerance = 0.001f) where T : Component
    {
        T[] components = Object.FindObjectsOfType<T>();

        foreach (T component in components)
        {
            if (component != null && Vector3.Distance(component.transform.position, position) <= tolerance)
            {
                return component.gameObject;
            }
        }

        return null;
    }
}
