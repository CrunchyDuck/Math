using RimWorld;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Verse;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using Verse.Sound;

// This file handles making the bill menu work nicer.
namespace CrunchyDuck.Math {
	static class BillMenuData {
		public static BillComponent bc = null;
		public static Dialog_BillConfig billDialog = null;
		public static Bill_Production billProduction = null;
		public static bool didTargetCountThisFrame = false;
		public static Rect rect;
	
		public static bool RenderingTarget { get { return bc.isDoUntilX && !didTargetCountThisFrame; } }
		public static bool RenderingUnpause { get { return bc.isDoUntilX && didTargetCountThisFrame; } }
		public static bool RenderingRepeat { get { return bc.isDoXTimes; } }

		public static void AssignTo(BillComponent bc) {
			didTargetCountThisFrame = false;
			BillMenuData.bc = bc;
		}

		public static void AssignTo(Dialog_BillConfig bill_dialogue, Rect rect) {
			didTargetCountThisFrame = false;
			billDialog = bill_dialogue;
			BillMenuData.rect = rect;
			billProduction = (Bill_Production)AccessTools.Field(typeof(Dialog_BillConfig), "bill").GetValue(bill_dialogue);
			bc = BillManager.AddGetBillComponent(billProduction);
		}

		public static void Unassign() {
			billDialog = null;
			billProduction = null;
			bc = null;
		}

		public static void AssignCurrentlyRenderingField(int value) {
			if (RenderingRepeat) {
				bc.doXTimes.SetAll(value);
			}
			else if (RenderingTarget) {
				bc.doUntilX.SetAll(value);
			}
			else {
				bc.unpause.SetAll(value);
			}
		}

		public static InputField GetCurrentlyRenderingField() {
			if (RenderingRepeat) {
				return bc.doXTimes;
			}
			else if (RenderingTarget) {
				return bc.doUntilX;
			}
			else {
				return bc.unpause;
			}
		}
	}

	// This patch is used to check when NumericTextField is being invoked, and to pass a reference to the calling Dialogue_BillConfig.
	class Dialog_BillConfig_Patch {
		public static MethodInfo Target() {
			return AccessTools.Method(typeof(Dialog_BillConfig), "DoWindowContents");
		}

		// This transpiler expands the size of the panel.
#if v1_4
		// oh jeez oh gosh how do i use this
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
			var codes = new List<CodeInstruction>(instructions);
			int num_codes_found = 0;
			// This needs to be done because when scaling up the width of the element in Prefix2, that width is evenly distributed.
			float panel_allocation = Settings.textInputAreaBonus / 3;

			for (var i = 0; i < codes.Count; i++) {
				// Increase size of panel
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
					num_codes_found++;
				}
			}

			return codes.AsEnumerable();
		}
#elif v1_3
		// oh jeez oh gosh how do i use this
		public static IEnumerable<CodeInstruction> Transpiler1(IEnumerable<CodeInstruction> instructions) {
			var codes = new List<CodeInstruction>(instructions);
			int num_codes_found = 0;
			// This needs to be done because when scaling up the width of the element in Prefix2, that width is evenly distributed.
			// TODO: This scales weirdly because it's only updated at the start. Make it a call.
			float panel_allocation = Settings.textInputAreaBonus / 3;

			for (var i = 0; i < codes.Count; i++) {
				// Increase size of panel
				if (codes[i].opcode == OpCodes.Ldloc_1) {
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
					num_codes_found++;
				}
			}

			return codes.AsEnumerable();
		}
