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
		public StatCategoryDef catTraits = DefDatabase<StatCategoryDef>.AllDefs.First(scd => scd.defName == "CDTraits");

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

		private Dictionary<string, float> valueCache = new Dictionary<string, float>();


		// TODO: Add X in top right.
		public Dialog_MathInfoCard(BillComponent bc) {
			if (MathSettings.settings.lastVersionInfocardChecked != Math.version) {
				MathSettings.settings.lastVersionInfocardChecked = Math.version;
				MathSettings.settings.Write();
			}
			attachedBill = bc;
			// Get a cache of current values. I'll clean this up some other time.
			valueCache["pawns"] = bc.Cache.humanPawns.Count();
			object r;

			new MathFilters.PawnFilter(bc).Parse("colonists", out r);
			valueCache["colonists"] = ((MathFilters.MathFilter)r).Count();

			new MathFilters.PawnFilter(bc).Parse("mechanitors", out r);
			valueCache["mechanitors"] = ((MathFilters.MathFilter)r).Count();

			new MathFilters.PawnFilter(bc).Parse("slaves", out r);
			valueCache["slaves"] = ((MathFilters.MathFilter)r).Count();

			new MathFilters.PawnFilter(bc).Parse("prisoners", out r);
			valueCache["prisoners"] = ((MathFilters.MathFilter)r).Count();

			new MathFilters.PawnFilter(bc).Parse("guests", out r);
			valueCache["guests"] = ((MathFilters.MathFilter)r).Count();

			new MathFilters.PawnFilter(bc).Parse("animals", out r);
			valueCache["animals"] = ((MathFilters.MathFilter)r).Count();

			new MathFilters.PawnFilter(bc).Parse("babies", out r);
			valueCache["babies"] = ((MathFilters.MathFilter)r).Count();

			new MathFilters.PawnFilter(bc).Parse("kids", out r);
			valueCache["kids"] = ((MathFilters.MathFilter)r).Count();

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
			// BIG TODO: Change log.
			// BIG TODO: Update documentation for 1.2.0
			List<TabRecord> tabs = new List<TabRecord>();

			// TODO: More tabs, such as for categorydefs and maybe pawn groups?
			tabs.Add(new TabRecord("Basic", () => tab = InfoCardTab.Basic, tab == InfoCardTab.Basic));
			tabs.Add(new TabRecord("Traits", () => tab = InfoCardTab.Traits, tab == InfoCardTab.Traits));
			tabs.Add(new TabRecord("StatDefs", () => tab = InfoCardTab.StatDefs, tab == InfoCardTab.StatDefs));

			Rect label_area = new Rect(inRect);
			label_area = label_area.ContractedBy(18f);
			label_area.height = 34f;
			label_area.xMax -= 34f;

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
			else if (tab == InfoCardTab.Traits)
				statEntries = GetTraitEntries();
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

			// Draw version number
			//Rect version_pos = new Rect(inRect.xMax - 9f, label_area.y, 200f, 34f);
			var anch = Text.Anchor;
			Text.Anchor = TextAnchor.MiddleRight;
			Widgets.Label(label_area, "v" + Math.version);
			Text.Anchor = anch;

			//if (this.thing != null)
			//	Widgets.ThingIcon(rect2, this.thing);
			//else
			//	Widgets.DefIcon(rect2, this.def, this.stuff, drawPlaceholder: true);

			StatsWorker.Invoke(null, new object[] { stats_area.ContractedBy(18f), null, null });
		}

		private List<StatDrawEntry> GetTraitEntries() {
			var stats = new List<StatDrawEntry>();
			StatDrawEntry stat;

			var cat = catIntroduction;
			stat = new StatDrawEntry(cat, "Description".Translate(), "", "CD_M_infocard_traits_description".Translate(),
				10000);
			stats.Add(stat);

			cat = catTraits;
			var traits_sorted = Math.searchableTraits.Values.OrderByDescending(t => t.traitDef.degreeDatas[t.index].label);
			int i = 0;
			foreach (var (traitDef, index) in traits_sorted) {
				var trait_dat = traitDef.degreeDatas[index];
				stats.Add(new StatDrawEntry(cat, "​" + trait_dat.label.ToParameter(), "", trait_dat.description, i++));
			}

			return stats;
		}

		private List<StatDrawEntry> GetBasicEntries() {
			var stats = new List<StatDrawEntry>();
			StatDrawEntry stat;
			int display_priority = 10000;

			var cat = catIntroduction;
			stat = new StatDrawEntry(cat, "Description".Translate(), "", "CD_M_infocard_introduction_description".Translate(),
				display_priority--);
			stats.Add(stat);

			stats.Add(new StatDrawEntry(cat, "CD_M_tutorial_item_count".Translate(), "", "CD_M_tutorial_item_count_description".Translate(), display_priority--));

			stats.Add(new StatDrawEntry(cat, "CD_M_tutorial_searching_things".Translate(), "", "CD_M_tutorial_searching_things_description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "CD_M_tutorial_searching_stats".Translate(), "", "CD_M_tutorial_searching_stats_description".Translate(), display_priority--));

			stats.Add(new StatDrawEntry(cat, "CD_M_tutorial_searching_categories".Translate(), "", "CD_M_tutorial_searching_categories_description".Translate(), display_priority--));

			stats.Add(new StatDrawEntry(cat, "CD_M_tutorial_searching_traits".Translate(), "", "CD_M_tutorial_searching_traits_description".Translate(), display_priority--));

			// !!! BIG WARNING !!!
			// the label/second variable in these all have a zero-width space placed at the start.
			// This is done to force the variables to be lowercase in menus.
			// nice thinking oken
			// BEWARE THE HIDDEN HORRORS.
			// TODO: Fill these in with their in-game values.
			cat = catExamples;
			stats.Add(new StatDrawEntry(cat, "​" + "CD_M_tutorial_example_intake".Translate(), "", "CD_M_tutorial_example_intake_description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "CD_M_tutorial_example_colonists".Translate(), "", "CD_M_tutorial_example_colonists_description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "CD_M_tutorial_example_clothing_production".Translate(), "", "CD_M_tutorial_example_clothing_production_description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "CD_M_tutorial_example_if".Translate(), "", "CD_M_tutorial_example_if_description".Translate(), 2950));
			// TODO: This was being cropped for too long.
			//stats.Add(new StatDrawEntry(cat, "​if(\"c meat\" > 200, \"animals.intake\" * 20 * 15, 0)", "", "My kibble production equation! If we have more than 200 meat, create 15 days worth of kibble for our animals.\n\n\"animals.intake\" is the intake of all of your animals, for 1 day.\n\nThe *20 accounts for kibble's 0.05 nutritional intake. In the future, this can value will be added to the mod itself.\n\nThe *15 determines the number of days.", 2998));

			cat = catPawns;
			stats.Add(new StatDrawEntry(cat, "​" + "pawns", valueCache["pawns"].ToString(), "CD_M_pawn_group_pawns_description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "colonists", valueCache["colonists"].ToString(), "CD_M_pawn_group_colonists_description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "mechanitors", valueCache["mechanitors"].ToString(), "CD_M_pawn_group_mechanitors_description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "slaves", valueCache["slaves"].ToString(), "CD_M_pawn_group_slaves_description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "prisoners", valueCache["prisoners"].ToString(), "CD_M_pawn_group_prisoners_description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "guests", valueCache["guests"].ToString(), "CD_M_pawn_group_guests_description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "animals", valueCache["animals"].ToString(), "CD_M_pawn_group_animals_description".Translate(), display_priority--));
#if v1_4
			stats.Add(new StatDrawEntry(cat, "​" + "babies", valueCache["babies"].ToString(), "CD_M_pawn_group_babies_description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "kids", valueCache["kids"].ToString(), "CD_M_pawn_group_kids_description".Translate(), display_priority--));
#endif

			return stats;
		}
	
		private List<StatDrawEntry> GetStatDefEntries() {
			var stats = new List<StatDrawEntry>();
			int display_priority = 10000;

			var cat = catIntroduction;
			stats.Add(new StatDrawEntry(cat, "Description".Translate(), "", "CD_M_infocard_statdefs_description".Translate(), display_priority--));

			// Add specially added stats first.
			cat = catModifiers;
//			stats.Add(new StatDrawEntry(cat, "Description", "",
//@"These are ""StatDefs"" that aren't inherantly searchable, so I've added them manually. Search these the same as you would any other StatDef.",
//				10000));
			stats.Add(new StatDrawEntry(cat, "​" + "male", "", "CD_M_counters_male_description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "female", "", "CD_M_counters_female_description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "intake", "", "CD_M_counters_intake_description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "bandwidth", "", "CD_M_counters_bandwidth_description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "stack limit", "", "CD_M_counters_stack_limit_description".Translate(), display_priority--));

			cat = catBasics;
			var stats_sorted = Math.searchableStats.Values.OrderByDescending(t => t.label);
			int i = 0;
			foreach (StatDef statdef in stats_sorted) {
				stats.Add(new StatDrawEntry(cat, "​" + statdef.label.ToParameter(), "", statdef.description ?? "", i++));
			}

			return stats;
		}	
	}

	public enum InfoCardTab {
		Basic,
		Traits,
		StatDefs,
	}
}
