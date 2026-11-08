namespace CustomActivatableEquipment.AI.Scores
{
    internal class SensorsScore : AIScore
    {
        public override string Tag => "cae_ai_sensors";

        public override bool NeedsThreats => true;

        public override bool NeedsTargetsInRange => true;

        public override float Score(AISituation situation, AIComponent component, bool isActive)
        {
            switch (situation.VisibleTargets)
            {
                case 0:
                    return 0.8f;
                case 1:
                    return situation.TargetsWithoutSensors > 0 ? 0.8f : 0.5f;
            }

            if (situation.TargetsWithoutSensors == 0)
            {
                return 0f;
            }

            return 0.5f + 0.3f * situation.TargetsWithoutSensors / situation.TargetsInRange;
        }
    }
}
