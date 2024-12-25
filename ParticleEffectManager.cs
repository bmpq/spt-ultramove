using System.Collections.Generic;
using UnityEngine;

namespace ultramove
{
    public class ParticleEffectManager : MonoBehaviour
    {
        public static ParticleEffectManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameObject("ParticleEffectManager").AddComponent<ParticleEffectManager>();
                    _instance.Init();
                }
                return _instance;
            }
        }
        private static ParticleEffectManager _instance;

        private Queue<ParticleSystem> particlePool = new Queue<ParticleSystem>();
        private const int PoolSize = 10; // Adjust based on expected usage

        private void Init()
        {
            // Pre-populate the pool with reusable particle systems
            for (int i = 0; i < PoolSize; i++)
            {
                particlePool.Enqueue(CreateParticleSystem());
            }
        }

        public void PlayEffectCoinRicochet(Transform tr)
        {
            ParticleSystem particleSystem = GetParticleSystem();

            SetEffectCoinRicochet(particleSystem);

            particleSystem.transform.position = tr.position;
            particleSystem.transform.rotation = tr.rotation;

            particleSystem.gameObject.SetActive(true);
            particleSystem.Play();

            StartCoroutine(RecycleAfterFinish(particleSystem));
        }

        public void PlayWhoosh(Transform tr)
        {
            ParticleSystem particleSystem = GetParticleSystem();

            SetEffectWhoosh(particleSystem);

            particleSystem.transform.SetParent(tr);
            particleSystem.transform.localPosition = Vector3.zero;
            particleSystem.transform.localRotation = Quaternion.identity;

            particleSystem.gameObject.SetActive(true);
            particleSystem.Play();

            StartCoroutine(RecycleAfterFinish(particleSystem));
        }

        public void PlayBloodEffect(Vector3 position, Vector3 normal)
        {
            ParticleSystem particleSystem = GetParticleSystem();

            SetEffectBlood(particleSystem);

            Transform psTransform = particleSystem.transform;
            psTransform.position = position;
            psTransform.rotation = Quaternion.LookRotation(normal);

            particleSystem.gameObject.SetActive(true);
            particleSystem.Play();

            StartCoroutine(RecycleAfterFinish(particleSystem));
        }

        private void SetEffectCoinRicochet(ParticleSystem ps)
        {
            var main = ps.main;
            main.loop = false;
            main.startColor = Color.yellow;
            main.startSize = 0.05f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.gravityModifier = 0;

            var limit = ps.limitVelocityOverLifetime;
            limit.drag = 200;
            limit.enabled = true;

            var emission = ps.emission;
            emission.rateOverTime = 0f; // No continuous emission
            var burst = new ParticleSystem.Burst(0f, 20);
            emission.SetBursts(new ParticleSystem.Burst[] { burst });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;
            shape.angle = 50;
            shape.scale = Vector3.one;

            var trails = ps.trails;
            trails.enabled = false;

            trails.worldSpace = false;
        }

        private void SetEffectWhoosh(ParticleSystem ps)
        {
            var main = ps.main;
            main.loop = false;
            main.startColor = Color.white;
            main.startSize = 0.1f;
            main.startSpeed = 0;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 2f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.gravityModifier = 0;

            var emission = ps.emission;
            emission.rateOverTime = 0f; // No continuous emission
            var burst = new ParticleSystem.Burst(0f, 300);
            emission.SetBursts(new ParticleSystem.Burst[] { burst });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = Vector3.one * 4f;

            var trails = ps.trails;
            trails.enabled = true;
            trails.ratio = 1.0f;
            trails.mode = ParticleSystemTrailMode.PerParticle;
            trails.lifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            trails.dieWithParticles = false;

            trails.textureMode = ParticleSystemTrailTextureMode.Stretch;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 1f), new Keyframe(1, 0)));
            trails.colorOverTrail = new ParticleSystem.MinMaxGradient(Color.white, new Color(1, 1, 1, 0f));

            trails.worldSpace = true;
        }

        private void SetEffectBlood(ParticleSystem ps)
        {
            var main = ps.main;
            main.loop = false;
            main.startColor = Color.red;
            main.startSize = 0.1f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2, 6);
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 2f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 1f;

            var emission = ps.emission;
            emission.rateOverTime = 0f; // No continuous emission
            var burst = new ParticleSystem.Burst(0f, 30);
            emission.SetBursts(new ParticleSystem.Burst[] { burst });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 40f;
            shape.radius = 0.3f;
            shape.scale = Vector3.one;

            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = false;

            var trails = ps.trails;
            trails.enabled = true;
            trails.ratio = 1.0f;
            trails.mode = ParticleSystemTrailMode.PerParticle;
            trails.lifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            trails.dieWithParticles = false;

            trails.textureMode = ParticleSystemTrailTextureMode.Stretch;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(2f, new AnimationCurve(new Keyframe(0, 1f), new Keyframe(1, 0)));
            trails.colorOverTrail = new ParticleSystem.MinMaxGradient(Color.red, new Color(0.5f, 0f, 0f, 0f));
        }

        ParticleSystem GetParticleSystem()
        {
            if (particlePool.Count > 0)
            {
                return particlePool.Dequeue();
            }
            else
            {
                return CreateParticleSystem();
            }
        }

        private ParticleSystem CreateParticleSystem()
        {
            GameObject bloodEffect = new GameObject("ParticleEffect");
            ParticleSystem ps = bloodEffect.AddComponent<ParticleSystem>();

            var particleRenderer = bloodEffect.GetComponent<ParticleSystemRenderer>();
            Material trailMaterial = new Material(Shader.Find("Sprites/Default"));
            particleRenderer.material = trailMaterial;
            particleRenderer.trailMaterial = trailMaterial;
            particleRenderer.trailMaterial.color = Color.white;

            ps.Stop();

            return ps;
        }

        private System.Collections.IEnumerator RecycleAfterFinish(ParticleSystem ps)
        {
            yield return new WaitForSeconds(ps.main.startLifetime.constantMax);

            ps.transform.SetParent(null);
            ps.Stop();

            particlePool.Enqueue(ps);
        }
    }
}
