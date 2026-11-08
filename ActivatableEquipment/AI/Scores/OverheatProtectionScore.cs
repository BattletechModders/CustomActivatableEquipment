namespace CustomActivatableEquipment.AI.Scores
{
    internal class OverheatProtectionScore : AIScore
    {
        public override string Tag => "cae_ai_overheat_protection";

        public override float Score(AISituation situation, AIComponent component, bool isActive)
        {
            Log.Debug?.Write("  overheat protection: has heat:" + situation.HasHeat + " heat / overheat level:" + situation.HeatRatio + "\n");
            if (!situation.HasHeat)
            {
                return 1f;
            }

            if (situation.HeatRatio > Core.Settings.AIOverheatProtectionOffHeat)
            {
                return 0f;
            }

            if (situation.HeatRatio >= Core.Settings.AIOverheatProtectionOnHeat)
            {
                return isActive ? Core.Settings.AIScoreOn : 0f;
            }

            return 1f;
        }
    }
}
