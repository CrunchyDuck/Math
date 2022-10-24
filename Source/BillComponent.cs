using RimWorld;
using Verse;

namespace CrunchyDuck.Math {
	class BillComponent {
		public Bill_Production targetBill;
		public CachedMapData Cache { get { return Math.GetCachedMap(targetBill.Map); } }
		public int loadID { get { return BillManager.GetBillID(targetBill); } }
		// I have to maintain my own buffers so I can modify them at will, e.g. when a + or - button is pressed.
		public string doXTimesLastValid = "";
		public string doXTimesBuffer = "";
		public string unpauseLastValid = "";
		public string unpauseBuffer = "";
		public string doUntilXLastValid = "";
		public string doUntilXBuffer = "";
		public int doUntilXLastResult = 0;
		public int doXTimesLastResult = 0;
		public int unpauseLastResult = 0;
		public bool isDoXTimes { get { return targetBill.repeatMode == BillRepeatModeDefOf.RepeatCount; } }
		public bool isDoUntilX { get { return targetBill.repeatMode == BillRepeatModeDefOf.TargetCount; } }


		public BillComponent(Bill_Production bill) {
			targetBill = bill;
			doXTimesLastValid = bill.repeatCount.ToString();
			doUntilXLastValid = bill.targetCount.ToString();
			unpauseLastValid = bill.unpauseWhenYouHave.ToString();
		}

		public void ExposeData() {
			// I wanna change these but that'd break peoples' saves lmao
			Scribe_Values.Look(ref doXTimesLastValid, "repeat_count_last_valid");
			Scribe_Values.Look(ref doUntilXLastValid, "target_count_last_valid");
			Scribe_Values.Look(ref unpauseLastValid, "unpause_last_valid");
			Scribe_Values.Look(ref doUntilXLastResult, "target_count_last_result");
			Scribe_Values.Look(ref doXTimesLastResult, "doXTimesLastResult");
			Scribe_Values.Look(ref unpauseLastResult, "unpauseLastResult");
			unpauseBuffer = unpauseLastValid;
		}
	}
}
