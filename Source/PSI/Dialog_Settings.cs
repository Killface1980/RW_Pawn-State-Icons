using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Verse;

namespace PSI
{
    internal class DialogSettings : Window
    {
        public string Page = "main";
        public bool CloseButtonClicked = true;
        public Window OptionsDialog;

        public DialogSettings()
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
            if (listing.DoTextButton("PSI.Settings.IconSet".Translate() + Psi.Settings.IconSet))
            {
                var options = new List<FloatMenuOption>();
                foreach (var str in Psi.IconSets)
                {
                    var setname = str;
                    options.Add(new FloatMenuOption(setname, () =>
                    {
                        Psi.Settings.IconSet = setname;
                        Psi.Materials = new Materials(setname);
                        Psi.Materials.ReloadTextures(true);
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
                            Psi.Settings = XmlLoader.ItemFromXmlFile<ModSettings>(setname);
                            Psi.SaveSettings();
                            Psi.Reinit();
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
                            var settings = XmlLoader.ItemFromXmlFile<ModSettings>(setname);
                            Psi.Settings.LimitBleedMult = settings.LimitBleedMult;
                            Psi.Settings.LimitDiseaseLess = settings.LimitDiseaseLess;
                            Psi.Settings.LimitEfficiencyLess = settings.LimitEfficiencyLess;
                            Psi.Settings.LimitFoodLess = settings.LimitFoodLess;
                            Psi.Settings.LimitMoodLess = settings.LimitMoodLess;
                            Psi.Settings.LimitRestLess = settings.LimitRestLess;
                            Psi.Settings.LimitApparelHealthLess = settings.LimitApparelHealthLess;
                            Psi.Settings.LimitTempComfortOffset = settings.LimitTempComfortOffset;
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

            listing.DoLabel("PSI.Settings.Sensitivity.Bleeding".Translate() + ("PSI.Settings.Sensitivity.Bleeding." + Math.Round(Psi.Settings.LimitBleedMult - 0.25)).Translate());
            Psi.Settings.LimitBleedMult = listing.DoSlider(Psi.Settings.LimitBleedMult, 0.5f, 5f);

            listing.DoLabel("PSI.Settings.Sensitivity.Injured".Translate() + (int)(Psi.Settings.LimitEfficiencyLess * 100.0) + "%");
            Psi.Settings.LimitEfficiencyLess = listing.DoSlider(Psi.Settings.LimitEfficiencyLess, 0.01f, 0.99f);

            listing.DoLabel("PSI.Settings.Sensitivity.Food".Translate() + (int)(Psi.Settings.LimitFoodLess * 100.0) + "%");
            Psi.Settings.LimitFoodLess = listing.DoSlider(Psi.Settings.LimitFoodLess, 0.01f, 0.99f);

            listing.DoLabel("PSI.Settings.Sensitivity.Mood".Translate() + (int)(Psi.Settings.LimitMoodLess * 100.0) + "%");
            Psi.Settings.LimitMoodLess = listing.DoSlider(Psi.Settings.LimitMoodLess, 0.01f, 0.99f);

            listing.DoLabel("PSI.Settings.Sensitivity.Rest".Translate() + (int)(Psi.Settings.LimitRestLess * 100.0) + "%");
            Psi.Settings.LimitRestLess = listing.DoSlider(Psi.Settings.LimitRestLess, 0.01f, 0.99f);

            listing.DoLabel("PSI.Settings.Sensitivity.ApparelHealth".Translate() + (int)(Psi.Settings.LimitApparelHealthLess * 100.0) + "%");
            Psi.Settings.LimitApparelHealthLess = listing.DoSlider(Psi.Settings.LimitApparelHealthLess, 0.01f, 0.99f);

            listing.DoLabel("PSI.Settings.Sensitivity.Temperature".Translate() + (int)Psi.Settings.LimitTempComfortOffset + "C");
            Psi.Settings.LimitTempComfortOffset = listing.DoSlider(Psi.Settings.LimitTempComfortOffset, -10f, 10f);

            if (!listing.DoTextButton("PSI.Settings.ReturnButton".Translate()))
                return;

            Page = "main";
        }

        private void FillPageShowHide(Listing_Standard listing)
        {
            listing.OverrideColumnWidth = 230f;
            DoHeading(listing, "PSI.Settings.Visibility.Header");
            listing.OverrideColumnWidth = 95f;
            listing.DoLabelCheckbox("PSI.Settings.Visibility.TargetPoint".Translate(), ref Psi.Settings.ShowTargetPoint);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Aggressive".Translate(), ref Psi.Settings.ShowAggressive);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Dazed".Translate(), ref Psi.Settings.ShowDazed);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Leave".Translate(), ref Psi.Settings.ShowLeave);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Draft".Translate(), ref Psi.Settings.ShowDraft);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Idle".Translate(), ref Psi.Settings.ShowIdle);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Unarmed".Translate(), ref Psi.Settings.ShowUnarmed);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Hungry".Translate(), ref Psi.Settings.ShowHungry);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Sad".Translate(), ref Psi.Settings.ShowSad);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Tired".Translate(), ref Psi.Settings.ShowTired);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Disease".Translate(), ref Psi.Settings.ShowDisease);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.NightOwl".Translate(), ref Psi.Settings.ShowNightOwl);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Greedy".Translate(), ref Psi.Settings.ShowGreedy);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Jealous".Translate(), ref Psi.Settings.ShowJealous);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Lovers".Translate(), ref Psi.Settings.ShowLovers);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Prosthophile".Translate(), ref Psi.Settings.ShowProsthophile);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Prosthophobe".Translate(), ref Psi.Settings.ShowProsthophobe);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.RoomStatus".Translate(), ref Psi.Settings.ShowRoomStatus);

            listing.OverrideColumnWidth = 230f;
            if (listing.DoTextButton("PSI.Settings.ReturnButton".Translate()))
                Page = "main";
            listing.OverrideColumnWidth = 95f;
            listing.NewColumn();
            DoHeading(listing, " ", false);
            DoHeading(listing, " ", false);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Injury".Translate(), ref Psi.Settings.ShowEffectiveness);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Bloodloss".Translate(), ref Psi.Settings.ShowBloodloss);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Hot".Translate(), ref Psi.Settings.ShowHot);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Cold".Translate(), ref Psi.Settings.ShowCold);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Naked".Translate(), ref Psi.Settings.ShowNaked);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Drunk".Translate(), ref Psi.Settings.ShowDrunk);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.ApparelHealth".Translate(), ref Psi.Settings.ShowApparelHealth);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Pacific".Translate(), ref Psi.Settings.ShowPacific);
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
                            var settings = XmlLoader.ItemFromXmlFile<ModSettings>(setname);
                            Psi.Settings.IconDistanceX = settings.IconDistanceX;
                            Psi.Settings.IconDistanceY = settings.IconDistanceY;
                            Psi.Settings.IconOffsetX = settings.IconOffsetX;
                            Psi.Settings.IconOffsetY = settings.IconOffsetY;
                            Psi.Settings.IconsHorizontal = settings.IconsHorizontal;
                            Psi.Settings.IconsScreenScale = settings.IconsScreenScale;
                            Psi.Settings.IconsInColumn = settings.IconsInColumn;
                            Psi.Settings.IconSize = settings.IconSize;
                        }
                        catch (IOException)
                        {
                            Log.Error("PSI.Settings.LoadPreset.UnableToLoad".Translate() + setname);
                        }


                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            var num = (int)(Psi.Settings.IconSize * 4.5);

            if (num > 8)
                num = 8;
            else if (num < 0)
                num = 0;

            listing.DoLabel("PSI.Settings.Arrangement.IconSize".Translate() + ("PSI.Settings.SizeLabel." + num).Translate());
            Psi.Settings.IconSize = listing.DoSlider(Psi.Settings.IconSize, 0.5f, 2f);

            listing.DoLabel(string.Concat("PSI.Settings.Arrangement.IconPosition".Translate(), (int)(Psi.Settings.IconDistanceX * 100.0), " , ", (int)(Psi.Settings.IconDistanceY * 100.0)));
            Psi.Settings.IconDistanceX = listing.DoSlider(Psi.Settings.IconDistanceX, -2f, 2f);
            Psi.Settings.IconDistanceY = listing.DoSlider(Psi.Settings.IconDistanceY, -2f, 2f);

            listing.DoLabel(string.Concat("PSI.Settings.Arrangement.IconOffset".Translate(), (int)(Psi.Settings.IconOffsetX * 100.0), " , ", (int)(Psi.Settings.IconOffsetY * 100.0)));
            Psi.Settings.IconOffsetX = listing.DoSlider(Psi.Settings.IconOffsetX, -2f, 2f);
            Psi.Settings.IconOffsetY = listing.DoSlider(Psi.Settings.IconOffsetY, -2f, 2f);

            listing.DoLabelCheckbox("PSI.Settings.Arrangement.Horizontal".Translate(), ref Psi.Settings.IconsHorizontal);

            listing.DoLabelCheckbox("PSI.Settings.Arrangement.ScreenScale".Translate(), ref Psi.Settings.IconsScreenScale);

            listing.DoLabel("PSI.Settings.Arrangement.IconsPerColumn".Translate() + Psi.Settings.IconsInColumn);

            Psi.Settings.IconsInColumn = (int)listing.DoSlider(Psi.Settings.IconsInColumn, 1f, 9f);

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
            Psi.SaveSettings();
            Psi.Reinit();
            CloseButtonClicked = true;
            base.PreClose();
        }
    }
}
