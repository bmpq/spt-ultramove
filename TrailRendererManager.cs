using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        private Queue<TrailRenderer> availableTrails;
        private Dictionary<TrailRenderer, float> activeTrails;

        Material mat;

        public void Init()
        {
            mat = new Material(Shader.Find("Sprites/Default"));

            availableTrails = new Queue<TrailRenderer>();
            activeTrails = new Dictionary<TrailRenderer, float>();

            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateNewTrailRenderer();
            }
        }

        public void Trail(Vector3 a, Vector3 b, Color color, float lifetime = 1f, float width = 0.06f)
        {
            TrailRenderer trail = GetTrail(1f);

            trail.transform.position = a;
            trail.Clear();
            trail.emitting = true;
            trail.startColor = color;
            trail.endColor = color;
            trail.time = lifetime;
            trail.startWidth = width;
            trail.endWidth = trail.startWidth;

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
                trail = availableTrails.Dequeue();
            }
            else
            {
                trail = CreateNewTrailRenderer();
            }

            lifetime = lifetime > 0 ? lifetime : defaultTrailLifetime;

            trail.time = lifetime;
            trail.startWidth = 0.06f;
            trail.endWidth = trail.startWidth;

            activeTrails[trail] = lifetime;

            return trail;
        }

        private void ReturnTrail(TrailRenderer trail)
        {
            if (trail == null || !activeTrails.ContainsKey(trail))
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

            foreach (TrailRenderer trail in activeTrails.Keys.ToArray())
            {
                activeTrails[trail] -= Time.deltaTime;

                trail.startWidth = Mathf.Max(0, trail.startWidth - Time.deltaTime / trail.time / 3f);
                trail.endWidth = trail.startWidth;

                if (activeTrails[trail] < 0)
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

            foreach (var trail in activeTrails.Keys)
            {
                if (trail != null) Destroy(trail.gameObject);
            }
        }
    }
}
