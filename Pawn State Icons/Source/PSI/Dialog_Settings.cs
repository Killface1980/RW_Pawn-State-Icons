﻿using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Verse;

namespace PSI
{
    // ReSharper disable once InconsistentNaming
    internal class Dialog_Settings : Window
    {
        public string Page = "main";
        public bool CloseButtonClicked = true;
        public Window OptionsDialog;

        public Dialog_Settings()
        {
            closeOnEscapeKey = false;
            doCloseButton = false;
            doCloseX = true;
            absorbInputAroundWindow = false;
            forcePause = false;
        }

        private void DoHeading(Listing_Standard listing, string translatorKey, bool translate = true)
        {
            Text.Font = GameFont.Medium;
            listing.DoLabel(translate ? translatorKey.Translate() : translatorKey);
            Text.Font = GameFont.Small;
        }

        private void FillPageMain(Listing_Standard listing)
        {
            if (listing.DoTextButton("PSI.Settings.IconSet".Translate() + PSI.settings.icon_Set))
            {
                var options = new List<FloatMenuOption>();
                foreach (var str in PSI.IconSets)
                {
                    var setname = str;
                    options.Add(new FloatMenuOption(setname, () =>
                    {
                        PSI.settings.icon_Set = setname;
                        PSI.materials = new Materials(setname);
                        PSI.materials.ReloadTextures(true);
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            if (listing.DoTextButton("PSI.Settings.LoadPresetButton".Translate()))
            {
                var strArray = new string[0];
                var path = GenFilePaths.CoreModsFolderPath + "/Pawn State Icons/Presets/Complete/";
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
                            PSI.settings = XmlLoader.ItemFromXmlFile<Settings>(setname);
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
            listing.DoGap();
            DoHeading(listing, "PSI.Settings.Advanced");
            if (listing.DoTextButton("PSI.Settings.VisibilityButton".Translate()))
                Page = "showhide";
            if (listing.DoTextButton("PSI.Settings.ArrangementButton".Translate()))
                Page = "arrange";
            if (!listing.DoTextButton("PSI.Settings.SensitivityButton".Translate()))
                return;
            Page = "limits";
        }

        private void FillPageLimits(Listing_Standard listing)
        {
            DoHeading(listing, "PSI.Settings.Sensitivity.Header");
            if (listing.DoTextButton("PSI.Settings.LoadPresetButton".Translate()))
            {
                var strArray = new string[0];
                var path = GenFilePaths.CoreModsFolderPath + "/Pawn State Icons/Presets/Sensitivity/";
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
                            var settings = XmlLoader.ItemFromXmlFile<Settings>(setname);
                            PSI.settings.limit_BleedMult = settings.limit_BleedMult;
                            PSI.settings.limit_DiseaseLess = settings.limit_DiseaseLess;
                            PSI.settings.limit_EfficiencyLess = settings.limit_EfficiencyLess;
                            PSI.settings.limit_FoodLess = settings.limit_FoodLess;
                            PSI.settings.limit_MoodLess = settings.limit_MoodLess;
                            PSI.settings.limit_RestLess = settings.limit_RestLess;
                            PSI.settings.limit_ApparelHealthLess = settings.limit_ApparelHealthLess;
                            PSI.settings.limit_TempComfortOffset = settings.limit_TempComfortOffset;
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
            listing.DoLabel("PSI.Settings.Sensitivity.Bleeding".Translate() + ("PSI.Settings.Sensitivity.Bleeding." + Math.Round(PSI.settings.limit_BleedMult - 0.25)).Translate());
            PSI.settings.limit_BleedMult = listing.DoSlider(PSI.settings.limit_BleedMult, 0.5f, 5f);
            listing.DoLabel("PSI.Settings.Sensitivity.Injured".Translate() + (int)(PSI.settings.limit_EfficiencyLess * 100.0) + "%");
            PSI.settings.limit_EfficiencyLess = listing.DoSlider(PSI.settings.limit_EfficiencyLess, 0.01f, 0.99f);
            listing.DoLabel("PSI.Settings.Sensitivity.Food".Translate() + (int)(PSI.settings.limit_FoodLess * 100.0) + "%");
            PSI.settings.limit_FoodLess = listing.DoSlider(PSI.settings.limit_FoodLess, 0.01f, 0.99f);
            listing.DoLabel("PSI.Settings.Sensitivity.Mood".Translate() + (int)(PSI.settings.limit_MoodLess * 100.0) + "%");
            PSI.settings.limit_MoodLess = listing.DoSlider(PSI.settings.limit_MoodLess, 0.01f, 0.99f);
            listing.DoLabel("PSI.Settings.Sensitivity.Rest".Translate() + (int)(PSI.settings.limit_RestLess * 100.0) + "%");
            PSI.settings.limit_RestLess = listing.DoSlider(PSI.settings.limit_RestLess, 0.01f, 0.99f);
            listing.DoLabel("PSI.Settings.Sensitivity.ApparelHealth".Translate() + (int)(PSI.settings.limit_ApparelHealthLess * 100.0) + "%");
            PSI.settings.limit_ApparelHealthLess = listing.DoSlider(PSI.settings.limit_ApparelHealthLess, 0.01f, 0.99f);
            listing.DoLabel("PSI.Settings.Sensitivity.Temperature".Translate() + (int)PSI.settings.limit_TempComfortOffset + "C");
            PSI.settings.limit_TempComfortOffset = listing.DoSlider(PSI.settings.limit_TempComfortOffset, -10f, 10f);
            if (!listing.DoTextButton("PSI.Settings.ReturnButton".Translate()))
                return;
            Page = "main";
        }

        private void FillPageShowHide(Listing_Standard listing)
        {
            listing.OverrideColumnWidth = 230f;
            DoHeading(listing, "PSI.Settings.Visibility.Header");
            listing.OverrideColumnWidth = 95f;
            listing.DoLabelCheckbox("PSI.Settings.Visibility.TargetPoint".Translate(), ref PSI.settings.show_TargetPoint);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Aggressive".Translate(), ref PSI.settings.show_Aggressive);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Dazed".Translate(), ref PSI.settings.show_Dazed);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Leave".Translate(), ref PSI.settings.show_Leave);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Draft".Translate(), ref PSI.settings.show_Draft);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Idle".Translate(), ref PSI.settings.show_Idle);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Unarmed".Translate(), ref PSI.settings.show_Unarmed);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Hungry".Translate(), ref PSI.settings.show_Hungry);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Sad".Translate(), ref PSI.settings.show_Sad);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Tired".Translate(), ref PSI.settings.show_Tired);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Disease".Translate(), ref PSI.settings.show_Disease);
            listing.OverrideColumnWidth = 230f;
            if (listing.DoTextButton("PSI.Settings.ReturnButton".Translate()))
                Page = "main";
            listing.OverrideColumnWidth = 95f;
            listing.NewColumn();
            DoHeading(listing, " ", false);
            DoHeading(listing, " ", false);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Injury".Translate(), ref PSI.settings.show_Effectiveness);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Bloodloss".Translate(), ref PSI.settings.show_Bloodloss);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Hot".Translate(), ref PSI.settings.show_Hot);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Cold".Translate(), ref PSI.settings.show_Cold);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Naked".Translate(), ref PSI.settings.show_Naked);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Drunk".Translate(), ref PSI.settings.show_Drunk);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.ApparelHealth".Translate(), ref PSI.settings.show_ApparelHealth);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Pacific".Translate(), ref PSI.settings.show_Pacific);
        }

        private void FillPageArrangement(Listing_Standard listing)
        {
            DoHeading(listing, "PSI.Settings.Arrangement.Header");
            if (listing.DoTextButton("PSI.Settings.LoadPresetButton".Translate()))
            {
                var strArray = new string[0];
                var path = GenFilePaths.CoreModsFolderPath + "/Pawn State Icons/Presets/Position/";
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
                            var settings = XmlLoader.ItemFromXmlFile<Settings>(setname);
                            PSI.settings.icon_DistanceX = settings.icon_DistanceX;
                            PSI.settings.icon_DistanceY = settings.icon_DistanceY;
                            PSI.settings.icon_OffsetX = settings.icon_OffsetX;
                            PSI.settings.icon_OffsetY = settings.icon_OffsetY;
                            PSI.settings.icons_Horizontal = settings.icons_Horizontal;
                            PSI.settings.icons_ScreenScale = settings.icons_ScreenScale;
                            PSI.settings.icons_InColumn = settings.icons_InColumn;
                            PSI.settings.icon_Size = settings.icon_Size;
                        }
                        catch (IOException)
                        {
                            Log.Error("PSI.Settings.LoadPreset.UnableToLoad".Translate() + setname);
                        }
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            var num = (int)(PSI.settings.icon_Size * 4.5);
            if (num > 8)
                num = 8;
            else if (num < 0)
                num = 0;
            listing.DoLabel("PSI.Settings.Arrangement.IconSize".Translate() + ("PSI.Settings.SizeLabel." + num).Translate());
            PSI.settings.icon_Size = listing.DoSlider(PSI.settings.icon_Size, 0.5f, 2f);
            listing.DoLabel(string.Concat("PSI.Settings.Arrangement.IconPosition".Translate(), (int) (PSI.settings.icon_DistanceX * 100.0), " , ", (int) (PSI.settings.icon_DistanceY * 100.0)));
            PSI.settings.icon_DistanceX = listing.DoSlider(PSI.settings.icon_DistanceX, -2f, 2f);
            PSI.settings.icon_DistanceY = listing.DoSlider(PSI.settings.icon_DistanceY, -2f, 2f);
            listing.DoLabel(string.Concat("PSI.Settings.Arrangement.IconOffset".Translate(), (int) (PSI.settings.icon_OffsetX * 100.0), " , ", (int) (PSI.settings.icon_OffsetY * 100.0)));
            PSI.settings.icon_OffsetX = listing.DoSlider(PSI.settings.icon_OffsetX, -2f, 2f);
            PSI.settings.icon_OffsetY = listing.DoSlider(PSI.settings.icon_OffsetY, -2f, 2f);
            listing.DoLabelCheckbox("PSI.Settings.Arrangement.Horizontal".Translate(), ref PSI.settings.icons_Horizontal);
            listing.DoLabelCheckbox("PSI.Settings.Arrangement.ScreenScale".Translate(), ref PSI.settings.icons_ScreenScale);
            listing.DoLabel("PSI.Settings.Arrangement.IconsPerColumn".Translate() + PSI.settings.icons_InColumn);
            PSI.settings.icons_InColumn = (int)listing.DoSlider(PSI.settings.icons_InColumn, 1f, 9f);
            if (!listing.DoTextButton("PSI.Settings.ReturnButton".Translate()))
                return;
            Page = "main";
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (OptionsDialog == null)
                return;
            var rect = OptionsDialog.currentWindowRect;
            currentWindowRect = new Rect(rect.xMax - 240f, rect.yMin, 240f, rect.height);
            var listing = new Listing_Standard(inRect);
            DoHeading(listing, "Pawn State Icons", false);
            listing.OverrideColumnWidth = currentWindowRect.width;
            if (Page == "showhide")
                FillPageShowHide(listing);
            else if (Page == "arrange")
                FillPageArrangement(listing);
            else if (Page == "limits")
                FillPageLimits(listing);
            else
                FillPageMain(listing);
            listing.End();
        }

        public override void PreClose()
        {
            PSI.SaveSettings();
            PSI.Reinit();
            CloseButtonClicked = true;
            base.PreClose();
        }
    }
}
