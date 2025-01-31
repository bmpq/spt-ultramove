using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;

namespace ultramove
{
    internal class PatchRagdollAmplify : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(RagdollClass).GetMethod(nameof(RagdollClass.Start));
        }

        [PatchPostfix]
        public static void PatchPostfix(RagdollClass __instance, RigidbodySpawner[] ___rigidbodySpawner_0)
        {
            foreach (var rbspawner in ___rigidbodySpawner_0)
            {
                rbspawner.GetComponent<Rigidbody>().velocity *= 4f;
            }
        }
    }
}
