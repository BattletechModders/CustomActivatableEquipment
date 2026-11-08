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
        private static readonly string[] AIPurposeTags = { "cae_ai_offence", "cae_ai_defence", "cae_ai_sensors", "cae_ai_cool", "cae_ai_speed", "cae_ai_ecm", "cae_ai_stability", "cae_ai_random", "cae_ai_siege", "cae_ai_overheat_protection" };

        private static readonly string[] AITargetTags = { "cae_ai_speed", "cae_ai_offence", "cae_ai_defence", "cae_ai_sensors", "cae_ai_siege", "cae_ai_ecm" };

        private static readonly string[] AIInRangeTags = { "cae_ai_sensors", "cae_ai_siege" };

        private class AIDefInfo
        {
            public bool Relevant;
            public bool NeedTargets;
            public bool NeedSensors;
            public bool NeedSpawn;
        }

        private static readonly Dictionary<string, AIDefInfo> aiDefInfos = new Dictionary<string, AIDefInfo>();

        private static AIDefInfo AIInfo(this MechComponent component)
        {
            string id = component.componentDef.Description.Id;
            if (aiDefInfos.TryGetValue(id, out AIDefInfo info))
            {
                return info;
            }

            ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
            info = new AIDefInfo
            {
                Relevant = activatable != null && activatable.CanBeactivatedManualy && (activatable.InjuryOnFail || component.hasAIPurposeTag()),
                NeedTargets = component.hasAnyTag(AITargetTags),
                NeedSensors = component.hasAnyTag(AIInRangeTags),
                NeedSpawn = activatable != null && activatable.ChargesCount > 0 && !component.isSpeed()
            };
            aiDefInfos[id] = info;
            return info;
        }

        private static bool hasAnyTag(this MechComponent component, string[] tags)
        {
            return tags.Any(tag => component.ComponentTags().Contains(tag));
        }

        private static bool hasAIPurposeTag(this MechComponent component)
        {
            return component.hasAnyTag(AIPurposeTags);
        }

        public static bool isOffence(this MechComponent component)
        {
            return component.ComponentTags().Contains("cae_ai_offence");
        }

        public static bool isDefence(this MechComponent component)
        {
            return component.ComponentTags().Contains("cae_ai_defence");
        }

        public static bool isSensors(this MechComponent component)
        {
            return component.ComponentTags().Contains("cae_ai_sensors");
        }

        public static bool isCool(this MechComponent component)
        {
            return component.ComponentTags().Contains("cae_ai_cool");
        }

        public static bool isSpeed(this MechComponent component)
        {
            return component.ComponentTags().Contains("cae_ai_speed");
        }

        public static bool isEcm(this MechComponent component)
        {
            return component.ComponentTags().Contains("cae_ai_ecm");
        }

        public static bool isStability(this MechComponent component)
        {
            return component.ComponentTags().Contains("cae_ai_stability");
        }

        public static bool isSiege(this MechComponent component)
        {
            return component.ComponentTags().Contains("cae_ai_siege");
        }

        public static bool isRandom(this MechComponent component)
        {
            return component.ComponentTags().Contains("cae_ai_random");
        }

        public static bool isOverheatProtection(this MechComponent component)
        {
            return component.ComponentTags().Contains("cae_ai_overheat_protection");
        }

        public static bool isHeat(this MechComponent component)
        {
            return component.ComponentTags().Contains("cae_ai_heat");
        }

        public static bool isExplode(this MechComponent component)
        {
            return component.ComponentTags().Contains("cae_ai_explode");
        }

        public static bool CanBeActivated(this MechComponent component)
        {
            ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
            if (component.DamageLevel >= ComponentDamageLevel.Destroyed)
            {
                return false;
            }

            if (activatable == null)
            {
                return false;
            }

            ;
            if (!activatable.CanBeactivatedManualy)
            {
                return false;
            }

            if (activatable.ChargesCount == -1)
            {
                return true;
            }

            ;
            if (activatable.ChargesCount > 0)
            {
                if (component.ChargesCount() <= 0)
                {
                    return false;
                }

                ;
            }

            ;
            return true;
        }

        public static int ChargesCount(this MechComponent component)
        {
            return ActivatableComponent.getChargesCount(component);
        }

        public static bool CanBeSwitchedNow(this MechComponent component, bool activationEnd)
        {
            if (!activationEnd)
            {
                return true;
            }

            ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
            if (activatable == null)
            {
                return false;
            }

            if (!activatable.CanActivateAfterMove && component.parent.HasMovedThisRound)
            {
                return false;
            }

            if (!activatable.CanActivateAfterFire && component.parent.HasFiredThisRound)
            {
                return false;
            }

            return true;
        }

        public static bool isDamaged(this MechComponent component)
        {
            return component.DamageLevel > ComponentDamageLevel.Functional && component.DamageLevel < ComponentDamageLevel.Destroyed;
        }

        public static float FailChance(this MechComponent component)
        {
            ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
            if (activatable == null)
            {
                return 0f;
            }

            ;
            if (activatable.AlwaysFail)
            {
                return 1f;
            }

            ;
            return ActivatableComponent.getEffectiveComponentFailChance(component);
        }

        public static bool isFailDanger(this MechComponent component)
        {
            ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
            Mech mech = component.parent as Mech;
            if (component.isExplode() && (activatable.FailCrit || activatable.FailCritComponents || activatable.FailISDamage > Core.Epsilon || activatable.ExplodeOnFail))
            {
                return true;
            }

            ;
            if (mech == null)
            {
                return false;
            }

            foreach (var Location in activatable.FailDamageLocations)
            {
                foreach (var lcomp in mech.GetComponentsForLocation(Location, ComponentType.AmmunitionBox))
                {
                    return true;
                }

                if (component.parent.StructureForLocation((int)Location) < activatable.FailISDamage)
                {
                    return true;
                }
            }

            return false;
        }

        public static int PilotHealthLeft(this MechComponent component)
        {
            Pilot pilot = component.parent?.GetPilot();
            if (pilot == null)
            {
                return int.MaxValue;
            }

            return pilot.TotalHealth - pilot.Injuries;
        }

        public static float InjuryRiskFactor(this MechComponent component)
        {
            ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
            if (activatable == null || !activatable.InjuryOnFail)
            {
                return 1f;
            }

            if (component.parent is ICustomMech custMech && custMech.isSquad)
            {
                return 1f;
            }

            int healthLeft = component.PilotHealthLeft();
            if (healthLeft >= 4)
            {
                return 1f;
            }

            if (healthLeft == 3)
            {
                return 0.75f;
            }

            if (healthLeft == 2)
            {
                return 0.34f;
            }

            return 0f;
        }

        public static bool IsInjuryRiskTooHigh(this MechComponent component, float injuryRisk)
        {
            if (injuryRisk >= 1f)
            {
                return false;
            }

            ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
            float chance = component.FailChance();
            if (injuryRisk <= 0f)
            {
                return chance > Core.Epsilon || activatable.FailChancePerTurn > Core.Epsilon;
            }

            return chance >= Core.Settings.AIScoreMaxRisk * injuryRisk;
        }
    }
}
