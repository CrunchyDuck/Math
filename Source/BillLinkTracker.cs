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

		private static FieldInfo ThingFilter_allowedDefs = AccessTools.Field(typeof(ThingFilter), "allowedDefs");


		public BillComponent bc;
		public int myID = -1;
		public bool isMasterBC = false;
		private HashSet<BillLinkTracker> childBCs = new HashSet<BillLinkTracker>();

		public BillLinkTracker parent = null;
		public bool ingredientsCompatible = false;
		public bool repeatModeCompatible = false;

		public bool linkSuspended = false;
		public bool linkInputFields = false;
		public bool linkRepeatMode = false;
		public bool linkIngredients = false;

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
			child.linkInputFields = true;
			child.linkSuspended = true;
			child.repeatModeCompatible = child.linkRepeatMode = CanCountProducts(child) == CanCountProducts(this);
			child.ingredientsCompatible = child.linkIngredients = IsIngredientsCompatible(child.bc);
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
			foreach (BillLinkTracker child in childBCs) {
				var other = child.bc;

				if (child.linkInputFields) {
					MatchInputField(other.doXTimes, bc.doXTimes);
					MatchInputField(other.doUntilX, bc.doUntilX);
					MatchInputField(other.itemsToCount, bc.itemsToCount);
					MatchInputField(other.unpause, bc.unpause);
					other.customItemsToCount = bc.customItemsToCount;
				}

				if (child.linkSuspended)
					other.targetBill.suspended = bc.targetBill.suspended;

				if (child.repeatModeCompatible && child.linkRepeatMode) {
					other.targetBill.repeatMode = bc.targetBill.repeatMode;
					other.targetBill.paused = bc.targetBill.paused;
				}

				// TODO: Check store locations are compatible.
				other.targetBill.SetStoreMode(bc.targetBill.GetStoreMode());
				
				if (child.ingredientsCompatible && child.linkIngredients)
					MatchIngredients(bc, other);
			}
		}

		private static void MatchInputField(InputField from, InputField to) {
			from.buffer = to.buffer;
			from.CurrentValue = to.CurrentValue;
			from.lastValid = to.lastValid;
		}

		private static void MatchIngredients(BillComponent from, BillComponent to) {
			// TODO: Match special filters.
			var f = (HashSet<ThingDef>)ThingFilter_allowedDefs.GetValue(from.targetBill.ingredientFilter);
			ThingFilter_allowedDefs.SetValue(to.targetBill.ingredientFilter, new HashSet<ThingDef>(f));
		}

		// Yes, it should be are. But naming consistency.
		public bool IsIngredientsCompatible(BillComponent other) {
			// Can fixedIngredientFilter be null? i suspect so.
			var f = (HashSet<ThingDef>)ThingFilter_allowedDefs.GetValue(bc.targetBill.recipe.fixedIngredientFilter);
			var t = (HashSet<ThingDef>)ThingFilter_allowedDefs.GetValue(other.targetBill.recipe.fixedIngredientFilter);

			return f.SetEquals(t);
		}
	}
}
