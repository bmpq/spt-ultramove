using EFT;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;

namespace ultramove
{
    internal class PatchJump : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(JumpState).GetMethod(nameof(JumpState.Enter));
        }

        [PatchPostfix]
        public static void PatchPostfix(JumpState __instance, MovementContext ___MovementContext, ref float ___float_3, ref Vector3 ___vector3_0, ref Vector3 ___vector3_1)
        {
            ___vector3_0 = Vector3.zero;
            ___vector3_1 = EFTHardSettings.Instance.LIFT_VELOCITY_BY_SPEED.Evaluate(1) * Vector3.up;
            ___float_3 = EFTHardSettings.Instance.JUMP_DELAY_BY_SPEED.Evaluate(1);
        }
    }
}
