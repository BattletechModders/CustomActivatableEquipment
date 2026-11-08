using BattleTech;
using System;

namespace CustomActivatableEquipment.AI.Scores
{
    internal class SiegeScore : AIScore
    {
        public override string Tag => "cae_ai_siege";

        public override bool NeedsThreats => true;

        public override bool NeedsTargetsInRange => true;

        public override float Score(AISituation situation, AIComponent component, bool isActive)
        {
            AbstractActor enemyInReach = situation.EnemyThatCanReachUs();
            Log.Debug?.Write("  siege: targets in range:" + situation.TargetsInRange + " enemy in reach:" + (enemyInReach == null ? "none" : enemyInReach.DisplayName) + "\n");
            if (situation.TargetsInRange == 0 || enemyInReach != null)
            {
                return 0f;
            }

            return Math.Max(0.5f, Ramp(situation.OffenceValue, Core.Settings.AIOffenceUsefullCoeff));
        }
    }
}
