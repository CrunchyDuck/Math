using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using System.Linq;

namespace CrunchyDuck.Math.MathFilters {
	class PawnFilter : MathFilter {
		List<Pawn> contains = new List<Pawn>();
		public override bool CanCount { get { return true; } }

		public static Dictionary<string, Func<Pawn, bool>> filterMethods = new Dictionary<string, Func<Pawn, bool>>() {
			{ "pawns", p => !p.AnimalOrWildMan() },
			{ "colonists", p => !p.IsSlave && !p.IsPrisoner && !p.IsQuestLodger()},
			{ "mechanitors", p => p.mechanitor != null },
			{ "prisoners", p => p.IsPrisoner},
			{ "slaves", p => p.IsSlave},
			{ "guests", p => p.IsQuestLodger()},
			{ "animals", p => p.AnimalOrWildMan()},
			{ "kids", p => p.DevelopmentalStage == DevelopmentalStage.Child},
			{ "babies", p => p.DevelopmentalStage == DevelopmentalStage.Baby || p.DevelopmentalStage == DevelopmentalStage.Newborn},
		};

		public static Dictionary<string, Func<Pawn, float>> counterMethods = new Dictionary<string, Func<Pawn, float>>() {
			{ "bandwidth", CachedMapData.CountBandwidth },
			{ "male", CachedMapData.CountMalePawns },
			{ "female", CachedMapData.CountFemalePawns },
			{ "intake", CachedMapData.CountIntake },
		};

		public PawnFilter(BillComponent bc) {
			// TODO: Cache + performance test this.
			contains = bc.targetBill.Map.mapPawns.PawnsInFaction(Faction.OfPlayer);
		}

		public override float Count() {
			return contains.Count;
		}

		public override ReturnType Parse(string command, out object result) {
			result = null;
			if (filterMethods.ContainsKey(command)) {
				var method = filterMethods[command];
				List<Pawn> filtered_pawns = new List<Pawn>();
				foreach(Pawn pawn in contains) {
					if (method.Invoke(pawn)) {
						filtered_pawns.Add(pawn);
					}
				}
				contains = filtered_pawns;
				result = this;
				return ReturnType.PawnFilter;
			}

			if (counterMethods.ContainsKey(command)) {
				var method = counterMethods[command];
				float count = 0;
				foreach (Pawn p in contains) {
					count += method.Invoke(p);
				}
				result = count;
				return ReturnType.Count;
			}

			return ReturnType.Null;
		}
	}
}
