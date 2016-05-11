using System;
using System.Collections.Generic;
using System.IO;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;


namespace PSI
{

    internal class Psi : MonoBehaviour
    {
        private static double _fDelta;

        private static bool _inGame;

        private static Dictionary<Pawn, PawnStats> _statsDict = new Dictionary<Pawn, PawnStats>();

        private static bool _iconsEnabled = true;

        private static DialogSettings _modSettingsDialog = new DialogSettings();

        public static ModSettings Settings = new ModSettings();

        private static readonly Color TargetColor = new Color(1f, 1f, 1f, 0.6f);

        private static float _worldScale = 1f;

        public static string[] IconSets = { "default" };

        public static Materials Materials = new Materials();

        private static PawnCapacityDef[] _pawnCapacities;

        private static Vector3[] _iconPosVectors;


        public Psi()
        {
            Reinit();
        }

        public static void Reinit(bool reloadSettings = true, bool reloadIconSet = true, bool recalcIconPos = true)
        {
            Psi._pawnCapacities = new PawnCapacityDef[]
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
                Psi.Settings = Psi.LoadSettings("psi-settings.cfg");
            }
            if (reloadIconSet)
            {
                Psi.Materials = new Materials(Psi.Settings.IconSet);
                ModSettings modSettings = XmlLoader.ItemFromXmlFile<ModSettings>(GenFilePaths.CoreModsFolderPath + "/Pawn State Icons/Textures/UI/Overlays/PawnStateIcons/" + Psi.Settings.IconSet + "/iconset.cfg", true);
                Psi.Settings.IconSizeMult = modSettings.IconSizeMult;
                Psi.Materials.ReloadTextures(true);
            }
            if (recalcIconPos)
            {
                Psi.RecalcIconPositions();
            }
        }

        public static ModSettings LoadSettings(string path = "psi-settings.cfg")
        {
            ModSettings result = XmlLoader.ItemFromXmlFile<ModSettings>(GenFilePaths.CoreModsFolderPath + "/Pawn State Icons/" + path, true);
            string path2 = GenFilePaths.CoreModsFolderPath + "/Pawn State Icons/Textures/UI/Overlays/PawnStateIcons/";
            if (Directory.Exists(path2))
            {
                Psi.IconSets = Directory.GetDirectories(path2);
                for (int i = 0; i < Psi.IconSets.Length; i++)
                {
                    Psi.IconSets[i] = new DirectoryInfo(Psi.IconSets[i]).Name;
                }
            }
            return result;
        }

        public static void SaveSettings(string path = "psi-settings.cfg")
        {
            XmlSaver.SaveDataObject(Psi.Settings, GenFilePaths.CoreModsFolderPath + "/Pawn State Icons/" + path);
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
            DrawIcon(bodyPos, num, icon, new Color(1f, v, v));
        }

        private static void DrawIcon(Vector3 bodyPos, int num, Icons icon, float v, Color c1, Color c2)
        {
            DrawIcon(bodyPos, num, icon, Color.Lerp(c1, c2, v));
        }

