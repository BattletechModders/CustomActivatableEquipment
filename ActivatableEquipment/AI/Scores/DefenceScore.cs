namespace CustomActivatableEquipment.AI.Scores
{
    internal class DefenceScore : ThreatScore
    {
        public override string Tag => "cae_ai_defence";

        protected override float UsefulValue => Core.Settings.AIDefenceUsefullCoeff;

        protected override float Value(AISituation situation)
        {
            return situation.DefenceValue;
        }
    }
}
