﻿using System;
using System.Collections.Generic;
using System.IO;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using System.Linq;


namespace PSI
{

    internal class PSI : MonoBehaviour
    {
        private static double _fDelta;

        private static bool _inGame;

        private static Dictionary<Pawn, PawnStats> _statsDict = new Dictionary<Pawn, PawnStats>();

        private static bool _iconsEnabled = true;

        public static ModSettings Settings = new ModSettings();

        private static float _worldScale = 1f;

        public static string[] IconSets = { "default" };

        public static Materials Materials = new Materials();

        private static PawnCapacityDef[] _pawnCapacities;

        private static Vector3[] _iconPosVectors;

        public PSI()
        {
            Reinit();
        }


        public static void Reinit(bool reloadSettings = true, bool reloadIconSet = true, bool recalcIconPos = true)
        {
            _pawnCapacities = new[]
            {
                PawnCapacityDefOf.BloodFiltration,
                PawnCapacityDefOf.BloodPumping,
                PawnCapacityDefOf.Breathing,
                PawnCapacityDefOf.Consciousness,
                PawnCapacityDefOf.Eating,
                PawnCapacityDefOf.Hearing,
                PawnCapacityDefOf.Manipulation,
                PawnCapacityDefOf.Metabolism,
                PawnCapacityDefOf.Moving,
                PawnCapacityDefOf.Sight,
                PawnCapacityDefOf.Talking
            };

            if (recalcIconPos)
            {
                RecalcIconPositions();
            }

            if (reloadIconSet)
            {
                LongEventHandler.ExecuteWhenFinished(() =>
               {
                   Materials = new Materials(Settings.IconSet);
                   ModSettings modSettings =
                       XmlLoader.ItemFromXmlFile<ModSettings>(GenFilePaths.CoreModsFolderPath + "/RW_PawnStateIcons/Textures/UI/Overlays/PawnStateIcons/" + Settings.IconSet + "/iconset.cfg");
                   Settings.IconSizeMult = modSettings.IconSizeMult;
                   Materials.ReloadTextures(true);
                   Log.Message(GenFilePaths.CoreModsFolderPath + "/RW_PawnStateIcons/Textures/UI/Overlays/PawnStateIcons/" + Settings.IconSet + "/iconset.cfg");
               });
            }

        }

        public static ModSettings LoadSettings(string path = "psi-settings.cfg")
        {
            ModSettings result = XmlLoader.ItemFromXmlFile<ModSettings>(GenFilePaths.CoreModsFolderPath + "/RW_PawnStateIcons/" + path, true);
            string path2 = GenFilePaths.CoreModsFolderPath + "/RW_PawnStateIcons/Textures/UI/Overlays/PawnStateIcons/";
            if (Directory.Exists(path2))
            {
                IconSets = Directory.GetDirectories(path2);
                for (int i = 0; i < IconSets.Length; i++)
                {
                    IconSets[i] = new DirectoryInfo(IconSets[i]).Name;
                }
            }
            return result;
        }

        public static void SaveSettings(string path = "psi-settings.cfg")
        {
            XmlSaver.SaveDataObject(Settings, GenFilePaths.CoreModsFolderPath + "/RW_PawnStateIcons/" + path);
        }

        public virtual void OnGUI()
        {

            //    if (_inGame && Find.TickManager.Paused)
            //        UpdateOptionsDialog();
            //
            //    if (!_inGame)
            //        UpdateOptionsDialog();

            //     if (!_inGame || Find.TickManager.Paused))
            //         UpdateOptionsDialog();


            _inGame = GameObject.Find("ReverbZoneDummy");
            if (!_iconsEnabled || !_inGame)
                //  if (!_iconsEnabled)
                return;

            foreach (Pawn pawn in Find.Map.mapPawns.FreeColonistsAndPrisonersSpawned)
            {
                if (pawn?.RaceProps == null) continue;


                if (pawn.RaceProps.Animal)
                    DrawAnimalIcons(pawn);
                else if (pawn.IsColonist || pawn.IsPrisonerOfColony)
                {
                    DrawColonistIcons(pawn);
                }
            }
        }

        public virtual void Update()
        {
            //       if (!_inGame)
            //           return;
            if (Input.GetKeyUp(KeyCode.F11))
            {
                _iconsEnabled = !_iconsEnabled;
                Messages.Message(_iconsEnabled ? "PSI.Enabled".Translate() : "PSI.Disabled".Translate(), MessageSound.Standard);
            }
            _worldScale = Screen.height / (2f * Camera.current.orthographicSize);
        }

        #region Icon Drawing 

        private static void DrawIcon_posOffset(Vector3 bodyPos, Vector3 posOffset, Icons icon, Color color)
        {
            Material material = Materials[icon];
            if (material == null)
            {
                Debug.LogError("Material = null.");
                return;
            }
            material.color = color;
            Color guiColor = GUI.color;
            GUI.color = color;
            Vector2 vector2;

            if (Settings.IconsScreenScale)
            {
                vector2 = bodyPos.ToScreenPosition();
                vector2.x += posOffset.x * 45f;
                vector2.y -= posOffset.z * 45f;
            }
            else
                vector2 = (bodyPos + posOffset).ToScreenPosition();

            float wordscale = _worldScale;

            if (Settings.IconsScreenScale)
                wordscale = 45f;
            float num2 = wordscale * (Settings.IconSizeMult * 0.5f);
            Rect position = new Rect(vector2.x, vector2.y, num2 * Settings.IconSize, num2 * Settings.IconSize);
            position.x -= position.width * 0.5f;
            position.y -= position.height * 0.5f;
            GUI.DrawTexture(position, material.mainTexture, ScaleMode.ScaleToFit, true);
            GUI.color = guiColor;
        }

