using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace CrunchyDuck.Math.ModCompat {
	public class RimFactorySupport {

		private static Type PrfGameCompType { get; } = Type.GetType("ProjectRimFactory.PRFGameComponent, ProjectRimFactory");
		private static Type PrfMapCompType { get; } = Type.GetType("ProjectRimFactory.Common.PRFMapComponent, ProjectRimFactory");
		private static Type PrfAssemblerQueueType { get; } = Type.GetType("ProjectRimFactory.Common.HarmonyPatches.IAssemblerQueue, ProjectRimFactory");
		private static Type PrfPatchStorageUtilType { get; } = Type.GetType("ProjectRimFactory.Common.HarmonyPatches.PatchStorageUtil, ProjectRimFactory");
		private static Type PrfILinkableStorageParentType { get; } = Type.GetType("ProjectRimFactory.Storage.ILinkableStorageParent, ProjectRimFactory");
		
		private static readonly AccessTools.FieldRef<GameComponent, IReadOnlyList<object>> PrfGameCompAssemblerQueue = AccessTools.FieldRefAccess<IReadOnlyList<object>>(PrfGameCompType, "AssemblerQueue");
		private static readonly AccessTools.FieldRef<MapComponent, IReadOnlyList<object>> PrfMapCompColdStorageBuildings = AccessTools.FieldRefAccess<IReadOnlyList<object>>(PrfMapCompType, "ColdStorageBuildings");
		private static readonly PropertyInfo PrfStorageParentAdvancedIoAllowed = AccessTools.Property(PrfILinkableStorageParentType, "AdvancedIOAllowed");
		private static readonly PropertyInfo PrfStorageParentStoredItems = AccessTools.Property(PrfILinkableStorageParentType, "StoredItems");
		private static readonly PropertyInfo PrfAssemblerQueueMap = AccessTools.Property(PrfAssemblerQueueType, "Map");
		private static readonly MethodInfo PrfGetAssemblerThingQueue = AccessTools.Method(PrfAssemblerQueueType, "GetThingQueue", Type.EmptyTypes);
		private static readonly Func<Map, MapComponent> GetPrfMapComp = AccessTools.MethodDelegate<Func<Map, MapComponent>>(
			AccessTools.Method(PrfPatchStorageUtilType, "GetPRFMapComponent", new[] { typeof(Map) }));
		private static GameComponent GetPrfGameComp() => Current.Game.GetComponent(PrfGameCompType);
		
		// RimFactory CountProducts Support
		public static List<Thing> GetThingsFromPRF(Map map, ThingDef def) {
			// Adapted from PRF Patch_RecipeWorkerCounter_CountProducts
			GameComponent prfGameComponent = GetPrfGameComp();
			IEnumerable<Thing> assemblerQueuedThings = PrfGameCompAssemblerQueue(prfGameComponent)
				.Where(assembler => PrfAssemblerQueueMap.GetValue(assembler) == map)
				.SelectMany(assembler => (IEnumerable<Thing>)PrfGetAssemblerThingQueue.Invoke(assembler, null));

			
			MapComponent prfMapComp = GetPrfMapComp(map);
			IEnumerable<Thing> coldStoredThings = PrfMapCompColdStorageBuildings(prfMapComp)
				.Where(storageParent => !(bool)PrfStorageParentAdvancedIoAllowed.GetValue(storageParent))
				.SelectMany(storageParent => (IEnumerable<Thing>)PrfStorageParentStoredItems.GetValue(storageParent));

			return assemblerQueuedThings.Concat(coldStoredThings)
				.Select(thing => thing.GetInnerIfMinified())
				.Where(thing => thing.def == def)
				.ToList();
		}
	}
}
