using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Minos : MonoBehaviour
{
    Animator animator;

    float timeIdle;

    float health;
    public bool alive => health > 0;

    void Start()
    {
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

    void Hit(float dmg)
    {
        if (!alive)
            return;

        health -= dmg;

        if (!alive)
            Die();
    }

    void Die()
    {
        animator.SetBool("Dead", true);
    }

    public bool Parry()
    {
        animator.SetTrigger("Parry");

        Hit(500);

        return true;
    }
}
