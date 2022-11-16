using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using System.Linq;

namespace CrunchyDuck.Math.MathFilters {
	class PawnFilter : MathFilter {
		Dictionary<string, Pawn> contains = new Dictionary<string, Pawn>();
		public override bool CanCount { get { return true; } }

		public static Dictionary<string, Func<Pawn, bool>> filterMethods = new Dictionary<string, Func<Pawn, bool>>() {
			{ "pawns", p => !p.AnimalOrWildMan() },
			
			{ "colonists", p => !p.IsSlave && !p.IsPrisoner && !p.IsQuestLodger()},
			{ "col", p => !p.IsSlave && !p.IsPrisoner && !p.IsQuestLodger()},

			{ "mechanitors", p => p.mechanitor != null },
			{ "mech", p => p.mechanitor != null },

			{ "prisoners", p => p.IsPrisoner},
			{ "pri", p => p.IsPrisoner},

			{ "slaves", p => p.IsSlave},
			{ "slv", p => p.IsSlave},

			{ "guests", p => p.IsQuestLodger()},

			{ "animals", p => p.AnimalOrWildMan()},
			{ "anim", p => p.AnimalOrWildMan()},

			{ "kids", p => p.DevelopmentalStage == DevelopmentalStage.Child},

			{ "babies", p => p.DevelopmentalStage == DevelopmentalStage.Baby || p.DevelopmentalStage == DevelopmentalStage.Newborn},
			{ "bab", p => p.DevelopmentalStage == DevelopmentalStage.Baby || p.DevelopmentalStage == DevelopmentalStage.Newborn},
		};

		public static Dictionary<string, Func<Pawn, float>> counterMethods = new Dictionary<string, Func<Pawn, float>>() {
			{ "bandwidth", CachedMapData.CountBandwidth },
			{ "male", CachedMapData.CountMalePawns },
			{ "female", CachedMapData.CountFemalePawns },
			{ "intake", CachedMapData.CountIntake },
		};

		public PawnFilter(BillComponent bc) {
			// TODO: Cache + performance test this.
			contains = bc.Cache.pawns_dict;
		}

		public override float Count() {
			return contains.Count;
		}

		public override ReturnType Parse(string command, out object result) {
			result = null;
			// Search pawn.
			if (contains.ContainsKey(command)) {
				contains = new Dictionary<string, Pawn>() {
					{ command, contains[command] }
				};
				result = this;
				return ReturnType.PawnFilter;
			}
			
			if (filterMethods.ContainsKey(command)) {
				var method = filterMethods[command];
				Dictionary<string, Pawn> filtered_pawns = new Dictionary<string, Pawn>();
				foreach(KeyValuePair<string, Pawn> entry in contains) {
					if (method.Invoke(entry.Value)) {
						filtered_pawns[entry.Key] = entry.Value;
					}
				}
				contains = filtered_pawns;
				result = this;
				return ReturnType.PawnFilter;
			}

			if (counterMethods.ContainsKey(command)) {
				var method = counterMethods[command];
				float count = 0;
				foreach (Pawn p in contains.Values) {
					count += method.Invoke(p);
				}
				result = count;
				return ReturnType.Count;
			}

			return ReturnType.Null;
		}
	}
}
