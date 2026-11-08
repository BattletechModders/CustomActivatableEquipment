using BattleTech;
using CustAmmoCategories;
using CustomAmmoCategoriesPatches;
using IRBTModUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CustomActivatableEquipment.AI
{
    internal class AISituation
    {
        private readonly AbstractActor Unit;
        public readonly Mech Mech;
        public readonly bool ActivationEnd;
        public readonly int VisibleTargets;
        public readonly float OffenceValue;
        public readonly float DefenceValue;
        public readonly int TargetsInRange;
        public readonly int TargetsWithoutSensors;
        public readonly float HeatRatio;
        public readonly float OverheatRatio;
        public readonly bool IsExposed;
        public readonly bool IsHot;
        public readonly bool ChargesBlocked;
        public readonly bool IsSelfJammed;
        private bool? enemyInWeaponRange;

        public AISituation(AbstractActor unit, bool activationEnd, List<AIComponent> components)
        {
            Unit = unit;
            Mech = unit as Mech;
            ActivationEnd = activationEnd;
            if (components.Any(component => component.Info.NeedsThreats))
            {
                CountThreats(out VisibleTargets, out OffenceValue, out DefenceValue);
            }

            if (components.Any(component => component.Info.NeedsTargetsInRange))
            {
                CountTargetsInRange(out TargetsInRange, out TargetsWithoutSensors);
            }

            if (Mech != null)
            {
                HeatRatio = Mech.OverheatLevel > 0 ? (float)Mech.CurrentHeat / Mech.OverheatLevel : 0f;
                OverheatRatio = Mech.CurrentHeatAsRatio;
            }

            IsExposed = unit.IsAnyStructureExposed;
            IsHot = HeatRatio > Core.Settings.AIHeatCoeff || OverheatRatio > Core.Settings.AIOverheatCoeff;
            ChargesBlocked = components.Any(component => component.Info.NeedsSpawnCheck) && IsSpawnProtectionActive();
            IsSelfJammed = IsEcmJammed(unit);
            LogSummary();
        }

        public bool EnemyInWeaponRange
        {
            get
            {
                if (enemyInWeaponRange == null)
                {
                    enemyInWeaponRange = FindEnemyInWeaponRange();
                }

                return enemyInWeaponRange.Value;
            }
        }

        public bool HasHeat => Unit.isHasHeat() && !(Unit is ICustomMech custMech && custMech.isSquad);

        public AbstractActor EnemyThatCanReachUs()
        {
            return LivingVisibleEnemies().FirstOrDefault(enemy => IsWithin(enemy, enemy.MaxSprintDistance));
        }

        public int EnemiesWithin(float range)
        {
            return LivingVisibleEnemies().Count(enemy => IsWithin(enemy, range));
        }

        public int JammedAlliesWithin(float range)
        {
            return Unit.Combat.GetAllAlliesOf(Unit).Where(ally => !ally.IsDead && ally != Unit && IsWithin(ally, range)).Count(IsEcmJammed);
        }

        private void LogSummary()
        {
            Log.Debug?.Write("AI activatable coeffs:" + Unit.DisplayName + ":" + Unit.GUID + (ActivationEnd ? " (activation end)" : " (round start)") + "\n");
            Log.Debug?.Write(" visible targets:" + VisibleTargets + "\n");
            Log.Debug?.Write(" offence value:" + OffenceValue + "\n");
            Log.Debug?.Write(" defence value:" + DefenceValue + "\n");
            Log.Debug?.Write(" heat ratio:" + HeatRatio + "\n");
            Log.Debug?.Write(" overheat ratio:" + OverheatRatio + "\n");
            Log.Debug?.Write(" exposed:" + IsExposed + "\n");
            Log.Debug?.Write(" targets in range:" + TargetsInRange + " without sensors:" + TargetsWithoutSensors + "\n");
            Log.Debug?.Write(" charges blocked by spawn protection:" + ChargesBlocked + "\n");
        }

        private void CountThreats(out int visibleTargets, out float offence, out float defence)
        {
            visibleTargets = 0;
            float damageDealt = 0f;
            float enemyArmor = 0f;
            float damageTaken = 0f;
            foreach (AbstractActor target in Unit.GetVisibleEnemyUnits())
            {
                visibleTargets += 1;
                damageDealt += ExpectedDamage(Unit, target);
                damageTaken += ExpectedDamage(target, Unit);
                enemyArmor += target.SummaryArmorCurrent;
            }

            if (visibleTargets == 0)
            {
                offence = 0f;
                defence = 0f;
                return;
            }

            float ownArmor = Unit.SummaryArmorCurrent;
            offence = enemyArmor > Core.Epsilon ? damageDealt / enemyArmor : 1f;
            defence = ownArmor > Core.Epsilon ? damageTaken / ownArmor : 1f;
        }

        private static float ExpectedDamage(AbstractActor attacker, AbstractActor target)
        {
            float damage = 0f;
            foreach (Weapon weapon in attacker.Weapons)
            {
                if (!weapon.CanFire || !weapon.WillFireAtTargetFromPosition(target, attacker.CurrentPosition))
                {
                    continue;
                }

                float toHit = weapon.GetToHitFromPosition(target, 1, attacker.CurrentPosition, target.CurrentPosition, true, target.IsEvasive, false);
                if (toHit >= Core.Epsilon)
                {
                    damage += toHit * weapon.DamagePerShot * weapon.ShotsWhenFired;
                }
            }

            return damage;
        }

        private void CountTargetsInRange(out int inRange, out int withoutSensors)
        {
            inRange = 0;
            withoutSensors = 0;
            ToHitModifierNode node = FindNoSensorsModifier(out ToHitNodeModifier modifier);
            foreach (AbstractActor target in Unit.GetVisibleEnemyUnits())
            {
                Weapon weapon = Unit.Weapons.FirstOrDefault(w => w.CanFire && w.WillFireAtTargetFromPosition(target, Unit.CurrentPosition));
                if (weapon == null)
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
                    LineOfFireLevel lof = Unit.Combat.LOS.GetLineOfFire(Unit, Unit.CurrentPosition, target, target.CurrentPosition, target.CurrentRotation, out Vector3 _);
                    object state = node.prepare(Unit.Combat.ToHit, Unit, weapon, target, Unit.CurrentPosition, target.CurrentPosition, lof, MeleeAttackType.NotSet, false);
                    float mod = modifier.modifier(state, Unit.Combat.ToHit, Unit, weapon, target, Unit.CurrentPosition, target.CurrentPosition, lof, MeleeAttackType.NotSet, false);
                    if (mod > Core.Epsilon)
                    {
                        withoutSensors += 1;
                    }
                }
                catch (Exception e)
                {
                    Log.Debug?.Write(" no sensors check failed: " + e.Message + "\n");
                }
            }
        }

        private static ToHitModifierNode FindNoSensorsModifier(out ToHitNodeModifier modifier)
        {
            modifier = null;
            string modifierId = Core.Settings.AINoSensorsModifierId;
            if (string.IsNullOrEmpty(modifierId))
            {
                return null;
            }

            foreach (ToHitModifierNode node in ToHitModifiersHelper.mod_nodes.Values)
            {
                if (node.modifiers.TryGetValue(modifierId, out modifier))
                {
                    return node;
                }
            }

            Log.Debug?.Write(" no to-hit modifier " + modifierId + " registered\n");
            return null;
        }

        private bool IsSpawnProtectionActive()
        {
            if (Unit.Combat.TurnDirector.CurrentRound <= 1 || Unit.isSpawnProtected())
            {
                return true;
            }

            List<AbstractActor> visible = Unit.GetVisibleEnemyUnits();
            if (visible.Count > 0)
            {
                return visible.All(enemy => enemy.IsDead || enemy.isSpawnProtected());
            }

            return Unit.Combat.GetAllEnemiesOf(Unit).Any(enemy => !enemy.IsDead && enemy.isSpawnProtected());
        }

        private static bool IsEcmJammed(AbstractActor actor)
        {
            return actor.StatCollection.ContainsStatistic(Core.Settings.AIEcmJammedStat) && actor.StatCollection.GetValue<int>(Core.Settings.AIEcmJammedStat) > 0;
        }

        private IEnumerable<AbstractActor> LivingVisibleEnemies()
        {
            return Unit.GetVisibleEnemyUnits().Where(enemy => !enemy.IsDead);
        }

        private bool IsWithin(AbstractActor other, float range)
        {
            return Vector3.Distance(Unit.CurrentPosition, other.CurrentPosition) <= range;
        }

        private bool FindEnemyInWeaponRange()
        {
            foreach (AbstractActor enemy in LivingVisibleEnemies())
            {
                float distance = Vector3.Distance(Unit.CurrentPosition, enemy.CurrentPosition);
                Weapon weapon = Unit.Weapons.FirstOrDefault(w => w.IsFunctional && distance <= w.MaxRange);
                if (weapon != null)
                {
                    Log.Debug?.Write("  in weapon range: " + weapon.defId + " (" + weapon.DamageLevel + ", max range " + weapon.MaxRange + ") to " + enemy.DisplayName + " at " + distance + "\n");
                    return true;
                }
            }

            return false;
        }
    }
}
