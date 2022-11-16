using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using System.Linq;

namespace CrunchyDuck.Math.MathFilters {
	class PawnFilter : MathFilter {
		Dictionary<string, Pawn> contains = new Dictionary<string, Pawn>();
		private bool primedForTrait = false;
		private bool canCount = true;
		public override bool CanCount { get { return canCount; } }

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
			// We were expecting a trait.
			if (primedForTrait) {
				if (!Math.searchableTraits.ContainsKey(command)) {
					return ReturnType.Null;
				}
				primedForTrait = false;
				canCount = true;

				Dictionary<string, Pawn> filtered_pawns = new Dictionary<string, Pawn>();
				foreach (KeyValuePair<string, Pawn> entry in contains) {
					if (HasTrait(entry.Value, command)) {
						filtered_pawns[entry.Key] = entry.Value;
					}
				}
				contains = filtered_pawns;
				result = this;
				return ReturnType.PawnFilter;
			}
			if (command == "traits") {
				primedForTrait = true;
				result = this;
				canCount = false;
				return ReturnType.PawnFilter;
			}


			// Search pawn.
			if (contains.ContainsKey(command)) {
				contains = new Dictionary<string, Pawn>() {
					{ command, contains[command] }
				};
				result = this;
				return ReturnType.PawnFilter;
			}
			// Search filter.
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
			// Search counter.
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
	
		public static bool HasTrait(Pawn p, string trait_name) {
			var trait_dat = Math.searchableTraits[trait_name];
			var trait_def = trait_dat.traitDef;
			var trait_degree = trait_def.degreeDatas[trait_dat.index].degree;
			if (p.story.traits.HasTrait(trait_def, trait_degree))
				return true;
			return false;
		}
	}
}
