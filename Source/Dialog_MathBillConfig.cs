using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;
using Verse.Sound;

namespace CrunchyDuck.Math {
	// After so much patching, I've decided to just completely reimplement the window.
	class Dialog_MathBillConfig : Window {
        private IntVec3 billGiverPos;
        protected Bill_Production bill;
        private ThingFilterUI.UIState thingFilterState = new ThingFilterUI.UIState();
        protected const float RecipeIconSize = 34f;
        [TweakValue("Interface", 0.0f, 400f)]
        private static int RepeatModeSubdialogHeight = 324;
        [TweakValue("Interface", 0.0f, 400f)]
        private static int StoreModeSubdialogHeight = 30;
        [TweakValue("Interface", 0.0f, 400f)]
        private static int WorkerSelectionSubdialogHeight = 85;
        [TweakValue("Interface", 0.0f, 400f)]
        private static int IngredientRadiusSubdialogHeight = 50;
		public BillComponent bc;
        public override Vector2 InitialSize => new Vector2(800f + Settings.textInputAreaBonus, 634f);


        private static List<SpecialThingFilterDef> cachedHiddenSpecialThingFilters;
		private static IEnumerable<SpecialThingFilterDef> HiddenSpecialThingFilters {
			get {
				if (cachedHiddenSpecialThingFilters == null) {
					cachedHiddenSpecialThingFilters = new List<SpecialThingFilterDef>();
					if (ModsConfig.IdeologyActive) {
						cachedHiddenSpecialThingFilters.Add(SpecialThingFilterDefOf.AllowCarnivore);
						cachedHiddenSpecialThingFilters.Add(SpecialThingFilterDefOf.AllowVegetarian);
						cachedHiddenSpecialThingFilters.Add(SpecialThingFilterDefOf.AllowCannibal);
						cachedHiddenSpecialThingFilters.Add(SpecialThingFilterDefOf.AllowInsectMeat);
					}
				}
				return cachedHiddenSpecialThingFilters;
			}
		}

		public Dialog_MathBillConfig(Bill_Production bill, IntVec3 billGiverPos) {
			this.billGiverPos = billGiverPos;
			this.bill = bill;
			bc = BillManager.AddGetBillComponent(bill);
			forcePause = true;
			doCloseX = true;
			doCloseButton = true;
			absorbInputAroundWindow = true;
			closeOnClickedOutside = true;
		}

		protected override void LateWindowOnGUI(Rect inRect) {
			Rect rect = new Rect(inRect.x, inRect.y, 34f, 34f);
			ThingStyleDef thingStyleDef = null;
			if (ModsConfig.IdeologyActive && bill.recipe.ProducedThingDef != null) {
				thingStyleDef = (!bill.globalStyle) ? bill.style : Faction.OfPlayer.ideos.PrimaryIdeo.style.StyleForThingDef(bill.recipe.ProducedThingDef)?.styleDef;
			}
			Widgets.DefIcon(rect, bill.recipe, null, 1f, thingStyleDef, drawPlaceholder: true, null, null, bill.graphicIndexOverride);
		}

