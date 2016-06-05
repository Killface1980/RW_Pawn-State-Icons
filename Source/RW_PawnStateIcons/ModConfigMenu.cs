using CommunityCoreLibrary;
using CommunityCoreLibrary.UI;
using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CommunityCoreLibrary.MiniMap;
using UnityEngine;
using Verse;

namespace PSI
{
    public class ModConfigMenu : ModConfigurationMenu
    {
        #region Fields

        public string Page = "main";
        //       public bool CloseButtonClicked = true;
        public Window OptionsDialog;

        #endregion

        #region Methods

        public override float DoWindowContents(Rect inRect)
        {
            float curY = 0f;

            inRect.xMin += 5f;
            inRect.width -= 10f;

            Rect headerRect = inRect;
            Rect headerRect2 = inRect;

            var headerListing = new Listing_Standard(headerRect);

            //       DoHeading(listing, "Pawn State Icons", false);

            headerListing.OverrideColumnWidth = inRect.width / 2 - 10f;

            FillPageMain(headerListing);

            headerListing.End();

            curY += headerListing.CurHeight;


            headerRect2.yMin += curY;
            var listinghead = new Listing_Standard(headerRect2);
            listinghead.OverrideColumnWidth = headerListing.ColumnWidth();
            FillPageMain2(listinghead);

            listinghead.End();

            curY += listinghead.CurHeight;

            curY += 15f;

            Rect contentRect = inRect;
            contentRect.yMin += curY;

            var listing2 = new Listing_Standard(contentRect);

            if (Page == "showhide")
            {
                FillPageShowHide(listing2, contentRect.width);
                curY += 27 * 30f;
            }
            else if (Page == "opacityandcolor")
            {
                FillPageOpacityAndColor(listing2, contentRect.width);
                curY += 15 * 30f;
            }
            else if (Page == "arrange")
            {
                FillPageArrangement(listing2, contentRect.width);
                curY += 10 * 30f;
            }
            else if (Page == "limits")
            {
                FillPageLimits(listing2, contentRect.width);
                curY += 14 * 30f;
            }
            listing2.End();

            return curY;
        }

        public static void DoCheckbox(Rect rect, ref bool value, string labelKey, string tipKey)
        {
            GameFont font = Text.Font;
            TextAnchor anchor = Text.Anchor;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            string text = Translator.Translate(labelKey);
            Vector2 vector = new Vector2(rect.x, rect.y + (rect.height - 24f) / 2f);
            float x = Text.CalcSize(text).x;
            Rect rect2 = new Rect(rect.x + 24f + 4f, rect.y, x, rect.height);
            Widgets.Checkbox(vector, ref value, 24f, false);
            DoLabel(rect2, text, Translator.Translate(tipKey));
            Text.Anchor = anchor;
            Text.Font = font;
        }

        public static void DoLabel(Rect rect, string label, string tipText = "")
        {
            GameFont font = Text.Font;
            TextAnchor anchor = Text.Anchor;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            if (!GenText.NullOrEmpty(tipText))
            {
                Widgets.DrawHighlightIfMouseover(rect);
                if (Mouse.IsOver(rect))
                {
                    GUI.DrawTexture(rect, TexUI.HighlightTex);
                }
                TooltipHandler.TipRegion(rect, tipText);
            }
            Widgets.Label(rect, label);
            Text.Anchor = anchor;
            Text.Font = font;
        }

        private void DoHeading(Listing_Standard listing, string translatorKey, bool translate = true)
        {
            Text.Font = GameFont.Medium;
            listing.DoLabel(translate ? translatorKey.Translate() : translatorKey);
            Text.Font = GameFont.Small;
        }

