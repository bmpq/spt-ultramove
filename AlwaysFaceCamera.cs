using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlwaysFaceCamera : MonoBehaviour
{
    Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
            return;

        Vector3 toCamera = mainCamera.transform.position - transform.position;
        toCamera.y = 0;
        Quaternion lookRotation = Quaternion.LookRotation(toCamera.normalized);
        transform.rotation = lookRotation;
    }
}
