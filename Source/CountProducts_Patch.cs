using HarmonyLib;
using System.Reflection;
using Verse;
using RimWorld;

namespace CrunchyDuck.Math {
	// This lets us have custom counting logic.
	class CountProducts_Patch {
		public static MethodInfo Target() {
			return AccessTools.Method(typeof(RecipeWorkerCounter), "CountProducts");
		}

		// TODO: How regularly is this called? I might want to make sure it only runs on a regular rate rather than continuously.
		public static bool Prefix(ref int __result, Bill_Production bill) {
			var bc = BillManager.AddGetBillComponent(bill);
			// Use default behaviour.
			if (!bc.customItemsToCount)
				return true;

			// TODO: Last valid result.
			Math.DoMath(bc.itemsToCount.lastValid, ref __result, bc.itemsToCount);
			return false;
		}
	}
}
