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
	// TODO: Add math variable name to the i menu of all objects.
	// TODO: Clothing rules/restriction variable.
	// TODO: Add support for other lanuages/redefine how variables are segmented.
	// TODO: unpause doesn't update properly. Confirm this?
	// TODO: Add pop up window for inputting larger bills.
	// TODO: Update "look everywhere" to ACTUALLY look everywhere, not just in stockpiles.
	// TODO: Market value slider.
	// TODO: Expanded view with the ability to make variables.
	// TODO: Copy and link bills together (BWM)
	// TODO: Drag to rearrange bills (BWM)
	// TODO: Set up translation files.
	// TODO: Change "Unpause field" to accept booleans rather than thresholds.
	[StaticConstructorOnStartup]
	class Math {
		// Cached variables
		private static Dictionary<Map, CachedMapData> cachedMaps = new Dictionary<Map, CachedMapData>();

		private static Regex parameterNames_old = new Regex(@"(\w+)", RegexOptions.Compiled);

		private static Regex parameterNames = new Regex("(?:(\")(.+?)(\"))|([a-zA-Z0-9]+)", RegexOptions.Compiled);
		public static Dictionary<string, ThingDef> searchableThings = new Dictionary<string, ThingDef>();
		public static Dictionary<string, ThingCategoryDef> searchableCategories = new Dictionary<string, ThingCategoryDef>();
		public static Dictionary<string, StatDef> searchableStats = new Dictionary<string, StatDef>();

		static Math() {
			PerformPatches();

			// I checked, this does run after all defs are loaded :)
			// Code taken from DebugThingPlaceHelper.TryPlaceOptionsForStackCount
			var thing_list = DefDatabase<ThingDef>.AllDefs;
			foreach (ThingDef thingDef in thing_list) {
				if (thingDef.label == null) {
					continue;
				}
				searchableThings[thingDef.label.ToParameter()] = thingDef;
			}

			var thing_list2 = DefDatabase<ThingCategoryDef>.AllDefs;
			foreach (ThingCategoryDef thingDef in thing_list2) {
				if (thingDef.label == null) {
					continue;
				}
				searchableCategories[thingDef.label.ToParameter()] = thingDef;
			}

			var thing_list3 = DefDatabase<StatDef>.AllDefs;
			foreach (StatDef def in thing_list3) {
				if (def.label == null) {
					continue;
				}
				searchableStats[def.label.ToParameter()] = def;
			}
		}

		private static void PerformPatches() {
			// What can I say, I prefer a manual method of patching.
			var harmony = new Harmony("CrunchyDuck.Math");
			AddPatch(harmony, typeof(PatchExposeData));
			AddPatch(harmony, typeof(DoConfigInterface_Patch));
			AddPatch(harmony, typeof(Bill_Production_Constructor_Patch));
			AddPatch(harmony, typeof(BillDetails_Patch));
			AddPatch(harmony, typeof(CountProducts_Patch));
			AddPatch(harmony, typeof(Bill_Production_DoConfigInterface_Patch));
			AddPatch(harmony, typeof(Patch_Bill_LabelCap));
		}

		private static void AddPatch(Harmony harmony, Type type) {
			// TODO: Sometime make a patch interface.
			var prefix = type.GetMethod("Prefix") != null ? new HarmonyMethod(type, "Prefix") : null;
			var postfix = type.GetMethod("Postfix") != null ? new HarmonyMethod(type, "Postfix") : null;
			var trans = type.GetMethod("Transpiler") != null ? new HarmonyMethod(type, "Transpiler") : null;
			harmony.Patch((MethodBase)type.GetMethod("Target").Invoke(null, null), prefix: prefix, postfix: postfix, transpiler: trans);
		}

		public static void ClearCacheMaps() {
			cachedMaps = new Dictionary<Map, CachedMapData>();
		}

		// TODO: remove str from variable here.
		// TODO: Remove val too?
		/// <returns>True if sequence is valid.</returns>
		public static bool DoMath(string str, ref int val, InputField field) {
			if (str.NullOrEmpty())
				return false;

			if (DoMath_new(str, ref val, field)) {
				return true;
			}
			return false;
		}

		public static bool DoMath_new(string str, ref int val, InputField field) {
			List<string> parameter_list = new List<string>();
			foreach (Match match in parameterNames.Matches(str)) {
				// Matched single word.
				if (match.Groups[4].Success) {
					parameter_list.Add(match.Groups[4].Value);
					continue;
				}

				// Reformat the user input to work for ncalc.
				// The reason I use "pawn" rather than [pawn] is for language compatibility.
				// My spanish friend complained that [ and ] aren't on his keyboard.
				// Spanish people aren't allowed to program.
				int i = match.Index;
				int i2 = match.Index + match.Length - 1;
				str = str.Remove(i, 1).Insert(i, "[");
				str = str.Remove(i2, 1).Insert(i2, "]");

				string str2 = match.Groups[2].Value;
				parameter_list.Add(str2);
			}
			Expression e = new Expression(str);
			AddParameters(e, field, parameter_list);
			// KNOWN BUG: `if` equations don't properly update. This is an ncalc issue - it evaluates the current path and ignores the other.
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

		public static CachedMapData GetCachedMap(Bill_Production bp) {
			if (bp == null)
				return null;
			return GetCachedMap(bp.Map);
		}

		public static CachedMapData GetCachedMap(Map map) {
			// I was able to get a null error by abandoning a base. This handles that.
			if (map == null)
				return null;
			if (!cachedMaps.ContainsKey(map)) {
				// Generate cache.
				cachedMaps[map] = new CachedMapData(map);
			}
			CachedMapData cache = cachedMaps[map];
			return cache;
		}

		public static void AddParameters(Expression e, InputField field, List<string> parameter_list) {
			CachedMapData cache = field.bc.Cache;
			if (cache == null) {
				BillManager.instance.RemoveBillComponent(field.bc);
				return;
			}

			foreach (string parameter in parameter_list) {
				float count;
				if (cache.SearchForResource(parameter, field.bc, out count)) {
					e.Parameters[parameter] = count;
				}
			}
		}
	}
}