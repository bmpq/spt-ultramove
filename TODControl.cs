using Comfort.Common;
using EFT;
using SPT.Reflection.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ultramove
{
    internal static class TODControl
    {
        public static GameDateTime GameDateTime
        {
            get
            {
                return TOD_Sky.Instance.Components.Time.GameDateTime;
            }
        }

        public static void AddSeconds(float t)
        {
            GameDateTime.ResetForce(GameDateTime.Calculate().AddSeconds(t));
        }

        public static void SetTime(int targetTimeHours, int targetTimeMinutes)
        {
            DateTime currentDateTime = GameDateTime.Calculate();
            DateTime modifiedDateTime = currentDateTime.AddHours((double)targetTimeHours - currentDateTime.Hour);
            modifiedDateTime = modifiedDateTime.AddMinutes((double)targetTimeMinutes - currentDateTime.Minute);
            GameDateTime.ResetForce(modifiedDateTime);
        }
    }
}
