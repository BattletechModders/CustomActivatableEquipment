using System;
using System.Collections.Generic;

namespace CustomActivatableEquipment.AI
{
    [Flags]
    internal enum AITrait
    {
        None = 0,
        Speed = 1 << 0,
        Offence = 1 << 1,
        Defence = 1 << 2,
        Sensors = 1 << 3,
        Cool = 1 << 4,
        Siege = 1 << 5,
        Stability = 1 << 6,
        OverheatProtection = 1 << 7,
        Ecm = 1 << 8,
        Always = 1 << 9,
        Heat = 1 << 10,
        Explode = 1 << 11
    }

    internal static class AITraits
    {
        private static readonly Dictionary<string, AITrait> TagTraits = new Dictionary<string, AITrait>(StringComparer.OrdinalIgnoreCase)
        {
            { "cae_ai_speed", AITrait.Speed },
            { "cae_ai_offence", AITrait.Offence },
            { "cae_ai_defence", AITrait.Defence },
            { "cae_ai_sensors", AITrait.Sensors },
            { "cae_ai_cool", AITrait.Cool },
            { "cae_ai_siege", AITrait.Siege },
            { "cae_ai_stability", AITrait.Stability },
            { "cae_ai_overheat_protection", AITrait.OverheatProtection },
            { "cae_ai_ecm", AITrait.Ecm },
            { "cae_ai_always", AITrait.Always },
            { "cae_ai_heat", AITrait.Heat },
            { "cae_ai_explode", AITrait.Explode }
        };

        public static AITrait FromTags(IEnumerable<string> tags)
        {
            AITrait traits = AITrait.None;
            foreach (string tag in tags)
            {
                if (TagTraits.TryGetValue(tag, out AITrait trait))
                {
                    traits |= trait;
                }
            }

            return traits;
        }
    }
}
