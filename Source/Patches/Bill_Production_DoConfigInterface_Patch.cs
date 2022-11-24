using HarmonyLib;
using System.Reflection;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.Sound;

namespace CrunchyDuck.Math {
	class Bill_Production_DoConfigInterface_Patch {
        public static MethodInfo Target() {
			return AccessTools.Method(typeof(Bill_Production), "DoConfigInterface");
		}

		public static void Postfix(Bill_Production __instance, Rect baseRect, Color baseColor) {
            BillComponent bc = BillManager.instance.AddGetBillComponent(__instance);
            BillLinkTracker blt = bc.linkTracker;
			BillLinkTracker curr_copied = BillLinkTracker.currentlyCopied;

            var storeModeImage = Resources.bestStockpileImage;
            var nextStoreMode = BillStoreModeDefOf.DropOnFloor;
            //tip = "IW.ClickToTakeToStockpileTip".Translate();
            var tip = "Currently taking output to stockpile. Click to drop on floor.";
            if (__instance.GetStoreMode() == BillStoreModeDefOf.DropOnFloor) {
                storeModeImage = Resources.dropOnFloorImage;
                nextStoreMode = BillStoreModeDefOf.BestStockpile;
                // TODO: Implement translations.
                //var tip = "IW.ClickToDropTip".Translate();
                tip = "Currently dropping output on floor. Click to take to stockpile.";
            }
            // Drop/take to stockpile
            var button_rect = new Rect(baseRect.xMax - (24 + 4) * 4 + 12, baseRect.y, 24f, 24f);
            if (Widgets.ButtonImage(button_rect, storeModeImage, baseColor)) {
                SoundDefOf.DragSlider.PlayOneShotOnCamera();
                __instance.SetStoreMode(nextStoreMode);
            }
            TooltipHandler.TipRegion(button_rect, tip);
            // Paste bill as linked
            button_rect.width = 24;
            button_rect.x -= button_rect.width + 4;
			if (curr_copied != null && blt.Parent != curr_copied && curr_copied != blt) {
				if (Widgets.ButtonText(button_rect, "", true, true, baseColor)) {
                    SoundDefOf.DragSlider.PlayOneShotOnCamera();
                    blt.LinkToParent(curr_copied);
				}
                // Link symbol
                Rect img = button_rect.ContractedBy(2);
                var col = Mouse.IsOver(button_rect) ? Widgets.MouseoverOptionColor : Widgets.NormalOptionColor;
				GUI.DrawTexture(img, Resources.linkImage, ScaleMode.ScaleToFit, true, 1, col, 0, 0);

                TooltipHandler.TipRegion(button_rect, "CD.M.tooltips.make_link".Translate());
            }
            // Break link to parent bill
            button_rect.width = 24 + 4 + 24;
            button_rect.x -= button_rect.width + 4;
            if (blt.Parent != null) {
                if (BillLinkTracker.RenderBreakLink(blt, button_rect.x, button_rect.y)) {
                    SoundDefOf.DragSlider.PlayOneShotOnCamera();
                    blt.BreakLink();
                }
            }
        }
	}
}
