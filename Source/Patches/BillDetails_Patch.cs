﻿using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace CrunchyDuck.Math {
	// This patch overrides the bill details popup to be my own.
	// TODO: Can I consolidate this into DoConfigInterface_Patch?
	class BillDetails_Patch {
		private static Bill_Production currentBp;
		private static MethodInfo spawnBill = AccessTools.Method("BillDetails_Patch:SpawnBillDialog");
		private static MethodInfo targetCall = AccessTools.Method("WidgetRow:ButtonText");

		public static MethodInfo Target() {
			return AccessTools.Method(typeof(Bill_Production), "DoConfigInterface");
		}

		public static void Prefix(Bill_Production __instance, UnityEngine.Rect baseRect, UnityEngine.Color baseColor) {
			currentBp = __instance;
		}

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
			var codes = new List<CodeInstruction>(instructions);

			for (var i = 0; i < codes.Count; i++) {
				CodeInstruction code = codes[i];
				if (code.opcode == OpCodes.Callvirt && code.Calls(targetCall)) {
					Label? branch_target;
					codes[i + 1].Branches(out branch_target);
					codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, spawnBill));
					codes.Insert(i + 3, new CodeInstruction(OpCodes.Br, branch_target));
					break;
				}
			}

			return codes.AsEnumerable();
		}

		private static void SpawnBillDialog() {
			var p = ((Thing)currentBp.billStack.billGiver).Position;
			Find.WindowStack.Add(new Dialog_MathBillConfig(currentBp, p));
		}
	}
}