        private static void DrawIcon(Vector3 bodyPos, int num, Icons icon, Color color)
        {
            DrawIcon_posOffset(bodyPos, _iconPosVectors[num], icon, color);
        }

        private static void DrawIcon_FadeRedAlertToNeutral(Vector3 bodyPos, int num, Icons icon, float v)
        {
            v = v * 0.9f; // max settings according to neutral icon
            DrawIcon(bodyPos, num, icon, new Color(0.9f, v, v, Settings.IconOpacity));
        }

        private static void DrawIcon_FadeFloatWithTwoColors(Vector3 bodyPos, int num, Icons icon, float v, Color c1, Color c2)
        {
            DrawIcon(bodyPos, num, icon, Color.Lerp(c1, c2, v));
        }

        private static void DrawIcon_FadeFloatWithThreeColors(Vector3 bodyPos, int num, Icons icon, float v, Color c1, Color c2, Color c3)
        {
            DrawIcon(bodyPos, num, icon, v < 0.5 ? Color.Lerp(c1, c2, v * 2f) : Color.Lerp(c2, c3, (float)((v - 0.5) * 2.0)));
        }

        private static void DrawIcon_FadeFloatWithFourColorsHB(Vector3 bodyPos, int num, Icons icon, float v, Color c1, Color c2, Color c3, Color c4)
        {
            if (v > 0.8f)
            {
                DrawIcon(bodyPos, num, icon, Color.Lerp(c2, c1, (v - 0.8f) * 5));
            }
            else if (v > 0.6f)
            {
                DrawIcon(bodyPos, num, icon, Color.Lerp(c3, c2, (v - 0.6f) * 5));
            }
            else if (v > 0.4f)
            {
                DrawIcon(bodyPos, num, icon, Color.Lerp(c4, c3, (v - 0.4f) * 5));
            }
            else
            {
                DrawIcon(bodyPos, num, icon, c4);
            }
        }

        private static void DrawIcon_FadeFloatFiveColors(Vector3 bodyPos, int num, Icons icon, float v, Color c1, Color c2, Color c3, Color c4, Color c5)
        {
            if (v < 0.2f)
            {
                DrawIcon(bodyPos, num, icon, Color.Lerp(c1, c2, v * 5));
            }
            else if (v < 0.4f)
            {
                DrawIcon(bodyPos, num, icon, Color.Lerp(c2, c3, (v - 0.2f) * 5));
            }
            else if (v < 0.6f)
            {
                DrawIcon(bodyPos, num, icon, Color.Lerp(c3, c4, (v - 0.4f) * 5));
            }
            else if (v < 0.8f)
            {
                DrawIcon(bodyPos, num, icon, Color.Lerp(c4, c5, (v - 0.6f) * 5));
            }
            else
            {
                DrawIcon(bodyPos, num, icon, c5);
            }
        }

        private static void RecalcIconPositions()
        {
            //            _iconPosVectors = new Vector3[18];
            _iconPosVectors = new Vector3[40];
            for (int index = 0; index < _iconPosVectors.Length; ++index)
            {
                int num1 = index / Settings.IconsInColumn;
                int num2 = index % Settings.IconsInColumn;
                if (Settings.IconsHorizontal)
                {
                    int num3 = num1;
                    num1 = num2;
                    num2 = num3;
                }


                _iconPosVectors[index] =
                    new Vector3(
                        (float)
                            (-0.600000023841858 * Settings.IconDistanceX -
                             0.550000011920929 * Settings.IconSize * Settings.IconOffsetX * num1), 3f,
                        (float)
                            (-0.600000023841858 * Settings.IconDistanceY +
                             0.550000011920929 * Settings.IconSize * Settings.IconOffsetY * num2));

            }
        }

        #endregion

        #region Status + Updates

