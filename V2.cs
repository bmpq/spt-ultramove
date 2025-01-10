using Comfort.Common;
using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Systems.Effects;
using UnityEngine;

namespace ultramove
{
    internal class V2 : UltraEnemy
    {
        protected override float GetStartingHealth() => 2000f;

        bool crashedThroughRoof;
        bool landed;

        Animator animator;

        void Awake()
        {
            animator = GetComponent<Animator>();

            Singleton<GameWorld>.Instance.MainPlayer.Position = new Vector3(-91.9198f, 27.0865f, 156.2152f);
        }

        protected override void Revive()
        {
            base.Revive();

            TODControl.SetTime(2, 0);
            TOD_Sky.Instance.Components.Sky.transform.rotation = Quaternion.Euler(54.7627f, 31.5436f, 45.8763f);

            crashedThroughRoof = false;
            landed = false;
            transform.position = new Vector3(-91.9198f, 100f, 156.2152f);
            transform.localScale = Vector3.one * 0.5f;
            transform.rotation = Quaternion.Euler(0, 90f, 0);

            animator.Play("Intro", -1);
            animator.speed = 0f;
            animator.Rebind();
            animator.Update(0f);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.L))
                Revive();

            if (!crashedThroughRoof && transform.position.y < 45.5f)
            {
                crashedThroughRoof = true;
                PlayGlassCrashEffect();
                Singleton<Effects>.Instance.Emit("Glass", transform.position, Vector3.down);

                Singleton<UltraTime>.Instance.SlowMo(1f, 0.4f);
            }

            if (!landed)
            {
                transform.position += new Vector3(0, Physics.gravity.y * Time.deltaTime, 0);
                if (transform.position.y < 27.1f)
                {
                    landed = true;
                    transform.position = new Vector3(transform.position.x, 27.0865f, transform.position.z);

                    Singleton<Effects>.Instance.Emit("big_round_impact", transform.position, Vector3.up);

                    CameraShaker.Shake(1f);

                    animator.speed = 1f;
                }
            }
        }

        void PlayGlassCrashEffect()
        {

        }
    }
}
