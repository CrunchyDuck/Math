using RimWorld;
using Verse;

namespace CrunchyDuck.Math {
	class BillComponent {
		public Bill_Production targetBill;
		public int loadID { get { return BillManager.GetBillID(targetBill); } }
		public string field_text = "";
		public string last_valid_input = "";

		public BillComponent(Bill_Production bill) {
			this.targetBill = bill;
			last_valid_input = bill.targetCount.ToString();
		}

		public void ExposeData() {
			Scribe_Values.Look(ref last_valid_input, "last_valid_input");
		}
	}
}