        private static void UpdateColonistStats(Pawn colonist)
        {

            if (colonist != null && !_statsDict.ContainsKey(colonist))
            {
                _statsDict.Add(colonist, new PawnStats());
            }

            if (colonist == null) return;

            PawnStats pawnStats = _statsDict[colonist];


            // efficiency
            float efficiency = 10f;

            PawnCapacityDef[] array = _pawnCapacities;
            foreach (PawnCapacityDef pawnCapacityDef in array)
            {
                if (pawnCapacityDef != PawnCapacityDefOf.Consciousness)
                {
                    efficiency = Math.Min(efficiency, colonist.health.capacities.GetEfficiency(pawnCapacityDef));
                }
                if (efficiency < 0f)
                    efficiency = 0f;
            }

            pawnStats.TotalEfficiency = efficiency;



            //target
            pawnStats.TargetPos = Vector3.zero;

            if (colonist.jobs.curJob != null)
            {
                JobDriver curDriver = colonist.jobs.curDriver;
                Job curJob = colonist.jobs.curJob;
                TargetInfo targetInfo = curJob.targetA;
                if (curDriver is JobDriver_HaulToContainer || curDriver is JobDriver_HaulToCell ||
                    curDriver is JobDriver_FoodDeliver || curDriver is JobDriver_FoodFeedPatient ||
                    curDriver is JobDriver_TakeToBed)
                {
                    targetInfo = curJob.targetB;
                }
                JobDriver_DoBill bill = curDriver as JobDriver_DoBill;
                if (bill != null)
                {
                    JobDriver_DoBill jobDriverDoBill = bill;
                    if (jobDriverDoBill.workLeft >= 0.0)
                    {
                        targetInfo = curJob.targetA;
                    }
                    else if (jobDriverDoBill.workLeft <= 0.01f)
                    {
                        targetInfo = curJob.targetB;
                    }
                }
                if (curDriver is JobDriver_Hunt && colonist.carrier?.CarriedThing != null)
                {
                    targetInfo = curJob.targetB;
                }
                if (curJob.def == JobDefOf.Wait)
                {
                    targetInfo = null;
                }
                if (curDriver is JobDriver_Ingest)
                {
                    targetInfo = null;
                }
                if (curJob.def == JobDefOf.LayDown && colonist.InBed())
                {
                    targetInfo = null;
                }
                if (!curJob.playerForced && curJob.def == JobDefOf.Goto)
                {
                    targetInfo = null;
                }
                bool flag;
                if (targetInfo != null)
                {
                    IntVec3 arg2420 = targetInfo.Cell;
                    flag = false;
                }
                else
                {
                    flag = true;
                }
                if (!flag)
                {
                    Vector3 a = targetInfo.Cell.ToVector3Shifted();
                    pawnStats.TargetPos = a + new Vector3(0f, 3f, 0f);
                }
            }

            // temperature
            float temperatureForCell = GenTemperature.GetTemperatureForCell(colonist.Position);

            pawnStats.TooCold =
                (float)
                    ((colonist.ComfortableTemperatureRange().min - (double)Settings.LimitTempComfortOffset -
                      temperatureForCell) / 10f);

            pawnStats.TooHot =
                (float)
                    ((temperatureForCell - (double)colonist.ComfortableTemperatureRange().max -
                      Settings.LimitTempComfortOffset) / 10f);

            pawnStats.TooCold = Mathf.Clamp(pawnStats.TooCold, 0f, 2f);

            pawnStats.TooHot = Mathf.Clamp(pawnStats.TooHot, 0f, 2f);
            /*
            // Drunkness - DEACTIVATED FOR NOW
            pawnStats.Drunkness =  DrugUtility.DrunknessPercent(colonist);
*/
            // Mental Sanity
            pawnStats.MentalSanity = null;
            if (colonist.mindState != null && colonist.InMentalState)
            {
                pawnStats.MentalSanity = colonist.MentalStateDef;
            }

            // Health Calc
            pawnStats.DiseaseDisappearance = 1f;
            pawnStats.HasLifeThreateningDisease = false;
            pawnStats.HealthDisease = 1f;


            if (pawnStats.IsSick && colonist.SelectableNow() && !colonist.Destroyed && colonist.playerSettings.medCare >= 0)
            {
                if (colonist.health?.hediffSet?.hediffs != null)
                {
                    int i;
                    for (i = 0; i < colonist.health.hediffSet.hediffs.Count; i++)
                    {
                        Hediff hediff = colonist.health.hediffSet.hediffs[i];
                        HediffWithComps hediffWithComps;

                        if ((HediffWithComps)hediff != null)
                            hediffWithComps = (HediffWithComps)hediff;
                        else continue;

                        if (hediffWithComps.IsOld()) continue;

                        pawnStats.ToxicBuildUp = 0;

                        //pawnStats.ToxicBuildUp
                        if (hediffWithComps.def.defName.Equals("ToxicBuildup"))
                        {
                            pawnStats.ToxicBuildUp = hediffWithComps.Severity;
                        }

                        if (hediffWithComps.def.defName.Equals("WoundInfection") || hediffWithComps.def.defName.Equals("Flu") || hediffWithComps.def.defName.Equals("Plague") || hediffWithComps.def.defName.Equals("Malaria") || hediffWithComps.def.defName.Equals("SleepingSickness"))
                        {
                            float severity = hediffWithComps.Severity;
                            float immunity = colonist.health.immunity.GetImmunity(hediffWithComps.def);
                            float basehealth = pawnStats.HealthDisease - (severity - immunity / 4) - 0.25f;
                            pawnStats.HasLifeThreateningDisease = true;
                            pawnStats.HealthDisease = basehealth;
                        }


                        if (!hediffWithComps.Visible) continue;

                        if (!hediffWithComps.def.PossibleToDevelopImmunity()) continue;

                        if (hediffWithComps.CurStage == null) continue;

                        if (!hediffWithComps.CurStage.everVisible) continue;

                        if (hediffWithComps.FullyImmune()) continue;

                        if (hediffWithComps.def.naturallyHealed) continue;

                        if (!hediffWithComps.def.makesSickThought) continue;

                        if (!hediffWithComps.def.tendable) continue;

                        if (Math.Abs(colonist.health.immunity.GetImmunity(hediffWithComps.def) - 1.0) < 0.05) continue;

                        //


                        if (pawnStats.DiseaseDisappearance > colonist.health.immunity.GetImmunity(hediffWithComps.def))
                        {
                            pawnStats.DiseaseDisappearance = colonist.health.immunity.GetImmunity(hediffWithComps.def);
                        }
                    }
                }
            }

            // Apparel Calc
            float worstApparel = 999f;
            List<Apparel> apparelListForReading = colonist.apparel.WornApparel;
            foreach (Apparel t in apparelListForReading)
            {
                float curApparel = (float)t.HitPoints / (float)t.MaxHitPoints;
                if (curApparel >= 0f && curApparel < worstApparel)
                {
                    worstApparel = curApparel;
                }
            }
            pawnStats.ApparelHealth = worstApparel;

            // Bleed rate


            if (colonist.health?.hediffSet != null)
                pawnStats.BleedRate = Mathf.Clamp01(colonist.health.hediffSet.BleedingRate * Settings.LimitBleedMult);


            // Bed status
            if (colonist.ownership.OwnedBed != null)
                //    if (colonist.ownership.OwnedBed.SleepingSlotsCount >= 1)
                pawnStats.HasBed = true;
            if (colonist.ownership.OwnedBed == null)
            {
                pawnStats.HasBed = false;
            }

            // Sick thoughts
            if (colonist.health?.hediffSet != null)
                pawnStats.IsSick = colonist.health.hediffSet.AnyHediffMakesSickThought;


            pawnStats.CrowdedMoodLevel = -1;

            pawnStats.PainMoodLevel = -1;

            // Moods

            if (colonist.needs.mood.thoughts.Thoughts != null)
            {
                int i;
                for (i = 0; i < colonist.needs.mood.thoughts.Thoughts.Count; i++)
                {
                    Thought thoughtDef = colonist.needs.mood.thoughts.Thoughts[i];
                    if (thoughtDef.CurStage != null)
                    {
                        if (thoughtDef.def.defName.Equals("Crowded"))
                        {
                            pawnStats.CrowdedMoodLevel = thoughtDef.CurStageIndex;
                        }
                        else
                        {
                            pawnStats.CrowdedMoodLevel = -1;
                        }
                        if (thoughtDef.def.defName.Equals("Pain") || thoughtDef.def.defName.Equals("MasochistPain"))
                        {
                            pawnStats.PainMoodLevel = thoughtDef.CurStageIndex;

                        }
                        else pawnStats.PainMoodLevel = -1;
                    }
                }
            }

            _statsDict[colonist] = pawnStats;
        }

