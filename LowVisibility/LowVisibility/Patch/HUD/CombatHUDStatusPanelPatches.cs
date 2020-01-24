﻿using BattleTech;
using BattleTech.UI;
using Harmony;
using HBS;
using Localize;
using LowVisibility.Helper;
using LowVisibility.Object;
using SVGImporter;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using us.frostraptor.modUtils;

namespace LowVisibility.Patch {

    [HarmonyPatch()]
    public static class CombatHUDStatusPanel_RefreshDisplayedCombatant {

        // Private method can't be patched by annotations, so use MethodInfo
        public static MethodInfo TargetMethod() {
            return AccessTools.Method(typeof(CombatHUDStatusPanel), "RefreshDisplayedCombatant", new Type[] { });
        }

        public static void Postfix(CombatHUDStatusPanel __instance, List<CombatHUDStatusIndicator> ___Buffs, List<CombatHUDStatusIndicator> ___Debuffs) {
            Mod.Log.Trace("CHUDSP:RDC - entered.");
            if (__instance != null && __instance.DisplayedCombatant != null) {
                AbstractActor target = __instance.DisplayedCombatant as AbstractActor;
                // We can receive a building here, so 
                if (target != null) {
                    if (target.Combat.HostilityMatrix.IsLocalPlayerEnemy(target.team)) {

                        SensorScanType scanType = SensorLockHelper.CalculateSharedLock(target, ModState.LastPlayerActorActivated);

                        if (scanType < SensorScanType.Vector) {
                            //// Hide the evasive indicator, hide the buffs and debuffs
                            //Traverse hideEvasionIndicatorMethod = Traverse.Create(__instance).Method("HideEvasiveIndicator", new object[] { });
                            //hideEvasionIndicatorMethod.GetValue();
                            ___Buffs.ForEach(si => si.gameObject.SetActive(false));
                            ___Debuffs.ForEach(si => si.gameObject.SetActive(false));
                        } else if (scanType < SensorScanType.StructureAnalysis) {
                            // Hide the buffs and debuffs
                            ___Buffs.ForEach(si => si.gameObject.SetActive(false));
                            ___Debuffs.ForEach(si => si.gameObject.SetActive(false));
                        } else if (scanType >= SensorScanType.StructureAnalysis) {
                            // Do nothing; normal state
                        }
                    }

                    // Calculate stealth pips
                    Traverse stealthDisplayT = Traverse.Create(__instance).Field("stealthDisplay");
                    CombatHUDStealthBarPips stealthDisplay = stealthDisplayT.GetValue<CombatHUDStealthBarPips>();
                    VfxHelper.CalculateMimeticPips(stealthDisplay, target);
                }
            }
        }
    }


    [HarmonyPatch(typeof(CombatHUDStatusPanel), "HideStealthIndicator")]
    public static class CombatHUDStatusPanel_HideStealthIndicator {
        public static void Postfix(CombatHUDStatusPanel __instance) {
            Mod.Log.Trace("CHUDSP:HSI - entered.");
        }
    }

    [HarmonyPatch(typeof(CombatHUDStatusPanel), "ShowStealthIndicators")]
    [HarmonyPatch(new Type[] {  typeof(AbstractActor), typeof(Vector3) })]
    public static class CombatHUDStatusPanel_ShowStealthIndicators_Vector3 {
        public static void Postfix(CombatHUDStatusPanel __instance, AbstractActor target, Vector3 previewPos, CombatHUDStealthBarPips ___stealthDisplay) {
            if (___stealthDisplay == null) { return; }
            Mod.Log.Trace("CHUDSP:SSI:Vector3 - entered.");

            VfxHelper.CalculateMimeticPips(___stealthDisplay, target, previewPos);
        }
    }

