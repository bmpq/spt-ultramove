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
        float originalBloomThreshold;
        float originalBloomIntensity;
        bool originalUseBloom;

        void Start()
        {
            instance = this;
            prism = GetComponent<PrismEffects>();
            originalBloomThreshold = prism.bloomThreshold;
            originalBloomIntensity = prism.bloomIntensity;
            originalUseBloom = prism.useBloom;
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

            if (Time.timeScale < 0.1f)
            {
                prism.bloomThreshold = 0;
                prism.bloomIntensity = 0.2f;
                prism.useBloom = true;
            }
            else
            {
                prism.bloomThreshold = originalBloomThreshold;
                prism.bloomIntensity = originalBloomIntensity;
                prism.useBloom = originalUseBloom;
            }
        }
    }
}