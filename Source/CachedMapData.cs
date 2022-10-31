using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace CrunchyDuck.Math {
	// TODO: Add male/female searching for pawns.
	// TODO: Guests from quests. They also ask for "from mods" but I don't know how I can do that if they're different.
	class CachedMapData {
		private Map map;
		private static Regex v13_getIntake = new Regex(@"Final value: (\d+(?:.\d+)?)", RegexOptions.Compiled);

		private static Regex getTarget = new Regex(@"(?<target>.+?)(?:\.|$)(?<statDef>.+)?", RegexOptions.Compiled);
		private static Regex getStatDef = new Regex(@"(?:(?<isIndividual>o|own|owned) )?(?<statDef>.+)?", RegexOptions.Compiled);
		private static Regex checkCategory = new Regex(@"(c|cat|category) (?<target>.+)", RegexOptions.Compiled);

		public List<Pawn> pawns = new List<Pawn>();
		public List<Pawn> mechanitors = new List<Pawn>();
		public int mechanitorsAvailableBandwidth = 0;
		public List<Pawn> colonists = new List<Pawn>();
		public List<Pawn> kids = new List<Pawn>();
		public List<Pawn> babies = new List<Pawn>();
		public List<Pawn> prisoners = new List<Pawn>();
		public List<Pawn> slaves = new List<Pawn>();
		public List<Pawn> ownedAnimals = new List<Pawn>();
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

			pawns = map.mapPawns.FreeColonistsAndPrisoners;
			slaves = map.mapPawns.SlavesOfColonySpawned;
			colonists = map.mapPawns.FreeColonists.Except(slaves).ToList();
			prisoners = map.mapPawns.PrisonersOfColony;
			// stolen from MainTabWindow_Animals.Pawns :)
			ownedAnimals = map.mapPawns.PawnsInFaction(Faction.OfPlayer).Where(p => p.RaceProps.Animal).ToList();

			pawnsIntake = CountIntake(pawns);
			colonistsIntake = CountIntake(colonists);
			slavesIntake = CountIntake(slaves);
			prisonersIntake = CountIntake(prisoners);
			ownedAnimalsIntake = CountIntake(ownedAnimals);

#if v1_4
			mechanitors = colonists.Where(p => p.mechanitor != null).ToList();
			mechanitorsIntake = CountIntake(mechanitors);
			mechanitorsAvailableBandwidth = 0;
			foreach(Pawn p in mechanitors) {
				mechanitorsAvailableBandwidth += p.mechanitor.TotalBandwidth - p.mechanitor.UsedBandwidth;
			}

			kids = colonists.Where(p => p.DevelopmentalStage == DevelopmentalStage.Child).ToList();
			babies = colonists.Where(p => p.DevelopmentalStage == DevelopmentalStage.Baby || p.DevelopmentalStage == DevelopmentalStage.Newborn).ToList();
			kidsIntake = CountIntake(kids);
			babiesIntake = CountIntake(babies);
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

		// TODO: Do multiple passes of DoMath, where the first tallies up all resources they want to search, and the second searches for all of them at once.
		public bool SearchForResource_old(string parameter_name, BillComponent bc, out int count) {
			count = 0;
			if (parameter_name.NullOrEmpty())
				return false;

			// Search thing
			if (Math.old_searchableThings.ContainsKey(parameter_name)) {
				count = GetResourceCount_old(parameter_name, bc);
				return true;
			}
			// Search category
			else if (Math.old_searchabeCategories.ContainsKey(parameter_name)) {
				ThingCategoryDef cat = Math.old_searchabeCategories[parameter_name];
				foreach (ThingDef thingdef in cat.childThingDefs) {
					count += GetResourceCount_old(thingdef.label.ToParameter_old(), bc);
				}
				return true;
			}

			return false;
		}

		/// <summary>
		/// Search for a resource, a category of resources, with an optional statdef modifier.
		/// </summary>
		/// <param name="parameter"></param>
		/// <param name="bc">Recipe component to search with.</param>
		/// <param name="count">How much of the searched thing there is</param>
		/// <returns>true if search was valid and count was filled.</returns>
		public bool SearchForResource(string parameter, BillComponent bc, out float count) {
			count = 0;
			if (parameter.NullOrEmpty())
				return false;

			// Get target
			Match match;
			match = getTarget.Match(parameter);
			string target = match.Groups["target"].Value;
			if (target.NullOrEmpty())
				return false;

			// Get statdef
			bool statdef_is_individual = false;
			StatDef statdef = null;
			if (match.Groups["statDef"].Success) {
				match = getStatDef.Match(match.Groups["statDef"].Value);
				if (match.Groups["statDef"].Value.NullOrEmpty()) {
					return false;
				}

				statdef_is_individual = match.Groups["isIndividual"].Success;
				var string_statdef = match.Groups["statDef"].Value.ToParameter();
				// Gave statdef but is invalid.
				if (!Math.searchableStats.ContainsKey(string_statdef)) {
					return false;
				}
				statdef = Math.searchableStats[string_statdef];
			}

			// Search thing
			if (Math.searchableThings.ContainsKey(target)) {
				count = CountThing(target, bc, statdef, statdef_is_individual);
				return true;
			}

			// Is it a category?
			Match category_split = checkCategory.Match(target);
			if (category_split.Success) {
				// Is the category valid?
				ThingCategoryDef cat;
				if (!Math.searchableCategories.TryGetValue(category_split.Groups["target"].Value.ToParameter(), out cat))
					return false;
				// BUG: This doesn't get all thingdefs. I think I need to iterate .childCategories too.
				foreach (ThingDef thingdef in cat.childThingDefs) {
					count += CountThing(thingdef.label.ToParameter(), bc, statdef, statdef_is_individual);
				}
				return true;
			}

			return false;
		}

		private float CountThing(string thing_name, BillComponent bc, StatDef sd = null, bool sd_is_individual = false) {
			if (sd == null) {
				return GetThings(thing_name, bc).Sum(t => t.stackCount);
			}
			else if (sd_is_individual) {
				return GetThings(thing_name, bc).Sum(t => t.GetStatValue(sd) * t.stackCount);
			}
			else {
				return Math.searchableThings[thing_name].GetStatValueAbstract(sd);
			}
		}

		public int GetResourceCount_old(string parameter_name, BillComponent bc) {
			int count = 0;
			if (!resources.ContainsKey(parameter_name)) {
				resources[parameter_name] = map.listerThings.ThingsOfDef(Math.old_searchableThings[parameter_name]);
			}

			foreach (Thing thing in resources[parameter_name]) {
				if (thing.IsForbidden(Faction.OfPlayer))
					continue;
				// Doesn't have enough hitpoints
				if (!bc.targetBill.hpRange.IncludesEpsilon(thing.HitPoints / thing.MaxHitPoints))
					continue;
				// Has quality and is not good enough.
				QualityCategory q;
				if (thing.TryGetQuality(out q) && !bc.targetBill.qualityRange.Includes(q))
					continue;
				// Tainted when should not be.
				Apparel a = thing.GetType() == typeof(Apparel) ? (Apparel)thing : null;
				if (a != null && !bc.targetBill.includeTainted && a.WornByCorpse)
					continue;

			  count += thing.stackCount;
			}
			return count;
		}

		public List<Thing> GetThings(string thing_name, BillComponent bc) {
			List<Thing> found_things = new List<Thing>();
			// Fill the list of *all* of this thing first
			if (!resources.ContainsKey(thing_name)) {
				ThingDef td = Math.searchableThings[thing_name];
				resources[thing_name] = map.listerThings.ThingsOfDef(td).ListFullCopy();
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
