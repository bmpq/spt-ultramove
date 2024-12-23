using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ultramove
{
    public class CameraShaker : MonoBehaviour
    {
        private static float shakeIntensity;

        void Start()
        {

        }

        public static void Shake(float intensity)
        {
            shakeIntensity = Mathf.Max(intensity, shakeIntensity);
        }

        void Update()
        {
            Vector3 randomOffset = Random.insideUnitSphere * shakeIntensity;
            transform.localPosition = Vector3.Lerp(Vector3.zero, randomOffset, Time.unscaledDeltaTime * 10f);

            shakeIntensity -= Time.unscaledDeltaTime * 2f;
            if (shakeIntensity < 0f)
                shakeIntensity = 0f;
        }
    }
}