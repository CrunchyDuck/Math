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
            var buttonRect = new Rect(baseRect.xMax - (24 + 4) * 4, baseRect.y, 24f, 24f);

            if (Widgets.ButtonImage(buttonRect, storeModeImage, baseColor)) {
                SoundDefOf.DragSlider.PlayOneShotOnCamera();
                __instance.SetStoreMode(nextStoreMode);
            }
            TooltipHandler.TipRegion(buttonRect, tip);

            // Paste bill as linked
            buttonRect.x -= 24 + 4;
            if (curr_copied != null && blt.parent != curr_copied && curr_copied != blt) {
                if (Widgets.ButtonImage(buttonRect, Resources.linkImage, baseColor)) {
                    SoundDefOf.DragSlider.PlayOneShotOnCamera();
                    curr_copied.AddChild(blt);
                }
            }

			// Break link to parent bill
            buttonRect.x -= 24 + 4;
			if (blt.parent != null) {
				if (Widgets.ButtonImage(buttonRect, Resources.breakLinkImage, baseColor)) {
					SoundDefOf.DragSlider.PlayOneShotOnCamera();
                    blt.BreakLink();
				}
			}
		}
	}
}
