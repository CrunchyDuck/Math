using RimWorld;
using HarmonyLib;
using System.Reflection;
using System.Collections.Generic;
using Verse;
using UnityEngine;
using NCalc;
using System;
using System.Linq;

namespace CrunchyDuck.Math {
	[StaticConstructorOnStartup]
	class Math {
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
			
			val = (int)Convert.ChangeType(result, type);
			return true;
		}


		// TODO: Add support for amount_of_resource.
		public static void AddParameters(Expression e, BillComponent bc) {
			// "Spawned" means that the thing isn't held in a container/held. Non spawned things are in a container.
			e.Parameters["col"] = e.Parameters["colonists"] = bc.targetBill.Map.mapPawns.FreeColonistsCount;
			e.Parameters["pri"] = e.Parameters["prisoners"] = bc.targetBill.Map.mapPawns.PrisonersOfColonyCount;
			e.Parameters["slv"] = e.Parameters["slaves"] = bc.targetBill.Map.mapPawns.SlavesOfColonySpawned.Count;
			e.Parameters["pwn"] = e.Parameters["pawns"] = bc.targetBill.Map.mapPawns.ColonistCount;
			//e.Parameters["anim"] = bc.targetBill.Map.mapPawns.SpawnedColonyAnimals;  // TODO: This doesn't work. 
		}
	}
}
