namespace CustomActivatableEquipment.AI.Scores
{
    internal abstract class ThreatScore : AIScore
    {
        public override bool NeedsThreats => true;

        protected abstract float UsefulValue { get; }

        protected abstract float Value(AISituation situation);

        public override float Score(AISituation situation, AIComponent component, bool isActive)
        {
            if (situation.VisibleTargets > 1 && situation.IsExposed)
            {
                return 1f;
            }

            return Ramp(Value(situation), UsefulValue);
        }
    }
}
