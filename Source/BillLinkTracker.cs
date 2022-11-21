using RimWorld;
using Verse;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Collections.Generic;


namespace CrunchyDuck.Math {
	class BillLinkTracker {
		public static BillLinkTracker currentlyCopied;

		public BillComponent bc;
		public bool isMasterBC = false;
		private HashSet<BillLinkTracker> childBCs = new HashSet<BillLinkTracker>();

		public BillLinkTracker parent = null;
		public bool ingredientsCompatible = false;

		public BillLinkTracker(BillComponent bc) {
			this.bc = bc;
		}

		public void AddChild(BillLinkTracker blt) {
			isMasterBC = true;
			Log.Message("here");
			blt.parent = this;
			blt.ingredientsCompatible = IsIngredientsCompatible(blt.bc);
			childBCs.Add(blt);
		}

		/// <summary>
		/// Break the link with a parent, from the child.
		/// </summary>
		public void BreakLink() {
			parent.childBCs.Remove(this);
			parent = null;
		}

		/// <summary>
		/// Break the link with a child, from the parent.
		/// </summary>
		public void BreakLink(BillLinkTracker child) {
			childBCs.Remove(child);
			parent = null;
		}

		public void UpdateLinkedBills() {
			// TODO: Link more fields.
			foreach (BillLinkTracker blt in childBCs) {
				var other = blt.bc;
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
