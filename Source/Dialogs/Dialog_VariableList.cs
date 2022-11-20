using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Verse;
using RimWorld;

namespace CrunchyDuck.Math {
	class Dialog_VariableList : Window {
		private Vector2 scrollPosition = Vector2.zero;
		public override Vector2 InitialSize => new Vector2(900f, 700f);

		private const float EntryHeight = 30f;
		protected const float DeleteButSize = EntryHeight - 2;
		private int reorderableGroup = -1;

		public Dialog_VariableList(BillComponent bill) {
			forcePause = true;
			doCloseX = true;
			doCloseButton = true;
			//absorbInputAroundWindow = true;
			//closeOnClickedOutside = true;
		}

		public override void Close(bool doCloseSound = true) {
			base.Close(doCloseSound);
			MathSettings.settings.Write();
		}

		private static void ReorderVariable(List<UserVariable> uvs, int from, int to) {
			if (to >= uvs.Count)
				to = uvs.Count - 1;
			if (from == to)
				return;

			UserVariable uv = uvs[from];
			uvs.RemoveAt(from);
			uvs.Insert(to, uv);
		}

		public override void DoWindowContents(Rect inRect) {
			var uvs = MathSettings.settings.userVariables;


			Vector2 vector2 = new Vector2(inRect.width - 16f, EntryHeight);
			float height1 = uvs.Count * vector2.y;
			Rect view_rect = new Rect(0.0f, 0.0f, inRect.width - 16f, height1);
			float height2 = inRect.height - CloseButSize.y - 50f - 18f;
			Rect out_rect = inRect.TopPartPixels(height2);

			// Draw variables.
			Widgets.BeginScrollView(out_rect, ref scrollPosition, view_rect);
			if (Event.current.type == EventType.Repaint)
				reorderableGroup = ReorderableWidget.NewGroup((from, to) => ReorderVariable(uvs, from, to), ReorderableDirection.Vertical, out_rect);

			for (int i = 0; i < uvs.Count; i++) {
				float y = vector2.y * i;
				var uv = uvs[i];
				Rect row_rect = new Rect(0.0f, y, vector2.x, vector2.y);
				ReorderableWidget.Reorderable(reorderableGroup, row_rect);

				// Draw alternating backgrounds to help visually distinguish rows.
				if (i % 2 == 0)
					Widgets.DrawAltRect(row_rect);

				Widgets.BeginGroup(row_rect);
				float left_pos = 5;

				// Variable name
				var variable_name_rect = new Rect(left_pos, 0, 150, row_rect.height - 2);
				uv.name = Widgets.TextField(variable_name_rect, uv.name);
				left_pos += 150 + 5;

				// Equation
				var variable_equation_rect = new Rect(left_pos, 0, 350, row_rect.height - 2);
				uv.equation = Widgets.TextField(variable_equation_rect, uv.equation);

				// Delete button
				Rect delete_button_rect = new Rect(row_rect.width - DeleteButSize, (row_rect.height - DeleteButSize) / 2f, DeleteButSize, DeleteButSize);
				if (Widgets.ButtonImage(delete_button_rect, TexButton.DeleteX, Color.white, GenUI.SubtleMouseoverColor)) {

				}

				Widgets.EndGroup();
			}

			Widgets.EndScrollView();
		}
	}
}
