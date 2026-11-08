using System;
using UnityEngine;

namespace CustomActivatableEquipment.AI.Scores
{
    internal class EcmScore : AIScore
    {
        public override string Tag => "cae_ai_ecm";

        public override bool NeedsThreats => true;

        public override float Score(AISituation situation, AIComponent component, bool isActive)
        {
            float range = component.AuraRange;
            if (range <= Core.Epsilon)
            {
                return 0f;
            }

            int enemiesNear = situation.EnemiesWithin(range);
            int jammedAlliesNear = situation.JammedAlliesWithin(range);
            float active = 0f;
            if (enemiesNear > 0)
            {
                active = Math.Min(0.8f, 0.5f + 0.15f * (enemiesNear - 1));
            }

            if (jammedAlliesNear > 0)
            {
                active += 0.2f;
            }

            float passive = 0.5f * Ramp(situation.DefenceValue, Core.Settings.AIDefenceUsefullCoeff);
            if (situation.IsSelfJammed)
            {
                passive += 0.3f;
            }

            Log.Debug?.Write("  ecm range:" + range + " enemies near:" + enemiesNear + " jammed allies near:" + jammedAlliesNear + " self jammed:" + situation.IsSelfJammed + " active:" + active + " passive:" + passive + "\n");
            return Mathf.Clamp01(active - passive);
        }
    }
}