		public override void DoWindowContents(Rect inRect) {
			Text.Font = GameFont.Medium;
			Widgets.Label(new Rect(40f, 0.0f, 400f, 34f), this.bill.LabelCap);
			float width = (int)((inRect.width - 34.0) / 3.0);
			Rect rect_left = new Rect(0.0f, 80f, width, inRect.height - 80f);
			Rect rect_middle = new Rect(rect_left.xMax + 17f, 50f, width, inRect.height - 50f - CloseButSize.y);
			Rect rect_right = new Rect(rect_middle.xMax + 17f, 50f, 0.0f, inRect.height - 50f - CloseButSize.y);
			rect_right.xMax = inRect.xMax;
			Text.Font = GameFont.Small;

			// Middle panel
			RenderMiddlePanel(rect_middle);

			// Ingredient panel.
			Rect rect5 = rect_right;
			bool flag = true;
			for (int j = 0; j < bill.recipe.ingredients.Count; j++) {
				if (!bill.recipe.ingredients[j].IsFixedIngredient) {
					flag = false;
					break;
				}
			}
			if (!flag) {
				rect5.yMin = rect5.yMax - (float)IngredientRadiusSubdialogHeight;
				rect_right.yMax = rect5.yMin - 17f;
				bool num = bill.GetStoreZone() == null || bill.recipe.WorkerCounter.CanPossiblyStoreInStockpile(bill, bill.GetStoreZone());
				ThingFilterUI.DoThingFilterConfigWindow(rect_right, thingFilterState, bill.ingredientFilter, bill.recipe.fixedIngredientFilter, 4, null, HiddenSpecialThingFilters.ConcatIfNotNull(bill.recipe.forceHiddenSpecialFilters), forceHideHitPointsConfig: false, bill.recipe.GetPremultipliedSmallIngredients(), bill.Map);
				bool flag2 = bill.GetStoreZone() == null || bill.recipe.WorkerCounter.CanPossiblyStoreInStockpile(bill, bill.GetStoreZone());
				if (num && !flag2) {
					Messages.Message("MessageBillValidationStoreZoneInsufficient".Translate(bill.LabelCap, bill.billStack.billGiver.LabelShort.CapitalizeFirst(), bill.GetStoreZone().label), bill.billStack.billGiver as Thing, MessageTypeDefOf.RejectInput, historical: false);
				}
			}
			else {
				rect5.yMin = 50f;
			}
			// Ingredient search slider.
			Listing_Standard listing_Standard5 = new Listing_Standard();
			listing_Standard5.Begin(rect5);
			string text3 = "IngredientSearchRadius".Translate().Truncate(rect5.width * 0.6f);
			string text4 = ((bill.ingredientSearchRadius == 999f) ? "Unlimited".TranslateSimple().Truncate(rect5.width * 0.3f) : bill.ingredientSearchRadius.ToString("F0"));
			listing_Standard5.Label(text3 + ": " + text4);
			bill.ingredientSearchRadius = listing_Standard5.Slider((bill.ingredientSearchRadius > 100f) ? 100f : bill.ingredientSearchRadius, 3f, 100f);
			if (bill.ingredientSearchRadius >= 100f) {
				bill.ingredientSearchRadius = 999f;
			}
			listing_Standard5.End();

			// Suspended button.
			Listing_Standard listing_Standard6 = new Listing_Standard();
			listing_Standard6.Begin(rect_left);
			if (bill.suspended) {
				if (listing_Standard6.ButtonText("Suspended".Translate())) {
					bill.suspended = false;
					SoundDefOf.Click.PlayOneShotOnCamera();
				}
			}
			else if (listing_Standard6.ButtonText("NotSuspended".Translate())) {
				bill.suspended = true;
				SoundDefOf.Click.PlayOneShotOnCamera();
			}

			// Description + work amount.
			StringBuilder stringBuilder = new StringBuilder();
			if (bill.recipe.description != null) {
				stringBuilder.AppendLine(bill.recipe.description);
				stringBuilder.AppendLine();
			}
			stringBuilder.AppendLine("WorkAmount".Translate() + ": " + bill.recipe.WorkAmountTotal(null).ToStringWorkAmount());
			for (int k = 0; k < bill.recipe.ingredients.Count; k++) {
				IngredientCount ingredientCount = bill.recipe.ingredients[k];
				if (!ingredientCount.filter.Summary.NullOrEmpty()) {
					stringBuilder.AppendLine(bill.recipe.IngredientValueGetter.BillRequirementsDescription(bill.recipe, ingredientCount));
				}
			}
			stringBuilder.AppendLine();
			string text5 = bill.recipe.IngredientValueGetter.ExtraDescriptionLine(bill.recipe);
			if (text5 != null) {
				stringBuilder.AppendLine(text5);
				stringBuilder.AppendLine();
			}
			if (!bill.recipe.skillRequirements.NullOrEmpty()) {
				stringBuilder.AppendLine("MinimumSkills".Translate());
				stringBuilder.AppendLine(bill.recipe.MinSkillString);
			}
			Text.Font = GameFont.Small;
			string text6 = stringBuilder.ToString();
			if (Text.CalcHeight(text6, rect_left.width) > rect_left.height) {
				Text.Font = GameFont.Tiny;
			}
			listing_Standard6.Label(text6);
			Text.Font = GameFont.Small;
			listing_Standard6.End();
			if (bill.recipe.products.Count == 1) {
				ThingDef thingDef = bill.recipe.products[0].thingDef;
				Widgets.InfoCardButton(rect_left.x, rect_right.y, thingDef, GenStuff.DefaultStuffFor(thingDef));
			}
		}

