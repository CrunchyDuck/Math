﻿using System;
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace CrunchyDuck.Math.MathFilters {
	// BIG TODO: Describe how to handle naming collisions - e.g., if there are multiple "armored" categories, you can do "head.armored" or "body.armored"
	// For now, this isn't used. I don't have enough advanced logic with categories to justify an object - Instead I just cast the user's category into a ThingFilter.
	class CategoryFilter : MathFilter {
		public static HashSet<string> names = new HashSet<string> { "categories", "c" };
		public static Dictionary<string, ThingCategoryDef> searchableCategories = new Dictionary<string, ThingCategoryDef>();
		public override bool CanCount { get { return true; } }

		private ThingCategoryDef category;
		private BillComponent bc;

		public CategoryFilter(BillComponent bc, ThingCategoryDef category) {
			this.category = category;
			this.bc = bc;
		}

		public override float Count() {
			float count = 0;
			foreach (ThingDef cat_thingdef in category.childThingDefs) {
				foreach (Thing thing in bc.Cache.GetThings(cat_thingdef.label.ToParameter(), bc))
					count += thing.stackCount;
			}

			foreach (ThingCategoryDef catdef in category.childCategories) {
				foreach (ThingDef cat_thingdef in catdef.childThingDefs) {
					foreach (Thing thing in bc.Cache.GetThings(cat_thingdef.label.ToParameter(), bc))
						count += thing.stackCount;
				}
			}

			return count;
		}

		public override ReturnType Parse(string command, out object result) {
			throw new NotImplementedException();
		}

		//public override ReturnType ParseType(string command) {
		//	throw new NotImplementedException();
		//}
	}
}
