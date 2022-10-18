using RimWorld;
using Verse;
using HarmonyLib;
using System.Collections.Generic;

namespace CrunchyDuck.Math {
	// TODO: check how this works with medical bills.
	// TODO: How do we know when a bill is destroyed? Guess it doesn't matter too much.
	// TODO: Handle copy/pasting.
	// TODO: The + and - recipe buttons don't work. This is because of how my last_valid is filled.
	class BillManager : GameComponent {
		public static Dictionary<int, BillComponent> billTable = new Dictionary<int, BillComponent>();

		public BillManager(Game game) {
		}

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
			foreach (BillComponent item in billTable.Values) {
				Math.DoMath(item.target_count_last_valid, ref item.targetBill.targetCount, item);
				Math.DoMath(item.repeat_count_last_valid, ref item.targetBill.repeatCount, item);
				Math.DoMath(item.unpause_last_valid, ref item.targetBill.unpauseWhenYouHave, item);
			}
		}

		public static int GetBillID(Bill_Production bill_production) {
			return (int)AccessTools.Field(typeof(Bill), "loadID").GetValue(bill_production);
		}
	}
}
