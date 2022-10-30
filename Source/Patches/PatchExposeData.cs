using HarmonyLib;
using System.Reflection;
using RimWorld;
using Verse;

namespace CrunchyDuck.Math {
	class PatchExposeData {
		// Patches
		public static MethodInfo Target() {
			return AccessTools.Method(typeof(Bill_Production), "ExposeData");
		}

		// Saves/loads data.
		public static void Postfix(Bill_Production __instance) {
			BillComponent b = BillManager.instance.AddGetBillComponent(__instance);
			b.ExposeData();
		}
	}
}
