﻿using BattleTech;
using BattleTech.UI;
using Harmony;
using LowVisibility.Helper;
using System.Linq;
using static LowVisibility.Helper.ActorHelper;

namespace LowVisibility.Patch {

    [HarmonyPatch(typeof(AbstractActor), "OnActivationBegin")]
    public static class AbstractActor_OnActivationBegin {        

        public static void Prefix(AbstractActor __instance, int stackItemID) {
            if (stackItemID == -1 || __instance == null || __instance.HasBegunActivation ) {
                // For some bloody reason DoneWithActor() invokes OnActivationBegin, EVEN THOUGH IT DOES NOTHING. GAH!
                return;
            }

            LowVisibility.Logger.LogIfTrace($"=== AbstractActor:OnActivationBegin:pre - processing {CombatantHelper.Label(__instance)}");
            bool isPlayer = __instance.team == __instance.Combat.LocalPlayerTeam;
            if (isPlayer) {
                State.LastPlayerActivatedActorGUID = __instance.GUID; 
            }
        }
    }
    
   [HarmonyPatch(typeof(CombatSelectionHandler), "TrySelectActor")]
    public static class CombatSelectionHandler_TrySelectActor {
        public static void Postfix(CombatSelectionHandler __instance, bool __result, AbstractActor actor, bool manualSelection) {
            LowVisibility.Logger.LogIfDebug($"=== CombatSelectionHandler:TrySelectActor:post - entered for {CombatantHelper.Label(actor)}.");
            if (__instance != null && actor != null && __result == true) {

            }
        }
    }

    [HarmonyPatch(typeof(AbstractActor), "UpdateLOSPositions")]
    public static class AbstractActor_UpdateLOSPositions {
        public static void Prefix(AbstractActor __instance) {
            if (State.TurnDirectorStarted) {
                LowVisibility.Logger.LogIfDebug($"AbstractActor_UpdateLOSPositions:pre - entered for {CombatantHelper.Label(__instance)}.");
                JammingHelper.ResolveJammingState(__instance);

                bool isPlayer = __instance.team == __instance.Combat.LocalPlayerTeam;
                AbstractActor updateActor = isPlayer ? 
                    __instance : 
                    __instance.Combat.AllActors.Where(aa => aa.TeamId == __instance.Combat.LocalPlayerTeamGuid).First();
                VisibilityHelper.UpdateDetectionForAllActors(__instance.Combat);
                VisibilityHelper.UpdateVisibilityForAllTeams(__instance.Combat);
            }
        }
    }

    // Update the visibility checks
    [HarmonyPatch(typeof(Mech), "OnMovePhaseComplete")]
    public static class Mech_OnMovePhaseComplete {
        public static void Postfix(Mech __instance) {
            LowVisibility.Logger.LogIfDebug($"=== Mech:OnMovePhaseComplete:post - entered for {CombatantHelper.Label(__instance)}.");

            bool isPlayer = __instance.team == __instance.Combat.LocalPlayerTeam;
            if (isPlayer && State.IsJammed(__instance)) {
                // Send a floatie indicating the jamming
                MessageCenter mc = __instance.Combat.MessageCenter;
                mc.PublishMessage(new FloatieMessage(__instance.GUID, __instance.GUID, "JAMMED BY ECM", FloatieMessage.MessageNature.Debuff));
            }
        }
    }
}
