using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Verse;

namespace CrunchyDuck.Math.ModCompat {
	public class RimFactorySupport {

		private static Type PrfGameCompType { get; } = Type.GetType("ProjectRimFactory.PRFGameComponent, ProjectRimFactory");
		private static Type PrfMapCompType { get; } = Type.GetType("ProjectRimFactory.Common.PRFMapComponent, ProjectRimFactory");
		private static Type PrfAssemblerQueueType { get; } = Type.GetType("ProjectRimFactory.Common.HarmonyPatches.IAssemblerQueue, ProjectRimFactory");
		private static Type PrfPatchStorageUtilType { get; } = Type.GetType("ProjectRimFactory.Common.HarmonyPatches.PatchStorageUtil, ProjectRimFactory");
		private static Type PrfILinkableStorageParentType { get; } = Type.GetType("ProjectRimFactory.Storage.ILinkableStorageParent, ProjectRimFactory");
		private static AccessTools.FieldRef<GameComponent, IReadOnlyList<object>> PrfGameCompAssemblerQueue = AccessTools.FieldRefAccess<IReadOnlyList<object>>(PrfGameCompType, "AssemblerQueue");
		private static AccessTools.FieldRef<MapComponent, IReadOnlyList<object>> PrfMapCompColdStorageBuildings = AccessTools.FieldRefAccess<IReadOnlyList<object>>(PrfMapCompType, "ColdStorageBuildings");
		
		private static dynamic PrfStorageParentAdvancedIOAllowed = AccessTools.PropertyGetter(PrfILinkableStorageParentType, "AdvancedIOAllowed")
			.CreateDelegate(Expression.GetDelegateType(PrfILinkableStorageParentType, typeof(bool)));
		
		private static dynamic PrfStorageParentStoredItems = AccessTools.PropertyGetter(PrfILinkableStorageParentType, "StoredItems")
			.CreateDelegate(Expression.GetDelegateType(PrfILinkableStorageParentType, typeof(List<Thing>)));
		
		private static dynamic PrfAssemblerQueueMap = AccessTools.PropertyGetter(PrfAssemblerQueueType, "Map")
			.CreateDelegate(Expression.GetDelegateType(PrfAssemblerQueueType, typeof(Map)));
		
		private static dynamic PrfGetAssemblerThingQueue = AccessTools.Method(PrfAssemblerQueueType, "GetThingQueue", Type.EmptyTypes)
			.CreateDelegate(Expression.GetDelegateType(PrfAssemblerQueueType, typeof(List<Thing>)));

		private static Func<Map, MapComponent> GetPrfMapComp = AccessTools.MethodDelegate<Func<Map, MapComponent>>(
			AccessTools.Method(PrfPatchStorageUtilType, "GetPRFMapComponent", new[] { typeof(Map) }));
		private static GameComponent GetPrfGameComp() => Current.Game.GetComponent(PrfGameCompType);
		
		// RimFactory CountProducts Support
		public static List<Thing> GetThingsFromPRF(Map map, ThingDef def) {
			// Adapted from PRF Patch_RecipeWorkerCounter_CountProducts
			GameComponent prfGameComponent = GetPrfGameComp();
			IEnumerable<Thing> assemblerQueuedThings = PrfGameCompAssemblerQueue(prfGameComponent)
				.Where(assembler => PrfAssemblerQueueMap(assembler) == map)
				.SelectMany(assembler => (IEnumerable<Thing>)PrfGetAssemblerThingQueue(assembler));

			
			MapComponent prfMapComp = GetPrfMapComp(map);
			IEnumerable<Thing> coldStoredThings = PrfMapCompColdStorageBuildings(prfMapComp)
				.Where(storageParent => !PrfStorageParentAdvancedIOAllowed(storageParent))
				.SelectMany(storageParent => (IEnumerable<Thing>)PrfStorageParentStoredItems(storageParent));

			return assemblerQueuedThings.Concat(coldStoredThings)
				.Select(thing => thing.GetInnerIfMinified())
				.Where(thing => thing.def == def)
				.ToList();
		}
	}
}
