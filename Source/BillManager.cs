using RimWorld;
using Verse;
using HarmonyLib;
using System.Collections.Generic;

namespace CrunchyDuck.Math {
	// TODO: check how this works with medical bills.
	// TODO: How do we know when a bill is destroyed? Guess it doesn't matter too much.
	// TODO: Handle copy/pasting.
	// TODO: Support for Pause When Satisfied + Do X Times
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
				var num = item.targetBill.targetCount;
				Math.DoMath(item.last_valid_input, ref num, item);
				item.targetBill.targetCount = num;
			}
		}

		public static int GetBillID(Bill_Production bill_production) {
			return (int)AccessTools.Field(typeof(Bill), "loadID").GetValue(bill_production);
		}
	}
}
