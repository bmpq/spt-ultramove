using BepInEx;
using BepInEx.Logging;
using EFT;
using ultramove;

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
    }

    private void InitConfiguration()
    {
    }
}