        private void FillPageMain(Listing_Standard listing)
        {
            if (listing.DoTextButton("PSI.Settings.IconSet".Translate() + PSI.Settings.IconSet))
            {
                var options = new List<FloatMenuOption>();
                foreach (var str in PSI.IconSets)
                {
                    var setname = str;
                    options.Add(new FloatMenuOption(setname, () =>
                    {
                        PSI.Settings.IconSet = setname;
                        PSI.Materials = new Materials(setname);
                        PSI.Materials.ReloadTextures(true);
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            listing.NewColumn();

            if (listing.DoTextButton("PSI.Settings.LoadPresetButton".Translate()))
            {
                var strArray = new string[0];
                var path = GenFilePaths.CoreModsFolderPath + "/RW_PawnStateIcons/Presets/Complete/";
                if (Directory.Exists(path))
                    strArray = Directory.GetFiles(path, "*.cfg");
                var options = new List<FloatMenuOption>();
                foreach (var str in strArray)
                {
                    var setname = str;
                    options.Add(new FloatMenuOption(Path.GetFileNameWithoutExtension(setname), () =>
                    {
                        try
                        {
                            PSI.Settings = XmlLoader.ItemFromXmlFile<ModSettings>(setname);
                            PSI.SaveSettings();
                            PSI.Reinit();
                        }
                        catch (IOException)
                        {
                            Log.Error("PSI.Settings.LoadPreset.UnableToLoad".Translate() + setname);
                        }
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

        }

        private void FillPageMain2(Listing_Standard listing)
        {


            listing.DoGap();
            listing.OverrideColumnWidth = listing.ColumnWidth() * 2;

            DoHeading(listing, "PSI.Settings.Advanced");

            listing.OverrideColumnWidth = listing.ColumnWidth() / 2;


            if (listing.DoTextButton("PSI.Settings.VisibilityButton".Translate()))
                Page = "showhide";

            if (listing.DoTextButton("PSI.Settings.OpacityAndColorButton".Translate()))
                Page = "opacityandcolor";

            listing.NewColumn();
            DoHeading(listing, "");
            listing.DoGap();


            if (listing.DoTextButton("PSI.Settings.ArrangementButton".Translate()))
                Page = "arrange";

            if (!listing.DoTextButton("PSI.Settings.SensitivityButton".Translate()))
                return;

            Page = "limits";
        }

        private void FillPageLimits(Listing_Standard listing, float columnwidth)
        {
            listing.OverrideColumnWidth = columnwidth;

            DoHeading(listing, "PSI.Settings.Sensitivity.Header");
            if (listing.DoTextButton("PSI.Settings.LoadPresetButton".Translate()))
            {
                var strArray = new string[0];
                var path = GenFilePaths.CoreModsFolderPath + "/RW_PawnStateIcons/Presets/Sensitivity/";
                if (Directory.Exists(path))
                    strArray = Directory.GetFiles(path, "*.cfg");
                var options = new List<FloatMenuOption>();
                foreach (var str in strArray)
                {
                    var setname = str;
                    options.Add(new FloatMenuOption(Path.GetFileNameWithoutExtension(setname), () =>
                    {
                        try
                        {
                            var settings = XmlLoader.ItemFromXmlFile<ModSettings>(setname);
                            PSI.Settings.LimitBleedMult = settings.LimitBleedMult;
                            PSI.Settings.LimitDiseaseLess = settings.LimitDiseaseLess;
                            PSI.Settings.LimitEfficiencyLess = settings.LimitEfficiencyLess;
                            PSI.Settings.LimitFoodLess = settings.LimitFoodLess;
                            PSI.Settings.LimitMoodLess = settings.LimitMoodLess;
                            PSI.Settings.LimitRestLess = settings.LimitRestLess;
                            PSI.Settings.LimitApparelHealthLess = settings.LimitApparelHealthLess;
                            PSI.Settings.LimitTempComfortOffset = settings.LimitTempComfortOffset;
                        }
                        catch (IOException)
                        {
                            Log.Error("PSI.Settings.LoadPreset.UnableToLoad".Translate() + setname);
                        }
                    }));
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }

            listing.DoGap();

            listing.DoLabel("PSI.Settings.Sensitivity.Bleeding".Translate() + ("PSI.Settings.Sensitivity.Bleeding." + Math.Round(PSI.Settings.LimitBleedMult - 0.25)).Translate());
            PSI.Settings.LimitBleedMult = listing.DoSlider(PSI.Settings.LimitBleedMult, 0.5f, 5f);

            listing.DoLabel("PSI.Settings.Sensitivity.Injured".Translate() + (int)(PSI.Settings.LimitEfficiencyLess * 100.0) + "%");
            PSI.Settings.LimitEfficiencyLess = listing.DoSlider(PSI.Settings.LimitEfficiencyLess, 0.01f, 0.99f);

            listing.DoLabel("PSI.Settings.Sensitivity.Food".Translate() + (int)(PSI.Settings.LimitFoodLess * 100.0) + "%");
            PSI.Settings.LimitFoodLess = listing.DoSlider(PSI.Settings.LimitFoodLess, 0.01f, 0.99f);

            listing.DoLabel("PSI.Settings.Sensitivity.Mood".Translate() + (int)(PSI.Settings.LimitMoodLess * 100.0) + "%");
            PSI.Settings.LimitMoodLess = listing.DoSlider(PSI.Settings.LimitMoodLess, 0.01f, 0.99f);

            listing.DoLabel("PSI.Settings.Sensitivity.Rest".Translate() + (int)(PSI.Settings.LimitRestLess * 100.0) + "%");
            PSI.Settings.LimitRestLess = listing.DoSlider(PSI.Settings.LimitRestLess, 0.01f, 0.99f);

            listing.DoLabel("PSI.Settings.Sensitivity.ApparelHealth".Translate() + (int)(PSI.Settings.LimitApparelHealthLess * 100.0) + "%");
            PSI.Settings.LimitApparelHealthLess = listing.DoSlider(PSI.Settings.LimitApparelHealthLess, 0.01f, 0.99f);

            listing.DoLabel("PSI.Settings.Sensitivity.Temperature".Translate() + (int)PSI.Settings.LimitTempComfortOffset + "C");
            PSI.Settings.LimitTempComfortOffset = listing.DoSlider(PSI.Settings.LimitTempComfortOffset, -10f, 10f);

            //  if (!listing.DoTextButton("PSI.Settings.ReturnButton".Translate()))
            //      return;
            //
            //  Page = "main";
        }

        private LabeledInput_Color colorInput;

        public Color color = PSI.Settings.ColorRedAlert;



        private void FillPageOpacityAndColor(Listing_Standard listing, float columnwidth)
        {
            listing.OverrideColumnWidth = columnwidth;
            DoHeading(listing, "PSI.Settings.IconOpacityAndColor.Header");

            listing.DoLabel("PSI.Settings.IconOpacityAndColor.Opacity".Translate());
            PSI.Settings.IconOpacity = listing.DoSlider(PSI.Settings.IconOpacity, 0.05f, 1f);

            listing.DoLabel("PSI.Settings.IconOpacityAndColor.OpacityCritical".Translate());
            PSI.Settings.IconOpacityCritical = listing.DoSlider(PSI.Settings.IconOpacityCritical, 0f, 1f);

            listing.DoLabelCheckbox("PSI.Settings.IconOpacityAndColor.UseColoredTarget".Translate(), ref PSI.Settings.UseColoredTarget);


            Rect row = new Rect(0f, listing.CurHeight, listing.ColumnWidth(), 24f);

            colorInput = new LabeledInput_Color(color, "MiniMap.CP.Color".Translate(), "MiniMap.CP.ColorTip".Translate());

            colorInput.Draw(row);

         //   Log.Error(colorInput.Value.ToString());
       //     Log.Error(color.ToString());

            // PSI.Settings.ColorRedAlert = colorInput.Value;
            color = colorInput.Value;
            PSI.Settings.ColorRedAlert = colorInput.Value;
            PSI.SaveSettings();

            listing.DoGap();
            listing.DoGap();


            listing.DoLabel("Custom color settings coming from CCL in future");

            //  if (listing.DoTextButton("PSI.Settings.ReturnButton".Translate()))
            //      Page = "main";
        }

        private void FillPageShowHide(Listing_Standard listing, float columnwidth)
        {

            listing.OverrideColumnWidth = columnwidth;
            DoHeading(listing, "PSI.Settings.Visibility.Header");
            listing.DoLabelCheckbox("PSI.Settings.Visibility.TargetPoint".Translate(), ref PSI.Settings.ShowTargetPoint);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Aggressive".Translate(), ref PSI.Settings.ShowAggressive);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Dazed".Translate(), ref PSI.Settings.ShowDazed);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Leave".Translate(), ref PSI.Settings.ShowLeave);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Draft".Translate(), ref PSI.Settings.ShowDraft);
            //
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Idle".Translate(), ref PSI.Settings.ShowIdle);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Unarmed".Translate(), ref PSI.Settings.ShowUnarmed);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Hungry".Translate(), ref PSI.Settings.ShowHungry);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Sad".Translate(), ref PSI.Settings.ShowSad);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Tired".Translate(), ref PSI.Settings.ShowTired);
            //
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Sickness".Translate(), ref PSI.Settings.ShowDisease);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.NightOwl".Translate(), ref PSI.Settings.ShowNightOwl);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Greedy".Translate(), ref PSI.Settings.ShowGreedy);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Jealous".Translate(), ref PSI.Settings.ShowJealous);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Lovers".Translate(), ref PSI.Settings.ShowLovers);
            //
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Prosthophile".Translate(), ref PSI.Settings.ShowProsthophile);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Prosthophobe".Translate(), ref PSI.Settings.ShowProsthophobe);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.RoomStatus".Translate(), ref PSI.Settings.ShowRoomStatus);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Bedroom".Translate(), ref PSI.Settings.ShowBedroom);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Pain".Translate(), ref PSI.Settings.ShowPain);
            //
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Health".Translate(), ref PSI.Settings.ShowHealth);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Injury".Translate(), ref PSI.Settings.ShowEffectiveness);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Bloodloss".Translate(), ref PSI.Settings.ShowBloodloss);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Hot".Translate(), ref PSI.Settings.ShowHot);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Cold".Translate(), ref PSI.Settings.ShowCold);
            //
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Naked".Translate(), ref PSI.Settings.ShowNaked);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Drunk".Translate(), ref PSI.Settings.ShowDrunk);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.ApparelHealth".Translate(), ref PSI.Settings.ShowApparelHealth);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Pacific".Translate(), ref PSI.Settings.ShowPacific);
        }