		private void RenderMiddlePanel(Rect rect) {
			Listing_Standard listing_standard = new Listing_Standard();
			listing_standard.Begin(rect);

			RenderBillSettings(listing_standard);
			RenderStockpileSettings(listing_standard);
			RenderWorkerSettings(listing_standard);

			listing_standard.End();
		}

		private void RenderBillSettings(Listing_Standard listing_standard) {
			Listing_Standard listing = listing_standard.BeginSection(RepeatModeSubdialogHeight);
			if (listing.ButtonText(bill.repeatMode.LabelCap))
				BillRepeatModeUtility.MakeConfigFloatMenu(bill);
			listing.Gap();

			TaggedString taggedString1;
			// Repeat count
			if (bill.repeatMode == BillRepeatModeDefOf.RepeatCount) {
				listing.Label("RepeatCount".Translate(bill.repeatCount));
				// TODO: Test this instead of intentry.
				//listing1.TextEntry
				listing.IntEntry(ref bill.repeatCount, ref bc.doXTimes.buffer);
			}

			// Target count
			else if (bill.repeatMode == BillRepeatModeDefOf.TargetCount) {
				string str1 = "CurrentlyHave".Translate() + ": " + bill.recipe.WorkerCounter.CountProducts(bill) + " / ";
				string str2;
				if (bill.targetCount >= 999999) {
					taggedString1 = "Infinite".Translate();
					taggedString1 = taggedString1.ToLower();
					str2 = taggedString1.ToString();
				}
				else
					str2 = bill.targetCount.ToString();
				string label = str1 + str2;
				string str3 = bill.recipe.WorkerCounter.ProductsDescription(bill);
				if (!str3.NullOrEmpty())
					label = label + ("\n" + "CountingProducts".Translate() + ": " + str3.CapitalizeFirst());
				listing.Label(label);
				int targetCount = bill.targetCount;
				listing.IntEntry(ref bill.targetCount, ref bc.doXTimes.buffer, bill.recipe.targetCountAdjustment);
				bill.unpauseWhenYouHave = Mathf.Max(0, bill.unpauseWhenYouHave + (bill.targetCount - targetCount));
				ThingDef producedThingDef = bill.recipe.ProducedThingDef;
				if (producedThingDef != null) {
					if (producedThingDef.IsWeapon || producedThingDef.IsApparel)
						listing.CheckboxLabeled("IncludeEquipped".Translate(), ref bill.includeEquipped);
					if (producedThingDef.IsApparel && producedThingDef.apparel.careIfWornByCorpse)
						listing.CheckboxLabeled("IncludeTainted".Translate(), ref bill.includeTainted);

					// Drop down menu for where to search.
					// TODO: Test this.
					var f = (Func<Bill_Production, IEnumerable<Widgets.DropdownMenuElement<Zone_Stockpile>>>)(b => GenerateStockpileInclusion());
					string button_label = bill.includeFromZone == null ? "IncludeFromAll".Translate() : "IncludeSpecific".Translate(bill.includeFromZone.label);
					Widgets.Dropdown(listing.GetRect(30f), bill, b => b.includeFromZone, f, button_label);

					// Hitpoints slider.
					if (bill.recipe.products.Any<ThingDefCountClass>(prod => prod.thingDef.useHitPoints)) {
						Widgets.FloatRange(listing.GetRect(28f), 975643279, ref bill.hpRange, labelKey: "HitPoints", valueStyle: ToStringStyle.PercentZero);
						bill.hpRange.min = Mathf.Round(bill.hpRange.min * 100f) / 100f;
						bill.hpRange.max = Mathf.Round(bill.hpRange.max * 100f) / 100f;
					}
					// Quality slider.
					if (producedThingDef.HasComp(typeof(CompQuality)))
						Widgets.QualityRange(listing.GetRect(28f), 1098906561, ref bill.qualityRange);

					// Limit material
					if (producedThingDef.MadeFromStuff)
						listing.CheckboxLabeled("LimitToAllowedStuff".Translate(), ref bill.limitToAllowedStuff);
				}
			}

			// Pause when satisfied
			if (bill.repeatMode == BillRepeatModeDefOf.TargetCount) {
				listing.CheckboxLabeled("PauseWhenSatisfied".Translate(), ref bill.pauseWhenSatisfied);
				if (bill.pauseWhenSatisfied) {
					listing.Label("UnpauseWhenYouHave".Translate() + ": " + bc.unpause.CurrentValue.ToString("F0"));
					listing.IntEntry(ref bill.unpauseWhenYouHave, ref bc.unpause.buffer, bill.recipe.targetCountAdjustment);
					//if (bill.unpauseWhenYouHave >= bill.targetCount) {
					//	bill.unpauseWhenYouHave = bill.targetCount - 1;
					//	this.unpauseCountEditBuffer = bill.unpauseWhenYouHave.ToStringCached();
					//}
				}
			}

			listing_standard.EndSection(listing);
			listing_standard.Gap();
		}

