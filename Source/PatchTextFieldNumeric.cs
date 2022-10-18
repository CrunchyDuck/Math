using Verse;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace CrunchyDuck.Math {
	class PatchTextFieldNumeric {
		public static MethodInfo Target() {
			return AccessTools.Method(typeof(Widgets), "TextFieldNumeric", generics: new System.Type[] { typeof(int) });
		}

		public static bool Prefix(Rect rect, ref int val, ref string buffer, float min = 0.0f, float max = 1E+09f) {
			if (PatchDoWindowContents.bill_dialogue != null) {
				BillComponent bc = BillManager.AddGetBillComponent(PatchDoWindowContents.BillRef);

				if (buffer == null) {
					buffer = bc.last_valid_input;
				}

				string name = "TextField" + rect.y.ToString("F0") + rect.x.ToString("F0");
				GUI.SetNextControlName(name);
				string str = Widgets.TextField(rect, buffer);
				if (buffer != str) {
					bc.field_text = str;
					buffer = str;

					// Math
					if (Math.DoMath(buffer, ref val, bc))
						bc.last_valid_input = buffer;
				}
				return false;
			}
			return true;
		}
	}

}
