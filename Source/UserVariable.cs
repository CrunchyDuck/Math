using RimWorld;
using Verse;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System;

namespace CrunchyDuck.Math {
	// Will move this somewhere more appropriate when I know what it'll be used for.
	//public class UserVariableContainer : IExposable {
	//	public List<UserVariable> variables = new List<UserVariable>();
	//	public void ExposeData() {
	//		Scribe_Collections.Look(variables);
	//	}
	//}

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
