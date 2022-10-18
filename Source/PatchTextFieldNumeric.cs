using Verse;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using RimWorld;

namespace CrunchyDuck.Math {
	// TODO: unpause amount is weird, stop it from being overriden when above build amount
	class PatchTextFieldNumeric {
		public static MethodInfo Target() {
			return AccessTools.Method(typeof(Widgets), "TextFieldNumeric", generics: new System.Type[] { typeof(int) });
		}

		public static bool Prefix(Rect rect, ref int val, ref string buffer, float min = 0.0f, float max = 1E+09f) {
			if (PatchDoWindowContents.bill_dialogue != null) {
				BillComponent bc = BillManager.AddGetBillComponent(PatchDoWindowContents.BillRef);
				bool is_repeat = bc.targetBill.repeatMode == BillRepeatModeDefOf.RepeatCount;
				bool is_target = bc.targetBill.repeatMode == BillRepeatModeDefOf.TargetCount;
				bool is_unpause = is_target && PatchDoWindowContents.didTargetCount;

				if (bc.targetBill.repeatMode == BillRepeatModeDefOf.Forever) {
					return false;
				}

				if (buffer == null) {
					if (is_repeat) {
						buffer = bc.repeat_count_last_valid;
					}
					// Rendering target count
					else if (is_target) {
						if (!is_unpause)
							buffer = bc.target_count_last_valid;
						else
							buffer = bc.unpause_last_valid;
					}
				}
				// Rimworld has some fucky logic for the unpause field. This overrides that logic.
				else if (is_unpause) {
					buffer = bc.unpause_buffer;
				}

				string name = "TextField" + rect.y.ToString("F0") + rect.x.ToString("F0");
				GUI.SetNextControlName(name);
				string str = Widgets.TextField(rect, buffer);
				if (buffer != str) {
					buffer = str;
					if (is_unpause) {
						bc.unpause_buffer = buffer;
					}

					// Math
					if (Math.DoMath(buffer, ref val, bc)) {
						if (is_repeat) {
							bc.repeat_count_last_valid = buffer;
						}
						// Rendering target count
						else if (is_target) {
							if (!is_unpause) {
								bc.target_count_last_valid = buffer;
							}
							else {
								bc.unpause_last_valid = buffer;
							}
						}
					}
				}
				if (is_target) {
					PatchDoWindowContents.didTargetCount = true;
				}
				return false;
			}
			return true;
		}
	}

}
