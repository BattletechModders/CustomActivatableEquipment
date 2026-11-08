using BattleTech;
using CustomActivatableEquipment;
using HarmonyLib;
using System;
using CustomComponents;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;
using CustomActivatableEquipment.DamageHelpers;
using CustAmmoCategories;
using IRBTModUtils;
using CustomAmmoCategoriesPatches;

namespace CustomActivatableEquipment
{
    public static partial class CAEAIHelper
    {
        private const string AIRollPassStatName = "CAEAIFailRollPassed";

        private const string AISwitchedOnRoundStatName = "CAEAISwitchedOnRound";

        private const string AISwitchedOffRoundStatName = "CAEAISwitchedOffRound";

        private static void SetOrAddStat<T>(StatCollection stats, string name, T value)
        {
            if (!Core.checkExistance(stats, name))
            {
                stats.AddStatistic(name, value);
            }
            else
            {
                stats.Set(name, value);
            }
        }

        private static bool isAIUsedThisRound(this MechComponent component)
        {
            ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
            if (activatable == null || !activatable.ActivateOncePerRound)
            {
                return false;
            }

            return ActivatableComponent.getComponentActivedRound(component) == component.parent.Combat.TurnDirector.CurrentRound;
        }

        private static void setAIUsedThisRound(this MechComponent component)
        {
            ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
            if (activatable == null || !activatable.ActivateOncePerRound)
            {
                return;
            }

            SetOrAddStat(component.StatCollection, ActivatableComponent.CAEComponentActivedRound, component.parent.Combat.TurnDirector.CurrentRound);
        }

        public static bool isAIRollPassed(this MechComponent component)
        {
            if (!Core.checkExistance(component.StatCollection, AIRollPassStatName))
            {
                component.StatCollection.AddStatistic<bool>(AIRollPassStatName, false);
            }

            return component.StatCollection.GetStatistic(AIRollPassStatName).Value<bool>();
        }

        public static void setAIRollPassed(this MechComponent component, bool val)
        {
            SetOrAddStat(component.StatCollection, AIRollPassStatName, val);
        }

        private static bool isAISwitchedOnThisRound(this MechComponent component)
        {
            if (!Core.checkExistance(component.StatCollection, AISwitchedOnRoundStatName))
            {
                return false;
            }

            return component.StatCollection.GetStatistic(AISwitchedOnRoundStatName).Value<int>() == component.parent.Combat.TurnDirector.CurrentRound;
        }

        private static void setAISwitchedOnThisRound(this MechComponent component)
        {
            SetOrAddStat(component.StatCollection, AISwitchedOnRoundStatName, component.parent.Combat.TurnDirector.CurrentRound);
            component.setAIUsedThisRound();
        }

        private static bool isAISwitchedOffThisRound(this MechComponent component)
        {
            if (!Core.checkExistance(component.StatCollection, AISwitchedOffRoundStatName))
            {
                return false;
            }

            return component.StatCollection.GetStatistic(AISwitchedOffRoundStatName).Value<int>() == component.parent.Combat.TurnDirector.CurrentRound;
        }

        private static void AIDeactivate(this MechComponent component)
        {
            ActivatableComponent.deactivateComponent(component);
            SetOrAddStat(component.StatCollection, AISwitchedOffRoundStatName, component.parent.Combat.TurnDirector.CurrentRound);
        }

        private static bool AICheatRoll(this AbstractActor actor)
        {
            if (!actor.isAIUnit())
            {
                return false;
            }

            float roll = Random.Range(0f, 1f);
            return roll < Core.Settings.AIActivatableCheating;
        }
    }
}
