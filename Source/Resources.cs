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
	[StaticConstructorOnStartup]
	class Resources {
		public static readonly Texture2D infoButtonImage = ContentFinder<Texture2D>.Get("yin_yang_kobold");
		public static readonly Texture2D variablesButtonImage = ContentFinder<Texture2D>.Get("variables_button");
		public static readonly Texture2D bestStockpileImage = ContentFinder<Texture2D>.Get("BWM_BestStockpile");
		public static readonly Texture2D dropOnFloorImage = ContentFinder<Texture2D>.Get("BWM_DropOnFloor");
		public static readonly Texture2D DragHash = ContentFinder<Texture2D>.Get("UI/Buttons/DragHash");

		public Resources() {}
	}
}
