using RimWorld;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Verse;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;

namespace CrunchyDuck.Math {
	class Patch_BillStack_DoListing {
		public static MethodInfo Target() {
			return AccessTools.Method(typeof(BillStack), "DoListing");
		}

		public static void Prefix(BillStack __instance) {
			if (Event.current.type == EventType.Repaint) {
				Patch_Bill_DoInterface.reorderGroup = ReorderableWidget.NewGroup((from, to) => ReorderBillInStack(__instance, from, to), ReorderableDirection.Vertical, new Rect(0.0f, 0.0f, UI.screenWidth, UI.screenHeight));
				Log.Message(Patch_Bill_DoInterface.reorderGroup.ToString());
			}
		}

		private static void ReorderBillInStack(BillStack stack, int from, int to) {
			if (to >= stack.Count)
				to = stack.Count - 1;
			if (from == to)
				return;
			Bill bill = stack[from];
			int offset = to - from;
			// I'm not sure why, but when moving bills down, they would always move 1 more than they should.
			if (offset > 0)
				offset -= 1;
			stack.Reorder(bill, offset);
		}
	}
}
