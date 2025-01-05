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

        void Update()
        {
            if (delayBeforeFreezing > 0f)
            {
                delayBeforeFreezing -= Time.unscaledDeltaTime;
                return;
            }

            if (freeze > 0f)
            {
                Time.timeScale = 0;
                freeze -= Time.unscaledDeltaTime;

                if (freeze <= 0f)
                    Time.timeScale = 1f;
            }
        }

        public void Freeze(float delayBeforeFreezing, float freezeDuration)
        {
            this.delayBeforeFreezing = delayBeforeFreezing;
            freeze = freezeDuration;
        }
    }
}
