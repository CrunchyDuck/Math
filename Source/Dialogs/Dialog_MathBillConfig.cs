﻿using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;
using Verse.Sound;
using HarmonyLib;

namespace CrunchyDuck.Math {
	// After so much patching, I've decided to just completely reimplement the window.
	class Dialog_MathBillConfig : Window {
		// This is only for drawing the ingredient search radius for now. Not implemented.
		IntVec3 billGiverPos;
		public Bill_Production bill;
        private ThingFilterUI.UIState thingFilterState = new ThingFilterUI.UIState();
        protected const float RecipeIconSize = 34f;
        [TweakValue("Interface", 0.0f, 400f)]
        private static int RepeatModeSubdialogHeight = 324 + 100;
        [TweakValue("Interface", 0.0f, 400f)]
        private static int StoreModeSubdialogHeight = 30;
        [TweakValue("Interface", 0.0f, 400f)]
        private static int WorkerSelectionSubdialogHeight = 85;
        [TweakValue("Interface", 0.0f, 400f)]
        private static int IngredientRadiusSubdialogHeight = 50;
		public BillComponent bc;
        public override Vector2 InitialSize => new Vector2(800f + MathSettings.settings.textInputAreaBonus, 634f + 100f);
		public Vector2 linkSettingsScrollPos = Vector2.zero;
		public const int LinkSettingsHeight = 150;
		public const int ScrollBarWidth = 16;
		public TreeNode linkSettingsMaster;
		public float BottomAreaHeight { get { return CloseButSize.y + 18; } }

		private float extraPanelAllocation = MathSettings.settings.textInputAreaBonus / 3;

		private float infoHoverHue = 0;
		private float hueSpeed = 1f / (60f * 5f);

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
			bc = BillManager.instance.AddGetBillComponent(bill);
			// I was pretty sure that buffer was set to lastValid *somewhere else*, but I can't find it anywhere in the code. Bad sign for my cleanliness.
			bc.doUntilX.buffer = bc.doUntilX.lastValid;
			bc.unpause.buffer = bc.unpause.lastValid;
			bc.doXTimes.buffer = bc.doXTimes.lastValid;
			bc.itemsToCount.buffer = bc.itemsToCount.lastValid;
			linkSettingsMaster = GetLinkSettingsTree();

			forcePause = true;
			doCloseX = true;
			doCloseButton = true;
			absorbInputAroundWindow = true;
			closeOnClickedOutside = true;
		}

		public override void WindowUpdate() => bill.TryDrawIngredientSearchRadiusOnMap(billGiverPos);

		public override void LateWindowOnGUI(Rect inRect) {
			Rect rect = new Rect(inRect.x, inRect.y, 34f, 34f);
			ThingStyleDef thingStyleDef = null;
			if (ModsConfig.IdeologyActive && bill.recipe.ProducedThingDef != null) {
				thingStyleDef = (!bill.globalStyle) ? bill.style : Faction.OfPlayer.ideos.PrimaryIdeo.style.StyleForThingDef(bill.recipe.ProducedThingDef)?.styleDef;
			}
			Widgets.DefIcon(rect, bill.recipe, null, 1f, thingStyleDef, drawPlaceholder: true, null, null, bill.graphicIndexOverride);
		}