		private void RenderStockpileSettings(Listing_Standard listing_standard) {
			// Take to stockpile
			Listing_Standard listing2 = listing_standard.BeginSection(StoreModeSubdialogHeight);
			string label1 = string.Format(bill.GetStoreMode().LabelCap, bill.GetStoreZone() != null ? bill.GetStoreZone().SlotYielderLabel() : "");
			if (bill.GetStoreZone() != null && !bill.recipe.WorkerCounter.CanPossiblyStoreInStockpile(bill, bill.GetStoreZone())) {
				label1 += string.Format(" ({0})", "IncompatibleLower".Translate());
				Text.Font = GameFont.Tiny;
			}
			if (listing2.ButtonText(label1)) {
				Text.Font = GameFont.Small;
				List<FloatMenuOption> options = new List<FloatMenuOption>();
				foreach (BillStoreModeDef billStoreModeDef in DefDatabase<BillStoreModeDef>.AllDefs.OrderBy<BillStoreModeDef, int>(bsm => bsm.listOrder)) {
					if (billStoreModeDef == BillStoreModeDefOf.SpecificStockpile) {
						List<SlotGroup> listInPriorityOrder = bill.billStack.billGiver.Map.haulDestinationManager.AllGroupsListInPriorityOrder;
						int count = listInPriorityOrder.Count;
						for (int index = 0; index < count; ++index) {
							SlotGroup group = listInPriorityOrder[index];
							if (group.parent is Zone_Stockpile parent) {
								if (!bill.recipe.WorkerCounter.CanPossiblyStoreInStockpile(bill, parent))
									options.Add(new FloatMenuOption(string.Format("{0} ({1})", string.Format(billStoreModeDef.LabelCap, group.parent.SlotYielderLabel()), "IncompatibleLower".Translate()), null));
								else
									options.Add(new FloatMenuOption(string.Format(billStoreModeDef.LabelCap, group.parent.SlotYielderLabel()), () => bill.SetStoreMode(BillStoreModeDefOf.SpecificStockpile, (Zone_Stockpile)group.parent)));
							}
						}
					}
					else {
						BillStoreModeDef smLocal = billStoreModeDef;
						options.Add(new FloatMenuOption(smLocal.LabelCap, () => bill.SetStoreMode(smLocal, null)));
					}
				}
				Find.WindowStack.Add(new FloatMenu(options));
			}
			Text.Font = GameFont.Small;
			listing_standard.EndSection(listing2);
			listing_standard.Gap();
		}

