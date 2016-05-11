using System;
using System.Collections.Generic;
using System.IO;
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
        private static double fDelta;

        private static bool inGame;

        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private static Dictionary<Pawn, PawnStats> _statsDict = new Dictionary<Pawn, PawnStats>();

        private static bool _iconsEnabled = true;

        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private static Dialog_Settings modSettingsDialog = new Dialog_Settings();

        public static ModSettings settings = new ModSettings();

        private static readonly Color targetColor = new Color(1f, 1f, 1f, 0.6f);

        private static float worldScale = 1f;

        public static string[] iconSets = { "default" };

        public static Materials materials = new Materials();

        private static PawnCapacityDef[] pawnCapacities;

        private static Vector3[] iconPosVectors;


        public PSI()
        {
            Reinit();
        }

        public static void Reinit(bool reloadSettings = true, bool reloadIconSet = true, bool recalcIconPos = true)
        {
            PSI.pawnCapacities = new PawnCapacityDef[]
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
                PSI.settings = PSI.loadSettings("psi-settings.cfg");
            }
            if (reloadIconSet)
            {
                PSI.materials = new Materials(PSI.settings.iconSet);
                ModSettings modSettings = XmlLoader.ItemFromXmlFile<ModSettings>(GenFilePaths.CoreModsFolderPath + "/Pawn State Icons/Textures/UI/Overlays/PawnStateIcons/" + PSI.settings.iconSet + "/iconset.cfg", true);
                PSI.settings.iconSizeMult = modSettings.iconSizeMult;
                PSI.materials.ReloadTextures(true);
            }
            if (recalcIconPos)
            {
                PSI.RecalcIconPositions();
            }
        }

        public static ModSettings loadSettings(string path = "psi-settings.cfg")
        {
            ModSettings result = XmlLoader.ItemFromXmlFile<ModSettings>(path, true);
            string path2 = GenFilePaths.CoreModsFolderPath + "/Pawn State Icons/Textures/UI/Overlays/PawnStateIcons/";
            if (Directory.Exists(path2))
            {
                PSI.iconSets = Directory.GetDirectories(path2);
                for (int i = 0; i < PSI.iconSets.Length; i++)
                {
                    PSI.iconSets[i] = new DirectoryInfo(PSI.iconSets[i]).Name;
                }
            }
            return result;
        }

        public static void SaveSettings(string path = "psi-settings.cfg")
        {
            XmlSaver.SaveDataObject(PSI.settings, path);
        }

        #region Draw icons

        private static void DrawIcon(Vector3 bodyPos, Vector3 posOffset, Icons icon, Color color)
        {

            var material = materials[icon];
            if (material == null)
                return;
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                material.color = color;
                var guiColor = GUI.color;
                GUI.color = color;
                Vector2 vector2;
                if (settings.iconsScreenScale)
                {
                    vector2 = bodyPos.ToScreenPosition();
                    vector2.x += posOffset.x * 45f;
                    vector2.y -= posOffset.z * 45f;
                }
                else
                    vector2 = (bodyPos + posOffset).ToScreenPosition();
                var num1 = worldScale;
                if (settings.iconsScreenScale)
                    num1 = 45f;
                var num2 = num1 * (settings.iconSizeMult * 0.5f);
                var position = new Rect(vector2.x, vector2.y, num2 * settings.iconSize, num2 * settings.iconSize);
                position.x -= position.width * 0.5f;
                position.y -= position.height * 0.5f;
                GUI.DrawTexture(position, material.mainTexture, ScaleMode.ScaleToFit, true);
                GUI.color = guiColor;
            });

        }

        private static void DrawIcon(Vector3 bodyPos, int num, Icons icon, Color color)
        {
            DrawIcon(bodyPos, iconPosVectors[num], icon, color);
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
            iconPosVectors = new Vector3[18];
            for (var index = 0; index < iconPosVectors.Length; ++index)
            {
                var num1 = index / settings.iconsInColumn;
                var num2 = index % settings.iconsInColumn;
                if (settings.iconsHorizontal)
                {
                    var num3 = num1;
                    num1 = num2;
                    num2 = num3;
                }
                iconPosVectors[index] = new Vector3((float)(-0.600000023841858 * settings.iconDistanceX - 0.550000011920929 * settings.iconSize * settings.iconOffsetX * num1), 3f, (float)(-0.600000023841858 * settings.iconDistanceY + 0.550000011920929 * settings.iconSize * settings.iconOffsetY * num2));
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

            foreach (var activity in pawnCapacities)
            {
                if (activity != PawnCapacityDefOf.Consciousness)
                    efficiency = Math.Min(efficiency, colonist.health.capacities.GetEfficiency(activity));
            }

            if (efficiency < 0.0)
                efficiency = 0.0f;

            pawnStats.pawn_TotalEfficiency = efficiency;

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
            pawnStats.pawn_TooCold = (float)((colonist.ComfortableTemperatureRange().min - (double)settings.limit_TempComfortOffset - temperatureForCell) / 10.0);
            pawnStats.pawn_TooHot = (float)((temperatureForCell - (double)colonist.ComfortableTemperatureRange().max - settings.limit_TempComfortOffset) / 10.0);
            pawnStats.pawn_TooCold = Mathf.Clamp(pawnStats.pawn_TooCold, 0.0f, 2f);
            pawnStats.pawn_TooHot = Mathf.Clamp(pawnStats.pawn_TooHot, 0.0f, 2f);


            // Health Calc
            pawnStats.DiseaseDisappearance = 1f;
            pawnStats.pawn_Drunkness = DrugUtility.DrunknessPercent(colonist);


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

            pawnStats.pawn_ApparelHealth = num1;

            pawnStats.pawn_BleedRate = Mathf.Clamp01(colonist.health.hediffSet.BleedingRate * settings.limit_BleedMult);

            _statsDict[colonist] = pawnStats;

        }

        public static Boolean HasMood(Pawn pawn, ThoughtDef Tdef)
        {
            if (pawn.needs.mood.thoughts.DistinctThoughtDefs.Contains(Tdef))
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
            fDelta += Time.fixedDeltaTime;

            if (fDelta < 0.1)
                return;
            fDelta = 0.0;
            inGame = GameObject.Find("CameraMap");

            if (!inGame || !_iconsEnabled)
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
            var flag2 = Find.WindowStack.IsOpen(typeof(Dialog_Settings));

            if (flag1 && flag2)
            {
                modSettingsDialog.OptionsDialog = dialogOptions;
                RecalcIconPositions();
            }
            else if (flag1 && !flag2)
            {
                if (!modSettingsDialog.CloseButtonClicked)
                {
                    Find.UIRoot.windows.Add(modSettingsDialog);
                    modSettingsDialog.Page = "main";
                }
                else
                    dialogOptions.Close();
            }
            else if (!flag1 && flag2)
            {
                modSettingsDialog.Close(false);
            }
            else
            {
                if (flag1 || flag2)
                    return;
                modSettingsDialog.CloseButtonClicked = false;
            }
        }

        private static void DrawAnimalIcons(Pawn animal)
        {
            if (animal.Dead || animal.holder != null)
                return;
            var drawPos = animal.DrawPos;

            if (!settings.show_Aggressive || animal.MentalStateDef != MentalStateDefOf.Berserk && animal.MentalStateDef != MentalStateDefOf.Manhunter)
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
                if (settings.show_Pacific)
                    DrawIcon(drawPos, num1++, Icons.Pacific, Color.white);
            }
            else if (settings.show_Unarmed && colonist.equipment.Primary == null && !colonist.IsPrisonerOfColony)
                DrawIcon(drawPos, num1++, Icons.Unarmed, Color.white);

            // Idle
            if (settings.show_Idle && colonist.mindState.IsIdle)
                DrawIcon(drawPos, num1++, Icons.Idle, Color.white);

            //Drafted
            if (settings.show_Draft && colonist.drafter.Drafted)
                DrawIcon(drawPos, num1++, Icons.Draft, Color.white);

            // Bad Mood
            if (settings.show_Sad && colonist.needs.mood.CurLevel < (double)settings.limit_MoodLess)
                DrawIcon(drawPos, num1++, Icons.Sad, colonist.needs.mood.CurLevel / settings.limit_MoodLess);

            // Hungry
            if (settings.show_Hungry && colonist.needs.food.CurLevel < (double)settings.limit_FoodLess)
                DrawIcon(drawPos, num1++, Icons.Hungry, colonist.needs.food.CurLevel / settings.limit_FoodLess);

            //Tired
            if (settings.show_Tired && colonist.needs.rest.CurLevel < (double)settings.limit_RestLess)
                DrawIcon(drawPos, num1++, Icons.Tired, colonist.needs.rest.CurLevel / settings.limit_RestLess);

            // Too Cold & too hot
            if (settings.show_Cold && pawnStats.pawn_TooCold > 0.0)
            {
                if (pawnStats.pawn_TooCold >= 0.0)
                {
                    if (pawnStats.pawn_TooCold <= 1.0)
                        DrawIcon(drawPos, num1++, Icons.Freezing, pawnStats.pawn_TooCold, new Color(1f, 1f, 1f, 0.3f), new Color(0.86f, 0.86f, 1f, 1f));
                    else if (pawnStats.pawn_TooCold <= 1.5)
                        DrawIcon(drawPos, num1++, Icons.Freezing, (float)((pawnStats.pawn_TooCold - 1.0) * 2.0), new Color(0.86f, 0.86f, 1f, 1f), new Color(1f, 0.86f, 0.86f));
                    else
                        DrawIcon(drawPos, num1++, Icons.Freezing, (float)((pawnStats.pawn_TooCold - 1.5) * 2.0), new Color(1f, 0.86f, 0.86f), Color.red);
                }
            }
            else if (settings.show_Hot && pawnStats.pawn_TooHot > 0.0 && pawnStats.pawn_TooCold >= 0.0)
            {
                if (pawnStats.pawn_TooHot <= 1.0)
                    DrawIcon(drawPos, num1++, Icons.Hot, pawnStats.pawn_TooHot, new Color(1f, 1f, 1f, 0.3f), new Color(1f, 0.7f, 0.0f, 1f));
                else
                    DrawIcon(drawPos, num1++, Icons.Hot, pawnStats.pawn_TooHot - 1f, new Color(1f, 0.7f, 0.0f, 1f), Color.red);
            }

            // Mental States
            if (settings.show_Aggressive && colonist.MentalStateDef == MentalStateDefOf.Berserk)
                DrawIcon(drawPos, num1++, Icons.Aggressive, Color.red);

            if (settings.show_Leave && colonist.MentalStateDef == MentalStateDefOf.GiveUpExit)
                DrawIcon(drawPos, num1++, Icons.Leave, Color.red);

            if (settings.show_Dazed && colonist.MentalStateDef == MentalStateDefOf.DazedWander)
                DrawIcon(drawPos, num1++, Icons.Dazed, Color.red);

            if (colonist.MentalStateDef == MentalStateDefOf.PanicFlee)
                DrawIcon(drawPos, num1++, Icons.Panic, Color.yellow);

            // Binging on alcohol
            if (settings.show_Drunk)
            {
                if (colonist.MentalStateDef == MentalStateDefOf.BingingAlcohol)
                    DrawIcon(drawPos, num1++, Icons.Drunk, Color.red);
                else if (pawnStats.pawn_Drunkness > 0.05)
                    DrawIcon(drawPos, num1++, Icons.Drunk, pawnStats.pawn_Drunkness, new Color(1f, 1f, 1f, 0.2f), Color.white, Color.red);
            }

            // Effectiveness
            if (settings.show_Effectiveness && pawnStats.pawn_TotalEfficiency < (double)settings.limit_EfficiencyLess)
                DrawIcon(drawPos, num1++, Icons.Effectiveness, pawnStats.pawn_TotalEfficiency / settings.limit_EfficiencyLess);

            // Disease
            if (settings.show_Disease)
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

                if ((pawnStats.DiseaseDisappearance < settings.limit_DiseaseLess) && (colonist.health.summaryHealth.SummaryHealthPercent < 1f))
                {
                    if ((pawnStats.DiseaseDisappearance / settings.limit_DiseaseLess) < colonist.health.summaryHealth.SummaryHealthPercent)
                        DrawIcon(drawPos, num1++, Icons.Disease, pawnStats.DiseaseDisappearance / settings.limit_DiseaseLess, Color.red, Color.white);
                    else
                        DrawIcon(drawPos, num1++, Icons.Disease, colonist.health.summaryHealth.SummaryHealthPercent, Color.red, Color.white);
                }

                else if (pawnStats.DiseaseDisappearance < settings.limit_DiseaseLess)
                    DrawIcon(drawPos, num1++, Icons.Disease, pawnStats.DiseaseDisappearance / settings.limit_DiseaseLess, Color.red, Color.white);

                else if (colonist.health.summaryHealth.SummaryHealthPercent < 1f)
                    DrawIcon(drawPos, num1++, Icons.Disease, colonist.health.summaryHealth.SummaryHealthPercent, Color.red, Color.white);
            }

            // Bloodloss
            if (settings.show_Bloodloss && pawnStats.pawn_BleedRate > 0.0f)
                DrawIcon(drawPos, num1++, Icons.Bloodloss, pawnStats.pawn_BleedRate, Color.red, Color.white);


            // Apparel
            if (settings.show_ApparelHealth && pawnStats.pawn_ApparelHealth < (double)settings.limit_ApparelHealthLess)
            {
                var bodyPos = drawPos;
                var num2 = num1;
                var num6 = pawnStats.pawn_ApparelHealth / (double)settings.limit_ApparelHealthLess;
                DrawIcon(bodyPos, num2, Icons.ApparelHealth, (float)num6);
            }

            // Target Point 
            if (!settings.show_TargetPoint || !(pawnStats.TargetPos != Vector3.zero))
                return;

            // Traiits and bad thoughts

            if (settings.show_Prosthophile && HasMood(colonist, ThoughtDef.Named("ProsthophileNoProsthetic")))
            {
                DrawIcon(drawPos, num1++, Icons.Prosthophile, Color.white);
            }

            if (settings.show_Prosthophobe && HasMood(colonist, ThoughtDef.Named("ProsthophobeUnhappy")))
            {
                DrawIcon(drawPos, num1++, Icons.Prosthophobe, Color.white);
            }

            if (settings.show_NightOwl && HasMood(colonist, ThoughtDef.Named("NightOwlDuringTheDay")))
            {
                DrawIcon(drawPos, num1++, Icons.NightOwl, Color.white);
            }

            if (settings.show_Greedy && HasMood(colonist, ThoughtDef.Named("Greedy")))
            {
                DrawIcon(drawPos, num1++, Icons.Greedy, Color.white);
            }

            if (settings.show_Jealous && HasMood(colonist, ThoughtDef.Named("Jealous")))
            {
                DrawIcon(drawPos, num1++, Icons.Jealous, Color.white);
            }

            if (settings.show_Lovers && HasMood(colonist, ThoughtDef.Named("WantToSleepWithSpouseOrLover")))
            {
                DrawIcon(drawPos, num1++, Icons.Love, Color.red);
            }

            if (settings.show_Naked && HasMood(colonist, ThoughtDef.Named("Naked")))
            {
                DrawIcon(drawPos, num1++, Icons.Naked, Color.white);
            }
            DrawIcon(pawnStats.TargetPos, Vector3.zero, Icons.Target, targetColor);
        }

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Global

        public virtual void OnGUI()
        {
            if (!inGame || Find.TickManager.Paused)
                UpdateOptionsDialog();
            if (!_iconsEnabled || !inGame)
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

        // ReSharper disable once UnusedMember.Global

        public virtual void Update()
        {
            if (!inGame)
                return;
            if (Input.GetKeyUp(KeyCode.F11))
            {
                _iconsEnabled = !_iconsEnabled;
                Messages.Message(_iconsEnabled ? "PSI.Enabled".Translate() : "PSI.Disabled".Translate(),
                    MessageSound.Standard);
            }
            worldScale = Screen.height / (2f * Camera.current.orthographicSize);
        }
    }
}
