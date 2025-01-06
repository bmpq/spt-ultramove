using System.Collections;
using System.Collections.Generic;
using ultramove;
using UnityEngine;

internal class Minos : UltraEnemy
{
    Animator animator;

    float timeIdle;

    float health;
    public bool alive => health > 0;
    protected override float GetStartingHealth() => 1000f;

    VolumetricLight[] eyeLights;

    protected override void Start()
    {
        base.Start();
        animator = GetComponent<Animator>();
        health = 1000f;

        Light[] lights = GetComponentsInChildren<Light>();
        eyeLights = new VolumetricLight[lights.Length];
        for (int i = 0; i < lights.Length; i++)
        {
            eyeLights[i] = lights[i].gameObject.GetOrAddComponent<VolumetricLight>();
        }
        LightEyes(true);
    }

    void LightEyes(bool on)
    {
        for (int i = 0; i < eyeLights[i].MaxRayLength; i++)
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

            string[] attacks = { "SlamLeft", "SlamRight", "SlamMiddle", "SlamMiddleLow" };
            animator.SetTrigger(attacks[Random.Range(0, attacks.Length)]);
        }
    }

    protected override void Die()
    {
        base.Die();
        animator.SetBool("Dead", true);
        LightEyes(false);
    }

    public bool Parry()
    {
        animator.SetTrigger("Parry");

        DamageInfoStruct dmg = new DamageInfoStruct();
        dmg.Damage = 500f;
        Hit(dmg);

        return true;
    }
}
