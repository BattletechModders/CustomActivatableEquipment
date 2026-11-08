using BattleTech;
using System;

namespace CustomActivatableEquipment.AI.Scores
{
    internal class StabilityScore : AIScore
    {
        public override string Tag => "cae_ai_stability";

        public override float Score(AISituation situation, AIComponent component, bool isActive)
        {
            if (isActive)
            {
                Log.Debug?.Write("  stability frozen while active, enemy in weapon range:" + situation.EnemyInWeaponRange + "\n");
                return situation.EnemyInWeaponRange ? 0.5f : 0f;
            }

            Mech mech = situation.Mech;
            if (mech == null || mech.UnsteadyThreshold <= Core.Epsilon)
            {
                return 0f;
            }

            float ratio = mech.CurrentStability / mech.UnsteadyThreshold;
            float limit = Core.Settings.AIStabilityRatio;
            Log.Debug?.Write("  stability:" + mech.CurrentStability + " / unsteady:" + mech.UnsteadyThreshold + "\n");
            if (ratio < limit)
            {
                return 0f;
            }

            return Math.Min(1f, 0.5f + 0.5f * (ratio - limit) / Math.Max(Core.Epsilon, 1f - limit));
        }
    }
}
