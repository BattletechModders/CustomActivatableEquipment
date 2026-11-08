using BattleTech;
using CustomActivatableEquipment;
using HarmonyLib;
using System;
using CustomComponents;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;
using CustomActivatableEquipment.DamageHelpers;
using CustAmmoCategories;
using IRBTModUtils;
using CustomAmmoCategoriesPatches;

namespace CustomActivatableEquipment
{
    public static partial class CAEAIHelper
    {
        private class AISituation
        {
            public AbstractActor Unit;
            public Mech Mech;
            public bool ActivationEnd;
            public int VisibleTargets;
            public float OffenceCoeff;
            public float DefenceCoeff;
            public float HeatCoeff;
            public float OverheatCoeff;
            public int TargetsInRange;
            public int NoSensorsTargets;
            public bool IsAnyLocationExposed;
            public bool IsHot;
            public bool ChargesBlocked;
            private bool? enemyInWeaponRange;

            public bool EnemyInWeaponRange
            {
                get
                {
                    if (enemyInWeaponRange == null)
                    {
                        enemyInWeaponRange = Unit.AnyEnemyInWeaponRange();
                    }
                    return enemyInWeaponRange.Value;
                }
            }
        }

        public static void TargetsCalc(this AbstractActor unit, ref int isVisibleTargets, ref float offenceCoeff, ref float deffenceCoeff)
        {
            isVisibleTargets = 0;
            float offenceCoeffUp = 0f;
            float offenceCoeffDown = 0f;
            float defenceCoeffUp = 0f;
            float defenceCoeffDown = 0f;
            foreach (var target in unit.GetVisibleEnemyUnits())
            {
                isVisibleTargets += 1;
                foreach (Weapon weapon in unit.Weapons)
                {
                    float toHit = 0f;
                    if (!weapon.CanFire)
                    {
                        continue;
                    }

                    if (weapon.WillFireAtTargetFromPosition(target, unit.CurrentPosition))
                    {
                        toHit = weapon.GetToHitFromPosition(target, 1, unit.CurrentPosition, target.CurrentPosition, true, target.IsEvasive, false);
                    }

                    if (toHit < Core.Epsilon)
                    {
                        continue;
                    }

                    ;
                    offenceCoeffUp += toHit * weapon.DamagePerShot * weapon.ShotsWhenFired;
                }

                foreach (Weapon weapon in target.Weapons)
                {
                    float toHit = 0f;
                    if (!weapon.CanFire)
                    {
                        continue;
                    }

                    if (weapon.WillFireAtTargetFromPosition(unit, target.CurrentPosition))
                    {
                        toHit = weapon.GetToHitFromPosition(unit, 1, target.CurrentPosition, unit.CurrentPosition, true, unit.IsEvasive, false);
                    }

                    if (toHit < Core.Epsilon)
                    {
                        continue;
                    }

                    ;
                    defenceCoeffUp += toHit * weapon.DamagePerShot * weapon.ShotsWhenFired;
                }

                offenceCoeffDown += target.SummaryArmorCurrent;
            }

            defenceCoeffDown = unit.SummaryArmorCurrent;
            if (isVisibleTargets == 0)
            {
                offenceCoeff = 0f;
                deffenceCoeff = 0f;
            }
            else
            {
                if (offenceCoeffDown > Core.Epsilon)
                {
                    offenceCoeff = offenceCoeffUp / offenceCoeffDown;
                }
                else
                {
                    offenceCoeff = 1f;
                }

                if (defenceCoeffDown > Core.Epsilon)
                {
                    deffenceCoeff = defenceCoeffUp / defenceCoeffDown;
                }
                else
                {
                    deffenceCoeff = 1f;
                }
            }
        }

