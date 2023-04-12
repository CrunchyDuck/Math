using CrunchyDuck.Math.ModCompat;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CrunchyDuck.Math.MathFilters {
	class CompositableLoadoutTagsFilter : MathFilter {
		public static HashSet<string> names = new HashSet<string> { "loadout tags", "lt" };
		public override bool CanCount { get { return true; } }

		private object tag;
		private BillComponent bc;

		public CompositableLoadoutTagsFilter(BillComponent bc, object tag) {
			this.bc = bc;
			this.tag = tag;
		}

		public override float Count() {
			if (tag == null) {
				return 0;
			}
			return CompositableLoadoutsSupport.GetPawnsWithTag(tag).Count(p => p != null && !p.Dead && CompositableLoadoutsSupport.IsValidLoadoutHolder(p) && p.Map == bc.targetBill.Map && p.HostFaction == null);
		}

		public override ReturnType Parse(string command, out object result) {
			throw new NotImplementedException();
		}

		//public override ReturnType ParseType(string command) {
		//	throw new NotImplementedException();
		//}
	}
}
