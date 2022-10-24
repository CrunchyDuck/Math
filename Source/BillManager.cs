using RimWorld;
using Verse;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System;

namespace CrunchyDuck.Math {
	// TODO: Handle copy/pasting.
	class BillManager : GameComponent {
		public static Dictionary<int, BillComponent> billTable = new Dictionary<int, BillComponent>();
		public const int updateRegularity = 2500;  // 1 in game hour.
		public static Dictionary<string, ThingDef> searchabeThings = new Dictionary<string, ThingDef>();

		public BillManager(Game game) {}

		// Create it if it doesn't exist and return it.
		public static BillComponent AddGetBillComponent(Bill_Production bill) {
			int load_id = GetBillID(bill);
			BillComponent bill_comp;
			if (billTable.ContainsKey(load_id)) {
				bill_comp = billTable[load_id];
			}
			else {
				bill_comp = new BillComponent(bill);
				billTable[load_id] = bill_comp;
			}
			return bill_comp;
		}

		public override void GameComponentTick() {
			base.GameComponentTick();
			// Make sure bills are up to date.
			if (Current.Game.tickManager.TicksGame % updateRegularity == 0) {
				foreach (BillComponent item in billTable.Values) {
					// I think I put this here to fix something sometime.
					// But testing, the problem it fixed isn't a problem, and it actually breaks other things.
					// Number changed, likely because they pressed + or -.
					//if (item.targetBill.targetCount != item.target_count_last_result) {
					//	item.target_count_last_result = item.targetBill.targetCount;
					//	item.target_count_last_valid = item.targetBill.targetCount.ToString();
					//}
					//else
					Math.DoMath(item.doUntilXLastValid, ref item.targetBill.targetCount, item);
					//Math.DoMath(item.repeat_count_last_valid, ref item.targetBill.repeatCount, item);
					Math.DoMath(item.unpauseLastValid, ref item.targetBill.unpauseWhenYouHave, item);
				}

				Math.ClearCacheMaps();
			}
		}

		public static int GetBillID(Bill_Production bill_production) {
			return (int)AccessTools.Field(typeof(Bill), "loadID").GetValue(bill_production);
		}
	}
}
