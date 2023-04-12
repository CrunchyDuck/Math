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
			}
		}

		private static void ReorderBillInStack(BillStack stack, int from, int to) {
			// For some reason, it counts the element that you're trying to move when it determines the new position. So "0 -> 1" becomes "0 -> 2".
			if (to > from)
				to -= 1;
			if (to >= stack.Count)
				to = stack.Count - 1;
			if (from == to)
				return;

			Bill bill = stack[from];
			int offset = to - from;
			stack.Reorder(bill, offset);
		}
	}
}
