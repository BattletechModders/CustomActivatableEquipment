using BattleTech;
using CustomActivatableEquipment.AI.Scores;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CustomActivatableEquipment.AI
{
    internal static class AIScoring
    {
        private static readonly AIScore[] Scores =
        {
            new SpeedScore(),
            new OffenceScore(),
            new DefenceScore(),
            new SensorsScore(),
            new CoolScore(),
            new SiegeScore(),
            new StabilityScore(),
            new OverheatProtectionScore(),
            new EcmScore(),
            new AlwaysScore()
        };

        public static readonly string[] PurposeTags = Scores.Select(score => score.Tag).ToArray();

        public static bool NeedsThreats(HashSet<string> tags)
        {
            return Scores.Any(score => score.NeedsThreats && tags.Contains(score.Tag));
        }

        public static bool NeedsTargetsInRange(HashSet<string> tags)
        {
            return Scores.Any(score => score.NeedsTargetsInRange && tags.Contains(score.Tag));
        }

        public static float Score(AISituation situation, AIComponent component, bool isActive)
        {
            float result = 0f;
            foreach (AIScore score in Scores.Where(score => component.Info.Tags.Contains(score.Tag)))
            {
                float value = score.Score(situation, component, isActive);
                Log.Debug?.Write("  " + score.GetType().Name + ":" + value + "\n");
                result = Math.Max(result, value);
            }

            return Adjust(situation, component, result);
        }

        public static bool IsRiskAcceptable(AISituation situation, AIComponent component, float score, float injuryFactor)
        {
            if (!component.IsFailDangerous && injuryFactor >= 1f)
            {
                return true;
            }

            if (injuryFactor <= 0f)
            {
                bool tooRisky = component.IsInjuryRiskTooHigh(injuryFactor);
                Log.Debug?.Write(tooRisky ? "  pilot can't take another injury\n" : "  pilot can't take another injury, but it can't fail now\n");
                return !tooRisky;
            }

            float allowed = score * Core.Settings.AIScoreMaxRisk * injuryFactor;
            if (component.Component.DamageLevel > ComponentDamageLevel.Functional && component.Component.DamageLevel < ComponentDamageLevel.Destroyed && !situation.IsExposed)
            {
                allowed *= 0.5f;
            }

            float failChance = ActivatableComponent.getActivationFailChance(component.Component);
            Log.Debug?.Write("  danger. FailChance:" + failChance + " allowed:" + allowed + "\n");
            return failChance < allowed;
        }

        private static float Adjust(AISituation situation, AIComponent component, float score)
        {
            if (situation.ChargesBlocked && component.Info.NeedsSpawnCheck)
            {
                Log.Debug?.Write("  limited uses, spawn protection: score 0\n");
                return 0f;
            }

            if (score <= 0f)
            {
                return score;
            }

            if (!situation.ActivationEnd && !component.Info.Tags.Contains("cae_ai_overheat_protection") && (!component.Activatable.CanActivateAfterMove || !component.Activatable.CanActivateAfterFire))
            {
                score += Core.Settings.AILastChanceBonus;
                Log.Debug?.Write("  can't be switched after move / fire: last chance +" + Core.Settings.AILastChanceBonus + "\n");
            }

            if (component.Info.Tags.Contains("cae_ai_heat") && situation.IsHot)
            {
                score -= Core.Settings.AIHotPenalty;
                Log.Debug?.Write("  i'm hot: -" + Core.Settings.AIHotPenalty + "\n");
            }

            return Mathf.Clamp01(score);
        }
    }
}
