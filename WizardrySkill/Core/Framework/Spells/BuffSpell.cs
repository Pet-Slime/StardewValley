using System;
using System.Linq;
using SpaceCore;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    // This class defines a "BuffSpell" that temporarily increases multiple player skills
    public class BuffSpell : Spell
    {
        /*********
        ** Public methods
        *********/

        public BuffSpell()
            : base(SchoolId.Life, "buff")
        {
            // SchoolId.Life identifies the spell's magical school
            // "buff" is the internal name for this spell
        }

        public override bool CanCast(Farmer player, int level)
        {
            return base.CanCast(player, level) && !player.hasBuff($"spell:life:buff:{level}");
        }

        public override int GetManaCost(Farmer player, int level)
        {
            return 25 * (level + 1);
        }

        // Called when the spell is cast
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            // Only run this for the local player
            if (!player.IsLocalPlayer)
                return null;

            // If the player already has this buff, the spell fails and costs mana
            if (player.hasBuff($"spell:life:buff:{level}"))
                return new SpellFizzle(player, this.GetManaCost(player, level));

            // Adjust level for calculations (level + 1)
            int l = level + 1;

            // Create a new buff that affects multiple skills
            var baseSkillBuff = new Buff(
                id: $"spell:life:buff:{level}", // unique identifier for the buff
                source: $"spell:life:buff:{level}", // origin of the buff
                displaySource: ModEntry.Instance.I18N.Get("moonslime.Wizardry.buff.buffDescription") + level.ToString(),
                duration: (int)TimeSpan.FromSeconds(60 + level * 120).TotalMilliseconds, // how long the buff lasts (in ms)
                effects: new StardewValley.Buffs.BuffEffects
                {
                    // Boosts core skills by "l" (level + 1)
                    FarmingLevel = { l },
                    FishingLevel = { l },
                    MiningLevel = { l },
                    CombatLevel = { l },
                    ForagingLevel = { l }
                }
            );

            // Add custom skill buffs for mods or extended skills
            foreach (string customSkills in Skills.GetSkillList())
            {
                baseSkillBuff.customFields.Add($"spacechase.SpaceCore.SkillBuff.{customSkills}", $"{l}");
            }

            // Apply the buff to the player
            player.buffs.Apply(baseSkillBuff);

            // Return a success effect with sound feedback
            return new SpellSuccess(player, "powerup", 10);
        }
    }
}