    [HarmonyPatch(typeof(CombatHUDStatusPanel), "ShowStealthIndicators")]
    [HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(float) })]
    public static class CombatHUDStatusPanel_ShowStealthIndicators_float {
        public static void Postfix(CombatHUDStatusPanel __instance, AbstractActor target, float previewStealth, CombatHUDStealthBarPips ___stealthDisplay) {
            if (___stealthDisplay == null) { return; }
            Mod.Log.Trace("CHUDSP:SSI:float - entered.");

            VfxHelper.CalculateMimeticPips(___stealthDisplay, target);
        }
    }


    [HarmonyPatch()]
    public static class CombatHUDStatusPanel_ShowActorStatuses {

        // Private method can't be patched by annotations, so use MethodInfo
        public static MethodInfo TargetMethod() {
            return AccessTools.Method(typeof(CombatHUDStatusPanel), "ShowActorStatuses", new Type[] { typeof(AbstractActor) });
        }

        public static void Postfix(CombatHUDStatusPanel __instance) {
            Mod.Log.Trace("CHUDSP:SAS - entered.");

            if (__instance.DisplayedCombatant != null) {
                Type[] iconMethodParams = new Type[] { typeof(SVGAsset), typeof(Text), typeof(Text), typeof(Vector3), typeof(bool) };
                Traverse showDebuffIconMethod = Traverse.Create(__instance).Method("ShowDebuff", iconMethodParams);
                Traverse showBuffIconMethod = Traverse.Create(__instance).Method("ShowBuff", iconMethodParams);

                AbstractActor actor = __instance.DisplayedCombatant as AbstractActor;
                EWState actorState = new EWState(actor);

                Traverse svgAssetT = Traverse.Create(__instance.DisplayedCombatant.Combat.DataManager).Property("SVGCache");
                object svgCache = svgAssetT.GetValue();
                Traverse svgCacheT = Traverse.Create(svgCache).Method("GetAsset", new Type[] { typeof(string) });

                bool isPlayer = actor.team == actor.Combat.LocalPlayerTeam;
                if (isPlayer) {

                    SVGAsset icon = svgCacheT.GetValue<SVGAsset>(new object[] { ModIcons.VisionAndSensors });
                    Text title = new Text(Mod.Config.LocalizedText[ModConfig.LT_TT_TITLE_VISION_AND_SENSORS]);
                    showBuffIconMethod.GetValue(new object[] { icon, title, new Text(BuildToolTip(actor)), __instance.effectIconScale, false });

                    // Disable the sensors
                    if (actor.Combat.TurnDirector.CurrentRound == 1) {
                        SVGAsset sensorsDisabledIcon = svgCacheT.GetValue<SVGAsset>(new object[] { ModIcons.SensorsDisabled });
                        Text sensorsDisabledTitle = new Text(Mod.Config.LocalizedText[ModConfig.LT_TT_TITLE_SENSORS_DISABLED]);
                        Text sensorsDisabledText = new Text(Mod.Config.LocalizedText[ModConfig.LT_TT_TEXT_SENSORS_DISABLED]);
                        showDebuffIconMethod.GetValue(new object[] { 
                            sensorsDisabledIcon, sensorsDisabledTitle, sensorsDisabledText, __instance.effectIconScale, false });
                    }
                }

                if (actorState.GetRawECMShield() != 0|| actorState.GetRawECMJammed() != 0 || actorState.ProbeCarrierMod() != 0 || actorState.PingedByProbeMod() != 0 ||
                    actorState.GetRawStealth() != null || actorState.GetRawMimetic() != null || actorState.GetRawNarcEffect() != null || actorState.GetRawTagEffect() != null) {
                    // Build out the detailed string
                    StringBuilder sb = new StringBuilder();

                    if (actorState.GetRawECMShield() != 0) {
                        // A positive is good, a negative is bad
                        string color = actorState.GetRawECMShield() >= 0 ? "00FF00" : "FF0000";
                        string localText = new Text(Mod.Config.LocalizedText[ModConfig.LT_TT_TEXT_EW_ECM_SHIELD], 
                            new object[] { color, actorState.GetRawECMShield() }
                            ).ToString();
                        sb.Append(localText);
                    }

                    if (actorState.GetRawECMJammed() != 0) {
                        // A positive is good, a negative is bad
                        string color = actorState.GetRawECMJammed() >= 0 ? "00FF00" : "FF0000";
                        string localText = new Text(Mod.Config.LocalizedText[ModConfig.LT_TT_TEXT_EW_ECM_JAMMING],
                            new object[] { color, actorState.GetRawECMJammed() }
                            ).ToString();
                        sb.Append(localText);
                    }

                    if (actorState.ProbeCarrierMod() != 0) {
                        // A positive is good, a negative is bad
                        string color = actorState.ProbeCarrierMod() >= 0 ? "00FF00" : "FF0000";
                        string localText = new Text(Mod.Config.LocalizedText[ModConfig.LT_TT_TEXT_EW_PROBE_CARRIER],
                            new object[] { color, -1 * actorState.ProbeCarrierMod() }
                            ).ToString();
                        sb.Append(localText);
                    }

                    // Armor
                    if (actorState.GetRawStealth() != null) {
                        string color = "00FF00";
                        string localText = new Text(Mod.Config.LocalizedText[ModConfig.LT_TT_TEXT_EW_STEALTH],
                            new object[] { color, actorState.GetRawStealth().MediumRangeAttackMod, actorState.GetRawStealth().LongRangeAttackMod, actorState.GetRawStealth().ExtremeRangeAttackMod, }
                            ).ToString();
                        sb.Append(localText);
                    }

                    if (actorState.GetRawMimetic() != null) {
                        // A positive is good (harder to hit), should be no negative?
                        string color = "00FF00";
                        string localText = new Text(Mod.Config.LocalizedText[ModConfig.LT_TT_TEXT_EW_MIMETIC],
                            new object[] { color, actorState.CurrentMimeticPips() }
                            ).ToString();
                        sb.Append(localText);
                    }

                    // Transient effects
                    if (actorState.PingedByProbeMod() != 0) {
                        // A positive is good, a negative is bad
                        string color = -1 * actorState.PingedByProbeMod() >= 0 ? "00FF00" : "FF0000";
                        string localText = new Text(Mod.Config.LocalizedText[ModConfig.LT_TT_TEXT_EW_PROBE_EFFECT],
                            new object[] { color, -1 * actorState.PingedByProbeMod() }
                            ).ToString();
                        sb.Append(localText);
                    }

                    if (actorState.GetRawNarcEffect() != null) {
                        // A positive is good, a negative is bad
                        string color = -1 * actorState.GetRawNarcEffect().AttackMod >= 0 ? "00FF00" : "FF0000";
                        string localText = new Text(Mod.Config.LocalizedText[ModConfig.LT_TT_TEXT_EW_NARC_EFFECT],
                            new object[] { color, -1 * actorState.GetRawNarcEffect().AttackMod }
                            ).ToString();
                        sb.Append(localText);
                    }

                    if (actorState.GetRawTagEffect() != null) {
                        // A positive is good, a negative is bad
                        string color = -1 * actorState.GetRawTagEffect().AttackMod >= 0 ? "00FF00" : "FF0000";
                        string localText = new Text(Mod.Config.LocalizedText[ModConfig.LT_TT_TEXT_EW_TAG_EFFECT],
                            new object[] { color, -1 * actorState.GetRawTagEffect().AttackMod }
                            ).ToString();
                        sb.Append(localText);
                    }

                    SVGAsset icon = svgCacheT.GetValue<SVGAsset>(new object[] { ModIcons.ElectronicWarfare });
                    Text title = new Text(Mod.Config.LocalizedText[ModConfig.LT_TT_TITLE_EW]);
                    showBuffIconMethod.GetValue(new object[] { icon, title, new Text(sb.ToString()), __instance.effectIconScale, false });
                }

            }
        }

        private static string BuildToolTip(AbstractActor actor) {
            //Mod.Log.Debug($"EW State for actor:{CombatantUtils.Label(actor)} = {ewState}");

            List<string> details = new List<string>();

            // Visuals check
            float visualLockRange = VisualLockHelper.GetVisualLockRange(actor);
            float visualScanRange = VisualLockHelper.GetVisualScanRange(actor);
            details.Add(
                new Text(Mod.Config.LocalizedText[ModConfig.LT_PANEL_VISUALS], 
                    new object[] { visualLockRange, visualScanRange, ModState.GetMapConfig().UILabel() })
                    .ToString()
                );

            // Sensors check
            EWState ewState = new EWState(actor);
            SensorScanType checkLevel;
            if (ewState.GetCurrentEWCheck() > (int)SensorScanType.DentalRecords) { checkLevel = SensorScanType.DentalRecords; } 
            else if (ewState.GetCurrentEWCheck() < (int)SensorScanType.NoInfo) { checkLevel = SensorScanType.NoInfo; } 
            else { checkLevel = (SensorScanType)ewState.GetCurrentEWCheck(); }
            float sensorsRange = SensorLockHelper.GetSensorsRange(actor);
            string sensorColor = ewState.GetCurrentEWCheck() >= 0 ? "00FF00" : "FF0000";
            details.Add(
                new Text(Mod.Config.LocalizedText[ModConfig.LT_PANEL_SENSORS], 
                    new object[] { sensorColor, sensorsRange, sensorColor, ewState.GetSensorsRangeMulti(), checkLevel.Label() })
                    .ToString()
                );

            // Details
            //{ LT_PANEL_DETAILS, "  Total:{0}<size=90%> Roll:<color=#{1}>{2}</color> Tactics:<color=#00FF00>{3+0;-#}</color> AdvSen:<color=#{4}>{5+0;-#}</color>\n" },
            string checkColor = ewState.GetRawCheck() >= 0 ? "00FF00" : "FF0000";
            string advSenColor = ewState.AdvancedSensorsMod() >= 0 ? "00FF00" : "FF0000";
            details.Add(
                new Text(Mod.Config.LocalizedText[ModConfig.LT_PANEL_DETAILS],
                    new object[] { ewState.GetCurrentEWCheck(), checkColor, ewState.GetRawCheck(), ewState.GetRawTactics(), advSenColor, ewState.AdvancedSensorsMod() })
                    .ToString()
                );

            //  Heat Vision
            if (ewState.GetRawHeatVision() != null) {
                // { LT_PANEL_HEAT, "<b>Thermals</b><size=90%> Mod:<color=#{0}>{1:+0;-#}</color> / {2} heat Range:{3}m\n" },
                HeatVision heatVis = ewState.GetRawHeatVision();
                // Positive is bad, negative is good
                string modColor = heatVis.AttackMod >= 0 ? "FF0000" : "00FF00";
                details.Add(
                    new Text(Mod.Config.LocalizedText[ModConfig.LT_PANEL_HEAT],
                        new object[] { modColor, heatVis.AttackMod, heatVis.HeatDivisor, heatVis.MaximumRange })
                        .ToString()
                    );
            }

            //  Zoom Vision
            if (ewState.GetRawZoomVision() != null) {
                // { LT_PANEL_ZOOM, "<b>Zoom</b><size=90%> Mod:<color=#{0}>{1:+0;-#}</color? Cap:<color=#{2}>{3:+0;-#}</color> Range:{4}m\n" },
                ZoomVision zoomVis = ewState.GetRawZoomVision();
                // Positive is bad, negative is good
                string modColor = zoomVis.AttackMod >= 0 ? "FF0000" : "00FF00";
                string capColor = zoomVis.AttackCap >= 0 ? "FF0000" : "00FF00";
                details.Add(
                    new Text(Mod.Config.LocalizedText[ModConfig.LT_PANEL_ZOOM],
                        new object[] { modColor, zoomVis.AttackMod, capColor, zoomVis.AttackCap, zoomVis.MaximumRange })
                        .ToString()
                    );
            }

            string tooltipText = String.Join("", details.ToArray());
            return tooltipText;
        }
    }

}
