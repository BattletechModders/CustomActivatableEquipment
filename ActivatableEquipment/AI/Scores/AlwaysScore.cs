namespace CustomActivatableEquipment.AI.Scores
{
    internal class AlwaysScore : AIScore
    {
        public override string Tag => "cae_ai_always";

        public override float Score(AISituation situation, AIComponent component, bool isActive)
        {
            return 1f;
        }
    }
}