		private void RenderWorkerSettings(Listing_Standard listing_standard) {
			// Worker restriction
			Listing_Standard listing = listing_standard.BeginSection(WorkerSelectionSubdialogHeight);

			// Here's what the original code for this looked like, so you can see how much shit I went through for this.
			// string buttonLabel = this.bill.PawnRestriction == null ? (!ModsConfig.IdeologyActive || !this.bill.SlavesOnly ? (!ModsConfig.BiotechActive || !this.bill.recipe.mechanitorOnlyRecipe ? (!ModsConfig.BiotechActive || !this.bill.MechsOnly ? (string)"AnyWorker".Translate() : (string)"AnyMech".Translate()) : (string)"AnyMechanitor".Translate()) : (string)"AnySlave".Translate()) : this.bill.PawnRestriction.LabelShortCap;
			string button_label;
			if (bill.PawnRestriction != null)
				button_label = bill.PawnRestriction.LabelShortCap;
			else if (ModsConfig.IdeologyActive && bill.SlavesOnly)
				button_label = "AnySlave".Translate();
			else if (ModsConfig.BiotechActive && bill.recipe.mechanitorOnlyRecipe)
				button_label = "AnyMechanitor".Translate();
			else if (ModsConfig.BiotechActive && bill.MechsOnly)
				button_label = "AnyMech".Translate();
			else
				button_label = "AnyWorker".Translate();

			// Worker restriction dropdown.
			var f = (Func<Bill_Production, IEnumerable<Widgets.DropdownMenuElement<Pawn>>>)(b => this.GeneratePawnRestrictionOptions());
			Widgets.Dropdown<Bill_Production, Pawn>(listing.GetRect(30f), bill, b => b.PawnRestriction, f, button_label);
			
			// Worker skill restriction.
			if (bill.PawnRestriction == null && bill.recipe.workSkill != null && !bill.MechsOnly) {
				listing.Label("AllowedSkillRange".Translate(bill.recipe.workSkill.label));
				listing.IntRange(ref bill.allowedSkillRange, 0, 20);
			}
			listing_standard.EndSection(listing);
		}

		// The dotpeek version of these functions were... irrecoverable. Praise ILSpy.
		private IEnumerable<Widgets.DropdownMenuElement<Zone_Stockpile>> GenerateStockpileInclusion() {
			yield return new Widgets.DropdownMenuElement<Zone_Stockpile> {
				option = new FloatMenuOption("IncludeFromAll".Translate(), delegate
				{
					bill.includeFromZone = null;
				}),
				payload = null
			};
			List<SlotGroup> groupList = bill.billStack.billGiver.Map.haulDestinationManager.AllGroupsListInPriorityOrder;
			int groupCount = groupList.Count;
			int i = 0;
			while (i < groupCount) {
				SlotGroup slotGroup = groupList[i];
				Zone_Stockpile stockpile = slotGroup.parent as Zone_Stockpile;
				if (stockpile != null) {
					if (!bill.recipe.WorkerCounter.CanPossiblyStoreInStockpile(bill, stockpile)) {
						yield return new Widgets.DropdownMenuElement<Zone_Stockpile> {
							option = new FloatMenuOption(string.Format("{0} ({1})", "IncludeSpecific".Translate(slotGroup.parent.SlotYielderLabel()), "IncompatibleLower".Translate()), null),
							payload = stockpile
						};
					}
					else {
						yield return new Widgets.DropdownMenuElement<Zone_Stockpile> {
							option = new FloatMenuOption("IncludeSpecific".Translate(slotGroup.parent.SlotYielderLabel()), delegate
							{
								bill.includeFromZone = stockpile;
							}),
							payload = stockpile
						};
					}
				}
				int num = i + 1;
				i = num;
			}
		}

