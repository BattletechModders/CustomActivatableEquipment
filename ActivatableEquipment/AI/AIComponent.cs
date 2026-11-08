using BattleTech;
using CustomComponents;
using IRBTModUtils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CustomActivatableEquipment.AI
{
    internal class AIComponent
    {
        private const string NextRollForcedToPassStatName = "CAEAIFailRollPassed";
        private const string SwitchedOnRoundStatName = "CAEAISwitchedOnRound";
        private const string SwitchedOffRoundStatName = "CAEAISwitchedOffRound";
        private static readonly Dictionary<string, DefInfo> DefInfos = new Dictionary<string, DefInfo>();

        public readonly MechComponent Component;
        public readonly ActivatableComponent Activatable;
        private DefInfo info;

        internal class DefInfo
        {
            public readonly HashSet<string> Tags;
            public readonly bool HasPurpose;
            public readonly bool Controllable;
            public readonly bool NeedsThreats;
            public readonly bool NeedsTargetsInRange;
            public readonly bool NeedsSpawnCheck;

            public DefInfo(MechComponent component, ActivatableComponent activatable)
            {
                Tags = new HashSet<string>(component.ComponentTags().Select(tag => tag.ToLowerInvariant()).Where(tag => tag.StartsWith("cae_ai_", StringComparison.Ordinal)));
                HasPurpose = AIScoring.PurposeTags.Any(Tags.Contains);
                Controllable = activatable != null && activatable.CanBeactivatedManualy && (activatable.InjuryOnFail || HasPurpose);
                NeedsThreats = AIScoring.NeedsThreats(Tags);
                NeedsTargetsInRange = AIScoring.NeedsTargetsInRange(Tags);
                NeedsSpawnCheck = activatable != null && activatable.ChargesCount > 0 && !Tags.Contains("cae_ai_speed");
            }
        }

        public AIComponent(MechComponent component)
        {
            Component = component;
            Activatable = component.componentDef.GetComponent<ActivatableComponent>();
        }

        public DefInfo Info
        {
            get
            {
                if (info == null)
                {
                    string id = Component.componentDef.Description.Id;
                    if (!DefInfos.TryGetValue(id, out info))
                    {
                        info = new DefInfo(Component, Activatable);
                        DefInfos[id] = info;
                    }
                }

                return info;
            }
        }

        private int Round => Component.parent.Combat.TurnDirector.CurrentRound;

        public bool IsAIControllable => Info.Controllable && Component.DamageLevel < ComponentDamageLevel.Destroyed && (Activatable.ChargesCount <= 0 || ActivatableComponent.getChargesCount(Component) > 0);

        public bool CanSwitchNow(bool activationEnd)
        {
            if (!activationEnd)
            {
                return true;
            }

            if (!Activatable.CanActivateAfterMove && Component.parent.HasMovedThisRound)
            {
                return false;
            }

            return Activatable.CanActivateAfterFire || !Component.parent.HasFiredThisRound;
        }

        public bool IsFailDangerous
        {
            get
            {
                if (Info.Tags.Contains("cae_ai_explode") && (Activatable.FailCrit || Activatable.FailCritComponents || Activatable.FailISDamage > Core.Epsilon || Activatable.ExplodeOnFail))
                {
                    return true;
                }

                if (!(Component.parent is Mech mech))
                {
                    return false;
                }

                foreach (ChassisLocations location in Activatable.FailDamageLocations)
                {
                    if (mech.GetComponentsForLocation(location, ComponentType.AmmunitionBox).Any())
                    {
                        return true;
                    }

                    if (mech.StructureForLocation((int)location) < Activatable.FailISDamage)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public int PilotHealthLeft
        {
            get
            {
                Pilot pilot = Component.parent?.GetPilot();
                return pilot == null ? int.MaxValue : pilot.TotalHealth - pilot.Injuries;
            }
        }

        public float InjuryFactor
        {
            get
            {
                if (!Activatable.InjuryOnFail || (Component.parent is ICustomMech custMech && custMech.isSquad))
                {
                    return 1f;
                }

                int healthLeft = PilotHealthLeft;
                if (healthLeft >= 4)
                {
                    return 1f;
                }

                if (healthLeft == 3)
                {
                    return 0.75f;
                }

                return healthLeft == 2 ? 0.33f : 0f;
            }
        }

        public bool IsInjuryRiskTooHigh(float injuryFactor)
        {
            if (injuryFactor >= 1f)
            {
                return false;
            }

            float failChance = ActivatableComponent.getActivationFailChance(Component);
            if (injuryFactor <= 0f)
            {
                return failChance > Core.Epsilon || Activatable.FailChancePerTurn > Core.Epsilon;
            }

            return failChance >= Core.Settings.AIScoreMaxRisk * injuryFactor;
        }

        public float AuraRange
        {
            get
            {
                StatCollection stats = Component.parent.StatCollection;
                float range = 0f;
                foreach (AuraDef aura in Component.componentDef.GetAuras().Where(aura => aura.State == AuraState.Online))
                {
                    bool hasRangeStat = !string.IsNullOrEmpty(aura.RangeStatistic) && stats.ContainsStatistic(aura.RangeStatistic);
                    range = Math.Max(range, hasRangeStat ? stats.GetValue<float>(aura.RangeStatistic) : aura.Range);
                }

                return range;
            }
        }

        public bool NextRollForcedToPass
        {
            get => GetStat(NextRollForcedToPassStatName, false);
            set => SetStat(NextRollForcedToPassStatName, value);
        }

        public bool OncePerRoundUsed => Activatable.ActivateOncePerRound && ActivatableComponent.getComponentActivedRound(Component) == Round;

        public bool SwitchedOnThisRound => IsCurrentRound(SwitchedOnRoundStatName);

        public bool SwitchedOffThisRound => IsCurrentRound(SwitchedOffRoundStatName);

        public void SwitchOn()
        {
            ActivatableComponent.activateComponent(Component, false, false);
            SetStat(SwitchedOnRoundStatName, Round);
            if (Activatable.ActivateOncePerRound)
            {
                SetStat(ActivatableComponent.CAEComponentActivedRound, Round);
            }
        }

        public void SwitchOff()
        {
            ActivatableComponent.deactivateComponent(Component);
            SetStat(SwitchedOffRoundStatName, Round);
        }

        private bool IsCurrentRound(string statName)
        {
            return GetStat(statName, -1) == Round;
        }

        private T GetStat<T>(string statName, T fallback)
        {
            return Core.checkExistance(Component.StatCollection, statName) ? Component.StatCollection.GetValue<T>(statName) : fallback;
        }

        private void SetStat<T>(string statName, T value)
        {
            if (Core.checkExistance(Component.StatCollection, statName))
            {
                Component.StatCollection.Set(statName, value);
            }
            else
            {
                Component.StatCollection.AddStatistic(statName, value);
            }
        }
    }
}
