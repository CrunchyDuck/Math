using RimWorld;
using Verse;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System;

namespace CrunchyDuck.Math {
	//// Will move this somewhere more appropriate when I know what it'll be used for.
	//public class UserVariableContainer : IExposable {
	//	public List<>
	//	public void ExposeData() {
	//		throw new NotImplementedException();
	//	}
	//}

	public class UserVariable : IExposable {
		public string name = "nya";
		public string equation = "x * 10";

		public void ExposeData() {
			Scribe_Values.Look(ref name, "name");
			Scribe_Values.Look(ref equation, "equation");
		}
	}
}