        public static bool HasMood(Pawn pawn, ThoughtDef tdef)
        {
            return pawn.needs.mood.thoughts.Thoughts.Any((thought) => thought.def == tdef);
        }

        public virtual void FixedUpdate()
        {
            _fDelta += Time.fixedDeltaTime;

            if (_fDelta < 0.1)
                return;
            _fDelta = 0.0;
            _inGame = GameObject.Find("ReverbZoneDummy");

            if (!_inGame || !_iconsEnabled)
                return;

            if (!Find.Map.mapPawns.AllPawns.Any())
                return;

            foreach (Pawn pawn in Find.Map.mapPawns.FreeColonistsAndPrisoners) //.FreeColonistsAndPrisoners)
                                                                               //               foreach (var colonist in Find.Map.mapPawns.FreeColonistsAndPrisonersSpawned) //.FreeColonistsAndPrisoners)
            {
                if (pawn.SelectableNow() && !pawn.Dead && !pawn.DestroyedOrNull() && pawn.Name.IsValid)
                {
                    try
                    {
                        UpdateColonistStats(pawn);
                    }
                    catch (Exception ex)
                    {
                        Log.Notify_Exception(ex);
                    }
                }
            }
        }

        //   public void UpdateOptionsDialog()
        //   {
        //       Dialog_Options dialogOptions = Find.WindowStack.WindowOfType<Dialog_Options>();
        //       bool optionsOpened = dialogOptions != null;
        //       bool psiSettingsShowed = Find.WindowStack.IsOpen(typeof(Dialog_Settings));
        //       if (optionsOpened && psiSettingsShowed)
        //       {
        //           _settingsDialog.OptionsDialog = dialogOptions;
        //           if (_inGame)
        //           {                    
        //           RecalcIconPositions();
        //           }
        //           return;
        //       }
        //       if (optionsOpened && !psiSettingsShowed)
        //       {
        //           if (!_settingsDialog.CloseButtonClicked)
        //           {
        //               Find.UIRoot.windows.Add(_settingsDialog);
        //               _settingsDialog.Page = "main";
        //               return;
        //           }
        //           dialogOptions.Close(true);
        //       }
        //       else
        //       {
        //           if (!optionsOpened && psiSettingsShowed)
        //           {
        //               _settingsDialog.Close(false);
        //               return;
        //           }
        //           if (!optionsOpened && !psiSettingsShowed)
        //           {
        //               _settingsDialog.CloseButtonClicked = false;
        //           }
        //       }
        //   }

        #endregion

        #region Draw Icons

        private static void DrawAnimalIcons(Pawn animal)
        {
            float transparancy = Settings.IconOpacity;
            Color colorRedAlert = new Color(1f, 0f, 0f, transparancy);

            if (animal.Dead || animal.holder != null)
                return;
            Vector3 drawPos = animal.DrawPos;

            if (!Settings.ShowAggressive || animal.MentalStateDef != MentalStateDefOf.Berserk && animal.MentalStateDef != MentalStateDefOf.Manhunter)
                return;
            Vector3 bodyPos = drawPos;
            DrawIcon(bodyPos, 0, Icons.Aggressive, colorRedAlert);
        }

