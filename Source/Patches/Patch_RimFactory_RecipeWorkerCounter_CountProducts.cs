using System.Reflection;
using HarmonyLib;
using ProjectRimFactory.Common.HarmonyPatches;
using RimWorld;
using Verse;

namespace CrunchyDuck.Math
{
    public class Patch_RimFactory_RecipeWorkerCounter_CountProducts {
        public static MethodInfo Target() {
            return AccessTools.Method(typeof(Patch_RecipeWorkerCounter_CountProducts), nameof(Patch_RecipeWorkerCounter_CountProducts.Postfix));
        }

        public static bool Prefix(Bill_Production bill) {
            var bc = BillManager.instance.AddGetBillComponent(bill);
            // Skip this if custom item counting is in use.
            return !bc.customItemsToCount;
        }
    }
}