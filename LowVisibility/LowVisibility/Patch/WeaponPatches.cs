﻿using BattleTech;
using BattleTech.UI;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using static LowVisibility.Helper.ActorHelper;

namespace LowVisibility.Patch {

    [HarmonyPatch(typeof(CombatHUDTargetingComputer), "RefreshActorInfo")]
    public static class CombatHUDTargetingComputer_RefreshActorInfo {

        // TODO: Need vehicle, turret, building displays
        public static void Postfix(CombatHUDTargetingComputer __instance, List<TextMeshProUGUI> ___weaponNames) {
            //KnowYourFoe.Logger.Log("CombatHUDTargetingComputer:RefreshActorInfo:post - entered.");
            if (__instance.ActivelyShownCombatant != null) {
                // TODO: Make allies share info
                AbstractActor target = __instance.ActivelyShownCombatant as AbstractActor;
                bool isPlayer = target.Combat.HostilityMatrix.IsLocalPlayerEnemy(target.Combat.LocalPlayerTeam.GUID);

                //bool isPlayer = actor.team == actor.Combat.LocalPlayerTeam;
                //bool isFriendly = __instance.Combat.HostilityMatrix.IsFriendly(actor.team, actor.Combat.LocalPlayerTeam);

                if (!isPlayer) {
                    //KnowYourFoe.Logger.Log($"CombatHUDTargetingComputer:RefreshActorInfo:post - actor:{target.DisplayName}_{target.GetPilot().Name} is enemy, neutral or allied.");
                    IDState idState = CalculateTargetIDLevel(target);
                    if (idState < IDState.VisualID) {
                        //KnowYourFoe.Logger.Log($"Detection state:{detectState} for actor:{target.DisplayName}_{target.GetPilot().Name} requires weapons to be hidden.");
                        // Update the summary display
                        Transform weaponListT = __instance.WeaponList?.transform?.parent?.Find("tgtWeaponsLabel");
                        GameObject weaponsLabel = weaponListT.gameObject;
                        TextMeshProUGUI labelText = weaponsLabel.GetComponent<TextMeshProUGUI>();
                        //KnowYourFoe.Logger.Log($"CombatHUDTargetingComputer:RefreshActorInfo:post - found labelText with text:{labelText.text}");
                        labelText.SetText("???");

                        // Update the weapons
                        for (int i = 0; i < ___weaponNames.Count; i++) {
                            //KnowYourFoe.Logger.Log($"CombatHUDTargetingComputer:RefreshActorInfo:post - iterating weapon:{___weaponNames[i].text}");
                            // Update ranged weapons
                            if (i < target.Weapons.Count) {
                                Weapon targetWeapon = target.Weapons[i];
                                //KnowYourFoe.Logger.Log($"CombatHUDTargetingComputer:RefreshActorInfo:post - hiding weapon:{targetWeapon.Name}");
                                ___weaponNames[i].SetText("???");
                            } else if (!___weaponNames[i].text.Equals("XXXXXXXXXXXXXX")) {
                                // Update melee and dfa without using locale specific strings
                                ___weaponNames[i].SetText("???");
                            }
                        }
                    } else {
                        //KnowYourFoe.Logger.Log($"Detection state:{detectState} for actor:{target.DisplayName}_{target.GetPilot().Name} allows weapons to be seen.");
                    }
                } else {
                    LowVisibility.Logger.Log($"CombatHUDTargetingComputer:RefreshActorInfo:post - actor:{target.DisplayName}_{target.GetPilot().Name} is player, showing panel.");
                }
            }
        }
    }
}