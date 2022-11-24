using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;


namespace CrunchyDuck.Math {
	public class BillLinkTracker : IExposable {
		public static BillLinkTracker currentlyCopied;
		/// I separate out these links
		/// 1. so they can be iterated nicely.
		/// 2. so they can have nicer display values, rather than "link 263"
		public static SortedDictionary<int, BillLinkTracker> linkIDs = new SortedDictionary<int, BillLinkTracker>();
		private static int nextLinkID = 0;
		public static int NextLinkID { 
			get {
				if (!linkIDs.ContainsKey(nextLinkID))
					return nextLinkID;
				// Find next vacant ID.
				for (int i = 0; ; i++) {
					if (!linkIDs.ContainsKey(i)) {
						nextLinkID = i;
						return nextLinkID;
					}
				}
			}
		}
		public static SortedDictionary<int, BillLinkTracker> IDs = new SortedDictionary<int, BillLinkTracker>();
		private static int nextID = 0;
		public static int NextID {
			get {
				if (!IDs.ContainsKey(nextID))
					return nextID;
				// Find a vacant ID.
				for (int i = 0; ; i++) {
					if (!IDs.ContainsKey(i)) {
						nextID = i;
						return nextID;
					}
				}
			}
		}

		public BillComponent bc;

		private int myID = -1;  // What's my ID in IDs?
		private int parentID = -1;  // What my parent's ID is in linkIDs
		public int linkID = -1;  // What my ID is in linkIDs
		public bool isMasterBC = false;
		private HashSet<BillLinkTracker> children = new HashSet<BillLinkTracker>();

		public BillLinkTracker Parent {
			get {
				if (parentID == -1)
					return null;
				return linkIDs[parentID];
			}
			set {
				if (value == null)
					parentID = -1;
				else
					parentID = value.linkID;
			}
		}

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
			myID = NextID;
			IDs[myID] = this;
		}

		public void UpdateLinkedBills() {
			foreach (BillLinkTracker c in children) {
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
			Parent.BreakLink(this);
		}

		/// <summary>
		/// Break the link with a child, from the parent.
		/// </summary>
		public void BreakLink(BillLinkTracker child) {
			RemoveChild(child);
			child.Parent = null;
		}

		public void LinkToParent(BillLinkTracker parent) {
			parent.LinkToChild(this);
		}

		public void LinkToChild(BillLinkTracker child) {
			// steal baby
			if (child.Parent != null) {
				child.Parent.BreakLink(child);
			}
			AddChild(child);
		}

		/// <summary>
		/// Add a child, and inform that child it has been a new daddy.
		/// </summary>
		private void AddChild(BillLinkTracker child) {
			if (!isMasterBC) {
				isMasterBC = true;
				linkID = NextLinkID;
				linkIDs[linkID] = this;
			}

			child.Parent = this;
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
			children.Add(child);
		}

		/// <summary>
		/// Remove a child. This does not inform the child it has been abandoned.
		/// </summary>
		private void RemoveChild(BillLinkTracker child) {
			children.Remove(child);
			if (children.Count == 0) {
				isMasterBC = false;
				linkIDs.Remove(linkID);
				linkID = -1;
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

		public void ExposeData() {
			Scribe_Values.Look(ref myID, "myID");
			Scribe_Values.Look(ref linkID, "linkID", -1);
			if (linkID != -1)
				linkIDs[linkID] = this;
			Scribe_Values.Look(ref isMasterBC, "isMasterBC", false);
			Scribe_Values.Look(ref parentID, "parentID", -1);

			Scribe_Values.Look(ref linkName, "linkName", false, true);
			Scribe_Values.Look(ref linkSuspended, "linkSuspended", false, true);
			Scribe_Values.Look(ref linkTargetCount, "linkTargetCount", false, true);
			Scribe_Values.Look(ref linkCustomItemCount, "linkCustomItemCount", false, true);
			Scribe_Values.Look(ref linkPause, "linkPause", false, true);
			Scribe_Values.Look(ref linkTainted, "linkTainted", false, true);
			Scribe_Values.Look(ref linkEquipped, "linkEquipped", false, true);
			Scribe_Values.Look(ref linkOnlyAllowedIngredients, "linkOnlyAllowedIngredients", false, true);
			Scribe_Values.Look(ref linkCountHitpoints, "linkCountHitpoints", false, true);
			Scribe_Values.Look(ref linkCountQuality, "linkCountQuality", false, true);
			Scribe_Values.Look(ref linkCheckStockpiles, "linkCheckStockpiles", false, true);
			Scribe_Values.Look(ref linkStockpiles, "linkStockpiles", false, true);
			Scribe_Values.Look(ref linkRepeatMode, "linkRepeatMode", false, true);
			Scribe_Values.Look(ref linkWorkers, "linkWorkers", false, true);
			Scribe_Values.Look(ref linkIngredients, "linkIngredients", false, true);
			Scribe_Values.Look(ref linkIngredientsRadius, "linkIngredientsRadius", false, true);

			Scribe_Values.Look(ref compatibleStockpiles, "compatibleStockpiles", false, true);
			Scribe_Values.Look(ref compatibleRepeatMode, "compatibleRepeatMode", false, true);
			Scribe_Values.Look(ref compatibleWorkers, "compatibleWorkers", false, true);
			Scribe_Values.Look(ref compatibleIngredients, "compatibleIngredients", false, true);

			HashSet<int> child_ids = children.Select(bc => bc.myID).ToHashSet();
			Scribe_Collections.Look(ref child_ids, "children", LookMode.Value);
			if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs) {
				if (child_ids != null) {
					children = child_ids.Select(i => IDs[i]).ToHashSet();
				}
			}
		}
	}
}
