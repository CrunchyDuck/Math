using HarmonyLib;
using System.Reflection;
using RimWorld;
using Verse;
using UnityEngine;
using System;

namespace CrunchyDuck.Math {
	class Bill_Production_DoConfigInterface_Patch {
        private static Texture2D bestStockpileImage = ContentFinder<Texture2D>.Get("BWM_BestStockpile");
        private static Texture2D dropOnFloorImage = ContentFinder<Texture2D>.Get("BWM_DropOnFloor");

        public static MethodInfo Target() {
			return AccessTools.Method(typeof(Bill_Production), "DoConfigInterface");
		}

		public static void Postfix(Bill_Production __instance, Rect baseRect, Color baseColor) {
            var storeModeImage = bestStockpileImage;
            var nextStoreMode = BillStoreModeDefOf.DropOnFloor;
            // TODO: Implement translations.
            //var tip = "IW.ClickToDropTip".Translate();
            var tip = "Currently dropping output on floor. Click to take to stockpile.";
            if (__instance.GetStoreMode() == BillStoreModeDefOf.DropOnFloor) {
                storeModeImage = dropOnFloorImage;
                nextStoreMode = BillStoreModeDefOf.BestStockpile;
                //tip = "IW.ClickToTakeToStockpileTip".Translate();
                tip = "Current taking output to stockpile. Click to drop on floor.";
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
