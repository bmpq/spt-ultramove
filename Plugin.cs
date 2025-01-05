using BepInEx;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using ultramove;
using UnityEngine;

[BepInPlugin("com.tarkin.ultramove", "ultramove", "1.0.0.0")]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Log;

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
    }
}