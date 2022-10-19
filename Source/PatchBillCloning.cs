using HarmonyLib;
using System.Reflection;
using RimWorld;

namespace CrunchyDuck.Math {
	// This patch handles the cloning of bills.
	// TODO: I couldn't get this working elegantly. Making it work for copying breaks adding bills normally.
	class PatchBillCloning {
		public static BillComponent copied_bc;

		public static MethodInfo Target1() {
			return AccessTools.Method(typeof(Bill_Production), "Clone");
		}

		// __result is a BP that is cast down to a bill for the clone, so this should work.
		public static void Postfix1(ref Bill_Production __instance, ref Bill __result) {
			BillComponent bc = BillManager.AddGetBillComponent(__instance);
			Verse.Log.Message(__result.GetType().ToString());
			BillComponent new_bc = BillManager.AddGetBillComponent((Bill_Production)__result);
			new_bc.repeat_count_last_valid = bc.repeat_count_last_valid;
			new_bc.target_count_last_valid = bc.target_count_last_valid;
			new_bc.unpause_last_valid = bc.unpause_last_valid;
		}

		public static MethodInfo Target2() {
			return AccessTools.Method(typeof(Bill), "InitializeAfterClone");
		}

		public static void Postfix2() {
			Verse.Log.Message("here");
		}
	}
}
