using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Verse;
using RimWorld;

namespace CrunchyDuck.Math {
	// TODO: Finish this.
	class Dialog_VariableList : Window {
		private Vector2 scrollPosition = Vector2.zero;
		public override Vector2 InitialSize => new Vector2(700f, 700f);

		private const float EntryHeight = 30f;
		protected const float DeleteButSize = EntryHeight - 2;
		private const float HorizontalPadding = 5;
		private int reorderableGroup = -1;

		private BillComponent bc;

		public Dialog_VariableList(BillComponent bill) {
			forcePause = true;
			doCloseX = true;
			doCloseButton = true;
			absorbInputAroundWindow = true;
			closeOnClickedOutside = true;

			bc = bill;
		}

		public override void Close(bool doCloseSound = true) {
			base.Close(doCloseSound);
			MathSettings.settings.Write();
		}

		private static void ReorderVariable(List<UserVariable> uvs, int from, int to) {
			// For some reason, it counts the element that you're trying to move when it determines the new position. So "0 -> 1" becomes "0 -> 2".
			if (to > from)
				to -= 1;
			if (to >= uvs.Count)
				to = uvs.Count - 1;
			if (from == to)
				return;

			uvs.Move(from, to);
		}

		public override void DoWindowContents(Rect inRect) {
			Text.Font = GameFont.Small;
			var uvs = MathSettings.settings.userVariables;
			var uvs_dict = new Dictionary<string, UserVariable>();
			bool uvs_changed = false;

			Vector2 row_size = new Vector2(inRect.width - 16f, EntryHeight);
			float scroll_area_display_height = inRect.height - CloseButSize.y - 50f - 18f;
			Rect scroll_area_display = inRect.TopPartPixels(scroll_area_display_height);
			float scroll_area_total_height = uvs.Count * row_size.y;

			// Add variable button.
			Rect controls_rect = new Rect(scroll_area_display.x, scroll_area_display.yMax + 18f, scroll_area_display.width, 50f);
			if (Widgets.ButtonText(controls_rect, "Add variable")) {
				uvs.Add(new UserVariable());
				uvs_changed = true;
				scroll_area_total_height += row_size.y;
				scrollPosition.y = scroll_area_total_height;
			}

			// Draw scroll area.
			Rect scroll_area = new Rect(0.0f, 0.0f, inRect.width - 16f, scroll_area_total_height);
			Widgets.BeginScrollView(scroll_area_display, ref scrollPosition, scroll_area);
			if (Event.current.type == EventType.Repaint)
				reorderableGroup = ReorderableWidget.NewGroup((from, to) => ReorderVariable(uvs, from, to), ReorderableDirection.Vertical, scroll_area_display);

			for (int i = 0; i < uvs.Count; i++) {
				float y = row_size.y * i;
				var uv = uvs[i];
				Rect row_rect = new Rect(0.0f, y, row_size.x, row_size.y);

				// Register this row as a reorderable segment.
				ReorderableWidget.Reorderable(reorderableGroup, row_rect);

				// Draw alternating backgrounds to help visually distinguish rows.
				if (i % 2 == 0)
					Widgets.DrawAltRect(row_rect);

				Widgets.BeginGroup(row_rect);
				// Delete button
				Rect delete_button_rect = new Rect(row_rect.width - DeleteButSize, (row_rect.height - DeleteButSize) / 2f, DeleteButSize, DeleteButSize);
				if (Widgets.ButtonImage(delete_button_rect, TexButton.DeleteX, Color.white, GenUI.SubtleMouseoverColor)) {
					uvs.RemoveAt(i);
					uvs_changed = true;
					i--;
					continue;
				}

				float left_pos = HorizontalPadding;

				// Drag symbol
				Rect rect2 = new Rect(left_pos, 3, 24f, 24f);
				TooltipHandler.TipRegion(rect2, "DragToReorder".Translate());
				GUI.DrawTexture(rect2, Resources.DragHash);
				left_pos += 24 + HorizontalPadding;

				Color original_col = GUI.color;
				float fields_width = row_rect.width - DeleteButSize - left_pos - HorizontalPadding;

				// Variable name
				float name_width = fields_width * 0.3f;
				var variable_name_rect = new Rect(left_pos, 0, name_width, row_rect.height - 2);
				// These are separate if checks so that the item is still pushed into the dictionary if the name is invalid.
				// This makes sure that behaviour is consistent.
				if(!uv.name.IsParameter())
					GUI.color = new Color(1, 0, 0, 0.8f);
				if (uvs_dict.ContainsKey(uv.name))
					GUI.color = new Color(1, 0, 0, 0.8f);
				else
					uvs_dict[uv.name] = uv;
				uv.name = Widgets.TextField(variable_name_rect, uv.name);
				GUI.color = original_col;
				left_pos += name_width + HorizontalPadding;

				// Equals sign.
				var ta = Text.Anchor;
				Text.Font = GameFont.Medium;
				Text.Anchor = TextAnchor.MiddleCenter;
				Widgets.Label(new Rect(left_pos, 0, DeleteButSize, DeleteButSize), "=");
				
				left_pos += DeleteButSize + HorizontalPadding;
				Text.Font = GameFont.Small;
				Text.Anchor = ta;

				// Equation
				// Check if the provided equation is valid.
				float math_result = 0;
				if (!Math.DoMath(uv.equation, bc, ref math_result))
					GUI.color = new Color(1, 0, 0, 0.8f);
				float equation_width = fields_width * 0.7f - DeleteButSize - HorizontalPadding;
				var variable_equation_rect = new Rect(left_pos, 0, equation_width, row_rect.height - 2);
				uv.equation = Widgets.TextField(variable_equation_rect, uv.equation);
				GUI.color = original_col;

				Widgets.EndGroup();
			}

			Widgets.EndScrollView();
			MathSettings.settings.userVariablesDict = uvs_dict;
		}
	}
}
