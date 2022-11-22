using RimWorld;
using Verse;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Collections.Generic;
using HarmonyLib;
using System.Reflection;


namespace CrunchyDuck.Math {
	class BillLinkTracker {
		public static BillLinkTracker currentlyCopied;
		public static SortedDictionary<int, BillLinkTracker> linkIds = new SortedDictionary<int, BillLinkTracker>();
		private static int nextLinkID = 0;
		public static int NextLinkID { 
			get {
				if (!linkIds.ContainsKey(nextLinkID))
					return nextLinkID;
				// Find next vacant ID.
				for (int i = 0; ; i++) {
					if (!linkIds.ContainsKey(i)) {
						nextLinkID = i;
						return nextLinkID;
					}
				}
			}
		}

		private static FieldInfo getThings = AccessTools.Field(typeof(ThingFilter), "thingDefs");
		private static FieldInfo getDisallowedDefs = AccessTools.Field(typeof(ThingFilter), "disallowedThingDefs");


		public BillComponent bc;
		public int myID = -1;
		public bool isMasterBC = false;
		private HashSet<BillLinkTracker> childBCs = new HashSet<BillLinkTracker>();

		public BillLinkTracker parent = null;
		//public bool ingredientsCompatible = false;
		public bool countingCompatible = false;

		public BillLinkTracker(BillComponent bc) {
			this.bc = bc;
		}

		/// <summary>
		/// Break the link with a parent, from the child.
		/// </summary>
		public void BreakLink() {
			parent.BreakLink(this);
		}

		/// <summary>
		/// Break the link with a child, from the parent.
		/// </summary>
		public void BreakLink(BillLinkTracker child) {
			RemoveChild(child);
			child.parent = null;
		}

		public void LinkToParent(BillLinkTracker parent) {
			parent.LinkToChild(this);
		}

		public void LinkToChild(BillLinkTracker child) {
			// steal baby
			if (child.parent != null) {
				child.parent.BreakLink(child);
			}
			AddChild(child);
		}

		/// <summary>
		/// Add a child, and inform that child it has been a new daddy.
		/// </summary>
		private void AddChild(BillLinkTracker child) {
			if (!isMasterBC) {
				isMasterBC = true;
				myID = NextLinkID;
				linkIds[myID] = this;
			}

			child.parent = this;
			child.countingCompatible = CanCountProducts(child) == CanCountProducts(this);
			childBCs.Add(child);
		}

		private static bool CanCountProducts(BillLinkTracker blt) {
			// Taken from RecipeWorkerCounter.CanCountProducts (Why does that method need bill?)
			var recipe = blt.bc.targetBill.recipe;
			return recipe.specialProducts == null && recipe.products != null && recipe.products.Count == 1;
		}

		/// <summary>
		/// Remove a child. This does not inform the child it has been abandoned.
		/// </summary>
		private void RemoveChild(BillLinkTracker child) {
			childBCs.Remove(child);
			if (childBCs.Count == 0) {
				isMasterBC = false;
				linkIds.Remove(myID);
				myID = -1;
			}
		}

		public void UpdateLinkedBills() {
			// TODO: Link more fields.
			foreach (BillLinkTracker blt in childBCs) {
				var other = blt.bc;

				MatchInputField(other.doXTimes, bc.doXTimes);
				MatchInputField(other.doUntilX, bc.doUntilX);
				MatchInputField(other.itemsToCount, bc.itemsToCount);
				MatchInputField(other.unpause, bc.unpause);
				other.customItemsToCount = bc.customItemsToCount;

				other.targetBill.suspended = bc.targetBill.suspended;

				other.targetBill.repeatMode = bc.targetBill.repeatMode;
				other.targetBill.paused = bc.targetBill.paused;
				other.targetBill.SetStoreMode(bc.targetBill.GetStoreMode());
				MatchIngredients(bc, other);
			}
		}

		private static void MatchInputField(InputField from, InputField to) {
			from.buffer = to.buffer;
			from.CurrentValue = to.CurrentValue;
			from.lastValid = to.lastValid;
		}

		// TODO: Check performance of this.
		private static void MatchIngredients(BillComponent from, BillComponent to) {
			var t = to.targetBill.ingredientFilter;
			var f = from.targetBill.ingredientFilter;

			//foreach (ThingDef td in (HashSet<ThingDef>)getAllowedDefs.GetValue(f)) {
			//	t.SetAllow(td, true);
			//}
			//foreach (ThingDef td in (List<ThingDef>)getDisallowedDefs.GetValue(f)) {
			//	t.SetAllow(td, false);
			//	Log.Error("Here");
			//}
			// TODO: doesn't work
			Log.Error((getThings == null).ToString());
			var r = (List<ThingDef>)getThings.GetValue(f);
			Log.Error((r == null).ToString());
			foreach (ThingDef td in (List<ThingDef>)getThings.GetValue(f)) {
				t.SetAllow(td, f.Allows(td));
			}
		}

		// Yes, it should be are. But naming consistency.
		public bool IsIngredientsCompatible(BillComponent other) {
			// TODO: Check this works at all.
			return bc.targetBill.ingredientFilter == other.targetBill.ingredientFilter;
		}
	}
}
