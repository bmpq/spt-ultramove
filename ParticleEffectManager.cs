using AssetBundleLoader;
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

        ParticleSystem particleFall;
        ParticleSystem particleDash;

        private void Init()
        {
            particleFall = Instantiate(BundleLoader.LoadAssetBundle(BundleLoader.GetDefaultModAssetBundlePath("ultrakill")).LoadAsset<GameObject>("FallParticle")).GetComponent<ParticleSystem>();
            particleDash = Instantiate(BundleLoader.LoadAssetBundle(BundleLoader.GetDefaultModAssetBundlePath("ultrakill")).LoadAsset<GameObject>("DashParticle")).GetComponent<ParticleSystem>();

            // Pre-populate the pool with reusable particle systems
            for (int i = 0; i < PoolSize; i++)
            {
                particlePool.Enqueue(CreateParticleSystem());
            }
        }

        public void PlaySlam(Vector3 pos, bool on)
        {
            particleFall.transform.position = pos;

            if (particleFall.isPlaying != on)
            {
                particleFall.Clear();

                if (on)
                    particleFall.Play();
                else
                    particleFall.Stop();
            }
        }

        public void PlayDash(Vector3 pos, Vector3 dir, bool on)
        {
            particleDash.transform.position = pos;

            if (dir != Vector3.zero)
                particleDash.transform.rotation = Quaternion.LookRotation(dir);

            if (particleDash.isPlaying != on)
            {
                if (on)
                    particleDash.Play();
                else
                    particleDash.Stop();
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
