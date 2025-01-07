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
        private static Type gameDateTime;
        private static MethodInfo calculateTime;
        private static MethodInfo resetTime;

        private static DateTime currentDateTime;
        private static DateTime modifiedDateTime;

        static TODControl()
        {
            gameDateTime = PatchConstants.EftTypes.Single(x => x.GetMethod("CalculateTaxonomyDate") != null);
            calculateTime = gameDateTime.GetMethod("Calculate", BindingFlags.Public | BindingFlags.Instance);
            resetTime = gameDateTime.GetMethods(BindingFlags.Public | BindingFlags.Instance).Single(x => x.Name == "Reset" && x.GetParameters().Length == 1);
        }

        public static void SetTime(int targetTimeHours, int targetTimeMinutes)
        {
            currentDateTime = (DateTime)calculateTime.Invoke(typeof(GameWorld).GetField("GameDateTime").GetValue(Singleton<GameWorld>.Instance), null);
            modifiedDateTime = currentDateTime.AddHours((double)targetTimeHours - currentDateTime.Hour);
            modifiedDateTime = modifiedDateTime.AddMinutes((double)targetTimeMinutes - currentDateTime.Minute);
            resetTime.Invoke(typeof(GameWorld).GetField("GameDateTime").GetValue(Singleton<GameWorld>.Instance), new object[] { modifiedDateTime });
        }
    }
}
