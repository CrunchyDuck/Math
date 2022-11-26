using RimWorld;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Verse;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;

namespace CrunchyDuck.Math {
	public class Patch_Bill_DoInterface {
		public static int reorderGroup = -1;
		private static readonly MethodInfo CanUnpauseBill = AccessTools.Method(typeof(Bill_Production), "CanUnpause");


		public static MethodInfo Target() {
			return AccessTools.Method(typeof(Bill), "DoInterface");
		}

		// Skip rendering ^/v
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
			var codes = new List<CodeInstruction>(instructions);
			MethodInfo target_method = AccessTools.Method(typeof(BillStack), "IndexOf");
			int found = 0;
			int insert_index = 0;

			for (var i = 0; i < codes.Count; i++) {
				CodeInstruction code = codes[i];
				if (code.Calls(target_method)) {
					if (found == 0) {
						insert_index = i - 3;
						found++;
						continue;
					}
					else {
						codes[i + 6].Branches(out Label? branch_target);
						codes.Insert(insert_index, new CodeInstruction(OpCodes.Br, branch_target));
						break;
					}
				}
			}
			return codes.AsEnumerable();
		}

		// Add dragging to reorder.
		public static void Postfix(ref Bill __instance, float x, float y, float width) {
			Rect rect1 = new Rect(x, y, width, 53f);
			if (__instance is Bill_Production bp) {
				if (bp.paused) {
					if ((bool)CanUnpauseBill.Invoke(bp, new object[0]))
						rect1.height += GUIExtensions.SmallElementSize;
					else
						rect1.height += 17;
				}
			}

			ReorderableWidget.Reorderable(reorderGroup, rect1);
			Rect rect2 = new Rect(x, y + 12f, 24f, 24f);
			TooltipHandler.TipRegion(rect2, "DragToReorder".Translate());
			GUI.DrawTexture(rect2, Resources.DragHash);
		}
	}
}
