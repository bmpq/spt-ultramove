using AssetBundleLoader;
using Comfort.Common;
using EFT;
using EFT.CameraControl;
using EFT.InventoryLogic;
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
            PlayerAudio.Instance = new PlayerAudio(BundleLoader.LoadAssetBundle(BundleLoader.GetDefaultModAssetBundlePath("ultrakill_audio")));

            GameObject goPlayer = __instance.MainPlayer.gameObject;

            SkinnedMeshRenderer[] rends = goPlayer.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var rend in rends)
            {
                rend.forceRenderingOff = true;
            }

            goPlayer.GetComponent<EftGamePlayerOwner>().enabled = false;
            goPlayer.GetComponent<PlayerCameraController>().enabled = false;
            goPlayer.GetComponent<SimpleCharacterController>().enabled = false;
            goPlayer.GetComponent<CharacterControllerSpawner>().enabled = false;

            var firearm = goPlayer.GetComponent<Player.FirearmController>();
            GameObject weapon = firearm.ControllerGameObject;
            weapon.SetActive(false);
            firearm.enabled = false;
            Player player = goPlayer.GetComponent<Player>();
            player.enabled = false;

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

            foreach (Renderer rend in playerBody.BodySkins[EBodyModelPart.Hands].GetRenderers())
            {
                rend.forceRenderingOff = false;
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            goPlayer.AddComponent<UltraMovement>();
            goPlayer.AddComponent<DoorOpener>();
            goPlayer.AddComponent<HandsController>().SetWeapons(GetEquippedWeapons(player));
            goPlayer.AddComponent<HandsInertia>();
            goPlayer.AddComponent<CoinTosser>().SetPrefab(
                bundleUltrakill.LoadAsset<GameObject>("bitcoin"), 
                bundleUltrakill.LoadAsset<Texture>("item_barter_valuable_bitcoin_D"),
                bundleUltrakill.LoadAsset<GameObject>("glint"));

            Camera.main.gameObject.AddComponent<CameraShaker>();

            GameObject prefabMaurice = bundleUltrakill.LoadAsset<GameObject>("Maurice");
            GameObject newMaurice = GameObject.Instantiate(prefabMaurice, new Vector3(-10, 5, -10), Quaternion.identity);
            newMaurice.AddComponent<Maurice>().SetPrefabProjectile(bundleUltrakill.LoadAsset<GameObject>("Projectile"));
        }

        static List<(GameObject, Weapon)> GetEquippedWeapons(Player player)
        {
            Player.FirearmController controller = player.GetComponent<Player.FirearmController>();

            List<(GameObject, Weapon)> result = new List<(GameObject, Weapon)>();

            List<EquipmentSlot> slotsToGet = new List<EquipmentSlot>
            {
                EquipmentSlot.FirstPrimaryWeapon,
                EquipmentSlot.SecondPrimaryWeapon,
                EquipmentSlot.Holster
            };

            HashSet<Weapon> added = new HashSet<Weapon>();
            foreach (Item item in player.Inventory.GetItemsInSlots(slotsToGet))
            {
                if (added.Contains(item))
                    continue;

                Weapon weapon = item as Weapon;
                if (weapon == null)
                    continue;

                GameObject spawnedWeapon = Singleton<PoolManager>.Instance.CreateItem(item, true);
                result.Add((spawnedWeapon, weapon));
                added.Add(weapon);
            }

            return result;
        }
    }
}
