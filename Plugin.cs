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

    internal static ConfigEntry<bool> WhiplashYoink;
    internal static ConfigEntry<bool> DebugDrawBoxCollider;

    internal static ConfigEntry<bool> UndyingEnemies;
    internal static ConfigEntry<bool> SpawnMaurice;
    internal static ConfigEntry<bool> SpawnV2;
    internal static ConfigEntry<bool> SpawnMinos;

    internal static ConfigEntry<bool> MauriceAutoChargeback;
    internal static ConfigEntry<float> MauriceAutoChargebackOffset;
    internal static ConfigEntry<float> MauriceAutoChargebackDistanceMultiplier;

    internal static ConfigEntry<bool> TimedApexAutoShoot;
    internal static ConfigEntry<bool> FirstCoinSlowMo;

    internal static ConfigEntry<float> ParryRange;
    internal static ConfigEntry<bool> ParryContinuous;
    internal static ConfigEntry<bool> Punch;
    internal static ConfigEntry<float> PunchDamage;

    internal static ConfigEntry<float> DamageProjectile;
    internal static ConfigEntry<float> DamageRevolver;
    internal static ConfigEntry<float> DamageRail;

    internal static ConfigEntry<float> TimeScale;
    internal static ConfigEntry<KeyboardShortcut> KeybindSlowMo;

    internal static ConfigEntry<float> GroundSlamInfluence;

    internal static ConfigEntry<float> RagdollAddForce;

    private void Start()
    {
        Log = base.Logger;

        InitConfiguration();

        new OnGameStarted().Enable();
        new OnGrenadeSetThrowForce().Enable();
        new PatchPreventApplyGravity().Enable();

        GameObject ultraTime = new GameObject("UltraTime");
        Singleton<UltraTime>.Create(ultraTime.AddComponent<UltraTime>());
        DontDestroyOnLoad(ultraTime);
    }

    private void InitConfiguration()
    {
        Enabled = Config.Bind("_General", "Enabled", true, "");
        TimeScale = Config.Bind("_General", "TimeScale", 0.4f, "Hold SlowMo key to apply this time scale");
        KeybindSlowMo = Config.Bind("_General", "KeybindSlowMo", new KeyboardShortcut(KeyCode.CapsLock), "Hold to apply TimeScale");

        WhiplashYoink = Config.Bind("Whiplash", "Yoink", false, "Whiplash grabs the bot's equipment instead of the whole bot");
        DebugDrawBoxCollider = Config.Bind("Whiplash", "DebugDrawBoxCollider", false, "Draw debug overlay for the generated item collider");

        DamageProjectile = Config.Bind("Weapons", "DamageProjectile", 25f, "Shotgun and machinegun use projectiles");
        DamageRevolver = Config.Bind("Weapons", "DamageRevolver", 40f, "");
        DamageRail = Config.Bind("Weapons", "DamageRail", 300f, "");

        UndyingEnemies = Config.Bind("Ultra Enemies", "UndyingEnemies", false, "");
        SpawnMaurice = Config.Bind("Ultra Enemies", "Spawn Maurice", true, "Spawn a Maurice on any map on (-10, 5, -10) coordinates");
        SpawnV2 = Config.Bind("Ultra Enemies", "Spawn V2", true, "Spawn the V2 cutscene on Interchange");
        SpawnMinos = Config.Bind("Ultra Enemies", "Spawn Minos", true, "Spawn Minos on Woods");

        MauriceAutoChargeback = Config.Bind("Coin", "MauriceAutoChargeback", false, "Auto chargeback's a maurice if you are looking at it (this aint reliable, does not compensate for angles)");
        MauriceAutoChargebackOffset = Config.Bind("Coin", "MauriceAutoChargebackOffset", -0.66f, "Auto chargeback timing");
        MauriceAutoChargebackDistanceMultiplier = Config.Bind("Coin", "MauriceAutoChargebackDistanceMultiplier", 0f, "Auto chargeback distance");

        TimedApexAutoShoot = Config.Bind("Coin", "TimedApexAutoShoot", false, "Auto shoot at the current coin's apex timing window (for split shots)");
        FirstCoinSlowMo = Config.Bind("Coin", "FirstCoinSlowMo", false, "First tossed of the match coin triggers slow mo");

        ParryContinuous = Config.Bind("Parry", "ParryContinuous", false, "Allows holding down V to continously check for parries, instead of timing");
        ParryRange = Config.Bind("Parry", "ParryRange", 2f, "");
        Punch = Config.Bind("Parry", "Punch", false, "Allow punching (breaks minos parry)");
        PunchDamage = Config.Bind("Parry", "PunchDamage", 100f, "");

        RagdollAddForce = Config.Bind("Physics", "RagdollAddForce", 0.01f, "");

        GroundSlamInfluence = Config.Bind("Movement", "GroundSlamInfluence", 10f, "How high the enemies fly up after a ground slam");
    }
}