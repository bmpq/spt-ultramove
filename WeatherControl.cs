using EFT.Weather;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ultramove
{
    internal static class WeatherControl
    {
        private static WeatherController weatherController => WeatherController.Instance;

        private static float cloudDensity;
        private static float fog = 0.001f;
        private static float rain;
        private static float lightningThunderProb;
        private static float temperature;
        private static float windMagnitude;
        private static int windDir = 2;
        private static WeatherDebug.Direction windDirection;
        private static int topWindDir = 2;
        private static Vector2 topWindDirection;

        static WeatherControl()
        {
            Reflection.GetFieldInfos();
        }

        public static void SetWeather(float newCloudDensity, float newFog, float newRain, float newLightningThunderProb, float newTemperature, float newWindMagnitude, int newWindDir, int newTopWindDir)
        {
            if (weatherController == null)
            {
                Debug.LogError("[TWChanger]: WeatherController is null, cannot set weather. (Must be called in a raid).");
                return;
            }

            cloudDensity = newCloudDensity;
            fog = newFog;
            rain = newRain;
            lightningThunderProb = newLightningThunderProb;
            temperature = newTemperature;
            windMagnitude = newWindMagnitude;
            windDir = newWindDir;
            windDirection = (WeatherDebug.Direction)windDir;
            topWindDir = newTopWindDir;

            // Update the Weather Debugger
            UpdateWeatherDebug();
        }

        private static void UpdateWeatherDebug()
        {
            if (weatherController?.WeatherDebug == null)
                return;

            weatherController.WeatherDebug.Enabled = true;
            weatherController.WeatherDebug.CloudDensity = cloudDensity;

            // These must be done through reflection due to an ambiguous reference. (as of 3.9.0)
            Reflection.FogField.SetValue(weatherController.WeatherDebug, fog);
            Reflection.LighteningThunderField.SetValue(weatherController.WeatherDebug, lightningThunderProb);
            Reflection.RainField.SetValue(weatherController.WeatherDebug, rain);
            Reflection.TemperatureField.SetValue(weatherController.WeatherDebug, temperature);

            weatherController.WeatherDebug.TopWindDirection = GetTopWindDirection(topWindDir);
            weatherController.WeatherDebug.WindDirection = windDirection;
            weatherController.WeatherDebug.WindMagnitude = windMagnitude;
        }

        private static Vector2 GetTopWindDirection(int topWindDir)
        {
            switch (topWindDir)
            {
                case 1:
                    return Vector2.down;
                case 2:
                    return Vector2.left;
                case 3:
                    return Vector2.one;
                case 4:
                    return Vector2.right;
                case 5:
                    return Vector2.up;
                case 6:
                    return Vector2.zero;

                default: return Vector2.zero;
            }
        }
    }

    public static class Reflection
    {
        public static FieldInfo FogField;
        public static FieldInfo LighteningThunderField;
        public static FieldInfo RainField;
        public static FieldInfo TemperatureField;

        public static void GetFieldInfos()
        {
            FogField = AccessTools.Field(typeof(WeatherDebug), "Fog");
            LighteningThunderField = AccessTools.Field(typeof(WeatherDebug), "LightningThunderProbability");
            RainField = AccessTools.Field(typeof(WeatherDebug), "Rain");
            TemperatureField = AccessTools.Field(typeof(WeatherDebug), "Temperature");
        }
    }
}
