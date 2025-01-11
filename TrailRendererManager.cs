using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Spektr;

namespace ultramove
{
    public class TrailRendererManager : MonoBehaviour
    {
        public static TrailRendererManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameObject("TrailsManager").AddComponent<TrailRendererManager>();
                    _instance.Init();
                }
                return _instance;
            }
        }
        private static TrailRendererManager _instance;

        private int initialPoolSize = 10;

        private float defaultTrailLifetime = 0.5f;

        private Queue<TrailRenderer> availableTrails;
        private HashSet<TrailRenderer> activeTrails;

        Material mat;

        Camera cam;

        Light[] railLights;
        Coroutine animStripLights;

        public void Init()
        {
            mat = new Material(Shader.Find("Sprites/Default"));

            cam = Camera.main;

            availableTrails = new Queue<TrailRenderer>();
            activeTrails = new HashSet<TrailRenderer>();

            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateNewTrailRenderer();
            }

            railLights = new Light[16];
            for (int i = 0; i < railLights.Length; i++)
            {
                railLights[i] = new GameObject("RailLight").AddComponent<Light>();
                railLights[i].intensity = 0f;
            }
        }

        public void Trail(Vector3 a, Vector3 b, bool rail)
        {
            if (rail)
                Trail(a, b, new Color(0.3f, 0.7f, 1f), 0.2f, true);
            else
                Trail(a, b, Color.white);
        }

        public void Trail(Vector3 a, Vector3 b, Color color, float width = 0.06f, bool lightning = false)
        {
            TrailRenderer trail = GetTrail();

            trail.transform.position = a;
            trail.Clear();
            trail.emitting = true;
            trail.startColor = color;
            trail.endColor = color;
            trail.time = 5f;

            trail.startWidth = width;
            trail.endWidth = trail.startWidth;

            StartCoroutine(WaitOneFrame(trail.transform, b));

            if (lightning)
            {
                LightningSettings lightningSettings = new LightningSettings();
                lightningSettings.source = a;
                lightningSettings.hit = b;
                lightningSettings.color = color;
                lightningSettings.noiseAmplitude = width * 2f;
                lightningSettings.fadeSpeed = 1f;
                Lightning.Strike(lightningSettings);

                Trail(a, Vector3.LerpUnclamped(b, cam.transform.position, 0.001f), Color.white, width / 2f, false);
                Trail(a, Vector3.LerpUnclamped(b, cam.transform.position, -0.001f), Color.white, width / 2f, false);

                if (animStripLights != null)
                    StopCoroutine(animStripLights);
                animStripLights = StartCoroutine(LightStrip(a, b, color));
            }
        }

        IEnumerator LightStrip(Vector3 a, Vector3 b, Color color)
        {
            float t = 0f;

            Vector3 dir = (b - a).normalized;
            float spacing = 2f;

            for (int i = 0; i < railLights.Length; i++)
            {
                railLights[i].transform.position = a + (i * dir * spacing);

                railLights[i].color = color;
                railLights[i].range = 6f;
                railLights[i].shadows = LightShadows.None;
            }

            while (t < 1f)
            {
                t = Mathf.Clamp01(t + Time.deltaTime * 1.5f);

                for (int i = 0; i < railLights.Length; i++)
                {
                    railLights[i].intensity = Mathf.Lerp(2f, 0, t);
                }

                yield return null;
            }
        }

        IEnumerator WaitOneFrame(Transform tr, Vector3 point)
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            tr.position = point;
        }

        private TrailRenderer GetTrail()
        {
            TrailRenderer trail;

            if (availableTrails.Count > 0)
            {
                trail = availableTrails.Dequeue();
            }
            else
            {
                trail = CreateNewTrailRenderer();
            }

            activeTrails.Add(trail);

            return trail;
        }

        private void ReturnTrail(TrailRenderer trail)
        {
            if (trail == null || !activeTrails.Contains(trail))
                return;

            activeTrails.Remove(trail);

            trail.Clear();
            trail.transform.SetParent(null);
            trail.transform.localPosition = Vector3.zero;

            trail.emitting = false;

            availableTrails.Enqueue(trail);
        }

        private TrailRenderer CreateNewTrailRenderer()
        {
            TrailRenderer trail = new GameObject().AddComponent<TrailRenderer>();

            trail.material = mat;

            trail.startColor = Color.white;
            trail.endColor = Color.white;

            availableTrails.Enqueue(trail);
            return trail;
        }

        private void Update()
        {
            List<TrailRenderer> toReturn = new List<TrailRenderer>();

            foreach (TrailRenderer trail in activeTrails)
            {
                trail.startWidth = trail.startWidth - Time.deltaTime * 0.34f;
                trail.endWidth = trail.startWidth;

                if (trail.startWidth <= 0f)
                    toReturn.Add(trail);
            }

            foreach (var item in toReturn)
            {
                ReturnTrail(item);
            }
        }

        private void OnDestroy()
        {
            foreach (var trail in availableTrails)
            {
                if (trail != null)
                    Destroy(trail.gameObject);
            }

            foreach (var trail in activeTrails)
            {
                if (trail != null) Destroy(trail.gameObject);
            }
        }
    }
}
