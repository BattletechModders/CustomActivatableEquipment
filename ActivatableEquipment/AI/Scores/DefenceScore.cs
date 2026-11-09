namespace CustomActivatableEquipment.AI.Scores
{
    internal class DefenceScore : ThreatScore
    {
        public override AITrait Trait => AITrait.Defence;

        protected override float UsefulValue => Core.Settings.AIDefenceUsefullCoeff;

        protected override float Value(AISituation situation)
        {
            return situation.DefenceValue;
        }
    }
}
