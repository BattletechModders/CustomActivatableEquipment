namespace CustomActivatableEquipment.AI.Scores
{
    internal class OffenceScore : ThreatScore
    {
        public override AITrait Trait => AITrait.Offence;

        protected override float UsefulValue => Core.Settings.AIOffenceUsefullCoeff;

        protected override float Value(AISituation situation)
        {
            return situation.OffenceValue;
        }
    }
}
