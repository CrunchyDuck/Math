using RimWorld;
using Verse;

namespace CrunchyDuck.Math {
	class BillComponent {
		public Bill_Production targetBill;
		public CachedMapData Cache {
			get {
				return Math.GetCachedMap(targetBill.Map);
			}
		}
		public int loadID { get { return BillManager.GetBillID(targetBill); } }
		// I have to maintain my own buffers so I can modify them at will, e.g. when a + or - button is pressed.
		public InputField doXTimes;
		public InputField doUntilX;
		public InputField unpause;
		public bool isDoXTimes { get { return targetBill.repeatMode == BillRepeatModeDefOf.RepeatCount; } }
		public bool isDoUntilX { get { return targetBill.repeatMode == BillRepeatModeDefOf.TargetCount; } }


		public BillComponent(Bill_Production bill) {
			targetBill = bill;
			doXTimes = new InputField(bill, InputField.Field.DoXTimes);
			doUntilX = new InputField(bill, InputField.Field.DoUntilX);
			unpause = new InputField(bill, InputField.Field.Unpause);
		}

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
					default:
						return bill.repeatCount;
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
					default:
						bill.unpauseWhenYouHave = value;
						break;
				}
			}
		}

		public InputField(Bill_Production bp, Field field) {
			bill = bp;
			this.field = field;
		}

		public void SetAll(int value) {
			buffer = value.ToString();
			lastValid = value.ToString();
			CurrentValue = value;
		}

		public enum Field {
			DoUntilX,
			DoXTimes,
			Unpause
		}
	}
}