        public static void NoSensorsCalc(this AbstractActor unit, ref int inRange, ref int noSensors)
        {
            inRange = 0;
            noSensors = 0;
            string modId = Core.Settings.AINoSensorsModifierId;
            ToHitModifierNode node = null;
            ToHitNodeModifier modifier = null;
            if (!string.IsNullOrEmpty(modId))
            {
                foreach (ToHitModifierNode n in ToHitModifiersHelper.mod_nodes.Values)
                {
                    if (n.modifiers.TryGetValue(modId, out modifier))
                    {
                        node = n;
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(modId) && node == null)
            {
                Log.Debug?.Write(" no to-hit modifier " + modId + " registered\n");
            }

            foreach (AbstractActor target in unit.GetVisibleEnemyUnits())
            {
                Weapon inRangeWeapon = null;
                foreach (Weapon weapon in unit.Weapons)
                {
                    if (!weapon.CanFire)
                    {
                        continue;
                    }

                    if (weapon.WillFireAtTargetFromPosition(target, unit.CurrentPosition))
                    {
                        inRangeWeapon = weapon;
                        break;
                    }
                }

                if (inRangeWeapon == null)
                {
                    continue;
                }

                inRange += 1;
                if (node == null)
                {
                    continue;
                }

                try
                {
                    LineOfFireLevel lof = unit.Combat.LOS.GetLineOfFire(unit, unit.CurrentPosition, target, target.CurrentPosition, target.CurrentRotation, out Vector3 collision);
                    object state = node.prepare(unit.Combat.ToHit, unit, inRangeWeapon, target, unit.CurrentPosition, target.CurrentPosition, lof, MeleeAttackType.NotSet, false);
                    float mod = modifier.modifier(state, unit.Combat.ToHit, unit, inRangeWeapon, target, unit.CurrentPosition, target.CurrentPosition, lof, MeleeAttackType.NotSet, false);
                    if (mod > Core.Epsilon)
                    {
                        noSensors += 1;
                    }
                }
                catch (Exception e)
                {
                    Log.Debug?.Write(" no sensors check failed: " + e.Message + "\n");
                }
            }
        }

        public static bool ChargesBlockedBySpawnProtection(this AbstractActor unit)
        {
            if (unit.Combat.TurnDirector.CurrentRound <= 1)
            {
                return true;
            }

            if (unit.isSpawnProtected())
            {
                return true;
            }

            List<AbstractActor> visible = unit.GetVisibleEnemyUnits();
            if (visible.Count > 0)
            {
                foreach (AbstractActor enemy in visible)
                {
                    if (enemy.IsDead)
                    {
                        continue;
                    }

                    if (!enemy.isSpawnProtected())
                    {
                        return false;
                    }
                }

                return true;
            }

            foreach (AbstractActor enemy in unit.Combat.GetAllEnemiesOf(unit))
            {
                if (enemy.IsDead)
                {
                    continue;
                }

                if (enemy.isSpawnProtected())
                {
                    return true;
                }
            }

            return false;
        }

        public static int EcmJammed(this AbstractActor unit)
        {
            if (!unit.StatCollection.ContainsStatistic(Core.Settings.AIEcmJammedStat))
            {
                return 0;
            }

            return unit.StatCollection.GetValue<int>(Core.Settings.AIEcmJammedStat);
        }

        private static bool isAIUnit(this AbstractActor actor)
        {
            return actor.TeamId != actor.Combat.LocalPlayerTeamGuid;
        }

        private static float DistanceTo(this AbstractActor unit, AbstractActor other)
        {
            return Vector3.Distance(unit.CurrentPosition, other.CurrentPosition);
        }

        private static bool IsWithin(this AbstractActor unit, AbstractActor other, float range)
        {
            return unit.DistanceTo(other) <= range;
        }

        private static bool CanReachThisTurn(this AbstractActor enemy, AbstractActor unit)
        {
            return enemy.IsWithin(unit, enemy.MaxSprintDistance);
        }

        private static bool Reaches(this Weapon weapon, float distance)
        {
            return weapon.IsFunctional && distance <= weapon.MaxRange;
        }

        public static bool AnyEnemyInWeaponRange(this AbstractActor unit)
        {
            foreach (AbstractActor enemy in unit.GetVisibleEnemyUnits())
            {
                if (enemy.IsDead)
                {
                    continue;
                }

                float distance = unit.DistanceTo(enemy);
                foreach (Weapon weapon in unit.Weapons.Where(weapon => weapon.Reaches(distance)))
                {
                    Log.Debug?.Write("  in weapon range: " + weapon.defId + " (" + weapon.DamageLevel + ", max range " + weapon.MaxRange + ") to " + enemy.DisplayName + " at " + distance + "\n");
                    return true;
                }
            }

            return false;
        }

        public static AbstractActor EnemyInReach(this AbstractActor unit)
        {
            foreach (AbstractActor enemy in unit.GetVisibleEnemyUnits())
            {
                if (enemy.IsDead)
                {
                    continue;
                }

                if (enemy.CanReachThisTurn(unit))
                {
                    return enemy;
                }
            }

            return null;
        }
    }
}