		public override void DoWindowContents(Rect inRect) {
			BillMenuData.AssignTo(this, inRect);

			// This ensures that the map cache is updated.
			// Since the game is paused, this won't cause lag.
			if (RealTime.frameCount % 60 == 0) {
				Math.ClearCacheMaps();
				BillManager.UpdateBill(bc);
			}

			float width = (int)((inRect.width - 34.0) / 3.0);
			Rect rect_left = new Rect(0.0f, 80f, width - extraPanelAllocation, inRect.height - 80f);
			Rect rect_middle = new Rect(rect_left.xMax + 17f, 50f, width + (extraPanelAllocation * 2), inRect.height - 50f - CloseButSize.y);
			Rect rect_right = new Rect(rect_middle.xMax + 17f, 50f, 0.0f, inRect.height - 50f - CloseButSize.y);
			rect_right.xMax = inRect.xMax;

			Text.Font = GameFont.Medium;
			bc.name = Widgets.TextField(new Rect(40f, 0.0f, 400f, 34f), bc.name);
			Text.Font = GameFont.Small;

			// Middle panel.
			RenderMiddlePanel(rect_middle);

			// Ingredient panel.
			RenderIngredients(rect_right);

			// Bill info panel.
			RenderLeftPanel(rect_left);

			var buttons_x = rect_left.x;
			// infocard button.
			if (bill.recipe.products.Count == 1) {
				ThingDef thingDef = bill.recipe.products[0].thingDef;
				Widgets.InfoCardButton(buttons_x, rect_right.y, thingDef, GenStuff.DefaultStuffFor(thingDef));
				buttons_x += 24 + 4;
			}

			Rect button_rect = new Rect(buttons_x, rect_right.y, 24, 24);
			// Variables button
			TooltipHandler.TipRegion(button_rect, "CD.M.tooltips.user_variables".Translate());
			if (Widgets.ButtonImage(button_rect, Resources.variablesButtonImage, Color.white)) {
				Find.WindowStack.Add(new Dialog_VariableList(bc));
			}
			button_rect.x += 24 + 4;

			// math info button.
			infoHoverHue = (infoHoverHue + hueSpeed) % 1f;
			Color gay_color = Color.HSVToRGB(infoHoverHue, 1, 1);
			Color color = GUI.color;
			if (Math.IsNewImportantVersion())
				color = gay_color;
			if (Widgets.ButtonImage(button_rect, Resources.infoButtonImage, color, gay_color)) {
				Find.WindowStack.Add(new Dialog_MathInfoCard(bc));
			}
			button_rect.x += 24 + 4;

			BillMenuData.Unassign();
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

			// Repeat count
			if (bill.repeatMode == BillRepeatModeDefOf.RepeatCount) {
				listing.Label("RepeatCount".Translate(bill.repeatCount) + " " + bc.doXTimes.CurrentValue);
				MathBillEntry(bc.doXTimes, listing);
			}

			// Target count
			else if (bill.repeatMode == BillRepeatModeDefOf.TargetCount) {
				// Currently have label
				string currently_have = "CurrentlyHave".Translate() + ": " + bill.recipe.WorkerCounter.CountProducts(bill) + " / ";
				string out_of;
				if (bill.targetCount >= 999999) {
					TaggedString taggedString1;
					taggedString1 = "Infinite".Translate();
					taggedString1 = taggedString1.ToLower();
					out_of = taggedString1.ToString();
				}
				else
					out_of = bill.targetCount.ToString();
				string label = currently_have + out_of;
				//string str3 = bill.recipe.WorkerCounter.ProductsDescription(bill);
				//if (!str3.NullOrEmpty())
				//	label = label + ("\n" + "CountingProducts".Translate() + ": " + str3.CapitalizeFirst());
				listing.Label(label);

				// Counted items checkbox/field
				Listing_Standard item_count_listing = new Listing_Standard();
				item_count_listing.Begin(listing.GetRect(24f));
				item_count_listing.ColumnWidth = item_count_listing.ColumnWidth / 2 - 10;
				item_count_listing.CheckboxLabeled("Custom item count", ref bc.customItemsToCount);
				item_count_listing.NewColumn();
				if (bc.customItemsToCount) {
					Rect rect = item_count_listing.GetRect(24f);
					MathTextField(bc.itemsToCount, rect);
				}
				item_count_listing.End();

				listing.Label("Target value: " + bc.doUntilX.CurrentValue);
				MathBillEntry(bc.doUntilX, listing, bill.recipe.targetCountAdjustment);

				ThingDef producedThingDef = bill.recipe.ProducedThingDef;
				if (producedThingDef != null) {
					Listing_Standard equipped_tainted_listing = new Listing_Standard();
					equipped_tainted_listing.Begin(listing.GetRect(24f));
					equipped_tainted_listing.ColumnWidth = equipped_tainted_listing.ColumnWidth / 2 - 10;
					// Equipped check-box
					if (producedThingDef.IsWeapon || producedThingDef.IsApparel)
						equipped_tainted_listing.CheckboxLabeled("IncludeEquipped".Translate(), ref bill.includeEquipped);

					// Tainted check-box
					equipped_tainted_listing.NewColumn();
					if (producedThingDef.IsApparel && producedThingDef.apparel.careIfWornByCorpse)
						equipped_tainted_listing.CheckboxLabeled("IncludeTainted".Translate(), ref bill.includeTainted);
					equipped_tainted_listing.End();

					// Drop down menu for where to search.
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
					MathBillEntry(bc.unpause, listing, bill.recipe.targetCountAdjustment);
					//listing.IntEntry(ref bill.unpauseWhenYouHave, ref bc.unpause.buffer, bill.recipe.targetCountAdjustment);
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
				foreach (BillStoreModeDef billStoreModeDef in DefDatabase<BillStoreModeDef>.AllDefs.OrderBy(bsm => bsm.listOrder)) {
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
			// Someone was having issues with this method not being in their game. This will stop them getting errors. It can probably be removed eventually.
			bool non_mech = false;
			try {
				non_mech = bill.NonMechsOnly;
			}
			catch {}

			if (bill.PawnRestriction != null)
				button_label = bill.PawnRestriction.LabelShortCap;
			else if (ModsConfig.IdeologyActive && bill.SlavesOnly)
				button_label = "AnySlave".Translate();
			else if (ModsConfig.BiotechActive && bill.recipe.mechanitorOnlyRecipe)
				button_label = "AnyMechanitor".Translate();
			else if (ModsConfig.BiotechActive && bill.MechsOnly)
				button_label = "AnyMech".Translate();
			else if (ModsConfig.BiotechActive && non_mech)
				button_label = "AnyNonMech".Translate();
			else
				button_label = "AnyWorker".Translate();

			// Worker restriction dropdown.
			var f = (Func<Bill_Production, IEnumerable<Widgets.DropdownMenuElement<Pawn>>>)(b => GeneratePawnRestrictionOptions());
			Widgets.Dropdown(listing.GetRect(30f), bill, b => b.PawnRestriction, f, button_label);
			
			// Worker skill restriction.
			if (bill.PawnRestriction == null && bill.recipe.workSkill != null && !bill.MechsOnly) {
				listing.Label("AllowedSkillRange".Translate(bill.recipe.workSkill.label));
				listing.IntRange(ref bill.allowedSkillRange, 0, 20);
			}
			listing_standard.EndSection(listing);
		}

		private void RenderIngredients(Rect rect_right) {
			Rect rect5 = rect_right;
			bool only_fixed_ingredients = true;
			for (int j = 0; j < bill.recipe.ingredients.Count; j++) {
				if (!bill.recipe.ingredients[j].IsFixedIngredient) {
					only_fixed_ingredients = false;
					break;
				}
			}
			if (!only_fixed_ingredients) {
				rect5.yMin = rect5.yMax - IngredientRadiusSubdialogHeight;
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
		}

		private void RenderLeftPanel(Rect rect_left) {
			// Suspended button.
			Listing_Standard ls = new Listing_Standard();
			ls.Begin(rect_left);
			if (bill.suspended) {
				if (ls.ButtonText("Suspended".Translate())) {
					bill.suspended = false;
					SoundDefOf.Click.PlayOneShotOnCamera();
				}
			}
			else if (ls.ButtonText("NotSuspended".Translate())) {
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
			ls.Label(text6);
			Text.Font = GameFont.Small;
			ls.End();

			// Link options
			if (bc.linkTracker.parent != null)
				RenderLinkSettings(new Rect(rect_left.x, rect_left.yMax - BottomAreaHeight - LinkSettingsHeight, rect_left.width, LinkSettingsHeight));
		}

		private void RenderLinkSettings(Rect render_area) {
			Widgets.DrawMenuSection(render_area);

			render_area = render_area.ContractedBy(4);

			Vector2 row_size = new Vector2(render_area.width - ScrollBarWidth, 30);
			int checkboxes = 4;
			//Widgets.CheckboxLabeled();
			float scroll_area_total_height = checkboxes * row_size.y;
			Rect scroll_area = new Rect(0.0f, 0.0f, render_area.width - ScrollBarWidth, scroll_area_total_height);

			// Code heavily inspired by ThingFilterUI.DoThingFilterConfigWindow
			Widgets.BeginScrollView(render_area, ref linkSettingsScrollPos, scroll_area);
			// TODO: Change this to be the click-and-drag type of checkboxes.
			Listing_Tree lt = new Listing_Tree();
			lt.Begin(scroll_area);
			foreach (Generic_TreeNode node in linkSettingsMaster.children) {
				node.Render(lt, 0);
			}
			lt.End();
			Widgets.EndScrollView();
		}

		private static void MathBillEntry(InputField field, Listing_Standard ls, int multiplier = 1) {
			Rect rect = ls.GetRect(24f);
			// Not sure what this if check does.
			if (!ls.BoundingRectCached.HasValue || rect.Overlaps(ls.BoundingRectCached.Value)) {
				// Buttons
				int num = Mathf.Min(40, (int)rect.width / 5);
				if (Widgets.ButtonText(new Rect(rect.xMin, rect.yMin, num, rect.height), (-10 * multiplier).ToStringCached())) {
					field.SetAll(Mathf.CeilToInt(field.CurrentValue) - 10 * multiplier * GenUI.CurrentAdjustmentMultiplier());
					SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
				}
				if (Widgets.ButtonText(new Rect(rect.xMin + num, rect.yMin, num, rect.height), (-1 * multiplier).ToStringCached())) {
					field.SetAll(Mathf.CeilToInt(field.CurrentValue) - multiplier * GenUI.CurrentAdjustmentMultiplier());
					SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
				}
				if (Widgets.ButtonText(new Rect(rect.xMax - num, rect.yMin, num, rect.height), "+" + (10 * multiplier).ToStringCached())) {
					field.SetAll(Mathf.CeilToInt(field.CurrentValue) + 10 * multiplier * GenUI.CurrentAdjustmentMultiplier());
					SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
				}
				if (Widgets.ButtonText(new Rect(rect.xMax - num * 2, rect.yMin, num, rect.height), "+" + multiplier.ToStringCached())) {
					field.SetAll(Mathf.CeilToInt(field.CurrentValue) + multiplier * GenUI.CurrentAdjustmentMultiplier());
					SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
				}
				// Text field
				Rect input_rect = new Rect(rect.xMin + num * 2, rect.yMin, rect.width - num * 4, rect.height);
				MathTextField(field, input_rect);
			}

			ls.Gap(ls.verticalSpacing);
		}

		private static void MathTextField(InputField field, Rect area) {
			Color original_col = GUI.color;
			// Invalid input.
			if (!Math.DoMath(field.buffer, field))
				GUI.color = new Color(1, 0, 0, 0.8f);
			// Valid input.
			else
				field.SetAll(field.buffer, field.CurrentValue);
			field.buffer = Widgets.TextField(area, field.buffer);
			GUI.color = original_col;
		}

		// The dotpeek version of these functions were... irrecoverable. Praise ILSpy.
		private IEnumerable<Widgets.DropdownMenuElement<Zone_Stockpile>> GenerateStockpileInclusion() {
			// All stockpiles.
			yield return new Widgets.DropdownMenuElement<Zone_Stockpile> {
				option = new FloatMenuOption("IncludeFromAll".Translate(), delegate
				{
					bill.includeFromZone = null;
				}),
				payload = null
			};

			// Individual stockpiles.
			List<SlotGroup> groupList = bill.billStack.billGiver.Map.haulDestinationManager.AllGroupsListInPriorityOrder;
			int groupCount = groupList.Count;
			int i = 0;
			while (i < groupCount) {
				SlotGroup slotGroup = groupList[i];
				if (slotGroup.parent is Zone_Stockpile stockpile) {
					if (!bill.recipe.WorkerCounter.CanPossiblyStoreInStockpile(bill, stockpile)) {
						yield return new Widgets.DropdownMenuElement<Zone_Stockpile> {
							option = new FloatMenuOption(string.Format("{0} ({1})", "IncludeSpecific".Translate(slotGroup.parent.SlotYielderLabel()), "IncompatibleLower".Translate()), null),
							payload = stockpile
						};
					}
					else {
						yield return new Widgets.DropdownMenuElement<Zone_Stockpile> {
							option = new FloatMenuOption("IncludeSpecific".Translate(slotGroup.parent.SlotYielderLabel()), delegate {
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
		
		protected virtual IEnumerable<Widgets.DropdownMenuElement<Pawn>> GeneratePawnRestrictionOptions() {
			if (ModsConfig.BiotechActive && bill.recipe.mechanitorOnlyRecipe) {
				// Mechanitor category
				yield return new Widgets.DropdownMenuElement<Pawn> {
					option = new FloatMenuOption("AnyMechanitor".Translate(), delegate { bill.SetAnyPawnRestriction(); }),
					payload = null
				};
				// Mechanitor pawns
				foreach (Widgets.DropdownMenuElement<Pawn> item in BillDialogUtility.GetPawnRestrictionOptionsForBill(bill, (Pawn p) => MechanitorUtility.IsMechanitor(p))) {
					yield return item;
				}
				yield break;
			}
			// Any worker category
			yield return new Widgets.DropdownMenuElement<Pawn> {
				option = new FloatMenuOption("AnyWorker".Translate(), delegate
				{
					bill.SetAnyPawnRestriction();
				}),
				payload = null
			};
			// Any slave category
			if (ModsConfig.IdeologyActive) {
				yield return new Widgets.DropdownMenuElement<Pawn> {
					option = new FloatMenuOption("AnySlave".Translate(), delegate
					{
						bill.SetAnySlaveRestriction();
					}),
					payload = null
				};
			}
			// Any mech category
			if (ModsConfig.BiotechActive && MechWorkUtility.AnyWorkMechCouldDo(bill.recipe)) {
				yield return new Widgets.DropdownMenuElement<Pawn> {
					option = new FloatMenuOption("AnyMech".Translate(), delegate
					{
						bill.SetAnyMechRestriction();
					}),
					payload = null
				};
				yield return new Widgets.DropdownMenuElement<Pawn> {
					option = new FloatMenuOption("AnyNonMech".Translate(), delegate {
						bill.SetAnyNonMechRestriction();
					}),
					payload = null
				};
			}
			// Pawns
			foreach (Widgets.DropdownMenuElement<Pawn> item2 in BillDialogUtility.GetPawnRestrictionOptionsForBill(bill)) {
				yield return item2;
			}
		}
	
		private TreeNode GetLinkSettingsTree() {
			/// BEWARE, YE
			/// This code is a beautiful, dark hole.
			/// Cryptic indeed, arcane mayhaps, and artfully blunt.
			/// Dare not comprehend.

			// Just a container, never displayed.
			var master = new TreeNode();
			master.children = new List<TreeNode>();
			BillLinkTracker lt = bc.linkTracker;
			Action<Generic_TreeNode, bool> category_on_act = (node, value) => {
				foreach (Generic_TreeNode child in node.children) {
					child.ToggleAction(child, value);
				}
			};
			Func<Generic_TreeNode, MultiCheckboxState> category_check_state = node => {
				bool any_on = false;
				bool any_off = false;
				foreach (Generic_TreeNode child in node.children) {
					var res = child.CheckState(child);
					if (res == MultiCheckboxState.Partial)
						return MultiCheckboxState.Partial;
					else if (res == MultiCheckboxState.On)
						any_on = true;
					else
						any_off = true;

					if (any_on && any_off)
						return MultiCheckboxState.Partial;
				}
				if (any_on)
					return MultiCheckboxState.On;
				return MultiCheckboxState.Off;
			};
			Action<Generic_TreeNode, bool> on_act;
			Func<Generic_TreeNode, MultiCheckboxState> check_state;

			// Ingredients cat
			var link_ingredients = new Generic_TreeNode("Link ingredients", "", category_on_act, category_check_state);
			// Ingredients
			on_act = (node, b) => bc.linkTracker.linkIngredients = b;
			check_state = (node) => CheckboxState(bc.linkTracker.linkIngredients);
			link_ingredients.children.Add(new Generic_TreeNode("Ingredients", "", on_act, check_state));
			// Ingredients radius
			on_act = (node, b) => bc.linkTracker.linkIngredientsRadius = b;
			check_state = (node) => CheckboxState(bc.linkTracker.linkIngredientsRadius);
			link_ingredients.children.Add(new Generic_TreeNode("Ingredient radius", "", on_act, check_state));

			master.children.Add(link_ingredients);

			// Input settings
			Generic_TreeNode input_settings = new Generic_TreeNode("Link input fields", "", category_on_act, category_check_state);
			// Target count
			on_act = (node, b) => lt.linkTargetCount = b;
			check_state = (node) => CheckboxState(lt.linkTargetCount);
			input_settings.children.Add(new Generic_TreeNode("Target count", "", on_act, check_state));
			// Custom Item Count
			on_act = (node, b) => lt.linkCustomItemCount = b;
			check_state = (node) => CheckboxState(lt.linkCustomItemCount);
			input_settings.children.Add(new Generic_TreeNode("Target count", "", on_act, check_state));
			// Pause
			on_act = (node, b) => lt.linkPause = b;
			check_state = (node) => CheckboxState(lt.linkPause);
			input_settings.children.Add(new Generic_TreeNode("Target count", "", on_act, check_state));

			// Main settings
			Generic_TreeNode middle_settings = new Generic_TreeNode("Middle panel", "I couldn't think of a better name, sorry.", category_on_act, category_check_state);
			middle_settings.children.Add(input_settings);
			// Tainted
			on_act = (node, b) => lt.linkTainted = b;
			check_state = (node) => CheckboxState(lt.linkTainted);
			middle_settings.children.Add(new Generic_TreeNode("Tainted", "", on_act, check_state));
			// Equipped
			on_act = (node, b) => lt.linkEquipped = b;
			check_state = (node) => CheckboxState(lt.linkEquipped);
			middle_settings.children.Add(new Generic_TreeNode("Equipped", "", on_act, check_state));
			// Only allowed ingredients
			on_act = (node, b) => lt.linkOnlyAllowedIngredients = b;
			check_state = (node) => CheckboxState(lt.linkOnlyAllowedIngredients);
			middle_settings.children.Add(new Generic_TreeNode("Only allowed ingredients", "", on_act, check_state));
			// Hitpoints
			on_act = (node, b) => lt.linkCountHitpoints= b;
			check_state = (node) => CheckboxState(lt.linkCountHitpoints);
			middle_settings.children.Add(new Generic_TreeNode("Hitpoints filter", "", on_act, check_state));
			// Quality
			on_act = (node, b) => lt.linkCountQuality = b;
			check_state = (node) => CheckboxState(lt.linkCountQuality);
			middle_settings.children.Add(new Generic_TreeNode("Quality filter", "", on_act, check_state));
			// Stockpile to check
			on_act = (node, b) => lt.linkTainted = b;
			check_state = (node) => CheckboxState(lt.linkCheckStockpiles);
			middle_settings.children.Add(new Generic_TreeNode("Stockpile to check", "", on_act, check_state));

			master.children.Add(middle_settings);

			return master;
		}

		private static MultiCheckboxState CheckboxState(params bool[] bools) {
			bool any_on = false;
			bool any_off = false;
			foreach (bool b in bools) {
				if (b)
					any_on = true;
				else
					any_off = true;
				if (any_on && any_off)
					return MultiCheckboxState.Partial;
			}

			if (any_on)
				return MultiCheckboxState.On;
			return MultiCheckboxState.Off;
		}
	}

	public class Generic_TreeNode : TreeNode {
		public string name;
		public string tooltip;
		public Action<Generic_TreeNode, bool> ToggleAction;
		public Func<Generic_TreeNode, MultiCheckboxState> CheckState;
		public Func<bool> CheckDisabled;

		public Generic_TreeNode(string name, string tooltip, Action<Generic_TreeNode, bool> toggle_act, Func<Generic_TreeNode, MultiCheckboxState> check_state, Func<bool> check_disabled = null) {
			children = new List<TreeNode>();
			this.name = name;
			this.tooltip = tooltip;
			this.ToggleAction = toggle_act;
			this.CheckState = check_state;
			this.CheckDisabled = check_disabled;
		}

		public void Render(Listing_Tree lt, int indentation_level) {
			if (children.Count != 0) {
				RenderLine(lt, indentation_level);
				lt.OpenCloseWidget(this, indentation_level, 1);
				lt.EndLine();
				if (IsOpen(1)) {
					foreach (Generic_TreeNode node in children)
						node.Render(lt, indentation_level + 1);
				}
				return;
			}
			RenderLine(lt, indentation_level);
			lt.EndLine();
		}

		public void RenderLine(Listing_Tree lt, int indentation_level) {
			lt.LabelLeft(name, tooltip, indentation_level, textColor: Color.white);
			MultiCheckboxState state = CheckState(this);
			MultiCheckboxState multiCheckboxState = Widgets.CheckboxMulti(new Rect(lt.LabelWidth, lt.curY, lt.lineHeight, lt.lineHeight), state, true);
			if (multiCheckboxState == MultiCheckboxState.On)
				ToggleAction(this, true);
			else if (multiCheckboxState == MultiCheckboxState.Off)
				ToggleAction(this, false);
		}
	}
}
