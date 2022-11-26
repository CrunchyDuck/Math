using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;
using Verse.Sound;

namespace CrunchyDuck.Math {
	public static class GUIExtensions {
		public const int DetailsButtonWidth = 72;
		public const int SmallElementSize = 24;
		public const int ElementPadding = 4;
		public const int ScrollBarWidth = 16;

		public const int RecipeIconSize = 34;
		public const int BreakLinkWidth = SmallElementSize + ElementPadding + SmallElementSize;

		public static bool RenderBreakLink(BillLinkTracker lt, float x, float y) {
			Rect button_rect = new Rect(x, y, BreakLinkWidth, SmallElementSize);
			bool pressed = false;
			var left = button_rect.LeftPartPixels(SmallElementSize).ContractedBy(2);
			left.x += ElementPadding;
			var right = button_rect.RightPartPixels(SmallElementSize).ContractedBy(2);

			// Button
			int par_id = lt.Parent.linkID;
			if (Widgets.ButtonText(button_rect, "")) {
				pressed = true;
			}

			// Link symbol
			var col = Mouse.IsOver(button_rect) ? Widgets.MouseoverOptionColor : Widgets.NormalOptionColor;
			GUI.DrawTexture(left, Resources.breakLinkImage, ScaleMode.ScaleToFit, true, 1, col, 0, 0);

			// Link ID
			GUI.Label(right, par_id.ToString(), Text.CurFontStyle);

			TooltipHandler.TipRegion(button_rect, "CD.M.tooltips.break_link".Translate());
			return pressed;
		}
	}
}
