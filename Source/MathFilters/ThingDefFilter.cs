using System;
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace CrunchyDuck.Math.MathFilters {
	class ThingDefFilter : MathFilter {
		public static HashSet<string> names = new HashSet<string> { "prefab", "p", "thingdef", "t" };
		public HashSet<ThingDef> contains = new HashSet<ThingDef>();
		Dictionary<string, Func<object>> filterMethods = new Dictionary<string, Func<object>>() {

		};
		public static Dictionary<string, Func<ThingDef, float>> counterMethods = new Dictionary<string, Func<ThingDef, float>>() {
			//{ "stack limit", t => t.def.stackLimit }
		};

		public override bool CanCount { get { return true; } }

		public ThingDefFilter(ThingDef td) {
			contains = new HashSet<ThingDef>() { td };
		}

		public ThingDefFilter(HashSet<ThingDef> thingdefs) {
			contains = thingdefs;
		}

		public ThingDefFilter(BillComponent bc, ThingCategoryDef category) {
			foreach (ThingDef cat_thingdef in category.childThingDefs) {
				contains.Add(Math.searchableThings[cat_thingdef.label.ToParameter()]);
			}

			foreach (ThingCategoryDef catdef in category.childCategories) {
				foreach (ThingDef cat_thingdef in catdef.childThingDefs) {
					contains.Add(Math.searchableThings[cat_thingdef.label.ToParameter()]);
				}
			}
		}

		public override float Count() {
			return contains.Count;
		}

		public override ReturnType Parse(string command, out object result) {
			result = null;
			if (filterMethods.ContainsKey(command)) {
				filterMethods[command].Invoke();
				result = this;
				return ReturnType.ThingFilter;
			}
			if (counterMethods.TryGetValue(command, out var method)) {
				float count = 0;
				foreach (ThingDef thing in contains) {
					count += method.Invoke(thing);
				}
				result = count;
				return ReturnType.Count;
			}

			return ReturnType.Null;
		}
	}
}
