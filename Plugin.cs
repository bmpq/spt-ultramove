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
    internal static ConfigEntry<bool> SpawnMaurice;

    internal static ConfigEntry<bool> MauriceAutoChargeback;
    internal static ConfigEntry<float> MauriceAutoChargebackOffset;
    internal static ConfigEntry<float> MauriceAutoChargebackDistanceMultiplier;

    internal static ConfigEntry<bool> TimedApexAutoShoot;
    internal static ConfigEntry<bool> FirstCoinSlowMo;

    internal static ConfigEntry<float> ParryRange;

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
        Enabled = Config.Bind("_General", "Enabled", true, "");
        WhiplashItemInHand = Config.Bind("Whiplash", "Yoink", false, "Whiplash grabs the enemy's gun instead of the enemy");

        UndyingEnemies = Config.Bind("Ultra Enemies", "UndyingEnemies", false, "");
        SpawnMaurice = Config.Bind("Ultra Enemies", "SpawnMaurice", true, "");

        MauriceAutoChargeback = Config.Bind("Coin", "MauriceAutoChargeback", false, "Auto chargeback's a maurice if you are looking at it");
        MauriceAutoChargebackOffset = Config.Bind("Coin", "MauriceAutoChargebackOffset", -0.66f, "Auto chargeback timing");
        MauriceAutoChargebackDistanceMultiplier = Config.Bind("Coin", "MauriceAutoChargebackDistanceMultiplier", 0f, "Auto chargeback distance");

        TimedApexAutoShoot = Config.Bind("Coin", "TimedApexAutoShoot", false, "Auto shoot at the coin at its apex (cheat for split shots)");
        FirstCoinSlowMo = Config.Bind("Coin", "FirstCoinSlowMo", false, "First tossed of the match coin triggers slow mo");

        ParryRange = Config.Bind("Parry", "ParryRange", 2f, "");
    }
}