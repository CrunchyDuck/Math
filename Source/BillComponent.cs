using RimWorld;
using Verse;

namespace CrunchyDuck.Math {
	class BillComponent {
		public Bill_Production targetBill;
		public CachedMapData Cache { get { return Math.GetCachedMap(targetBill.Map); } }
		public int loadID { get { return BillManager.GetBillID(targetBill); } }
		public string repeat_count_last_valid = "";
		public string unpause_last_valid = "";
		public string unpause_buffer = "";
		public string target_count_last_valid = "";
		public int target_count_last_result = 0;
		public bool isDoXTimes { get { return targetBill.repeatMode == BillRepeatModeDefOf.RepeatCount; } }
		public bool isDoUntilX { get { return targetBill.repeatMode == BillRepeatModeDefOf.TargetCount; } }


		public BillComponent(Bill_Production bill) {
			targetBill = bill;
			repeat_count_last_valid = bill.repeatCount.ToString();
			target_count_last_valid = bill.targetCount.ToString();
			unpause_last_valid = bill.unpauseWhenYouHave.ToString();
		}

		public void ExposeData() {
			Scribe_Values.Look(ref repeat_count_last_valid, "repeat_count_last_valid");
			Scribe_Values.Look(ref target_count_last_valid, "target_count_last_valid");
			Scribe_Values.Look(ref target_count_last_result, "target_count_last_result");
			Scribe_Values.Look(ref unpause_last_valid, "unpause_last_valid");
			unpause_buffer = unpause_last_valid;
		}
	}
}
