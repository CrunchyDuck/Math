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


		public BillComponent bc;
		public int myID = -1;
		public bool isMasterBC = false;
		private HashSet<BillLinkTracker> childBCs = new HashSet<BillLinkTracker>();

		public BillLinkTracker parent = null;

		public bool linkName = false;
		public bool linkSuspended = false;
		public bool linkTargetCount = false;
		public bool linkCustomItemCount = false;
		public bool linkPause = false;
		public bool linkTainted = false;
		public bool linkEquipped = false;
		public bool linkOnlyAllowedIngredients = false;
		public bool linkCountHitpoints = false;
		public bool linkCountQuality = false;
		public bool linkCheckStockpiles = false;
		public bool linkStockpiles = false;
		public bool linkRepeatMode = false;
		public bool linkWorkers = false;
		public bool linkIngredients = false;
		public bool linkIngredientsRadius = false;

		public bool compatibleStockpiles = false;
		public bool compatibleRepeatMode = false;
		public bool compatibleWorkers = false;
		public bool compatibleIngredients = false;

		public BillLinkTracker(BillComponent bc) {
			this.bc = bc;
		}

		public void UpdateLinkedBills() {
			foreach (BillLinkTracker c in childBCs) {
				var other = c.bc;

				if (c.linkName)
					c.bc.name = bc.name;

				if (c.linkSuspended)
					other.targetBill.suspended = bc.targetBill.suspended;

				if (c.linkTargetCount) {
					MatchInputField(other.doXTimes, bc.doXTimes);
					MatchInputField(other.doUntilX, bc.doUntilX);
				}

				if (c.linkCustomItemCount) {
					MatchInputField(other.itemsToCount, bc.itemsToCount);
					other.customItemsToCount = bc.customItemsToCount;
				}

				if (c.linkPause) {
					MatchInputField(other.unpause, bc.unpause);
					other.targetBill.paused = bc.targetBill.paused;
				}

				if (c.linkTainted)
					c.bc.targetBill.includeTainted = bc.targetBill.includeTainted;

				if (linkEquipped)
					c.bc.targetBill.includeEquipped = bc.targetBill.includeEquipped;

				if (linkOnlyAllowedIngredients)
					c.bc.targetBill.limitToAllowedStuff = bc.targetBill.limitToAllowedStuff;

				if (linkCountHitpoints)
					c.bc.targetBill.hpRange = bc.targetBill.hpRange;

				if (linkCountQuality)
					c.bc.targetBill.qualityRange = bc.targetBill.qualityRange;

				if (linkCheckStockpiles)
					c.bc.targetBill.includeFromZone = bc.targetBill.includeFromZone;

				if (c.compatibleRepeatMode && c.linkRepeatMode)
					other.targetBill.repeatMode = bc.targetBill.repeatMode;

				if (c.compatibleStockpiles && c.linkStockpiles)
					other.targetBill.SetStoreMode(bc.targetBill.GetStoreMode());

				if (c.compatibleWorkers && c.linkWorkers) {
					// Similar code to Bill.SetPawnRestriction
					other.targetBill.pawnRestriction = bc.targetBill.pawnRestriction;
					other.targetBill.slavesOnly = bc.targetBill.slavesOnly;
					other.targetBill.mechsOnly = bc.targetBill.mechsOnly;
					other.targetBill.nonMechsOnly = bc.targetBill.nonMechsOnly;
				}

				if (c.compatibleIngredients && c.linkIngredients)
					MatchIngredients(bc, other);
			}
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
			child.linkName = false;
			child.linkSuspended = child.linkTargetCount =
				child.linkCustomItemCount = child.linkPause = 
				child.linkTainted = child.linkEquipped =
				child.linkOnlyAllowedIngredients = child.linkCountHitpoints =
				child.linkCountQuality = child.linkCheckStockpiles = true;
			
			child.compatibleStockpiles = child.linkStockpiles = AreStockpilesCompatible(child.bc);
			child.compatibleRepeatMode = child.linkRepeatMode = CanCountProducts(child) == CanCountProducts(this);
			child.compatibleWorkers = child.linkWorkers = child.linkIngredientsRadius = AreWorkersCompatible(child.bc);
			child.compatibleIngredients = child.linkIngredients = AreIngredientsCompatible(child.bc);
			childBCs.Add(child);
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

		private static void MatchInputField(InputField from, InputField to) {
			from.buffer = to.buffer;
			from.CurrentValue = to.CurrentValue;
			from.lastValid = to.lastValid;
		}

		private static void MatchIngredients(BillComponent from, BillComponent to) {
			to.targetBill.ingredientFilter.allowedDefs = new HashSet<ThingDef>(from.targetBill.ingredientFilter.allowedDefs);
			if (from.linkTracker.linkIngredientsRadius)
				to.targetBill.ingredientSearchRadius = from.targetBill.ingredientSearchRadius;
			// I believe that because they're structs, this is fine. I'm still learning to ref vs val stuff.
			to.targetBill.ingredientFilter.AllowedHitPointsPercents = from.targetBill.ingredientFilter.AllowedHitPointsPercents;
			to.targetBill.ingredientFilter.AllowedQualityLevels = from.targetBill.ingredientFilter.AllowedQualityLevels;
		}

		public bool AreIngredientsCompatible(BillComponent other) {
			// TODO: Special filters. I dislike vanilla's filter code immensely.
			// Can fixedIngredientFilter be null? i suspect so.
			if (bc.targetBill.recipe.fixedIngredientFilter == null || other.targetBill.recipe.fixedIngredientFilter == null) {
				Log.ErrorOnce("Hello! CrunchyDuck from Math!. Please tell me what recipe that was on so I improve my code :)", 2278);
				return false;
			}
			var t_defs = bc.targetBill.recipe.fixedIngredientFilter.allowedDefs;
			var o_defs = other.targetBill.recipe.fixedIngredientFilter.allowedDefs;

			return t_defs.SetEquals(o_defs);
		}
	
		public bool AreStockpilesCompatible(BillComponent other) {
			// This is the same code that is used to determine if you can select a stockpile, in RecipeWorkerCounter.CanPossiblyStoreInStockpile
			return bc.targetBill.recipe.products[0].thingDef == other.targetBill.recipe.products[0].thingDef;
		}

		public bool AreWorkersCompatible(BillComponent other) {
			// Similar definition to what's used in
			//  Dialog_BillConfig.GeneratePawnRestrictionOptions &
			//  BillDialogUtility.GetPawnRestrictionOptionsForBill
			var mb = bc.targetBill;
			var ob = other.targetBill;
			var mwg = mb.billStack.billGiver.GetWorkgiver();
			var owg = ob.billStack.billGiver.GetWorkgiver();

			if (mb.recipe.mechanitorOnlyRecipe != ob.recipe.mechanitorOnlyRecipe)
				return false;
			//if (mb.recipe.workSkill != ob.recipe.workSkill)
			//	return false;
			if (mwg != owg)
				return false;

			return true;
		}

		private static bool CanCountProducts(BillLinkTracker blt) {
			// Taken from RecipeWorkerCounter.CanCountProducts (Why does that method need bill?)
			var recipe = blt.bc.targetBill.recipe;
			return recipe.specialProducts == null && recipe.products != null && recipe.products.Count == 1;
		}
	}
}
