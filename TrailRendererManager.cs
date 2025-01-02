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
        private HashSet<TrailRenderer> activeTrails;

        Material mat;

        public void Init()
        {
            mat = new Material(Shader.Find("Sprites/Default"));

            availableTrails = new Queue<TrailRenderer>();
            activeTrails = new HashSet<TrailRenderer>();

            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateNewTrailRenderer();
            }
        }

        public void Trail(Vector3 a, Vector3 b, Color color, float width = 0.06f)
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
