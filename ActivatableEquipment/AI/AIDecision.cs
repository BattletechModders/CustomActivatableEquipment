using Random = UnityEngine.Random;

namespace CustomActivatableEquipment.AI
{
    internal enum AIAction
    {
        None,
        InjurySwitchOff,
        KeepOn,
        SwitchOn,
        SwitchOff
    }

    internal static class AIDecision
    {
        public static AIAction Decide(AISituation situation, AIComponent component)
        {
            float injuryFactor = component.InjuryFactor;
            bool isActive = ActivatableComponent.isComponentActivated(component.Component);
            if (injuryFactor < 1f)
            {
                Log.Debug?.Write("  injury on fail, pilot health left " + component.PilotHealthLeft + ": risk x" + injuryFactor + "\n");
            }

            if (isActive && component.IsInjuryRiskTooHigh(injuryFactor))
            {
                return AIAction.InjurySwitchOff;
            }

            float score = AIScoring.Score(situation, component, isActive);
            bool riskOk = AIScoring.IsRiskAcceptable(situation, component, score, injuryFactor);
            bool wanted = riskOk && score >= (isActive ? Core.Settings.AIScoreOff : Core.Settings.AIScoreOn);
            Log.Debug?.Write("  score:" + score + (isActive ? " active" : " inactive") + (wanted ? " wanted" : " not wanted") + "\n");
            if (wanted)
            {
                return isActive ? AIAction.KeepOn : AIAction.SwitchOn;
            }

            return isActive ? AIAction.SwitchOff : AIAction.None;
        }

        public static void Apply(AISituation situation, AIComponent component, AIAction action)
        {
            switch (action)
            {
                case AIAction.InjurySwitchOff:
                    SwitchOffIfAllowed(situation, component, "  injury risk too high for the pilot. deactivating\n", "  injury risk too high for the pilot, but can't be deactivated after move / fire\n");
                    break;
                case AIAction.KeepOn:
                    ApplyKeepOn(situation, component);
                    break;
                case AIAction.SwitchOn:
                    ApplySwitchOn(situation, component);
                    break;
                case AIAction.SwitchOff:
                    ApplySwitchOff(situation, component);
                    break;
            }
        }

        private static void ApplyKeepOn(AISituation situation, AIComponent component)
        {
            if (!CheatRoll())
            {
                return;
            }

            if (ActivatableComponent.rollFail(component.Component, false, true))
            {
                component.NextRollForcedToPass = true;
            }
            else if (situation.ActivationEnd && component.SwitchedOnThisRound)
            {
                Log.Debug?.Write("  high possible fail, but switched on this round: stays active until next round\n");
            }
            else
            {
                SwitchOffIfAllowed(situation, component, "  deactivating due to high possible fail\n", "  high possible fail, but can't be switched after move / fire\n");
            }
        }

        private static void ApplySwitchOn(AISituation situation, AIComponent component)
        {
            if (component.OncePerRoundUsed)
            {
                Log.Debug?.Write("  ActivateOncePerRound: already switched on this round\n");
                return;
            }

            if (situation.ActivationEnd && component.SwitchedOffThisRound)
            {
                Log.Debug?.Write("  switched off this round: stays off until next round\n");
                return;
            }

            if (situation.ActivationEnd && component.Activatable.ChargesCount != 0)
            {
                Log.Debug?.Write("  uses charges: only used at round start\n");
                return;
            }

            if (!component.CanSwitchNow(situation.ActivationEnd))
            {
                Log.Debug?.Write("  can't be activated after move / fire\n");
                return;
            }

            if (CheatRoll())
            {
                if (!ActivatableComponent.rollFail(component.Component, true, true))
                {
                    Log.Debug?.Write("  not activate due to high possible fail\n");
                    return;
                }

                Log.Debug?.Write("  activating (test roll passed, the activation roll will pass)\n");
                component.NextRollForcedToPass = true;
            }
            else
            {
                Log.Debug?.Write("  activating (no test roll)\n");
            }

            component.SwitchOn();
        }

        private static void ApplySwitchOff(AISituation situation, AIComponent component)
        {
            if (!component.Info.HasPurpose)
            {
                Log.Debug?.Write("  not needed. not AI managed, stays active\n");
            }
            else if (situation.ActivationEnd && component.SwitchedOnThisRound)
            {
                Log.Debug?.Write("  not needed, but switched on this round: stays active until next round\n");
            }
            else
            {
                SwitchOffIfAllowed(situation, component, "  not needed. deactivating\n", "  not needed, but can't be deactivated after move / fire\n");
            }
        }

        private static void SwitchOffIfAllowed(AISituation situation, AIComponent component, string switchingOffLog, string blockedLog)
        {
            if (!component.CanSwitchNow(situation.ActivationEnd))
            {
                Log.Debug?.Write(blockedLog);
                return;
            }

            Log.Debug?.Write(switchingOffLog);
            component.SwitchOff();
        }

        private static bool CheatRoll()
        {
            return Random.Range(0f, 1f) < Core.Settings.AIActivatableCheating;
        }
    }
}
