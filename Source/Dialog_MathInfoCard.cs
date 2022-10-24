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
	class Dialog_MathInfoCard : Window {
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
#if v1_4
		public static FieldInfo statsCacheValues = AccessTools.Field(typeof(StatsReportUtility), "cachedEntryValues");
#endif
		public override Vector2 InitialSize => new Vector2(950f, 760f);
		protected override float Margin => 0.0f;
		// TODO: Add this in.
		//public override QuickSearchWidget CommonSearchWidget => this.tab != Dialog_InfoCard.InfoCardTab.Stats ? (QuickSearchWidget)null : StatsReportUtility.QuickSearchWidget;

		public Dialog_MathInfoCard(BillComponent bill) {
			attachedBill = bill;
			statEntries = GetStatEntries();
			// If these values aren't reset you get some corruption nonsense because the system is jank.
#if v1_4
			statsCacheValues.SetValue(null, new List<string>());
#endif
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
			StatDrawEntry stat;

			var cat = catBasics;
			stat = new StatDrawEntry(cat, "description", "",
				"This menu provides a big reference for the Math mod, its functions and variables, and some examples of what you can do with them.\n\nThe left column is the variable name, the right column is the current value.\n\nClick on a row to get an explanation.",
				10000);
			stats.Add(stat);
			stats.Add(new StatDrawEntry(cat, "Searching ThingDefs", "",
@"You can search any ThingDef in the game such as slate_blocks, ostriches, compacted_steel, etc.
The count you get back will not include forbidden items, and will take into account the recipe's settings. E.G. if an item has 40% hitpoints but recipe requires 50%, it won't be counted.

To search a ThingDef you need to convert the in-game name into a valid parameter name. This is simple enough to do though!

First, if it's a crafted/built item, ignore the material/quality of the item. For stacks, ignore the quantity.
Then, make it all lowercase.
Finally, replace spaces and special characters like - or ' with underscores.

Let's look at some examples:
Compacted steel -> compacted_steel
Medicine x5 -> medicine
Go-juice -> [u]go_juice[/u]
Uranium mace (legendary) -> mace", 2999));
			stats.Add(new StatDrawEntry(cat, "Searching categories", "", 
@"You can search any category of things in game, such as c_raw_food, c_textiles, c_meals, etc. Categories can be seen in any stockpile or bill menu.

Just like ThingDefs, you need to convert the category name into a parameter name, but for categories you also need to prepend the name with c_
Here are some examples:
Meals -> c_meals
Mortar shells -> c_mortar_shells
Humanlike corpses -> c_humanlike_corpses", 2998));

			// !!! BIG WARNING !!!
			// the label/second variable in these all have a zero-width space placed at the start.
			// This is done to force the variables to be lowercase in menus.
			// nice thinking oken
			// BEWARE THE HIDDEN HORRORS.
			cat = catExamples;
			stats.Add(new StatDrawEntry(cat, "​pawns_intake * 5", "", "Calculates how much nutrition your pawns need for 5 days.\nA simple meal provides 0.9 nutrition, so this roughly gives you how many simple meals you'll need to cook to have 5 days of food.", 3001));
			stats.Add(new StatDrawEntry(cat, "​col * 2", "", "Create 2 of something for each pawn that you have. Good for medicine, clothing, weapons, etc.", 3000));
			stats.Add(new StatDrawEntry(cat, "​if(slate_blocks > 200, 50, 0)", "", "Check if we have more than 200 slate blocks. If we do, produce up to 50 of this thing. If not, produce 0 of this thing.", 2999));
			stats.Add(new StatDrawEntry(cat, "​if(c_meals < 50, 50, 0)", "", "Produce this thing only if we have less than 50 different 'meals' - simple, packaged, fine, etc.\nVery useful for producing simple meals instead of fine meals if the resources aren't available.", 2998));

			//cat = catFunctions;
			//stats.Add(new StatDrawEntry(cat, "if statements", "", "Example:\nif(fine_meal > 10, 10, 0)\n\n", 2999));

			cat = catPawns;
			stats.Add(new StatDrawEntry(cat, "​pawns", attachedBill.Cache.pawns.Count().ToString(), "Alias: pwn\nNumber of owned pawns on the map the bill is contained in.", 3001));
			stats.Add(new StatDrawEntry(cat, "​colonists", attachedBill.Cache.colonists.Count().ToString(), "Alias: col\nNumber of colonists on the map the bill is contained in. Does not include prisoners or slaves.", 3000));
			stats.Add(new StatDrawEntry(cat, "​slaves", attachedBill.Cache.slaves.Count().ToString(), "Alias: slv\nNumber of owned slaves on the map the bill is contained in.", 2999));
			stats.Add(new StatDrawEntry(cat, "​prisoners", attachedBill.Cache.prisoners.Count().ToString(), "Alias: pri\nNumber of owned prisoners on the map the bill is contained in.", 2998));
			stats.Add(new StatDrawEntry(cat, "​animals", attachedBill.Cache.ownedAnimals.Count().ToString(), "Alias: anim\nNumber of owned animals on the map the bill is contained in.", 2997));
#if v1_4
			stats.Add(new StatDrawEntry(cat, "​babies", attachedBill.Cache.babies.Count().ToString(), "Alias: bab\nNumber of owned babies on the map the bill is contained in.", 2996));
			stats.Add(new StatDrawEntry(cat, "​kids", attachedBill.Cache.kids.Count().ToString(), "Alias: kid\nNumber of owned children on the map the bill is contained in.", 2995));
#endif

			cat = catModifiers;
			stats.Add(new StatDrawEntry(cat, "​pawns_intake", attachedBill.Cache.pawnsIntake.ToString(), "Alias: pwn_in\nThe _in modifier can be used on any group of pawns like slv_in, anim_in, etc. It returns the amount of nutrition a pawn requires per day after all modifiers have been applied.\n\nThis number can also be seen in a pawn's info card under \"Food consumption\".", 2995));

			return stats;
		}
	}
}
