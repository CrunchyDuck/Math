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
		public StatCategoryDef catIntroduction = DefDatabase<StatCategoryDef>.AllDefs.First(scd => scd.defName == "CDIntroduction");
		public StatCategoryDef catPawns = DefDatabase<StatCategoryDef>.AllDefs.First(scd => scd.defName == "CDPawns");
		public StatCategoryDef catModifiers = DefDatabase<StatCategoryDef>.AllDefs.First(scd => scd.defName == "CDModifiers");
		public StatCategoryDef catExamples = DefDatabase<StatCategoryDef>.AllDefs.First(scd => scd.defName == "CDExamples");
		public StatCategoryDef catFunctions = DefDatabase<StatCategoryDef>.AllDefs.First(scd => scd.defName == "CDFunctions");
		public StatCategoryDef catBasics = DefDatabase<StatCategoryDef>.AllDefs.First(scd => scd.defName == "CDBasics");

		public static MethodInfo StatsWorker = AccessTools.Method(typeof(StatsReportUtility), "DrawStatsWorker");
		public static MethodInfo StatsFinalize = AccessTools.Method(typeof(StatsReportUtility), "FinalizeCachedDrawEntries");
		public static FieldInfo statsCache = AccessTools.Field(typeof(StatsReportUtility), "cachedDrawEntries");
#if v1_4
		public static FieldInfo statsCacheValues = AccessTools.Field(typeof(StatsReportUtility), "cachedEntryValues");
