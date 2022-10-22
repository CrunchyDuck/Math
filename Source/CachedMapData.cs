using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace CrunchyDuck.Math {
	class CachedMapData {
		private Map map;
		private static Regex v13_getIntake = new Regex(@"Final value: (\d+(?:.\d+)?)", RegexOptions.Compiled);

		public List<Pawn> pawns = new List<Pawn>();
		public List<Pawn> colonists = new List<Pawn>();
		public List<Pawn> kids = new List<Pawn>();
		public List<Pawn> babies = new List<Pawn>();
		public List<Pawn> prisoners = new List<Pawn>();
		public List<Pawn> slaves = new List<Pawn>();
		public List<Pawn> ownedAnimals = new List<Pawn>();
		public float pawnsIntake = 0;
		public float colonistsIntake = 0;
		public float kidsIntake = 0;
		public float babiesIntake = 0;
		public float prisonersIntake = 0;
		public float slavesIntake = 0;
		public float ownedAnimalsIntake = 0;
		public Dictionary<string, int> resources = new Dictionary<string, int>();

		public CachedMapData(Map map) {
			this.map = map;

			pawns = map.mapPawns.FreeColonistsAndPrisoners;
			slaves = map.mapPawns.SlavesOfColonySpawned;
			colonists = map.mapPawns.FreeColonists.Except(slaves).ToList();
#if v1_4
			kids = colonists.Where(p => p.DevelopmentalStage == DevelopmentalStage.Child).ToList();
			babies = colonists.Where(p => p.DevelopmentalStage == DevelopmentalStage.Baby || p.DevelopmentalStage == DevelopmentalStage.Newborn).ToList();
#endif
			prisoners = map.mapPawns.PrisonersOfColony;
			// stolen from MainTabWindow_Animals.Pawns :)
			ownedAnimals = map.mapPawns.PawnsInFaction(Faction.OfPlayer).Where(p => p.RaceProps.Animal).ToList();

			pawnsIntake = CountIntake(pawns);
			colonistsIntake = CountIntake(colonists);
#if v1_4
			kidsIntake = CountIntake(kids);
			babiesIntake = CountIntake(babies);
#endif
			slavesIntake = CountIntake(slaves);
			prisonersIntake = CountIntake(prisoners);
			ownedAnimalsIntake = CountIntake(ownedAnimals);
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
			}
			return intake;
		}
	
		// TODO: Do multiple passes of DoMath, where the first tallies up all resources they want to search, and the second searches for all of them at once.
		public int SearchForResource(string parameter_name) {
			if (!resources.ContainsKey(parameter_name)) {
				List<Thing> things = map.listerThings.ThingsOfDef(Math.searchabeThings[parameter_name]);
				int count = 0;
				foreach (Thing thing in things) {
					if (thing.IsForbidden(Faction.OfPlayer))
						continue;
					count += thing.stackCount;
				}
				resources[parameter_name] = count;
			}

			return resources[parameter_name];
		}
	}
}
