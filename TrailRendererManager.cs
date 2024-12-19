using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        private readonly List<TrailRenderer> availableTrails = new List<TrailRenderer>();
        private readonly Dictionary<TrailRenderer, Coroutine> activeTrailTimers = new Dictionary<TrailRenderer, Coroutine>();

        Shader shader;

        public void Init()
        {
            shader = Shader.Find("Sprites/Default");

            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateNewTrailRenderer();
            }
        }

        public void Trail(Vector3 a, Vector3 b)
        {
            TrailRenderer trail = GetTrail(1f);
            trail.transform.position = a;
            trail.Clear();
            trail.emitting = true;

            StartCoroutine(WaitOneFrame(trail.transform, b));
        }

        IEnumerator WaitOneFrame(Transform tr, Vector3 point)
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            tr.position = point;
        }

        private TrailRenderer GetTrail(float lifetime = -1f)
        {
            TrailRenderer trail;

            if (availableTrails.Count > 0)
            {
                trail = availableTrails[availableTrails.Count - 1];
                availableTrails.RemoveAt(availableTrails.Count - 1);
            }
            else
            {
                trail = CreateNewTrailRenderer();
            }

            lifetime = lifetime > 0 ? lifetime : defaultTrailLifetime;

            // Start shrinking and return management coroutine.
            if (activeTrailTimers.ContainsKey(trail))
            {
                Debug.LogWarning("Trying to reuse a trail that already has an active timer.");
                return null;
            }

            trail.startWidth = 0.06f;
            trail.endWidth = trail.startWidth;

            activeTrailTimers[trail] = StartCoroutine(HandleTrailLifetime(trail, lifetime));

            return trail;
        }

        private void ReturnTrail(TrailRenderer trail)
        {
            if (trail == null || !activeTrailTimers.ContainsKey(trail))
                return;

            if (activeTrailTimers.TryGetValue(trail, out Coroutine timer))
            {
                StopCoroutine(timer);
            }

            activeTrailTimers.Remove(trail);

            trail.Clear();
            trail.transform.SetParent(null);
            trail.transform.localPosition = Vector3.zero;

            trail.emitting = false;

            availableTrails.Add(trail);
        }

        private TrailRenderer CreateNewTrailRenderer()
        {
            TrailRenderer trail = new GameObject().AddComponent<TrailRenderer>();

            trail.material = new Material(shader);

            trail.startColor = Color.white;
            trail.endColor = Color.white;

            trail.time = 0.5f;

            availableTrails.Add(trail);
            return trail;
        }

        private IEnumerator HandleTrailLifetime(TrailRenderer trail, float lifetime)
        {
            float elapsed = 0f;

            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;

                trail.startWidth = Mathf.Max(0, trail.startWidth - Time.deltaTime * 0.2f);
                trail.endWidth = trail.startWidth;

                yield return null;
            }

            ReturnTrail(trail);
        }

        private void OnDestroy()
        {
            foreach (var trail in availableTrails)
            {
                if (trail != null)
                    Destroy(trail.gameObject);
            }

            foreach (var trail in activeTrailTimers.Keys)
            {
                if (trail != null) Destroy(trail.gameObject);
            }

            activeTrailTimers.Clear();
        }
    }
}
