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
	// TODO: Update mod page to show off using pawn intake to plan food.
	// TODO: Change default bill type to "do until you have x"
	// TODO: Method to "resolve" a calculation, so it doesn't remember what you've typed in. This would be triggered by ctrl + enter
	[StaticConstructorOnStartup]
	class Math {
		// Cached variables
		private static Dictionary<Map, CachedMapData> cachedMaps = new Dictionary<Map, CachedMapData>();
		private static Regex parameterNames = new Regex(@"(\w+)", RegexOptions.Compiled);
		public static Dictionary<string, ThingDef> searchabeThings = new Dictionary<string, ThingDef>();

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
				searchabeThings[param_name] = thingDef;
			}
		}

		private static void PerformPatches() {
			// I'll be honest, I couldn't figure out how to use annotations/attributes when patching a private/protected method.
			// I already knew how to do manual patching from OwO Stawdew Vawwey, so I just did that.
			// Read this but couldn't get it to work for me. https://github.com/pardeike/Harmony/issues/121
			// If you know, do tell me.
			var harmony = new Harmony("CrunchyDuck.Math");
			HarmonyMethod prefix;
			HarmonyMethod postfix;

			prefix = new HarmonyMethod(typeof(PatchTextFieldNumeric), "Prefix");  // Might be a nicer way to do this than using a string.
			postfix = null; // new HarmonyMethod(typeof(PatchNumericTextField), "Postfix");
			harmony.Patch(PatchTextFieldNumeric.Target(), prefix: prefix, postfix: postfix);

			prefix = new HarmonyMethod(typeof(PatchDoWindowContents), "Prefix");
			postfix = new HarmonyMethod(typeof(PatchDoWindowContents), "Postfix");
			harmony.Patch(PatchDoWindowContents.Target(), prefix: prefix, postfix: postfix);

			prefix = null; // new HarmonyMethod(typeof(PatchExposeData), "Prefix");
			postfix = new HarmonyMethod(typeof(PatchExposeData), "Postfix");
			harmony.Patch(PatchExposeData.Target(), prefix: prefix, postfix: postfix);

			prefix = null; // new HarmonyMethod(typeof(PatchBill_Production), "Prefix");
			postfix = new HarmonyMethod(typeof(PatchBill_ProductionConstructor), "Postfix");
			harmony.Patch(PatchBill_ProductionConstructor.Target(), prefix: prefix, postfix: postfix);

			//prefix = null; // new HarmonyMethod(typeof(PatchBill_Production), "Prefix");
			//postfix = new HarmonyMethod(typeof(PatchBillCloning), "Postfix1");
			//harmony.Patch(PatchBillCloning.Target1(), prefix: prefix, postfix: postfix);

			//prefix = null; // new HarmonyMethod(typeof(PatchBill_Production), "Prefix");
			//postfix = new HarmonyMethod(typeof(PatchBillCloning), "Postfix2");
			//harmony.Patch(PatchBillCloning.Target2(), prefix: prefix, postfix: postfix);
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

		// TODO: Add groups of resources, such as "Meals"
		public static void AddParameters(Expression e, BillComponent bc, List<string> parameter_list) {
			// TODO: Mech variable.
			// TODO: Cache these to improve performance
			// "Spawned" means that the thing isn't held in a container/held. Non spawned things are in a container.
			// TODO: Maybe redo this with a loop on pawns so there's only 1 call.
			Map map = bc.targetBill.Map;
			if (!cachedMaps.ContainsKey(map)) {
				// Generate cache.
				cachedMaps[map] = new CachedMapData(map);
			}
			CachedMapData cache = cachedMaps[map];

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

			foreach (string parameter in parameter_list) {
				if (e.Parameters.ContainsKey(parameter))
					continue;
				if (!searchabeThings.ContainsKey(parameter))
					continue;

				e.Parameters[parameter] = cache.SearchForResource(parameter);
			}
		}
	}
}
