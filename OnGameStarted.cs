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

            GameObject goPlayer = __instance.MainPlayer.gameObject;

            goPlayer.GetComponent<EftGamePlayerOwner>().enabled = false;
            goPlayer.GetComponent<PlayerCameraController>().enabled = false;
            goPlayer.GetComponent<SimpleCharacterController>().enabled = false;
            goPlayer.GetComponent<CharacterControllerSpawner>().enabled = false;
            goPlayer.GetComponent<Player.FirearmController>().enabled = false;
            goPlayer.GetComponent<Player>().enabled = false;

            goPlayer.GetComponentInChildren<Animator>().enabled = false;

            //GameObject.Destroy(goPlayer.GetComponentInChildren<PlayerBody>().gameObject);

            goPlayer.AddComponent<UltraMovement>();
        }
    }
}
