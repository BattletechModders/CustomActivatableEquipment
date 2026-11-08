using Random = UnityEngine.Random;

namespace CustomActivatableEquipment.AI.Scores
{
    internal class RandomScore : AIScore
    {
        public override string Tag => "cae_ai_random";

        public override float Score(AISituation situation, AIComponent component, bool isActive)
        {
            return LogScore("random", RandomPick(situation, isActive));
        }

        private static float RandomPick(AISituation situation, bool isActive)
        {
            if (isActive)
            {
                return Core.Settings.AIScoreOn;
            }

            if (!situation.ActivationEnd && Random.Range(0f, 1f) < Core.Settings.AIRandomChance)
            {
                return Core.Settings.AIScoreOn;
            }

            return 0f;
        }
    }
}
