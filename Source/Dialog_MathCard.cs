using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace CrunchyDuck.Math {
	// oh my god the vanilla game dialog_infocard is programmed so poorly
	// the interface is so simple, yet for some reason instead of abstracting it down,
	// they have a shit load of different modes for different types of defs
	// it is some of the most poorly thought out code i've seen in a while
	// TODO: Mess with StatDrawEntry.LabelCap to stop it capitalizing variable names.
	class Dialog_MathCard : Window {
		public List<StatDrawEntry> statEntries;
		public BillComponent attachedBill;
		public StatCategoryDef catBasics = DefDatabase<StatCategoryDef>.AllDefs.First(scd => scd.defName == "CDBasics");
		public StatCategoryDef catPawns = DefDatabase<StatCategoryDef>.AllDefs.First(scd => scd.defName == "CDPawns");
		public StatCategoryDef catModifiers = DefDatabase<StatCategoryDef>.AllDefs.First(scd => scd.defName == "CDModifiers");
		public StatCategoryDef catExamples = DefDatabase<StatCategoryDef>.AllDefs.First(scd => scd.defName == "CDExamples");
		public StatCategoryDef catFunctions = DefDatabase<StatCategoryDef>.AllDefs.First(scd => scd.defName == "CDFunctions");

		public static MethodInfo StatsWorker = AccessTools.Method(typeof(StatsReportUtility), "DrawStatsWorker");
		public static MethodInfo StatsFinalize = AccessTools.Method(typeof(StatsReportUtility), "FinalizeCachedDrawEntries");
		public static FieldInfo statsCache = AccessTools.Field(typeof(StatsReportUtility), "cachedDrawEntries");
		public static FieldInfo statsCacheValues = AccessTools.Field(typeof(StatsReportUtility), "cachedEntryValues");

		public override Vector2 InitialSize => new Vector2(950f, 760f);
		protected override float Margin => 0.0f;
		// TODO: Add this in.
		//public override QuickSearchWidget CommonSearchWidget => this.tab != Dialog_InfoCard.InfoCardTab.Stats ? (QuickSearchWidget)null : StatsReportUtility.QuickSearchWidget;

		public Dialog_MathCard(BillComponent bill) {
			attachedBill = bill;
			statEntries = GetStatEntries();
			// If these values aren't reset you get some corruption nonsense because the system is jank.
			statsCacheValues.SetValue(null, new List<string>());
		}

		public override void Close(bool doCloseSound = true) {
			base.Close(doCloseSound);
		}

		public override void DoWindowContents(Rect inRect) {
			statEntries = GetStatEntries();
			statsCache.SetValue(null, statEntries);
			StatsFinalize.Invoke(null, new object[] { statsCache.GetValue(null) });

			//Rect rect1 = new Rect(inRect).ContractedBy(18f) with
			//{
			//	height = 34f
			//};

			Rect rect1 = new Rect(inRect).ContractedBy(18f);
			rect1.height = 34f;
			rect1.x += 34f;
			Text.Font = GameFont.Medium;
			Widgets.Label(rect1, "Math");
			Rect rect2 = new Rect(inRect.x + 9f, rect1.y, 34f, 34f);
			Widgets.ButtonImage(rect2, Math.infoButtonImage, GUI.color);
			//if (this.thing != null)
			//	Widgets.ThingIcon(rect2, this.thing);
			//else
			//	Widgets.DefIcon(rect2, this.def, this.stuff, drawPlaceholder: true);
			Rect rect3 = new Rect(inRect);
			rect3.x += 18;  // Mine was weirdly offset and I'm not sure why. This is approximately right.
			rect3.yMin = rect1.yMax + 18;
			rect3.yMax -= 38f;
			StatsWorker.Invoke(null, new object[] { rect3, null, null });
		}

		private List<StatDrawEntry> GetStatEntries() {
			var stats = new List<StatDrawEntry>();

			//e.Parameters["pwn"] = e.Parameters["pawns"] = cache.pawns.Count();
			//e.Parameters["col"] = e.Parameters["colonists"] = cache.colonists.Count();
			//e.Parameters["slv"] = e.Parameters["slaves"] = cache.slaves.Count();
			//e.Parameters["pri"] = e.Parameters["prisoners"] = cache.prisoners.Count();
			//e.Parameters["anim"] = e.Parameters["animals"] = cache.ownedAnimals.Count();

			//e.Parameters["pwn_in"] = e.Parameters["pawns_intake"] = cache.pawnsIntake;
			//e.Parameters["col_in"] = e.Parameters["colonists_intake"] = cache.colonistsIntake;
			//e.Parameters["slv_in"] = e.Parameters["slaves_intake"] = cache.slavesIntake;
			//e.Parameters["pri_in"] = e.Parameters["prisoners_intake"] = cache.prisonersIntake;
			//e.Parameters["anim_in"] = e.Parameters["animals_intake"] = cache.ownedAnimalsIntake;

			StatDrawEntry stat;
			var cat = catBasics;

			stat = new StatDrawEntry(cat, "description", "",
				"This menu provides a big reference for the Math mod, its functions and variables, and some examples of what you can do with them.\n\nThe left column is the variable name, the right column is the current value.\n\nClick on a row to get an explanation.",
				10000);
			stats.Add(stat);

			// !!! BIG WARNING !!!
			// the label/second variable in these all have a zero-width space placed at the start.
			// This is done to force the variables to be lowercase in menus.
			// nice thinking oken
			// BEWARE THE HIDDEN HORRORS.
			cat = catExamples;
			stats.Add(new StatDrawEntry(cat, "​pawns_intake * 5", "", "Calculates how much nutrition your pawns need for 5 days.\nA simple meal provides 0.9 nutrition, so this roughly gives you how many simple meals you'll need to cook to have 5 days of food.", 3001));
			stats.Add(new StatDrawEntry(cat, "​col * 2", "", "Create 2 of something for each pawn that you have. Good for medicine, clothing, weapons, etc.", 3000));
			stats.Add(new StatDrawEntry(cat, "​if(slate_blocks > 200, 50, 0)", "", "Check if we have more than 200 slate blocks. If we do, produce up to 50 of this thing. If not, produce 0 of this thing.", 2999));
			// stats.Add(new StatDrawEntry(cat, "​if(meat > 200, 50, 0)", "", "Kibble production", 2998));

			//cat = catFunctions;
			//stats.Add(new StatDrawEntry(cat, "if statements", "", "Example:\nif(fine_meal > 10, 10, 0)\n\n", 2999));


			cat = catPawns;
			stats.Add(new StatDrawEntry(cat, "​pawns", attachedBill.Cache.pawns.Count().ToString(), "Alias: pwn\nNumber of owned pawns on the map.", 3001));
			stats.Add(new StatDrawEntry(cat, "​colonists", attachedBill.Cache.colonists.Count().ToString(), "Alias: col\nNumber of colonists on the map. Does not include prisoners or slaves.", 3000));
			stats.Add(new StatDrawEntry(cat, "​slaves", attachedBill.Cache.slaves.Count().ToString(), "Alias: slv\nNumber of owned slaves on the map.", 2999));
			stats.Add(new StatDrawEntry(cat, "​prisoners", attachedBill.Cache.prisoners.Count().ToString(), "Alias: pri\nNumber of owned prisoners on the map.", 2998));
			stats.Add(new StatDrawEntry(cat, "​animals", attachedBill.Cache.ownedAnimals.Count().ToString(), "Alias: anim\nNumber of owned animals on the map.", 2997));
#if v1_4
			stats.Add(new StatDrawEntry(cat, "​babies", attachedBill.Cache.babies.Count().ToString(), "Alias: bab\nNumber of owned babies on the map.", 2996));
			stats.Add(new StatDrawEntry(cat, "​kids", attachedBill.Cache.kids.Count().ToString(), "Alias: kid\nNumber of owned children on the map.", 2995));
#endif

			cat = catModifiers;
			stats.Add(new StatDrawEntry(cat, "​pawns_intake", attachedBill.Cache.pawnsIntake.ToString(), "Alias: pwn_in\nThe _in modifier can be used on any group of pawns like slv_in, pri_in, etc. It returns the amount of nutrition a pawn requires per day after all modifiers have been applied.\n\nThis number can also be seen in a pawn's info card under \"Food consumption\".", 2995));


			return stats;
		}
	}
}
