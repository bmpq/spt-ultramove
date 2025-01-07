using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ultramove
{
    public class CameraShaker : MonoBehaviour
    {
        private static CameraShaker instance;

        private static float shakeIntensity;

        PrismEffects prism;

        void Start()
        {
            instance = this;
            prism = GetComponent<PrismEffects>();
            prism.bloomThreshold = 0f;
            prism.bloomIntensity = 0.5f;
        }

        private static IEnumerator DelayedShakeCoroutine(float intensity, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            Shake(intensity);
        }

        public static void ShakeAfterDelay(float intensity, float delay)
        {
            instance.StartCoroutine(DelayedShakeCoroutine(intensity, delay));
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

            prism.useBloom = Time.timeScale < 0.1f;
        }
    }
}