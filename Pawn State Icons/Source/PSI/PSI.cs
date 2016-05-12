using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;


namespace PSI
{
    // ReSharper disable once ClassNeverInstantiated.Global
    // ReSharper disable once InconsistentNaming
    internal class PSI : MonoBehaviour
    {
        private static double _fDelta;

        private static bool _inGame;

        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private static Dictionary<Pawn, PawnStats> _statsDict = new Dictionary<Pawn, PawnStats>();

        private static bool _iconsEnabled = true;

        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private static Dialog_Settings _settingsDialog = new Dialog_Settings();

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
                PawnCapacityDefOf.Talking,
            };

            if (reloadSettings)
            {
                Settings = LoadSettings();
            }
            if (reloadIconSet)
            {
                Materials = new Materials(Settings.IconSet);
                ModSettings modSettings = XmlLoader.ItemFromXmlFile<ModSettings>(GenFilePaths.CoreModsFolderPath + "/Pawn State Icons/Textures/UI/Overlays/PawnStateIcons/" + Settings.IconSet + "/iconset.cfg");
                Settings.IconSizeMult = modSettings.IconSizeMult;
                Materials.ReloadTextures(true);
            }
            if (recalcIconPos)
            {
                RecalcIconPositions();
            }
        }

        public static ModSettings LoadSettings(string path = "psi-settings.cfg")
        {
            ModSettings result = XmlLoader.ItemFromXmlFile<ModSettings>(path);
            string path2 = GenFilePaths.CoreModsFolderPath + "/Pawn State Icons/Textures/UI/Overlays/PawnStateIcons/";
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
            XmlSaver.SaveDataObject(Settings, path);
        }

        #region Draw icons

        private static void DrawIcon(Vector3 bodyPos, Vector3 posOffset, Icons icon, Color color)
        {

            var material = Materials[icon];
            if (material == null)
                return;
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                material.color = color;
                var guiColor = GUI.color;
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
                var num1 = _worldScale;
                if (Settings.IconsScreenScale)
                    num1 = 45f;
                var num2 = num1 * (Settings.IconSizeMult * 0.5f);
                var position = new Rect(vector2.x, vector2.y, num2 * Settings.IconSize, num2 * Settings.IconSize);
                position.x -= position.width * 0.5f;
                position.y -= position.height * 0.5f;
                GUI.DrawTexture(position, material.mainTexture, ScaleMode.ScaleToFit, true);
                GUI.color = guiColor;
            });

        }

        private static void DrawIcon(Vector3 bodyPos, int num, Icons icon, Color color)
        {
            DrawIcon(bodyPos, _iconPosVectors[num], icon, color);
        }

        private static void DrawIcon(Vector3 bodyPos, int num, Icons icon, float v)
        {
            DrawIcon(bodyPos, num, icon, new Color(1f, v, v, Settings.IconTransparancy));
         //   DrawIcon(bodyPos, num, icon, new Color(1f, v, v));
        }

        private static void DrawIcon(Vector3 bodyPos, int num, Icons icon, float v, Color c1, Color c2)
        {
            v = v*Settings.IconTransparancy;
            DrawIcon(bodyPos, num, icon, Color.Lerp(c1, c2, v));
        }

        private static void DrawIcon(Vector3 bodyPos, int num, Icons icon, float v, Color c1, Color c2, Color c3)
        {
            //check how to change trans here
            DrawIcon(bodyPos, num, icon,
                v < 0.5 ? Color.Lerp(c1, c2, v * 2f) : Color.Lerp(c2, c3, (float)((v - 0.5) * 2.0)));
        }

        private static void RecalcIconPositions()
        {
            _iconPosVectors = new Vector3[18];
            for (var index = 0; index < _iconPosVectors.Length; ++index)
            {
                var num1 = index / Settings.IconsInColumn;
                var num2 = index % Settings.IconsInColumn;
                if (Settings.IconsHorizontal)
                {
                    var num3 = num1;
                    num1 = num2;
                    num2 = num3;
                }
                _iconPosVectors[index] = new Vector3((float)(-0.600000023841858 * Settings.IconDistanceX - 0.550000011920929 * Settings.IconSize * Settings.IconOffsetX * num1), 3f, (float)(-0.600000023841858 * Settings.IconDistanceY + 0.550000011920929 * Settings.IconSize * Settings.IconOffsetY * num2));
            }
        }

        #endregion

        private static void UpdateColonistStats(Pawn colonist)
        {

            if (!_statsDict.ContainsKey(colonist))
            {
                _statsDict.Add(colonist, new PawnStats());
            }

            PawnStats pawnStats = _statsDict[colonist];


            // efficiency
            float efficiency = 10f;

            var array = _pawnCapacities;
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
                if (curDriver is JobDriver_HaulToContainer || curDriver is JobDriver_HaulToCell || curDriver is JobDriver_FoodDeliver || curDriver is JobDriver_FoodFeedPatient || curDriver is JobDriver_TakeToBed)
                {
                    targetInfo = curJob.targetB;
                }
                if (curDriver is JobDriver_DoBill)
                {
                    JobDriver_DoBill jobDriverDoBill = (JobDriver_DoBill)curDriver;
                    if (jobDriverDoBill.workLeft == 0f)
                    {
                        targetInfo = curJob.targetA;
                    }
                    else if (jobDriverDoBill.workLeft <= 0.01f)
                    {
                        targetInfo = curJob.targetB;
                    }
                }
                if (curDriver is JobDriver_Hunt && colonist.carrier != null && colonist.carrier.CarriedThing != null)
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
            var temperatureForCell = GenTemperature.GetTemperatureForCell(colonist.Position);
            pawnStats.TooCold = (float)((colonist.ComfortableTemperatureRange().min - (double)Settings.LimitTempComfortOffset - temperatureForCell) / 10f);
            pawnStats.TooHot = (float)((temperatureForCell - (double)colonist.ComfortableTemperatureRange().max - Settings.LimitTempComfortOffset) / 10f);
            pawnStats.TooCold = Mathf.Clamp(pawnStats.TooCold, 0f, 2f);
            pawnStats.TooHot = Mathf.Clamp(pawnStats.TooHot, 0f, 2f);

            // Drunkness
            pawnStats.Drunkness = DrugUtility.DrunknessPercent(colonist);

            // Health Calc
            pawnStats.DiseaseDisappearance = 1f;

            foreach (var hediff in colonist.health.hediffSet.hediffs)
            {
                var hediffWithComps = (HediffWithComps)hediff;
                if (hediffWithComps != null
                    && !hediffWithComps.FullyImmune()
                    && hediffWithComps.Visible
                    && !hediffWithComps.IsOld()
                    //             && ((hediffWithComps.CurStage == null || hediffWithComps.CurStage.everVisible) && (hediffWithComps.def.tendable || hediffWithComps.def.naturallyHealed))
                    && hediffWithComps.def.PossibleToDevelopImmunity())

                    pawnStats.DiseaseDisappearance = Math.Min(pawnStats.DiseaseDisappearance, colonist.health.immunity.GetImmunity(hediffWithComps.def));
            }

            // Apparel Calc
            float num2 = 999f;
            List<Apparel> wornApparel = colonist.apparel.WornApparel;
            for (int j = 0; j < wornApparel.Count; j++)
            {
                float HitpointsPercent = (float)wornApparel[j].HitPoints / (float)wornApparel[j].MaxHitPoints;
                if (HitpointsPercent >= 0f && HitpointsPercent < num2)
                {
                    num2 = HitpointsPercent;
                }
            }
            pawnStats.ApparelHealth = num2;

            // Bleed rate

            pawnStats.BleedRate = Mathf.Clamp01(colonist.health.hediffSet.BleedingRate * Settings.LimitBleedMult);

            // Bed status
            if (colonist.ownership.OwnedBed != null)
                //    if (colonist.ownership.OwnedBed.SleepingSlotsCount >= 1)
                pawnStats.HasBed = true;
            if (colonist.ownership.OwnedBed == null)
            {
                pawnStats.HasBed = false;
            }
            if (colonist.health.hediffSet.AnyHediffMakesSickThought)
                pawnStats.IsSick = true;
            else
            {
                pawnStats.IsSick = false;
            }
            _statsDict[colonist] = pawnStats;
        }

        public static Boolean HasMood(Pawn pawn, ThoughtDef tdef)
        {
            if (pawn.needs.mood.thoughts.DistinctThoughtDefs.Contains(tdef))
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        // ReSharper disable once UnusedMember.Global

        public virtual void FixedUpdate()
        {
            _fDelta += Time.fixedDeltaTime;

            if (_fDelta < 0.1)
                return;
            _fDelta = 0.0;
            _inGame = GameObject.Find("CameraMap");

            if (!_inGame || !_iconsEnabled)
                return;

            foreach (var colonist in Find.Map.mapPawns.FreeColonistsAndPrisonersSpawned) //.FreeColonistsAndPrisoners)
            {
                if (colonist.SelectableNow())
                {
                    try
                    {
                        UpdateColonistStats(colonist);
                    }
                    catch (Exception ex)
                    {
                        Log.Notify_Exception(ex);
                    }
                }

                //UpdateColonistStats(colonist);
            }
        }

        public void UpdateOptionsDialog()
        {
            Dialog_Options dialogOptions = Find.WindowStack.WindowOfType<Dialog_Options>();
            bool flag = dialogOptions != null;
            bool flag2 = Find.WindowStack.IsOpen(typeof(Dialog_Settings));
            if (flag && flag2)
            {
                PSI._settingsDialog.OptionsDialog = dialogOptions;
                PSI.RecalcIconPositions();
                return;
            }
            if (flag && !flag2)
            {
                if (!PSI._settingsDialog.CloseButtonClicked)
                {
                    Find.UIRoot.windows.Add(PSI._settingsDialog);
                    PSI._settingsDialog.Page = "main";
                    return;
                }
                dialogOptions.Close(true);
                return;
            }
            else
            {
                if (!flag && flag2)
                {
                    PSI._settingsDialog.Close(false);
                    return;
                }
                if (!flag && !flag2)
                {
                    PSI._settingsDialog.CloseButtonClicked = false;
                }
                return;
            }
        }

        private static void DrawAnimalIcons(Pawn animal)
        {
            var transparancy = Settings.IconTransparancy;
            Color colorRedAlert = new Color(1f, 0f, 0f, transparancy);

            if (animal.Dead || animal.holder != null)
                return;
            var drawPos = animal.DrawPos;

            if (!Settings.ShowAggressive || animal.MentalStateDef != MentalStateDefOf.Berserk && animal.MentalStateDef != MentalStateDefOf.Manhunter)
                return;
            var bodyPos = drawPos;
            DrawIcon(bodyPos, 0, Icons.Aggressive, colorRedAlert);
        }

        private static void DrawColonistIcons(Pawn colonist)
        {
            var transparancy = Settings.IconTransparancy;

            Color color25To21 = new Color(1f,0f,0f, transparancy);

            Color color20To16 = new Color(1f, 0.5f, 0f, transparancy);

            Color color15To11 = new Color(1.0f,1.0f,0f, transparancy);

            Color color10To06 = new Color(1f, 1f, 0.5f, transparancy);

            Color color05AndLess = new Color(1f,1f,1f, transparancy);

            Color colorMoodBoost = new Color(0f,1f,0f, transparancy);

            Color colorNeutralStatus = new Color(1f, 1f, 1f, transparancy);

            Color colorRedAlert = new Color(1f, 0f, 0f, transparancy);

            Color colorOrangeAlert = new Color(1f, 0.5f, 0f, transparancy);

            Color colorYellowAlert = new Color(1f, 1f, 0f, transparancy);

            //  Color color25To21 = Color.red;
            //
            //  Color color20To16 = new Color(1f, 0.5f, 0f);
            //
            //  Color color15To11 = Color.yellow;
            //
            //  Color color10To06 = new Color(1f, 1f, 0.5f);
            //
            //  Color color05AndLess = Color.white;
            //
            //  Color colorMoodBoost = Color.green;

            var num1 = 0;
            PawnStats pawnStats;
            if (colonist.Dead || colonist.holder != null || (!_statsDict.TryGetValue(colonist, out pawnStats) || colonist.drafter == null) || colonist.skills == null)
                return;

            var drawPos = colonist.DrawPos;

            // Pacifc + Unarmed
            if (colonist.skills.GetSkill(SkillDefOf.Melee).TotallyDisabled && colonist.skills.GetSkill(SkillDefOf.Shooting).TotallyDisabled)
            {
                if (Settings.ShowPacific)
                    DrawIcon(drawPos, num1++, Icons.Pacific, colorNeutralStatus);
            }
            else if (Settings.ShowUnarmed && colonist.equipment.Primary == null && !colonist.IsPrisonerOfColony)
                DrawIcon(drawPos, num1++, Icons.Unarmed, colorNeutralStatus);

            // Idle
            if (Settings.ShowIdle && colonist.mindState.IsIdle)
                DrawIcon(drawPos, num1++, Icons.Idle, colorNeutralStatus);

            //Drafted
            if (Settings.ShowDraft && colonist.drafter.Drafted)
                DrawIcon(drawPos, num1++, Icons.Draft, Color.white); // might change status color

            // Bad Mood
            if (Settings.ShowSad && colonist.needs.mood.CurLevel < (double)Settings.LimitMoodLess)
                DrawIcon(drawPos, num1++, Icons.Sad, colonist.needs.mood.CurLevel / Settings.LimitMoodLess);

            // Hungry
            if (Settings.ShowHungry && colonist.needs.food.CurLevel < (double)Settings.LimitFoodLess)
                DrawIcon(drawPos, num1++, Icons.Hungry, colonist.needs.food.CurLevel / Settings.LimitFoodLess);

            //Tired
            if (Settings.ShowTired && colonist.needs.rest.CurLevel < (double)Settings.LimitRestLess)
                DrawIcon(drawPos, num1++, Icons.Tired, colonist.needs.rest.CurLevel / Settings.LimitRestLess);

            // Too Cold & too hot --- change to transparancy?!?
            if (Settings.ShowCold && pawnStats.TooCold > 0.0)
            {
                if (pawnStats.TooCold >= 0.0)
                {
                    if (pawnStats.TooCold <= 1.0)
                        DrawIcon(drawPos, num1++, Icons.Freezing, pawnStats.TooCold, new Color(1f, 1f, 1f, 0.3f), new Color(0.86f, 0.86f, 1f, 1f));
                    else if (pawnStats.TooCold <= 1.5)
                        DrawIcon(drawPos, num1++, Icons.Freezing, (float)((pawnStats.TooCold - 1.0) * 2.0), new Color(0.86f, 0.86f, 1f, 1f), new Color(1f, 0.86f, 0.86f));
                    else
                        DrawIcon(drawPos, num1++, Icons.Freezing, (float)((pawnStats.TooCold - 1.5) * 2.0), new Color(1f, 0.86f, 0.86f), Color.red);
                }
            }
            else if (Settings.ShowHot && pawnStats.TooHot > 0.0 && pawnStats.TooCold >= 0.0)
            {
                if (pawnStats.TooHot <= 1.0)
                    DrawIcon(drawPos, num1++, Icons.Hot, pawnStats.TooHot, new Color(1f, 1f, 1f, 0.3f), new Color(1f, 0.7f, 0.0f, 1f));
                else
                    DrawIcon(drawPos, num1++, Icons.Hot, pawnStats.TooHot - 1f, new Color(1f, 0.7f, 0.0f, 1f), Color.red);
            }

            // Mental States
            if (Settings.ShowAggressive && colonist.MentalStateDef == MentalStateDefOf.Berserk)
                DrawIcon(drawPos, num1++, Icons.Aggressive, colorRedAlert);

            if (Settings.ShowLeave && colonist.MentalStateDef == MentalStateDefOf.GiveUpExit)
                DrawIcon(drawPos, num1++, Icons.Leave, colorRedAlert);

            if (Settings.ShowDazed && colonist.MentalStateDef == MentalStateDefOf.DazedWander)
                DrawIcon(drawPos, num1++, Icons.Dazed, colorYellowAlert);

            if (colonist.MentalStateDef == MentalStateDefOf.PanicFlee)
                DrawIcon(drawPos, num1++, Icons.Panic, colorYellowAlert);

            // Binging on alcohol
            if (Settings.ShowDrunk)
            {
                if (colonist.MentalStateDef == MentalStateDefOf.BingingAlcohol)
                    DrawIcon(drawPos, num1++, Icons.Drunk, colorRedAlert);
                else if (pawnStats.Drunkness > 0.05)
                    DrawIcon(drawPos, num1++, Icons.Drunk, pawnStats.Drunkness, new Color(1f, 1f, 1f, 0.2f), colorNeutralStatus, colorRedAlert);
            }

            // Effectiveness
            if (Settings.ShowEffectiveness && pawnStats.TotalEfficiency < (double)Settings.LimitEfficiencyLess)
                DrawIcon(drawPos, num1++, Icons.Effectiveness, pawnStats.TotalEfficiency / Settings.LimitEfficiencyLess);

            // Disease
            if (Settings.ShowDisease)
            {
                if (pawnStats.IsSick)
                    DrawIcon(drawPos, num1++, Icons.Sick, colorNeutralStatus);

                if (colonist.health.ShouldBeTendedNow && !colonist.health.ShouldDoSurgeryNow)
                    DrawIcon(drawPos, num1++, Icons.MedicalAttention, colorOrangeAlert);
                else
                if (colonist.health.ShouldBeTendedNow && colonist.health.ShouldDoSurgeryNow)
                {
                    DrawIcon(drawPos, num1++, Icons.MedicalAttention, colorYellowAlert);
                    DrawIcon(drawPos, num1++, Icons.MedicalAttention, colorOrangeAlert);
                }
                else
                if (colonist.health.ShouldDoSurgeryNow)
                    DrawIcon(drawPos, num1++, Icons.MedicalAttention, colorYellowAlert);

                if (Settings.ShowDisease && pawnStats.IsSick && pawnStats.DiseaseDisappearance < Settings.LimitDiseaseLess)
                    DrawIcon(drawPos, num1++, Icons.Disease, pawnStats.DiseaseDisappearance / Settings.LimitDiseaseLess);
            }

            // Bloodloss
            if (Settings.ShowBloodloss && pawnStats.BleedRate > 0.0f)
                DrawIcon(drawPos, num1++, Icons.Bloodloss, pawnStats.BleedRate, colorRedAlert, colorNeutralStatus);


            // Apparel
            if (Settings.ShowApparelHealth && pawnStats.ApparelHealth < (double)Settings.LimitApparelHealthLess)
            {
                var pawnApparelHealth = pawnStats.ApparelHealth / (double)Settings.LimitApparelHealthLess;
                DrawIcon(drawPos, num1++, Icons.ApparelHealth, (float)pawnApparelHealth);
            }

            // Target Point 
            if (Settings.ShowTargetPoint && (pawnStats.TargetPos != Vector3.zero || pawnStats.TargetPos != null))
                DrawIcon(pawnStats.TargetPos, Vector3.zero, Icons.Target, colorNeutralStatus);

            // Traits and bad thoughts

            // Bed status
            if (Settings.ShowBedroom && !pawnStats.HasBed)
                DrawIcon(drawPos, num1++, Icons.Bedroom, color10To06);


            // Moods

            foreach (var thoughtDef in colonist.needs.mood.thoughts.Thoughts.ToArray())
            {

                if (Settings.ShowRoomStatus && thoughtDef.def.defName.Equals("Crowded"))
                {
                    var thoughtStage = thoughtDef.CurStage;
                    if (thoughtStage.baseMoodEffect == -20f)
                        DrawIcon(drawPos, num1++, Icons.Crowded, color20To16);
                    if (thoughtStage.baseMoodEffect == -12f)
                        DrawIcon(drawPos, num1++, Icons.Crowded, color15To11);
                    if (thoughtStage.baseMoodEffect == -5f)
                        DrawIcon(drawPos, num1++, Icons.Crowded, color05AndLess);
                }

                if (Settings.ShowPain && thoughtDef.def.defName.Equals("Pain"))
                {
                    var thoughtStage = thoughtDef.CurStage;
                    // pain is always worse, +5 to the icon color
                    if (thoughtStage.baseMoodEffect < -19.5f)
                        DrawIcon(drawPos, num1++, Icons.Pain, color25To21);
                    if (thoughtStage.baseMoodEffect < -14.5f && thoughtStage.baseMoodEffect > -15.5f)
                        DrawIcon(drawPos, num1++, Icons.Pain, color20To16);
                    if (thoughtStage.baseMoodEffect < -9.5f && thoughtStage.baseMoodEffect > -10.5f)
                        DrawIcon(drawPos, num1++, Icons.Pain, color15To11);
                    if (thoughtStage.baseMoodEffect < 0f && thoughtStage.baseMoodEffect > -5.5f)
                        DrawIcon(drawPos, num1++, Icons.Pain, color10To06);

                    // var thoughtStage = thoughtDef.CurStage;
                    // if (thoughtStage.baseMoodEffect == -20f)
                    //     DrawIcon(drawPos, num1++, Icons.Pain, color25To21);
                    // if (thoughtStage.baseMoodEffect == -15f)
                    //     DrawIcon(drawPos, num1++, Icons.Pain, color20To16);
                    // if (thoughtStage.baseMoodEffect == -10f)
                    //     DrawIcon(drawPos, num1++, Icons.Pain, color15To11);
                    // if (thoughtStage.baseMoodEffect == -5f)
                    //     DrawIcon(drawPos, num1++, Icons.Pain, color10To06);
                }

            }



            //   DrawIcon(drawPos, num1++, Icons.Crowded, Color.white);


            if (Settings.ShowProsthophile && HasMood(colonist, ThoughtDef.Named("ProsthophileNoProsthetic")))
            {
                DrawIcon(drawPos, num1++, Icons.Prosthophile, color05AndLess);
            }

            if (Settings.ShowProsthophobe && HasMood(colonist, ThoughtDef.Named("ProsthophobeUnhappy")))
            {
                DrawIcon(drawPos, num1++, Icons.Prosthophobe, color10To06);
            }

            if (Settings.ShowNightOwl && HasMood(colonist, ThoughtDef.Named("NightOwlDuringTheDay")))
            {
                DrawIcon(drawPos, num1++, Icons.NightOwl, color10To06);
            }

            if (Settings.ShowGreedy && HasMood(colonist, ThoughtDef.Named("Greedy")))
            {
                DrawIcon(drawPos, num1++, Icons.Greedy, color10To06);
            }

            if (Settings.ShowJealous && HasMood(colonist, ThoughtDef.Named("Jealous")))
            {
                DrawIcon(drawPos, num1++, Icons.Jealous, color10To06);
            }

            if (Settings.ShowLovers && HasMood(colonist, ThoughtDef.Named("WantToSleepWithSpouseOrLover")))
            {
                DrawIcon(drawPos, num1++, Icons.Love, color05AndLess);
            }

            if (Settings.ShowNaked && HasMood(colonist, ThoughtDef.Named("Naked")))
            {
                DrawIcon(drawPos, num1++, Icons.Naked, color10To06);
            }

            if (Settings.ShowLeftUnburied && HasMood(colonist, ThoughtDef.Named("ColonistLeftUnburied")))
            {
                DrawIcon(drawPos, num1++, Icons.LeftUnburied, color10To06);
            }

            if (Settings.ShowDeadColonists)
            {
                // Close Family & friends / 25



                if (HasMood(colonist, ThoughtDef.Named("MySonDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color25To21);
                }

                if (HasMood(colonist, ThoughtDef.Named("MyDaughterDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color25To21);
                }

                if (HasMood(colonist, ThoughtDef.Named("MyFianceDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color25To21);
                }

                if (HasMood(colonist, ThoughtDef.Named("MyFianceeDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color25To21);
                }

                if (HasMood(colonist, ThoughtDef.Named("MyLoverDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color25To21);
                }

                // 20

                if (HasMood(colonist, ThoughtDef.Named("MyHusbandDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color20To16);
                }

                if (HasMood(colonist, ThoughtDef.Named("MyWifeDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color20To16);
                }

                //

                //
                //friend depends on social
                if (HasMood(colonist, ThoughtDef.Named("PawnWithGoodOpinionDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color10To06);
                }

                // Notsoclose family / 15

                if (HasMood(colonist, ThoughtDef.Named("MyBrotherDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color15To11);
                }

                if (HasMood(colonist, ThoughtDef.Named("MySisterDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color15To11);
                }

                if (HasMood(colonist, ThoughtDef.Named("MyGrandchildDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color15To11);
                }

                // 10

                if (HasMood(colonist, ThoughtDef.Named("MyFatherDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color10To06);
                }

                if (HasMood(colonist, ThoughtDef.Named("MyMotherDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color10To06);
                }

                if (HasMood(colonist, ThoughtDef.Named("MyNieceDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color10To06);
                }

                if (HasMood(colonist, ThoughtDef.Named("MyNephewDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color10To06);
                }

                if (HasMood(colonist, ThoughtDef.Named("MyAuntDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color10To06);
                }

                if (HasMood(colonist, ThoughtDef.Named("MyUncleDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color10To06);
                }

                //


                if (HasMood(colonist, ThoughtDef.Named("BondedAnimalDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color15To11);
                }

                // not family, more whiter icon
                if (HasMood(colonist, ThoughtDef.Named("KilledColonist")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color05AndLess);
                }

                if (HasMood(colonist, ThoughtDef.Named("KilledColonyAnimal")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color05AndLess);
                }

                //Everyone else / < 10
                if (HasMood(colonist, ThoughtDef.Named("MyGrandparentDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color05AndLess);
                }
                if (HasMood(colonist, ThoughtDef.Named("MyHalfSiblingDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color05AndLess);
                }

                if (HasMood(colonist, ThoughtDef.Named("MyCousinDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color05AndLess);
                }
                if (HasMood(colonist, ThoughtDef.Named("MyKinDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color05AndLess);
                }
                if (HasMood(colonist, ThoughtDef.Named("MyGrandparentDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color05AndLess);
                }

                //non family
                if (HasMood(colonist, ThoughtDef.Named("WitnessedDeathAlly")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color05AndLess);
                }
                if (HasMood(colonist, ThoughtDef.Named("WitnessedDeathStranger")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color05AndLess);
                }

                if (HasMood(colonist, ThoughtDef.Named("WitnessedDeathStrangerBloodlust")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, colorMoodBoost);
                }
                if (HasMood(colonist, ThoughtDef.Named("KilledHumanlikeBloodlust")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, colorMoodBoost);
                }

                //Haters
                if (HasMood(colonist, ThoughtDef.Named("PawnWithBadOpinionDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, colorMoodBoost);
                }

                if (HasMood(colonist, ThoughtDef.Named("KilledMajorColonyEnemy")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, colorMoodBoost);
                }
            }
            return;

        }

        // ReSharper disable once InconsistentNaming
        public virtual void OnGUI()
        {
            if (!_inGame || Find.TickManager.Paused)
                UpdateOptionsDialog();
            if (!_iconsEnabled || !_inGame)
                return;
            foreach (var pawn in Find.Map.mapPawns.AllPawns)
            {
                if (pawn?.RaceProps == null) continue;
                if (pawn.RaceProps.Animal)
                    DrawAnimalIcons(pawn);
                else if (pawn.IsColonist || pawn.IsPrisonerOfColony)
                    DrawColonistIcons(pawn);
            }
        }

        public virtual void Update()
        {
            if (!_inGame)
                return;
            if (Input.GetKeyUp(KeyCode.F11))
            {
                _iconsEnabled = !_iconsEnabled;
                Messages.Message(_iconsEnabled ? "PSI.Enabled".Translate() : "PSI.Disabled".Translate(),
                    MessageSound.Standard);
            }
            _worldScale = Screen.height / (2f * Camera.current.orthographicSize);
        }
    }
}