        private static void DrawColonistIcons(Pawn colonist)
        {
            float opacity = Settings.IconOpacity;

            float opacityCritical = Settings.IconOpacityCritical;

            Color color25To21 = new Color(0.8f, 0f, 0f, opacity);

            Color color20To16 = new Color(0.9f, 0.45f, 0f, opacity);

            Color color15To11 = new Color(0.95f, 0.95f, 0f, opacity);

            Color color10To06 = new Color(0.95f, 0.95f, 0.66f, opacity);

            Color color05AndLess = new Color(0.9f, 0.9f, 0.9f, opacity);

            Color colorMoodBoost = new Color(0f, 0.8f, 0f, opacity);

            Color colorNeutralStatus = color05AndLess; // new Color(1f, 1f, 1f, transparancy);

            Color colorNeutralStatusSolid = new Color(colorNeutralStatus.r, colorNeutralStatus.g, colorNeutralStatus.b, 0.5f + opacity * 0.2f);

            Color colorNeutralStatusFade = new Color(colorNeutralStatus.r, colorNeutralStatus.g, colorNeutralStatus.b, opacity / 4);


            Color colorHealthBarGreen = new Color(0f, 0.8f, 0f, opacity * 0.5f);

            Color colorRedAlert = PSI.Settings.ColorRedAlert;

            colorRedAlert.a = opacityCritical + (1 - opacityCritical) * opacity;
            //            Color colorRedAlert = new Color(color25To21.r, color25To21.g, color25To21.b, opacityCritical + (1 - opacityCritical) * opacity);

            Color colorOrangeAlert = new Color(color20To16.r, color20To16.g, color20To16.b, opacityCritical + (1 - opacityCritical) * opacity);

            Color colorYellowAlert = new Color(color15To11.r, color15To11.g, color15To11.b, opacityCritical + (1 - opacityCritical) * opacity);

            int iconNum = 0;

            PawnStats pawnStats;
            if (colonist.Dead || colonist.holder != null || !_statsDict.TryGetValue(colonist, out pawnStats) ||
                colonist.drafter == null || colonist.skills == null)
                return;

            Vector3 bodyLoc = colonist.DrawPos;

            // Target Point 
            if (Settings.ShowTargetPoint && (pawnStats.TargetPos != Vector3.zero))
            {
                if (Settings.UseColoredTarget)
                {
                    Color skinColor = colonist.story.SkinColor;
                    Color hairColor = colonist.story.hairColor;
                    DrawIcon_posOffset(pawnStats.TargetPos, Vector3.zero, Icons.TargetSkin, new Color(skinColor.r, skinColor.g, skinColor.b, 0.5f + opacity * 0.2f));
                    DrawIcon_posOffset(pawnStats.TargetPos, Vector3.zero, Icons.TargetHair, new Color(hairColor.r, hairColor.g, hairColor.b, 0.5f + opacity * 0.2f)); ;
                }
                else
                {
                    DrawIcon_posOffset(pawnStats.TargetPos, Vector3.zero, Icons.Target, colorNeutralStatusSolid);
                }


            }
            //if (Settings.ShowTargetPoint && (pawnStats.TargetPos != Vector3.zero || pawnStats.TargetPos != null))

            //    if (Settings.ShowTargetPoint && (pawnStats.TargetPos != Vector3.zero))
            //        //if (Settings.ShowTargetPoint && (pawnStats.TargetPos != Vector3.zero || pawnStats.TargetPos != null))

            //Drafted
            if (Settings.ShowDraft && colonist.drafter.Drafted)
                DrawIcon(bodyLoc, iconNum++, Icons.Draft, colorNeutralStatusSolid);

            if (pawnStats.MentalSanity != null)
            {
                // Berserk
                if (Settings.ShowAggressive && pawnStats.MentalSanity == MentalStateDefOf.Berserk)
                    DrawIcon(bodyLoc, iconNum++, Icons.Aggressive, colorRedAlert);
                /*
                                // Binging on alcohol - needs refinement
                                if (Settings.ShowDrunk)
                                {
                                    if (pawnStats.MentalSanity == MentalStateDefOf.BingingAlcohol)
                                        DrawIcon(bodyLoc, iconNum++, Icons.Drunk, colorRedAlert);
                                }
                                */
                // Give Up Exit
                if (Settings.ShowLeave && pawnStats.MentalSanity == MentalStateDefOf.PanicFlee) // was GiveUpExit
                    DrawIcon(bodyLoc, iconNum++, Icons.Leave, colorRedAlert);

                //Daze Wander
                if (Settings.ShowDazed && pawnStats.MentalSanity == MentalStateDefOf.WanderSad) // + MentalStateDefOf.WanderPsychotic
                    DrawIcon(bodyLoc, iconNum++, Icons.Dazed, colorYellowAlert);

                //PanicFlee
                if (pawnStats.MentalSanity == MentalStateDefOf.PanicFlee)
                    DrawIcon(bodyLoc, iconNum++, Icons.Panic, colorYellowAlert);
            }

            // Drunknes percent
            if (Settings.ShowDrunk)
                if (pawnStats.Drunkness > 0.05)
                    DrawIcon_FadeFloatWithThreeColors(bodyLoc, iconNum++, Icons.Drunk, pawnStats.Drunkness, colorYellowAlert, colorOrangeAlert, colorRedAlert);


            // Pacifc + Unarmed
            if (Settings.ShowPacific || Settings.ShowUnarmed)
            {
                if (colonist.skills.GetSkill(SkillDefOf.Melee).TotallyDisabled && colonist.skills.GetSkill(SkillDefOf.Shooting).TotallyDisabled)
                {
                    if (Settings.ShowPacific)
                        DrawIcon(bodyLoc, iconNum++, Icons.Pacific, colorNeutralStatus);
                }
                else if (Settings.ShowUnarmed && colonist.equipment.Primary == null && !colonist.IsPrisonerOfColony)
                    DrawIcon(bodyLoc, iconNum++, Icons.Unarmed, colorNeutralStatus);
            }
            // Trait Pyromaniac
            if (Settings.ShowPyromaniac && colonist.story.traits.HasTrait(TraitDef.Named("Pyromaniac")))
                DrawIcon(bodyLoc, iconNum++, Icons.Pyromaniac, colorYellowAlert);

            // Idle
            if (Settings.ShowIdle && colonist.mindState.IsIdle)
                DrawIcon(bodyLoc, iconNum++, Icons.Idle, colorNeutralStatus);


            // Bad Mood
            if (Settings.ShowSad && colonist.needs.mood.CurLevel < (double)Settings.LimitMoodLess)
                DrawIcon_FadeRedAlertToNeutral(bodyLoc, iconNum++, Icons.Sad, colonist.needs.mood.CurLevel / Settings.LimitMoodLess);

            // Bloodloss
            if (Settings.ShowBloodloss && pawnStats.BleedRate > 0.0f)
                DrawIcon_FadeFloatWithTwoColors(bodyLoc, iconNum++, Icons.Bloodloss, pawnStats.BleedRate, colorRedAlert, colorNeutralStatus);

            //Health
            if (Settings.ShowHealth)
            {
                float pawnHealth = colonist.health.summaryHealth.SummaryHealthPercent;
                //Infection
                if (pawnStats.HasLifeThreateningDisease)
                {
                    if (pawnHealth < pawnStats.HealthDisease)
                        DrawIcon_FadeFloatWithFourColorsHB(bodyLoc, iconNum++, Icons.Health, pawnHealth, colorHealthBarGreen, colorYellowAlert, colorOrangeAlert, colorRedAlert);

                    else DrawIcon_FadeFloatWithFourColorsHB(bodyLoc, iconNum++, Icons.Health, pawnStats.HealthDisease, colorHealthBarGreen, colorYellowAlert, colorOrangeAlert, colorRedAlert);

                }
                else if (colonist.health.summaryHealth.SummaryHealthPercent < 1f)
                {
                    DrawIcon_FadeFloatWithFourColorsHB(bodyLoc, iconNum++, Icons.Health, pawnHealth, colorHealthBarGreen, colorYellowAlert, colorOrangeAlert, colorRedAlert);
                }

                //Toxic buildup
                if (pawnStats.ToxicBuildUp > 0.04f)
                    DrawIcon_FadeFloatFiveColors(bodyLoc, iconNum++, Icons.Toxic, pawnStats.ToxicBuildUp, colorNeutralStatusFade, colorHealthBarGreen, colorYellowAlert, colorOrangeAlert, colorRedAlert);
            }



            // Sickness
            if (Settings.ShowDisease && pawnStats.IsSick)
            {
                if (pawnStats.DiseaseDisappearance < Settings.LimitDiseaseLess)
                {
                    DrawIcon_FadeFloatWithFourColorsHB(bodyLoc, iconNum++, Icons.Sickness, pawnStats.DiseaseDisappearance / Settings.LimitDiseaseLess, colorNeutralStatus, colorYellowAlert, colorOrangeAlert, colorRedAlert);
                }
                else
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.Sickness, colorNeutralStatus);
                }

            }

