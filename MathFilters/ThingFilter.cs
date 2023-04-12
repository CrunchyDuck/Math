using System;
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace CrunchyDuck.Math.MathFilters {
	class ThingFilter : MathFilter {
		public List<Thing> contains = new List<Thing>();
		Dictionary<string, Func<object>> filterMethods = new Dictionary<string, Func<object>>() {
			
		};
		public static Dictionary<string, Func<Thing, float>> counterMethods = new Dictionary<string, Func<Thing, float>>() {
			//{ "stack limit", t => t.def.stackLimit }
			{"stacks", GetStacks}
		};

		public override bool CanCount { get { return true; } }

		public ThingFilter(BillComponent bc, string thing_name) {
			contains = bc.Cache.GetThings(thing_name, bc);
		}

		public ThingFilter(BillComponent bc, ThingCategoryDef category) {
			foreach (ThingDef cat_thingdef in category.childThingDefs) {
				contains.AddRange(bc.Cache.GetThings(cat_thingdef.label.ToParameter(), bc));
			}

			foreach (ThingCategoryDef catdef in category.childCategories) {
				foreach (ThingDef cat_thingdef in catdef.childThingDefs) {
					contains.AddRange(bc.Cache.GetThings(cat_thingdef.label.ToParameter(), bc));
				}
			}
		}

		public override float Count() {
			float count = 0;
			foreach (Thing thing in contains) {
				count += thing.stackCount;
			}
			return count;
		}
		
		// This function will assume that every Thing in this ThingFilter is the same ThingDef.
		public static float GetStacks(Thing thing) {
			return ((float) thing.stackCount) / thing.def.stackLimit;
		}

		public override ReturnType Parse(string command, out object result) {
			result = null;
			//if (command == "prefab") {
			//	HashSet<ThingDef> tds = new HashSet<ThingDef>();
			//	foreach(Thing thing in contains) {
			//		tds.Add(thing.def);
			//	}
			//	result = new ThingDefFilter(tds);
			//	return ReturnType.ThingDefFilter;
			//}
			if (filterMethods.ContainsKey(command)) {
				filterMethods[command].Invoke();
				result = this;
				return ReturnType.ThingFilter;
			}
			if (counterMethods.TryGetValue(command, out var method)) {
				float count = 0;
				foreach (Thing thing in contains) {
					count += method.Invoke(thing);
				}
				result = count;
				return ReturnType.Count;
			}

			return ReturnType.Null;
		}
	}
}
