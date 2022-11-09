using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System;

namespace CrunchyDuck.Math {
	// TODO: Add male/female searching for pawns.
	// TODO: Guests from quests. They also ask for "from mods" but I don't know how I can do that if they're different.
	class CachedMapData {
		private Map map;
		private static Regex v13_getIntake = new Regex(@"Final value: (\d+(?:.\d+)?)", RegexOptions.Compiled);

		private static Regex getTarget = new Regex(@"(?<target>.+?)(?:\.|$)(?<statDef>.+)?", RegexOptions.Compiled);
		private static Regex getStatDef = new Regex(@"(?:(?<isIndividual>o|own|owned) )?(?<statDef>.+)?", RegexOptions.Compiled);
		private static Regex checkCategory = new Regex(@"(c|cat|category) (?<target>.+)", RegexOptions.Compiled);
		// This codebase is getting to be messy.
		private static Dictionary<string, Func<Thing, float>> customThingCounters = new Dictionary<string, Func<Thing, float>> {
			{ "male", CountMalePawns },
			{ "female", CountFemalePawns },
			{ "intake", CountIntake },
			{ "bandwidth", CountBandwidth },
		};
		private static Dictionary<string, Func<ThingDef, float>> customThingDefCounters = new Dictionary<string, Func<ThingDef, float>> {
			{ "stack limit", t => t.stackLimit },
		};
		private static Dictionary<string, Func<CachedMapData, List<Thing>>> customThingGetters = new Dictionary<string, Func<CachedMapData, List<Thing>>> {
			{ "col", cmd => cmd.colonists },
			{ "colonists", cmd => cmd.colonists },

			{ "pwn", cmd => cmd.pawns },
			{ "pawns", cmd => cmd.pawns },

			{ "slv", cmd => cmd.slaves },
			{ "slaves", cmd => cmd.slaves },

			{ "pri", cmd => cmd.prisoners },
			{ "prisoners", cmd => cmd.prisoners },

			{ "anim", cmd => cmd.ownedAnimals },
			{ "animals", cmd => cmd.ownedAnimals },

			{ "bab", cmd => cmd.babies },
			{ "babies", cmd => cmd.babies },

			{ "mech", cmd => cmd.mechanitors },
			{ "mechanitors", cmd => cmd.mechanitors },

			{ "kid", cmd => cmd.kids },
			{ "kids", cmd => cmd.kids },
		};

		public List<Thing> pawns = new List<Thing>();
		public List<Thing> mechanitors = new List<Thing>();
		public int mechanitorsAvailableBandwidth = 0;
		public List<Thing> colonists = new List<Thing>();
		public List<Thing> kids = new List<Thing>();
		public List<Thing> babies = new List<Thing>();
		public List<Thing> prisoners = new List<Thing>();
		public List<Thing> slaves = new List<Thing>();
		public List<Thing> ownedAnimals = new List<Thing>();
		public float pawnsIntake = 0;
		public float colonistsIntake = 0;
		public float mechanitorsIntake = 0;
		public float kidsIntake = 0;
		public float babiesIntake = 0;
		public float prisonersIntake = 0;
		public float slavesIntake = 0;
		public float ownedAnimalsIntake = 0;
		public Dictionary<string, List<Thing>> resources = new Dictionary<string, List<Thing>>();

