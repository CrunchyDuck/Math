using RimWorld;
using Verse;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System;

namespace CrunchyDuck.Math {
	// This is its own object to make it easier to expand in the future.
	// I'm not certain *how* I want to expand it yet, but I probably will.
	public class UserVariable : IExposable {
		public string name = "variable name";
		public string equation = "";

		public UserVariable(string name, string equation) {
			this.name = name;
			this.equation = equation;
		}

		public UserVariable() {}

		public void ExposeData() {
			Scribe_Values.Look(ref name, "name");
			Scribe_Values.Look(ref equation, "equation");
		}
	}
}
