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

    internal static ConfigEntry<bool> UndyingEnemies;

    internal static ConfigEntry<bool> MauriceAutoChargeback;
    internal static ConfigEntry<float> MauriceAutoChargebackOffset;
    internal static ConfigEntry<float> MauriceAutoChargebackDistanceMultiplier;

    internal static ConfigEntry<bool> TimedApexAutoShoot;

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

        UndyingEnemies = Config.Bind("UndyingEnemies", "UndyingEnemies", false, "");

        MauriceAutoChargeback = Config.Bind("Coin", "MauriceAutoChargeback", false, "Auto chargeback's a maurice if you are looking at it");
        MauriceAutoChargebackOffset = Config.Bind("Coin", "MauriceAutoChargebackOffset", -0.66f, "Auto chargeback timing");
        MauriceAutoChargebackDistanceMultiplier = Config.Bind("Coin", "MauriceAutoChargebackDistanceMultiplier", 0f, "Auto chargeback distance");

        TimedApexAutoShoot = Config.Bind("Coin", "TimedApexAutoShoot", false, "Auto shoot at the coin at its apex (cheat for split shots)");
    }
}