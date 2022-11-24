using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;


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

		public Dictionary<string, LinkSetting> linkSettings = new Dictionary<string, LinkSetting>();

		public BillLinkTracker(BillComponent bc) {
			this.bc = bc;
			myID = NextID;
			IDs[myID] = this;
			linkSettings = GenerateLinkSettings(this);
		}

		public void UpdateLinkedBills() {
			foreach (BillLinkTracker c in children) {
				foreach (LinkSetting sett in c.linkSettings.Values) {
					sett.UpdateFromParent();
				}
			}
		}

		/// <summary>
		/// Make this child update its parents' values.
		/// </summary>
		public void UpdateParent() {
			foreach (LinkSetting sett in linkSettings.Values) {
				if (!sett.Enabled)
					continue;
				sett.UpdateToParent();
			}
		}

		/// <summary>
		/// Pastes every compatible setting, except name.
		/// </summary>
		public static void PasteAll(BillLinkTracker from, BillLinkTracker to) {

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
			// TODO: Fill in compatibilities.
			child.linkSettings["stockpiles"].compatibleWithParent = AreStockpilesCompatible(child.bc);
			child.linkSettings["repeatMode"].compatibleWithParent = CanCountProducts(child) == CanCountProducts(this);
			child.linkSettings["workers"].compatibleWithParent = AreWorkersCompatible(child.bc);
			child.linkSettings["ingredients"].compatibleWithParent = AreIngredientsCompatible(child.bc);
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
			if (from.linkTracker.linkSettings["ingredientRadius"].Enabled)
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

			foreach (LinkSetting sett in linkSettings.Values) {
				Scribe_Values.Look(ref sett.state, sett.name, sett.defaultState);
				Scribe_Values.Look(ref sett.state, sett.name + "Compatible", true);
			}

			HashSet<int> child_ids = children.Select(bc => bc.myID).ToHashSet();
			Scribe_Collections.Look(ref child_ids, "children", LookMode.Value);
			if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs) {
				if (child_ids != null) {
					children = child_ids.Select(i => IDs[i]).ToHashSet();
				}
			}
		}
	
		public static Dictionary<string, LinkSetting> GenerateLinkSettings(BillLinkTracker owner) {
			Dictionary<string, LinkSetting> sett = new Dictionary<string, LinkSetting>();

			string setting_name;
			Action<BillLinkTracker, BillLinkTracker> update;

			setting_name = "name";
			update = (from, to) => to.bc.name = from.bc.name;
			sett.Add(setting_name, new LinkSetting(owner, setting_name, update, false));

			setting_name = "suspended";
			update = (from, to) => to.bc.targetBill.suspended = from.bc.targetBill.suspended;
			sett.Add(setting_name, new LinkSetting(owner, setting_name, update));

			setting_name = "targetCount";
			update = (from, to) => {
				MatchInputField(from.bc.doXTimes, to.bc.doXTimes);
				MatchInputField(from.bc.doUntilX, to.bc.doUntilX);
			};
			sett.Add(setting_name, new LinkSetting(owner, setting_name, update));

			setting_name = "customItemCount";
			update = (from, to) => {
				MatchInputField(from.bc.itemsToCount, to.bc.itemsToCount);
				to.bc.customItemsToCount = from.bc.customItemsToCount;
			};
			sett.Add(setting_name, new LinkSetting(owner, setting_name, update));

			setting_name = "pause";
			update = (from, to) => {
				MatchInputField(from.bc.unpause, to.bc.unpause);
				to.bc.targetBill.paused = from.bc.targetBill.paused;
			};
			sett.Add(setting_name, new LinkSetting(owner, setting_name, update));

			setting_name = "tainted";
			update = (from, to) => to.bc.targetBill.includeTainted = from.bc.targetBill.includeTainted;
			sett.Add(setting_name, new LinkSetting(owner, setting_name, update));

			setting_name = "equipped";
			update = (from, to) => to.bc.targetBill.includeEquipped = from.bc.targetBill.includeEquipped;
			sett.Add(setting_name, new LinkSetting(owner, setting_name, update));

			setting_name = "onlyAllowedIngredients";
			update = (from, to) => to.bc.targetBill.limitToAllowedStuff = from.bc.targetBill.limitToAllowedStuff;
			sett.Add(setting_name, new LinkSetting(owner, setting_name, update));

			setting_name = "countHitpoints";
			update = (from, to) => to.bc.targetBill.hpRange = from.bc.targetBill.hpRange;
			sett.Add(setting_name, new LinkSetting(owner, setting_name, update));

			setting_name = "countQuality";
			update = (from, to) => to.bc.targetBill.qualityRange = from.bc.targetBill.qualityRange;
			sett.Add(setting_name, new LinkSetting(owner, setting_name, update));

			setting_name = "checkStockpile";
			update = (from, to) => to.bc.targetBill.includeFromZone = from.bc.targetBill.includeFromZone;
			sett.Add(setting_name, new LinkSetting(owner, setting_name, update));

			setting_name = "ingredientsRadius";
			update = (from, to) => to.bc.targetBill.ingredientSearchRadius = from.bc.targetBill.ingredientSearchRadius;
			sett.Add(setting_name, new LinkSetting(owner, setting_name, update));


			setting_name = "stockpiles";
			update = (from, to) => to.bc.targetBill.SetStoreMode(from.bc.targetBill.GetStoreMode());
			sett.Add(setting_name, new LinkSetting(owner, setting_name, update));

			setting_name = "repeatMode";
			update = (from, to) => to.bc.targetBill.repeatMode = from.bc.targetBill.repeatMode;
			sett.Add(setting_name, new LinkSetting(owner, setting_name, update));

			setting_name = "workers";
			update = (from, to) => {
				// Similar code to Bill.SetPawnRestriction
				to.bc.targetBill.pawnRestriction = from.bc.targetBill.pawnRestriction;
				to.bc.targetBill.slavesOnly = from.bc.targetBill.slavesOnly;
				to.bc.targetBill.mechsOnly = from.bc.targetBill.mechsOnly;
				to.bc.targetBill.nonMechsOnly = from.bc.targetBill.nonMechsOnly;
			};
			sett.Add(setting_name, new LinkSetting(owner, setting_name, update));

			setting_name = "ingredients";
			update = (from, to) => MatchIngredients(from.bc, to.bc);
			sett.Add(setting_name, new LinkSetting(owner, setting_name, update));

			sett["ingredientsRadius"].state = sett["ingredients"].Enabled;

			return sett;
		}
	}

	public class LinkSetting {
		public string name;
		public BillLinkTracker owner;
		private Action<BillLinkTracker, BillLinkTracker> update = null;

		public bool defaultState = true;
		public bool state = true;
		public bool compatibleWithParent = true;
		public bool Enabled { get { return state && compatibleWithParent; } }


		public LinkSetting(BillLinkTracker owner, string name, Action<BillLinkTracker, BillLinkTracker> update, bool default_state = true) {
			this.owner = owner;
			this.name = name;
			this.update = update;
			this.defaultState = this.state = default_state;
			//this.compatibleWithParent = compatible_with_parent;
		}

		public void UpdateFromParent() {
			update(owner.Parent, owner);
		}

		public void UpdateToParent() {
			update(owner, owner.Parent);
		}

		public void Reset() {
			state = defaultState;
		}
	}
}
