using RimWorld;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace CrunchyDuck.Math {
	/// <summary>
	/// This patch is used to check when PatchNumbericTextField is being invoked, and to pass a reference to the calling Dialogue_BillConfig.
	/// </summary>
	// TODO: Make field red if invalid.
	class PatchDoWindowContents {
		public static Dialog_BillConfig bill_dialogue = null;
		public static Bill_Production BillRef {
			get {
				if (bill_dialogue == null)
					return null;
				return (Bill_Production)AccessTools.Field(typeof(Dialog_BillConfig), "bill").GetValue(bill_dialogue);
			}
		}
		public static int BillID {
			get {
				var b = BillRef;
				if (b == null)
					return -1;
				return (int)AccessTools.Field(typeof(Bill), "loadID").GetValue(b);
			}
		}

		public static MethodInfo Target() {
			return AccessTools.Method(typeof(Dialog_BillConfig), "DoWindowContents");
		}

		// Started render
		public static void Prefix(Dialog_BillConfig __instance, Rect inRect) {
			bill_dialogue = __instance;
		}

		// Finished render
		public static void Postfix(Rect inRect) {
			bill_dialogue = null;
		}
	}
}