		public CachedMapData(Map map) {
			this.map = map;

			pawns = map.mapPawns.FreeColonistsAndPrisoners.Cast<Thing>().ToList();
			slaves = map.mapPawns.SlavesOfColonySpawned.Cast<Thing>().ToList();
			colonists = map.mapPawns.FreeColonists.Except(slaves).ToList().Cast<Thing>().ToList();
			prisoners = map.mapPawns.PrisonersOfColony.Cast<Thing>().ToList();
			// stolen from MainTabWindow_Animals.Pawns :)
			ownedAnimals = map.mapPawns.PawnsInFaction(Faction.OfPlayer).Where(p => p.RaceProps.Animal).ToList().Cast<Thing>().ToList();

#if v1_4
			mechanitors = colonists.Where(p => ((Pawn)p).mechanitor != null).ToList().Cast<Thing>().ToList();
			mechanitorsAvailableBandwidth = 0;
			foreach(Pawn p in mechanitors) {
				mechanitorsAvailableBandwidth += p.mechanitor.TotalBandwidth - p.mechanitor.UsedBandwidth;
			}

			kids = colonists.Where(p => ((Pawn)p).DevelopmentalStage == DevelopmentalStage.Child).ToList().Cast<Thing>().ToList();
			babies = colonists.Where(p => ((Pawn)p).DevelopmentalStage == DevelopmentalStage.Baby || ((Pawn)p).DevelopmentalStage == DevelopmentalStage.Newborn).ToList().Cast<Thing>().ToList();
#endif
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
					// See: Need_Food.BaseHungerRate
					intake += float.Parse(RaceProperties.NutritionEatenPerDay(p));
#endif
				}
				catch (System.ArgumentNullException) {
					Log.Warning("Could not parse nutrition of pawn. Name: " + p.Name + ". Please let me (CrunchyDuck) know so I can fix it!");
				}
				// I'm not sure when this is triggered. Someone reported it happened after a baby was born, regardless it stopped the field rendering.
				catch (System.NullReferenceException) {
					continue;
				}
			}
			return intake;
		}

		private static float CountIntake(Thing potential_pawn) {
			if (potential_pawn is Pawn) {
				Pawn pawn = (Pawn)potential_pawn;
				return float.Parse(RaceProperties.NutritionEatenPerDay(pawn));
			}
			return 0;
		}

		private static float CountMalePawns(Thing potential_pawn) {
			if (potential_pawn is Pawn) {
				Pawn pawn = (Pawn)potential_pawn;
				return pawn.gender == Gender.Male ? 1 : 0;
			}
			return 0;
		}

		private static float CountBandwidth(Thing potential_pawn) {
			if (potential_pawn is Pawn) {
				Pawn pawn = (Pawn)potential_pawn;
				var mechanitor = pawn.mechanitor;
				if (mechanitor != null)
					return mechanitor.TotalBandwidth - mechanitor.UsedBandwidth;
			}
			return 0;
		}

		private static float CountFemalePawns(Thing potential_pawn) {
			if (potential_pawn is Pawn) {
				Pawn pawn = (Pawn)potential_pawn;
				return pawn.gender == Gender.Female ? 1 : 0;
			}
			return 0;
		}

		/// <summary>
		/// Search for a resource, a category of resources, with an optional statdef modifier.
		/// </summary>
		/// <param name="parameter"></param>
		/// <param name="bc">Recipe component to search with.</param>
		/// <param name="count">How much of the searched thing there is</param>
		/// <returns>true if search was valid and count was filled.</returns>
		public bool SearchForResource(string parameter, BillComponent bc, out float count) {
			// TODO: Clean up this code. Please.
			count = 0;
			if (parameter.NullOrEmpty())
				return false;

			// Get target
			Match match;
			match = getTarget.Match(parameter);
			string target = match.Groups["target"].Value;
			if (target.NullOrEmpty())
				return false;

			bool count_individual_things = false;
			bool counting_statdef = false;
			StatDef statdef = null;
			Func<Thing, float> custom_thing_search = null;
			Func<ThingDef, float> custom_thingdef_search = null;
			List<Thing> things = null;
			ThingDef thingdef = null;
			if (customThingGetters.TryGetValue(target, out Func<CachedMapData, List<Thing>> getter)) {
				things = getter.Invoke(this);
			}

			// Searched a stat of the thing group.
			if (match.Groups["statDef"].Success) {
				counting_statdef = true;
				match = getStatDef.Match(match.Groups["statDef"].Value);
				if (match.Groups["statDef"].Value.NullOrEmpty()) {
					return false;
				}

				count_individual_things = match.Groups["isIndividual"].Success;
				var string_statdef = match.Groups["statDef"].Value.ToParameter();
				if (Math.searchableStats.ContainsKey(string_statdef))
					statdef = Math.searchableStats[string_statdef];
				else if (customThingCounters.ContainsKey(string_statdef))
					custom_thing_search = customThingCounters[string_statdef];
				else if (customThingDefCounters.ContainsKey(string_statdef))
					custom_thingdef_search = customThingDefCounters[string_statdef];
				// Gave statdef but is invalid.
				else
					return false;
			}

			if (counting_statdef) {
				// Searching custom thing.
				if (things != null) {
					if (custom_thing_search == null && custom_thingdef_search == null)
						return false;
				}
				else {
					if (!count_individual_things && statdef == null && custom_thingdef_search == null)
						return false;
				}
			}

			// Search thing
			if (Math.searchableThings.ContainsKey(target)) {
				if (!counting_statdef || count_individual_things) {
					things = GetThings(target, bc);
					thingdef = null;
				}
				else {
					thingdef = Math.searchableThings[target];
					things = null;
				}
				count = CountThingSorter(things, thingdef, statdef, custom_thing_search, custom_thingdef_search);
				return true;
			}

			// Search category
			Match category_split = checkCategory.Match(target);
			if (category_split.Success) {
				// Is the category valid?
				ThingCategoryDef cat;
				if (!Math.searchableCategories.TryGetValue(category_split.Groups["target"].Value.ToParameter(), out cat))
					return false;
				// Search things in category.
				foreach (ThingDef cat_thingdef in cat.childThingDefs) {
					if (!counting_statdef || count_individual_things) {
						things = GetThings(cat_thingdef.label.ToParameter(), bc);
						thingdef = null;
					}
					else {
						things = null;
						thingdef = cat_thingdef;
					}
					count += CountThingSorter(things, thingdef, statdef, custom_thing_search, custom_thingdef_search);
				}
				// Search child categories
				foreach (ThingCategoryDef catdef in cat.childCategories) {
					foreach (ThingDef cat_thingdef in catdef.childThingDefs) {
						if (!counting_statdef || count_individual_things) {
							things = GetThings(cat_thingdef.label.ToParameter(), bc);
							thingdef = null;
						}
						else {
							things = null;
							thingdef = cat_thingdef;
						}
						count += CountThingSorter(things, thingdef, statdef, custom_thing_search, custom_thingdef_search);
					}
				}
				return true;
			}

			// TODO: Let custom things search with statdef.
			// Search custom things
			if (things != null) {
				count = CountThingSorter(things, null, statdef, custom_thing_search, custom_thingdef_search);
				return true;
				//CountThingSorter(getter.Invoke(this), statdef, statdef_is_individual, custom_thing_search, custom_thingdef_search);
			}

			return false;
		}

		/// <summary>
		/// Used by SearchForResources to figure out how it should count what it has parsed.
		/// </summary>
		private float CountThingSorter(List<Thing> things, ThingDef thingdef, StatDef sd, Func<Thing, float> thing_counter, Func<ThingDef, float> thingdef_counter) {
			if (things != null) {
				if (sd != null)
					return CountThing(things, sd);
				else if (thing_counter != null)
					return CountThing(things, thing_counter);
				else
					return CountThing(things);
			}
			else {
				if (sd != null)
					return CountThingDef(thingdef, sd);
				else
					return CountThingDef(thingdef, thingdef_counter);
			}
		}

		/// <summary>
		/// Count how many of this thing are on bc's map.
		/// </summary>
		private float CountThing(List<Thing> things) {
			return things.Sum(t => t.stackCount);
		}

		/// <summary>
		/// Count a statdef.
		/// </summary>
		/// <param name="thing_name"></param>
		/// <param name="bc"></param>
		/// <param name="sd">StatDef to count.</param>
		/// <param name="sd_is_def">If set to false, will sum up all stats. Else, will only count the value in the def.</param>
		private float CountThing(List<Thing> things, StatDef sd) {
			// Count stats of all items.
			return things.Sum(t => t.GetStatValue(sd) * t.stackCount);
		}

		private float CountThingDef(ThingDef thingdef, StatDef sd) {
			return thingdef.GetStatValueAbstract(sd);
		}

		/// <summary>
		/// Count properties of a thingdef with an arbitrary counter.
		/// </summary>
		private float CountThingDef(ThingDef thingdef, Func<ThingDef, float> thingdef_counter) {
			return thingdef_counter.Invoke(thingdef);
		}

		/// <summary>
		/// Count properties of a thing with an arbitrary counter.
		/// </summary>
		private float CountThing(List<Thing> things, Func<Thing, float> thing_counter) {
			return things.Sum(thing_counter);
		}

		public List<Thing> GetThings(string thing_name, BillComponent bc) {
			List<Thing> found_things = new List<Thing>();
			// Fill the list of *all* of this thing first
			if (!resources.ContainsKey(thing_name)) {
				ThingDef td = Math.searchableThings[thing_name];
				// Patch to fix a missing key bug report:
				// https://steamcommunity.com/workshop/filedetails/discussion/2876902608/3487500856972015279/?ctp=3#c3495383439605482600
				var l = map.listerThings.ThingsOfDef(td).ListFullCopy();
				resources[thing_name] = l ?? new List<Thing>();
				// Count equipped/inventory/hands.
				foreach (Pawn pawn in map.mapPawns.FreeColonistsAndPrisonersSpawned) {
					List<Thing> things = GetThingInPawn(pawn, td);
					foreach (var thing in things) {
						resources[thing_name].Add(thing);
					}
				}
			}

			// TODO: Index things that are on corpses. 
			// Filter this thing based on parameters.
			foreach (Thing _thing in resources[thing_name]) {
				Thing thing = _thing.GetInnerIfMinified();
				// Check if in stockpile.
				// TODO: Make default only check stockpiles, with an option to make it check everywhere.
				var zone = bc.targetBill.includeFromZone;
				if (zone != null && !zone.ContainsCell(thing.InteractionCell)) {
					continue;
				}

				// Forbidden
				if (thing.IsForbidden(Faction.OfPlayer))
					continue;

				// Hitpoints
				if (thing.def.useHitPoints && !bc.targetBill.hpRange.Includes((float)thing.HitPoints / (float)thing.MaxHitPoints)) {
					continue;
				}

				// Quality
				QualityCategory q;
				if (thing.TryGetQuality(out q) && !bc.targetBill.qualityRange.Includes(q))
					continue;

				var producted_thing = bc.targetBill.recipe.ProducedThingDef;
				if (producted_thing != null) {
					// Tainted
					bool can_choose_tainted = producted_thing.IsApparel && producted_thing.apparel.careIfWornByCorpse;
					Apparel a = thing.GetType() == typeof(Apparel) ? (Apparel)thing : null;
					if (can_choose_tainted && !bc.targetBill.includeTainted && a?.WornByCorpse == true)
						continue;

					// Equipped.
					bool can_choose_equipped = producted_thing.IsWeapon || producted_thing.IsApparel;
					if (can_choose_equipped && !bc.targetBill.includeEquipped && thing.IsHeldByPawn()) {
						continue;
					}
				}
				found_things.Add(thing);
			}
			return found_things;
		}

		// Code taken from RecipeWorkerCounter.CountProducts
		public static List<Thing> GetThingInPawn(Pawn pawn, ThingDef def) {
			List<Thing> things = new List<Thing>();
			foreach (ThingWithComps equipment in pawn.equipment.AllEquipmentListForReading) {
				if (equipment.def.Equals(def)) {
					things.Add((Thing)equipment);
				}
			}
			foreach (var apparel in pawn.apparel.WornApparel) {
				if (apparel.def.Equals(def)) {
					things.Add((Thing)apparel);
				}
			}
			// TODO: Check this in the future. It doesn't work as of now in spite of multiple tests, and I believe it's a core game problem.
			foreach (Thing heldThing in pawn.inventory.GetDirectlyHeldThings()) {
				if (heldThing.def.Equals(def)) {
					things.Add((Thing)heldThing);
				}
			}

			return things;
		}
	}
}