		private IEnumerable<Widgets.DropdownMenuElement<Pawn>> GeneratePawnRestrictionOptions() {
			yield return new Widgets.DropdownMenuElement<Pawn> {
				option = new FloatMenuOption("AnyWorker".Translate(), delegate
				{
					bill.SetAnyPawnRestriction();
				}),
				payload = null
			};
			if (ModsConfig.IdeologyActive) {
				yield return new Widgets.DropdownMenuElement<Pawn> {
					option = new FloatMenuOption("AnySlave".Translate(), delegate
					{
						bill.SetAnySlaveRestriction();
					}),
					payload = null
				};
			}
			SkillDef workSkill = bill.recipe.workSkill;
			IEnumerable<Pawn> allMaps_FreeColonists = PawnsFinder.AllMaps_FreeColonists;
			allMaps_FreeColonists = allMaps_FreeColonists.OrderBy((Pawn pawn) => pawn.LabelShortCap);
			if (workSkill != null) {
				allMaps_FreeColonists = allMaps_FreeColonists.OrderByDescending((Pawn pawn) => pawn.skills.GetSkill(bill.recipe.workSkill).Level);
			}
			WorkGiverDef workGiver = bill.billStack.billGiver.GetWorkgiver();
			if (workGiver == null) {
				Log.ErrorOnce("Generating pawn restrictions for a BillGiver without a Workgiver", 96455148);
				yield break;
			}
			allMaps_FreeColonists = allMaps_FreeColonists.OrderByDescending((Pawn pawn) => pawn.workSettings.WorkIsActive(workGiver.workType));
			allMaps_FreeColonists = allMaps_FreeColonists.OrderBy((Pawn pawn) => pawn.WorkTypeIsDisabled(workGiver.workType));
			foreach (Pawn pawn2 in allMaps_FreeColonists) {
				if (pawn2.WorkTypeIsDisabled(workGiver.workType)) {
					yield return new Widgets.DropdownMenuElement<Pawn> {
						option = new FloatMenuOption(string.Format("{0} ({1})", pawn2.LabelShortCap, "WillNever".Translate(workGiver.verb)), null),
						payload = pawn2
					};
				}
				else if (bill.recipe.workSkill != null && !pawn2.workSettings.WorkIsActive(workGiver.workType)) {
					yield return new Widgets.DropdownMenuElement<Pawn> {
						option = new FloatMenuOption(string.Format("{0} ({1} {2}, {3})", pawn2.LabelShortCap, pawn2.skills.GetSkill(bill.recipe.workSkill).Level, bill.recipe.workSkill.label, "NotAssigned".Translate()), delegate
						{
							bill.SetPawnRestriction(pawn2);
						}),
						payload = pawn2
					};
				}
				else if (!pawn2.workSettings.WorkIsActive(workGiver.workType)) {
					yield return new Widgets.DropdownMenuElement<Pawn> {
						option = new FloatMenuOption(string.Format("{0} ({1})", pawn2.LabelShortCap, "NotAssigned".Translate()), delegate
						{
							bill.SetPawnRestriction(pawn2);
						}),
						payload = pawn2
					};
				}
				else if (bill.recipe.workSkill != null) {
					yield return new Widgets.DropdownMenuElement<Pawn> {
						option = new FloatMenuOption($"{pawn2.LabelShortCap} ({pawn2.skills.GetSkill(bill.recipe.workSkill).Level} {bill.recipe.workSkill.label})", delegate
						{
							bill.SetPawnRestriction(pawn2);
						}),
						payload = pawn2
					};
				}
				else {
					yield return new Widgets.DropdownMenuElement<Pawn> {
						option = new FloatMenuOption($"{pawn2.LabelShortCap}", delegate
						{
							bill.SetPawnRestriction(pawn2);
						}),
						payload = pawn2
					};
				}
			}
		}
	}
}
