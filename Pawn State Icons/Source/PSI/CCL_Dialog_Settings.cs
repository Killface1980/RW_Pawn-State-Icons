    using System.Collections.Generic;
    using System.Linq;
    using CommunityCoreLibrary;
    using UnityEngine;
    using Verse;
using PSI;
    using RimWorld;

namespace PSI

{
    public class ModConfigMenu : ModConfigurationMenu
    {
        #region Fields

        public Dictionary<string, object> values = new Dictionary<string, object>();
        private float rowHeight = 24f;
        private float rowMargin = 6f;
        public static ModSettings Settings = new ModSettings();

        #endregion Fields

        #region Methods

        public override float DoWindowContents(Rect canvas)
        {
            float curY = 0f;
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.LowerLeft;
            Widgets.Label(new Rect(0f, curY, canvas.width, rowHeight * 2), "Pawn State Icons");
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            curY += rowHeight * 2;

                string label = "Test";
                Widgets.Label(new Rect(0f, curY, canvas.width / 3f * 2f, rowHeight), label);
               
                curY += rowHeight + rowMargin;
            
            return curY;
        }
        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            { }

        }


    }
    #endregion
}
