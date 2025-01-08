using Comfort.Common;
using EFT;
using EFT.Weather;
using System.Collections;
using System.Collections.Generic;
using ultramove;
using UnityEngine;

internal class Minos : UltraEnemy
{
    Animator animator;
    protected override float GetStartingHealth() => 1000f;

    VolumetricLight[] eyeLights;

    int attackId = 0;

    SkinnedMeshRenderer minosRenderer;
    Material matBodyAlive;
    Material matBodyDead;

    void Awake()
    {
        animator = GetComponent<Animator>();

        Light[] lights = GetComponentsInChildren<Light>();
        eyeLights = new VolumetricLight[lights.Length];
        for (int i = 0; i < lights.Length; i++)
        {
            eyeLights[i] = lights[i].gameObject.GetOrAddComponent<VolumetricLight>();
        }

        minosRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

        Singleton<GameWorld>.Instance.MainPlayer.Transform.position = transform.position + new Vector3(0, 200, 0);

        matBodyAlive = minosRenderer.materials[0];
        matBodyDead = AssetBundleLoader.BundleLoader.LoadAssetBundle(AssetBundleLoader.BundleLoader.GetDefaultModAssetBundlePath("ultrakill")).LoadAsset<Material>("MinosDead");
    }

    void LightEyes(bool on)
    {
        for (int i = 0; i < eyeLights.Length; i++)
        {
            eyeLights[i].Light.intensity = on ? 0.4f : 0;
            eyeLights[i].CheckIntensity();
        }
    }

    void FixedUpdate()
    {
        if (!animator.IsInTransition(0) && animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
        {
            string[] attacks = { "SlamLeft", "SlamMiddle" };
            animator.SetTrigger(attacks[attackId]);

            attackId = (attackId == 0) ? 1 : 0;
            animator.ResetTrigger(attacks[attackId]);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            Revive();
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            Die();
        }

        if (!alive)
        {
            transform.position = new Vector3(-151.4899f, -56.2939f, -210.2503f);
            //transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(345.2719f, 242.4667f, 0), Time.deltaTime * 10f);
        }
    }

    protected override void Revive()
    {
        base.Revive();

        transform.position = new Vector3(-142.2445f, -62.9049f, -197.0192f);
        transform.rotation = Quaternion.Euler(345.2719f, 199.993f, 0);

        LightEyes(true);
        Material[] mats = new Material[minosRenderer.materials.Length];
        mats[0] = matBodyAlive;
        for (int i = 1; i < minosRenderer.materials.Length; i++)
        {
            mats[i] = minosRenderer.materials[i];
        }
        minosRenderer.materials = mats;

        animator.SetBool("Dead", false);
        animator.Play("Idle", 0);

        TODControl.SetTime(6, 0);
        WeatherEffect(1f);
    }

    protected override void Die()
    {
        base.Die();

        animator.SetBool("Dead", true);
        LightEyes(false);
        Material[] mats = new Material[minosRenderer.materials.Length];
        mats[0] = matBodyDead;
        for (int i = 1; i < minosRenderer.materials.Length; i++)
        {
            mats[i] = minosRenderer.materials[i];
        }
        minosRenderer.materials = mats;

        CameraShaker.ShakeAfterDelay(3f, 2.6f);
        CameraShaker.ShakeAfterDelay(3f, 3.13f);

        StartCoroutine(AnimDie());
    }

    IEnumerator AnimDie()
    {
        yield return new WaitForSecondsRealtime(2.6f);

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * 0.15f;
            float e = Mathf.Sin((t * Mathf.PI) / 2);
            WeatherEffect(e);
            yield return null;
        }
    }

    void WeatherEffect(float t)
    {
        IWeatherCurve curve = WeatherController.Instance.WeatherCurve;

        float cloudDensity = -1f;
        float fog = curve.Fog;
        float rain = 0f;
        float lightningThunderProb = curve.LightningThunderProbability;
        float temperature = curve.Temperature;
        float windMagnitude = Mathf.Lerp(1f, 0.01f, t);
        int windDirection = 1;

        WeatherControl.SetWeather(cloudDensity, fog, rain, lightningThunderProb, temperature, windMagnitude, windDirection, windDirection);
    }

    public bool Parry()
    {
        if (!alive)
            return false;

        animator.SetTrigger("Parry");

        DamageInfoStruct dmg = new DamageInfoStruct();
        dmg.Damage = 500f;
        Hit(dmg);

        return true;
    }
}