#endif

		// Started render
		public static void Prefix(Dialog_BillConfig __instance, Rect inRect) {
			BillMenuData.AssignTo(__instance, inRect);
			// Clear the cache and regenerate it.
			// Since the game is paused, it won't cause lag.
			if (RealTime.frameCount % 60 == 0) {
				Math.ClearCacheMaps();
				InputField f = BillMenuData.GetCurrentlyRenderingField();
				int val = 0;
				Math.DoMath(f.lastValid, ref val, BillMenuData.bc);
				f.CurrentValue = val;
			}
		}

		// Finished render
		public static void Postfix(Rect inRect) {
			// This overrides the logic for unpausing recipes. It breaks how my stuff works, and it's dumb anyway.
			// TODO: This might be able to be removed?
			BillComponent bc = BillMenuData.bc;
			Math.DoMath(bc.unpause.lastValid, ref bc.targetBill.unpauseWhenYouHave, bc);
			AccessTools.Field(typeof(Dialog_BillConfig), "unpauseCountEditBuffer").SetValue(BillMenuData.billDialog, bc.unpause.buffer);

			// Render help button.
			// Touch lazy to copy this from the method, but oh well.
			float width = (int)((inRect.width - 34.0) / 3.0);
			Rect rect1 = new Rect(0.0f, 80f, width, inRect.height - 80f);
			Rect rect2 = new Rect(rect1.xMax + 17f, 50f, width, inRect.height - 50f - Window.CloseButSize.y);
			Rect rect3 = new Rect(rect2.xMax + 17f, 50f, 0.0f, inRect.height - 50f - Window.CloseButSize.y);
			Rect rect = new Rect(rect1.x + 24 + 4, rect3.y, 24, 24);
			
			if (Widgets.ButtonImage(rect, Math.infoButtonImage, GUI.color)) {
				Find.WindowStack.Add(new Dialog_MathInfoCard(bc));
			}
			BillMenuData.Unassign();
		}
	}

	// This scales the container for the bill properly.
	class SetInitialSizeAndPosition_Patch {
		public static MethodInfo Target() {
			return AccessTools.Method(typeof(Window), "SetInitialSizeAndPosition");
		}

		public static bool Prefix(Window __instance) {
			if (__instance.GetType() == typeof(Dialog_BillConfig)) {
				Vector2 initialSize = __instance.InitialSize;
				initialSize.x += Settings.textInputAreaBonus;
				__instance.windowRect = new Rect((float)(((double)UI.screenWidth - (double)initialSize.x) / 2.0), (float)(((double)UI.screenHeight - (double)initialSize.y) / 2.0), initialSize.x, initialSize.y);
				__instance.windowRect = __instance.windowRect.Rounded();
				return false;
			}
			return true;
		}
	}

	// I have to patch this in order to get the +/- buttons within the submenu working nicely.
	class IntEntry_Patch {
		public static MethodInfo Target() {
			return AccessTools.Method(typeof(Widgets), "IntEntry");
		}

		// I could make this cleaner if if I used some transpiler magic, but doing it this way saves me hours of work
		public static bool Prefix(Rect rect, ref int value, ref string editBuffer, int multiplier = 1) {
			int IntEntryButtonWidth = 40;  // Yoinked from decomp.

			int width = Mathf.Min(IntEntryButtonWidth, (int)rect.width / 5);
			if (Widgets.ButtonText(new Rect(rect.xMin, rect.yMin, width, rect.height), (-10 * multiplier).ToStringCached())) {
				value -= 10 * multiplier * GenUI.CurrentAdjustmentMultiplier();
				editBuffer = value.ToStringCached();
				BillMenuData.AssignCurrentlyRenderingField(value);
				SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
			}
			if (Widgets.ButtonText(new Rect(rect.xMin + width, rect.yMin, width, rect.height), (-1 * multiplier).ToStringCached())) {
				value -= multiplier * GenUI.CurrentAdjustmentMultiplier();
				editBuffer = value.ToStringCached();
				BillMenuData.AssignCurrentlyRenderingField(value);
				SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
			}
			if (Widgets.ButtonText(new Rect(rect.xMax - width, rect.yMin, width, rect.height), "+" + (10 * multiplier).ToStringCached())) {
				value += 10 * multiplier * GenUI.CurrentAdjustmentMultiplier();
				editBuffer = value.ToStringCached();
				BillMenuData.AssignCurrentlyRenderingField(value);
				SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
			}
			if (Widgets.ButtonText(new Rect(rect.xMax - width * 2, rect.yMin, width, rect.height), "+" + multiplier.ToStringCached())) {
				value += multiplier * GenUI.CurrentAdjustmentMultiplier();
				editBuffer = value.ToStringCached();
				BillMenuData.AssignCurrentlyRenderingField(value);
				SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
			}
			Widgets.TextFieldNumeric<int>(new Rect(rect.xMin + width * 2, rect.yMin, rect.width - width * 4, rect.height), ref value, ref editBuffer);

			return false;
		}
	}

	// This handles input into the text field.
	// TODO: For some reason you can't put in no string for this. I suspect this is due to a NullOrEmpty check somewhere.
	class TextFieldNumeric_Patch {
		public static MethodInfo Target() {
			return AccessTools.Method(typeof(Widgets), "TextFieldNumeric", generics: new System.Type[] { typeof(int) });
		}

		//public static IEnumerable<CodeInstruction> Transpiler1(IEnumerable<CodeInstruction> instructions) {
		//	var codes = new List<CodeInstruction>(instructions);
		//	int num_codes_found = 0;
		//	// This needs to be done because when scaling up the width of the element in Prefix2, that width is evenly distributed.
		//	float panel_allocation = Settings.textInputAreaBonus / 3;

		//	for (var i = 0; i < codes.Count; i++) {
		//		// Increase size of panel
		//		if (codes[i].opcode == OpCodes.Ldloc_0) {
		//			switch (num_codes_found) {
		//				// Shrink the panel to the left
		//				case 0:
		//					codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldc_R4, panel_allocation));
		//					codes.Insert(i + 2, new CodeInstruction(OpCodes.Sub));
		//					break;
		//				// Expand the panel to the right.
		//				case 1:
		//					codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldc_R4, panel_allocation * 2));
		//					codes.Insert(i + 2, new CodeInstruction(OpCodes.Add));
		//					break;
		//			}
		//			num_codes_found++;
		//		}
		//	}

		//	return codes.AsEnumerable();
		//}

		public static bool Prefix(Rect rect, ref int val, ref string buffer, float min = 0.0f, float max = 1E+09f) {
			if (BillMenuData.billDialog != null) {
				BillComponent bc = BillMenuData.bc;

				if (BillMenuData.RenderingRepeat)
					return PrefixExtended(rect, ref val, ref buffer, bc, bc.doXTimes);
				else if (BillMenuData.RenderingTarget) {
					PrefixExtended(rect, ref val, ref buffer, bc, bc.doUntilX);
					BillMenuData.didTargetCountThisFrame = true;
					return false;
				}
				else if (BillMenuData.RenderingUnpause)
					return PrefixExtended(rect, ref val, ref buffer, bc, bc.unpause);
			}
			return true;
		}

		/// This is its own function so that I can use ref string field to clean up my code. C# has stupid rules about local refs.
		public static bool PrefixExtended(Rect rect, ref int val, ref string display_buffer, BillComponent bc, InputField field) {
			// I don't think this happens.
			if (bc.targetBill.repeatMode == BillRepeatModeDefOf.Forever)
				return false;

			// Fill buffer for first time.
			if (field.buffer.NullOrEmpty())
				field.buffer = field.lastValid;

			// Check if equation last was valid, tint red if not.
			// We can't tint this on the same frame because the act of rendering the text field is also the act of polling.
			// Therefore we can't poll and check the new value, then retroactively change the colour.
			var str = RenderTextField(rect, field.buffer, bc);
			if (field.buffer != str) {
				field.buffer = str;
				if (BillMenuData.RenderingUnpause)
					bc.unpause.buffer = field.buffer;

				// Try to evaluate equation
				if (Math.DoMath(field.buffer, ref val, bc)) {
					// Update last valid.
					field.lastValid = field.buffer;
				}
			}
			display_buffer = field.buffer;
			return false;
		}

		public static string RenderTextField(Rect rect, string buffer, BillComponent bc) {
			string name = "TextField" + rect.y.ToString("F0") + rect.x.ToString("F0");
			GUI.SetNextControlName(name);
			var base_color = GUI.color;
			int test_val = 0;
			if (!Math.DoMath(buffer, ref test_val, bc))
				GUI.color = new Color(1, 0, 0, 0.8f);
			// TODO: Some kind of tooltip here.
			if (Math.usedOldVariableNames) {
				GUI.color = new Color(0.8f, 0.2f, 0.8f, 0.8f);
			}

			string str = Widgets.TextField(rect, buffer);
			GUI.color = base_color;
			return str;
		}
	}

	class DoConfigInterface_Patch {
		public static MethodInfo Target() {
			return AccessTools.Method(typeof(Bill_Production), "DoConfigInterface");
		}

#if v1_4
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
			var codes = new List<CodeInstruction>(instructions);
			MethodInfo button_icon_method = AccessTools.Method(typeof(WidgetRow), "ButtonIcon");
			int call_count = 0;

			for (var i = 0; i < codes.Count; i++) {
				CodeInstruction code = codes[i];
				if (code.Calls(button_icon_method)) {
					MethodInfo m;
					Label? branch_target = null;
					switch (call_count) {
						case 0:
							codes[i + 1].Branches(out branch_target);
							m = AccessTools.Method(typeof(DoConfigInterface_Patch), "Increment");
							codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, m));
							codes.Insert(i + 3, new CodeInstruction(OpCodes.Br, branch_target));
							break;
						case 1:
							codes[i + 1].Branches(out branch_target);
							m = AccessTools.Method(typeof(DoConfigInterface_Patch), "Decrement");
							codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, m));
							codes.Insert(i + 3, new CodeInstruction(OpCodes.Br, branch_target));
							break;
					}
					call_count++;
					if (call_count > 1)
						break;
				}
			}
			return codes.AsEnumerable();
		}
#endif

		public static void Prefix(Bill_Production __instance, Rect baseRect, Color baseColor) {
			var bc = BillManager.AddGetBillComponent(__instance);
			BillMenuData.AssignTo(bc);
		}

		public static void Postfix() {
			BillMenuData.Unassign();
		}

		// lazy
		public static void Decrement() {
			DoEq(false);
		}

		public static void Increment() {
			DoEq(true);
		}

		public static void DoEq(bool increment) {
			var bc = BillMenuData.bc;
			InputField f = BillMenuData.GetCurrentlyRenderingField();
			int number = 0;
			Math.DoMath(f.lastValid, ref number, bc);
			var val = bc.targetBill.recipe.targetCountAdjustment * GenUI.CurrentAdjustmentMultiplier();
			if (increment)
				number += val;
			else
				number -= val;
			BillMenuData.AssignCurrentlyRenderingField(number);
			SoundDefOf.DragSlider.PlayOneShotOnCamera();
		}
	}
}