using System;
using System.Collections.Generic;
using Inventory;
using JetBrains.Annotations;
using Verse;
using RimWorld;

namespace CrunchyDuck.Math.MathFilters {
	class CompositableLoadoutTagsFilter : MathFilter {
		public static HashSet<string> names = new HashSet<string> { "loadout tags", "lt" };
		public override bool CanCount { get { return true; } }

		private Tag tag;
		private BillComponent bc;

		[CanBeNull]
		public static bool TryFindTagByName(string name, out Tag tagResult) {
			LoadoutManager loadoutManager = Current.Game.GetComponent<LoadoutManager>();
			// This looks like it uses a for loop internally, and is cleaner.
			// Also, since tag names are user defined, this will just return the first tag with the name, duplicate tag names are user error.
			tagResult = loadoutManager.tags.Find(tag => name.Equals(tag.name, StringComparison.CurrentCultureIgnoreCase));
			return tagResult != null;
		}
		
		public CompositableLoadoutTagsFilter(BillComponent bc, Tag tag) {
			this.bc = bc;
			this.tag = tag;
		}

		public override float Count() {
			if (tag == null) {
				return 0;
			}
			LoadoutManager loadoutManager = Current.Game.GetComponent<LoadoutManager>();
			return loadoutManager.pawnTags[tag].Pawns.Count(p => p != null && !p.Dead && p.IsValidLoadoutHolder() && p.Map == bc.targetBill.Map && p.HostFaction == null);
		}

		public override ReturnType Parse(string command, out object result) {
			throw new NotImplementedException();
		}

		//public override ReturnType ParseType(string command) {
		//	throw new NotImplementedException();
		//}
	}
}
