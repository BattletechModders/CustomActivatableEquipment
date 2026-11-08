using BattleTech;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CustomActivatableEquipment.AI
{
    internal static class AIController
    {
        public static void Process(AbstractActor unit, bool activationEnd)
        {
            try
            {
                if (unit.IsDead || unit.TeamId == unit.Combat.LocalPlayerTeamGuid)
                {
                    return;
                }

                List<AIComponent> components = unit.allComponents.Select(component => new AIComponent(component)).Where(component => component.IsAIControllable).ToList();
                if (components.Count == 0)
                {
                    return;
                }

                Stopwatch aiTime = Stopwatch.StartNew();
                AISituation situation = new AISituation(unit, activationEnd, components);
                Log.Debug?.Write("Activation:\n");
                foreach (AIComponent component in components.TakeWhile(comp => !unit.IsDead))
                {
                    try
                    {
                        Log.Debug?.Write(" " + component.Component.defId + "\n");
                        component.NextRollForcedToPass = false;
                        AIAction action = AIDecision.Decide(situation, component);
                        AIDecision.Apply(situation, component, action);
                    }
                    catch (Exception e)
                    {
                        Log.WriteCritical("AI activatable exception in " + component.Component.defId + ":" + e.ToString() + "\n");
                    }
                }

                aiTime.Stop();
                Log.Debug?.Write("AI activatable time:" + aiTime.ElapsedMilliseconds + " msec\n");
            }
            catch (Exception e)
            {
                Log.WriteCritical("AI activatable exception:" + e.ToString() + "\n");
            }
        }
    }
}
