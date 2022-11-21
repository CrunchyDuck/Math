using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Verse;
using RimWorld;

namespace CrunchyDuck.Math {
	class Dialog_Updates : Window {
		private Vector2 scrollPosition = Vector2.zero;
		public override Vector2 InitialSize => new Vector2(700f, 700f);
		public string version;
		private List<UpdateLog> updates = new List<UpdateLog>() {
			{ new UpdateLog("1.3.0", "CD.M.updates.1.3".Translate()) }
		};
		private int i = 0;

		public Dialog_Updates() {
			forcePause = true;
			doCloseX = true;
			doCloseButton = true;
			absorbInputAroundWindow = true;
			closeOnClickedOutside = true;
		}

		public override void Close(bool doCloseSound = true) {
			base.Close(doCloseSound);
		}

		public override void DoWindowContents(Rect inRect) {
			UpdateLog u = updates[i];

			Rect label_area = new Rect(inRect);
			label_area = label_area.ContractedBy(18f);
			label_area.height = 34f;
			label_area.xMax -= 34f;

			label_area.x += 34f;
			Text.Font = GameFont.Medium;
			Widgets.Label(label_area, "v" + u.version);

			float scroll_area_display_height = inRect.height - CloseButSize.y - label_area.height - 10;// - 18f;
			Rect scroll_area_display = inRect.TopPartPixels(scroll_area_display_height);
			scroll_area_display.y += label_area.height + 10;
			Widgets.TextAreaScrollable(scroll_area_display, u.log, ref scrollPosition, readOnly: true);
		}
	}

	public struct UpdateLog {
		public string version;
		public string log;

		public UpdateLog(string version, string log) {
			this.version = version;
			this.log = log;
		}
	}
}