            // Pain

            if (Settings.ShowPain)
            {
                if (colonist.story.traits.HasTrait(TraitDef.Named("Masochist")))
                {
                    if (pawnStats.PainMoodLevel == 0)
                        DrawIcon(bodyLoc, iconNum++, Icons.Pain, colorMoodBoost *0.4f);
                    if (pawnStats.PainMoodLevel == 1)
                        DrawIcon(bodyLoc, iconNum++, Icons.Pain, colorMoodBoost *0.6f);
                    if (pawnStats.PainMoodLevel == 2)
                        DrawIcon(bodyLoc, iconNum++, Icons.Pain, colorMoodBoost *0.8f);
                    if (pawnStats.PainMoodLevel == 3)
                        DrawIcon(bodyLoc, iconNum++, Icons.Pain, colorMoodBoost);
                }
                else
                {
                    // pain is always worse, +5 to the icon color
                    if (pawnStats.PainMoodLevel == 0)
                        DrawIcon(bodyLoc, iconNum++, Icons.Pain, color10To06);
                    if (pawnStats.PainMoodLevel == 1)
                        DrawIcon(bodyLoc, iconNum++, Icons.Pain, color15To11);
                    if (pawnStats.PainMoodLevel == 2)
                        DrawIcon(bodyLoc, iconNum++, Icons.Pain, color20To16);
                    if (pawnStats.PainMoodLevel == 3)
                        DrawIcon(bodyLoc, iconNum++, Icons.Pain, color25To21);
                }

            }

