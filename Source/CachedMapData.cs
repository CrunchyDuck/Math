﻿using System.Collections.Generic;
using System.Text.RegularExpressions;
using CrunchyDuck.Math.MathFilters;
using Inventory;
using ProjectRimFactory;
using ProjectRimFactory.Common.HarmonyPatches;
using ProjectRimFactory.Storage;
using RimWorld;
using Verse;
using ThingFilter = CrunchyDuck.Math.MathFilters.ThingFilter;

namespace CrunchyDuck.Math {
	// TODO: Add in checking against pawn skills, like "get all pawns with shooting > 3"
	public class CachedMapData {
		private Map map;
		private static Regex v13_getIntake = new Regex(@"Final value: (\d+(?:.\d+)?)", RegexOptions.Compiled);

		public Dictionary<string, Pawn> pawns_dict = new Dictionary<string, Pawn>();
		public List<Thing> humanPawns = new List<Thing>();
		public List<Thing> ownedAnimals = new List<Thing>();
		public Dictionary<string, List<Thing>> resources = new Dictionary<string, List<Thing>>();

		public CachedMapData(Map map) {
			this.map = map;

			foreach (Pawn p in map.mapPawns.AllPawns) {
				bool in_faction = p.Faction == Faction.OfPlayer;
				bool animal = p.AnimalOrWildMan();
				bool guest = p.IsQuestLodger() || p.guest?.HostFaction == Faction.OfPlayer;
				bool prisoner = p.IsPrisonerOfColony;
				bool slave = p.IsSlaveOfColony;

				if (animal && in_faction) {
					ownedAnimals.Add(p);
					pawns_dict[p.LabelShort.ToParameter()] = p;
				}
				else {
					if (in_faction || guest || prisoner || slave) {
						humanPawns.Add(p);
						pawns_dict[p.LabelShort.ToParameter()] = p;
					}
				}
			}
		}

		public bool SearchVariable(string input, BillComponent bc, out float count) {
			count = 0;
			string[] commands = input.Split('.');
			MathFilter filter = null;
			for (int i = 0; i < commands.Length; i++) {
				string command = commands[i];
				// Initialize a filter.
				if (filter == null) {
					// thing
					if (Math.searchableThings.ContainsKey(command)) {
						filter = new ThingFilter(bc, command);
						continue;
					}
					// category
					else if (CategoryFilter.names.Contains(command)) {
						if (i + 1 < commands.Length &&
						    CategoryFilter.searchableCategories.TryGetValue(commands[++i], out ThingCategoryDef value)) {
							filter = new ThingFilter(bc, value);
							continue;
						}
						else
							return false;
					}
					// thingdef
					else if (ThingDefFilter.names.Contains(command)) {
						if (i + 1 < commands.Length &&
						    Math.searchableThings.TryGetValue(commands[++i], out ThingDef value)) {
							filter = new ThingDefFilter(value);
							continue;
						}
						else
							return false;
					}
					// pawn
					else if (PawnFilter.filterMethods.ContainsKey(command) || pawns_dict.ContainsKey(command)) {
						filter = new PawnFilter(bc);
					}
					// compositable loadouts; Has to be extracted to separate methods because otherwise it throws exceptions when run without the mod.
					else if (Math.compositableLoadoutsSupportEnabled && IsCompositableLoadoutCommand(command)) {
						if (i + 1 >= commands.Length && GetCompositableLoadoutFilter(commands[++i], bc, ref filter))
							continue;
						return false;
					}
					// Can't find filter.
					else {
						return false;
					}

				}

				// Parse input
				ReturnType type = filter.Parse(command, out object result);
				switch (type) {
					case ReturnType.Count:
						count = (float) result;
						return true;
					case ReturnType.Null:
						return false;
					default:
						filter = (MathFilter) result;
						break;
				}
			}

			if (filter.CanCount) {
				count = filter.Count();
				return true;
			}

			return false;
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

				if (Math.rimfactorySupportEnabled)
					resources[thing_name].AddRange(GetThingFromPRF(map, td));
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
				if (thing.def.useHitPoints &&
				    !bc.targetBill.hpRange.Includes((float) thing.HitPoints / (float) thing.MaxHitPoints)) {
					continue;
				}

				// Quality
				if (thing.TryGetQuality(out QualityCategory q) && !bc.targetBill.qualityRange.Includes(q))
					continue;

				var producted_thing = bc.targetBill.recipe.ProducedThingDef;
				if (producted_thing != null) {
					// Tainted
					bool can_choose_tainted = producted_thing.IsApparel && producted_thing.apparel.careIfWornByCorpse;
					Apparel a = thing.GetType() == typeof(Apparel) ? (Apparel) thing : null;
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
					things.Add((Thing) equipment);
				}
			}

			foreach (var apparel in pawn.apparel.WornApparel) {
				if (apparel.def.Equals(def)) {
					things.Add((Thing) apparel);
				}
			}

			// TODO: Check this in the future. It doesn't work as of now in spite of multiple tests, and I believe it's a core game problem.
			foreach (Thing heldThing in pawn.inventory.GetDirectlyHeldThings()) {
				if (heldThing.def.Equals(def)) {
					things.Add((Thing) heldThing);
				}
			}

			return things;
		}

		// RimFactory CountProducts Support
		private static List<Thing> GetThingFromPRF(Map map, ThingDef def) {
			List<Thing> things = new List<Thing>();
			// Copied and adapted from PRF Patch_RecipeWorkerCounter_CountProducts
			PRFGameComponent prfGameComponent = Current.Game.GetComponent<PRFGameComponent>();
			for (int index = 0; index < prfGameComponent.AssemblerQueue.Count; ++index) {
				if (map == prfGameComponent.AssemblerQueue[index].Map) {
					foreach (Thing thing in prfGameComponent.AssemblerQueue[index].GetThingQueue()) {
						Thing innerIfMinified = thing.GetInnerIfMinified();
						if (innerIfMinified.def == def)
							things.Add(innerIfMinified);
					}
				}
			}

			foreach (ILinkableStorageParent linkableStorageParent in TradePatchHelper.AllPowered(map)) {
				if (!linkableStorageParent.AdvancedIOAllowed) {
					foreach (Thing storedItem in linkableStorageParent.StoredItems) {
						Thing innerIfMinified = storedItem.GetInnerIfMinified();
						if (innerIfMinified.def == def)
							things.Add(innerIfMinified);
					}
				}
			}

			return things;
		}

		private static bool IsCompositableLoadoutCommand(string command) {
			return CompositableLoadoutTagsFilter.names.Contains(command);
		}
		private static bool GetCompositableLoadoutFilter(string command, BillComponent bc, ref MathFilter filter) {
			if (!CompositableLoadoutTagsFilter.TryFindTagByName(command, out Tag tag))
				return false;
			filter = new CompositableLoadoutTagsFilter(bc, tag);
			return true;
		}

	}
	//public struct SearchVariableReturn {
	//	public bool success;
	//	public float count;
	//	public ReturnType type;

	//	public SearchVariableReturn(ReturnType type, bool success) {
	//		this.type = type;
	//		this.success = success;
	//	}
	//}
}
