using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using ultramove;
using UnityEngine;

[BepInPlugin("com.tarkin.ultramove", "ultramove", "1.0.0.0")]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Log;

    internal static ConfigEntry<bool> Enabled;
    internal static ConfigEntry<bool> WhiplashItemInHand;

    private void Start()
    {
        Log = base.Logger;

        InitConfiguration();

        new OnGameStarted().Enable();
        new OnGrenadeSetThrowForce().Enable();

        GameObject ultraTime = new GameObject("UltraTime");
        Singleton<UltraTime>.Create(ultraTime.AddComponent<UltraTime>());
        DontDestroyOnLoad(ultraTime);
    }

    private void InitConfiguration()
    {
        Enabled = Config.Bind("General", "Enabled", true, "");
        WhiplashItemInHand = Config.Bind("Whiplash", "Yoink", false, "Whiplash grabs the enemy's gun instead of the enemy");
    }
}