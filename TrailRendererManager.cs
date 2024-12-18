using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ultramove
{
    public class TrailRendererManager : MonoBehaviour
    {
        private int initialPoolSize = 10;

        private float defaultTrailLifetime = 0.5f;

        private readonly List<TrailRenderer> availableTrails = new List<TrailRenderer>();
        private readonly Dictionary<TrailRenderer, Coroutine> activeTrailTimers = new Dictionary<TrailRenderer, Coroutine>();

        Shader shader;

        private void Awake()
        {
            shader = Shader.Find("Unlit/Color");

            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateNewTrailRenderer();
            }
        }

        public TrailRenderer GetTrail(float lifetime = -1f)
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

            // Default to the manager's lifetime setting if no lifetime is specified.
            lifetime = lifetime > 0 ? lifetime : defaultTrailLifetime;

            // Start the timer for automated returning.
            if (activeTrailTimers.ContainsKey(trail))
            {
                Debug.LogWarning("Trying to reuse a trail that already has an active timer.");
                return null;
            }

            activeTrailTimers[trail] = StartCoroutine(ReturnTrailAfterDelay(trail, lifetime));

            return trail;
        }

        public void ReturnTrail(TrailRenderer trail)
        {
            if (trail == null || !activeTrailTimers.ContainsKey(trail))
                return;

            // Stop the automated timer if it’s still running.
            if (activeTrailTimers.TryGetValue(trail, out Coroutine timer))
            {
                StopCoroutine(timer);
            }

            activeTrailTimers.Remove(trail);

            // Clear and reset the trail for reuse.
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

            trail.startWidth = 0.05f;
            trail.endWidth = 0.05f;

            trail.startColor = Color.white;
            trail.endColor = new Color(1, 1, 1, 0f);

            trail.time = 0.5f;

            availableTrails.Add(trail);
            return trail;
        }

        private IEnumerator ReturnTrailAfterDelay(TrailRenderer trail, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (trail != null)
            {
                ReturnTrail(trail);
            }
        }

        private void OnDestroy()
        {
            foreach (var trail in availableTrails)
            {
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
