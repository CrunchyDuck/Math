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

		public static bool Prefix(ref int __result, Bill_Production bill) {
			var bc = BillManager.instance.AddGetBillComponent(bill);
			// Use default behaviour.
			if (!bc.customItemsToCount)
				return true;

			Math.DoMath(bc.itemsToCount.lastValid, bc.itemsToCount);
			__result = UnityEngine.Mathf.CeilToInt(bc.itemsToCount.CurrentValue);
			return false;
		}
	}
}
