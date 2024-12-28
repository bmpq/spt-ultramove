using EFT;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ultramove
{
    internal class OnGrenadeSetThrowForce : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Grenade).GetMethod(nameof(Grenade.SetThrowForce));
        }

        [PatchPostfix]
        public static void PatchPostfix(Grenade __instance)
        {
            __instance.gameObject.GetOrAddComponent<Projectile>();
        }
    }
}
