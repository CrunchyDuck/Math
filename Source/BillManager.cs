using RimWorld;
using Verse;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System;

namespace CrunchyDuck.Math {
	// TODO: Handle copy/pasting.
	class BillManager : GameComponent {
		public static BillManager instance;  // singleton my beloved

		public Dictionary<int, BillComponent> billTable = new Dictionary<int, BillComponent>();
		public const int updateRegularity = 2500;  // 1 in game hour.
		public static Dictionary<string, ThingDef> searchabeThings = new Dictionary<string, ThingDef>();

		public BillManager(Game game) {
			instance = this;
		}

		// Create it if it doesn't exist and return it.
		public BillComponent AddGetBillComponent(Bill_Production bill) {
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
				// We create a copy as billTable can be modified during iteration by having null bills removed.
				// It would be more elegant to throw and catch an error to make this object handle the billTable,
				// But I use Math.DoMath in a lot of places outside of here that I don't want erroring.
				Dictionary<int, BillComponent> bt_copy = billTable.ToDictionary(entry => entry.Key, entry => entry.Value);
				foreach (BillComponent item in bt_copy.Values) {
					UpdateBill(item);
				}

				Math.ClearCacheMaps();
			}
		}

		public static void UpdateBill(BillComponent bc) {
			Math.DoMath(bc.doUntilX.lastValid, ref bc.targetBill.targetCount, bc.doUntilX);
			//Math.DoMath(item.repeat_count_last_valid, ref item.targetBill.repeatCount, item);
			Math.DoMath(bc.unpause.lastValid, ref bc.targetBill.unpauseWhenYouHave, bc.unpause);

			int i = 0;
			Math.DoMath(bc.itemsToCount.lastValid, ref i, bc.itemsToCount);
		}

		public void RemoveBillComponent(BillComponent bc) {
			var i = billTable.FirstIndexOf(kvp => kvp.Value == bc);
			billTable.Remove(i);
		}

		public static int GetBillID(Bill_Production bill_production) {
			return (int)AccessTools.Field(typeof(Bill), "loadID").GetValue(bill_production);
		}
	}
}
