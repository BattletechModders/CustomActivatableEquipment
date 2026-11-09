using System;

namespace CustomActivatableEquipment.AI.Scores
{
    internal class CoolScore : AIScore
    {
        public override AITrait Trait => AITrait.Cool;

        public override float Score(AISituation situation, AIComponent component, bool isActive)
        {
            if (situation.OverheatRatio > Core.Settings.AIOverheatCoeff)
            {
                return Math.Min(1f, 0.8f + 0.2f * (situation.OverheatRatio - Core.Settings.AIOverheatCoeff) / 0.2f);
            }

            return situation.HeatRatio > Core.Settings.AIHeatCoeff ? 0.5f : 0f;
        }
    }
}
