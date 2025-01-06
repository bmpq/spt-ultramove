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

    protected override void Start()
    {
        base.Start();
        animator = GetComponent<Animator>();
        health = 1000f;
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
