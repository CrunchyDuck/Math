using HarmonyLib;
using System.Reflection;
using RimWorld;
using Verse;
using UnityEngine;
using System;

namespace CrunchyDuck.Math {
	class Bill_Production_DoConfigInterface_Patch {
        public static MethodInfo Target() {
			return AccessTools.Method(typeof(Bill_Production), "DoConfigInterface");
		}

		public static void Postfix(Bill_Production __instance, Rect baseRect, Color baseColor) {
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

            //var extendedBillDataStorage = Main.Instance.GetExtendedBillDataStorage();
            var storeModeRect = new Rect(baseRect.xMax - 110f, baseRect.y, 24f, 24f);
            if (Widgets.ButtonImage(storeModeRect, storeModeImage, baseColor)) {
                __instance.SetStoreMode(nextStoreMode);
            }
            TooltipHandler.TipRegion(storeModeRect, tip);
        }
	}
}
