namespace CustomActivatableEquipment.AI.Scores
{
    internal class SpeedScore : AIScore
    {
        public override AITrait Trait => AITrait.Speed;

        public override bool NeedsThreats => true;

        public override float Score(AISituation situation, AIComponent component, bool isActive)
        {
            if (situation.IsExposed)
            {
                return 1f;
            }

            if (situation.VisibleTargets == 0)
            {
                return 0.6f;
            }

            return situation.EnemyInWeaponRange ? 0f : 0.5f;
        }
    }
}