#endif
		public override Vector2 InitialSize => new Vector2(950f, 760f);
		protected override float Margin => 0.0f;
		private InfoCardTab tab;

		// TODO BUG: Using search bar disables scroll.
		public override QuickSearchWidget CommonSearchWidget => StatsReportUtility.QuickSearchWidget;
		public override void Notify_CommonSearchChanged() => StatsReportUtility.Notify_QuickSearchChanged();

		// TODO: Add X in top right.
		public Dialog_MathInfoCard(BillComponent bill) {
			attachedBill = bill;
			statEntries = GetBasicEntries();
			// If these values aren't reset you get some corruption nonsense because the system is jank.
#if v1_4
			statsCacheValues.SetValue(null, new List<string>());
#endif
			tab = InfoCardTab.Basic;
		}

		public override void Close(bool doCloseSound = true) {
			base.Close(doCloseSound);
		}

		public override void DoWindowContents(Rect inRect) {
			List<TabRecord> tabs = new List<TabRecord>();
			tabs.Add(new TabRecord("Basic", () => tab = InfoCardTab.Basic, tab == InfoCardTab.Basic));
			tabs.Add(new TabRecord("StatDefs", () => tab = InfoCardTab.StatDefs, tab == InfoCardTab.StatDefs));

			Rect label_area = new Rect(inRect);
			label_area = label_area.ContractedBy(18f);
			label_area.height = 34f;

			//draw_area.height = 34f;
			label_area.x += 34f;
			Text.Font = GameFont.Medium;
			Widgets.Label(label_area, "Math");

			Rect stats_area = new Rect(inRect);
			stats_area.yMin = label_area.yMax + 40f;
			stats_area.yMax += -38f;

			TabDrawer.DrawTabs(stats_area, tabs);

			// By default you need to pass in Defs to get Window to show entries. This gets around that.
			if (tab == InfoCardTab.Basic)
				statEntries = GetBasicEntries();
			else if (tab == InfoCardTab.StatDefs)
				statEntries = GetStatDefEntries();
			statsCacheValues.SetValue(null, new List<string>());
			statsCache.SetValue(null, statEntries);
			StatsFinalize.Invoke(null, new object[] { statsCache.GetValue(null) });

			//Rect rect1 = new Rect(inRect).ContractedBy(18f) with
			//{
			//	height = 34f
			//};

			// Draw image in top left.
			Rect card_image = new Rect(inRect.x + 9f, label_area.y, 34f, 34f);
			Widgets.ButtonImage(card_image, Resources.infoButtonImage, GUI.color);
			//if (this.thing != null)
			//	Widgets.ThingIcon(rect2, this.thing);
			//else
			//	Widgets.DefIcon(rect2, this.def, this.stuff, drawPlaceholder: true);

			StatsWorker.Invoke(null, new object[] { stats_area.ContractedBy(18f), null, null });
		}

		private List<StatDrawEntry> GetBasicEntries() {
			var stats = new List<StatDrawEntry>();
			StatDrawEntry stat;

			var cat = catIntroduction;
			stat = new StatDrawEntry(cat, "Description", "",
				@"This menu provides a reference for the Math! mod, its functions and variables, and some examples of what you can do with them.

The left column is the variable name, the right column is the current value.

Click on a row to get an explanation.",
				10000);
			stats.Add(stat);
			stats.Add(new StatDrawEntry(cat, "Old variable system", "", "Variables used to be input like \"col_in\", \"c_meals\", \"c_eggs__unfert__\". But this system was\n1. ugly\n2. Puts the burden on the user and\n3. kept breaking.\n\nInstead, I've switched to using \"variable\", which is much easier to parse, prettier, and also supports multiple langauges.\n\nEquations using the old method will appear purple to notify that it should be changed to the new version, as it will eventually be removed.", 3000));

			stats.Add(new StatDrawEntry(cat, @"Using ""item count"" and ""target value""", "",
@"You'll notice that there's a new checkbox on ""Do until you have X"" bills called ""Custom item count"".
Turning this on, you can customize what the recipe counts to check how much of something it has.
For example, if you type in ""category meals"", it will count ""simple meal"", ""fine meal"", ""packaged survival meal"", etc.

Leaving ""Custom item count"" off will use the default counting logic.", 2990));

			stats.Add(new StatDrawEntry(cat, "Searching Things", "",
@"You can search any Thing in the game such as ""slate blocks"", ostriches, ""compacted steel"", etc.
The count you get back will not include forbidden items, and will take into account the recipe's settings. E.G. if an item has 40% hitpoints but recipe requires 50%, it won't be counted.

To search a ThingDef you need to convert the in-game name, make it lowercase, and wrap it in speech marks.
Item quality, material and stack count should also be omitted from the name.
If it has speech marks or full stops in its name, replace them with an underscore: _
Singular words like ostrich don't always need speechmarks - it's up to you.

Let's look at some examples:
Compacted steel -> ""compacted steel""
Medicine x5 -> ""medicine""
Go-juice -> ""go-juice""
Uranium mace (legendary) -> mace", 2980));
			stats.Add(new StatDrawEntry(cat, "Searching Thing Stats", "",
@"Things have stats like ""market value"", ""nutrition"", ""mech bandwidth"". This mod supports two types of stat searching.

The first type lets you search the stat of a Thing's prefab. This value is always the same, it doesn't depend on how many of that item you have or the quality of those items - It's the value those items normally have.

The second type lets you search the stats of individual items you own. This search is performed by putting ""owned"", ""own"" or ""o"" before the name of the StatDef.

Some examples of both:
""kibble.nutrition"" will give you 0.05, the nutritional value of one piece of kibble.
""kibble.owned nutrition"" will give you the total nutrition you have in stored kibble.

This also works on categories, and will add up the result from every item in that category.
""category meals.o nutrition"" will return how much nutrition you have in stored meals.
""category meals.nutrition"" is pretty useless, but shows it's possible.
", 2975));

			stats.Add(new StatDrawEntry(cat, "Searching categories", "",
@"You can search any category of things in game, such as ""category raw food"", ""cat textiles"", ""c meals"", etc. Categories can be seen in any stockpile or bill menu.

Categories are written just like Things, except you start the variable with the word ""category"", or ""cat"", or simply ""c"".
Here are some examples:
Humanlike corpses -> ""category humanlike corpses""
Meals -> ""c meals""
Eggs (unfert.) -> ""cat eggs (unfert_)""", 2970));

			stats.Add(new StatDrawEntry(cat, "Contributing variables", "",
@"There are a lot of things that could be added to this mod. There are many things you might want to count or search or do that I haven't put support in for.
It's not because adding variables is particularly hard, most of it is handled by nice systems nowadays. It's just that if I tried to add every variable I could think of, I would not have enough time in the day to do anything.
I'm trying to find a unified way to add large groups of variable searching - like StatDef searching. But it takes time and experience and sometimes just isn't possible.

If you want something added to the mod and if you know C#, I'm more than happy to accept contributions. You should discuss it with me in my Discord (Link on Math!'s workshop page) so I can help you with understand my codebase, make sure the contribution is one we can agree on, and so I can give some advice if necessary.
", 2960));

			// !!! BIG WARNING !!!
			// the label/second variable in these all have a zero-width space placed at the start.
			// This is done to force the variables to be lowercase in menus.
			// nice thinking oken
			// BEWARE THE HIDDEN HORRORS.
			// TODO: Fill these in with their current values.
			cat = catExamples;
			stats.Add(new StatDrawEntry(cat, "​\"pawns.intake\" * 5", "", "Calculates how much nutrition your pawns need for 5 days.\nA simple meal provides 0.9 nutrition, so this roughly gives you how many simple meals you'll need to cook to have 5 days of food.", 3001));
			stats.Add(new StatDrawEntry(cat, "​col * 2", "", "Create 2 of something for each pawn that you have. Good for medicine, clothing, weapons, etc.", 3000));
			stats.Add(new StatDrawEntry(cat, "​if(\"slate blocks\" > 200, 50, 0)", "", "Check if we have more than 200 slate blocks. If we do, produce up to 50 of this thing. If not, produce 0 of this thing.", 2999));
			// TODO: This was being cropped for too long.
			//stats.Add(new StatDrawEntry(cat, "​if(\"c meat\" > 200, \"animals.intake\" * 20 * 15, 0)", "", "My kibble production equation! If we have more than 200 meat, create 15 days worth of kibble for our animals.\n\n\"animals.intake\" is the intake of all of your animals, for 1 day.\n\nThe *20 accounts for kibble's 0.05 nutritional intake. In the future, this can value will be added to the mod itself.\n\nThe *15 determines the number of days.", 2998));

			cat = catPawns;
			stats.Add(new StatDrawEntry(cat, "​pawns", attachedBill.Cache.pawns.Count().ToString(), "Alias: pwn\nNumber of owned pawns on the map the bill is contained in.", 3001));
			stats.Add(new StatDrawEntry(cat, "​colonists", attachedBill.Cache.colonists.Count().ToString(), "Alias: col\nNumber of colonists on the map the bill is contained in. Does not include prisoners or slaves.", 3000));
			stats.Add(new StatDrawEntry(cat, "​mechanitors", attachedBill.Cache.mechanitors.Count().ToString(), "Alias: mech\nNumber of owned mechanitors on the map the bill is contained in. Doesn't include slaves or prisoners", 2990));
			//stats.Add(new StatDrawEntry(cat, "​mechanitors bandwidth", attachedBill.Cache.mechanitorsAvailableBandwidth.ToString(), "Alias: mech ban\nAmount of available bandwidth for mechanitors on the map the bill is contained in.", 2980));
			stats.Add(new StatDrawEntry(cat, "​slaves", attachedBill.Cache.slaves.Count().ToString(), "Alias: slv\nNumber of owned slaves on the map the bill is contained in.", 2970));
			stats.Add(new StatDrawEntry(cat, "​prisoners", attachedBill.Cache.prisoners.Count().ToString(), "Alias: pri\nNumber of owned prisoners on the map the bill is contained in.", 2960));
			stats.Add(new StatDrawEntry(cat, "​animals", attachedBill.Cache.ownedAnimals.Count().ToString(), "Alias: anim\nNumber of owned animals on the map the bill is contained in.", 2950));
#if v1_4
			stats.Add(new StatDrawEntry(cat, "​babies", attachedBill.Cache.babies.Count().ToString(), "Alias: bab\nNumber of owned babies on the map the bill is contained in.", 2940));
			stats.Add(new StatDrawEntry(cat, "​kids", attachedBill.Cache.kids.Count().ToString(), "Alias: kid\nNumber of owned children on the map the bill is contained in.", 2930));
#endif

			//cat = catModifiers;
			//stats.Add(new StatDrawEntry(cat, "​\"pawns intake\"", attachedBill.Cache.pawnsIntake.ToString(), "Alias: \"pwn in\"\nThe \" in\" or \" intake\" modifier can be used on any group of pawns like \"slaves intake\", \"anim in\", etc. It returns the amount of nutrition a pawn requires per day after all modifiers have been applied.\n\nThis number can also be seen in a pawn's info card under \"Food consumption\".", 2995));

			return stats;
		}
	
		private List<StatDrawEntry> GetStatDefEntries() {
			var stats = new List<StatDrawEntry>();

			var cat = catIntroduction;
			stats.Add(new StatDrawEntry(cat, "Description", "",
@"Here you can see all StatDefs in the game. This is meant to be used as a reference to look up something you're not sure how to search.

Click on a row to get an explanation.",
				10000));

			// Add specially added stats first.
			cat = catModifiers;
//			stats.Add(new StatDrawEntry(cat, "Description", "",
//@"These are ""StatDefs"" that aren't inherantly searchable, so I've added them manually. Search these the same as you would any other StatDef.",
//				10000));
			stats.Add(new StatDrawEntry(cat, "​male", "", "Whether a pawn is male.", 3000));
			stats.Add(new StatDrawEntry(cat, "​female", "", "Whether a pawn is female.", 2990));
			stats.Add(new StatDrawEntry(cat, "​intake", "", "How much food a pawn requires per day.", 2980));
			stats.Add(new StatDrawEntry(cat, "​bandwidth", "", "How much bandwidth a pawn has.", 2970));
			stats.Add(new StatDrawEntry(cat, "​stack limit", "", "The maximum amount of this item that can be stacked.", 2970));

			cat = catBasics;
			foreach (StatDef statdef in Math.searchableStats.Values) {
				// TODO: Change this to be sort by alphabetical
				stats.Add(new StatDrawEntry(statdef.category ?? cat, "​" + (statdef.label ?? statdef.defName).ToParameter(), "", statdef.description ?? "", statdef.displayPriorityInCategory + 1000));
			}

			return stats;
		}	
	}

	public enum InfoCardTab {
		Basic,
		StatDefs,
	}
}
