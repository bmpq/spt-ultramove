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
}