        private void FillPageArrangement(Listing_Standard listing, float columnwidth)
        {
            listing.OverrideColumnWidth = columnwidth;

            DoHeading(listing, "PSI.Settings.Arrangement.Header");

            if (listing.DoTextButton("PSI.Settings.LoadPresetButton".Translate()))
            {
                var strArray = new string[0];
                var path = GenFilePaths.CoreModsFolderPath + "/RW_PawnStateIcons/Presets/Position/";
                if (Directory.Exists(path))
                    strArray = Directory.GetFiles(path, "*.cfg");
                var options = new List<FloatMenuOption>();
                foreach (var str in strArray)
                {
                    var setname = str;
                    options.Add(new FloatMenuOption(Path.GetFileNameWithoutExtension(setname), () =>
                    {
                        try
                        {
                            var settings = XmlLoader.ItemFromXmlFile<ModSettings>(setname);
                            PSI.Settings.IconDistanceX = settings.IconDistanceX;
                            PSI.Settings.IconDistanceY = settings.IconDistanceY;
                            PSI.Settings.IconOffsetX = settings.IconOffsetX;
                            PSI.Settings.IconOffsetY = settings.IconOffsetY;
                            PSI.Settings.IconsHorizontal = settings.IconsHorizontal;
                            PSI.Settings.IconsScreenScale = settings.IconsScreenScale;
                            PSI.Settings.IconsInColumn = settings.IconsInColumn;
                            PSI.Settings.IconSize = settings.IconSize;
                            PSI.Settings.IconOpacity = settings.IconOpacity;
                            PSI.Settings.IconOpacity = settings.IconOpacityCritical;
                        }
                        catch (IOException)
                        {
                            Log.Error("PSI.Settings.LoadPreset.UnableToLoad".Translate() + setname);
                        }


                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            var num = (int)(PSI.Settings.IconSize * 4.5);

            if (num > 8)
                num = 8;
            else if (num < 0)
                num = 0;

            listing.DoLabel("PSI.Settings.Arrangement.IconSize".Translate() + ("PSI.Settings.SizeLabel." + num).Translate());
            PSI.Settings.IconSize = listing.DoSlider(PSI.Settings.IconSize, 0.5f, 2f);

            listing.DoLabel(string.Concat("PSI.Settings.Arrangement.IconPosition".Translate(), (int)(PSI.Settings.IconDistanceX * 100.0), " , ", (int)(PSI.Settings.IconDistanceY * 100.0)));
            PSI.Settings.IconDistanceX = listing.DoSlider(PSI.Settings.IconDistanceX, -2f, 2f);
            PSI.Settings.IconDistanceY = listing.DoSlider(PSI.Settings.IconDistanceY, -2f, 2f);

            listing.DoLabel(string.Concat("PSI.Settings.Arrangement.IconOffset".Translate(), (int)(PSI.Settings.IconOffsetX * 100.0), " , ", (int)(PSI.Settings.IconOffsetY * 100.0)));
            PSI.Settings.IconOffsetX = listing.DoSlider(PSI.Settings.IconOffsetX, -2f, 2f);
            PSI.Settings.IconOffsetY = listing.DoSlider(PSI.Settings.IconOffsetY, -2f, 2f);

            listing.DoLabelCheckbox("PSI.Settings.Arrangement.Horizontal".Translate(), ref PSI.Settings.IconsHorizontal);

            listing.DoLabelCheckbox("PSI.Settings.Arrangement.ScreenScale".Translate(), ref PSI.Settings.IconsScreenScale);

            listing.DoLabel("PSI.Settings.Arrangement.IconsPerColumn".Translate() + PSI.Settings.IconsInColumn);

            PSI.Settings.IconsInColumn = (int)listing.DoSlider(PSI.Settings.IconsInColumn, 1f, 7f);

            //   if (!listing.DoTextButton("PSI.Settings.ReturnButton".Translate()))
            //       return;
            //
            //   Page = "main";
        }

        public override void ExposeData()
        {
            PSI.SaveSettings();
            PSI.Reinit();

            //          CloseButtonClicked = true;
        }

        #endregion
    }
}