            if (Settings.ShowDisease)
            {
                if (colonist.health.ShouldBeTendedNow && !colonist.health.ShouldDoSurgeryNow)
                    DrawIcon(bodyLoc, iconNum++, Icons.MedicalAttention, colorOrangeAlert);
                else if (colonist.health.ShouldBeTendedNow && colonist.health.ShouldDoSurgeryNow)
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.MedicalAttention, colorYellowAlert);
                    DrawIcon(bodyLoc, iconNum++, Icons.MedicalAttention, colorOrangeAlert);
                }
                else if (colonist.health.ShouldDoSurgeryNow)
                    DrawIcon(bodyLoc, iconNum++, Icons.MedicalAttention, colorYellowAlert);
            }

            // Hungry
            if (Settings.ShowHungry && colonist.needs.food.CurLevel < (double)Settings.LimitFoodLess)
                DrawIcon_FadeRedAlertToNeutral(bodyLoc, iconNum++, Icons.Hungry,
                    colonist.needs.food.CurLevel / Settings.LimitFoodLess);

            //Tired
            if (Settings.ShowTired && colonist.needs.rest.CurLevel < (double)Settings.LimitRestLess)
                DrawIcon_FadeRedAlertToNeutral(bodyLoc, iconNum++, Icons.Tired,
                    colonist.needs.rest.CurLevel / Settings.LimitRestLess);

            // Too Cold & too hot
            if (Settings.ShowCold && pawnStats.TooCold > 0f)
            {
                if (pawnStats.TooCold >= 0f)
                {
                    if (pawnStats.TooCold <= 1f)
                        DrawIcon_FadeFloatWithTwoColors(bodyLoc, iconNum++, Icons.Freezing, pawnStats.TooCold, colorNeutralStatusFade, colorYellowAlert);
                    else if (pawnStats.TooCold <= 1.5f)
                        DrawIcon_FadeFloatWithTwoColors(bodyLoc, iconNum++, Icons.Freezing, (pawnStats.TooCold - 1f) * 2f, colorYellowAlert, colorOrangeAlert);
                    else
                        DrawIcon_FadeFloatWithTwoColors(bodyLoc, iconNum++, Icons.Freezing, (pawnStats.TooCold - 1.5f) * 2f, colorOrangeAlert, colorRedAlert);
                }
            }
            else if (Settings.ShowHot && pawnStats.TooHot > 0f && pawnStats.TooCold >= 0f)
            {
                if (pawnStats.TooHot <= 1f)
                    DrawIcon_FadeFloatWithTwoColors(bodyLoc, iconNum++, Icons.Hot, pawnStats.TooHot, colorNeutralStatusFade, colorYellowAlert);
                else if (pawnStats.TooHot <= 1.5f)
                    DrawIcon_FadeFloatWithTwoColors(bodyLoc, iconNum++, Icons.Hot, pawnStats.TooHot, colorYellowAlert, colorOrangeAlert);
                else
                    DrawIcon_FadeFloatWithTwoColors(bodyLoc, iconNum++, Icons.Hot, pawnStats.TooHot - 1f, colorOrangeAlert, colorRedAlert);
            }


            // Bed status
            if (Settings.ShowBedroom && !pawnStats.HasBed)
                DrawIcon(bodyLoc, iconNum++, Icons.Bedroom, color10To06);


            // Usage of bed ...
            if (Settings.ShowLovers && HasMood(colonist, ThoughtDef.Named("WantToSleepWithSpouseOrLover")))
            {
                DrawIcon(bodyLoc, iconNum++, Icons.Love, colorYellowAlert);
            }

            //    if (Settings.ShowLovers && HasMood(colonist, ThoughtDef.Named("GotSomeLovin")))
            //    {
            //        DrawIcon(bodyLoc, iconNum++, Icons.Love, colorMoodBoost);
            //    }



            if (Settings.ShowLovers && HasMood(colonist, ThoughtDef.Named("GotMarried")))
            {
                DrawIcon(bodyLoc, iconNum++, Icons.Marriage, colorMoodBoost);
            }

            if (Settings.ShowLovers && HasMood(colonist, ThoughtDef.Named("HoneymoonPhase")))
            {
                DrawIcon(bodyLoc, iconNum++, Icons.Marriage, colorMoodBoost / 2);
            }

            if (Settings.ShowLovers && HasMood(colonist, ThoughtDef.Named("AttendedWedding")))
            {
                DrawIcon(bodyLoc, iconNum++, Icons.Marriage, colorMoodBoost / 4);
            }

            // Naked
            if (Settings.ShowNaked && HasMood(colonist, ThoughtDef.Named("Naked")))
            {
                DrawIcon(bodyLoc, iconNum++, Icons.Naked, color10To06);
            }

            // Apparel
            if (Settings.ShowApparelHealth && pawnStats.ApparelHealth < (double)Settings.LimitApparelHealthLess)
            {
                double pawnApparelHealth = pawnStats.ApparelHealth / (double)Settings.LimitApparelHealthLess;
                DrawIcon_FadeRedAlertToNeutral(bodyLoc, iconNum++, Icons.ApparelHealth, (float)pawnApparelHealth);
            }

            // Moods caused by traits

            if (Settings.ShowProsthophile && HasMood(colonist, ThoughtDef.Named("ProsthophileNoProsthetic")))
            {
                DrawIcon(bodyLoc, iconNum++, Icons.Prosthophile, color05AndLess);
            }

            if (Settings.ShowProsthophobe && HasMood(colonist, ThoughtDef.Named("ProsthophobeUnhappy")))
            {
                DrawIcon(bodyLoc, iconNum++, Icons.Prosthophobe, color10To06);
            }

            if (Settings.ShowNightOwl && HasMood(colonist, ThoughtDef.Named("NightOwlDuringTheDay")))
            {
                DrawIcon(bodyLoc, iconNum++, Icons.NightOwl, color10To06);
            }

            if (Settings.ShowGreedy && HasMood(colonist, ThoughtDef.Named("Greedy")))
            {
                DrawIcon(bodyLoc, iconNum++, Icons.Greedy, color10To06);
            }

            if (Settings.ShowJealous && HasMood(colonist, ThoughtDef.Named("Jealous")))
            {
                DrawIcon(bodyLoc, iconNum++, Icons.Jealous, color10To06);
            }


            // Effectiveness
            if (Settings.ShowEffectiveness && pawnStats.TotalEfficiency < (double)Settings.LimitEfficiencyLess)
                DrawIcon_FadeRedAlertToNeutral(bodyLoc, iconNum++, Icons.Effectiveness,
                    pawnStats.TotalEfficiency / Settings.LimitEfficiencyLess);






            // Bad thoughts

            if (Settings.ShowLeftUnburied && HasMood(colonist, ThoughtDef.Named("ColonistLeftUnburied")))
            {
                DrawIcon(bodyLoc, iconNum++, Icons.LeftUnburied, color10To06);
            }

            if (Settings.ShowDeadColonists)
            {
                // Close Family & friends / 25





                //




                // not family, more whiter icon
                if (HasMood(colonist, ThoughtDef.Named("KilledColonist")))
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, color05AndLess);
                }

                if (HasMood(colonist, ThoughtDef.Named("KilledColonyAnimal")))
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, color05AndLess);
                }

                #region DeathMemory
                //Deathmemory
                // some of those need staging - to do
                if (HasMood(colonist, ThoughtDef.Named("KnowGuestExecuted")))
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, color05AndLess);
                }
                if (HasMood(colonist, ThoughtDef.Named("KnowColonistExecuted")))
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, color05AndLess);
                }
                if (HasMood(colonist, ThoughtDef.Named("KnowPrisonerDiedInnocent")))
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, color10To06);
                }
                if (HasMood(colonist, ThoughtDef.Named("KnowColonistDied")))
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, color05AndLess);
                }
                //Bonded animal died
                if (HasMood(colonist, ThoughtDef.Named("BondedAnimalDied")))
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, color10To06);
                }
                // Friend / rival died
                if (HasMood(colonist, ThoughtDef.Named("PawnWithGoodOpinionDied")))
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, color10To06);
                }

                if (HasMood(colonist, ThoughtDef.Named("PawnWithBadOpinionDied")))
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, colorMoodBoost);
                }

                #endregion

                #region DeathMemoryFamily

                if (HasMood(colonist, ThoughtDef.Named("MySonDied")))
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, color25To21);
                }

                if (HasMood(colonist, ThoughtDef.Named("MyDaughterDied")))
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, color25To21);
                }

                if (HasMood(colonist, ThoughtDef.Named("MyHusbandDied")))
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, color25To21);
                }

                if (HasMood(colonist, ThoughtDef.Named("MyWifeDied")))
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, color25To21);
                }

                if (HasMood(colonist, ThoughtDef.Named("MyFianceDied")))
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, color20To16);
                }

                if (HasMood(colonist, ThoughtDef.Named("MyFianceeDied")))
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, color20To16);
                }

                if (HasMood(colonist, ThoughtDef.Named("MyLoverDied")))
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, color20To16);
                }

                if (HasMood(colonist, ThoughtDef.Named("MyBrotherDied")))
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, color15To11);
                }

                if (HasMood(colonist, ThoughtDef.Named("MySisterDied")))
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, color15To11);
                }

                if (HasMood(colonist, ThoughtDef.Named("MyGrandchildDied")))
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, color15To11);
                }

                // 10

                if (HasMood(colonist, ThoughtDef.Named("MyFatherDied")))
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, color10To06);
                }

                if (HasMood(colonist, ThoughtDef.Named("MyMotherDied")))
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, color10To06);
                }

                if (HasMood(colonist, ThoughtDef.Named("MyNieceDied")))
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, color05AndLess);
                }

                if (HasMood(colonist, ThoughtDef.Named("MyNephewDied")))
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, color05AndLess);
                }

                if (HasMood(colonist, ThoughtDef.Named("MyHalfSiblingDied")))
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, color05AndLess);
                }

                if (HasMood(colonist, ThoughtDef.Named("MyAuntDied")))
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, color05AndLess);
                }

                if (HasMood(colonist, ThoughtDef.Named("MyUncleDied")))
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, color05AndLess);
                }

                if (HasMood(colonist, ThoughtDef.Named("MyGrandparentDied")))
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, color05AndLess);
                }

                if (HasMood(colonist, ThoughtDef.Named("MyCousinDied")))
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, color05AndLess);
                }
                if (HasMood(colonist, ThoughtDef.Named("MyKinDied")))
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, color05AndLess);
                }

                #endregion

                //non family
                if (HasMood(colonist, ThoughtDef.Named("WitnessedDeathAlly")))
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, color05AndLess);
                }
                if (HasMood(colonist, ThoughtDef.Named("WitnessedDeathNonAlly")))
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, color05AndLess);
                }

                if (HasMood(colonist, ThoughtDef.Named("WitnessedDeathBloodlust")))
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, colorMoodBoost);
                }
                if (HasMood(colonist, ThoughtDef.Named("KilledHumanlikeBloodlust")))
                {
                    DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, colorMoodBoost);
                }



                //    if (HasMood(colonist, ThoughtDef.Named("KilledMajorColonyEnemy")))
                //    {
                //        DrawIcon(bodyLoc, iconNum++, Icons.DeadColonist, colorMoodBoost);
                //    }

                // Crowded missing since A14?

                if (Settings.ShowRoomStatus && pawnStats.CrowdedMoodLevel != 0)
                {
                    if (pawnStats.CrowdedMoodLevel == 0)
                        DrawIcon(bodyLoc, iconNum++, Icons.Crowded, colorNeutralStatusFade);
                    if (pawnStats.CrowdedMoodLevel == 1)
                        DrawIcon(bodyLoc, iconNum++, Icons.Crowded, colorYellowAlert);
                    if (pawnStats.CrowdedMoodLevel == 2)
                        DrawIcon(bodyLoc, iconNum++, Icons.Crowded, colorOrangeAlert);
                }
            }

        }

        #endregion
    }
}