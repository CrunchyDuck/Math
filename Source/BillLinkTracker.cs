using RimWorld;
using Verse;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Collections.Generic;


namespace CrunchyDuck.Math {
	class BillLinkTracker {
		public BillComponent bc;

		public bool isMasterBC = false;
		private List<BillComponent> childBCs = new List<BillComponent>();

		private BillComponent parentBC = null;
		public bool ingredientsCompatible = false;

		public BillLinkTracker(BillComponent bc) {
			this.bc = bc;
		}

		public void UpdateLinkedBills() {
			// TODO: Link more fields.
			foreach (BillComponent other in childBCs) {
				// Since these are references, perhaps this only needs to be assgined once, when the link is first made.
				other.itemsToCount = bc.itemsToCount;
				other.doXTimes = bc.doXTimes;
				other.doUntilX = bc.doUntilX;
				other.unpause = bc.unpause;
				other.customItemsToCount = bc.customItemsToCount;

				other.targetBill.suspended = bc.targetBill.suspended;

				other.targetBill.repeatMode = bc.targetBill.repeatMode;
				other.targetBill.paused = bc.targetBill.paused;
				other.targetBill.SetStoreMode(bc.targetBill.GetStoreMode());

				if (ingredientsCompatible) {
					other.targetBill.ingredientFilter = bc.targetBill.ingredientFilter;
				}
			}
		}

		// Yes, it should be are. But naming consistency.
		public bool IsIngredientsCompatible(BillComponent other) {
			// TODO: Check this works at all.
			return bc.targetBill.ingredientFilter == other.targetBill.ingredientFilter;
		}
	}
}
