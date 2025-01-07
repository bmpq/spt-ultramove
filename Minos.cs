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

    float timeIdle;
    protected override float GetStartingHealth() => 1000f;

    VolumetricLight[] eyeLights;

    void Awake()
    {
        animator = GetComponent<Animator>();

        Light[] lights = GetComponentsInChildren<Light>();
        eyeLights = new VolumetricLight[lights.Length];
        for (int i = 0; i < lights.Length; i++)
        {
            eyeLights[i] = lights[i].gameObject.GetOrAddComponent<VolumetricLight>();
        }

        Singleton<GameWorld>.Instance.MainPlayer.Transform.position = transform.position + new Vector3(0, 200, 0);
    }

    void LightEyes(bool on)
    {
        for (int i = 0; i < eyeLights.Length; i++)
        {
            eyeLights[i].Light.intensity = on ? 0.4f : 0;
            eyeLights[i].CheckIntensity();
        }
    }

    void Update()
    {
        timeIdle += Time.deltaTime;

        if (timeIdle > 4f)
        {
            timeIdle = 0f;

            string[] attacks = { "SlamLeft", "SlamMiddle" };
            animator.SetTrigger(attacks[Random.Range(0, attacks.Length)]);
        }

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
            transform.position = Vector3.Lerp(transform.position, new Vector3(-146.4899f, -56.2939f, -210.2503f), Time.deltaTime * 10f);
            //transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(345.2719f, 242.4667f, 0), Time.deltaTime * 10f);
        }
    }

    protected override void Revive()
    {
        base.Revive();

        transform.position = new Vector3(-142.2445f, -64.9049f, -197.0192f);
        transform.rotation = Quaternion.Euler(345.2719f, 199.993f, 0);

        LightEyes(true);
        animator.SetBool("Dead", false);
        animator.Play("Idle", 0);
    }

    protected override void Die()
    {
        base.Die();

        animator.SetBool("Dead", true);
        LightEyes(false);

        CameraShaker.ShakeAfterDelay(3f, 2.6f);
        CameraShaker.ShakeAfterDelay(3f, 3.13f);
    }
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
