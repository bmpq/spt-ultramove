using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ultramove
{
    internal class UltraTime : MonoBehaviour
    {
        float freeze;
        float delayBeforeFreezing;

        // Slow-motion variables
        float slowMoDurationRemaining = 0f;
        float targetTimeScale = 1f; // Default to normal timescale
        float slowMoTransitionDuration = 0.1f; // Duration of the smooth transition

        void Update()
        {
            // Handle delay before freezing
            if (delayBeforeFreezing > 0f)
            {
                delayBeforeFreezing -= Time.unscaledDeltaTime;
                return;
            }

            // Handle freezing
            if (freeze > 0f)
            {
                Time.timeScale = 0;
                freeze -= Time.unscaledDeltaTime;

                if (freeze <= 0f)
                    Time.timeScale = 1f;
                return; // Exit early to avoid slow-mo logic while freezing
            }

            if (Input.GetKey(KeyCode.Tab))
            {
                slowMoDurationRemaining = 0.1f;
                targetTimeScale = 0.4f;
            }

            // Handle slow-motion
            if (slowMoDurationRemaining > 0f)
            {
                slowMoDurationRemaining -= Time.unscaledDeltaTime;

                // Smoothly transition the timescale
                if (Time.timeScale != targetTimeScale)
                {
                    Time.timeScale = Mathf.MoveTowards(Time.timeScale, targetTimeScale, (1f / slowMoTransitionDuration) * Time.unscaledDeltaTime);
                }

                if (slowMoDurationRemaining <= 0f)
                {
                    // Smoothly transition back to normal time
                    targetTimeScale = 1f;
                }
            }
            else if (Time.timeScale < 1f) // If slow-mo is finished, ensure we return to 1
            {
                Time.timeScale = Mathf.MoveTowards(Time.timeScale, 1f, (1f / slowMoTransitionDuration) * Time.unscaledDeltaTime);
            }
            else if (Time.timeScale > 1f) // Safety to avoid issues if timescale accidentally goes above 1
            {
                Time.timeScale = 1f;
            }
        }

        public void Freeze(float delayBeforeFreezing, float freezeDuration)
        {
            this.delayBeforeFreezing = delayBeforeFreezing;
            freeze = freezeDuration;
        }

        public void SlowMo(float duration, float slowMoScale = 0.5f)
        {
            this.slowMoDurationRemaining = duration;
            this.targetTimeScale = slowMoScale;
        }
    }
}
