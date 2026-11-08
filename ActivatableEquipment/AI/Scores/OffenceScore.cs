namespace CustomActivatableEquipment.AI.Scores
{
    internal class OffenceScore : ThreatScore
    {
        public override string Tag => "cae_ai_offence";

        protected override float UsefulValue => Core.Settings.AIOffenceUsefullCoeff;

        protected override float Value(AISituation situation)
        {
            return situation.OffenceValue;
        }
    }
}