        private static void DrawIcon(Vector3 bodyPos, int num, Icons icon, float v, Color c1, Color c2, Color c3)
        {
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
                _statsDict.Add(colonist, new PawnStats());

            var pawnStats = _statsDict[colonist];

            // efficiency
            float efficiency = 10f;

            foreach (var activity in _pawnCapacities)
            {
                if (activity != PawnCapacityDefOf.Consciousness)
                    efficiency = Math.Min(efficiency, colonist.health.capacities.GetEfficiency(activity));
            }

            if (efficiency < 0.0)
                efficiency = 0.0f;

            pawnStats.PawnTotalEfficiency = efficiency;

            //target
            pawnStats.TargetPos = Vector3.zero;

            if (colonist.jobs.curJob != null)
            {
                var jobDriver = colonist.jobs.curDriver;
                var job = colonist.jobs.curJob;
                var targetInfo = job.targetA;

                if (jobDriver is JobDriver_HaulToContainer || jobDriver is JobDriver_HaulToCell || (jobDriver is JobDriver_FoodDeliver || jobDriver is JobDriver_FoodFeedPatient) || jobDriver is JobDriver_TakeToBed)
                    targetInfo = job.targetB;

                if (jobDriver is JobDriver_DoBill)
                {
                    var jobDriverDoBill = (JobDriver_DoBill)jobDriver;
                    if (jobDriverDoBill.workLeft == 0.0)
                        targetInfo = job.targetA;
                    else if (jobDriverDoBill.workLeft <= 0.00999999977648258)
                        targetInfo = job.targetB;
                }

                if (jobDriver is JobDriver_Hunt && colonist.carrier?.CarriedThing != null)
                    targetInfo = job.targetB;

                if (job.def == JobDefOf.Wait)
                    targetInfo = null;

                if (jobDriver is JobDriver_Ingest)
                    targetInfo = null;

                if (job.def == JobDefOf.LayDown && colonist.InBed())
                    targetInfo = null;

                if (!job.playerForced && job.def == JobDefOf.Goto)
                    targetInfo = null;

                bool flag;
                if (targetInfo != null)
                {
                    var cell = targetInfo.Cell;
                    flag = false;
                }
                else
                    flag = true;
                if (!flag)
                {
                    var vector3 = targetInfo.Cell.ToVector3Shifted();
                    pawnStats.TargetPos = vector3 + new Vector3(0.0f, 3f, 0.0f);
                }
            }

            // temperature
            var temperatureForCell = GenTemperature.GetTemperatureForCell(colonist.Position);
            pawnStats.PawnTooCold = (float)((colonist.ComfortableTemperatureRange().min - (double)Settings.LimitTempComfortOffset - temperatureForCell) / 10.0);
            pawnStats.PawnTooHot = (float)((temperatureForCell - (double)colonist.ComfortableTemperatureRange().max - Settings.LimitTempComfortOffset) / 10.0);
            pawnStats.PawnTooCold = Mathf.Clamp(pawnStats.PawnTooCold, 0.0f, 2f);
            pawnStats.PawnTooHot = Mathf.Clamp(pawnStats.PawnTooHot, 0.0f, 2f);


            // Health Calc
            pawnStats.DiseaseDisappearance = 1f;
            pawnStats.PawnDrunkness = DrugUtility.DrunknessPercent(colonist);


            foreach (var hediff in colonist.health.hediffSet.hediffs)
            {
                var hediffWithComps = (HediffWithComps)hediff;
                if (hediffWithComps != null
                    && !hediffWithComps.FullyImmune()
                    && (hediffWithComps.Visible
                    && !hediffWithComps.IsOld())
                    && ((hediffWithComps.CurStage == null || hediffWithComps.CurStage.everVisible) && (hediffWithComps.def.tendable || hediffWithComps.def.naturallyHealed))
                    && hediffWithComps.def.PossibleToDevelopImmunity())

                    pawnStats.DiseaseDisappearance = Math.Min(pawnStats.DiseaseDisappearance, colonist.health.immunity.GetImmunity(hediffWithComps.def));
            }

            // Apparel Calc
            var num1 = 999f;
            var wornApparel = colonist.apparel.WornApparel;
            foreach (var apparel in wornApparel)
            {
                var num2 = apparel.HitPoints / (float)apparel.MaxHitPoints;
                if (num2 >= 0.0 && num2 < (double)num1)
                    num1 = num2;
            }

            pawnStats.ApparelHealth = num1;

            pawnStats.BleedRate = Mathf.Clamp01(colonist.health.hediffSet.BleedingRate * Settings.LimitBleedMult);

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

        public virtual void FixedUpdate()
        {
            _fDelta += Time.fixedDeltaTime;

            if (_fDelta < 0.1)
                return;
            _fDelta = 0.0;
            _inGame = GameObject.Find("CameraMap");

            if (!_inGame || !_iconsEnabled)
                return;

            foreach (var colonist in Find.Map.mapPawns.FreeColonistsAndPrisoners)
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
        }

        private static void UpdateOptionsDialog()
        {
            var dialogOptions = Find.WindowStack.WindowOfType<Dialog_Options>();
            var flag1 = dialogOptions != null;
            var flag2 = Find.WindowStack.IsOpen(typeof(DialogSettings));

            if (flag1 && flag2)
            {
                _modSettingsDialog.OptionsDialog = dialogOptions;
                RecalcIconPositions();
            }
            else if (flag1 && !flag2)
            {
                if (!_modSettingsDialog.CloseButtonClicked)
                {
                    Find.UIRoot.windows.Add(_modSettingsDialog);
                    _modSettingsDialog.Page = "main";
                }
                else
                    dialogOptions.Close();
            }
            else if (!flag1 && flag2)
            {
                _modSettingsDialog.Close(false);
            }
            else
            {
                if (flag1 || flag2)
                    return;
                _modSettingsDialog.CloseButtonClicked = false;
            }
        }

        private static void DrawAnimalIcons(Pawn animal)
        {
            if (animal.Dead || animal.holder != null)
                return;
            var drawPos = animal.DrawPos;

            if (!Settings.ShowAggressive || animal.MentalStateDef != MentalStateDefOf.Berserk && animal.MentalStateDef != MentalStateDefOf.Manhunter)
                return;
            var bodyPos = drawPos;
            DrawIcon(bodyPos, 0, Icons.Aggressive, Color.red);
        }

        private static void DrawColonistIcons(Pawn colonist)
        {
            var num1 = 0;
            PawnStats pawnStats;
            if (colonist.Dead || colonist.holder != null || (!_statsDict.TryGetValue(colonist, out pawnStats) || colonist.drafter == null) || colonist.skills == null)
                return;

            var drawPos = colonist.DrawPos;

            // Pacifc + Unarmed
            if (colonist.skills.GetSkill(SkillDefOf.Melee).TotallyDisabled && colonist.skills.GetSkill(SkillDefOf.Shooting).TotallyDisabled)
            {
                if (Settings.ShowPacific)
                    DrawIcon(drawPos, num1++, Icons.Pacific, Color.white);
            }
            else if (Settings.ShowUnarmed && colonist.equipment.Primary == null && !colonist.IsPrisonerOfColony)
                DrawIcon(drawPos, num1++, Icons.Unarmed, Color.white);

            // Idle
            if (Settings.ShowIdle && colonist.mindState.IsIdle)
                DrawIcon(drawPos, num1++, Icons.Idle, Color.white);

            //Drafted
            if (Settings.ShowDraft && colonist.drafter.Drafted)
                DrawIcon(drawPos, num1++, Icons.Draft, Color.white);

            // Bad Mood
            if (Settings.ShowSad && colonist.needs.mood.CurLevel < (double)Settings.LimitMoodLess)
                DrawIcon(drawPos, num1++, Icons.Sad, colonist.needs.mood.CurLevel / Settings.LimitMoodLess);

            // Hungry
            if (Settings.ShowHungry && colonist.needs.food.CurLevel < (double)Settings.LimitFoodLess)
                DrawIcon(drawPos, num1++, Icons.Hungry, colonist.needs.food.CurLevel / Settings.LimitFoodLess);

            //Tired
            if (Settings.ShowTired && colonist.needs.rest.CurLevel < (double)Settings.LimitRestLess)
                DrawIcon(drawPos, num1++, Icons.Tired, colonist.needs.rest.CurLevel / Settings.LimitRestLess);

            // Too Cold & too hot
            if (Settings.ShowCold && pawnStats.PawnTooCold > 0.0)
            {
                if (pawnStats.PawnTooCold >= 0.0)
                {
                    if (pawnStats.PawnTooCold <= 1.0)
                        DrawIcon(drawPos, num1++, Icons.Freezing, pawnStats.PawnTooCold, new Color(1f, 1f, 1f, 0.3f), new Color(0.86f, 0.86f, 1f, 1f));
                    else if (pawnStats.PawnTooCold <= 1.5)
                        DrawIcon(drawPos, num1++, Icons.Freezing, (float)((pawnStats.PawnTooCold - 1.0) * 2.0), new Color(0.86f, 0.86f, 1f, 1f), new Color(1f, 0.86f, 0.86f));
                    else
                        DrawIcon(drawPos, num1++, Icons.Freezing, (float)((pawnStats.PawnTooCold - 1.5) * 2.0), new Color(1f, 0.86f, 0.86f), Color.red);
                }
            }
            else if (Settings.ShowHot && pawnStats.PawnTooHot > 0.0 && pawnStats.PawnTooCold >= 0.0)
            {
                if (pawnStats.PawnTooHot <= 1.0)
                    DrawIcon(drawPos, num1++, Icons.Hot, pawnStats.PawnTooHot, new Color(1f, 1f, 1f, 0.3f), new Color(1f, 0.7f, 0.0f, 1f));
                else
                    DrawIcon(drawPos, num1++, Icons.Hot, pawnStats.PawnTooHot - 1f, new Color(1f, 0.7f, 0.0f, 1f), Color.red);
            }

            // Mental States
            if (Settings.ShowAggressive && colonist.MentalStateDef == MentalStateDefOf.Berserk)
                DrawIcon(drawPos, num1++, Icons.Aggressive, Color.red);

            if (Settings.ShowLeave && colonist.MentalStateDef == MentalStateDefOf.GiveUpExit)
                DrawIcon(drawPos, num1++, Icons.Leave, Color.red);

            if (Settings.ShowDazed && colonist.MentalStateDef == MentalStateDefOf.DazedWander)
                DrawIcon(drawPos, num1++, Icons.Dazed, Color.red);

            if (colonist.MentalStateDef == MentalStateDefOf.PanicFlee)
                DrawIcon(drawPos, num1++, Icons.Panic, Color.yellow);

            // Binging on alcohol
            if (Settings.ShowDrunk)
            {
                if (colonist.MentalStateDef == MentalStateDefOf.BingingAlcohol)
                    DrawIcon(drawPos, num1++, Icons.Drunk, Color.red);
                else if (pawnStats.PawnDrunkness > 0.05)
                    DrawIcon(drawPos, num1++, Icons.Drunk, pawnStats.PawnDrunkness, new Color(1f, 1f, 1f, 0.2f), Color.white, Color.red);
            }

            // Effectiveness
            if (Settings.ShowEffectiveness && pawnStats.PawnTotalEfficiency < (double)Settings.LimitEfficiencyLess)
                DrawIcon(drawPos, num1++, Icons.Effectiveness, pawnStats.PawnTotalEfficiency / Settings.LimitEfficiencyLess);

            // Disease
            if (Settings.ShowDisease)
            {
                if (HasMood(colonist, ThoughtDef.Named("Sick")))
                    DrawIcon(drawPos, num1++, Icons.Sick, Color.white);

                if (colonist.health.ShouldBeTendedNow && !colonist.health.ShouldDoSurgeryNow)
                    DrawIcon(drawPos, num1++, Icons.MedicalAttention, new Color(1f, 0.5f, 0f));
                else
                if (colonist.health.ShouldBeTendedNow && colonist.health.ShouldDoSurgeryNow)
                {
                    DrawIcon(drawPos, num1++, Icons.MedicalAttention, new Color(1f, 0.5f, 0f));
                    DrawIcon(drawPos, num1++, Icons.MedicalAttention, Color.blue);
                }
                else
                if (colonist.health.ShouldDoSurgeryNow)
                    DrawIcon(drawPos, num1++, Icons.MedicalAttention, Color.blue);

                if ((pawnStats.DiseaseDisappearance < Settings.LimitDiseaseLess) && (colonist.health.summaryHealth.SummaryHealthPercent < 1f))
                {
                    if ((pawnStats.DiseaseDisappearance / Settings.LimitDiseaseLess) < colonist.health.summaryHealth.SummaryHealthPercent)
                        DrawIcon(drawPos, num1++, Icons.Disease, pawnStats.DiseaseDisappearance / Settings.LimitDiseaseLess, Color.red, Color.white);
                    else
                        DrawIcon(drawPos, num1++, Icons.Disease, colonist.health.summaryHealth.SummaryHealthPercent, Color.red, Color.white);
                }

                else if (pawnStats.DiseaseDisappearance < Settings.LimitDiseaseLess)
                    DrawIcon(drawPos, num1++, Icons.Disease, pawnStats.DiseaseDisappearance / Settings.LimitDiseaseLess, Color.red, Color.white);

                else if (colonist.health.summaryHealth.SummaryHealthPercent < 1f)
                    DrawIcon(drawPos, num1++, Icons.Disease, colonist.health.summaryHealth.SummaryHealthPercent, Color.red, Color.white);
            }

            // Bloodloss
            if (Settings.ShowBloodloss && pawnStats.BleedRate > 0.0f)
                DrawIcon(drawPos, num1++, Icons.Bloodloss, pawnStats.BleedRate, Color.red, Color.white);


            // Apparel
            if (Settings.ShowApparelHealth && pawnStats.ApparelHealth < (double)Settings.LimitApparelHealthLess)
            {
                var bodyPos = drawPos;
                var num2 = num1;
                var num6 = pawnStats.ApparelHealth / (double)Settings.LimitApparelHealthLess;
                DrawIcon(bodyPos, num2, Icons.ApparelHealth, (float)num6);
            }

            // Target Point 
            if (!Settings.ShowTargetPoint || !(pawnStats.TargetPos != Vector3.zero))
            DrawIcon(pawnStats.TargetPos, Vector3.zero, Icons.Target, TargetColor);

            // Traiits and bad thoughts

            // Room Status
            if (Settings.ShowRoomStatus && HasMood(colonist, ThoughtDef.Named("Crowded")))
            {
                DrawIcon(drawPos, num1++, Icons.Crowded, Color.white);
            }



            if (Settings.ShowProsthophile && HasMood(colonist, ThoughtDef.Named("ProsthophileNoProsthetic")))
            {
                DrawIcon(drawPos, num1++, Icons.Prosthophile, Color.white);
            }

            if (Settings.ShowProsthophobe && HasMood(colonist, ThoughtDef.Named("ProsthophobeUnhappy")))
            {
                DrawIcon(drawPos, num1++, Icons.Prosthophobe, Color.white);
            }

            if (Settings.ShowNightOwl && HasMood(colonist, ThoughtDef.Named("NightOwlDuringTheDay")))
            {
                DrawIcon(drawPos, num1++, Icons.NightOwl, Color.white);
            }

            if (Settings.ShowGreedy && HasMood(colonist, ThoughtDef.Named("Greedy")))
            {
                DrawIcon(drawPos, num1++, Icons.Greedy, Color.white);
            }

            if (Settings.ShowJealous && HasMood(colonist, ThoughtDef.Named("Jealous")))
            {
                DrawIcon(drawPos, num1++, Icons.Jealous, Color.white);
            }

            if (Settings.ShowLovers && HasMood(colonist, ThoughtDef.Named("WantToSleepWithSpouseOrLover")))
            {
                DrawIcon(drawPos, num1++, Icons.Love, Color.red);
            }

            if (Settings.ShowNaked && HasMood(colonist, ThoughtDef.Named("Naked")))
            {
                DrawIcon(drawPos, num1++, Icons.Naked, Color.white);
            }

            if (Settings.ShowLeftUnburied && HasMood(colonist, ThoughtDef.Named("ColonistLeftUnburied")))
            {
                DrawIcon(drawPos, num1++, Icons.LeftUnburied, Color.white);
            }

            if (Settings.ShowDeadColonists)
            {

                Color color25To21 = Color.red;

                Color color20To16 = new Color(1f, 0.5f, 0f);

                Color color15To11 = Color.yellow;

                Color color10 = new Color(1f, 1f, 0.5f);

                Color color9AndLess = Color.white;

                Color colorMoodBoost = Color.green;

                // Close Family & friends / -25



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

                // -20

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
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color10);
                }

                // Not-so-close family / -15

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

                // -10

                if (HasMood(colonist, ThoughtDef.Named("MyFatherDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color10);
                }
          
                if (HasMood(colonist, ThoughtDef.Named("MyMotherDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color10);
                }

                if (HasMood(colonist, ThoughtDef.Named("MyNieceDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color10);
                }

                if (HasMood(colonist, ThoughtDef.Named("MyNephewDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color10);
                }

                if (HasMood(colonist, ThoughtDef.Named("MyAuntDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color10);
                }

                if (HasMood(colonist, ThoughtDef.Named("MyUncleDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color10);
                }

                //


                if (HasMood(colonist, ThoughtDef.Named("BondedAnimalDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color15To11);
                }
          
                // not family, more whiter icon
                if (HasMood(colonist, ThoughtDef.Named("KilledColonist")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color9AndLess);
                }
          
                if (HasMood(colonist, ThoughtDef.Named("KilledColonyAnimal")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color9AndLess);
                }
          
                //Everyone else / < -10
                if (HasMood(colonist, ThoughtDef.Named("MyGrandparentDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color9AndLess);
                }
                if (HasMood(colonist, ThoughtDef.Named("MyHalfSiblingDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color9AndLess);
                }
          
                if (HasMood(colonist, ThoughtDef.Named("MyCousinDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color9AndLess);
                }
                if (HasMood(colonist, ThoughtDef.Named("MyKinDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color9AndLess);
                }
                if (HasMood(colonist, ThoughtDef.Named("MyGrandparentDied")))
                {
                    DrawIcon(drawPos, num1++, Icons.DeadColonist, color9AndLess);
                }

                //non family
              //if (HasMood(colonist, ThoughtDef.Named("WitnessedDeathAlly")))
              //{
              //    DrawIcon(drawPos, num1++, Icons.DeadColonist, color_9andLess);
              //}
              //if (HasMood(colonist, ThoughtDef.Named("WitnessedDeathStranger")))
              //{
              //    DrawIcon(drawPos, num1++, Icons.DeadColonist, color_9andLess);
              //}
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

        public virtual void OnGui()
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
