using RimWorld;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Verse;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;

namespace CrunchyDuck.Math {
	/// <summary>
	/// This patch is used to check when PatchNumbericTextField is being invoked, and to pass a reference to the calling Dialogue_BillConfig.
	/// </summary>
	// TODO: Make field red if invalid.
	class Dialog_BillConfig_Patch {
		public static Dialog_BillConfig bill_dialogue = null;
		public static bool didTargetCount = false;
		public static Rect r;
		public static float textInputAreaBonus = 100f;

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


		public static MethodInfo Target1() {
			return AccessTools.Method(typeof(Dialog_BillConfig), "DoWindowContents");
		}

		// Started render
		public static bool Prefix1(Dialog_BillConfig __instance, Rect inRect) {
			didTargetCount = false;
			bill_dialogue = __instance;
			r = inRect;

            //ReimplementMethod(__instance, inRect);
            return true;
		}

		// oh jeez oh gosh how do i use this
		public static IEnumerable<CodeInstruction> Transpiler1(IEnumerable<CodeInstruction> instructions) {
			var codes = new List<CodeInstruction>(instructions);
			int codes_to_find = 1;
			int num_codes_found = 0;
			// This needs to be done because when scaling up the width of the element in Prefix2, that width is evenly distributed.
			float panel_allocation = textInputAreaBonus / 3;

			for (var i = 0; i < codes.Count; i++) {
				if (codes[i].opcode == OpCodes.Ldloc_0) {
					switch (num_codes_found) {
						// Shrink the panel to the left
						case 0:
							codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldc_R4, panel_allocation));
							codes.Insert(i + 2, new CodeInstruction(OpCodes.Sub));
							break;
						// Expand the panel to the right.
						case 1:
							codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldc_R4, panel_allocation * 2));
							codes.Insert(i + 2, new CodeInstruction(OpCodes.Add));
							break;
					}

					if (num_codes_found == codes_to_find)
						break;
					num_codes_found++;
				}
			}

			return codes.AsEnumerable();
		}

		// Finished render
		public static void Postfix1(Rect inRect) {
			// This overrides the logic for unpausing recipes. It breaks how my stuff works, and it's dumb anyway.
			BillComponent bc = BillManager.AddGetBillComponent(BillRef);
			Math.DoMath(bc.unpause_last_valid, ref bc.targetBill.unpauseWhenYouHave, bc);
			AccessTools.Field(typeof(Dialog_BillConfig), "unpauseCountEditBuffer").SetValue(bill_dialogue, bc.unpause_buffer);
			bill_dialogue = null;
		}


		public static MethodInfo Target2() {
			return AccessTools.Method(typeof(Window), "SetInitialSizeAndPosition");
		}

		public static bool Prefix2(Window __instance) {
			if (__instance.GetType() == typeof(Dialog_BillConfig)) {
				Vector2 initialSize = __instance.InitialSize;
				initialSize.x += textInputAreaBonus;
				__instance.windowRect = new Rect((float)(((double)UI.screenWidth - (double)initialSize.x) / 2.0), (float)(((double)UI.screenHeight - (double)initialSize.y) / 2.0), initialSize.x, initialSize.y);
				__instance.windowRect = __instance.windowRect.Rounded();
				return false;
			}
			return true;
		}
	}
}
