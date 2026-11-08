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
        private static float ComponentScore(AISituation situation, MechComponent component, bool isActive)
        {
            float score = 0f;
            if (component.isSpeed())
            {
                score = Math.Max(score, LogScore("speed", SpeedScore(situation)));
            }

            if (component.isOffence())
            {
                score = Math.Max(score, LogScore("offence", ThreatScore(situation, situation.OffenceCoeff, Core.Settings.AIOffenceUsefullCoeff)));
            }

            if (component.isDefence())
            {
                score = Math.Max(score, LogScore("defence", ThreatScore(situation, situation.DefenceCoeff, Core.Settings.AIDefenceUsefullCoeff)));
            }

            if (component.isSensors())
            {
                score = Math.Max(score, LogScore("sensors", SensorsScore(situation)));
            }

            if (component.isCool())
            {
                score = Math.Max(score, LogScore("cool", CoolScore(situation)));
            }

            if (component.isSiege())
            {
                score = Math.Max(score, SiegeScore(situation));
            }

            if (component.isStability())
            {
                score = Math.Max(score, LogScore("stability", StabilityScore(situation, isActive)));
            }

            if (component.isRandom())
            {
                score = Math.Max(score, LogScore("random", RandomScore(situation, isActive)));
            }

            if (component.isOverheatProtection())
            {
                score = Math.Max(score, OverheatProtectionScore(situation, isActive));
            }

            if (component.isEcm())
            {
                score = Math.Max(score, LogScore("ecm", component.EcmScore(situation.DefenceCoeff)));
            }

            return score;
        }

        private static float LogScore(string name, float score)
        {
            Log.Debug?.Write("  " + name + " score:" + score + "\n");
            return score;
        }

        private static float SpeedScore(AISituation situation)
        {
            if (situation.IsAnyLocationExposed)
            {
                return 1f;
            }

            if (situation.VisibleTargets == 0)
            {
                return 0.6f;
            }

            return situation.EnemyInWeaponRange ? 0f : 0.5f;
        }

        private static float ThreatScore(AISituation situation, float coeff, float usefullCoeff)
        {
            if (situation.VisibleTargets > 1 && situation.IsAnyLocationExposed)
            {
                return 1f;
            }

            return RampScore(coeff, usefullCoeff);
        }

        private static float SensorsScore(AISituation situation)
        {
            float score = 0f;
            if (situation.VisibleTargets == 0)
            {
                score = 0.8f;
            }
            else if (situation.VisibleTargets == 1)
            {
                score = 0.5f;
            }

            if (situation.TargetsInRange > 0 && situation.NoSensorsTargets > 0)
            {
                score = Math.Max(score, 0.5f + 0.3f * situation.NoSensorsTargets / situation.TargetsInRange);
            }

            return score;
        }

        private static float CoolScore(AISituation situation)
        {
            float score = 0f;
            if (situation.HeatCoeff > Core.Settings.AIHeatCoeffCoeff)
            {
                score = 0.5f;
            }

            if (situation.OverheatCoeff > Core.Settings.AIOverheatCoeffCoeff)
            {
                score = Math.Max(score, Math.Min(1f, 0.8f + 0.2f * (situation.OverheatCoeff - Core.Settings.AIOverheatCoeffCoeff) / 0.2f));
            }

            return score;
        }

        private static float SiegeScore(AISituation situation)
        {
            float score = 0f;
            AbstractActor closest = situation.Unit.EnemyInReach();
            if (situation.TargetsInRange > 0 && closest == null)
            {
                score = Math.Max(0.5f, RampScore(situation.OffenceCoeff, Core.Settings.AIOffenceUsefullCoeff));
            }

            Log.Debug?.Write("  siege: targets in range:" + situation.TargetsInRange + " enemy in reach:" + (closest == null ? "none" : closest.DisplayName) + " score:" + score + "\n");
            return score;
        }

        private static float StabilityScore(AISituation situation, bool isActive)
        {
            if (isActive)
            {
                Log.Debug?.Write("  stability frozen while active, enemy in weapon range:" + situation.EnemyInWeaponRange + "\n");
                return situation.EnemyInWeaponRange ? 0.5f : 0f;
            }

            if (situation.Mech == null || situation.Mech.UnsteadyThreshold <= Core.Epsilon)
            {
                return 0f;
            }

            float ratio = situation.Mech.CurrentStability / situation.Mech.UnsteadyThreshold;
            float limit = Core.Settings.AIStabilityRatio;
            Log.Debug?.Write("  stability:" + situation.Mech.CurrentStability + " / unsteady:" + situation.Mech.UnsteadyThreshold + "\n");
            if (ratio < limit)
            {
                return 0f;
            }

            return Math.Min(1f, 0.5f + 0.5f * (ratio - limit) / Math.Max(Core.Epsilon, 1f - limit));
        }

        private static float RandomScore(AISituation situation, bool isActive)
        {
            if (isActive)
            {
                return Core.Settings.AIScoreOn;
            }

            if (!situation.ActivationEnd && Random.Range(0f, 1f) < Core.Settings.AIRandomChance)
            {
                return Core.Settings.AIScoreOn;
            }

            return 0f;
        }

        private static float OverheatProtectionScore(AISituation situation, bool isActive)
        {
            float score = 1f;
            bool hasHeat = situation.Unit.isHasHeat() && !(situation.Unit is ICustomMech custMech && custMech.isSquad);
            if (hasHeat && situation.Mech != null)
            {
                if (situation.HeatCoeff > Core.Settings.AIOverheatProtectionOffHeat)
                {
                    score = 0f;
                }
                else if (situation.HeatCoeff >= Core.Settings.AIOverheatProtectionOnHeat)
                {
                    score = isActive ? Core.Settings.AIScoreOn : 0f;
                }
            }

            Log.Debug?.Write("  overheat protection: has heat:" + hasHeat + " heat / overheat level:" + situation.HeatCoeff + " score:" + score + "\n");
            return score;
        }

        public static float EcmScore(this MechComponent component, float deffenceCoeff)
        {
            AbstractActor unit = component.parent;
            float range = 0f;
            foreach (AuraDef aura in component.componentDef.GetAuras())
            {
                if (aura.State != AuraState.Online)
                {
                    continue;
                }

                float r = aura.Range;
                if (!string.IsNullOrEmpty(aura.RangeStatistic) && unit.StatCollection.ContainsStatistic(aura.RangeStatistic))
                {
                    r = unit.StatCollection.GetValue<float>(aura.RangeStatistic);
                }

                range = Math.Max(range, r);
            }

            if (range <= Core.Epsilon)
            {
                return 0f;
            }

            int enemiesNear = unit.GetVisibleEnemyUnits()
                .Where(enemy => !enemy.IsDead)
                .Count(enemy => unit.IsWithin(enemy, range));

            int jammedAlliesNear = unit.Combat.GetAllAlliesOf(unit)
                .Where(ally => !ally.IsDead && ally != unit && unit.IsWithin(ally, range))
                .Count(ally => ally.EcmJammed() > 0);

            float active = 0f;
            if (enemiesNear > 0)
            {
                active = Math.Min(0.8f, 0.5f + 0.15f * (enemiesNear - 1));
            }

            if (jammedAlliesNear > 0)
            {
                active += 0.2f;
            }

            float passive = 0.5f * RampScore(deffenceCoeff, Core.Settings.AIDefenceUsefullCoeff);
            int selfJammed = unit.EcmJammed();
            if (selfJammed > 0)
            {
                passive += 0.3f;
            }

            Log.Debug?.Write("  ecm range:" + range + " enemies near:" + enemiesNear + " jammed allies near:" + jammedAlliesNear + " self jammed:" + selfJammed + " active:" + active + " passive:" + passive + "\n");
            return Mathf.Clamp01(active - passive);
        }

        public static float RampScore(float coeff, float usefullCoeff)
        {
            if (coeff <= Core.Epsilon)
            {
                return 0f;
            }

            if (coeff <= usefullCoeff)
            {
                return 0.8f * coeff / usefullCoeff;
            }

            return Math.Min(1f, 0.8f + 0.2f * (coeff - usefullCoeff) / usefullCoeff);
        }

        private static float AdjustScore(AISituation situation, MechComponent component, float score)
        {
            ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
            if (situation.ChargesBlocked && activatable.ChargesCount > 0 && !component.isSpeed())
            {
                Log.Debug?.Write("  limited uses, spawn protection: score 0\n");
                return 0f;
            }

            if (score <= 0f)
            {
                return score;
            }

            if (!situation.ActivationEnd && !component.isOverheatProtection() && (!activatable.CanActivateAfterMove || !activatable.CanActivateAfterFire))
            {
                score += Core.Settings.AILastChanceBonus;
                Log.Debug?.Write("  can't be switched after move / fire: last chance +" + Core.Settings.AILastChanceBonus + "\n");
            }

            if (component.isHeat() && situation.IsHot)
            {
                score -= Core.Settings.AIHotPenalty;
                Log.Debug?.Write("  i'm hot: -" + Core.Settings.AIHotPenalty + "\n");
            }

            return Mathf.Clamp01(score);
        }

        private static bool IsRiskAcceptable(AISituation situation, MechComponent component, float score, float injuryRisk)
        {
            if (!component.isFailDanger() && injuryRisk >= 1f)
            {
                return true;
            }

            if (injuryRisk <= 0f)
            {
                bool canFail = component.IsInjuryRiskTooHigh(injuryRisk);
                Log.Debug?.Write(canFail ? "  pilot can't take another injury\n" : "  pilot can't take another injury, but it can't fail now\n");
                return !canFail;
            }

            float allowed = score * Core.Settings.AIScoreMaxRisk * injuryRisk;
            if (component.isDamaged() && !situation.IsAnyLocationExposed)
            {
                allowed *= 0.5f;
            }

            Log.Debug?.Write("  danger. FailChance:" + component.FailChance() + " allowed:" + allowed + "\n");
            return component.FailChance() < allowed;
        }
    }
}
