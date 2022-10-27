using RimWorld;
using Verse;

namespace CrunchyDuck.Math {
	// TODO: I'm pretty sure I could actually add this component to bill tables, rather than maintaining it independently.
	// I have yet to test this though. If would clean things up a touch.
	class BillComponent {
		public Bill_Production targetBill;
		public CachedMapData Cache {
			get {
				return Math.GetCachedMap(targetBill.Map);
			}
		}
		public int loadID { get { return BillManager.GetBillID(targetBill); } }
		// I have to maintain my own buffers so I can modify them at will, e.g. when a + or - button is pressed.
		public InputField itemsToCount;
		public InputField doXTimes;
		public InputField doUntilX;
		public InputField unpause;
		public bool isDoXTimes { get { return targetBill.repeatMode == BillRepeatModeDefOf.RepeatCount; } }
		public bool isDoUntilX { get { return targetBill.repeatMode == BillRepeatModeDefOf.TargetCount; } }


		public BillComponent(Bill_Production bill) {
			targetBill = bill;
			itemsToCount = new InputField(bill, InputField.Field.itemsToCount, this);
			doXTimes = new InputField(bill, InputField.Field.DoXTimes, this);
			doUntilX = new InputField(bill, InputField.Field.DoUntilX, this);
			unpause = new InputField(bill, InputField.Field.Unpause, this, 5);
		}

		// BIG TODO: Save itemsToCount
		public void ExposeData() {
			// I wanna change these but that'd break peoples' saves lmao
			Scribe_Values.Look(ref doXTimes.lastValid, "repeat_count_last_valid");
			doXTimes.buffer = doXTimes.lastValid;
			Scribe_Values.Look(ref doUntilX.lastValid, "target_count_last_valid");
			doUntilX.buffer = doUntilX.lastValid;
			Scribe_Values.Look(ref unpause.lastValid, "unpause_last_valid");
			unpause.buffer = unpause.lastValid;

			Scribe_Values.Look(ref targetBill.targetCount, "target_count_last_result");
			Scribe_Values.Look(ref targetBill.repeatCount, "doXTimesLastResult");
			Scribe_Values.Look(ref targetBill.unpauseWhenYouHave, "unpauseLastResult");
		}
	}

	class InputField {
		private Bill_Production bill;
		public BillComponent bc;
		public Field field;
		public string lastValid = "";
		public string buffer = "";
		public int CurrentValue {
			get {
				switch (field) {
					case Field.DoUntilX:
						return bill.targetCount;
					case Field.DoXTimes:
						return bill.repeatCount;
					case Field.Unpause:
						return bill.unpauseWhenYouHave;
					// BIG TODO: What do I put here?
					default:
						return 0;
				}
			}
			set {
				switch (field) {
					case Field.DoUntilX:
						bill.targetCount = value;
						break;
					case Field.DoXTimes:
						bill.repeatCount = value;
						break;
					case Field.Unpause:
						bill.unpauseWhenYouHave = value;
						break;
					default:
						//bill.unpauseWhenYouHave = value;
						break;
				}
			}
		}

		public InputField(Bill_Production bp, Field field, BillComponent bc, int default_value = 10) {
			bill = bp;
			this.field = field;
			this.bc = bc;
			SetAll(default_value);
		}

		public void SetAll(int value) {
			buffer = value.ToString();
			lastValid = value.ToString();
			CurrentValue = value;
		}

		public void SetAll(string str, int value) {
			buffer = str;
			lastValid = str;
			CurrentValue = value;
		}

		public enum Field {
			itemsToCount,
			DoUntilX,
			DoXTimes,
			Unpause,
		}
	}
}
