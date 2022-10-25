using RimWorld;
using HarmonyLib;
using System.Reflection;
using System.Collections.Generic;
using Verse;
using UnityEngine;
using NCalc;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace CrunchyDuck.Math {
	// TODO: Show decimal values in Currently Have, Repeat and Unpause At, but round the ultimate value.
	// TODO: Method to "resolve" a calculation, so it doesn't remember what you've typed in. This would be triggered by ctrl + enter
	// TODO: comment: variable for how much bandwidth your mechanitors have? so as they scale in bandwidth your mechanoid production could automatically scale
	// TODO: Add math variable name to the i menu of all objects.
	// TODO: Clothing restriction category.
	[StaticConstructorOnStartup]
	class Math {
		// Cached variables
		private static Dictionary<Map, CachedMapData> cachedMaps = new Dictionary<Map, CachedMapData>();
		private static Regex parameterNames = new Regex(@"(\w+)", RegexOptions.Compiled);
		public static Dictionary<string, ThingDef> searchableThings = new Dictionary<string, ThingDef>();
		public static Dictionary<string, ThingCategoryDef> searchabeCategories = new Dictionary<string, ThingCategoryDef>();
		public static Texture2D infoButtonImage = ContentFinder<Texture2D>.Get("yin_yang_kobold");

		static Math() {
			PerformPatches();

			// I checked, this does run after all defs are loaded :)
			// Code taken from DebugThingPlaceHelper.TryPlaceOptionsForStackCount
			var thing_list = DefDatabase<ThingDef>.AllDefs;
			foreach (ThingDef thingDef in thing_list) {
				if (thingDef.label == null) {
					continue;
				}
				string param_name = thingDef.label.ToParameter();
				searchableThings[param_name] = thingDef;
			}

			var thing_list2 = DefDatabase<ThingCategoryDef>.AllDefs;
			foreach (ThingCategoryDef thingDef in thing_list2) {
				if (thingDef.label == null) {
					continue;
				}
				string param_name = thingDef.label.ToCategory();
				searchabeCategories[param_name] = thingDef;
			}
		}

		private static void PerformPatches() {
			var harmony = new Harmony("CrunchyDuck.Math");
			AddPatch(harmony, typeof(DoConfigInterface_Patch));
			AddPatch(harmony, typeof(IntEntry_Patch));
			AddPatch(harmony, typeof(Bill_ProductionConstructor_Patch));
			AddPatch(harmony, typeof(PatchExposeData));
			AddPatch(harmony, typeof(SetInitialSizeAndPosition_Patch));
			AddPatch(harmony, typeof(Dialog_BillConfig_Patch));
			AddPatch(harmony, typeof(TextFieldNumeric_Patch));
		}

		private static void AddPatch(Harmony harmony, Type type) {
			var prefix = type.GetMethod("Prefix") != null ? new HarmonyMethod(type, "Prefix") : null;
			var postfix = type.GetMethod("Postfix") != null ? new HarmonyMethod(type, "Postfix") : null;
			var trans = type.GetMethod("Transpiler") != null ? new HarmonyMethod(type, "Transpiler") : null;
			harmony.Patch((MethodBase)type.GetMethod("Target").Invoke(null, null), prefix: prefix, postfix: postfix, transpiler: trans);
		}

		public static void ClearCacheMaps() {
			cachedMaps = new Dictionary<Map, CachedMapData>();
		}

		/// <returns>True if sequence is valid.</returns>
		public static bool DoMath(string str, ref int val, BillComponent bc) {
			if (str.NullOrEmpty())
				return false;

			Expression e = new Expression(str);
			List<string> parameter_list = new List<string>();
			foreach(Match match in parameterNames.Matches(str)) {
				parameter_list.Add(match.Groups[1].Value.ToParameter());
			}
			AddParameters(e, bc, parameter_list);
			if (e.HasErrors())
				return false;
			object result;
			try {
				result = e.Evaluate();
			}
			// For some reason, HasErrors() doesn't check if parameters are valid.
			catch (ArgumentException) {
				return false;
			}

			Type type = result.GetType();
			Type[] accepted_types = new Type[] { typeof(int), typeof(decimal), typeof(double), typeof(float) };
			if (!accepted_types.Contains(type))
				return false;

			// this is dumb but necessary
			try {
				val = (int)Convert.ChangeType(Convert.ChangeType(result, type), typeof(int));
			}
			// Divide by 0, mostly.
			catch (OverflowException) {
				val = 999999;
			}
			return true;
		}

		public static CachedMapData GetCachedMap(Map map) {
			if (!cachedMaps.ContainsKey(map)) {
				// Generate cache.
				cachedMaps[map] = new CachedMapData(map);
			}
			CachedMapData cache = cachedMaps[map];
			return cache;
		}

		// TODO: Add groups of resources, such as "Meals"
		public static void AddParameters(Expression e, BillComponent bc, List<string> parameter_list) {
			// TODO: Mech variable.
			// "Spawned" means that the thing isn't held in a container/held. Non spawned things are in a container.
			// TODO: Maybe redo this with a loop on pawns so there's only 1 call.
			CachedMapData cache = bc.Cache;

			e.Parameters["pwn"] = e.Parameters["pawns"] = cache.pawns.Count();
			e.Parameters["col"] = e.Parameters["colonists"] = cache.colonists.Count();
			e.Parameters["slv"] = e.Parameters["slaves"] = cache.slaves.Count();
			e.Parameters["pri"] = e.Parameters["prisoners"] = cache.prisoners.Count();
			e.Parameters["anim"] = e.Parameters["animals"] = cache.ownedAnimals.Count();

			e.Parameters["pwn_in"] = e.Parameters["pawns_intake"] = cache.pawnsIntake;
			e.Parameters["col_in"] = e.Parameters["colonists_intake"] = cache.colonistsIntake;
			e.Parameters["slv_in"] = e.Parameters["slaves_intake"] = cache.slavesIntake;
			e.Parameters["pri_in"] = e.Parameters["prisoners_intake"] = cache.prisonersIntake;
			e.Parameters["anim_in"] = e.Parameters["animals_intake"] = cache.ownedAnimalsIntake;

#if v1_4
			e.Parameters["bab"] = e.Parameters["babies"] = cache.babies.Count();
			e.Parameters["kid"] = e.Parameters["kids"] = cache.kids.Count();
			e.Parameters["kid_in"] = e.Parameters["kids_intake"] = cache.kidsIntake;
			e.Parameters["bab_in"] = e.Parameters["babies_intake"] = cache.babiesIntake;
#endif

			// TODO: Add more searching modifiers, such as the nutritional value of foods.
			foreach (string parameter in parameter_list) {
				int count;
				if (cache.SearchForResource(parameter, bc, out count)) {
					e.Parameters[parameter] = count;
				}
			}
		}
	}
}