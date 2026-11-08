using System;
using BattleTech;
using CustomActivatableEquipment.DamageHelpers;
using HarmonyLib;

namespace CustomActivatableEquipment.AI
{
    [HarmonyPatch(typeof(AbstractActor))]
    [HarmonyPatch("OnNewRound")]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPatch(new Type[] { typeof(int) })]
    public static class AbstractActor_OnNewRound
    {
        public static void Prefix(AbstractActor __instance)
        {
            try
            {
                Log.Debug?.Write("AbstractActor.OnNewRound(" + __instance.DisplayName + ":" + __instance.GUID + ")\n");
                if (__instance.IsDead)
                {
                    return;
                }

                AIController.Process(__instance, false);
                __instance.CommitCAEDamageData();
            }
            catch (Exception e)
            {
                Log.Error.TWL(0, e.ToString(), true);
            }
        }
    }

    [HarmonyPatch(typeof(AbstractActor))]
    [HarmonyPatch("OnActivationEnd")]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPatch(new Type[] { typeof(string), typeof(int) })]
    public static class AbstractActor_OnActivationEnd
    {
        public static void Prefix(AbstractActor __instance)
        {
            try
            {
                Log.Debug?.Write("AbstractActor.OnActivationEnd(" + __instance.DisplayName + ":" + __instance.GUID + ")\n");
                AIController.Process(__instance, true);
                DamageHelper.completedTurnFor(__instance);
            }
            catch (Exception e)
            {
                Log.Error.TWL(0, e.ToString(), true);
            }
        }
    }
}
