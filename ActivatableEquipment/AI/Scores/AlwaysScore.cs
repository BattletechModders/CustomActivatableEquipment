namespace CustomActivatableEquipment.AI.Scores
{
    internal class AlwaysScore : AIScore
    {
        public override AITrait Trait => AITrait.Always;

        public override float Score(AISituation situation, AIComponent component, bool isActive)
        {
            return 1f;
        }
    }
}
