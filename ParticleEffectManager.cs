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
                particlePool.Enqueue(CreateBloodEffectObject());
            }
        }

        public void PlayBloodEffect(Vector3 position, Vector3 normal)
        {
            ParticleSystem particleSystem;

            if (particlePool.Count > 0)
            {
                particleSystem = particlePool.Dequeue();
            }
            else
            {
                particleSystem = CreateBloodEffectObject();
            }

            Transform psTransform = particleSystem.transform;
            psTransform.position = position;
            psTransform.rotation = Quaternion.LookRotation(normal);

            particleSystem.gameObject.SetActive(true);
            particleSystem.Play();

            StartCoroutine(RecycleAfterFinish(particleSystem));
        }

        private ParticleSystem CreateBloodEffectObject()
        {
            GameObject bloodEffect = new GameObject("BloodEffect");
            ParticleSystem ps = bloodEffect.AddComponent<ParticleSystem>();

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

            var trails = ps.trails;
            trails.enabled = true;
            trails.ratio = 1.0f;
            trails.mode = ParticleSystemTrailMode.PerParticle;
            trails.lifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            trails.dieWithParticles = false;

            trails.textureMode = ParticleSystemTrailTextureMode.Stretch;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(2f, new AnimationCurve(new Keyframe(0, 1f), new Keyframe(1, 0)));
            trails.colorOverTrail = new ParticleSystem.MinMaxGradient(Color.red, new Color(0.5f, 0f, 0f, 0f));

            var particleRenderer = bloodEffect.GetComponent<ParticleSystemRenderer>();
            Material trailMaterial = new Material(Shader.Find("Sprites/Default"));
            trailMaterial.color = Color.red;
            particleRenderer.material = trailMaterial;
            particleRenderer.trailMaterial = trailMaterial;

            return ps;
        }

        private System.Collections.IEnumerator RecycleAfterFinish(ParticleSystem ps)
        {
            yield return new WaitForSeconds(ps.main.startLifetime.constantMax);

            particlePool.Enqueue(ps);
        }
    }
}
