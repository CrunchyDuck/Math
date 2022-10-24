using HarmonyLib;
using System.Reflection;
using RimWorld;
using Verse;
using System;

namespace CrunchyDuck.Math {
	// Patches the constructor, so we know when a new bill was made.
	class Bill_ProductionConstructor_Patch {
		public static ConstructorInfo Target() {
			return AccessTools.Constructor(typeof(Bill_Production), new Type[] { typeof(RecipeDef), typeof(Precept_ThingStyle) });
		}

		// Saves/loads data.
		public static void Postfix(Bill_Production __instance, RecipeDef recipe, Precept_ThingStyle precept = null) {
			BillManager.AddGetBillComponent(__instance);
		}
	}
}
