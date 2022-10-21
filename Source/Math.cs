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
	[StaticConstructorOnStartup]
	class Math {
		// variables
		public static List<Pawn> pawns = new List<Pawn>();
		public static List<Pawn> colonists = new List<Pawn>();
		public static List<Pawn> prisoners = new List<Pawn>();
		public static List<Pawn> slaves = new List<Pawn>();
		public static List<Pawn> ownedAnimals = new List<Pawn>();
		public static float pawnsIntake = 0;
		public static float colonistsIntake = 0;
		public static float prisonersIntake = 0;
		public static float slavesIntake = 0;
		public static float ownedAnimalsIntake = 0;
		public static Regex v13_getIntake = new Regex(@"Final value: (\d+(?:.\d+)?)", RegexOptions.Compiled);

		static Math() {
			PerformPatches();
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

		/// <returns>True if sequence is valid.</returns>
		public static bool DoMath(string str, ref int val, BillComponent bc) {
			if (str.NullOrEmpty())
				return false;

			Expression e = new Expression(str);
			AddParameters(e, bc);
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
			val = (int)Convert.ChangeType(Convert.ChangeType(result, type), typeof(int));
			return true;
		}


		// TODO: Add support for amount_of_resource.
		public static void AddParameters(Expression e, BillComponent bc) {
			// TODO: Mech variable.
			// TODO: Cache these to improve performance
			// "Spawned" means that the thing isn't held in a container/held. Non spawned things are in a container.
			// TODO: Maybe redo this with a loop on pawns so there's only 1 call.
			pawns = bc.targetBill.Map.mapPawns.FreeColonistsAndPrisoners;
			slaves = bc.targetBill.Map.mapPawns.SlavesOfColonySpawned;
			colonists = bc.targetBill.Map.mapPawns.FreeColonists.Except(slaves).ToList();
			prisoners = bc.targetBill.Map.mapPawns.PrisonersOfColony;
			// stolen from MainTabWindow_Animals.Pawns :)
			ownedAnimals = bc.targetBill.Map.mapPawns.PawnsInFaction(Faction.OfPlayer).Where(p => p.RaceProps.Animal).ToList();

			pawnsIntake = CountIntake(pawns);
			colonistsIntake = CountIntake(colonists);
			slavesIntake = CountIntake(slaves);
			prisonersIntake = CountIntake(prisoners);
			ownedAnimalsIntake = CountIntake(ownedAnimals);

			e.Parameters["pwn"] = e.Parameters["pawns"] = pawns.Count();
			e.Parameters["col"] = e.Parameters["colonists"] = colonists.Count();
			e.Parameters["slv"] = e.Parameters["slaves"] = slaves.Count();
			e.Parameters["pri"] = e.Parameters["prisoners"] = prisoners.Count();
			e.Parameters["anim"] = e.Parameters["animals"] = ownedAnimals.Count();

			e.Parameters["pwn_in"] = e.Parameters["pawns_intake"] = pawnsIntake;
			e.Parameters["col_in"] = e.Parameters["colonists_intake"] = colonistsIntake;
			e.Parameters["slv_in"] = e.Parameters["slaves_intake"] = slavesIntake;
			e.Parameters["pri_in"] = e.Parameters["prisoners_intake"] = prisonersIntake;
			e.Parameters["anim_in"] = e.Parameters["animals_intake"] = ownedAnimalsIntake;
		}

		private static float CountIntake(List<Pawn> pawns) {
			float intake = 0;
			foreach (Pawn p in pawns) {
				// This whole thing feels absurd, but I don't know how else I'm meant to get the hunger rate.
				// I searched everywhere but it does seem like the stats menu is the only location it's displayed with all modifiers.
				try {
#if v1_3
					Match match = v13_getIntake.Match(RaceProperties.NutritionEatenPerDayExplanation(p, showCalculations: true));
					if (match.Success) {
						intake += float.Parse(match.Groups[1].Value);
					}
					//intake += float.Parse(RaceProperties.NutritionEatenPerDayExplanation(p, showCalculations: true));
#elif v1_4
					intake += float.Parse(RaceProperties.NutritionEatenPerDay(p));
#endif
				}
				catch (ArgumentNullException) {
					Log.Warning("Could not parse nutrition of pawn. Name: " + p.Name + ". Please let me (CrunchyDuck) know so I can fix it!");
				}
			}
			return intake;
		}
	}
}
