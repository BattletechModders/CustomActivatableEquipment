using System;

namespace CustomActivatableEquipment.AI.Scores
{
    internal abstract class AIScore
    {
        public abstract AITrait Trait { get; }

        public virtual bool NeedsThreats => false;

        public virtual bool NeedsTargetsInRange => false;

        public abstract float Score(AISituation situation, AIComponent component, bool isActive);

        protected static float Ramp(float value, float usefulValue)
        {
            if (value <= Core.Epsilon)
            {
                return 0f;
            }

            if (value <= usefulValue)
            {
                return 0.8f * value / usefulValue;
            }

            return Math.Min(1f, 0.8f + 0.2f * (value - usefulValue) / usefulValue);
        }
    }
}
