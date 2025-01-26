using EFT;
using SPT.Reflection.Patching;
using System.Reflection;

namespace ultramove
{
    internal class PatchPreventApplyGravity : ModulePatch
    {
        internal static Player targetPlayer;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(MovementContext).GetMethod(nameof(MovementContext.ApplyGravity));
        }

        [PatchPrefix]
        public static bool PatchPrefix(MovementContext __instance, Player ____player)
        {
            if (targetPlayer == ____player)
                return false;

            return true;
        }
    }
}
