using AssetBundleLoader;
using EFT;
using EFT.CameraControl;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ultramove
{
    internal class OnGameStarted : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
        }

        [PatchPostfix]
        public static void PatchPostfix(GameWorld __instance)
        {
            Physics.autoSimulation = true;

            EFTBallisticsInterface.Instance = new EFTBallisticsInterface(__instance);

            GameObject goPlayer = __instance.MainPlayer.gameObject;

            goPlayer.GetComponent<EftGamePlayerOwner>().enabled = false;
            goPlayer.GetComponent<PlayerCameraController>().enabled = false;
            goPlayer.GetComponent<SimpleCharacterController>().enabled = false;
            goPlayer.GetComponent<CharacterControllerSpawner>().enabled = false;

            var firearm = goPlayer.GetComponent<Player.FirearmController>();
            GameObject weapon = firearm.ControllerGameObject;
            firearm.enabled = false;
            goPlayer.GetComponent<Player>().enabled = false;

            AssetBundle bundleUltrakill = BundleLoader.LoadAssetBundle(BundleLoader.GetDefaultModAssetBundlePath("ultrakill"));
            goPlayer.GetComponentInChildren<Animator>().runtimeAnimatorController = bundleUltrakill.LoadAsset<RuntimeAnimatorController>("UltraFPS");

            PlayerBody playerBody = goPlayer.GetComponentInChildren<PlayerBody>();
            Collider[] cols = playerBody.GetComponentsInChildren<Collider>();
            foreach (Collider col in cols)
            {
                if (col is SphereCollider sphere)
                {
                    sphere.radius *= 0.3f;
                }
                else if (col is CapsuleCollider capsule)
                {
                    capsule.radius *= 0.3f;
                    capsule.height *= 0.3f;
                }
                else if (col is BoxCollider box)
                {
                    box.size *= 0.3f;
                }
            }

            foreach (Renderer rend in playerBody.BodySkins[EBodyModelPart.Feet].GetRenderers())
            {
                rend.forceRenderingOff = true;
            }

            foreach (Renderer rend in playerBody.BodySkins[EBodyModelPart.Body].GetRenderers())
            {
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            goPlayer.AddComponent<UltraMovement>();
            goPlayer.AddComponent<DoorOpener>();
            goPlayer.AddComponent<GunController>();
            goPlayer.AddComponent<HandsController>().SetWeapon(weapon);
            goPlayer.AddComponent<HandsInertia>();
            goPlayer.AddComponent<CoinTosser>().SetPrefab(
                bundleUltrakill.LoadAsset<GameObject>("bitcoin"), 
                bundleUltrakill.LoadAsset<Texture>("item_barter_valuable_bitcoin_D"),
                bundleUltrakill.LoadAsset<GameObject>("glint"));
        }
    }
}
