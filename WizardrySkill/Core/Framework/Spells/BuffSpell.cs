using System;
using System.Linq;
using SpaceCore;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;

namespace WizardrySkill.Core.Framework.Spells
{
    public class BuffSpell : Spell
    {
        /*********
        ** Public methods
        *********/
        public BuffSpell()
            : base(SchoolId.Life, "buff") { }

        public override bool CanCast(Farmer player, int level)
        {
            return base.CanCast(player, level) && !player.hasBuff($"spell:life:buff:{level}");
        }

        public override int GetManaCost(Farmer player, int level)
        {
            return 25 * (level + 1);
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player != Game1.player)
                return null;

            if (player.hasBuff($"spell:life:buff:{level}"))
                return null;

            int l = level + 1;


            var baseSkillBuff = new Buff(
                id: $"spell:life:buff:{level}",
                source: $"spell:life:buff:{level}",
                displaySource: ModEntry.Instance.I18N.Get("moonslime.Wizardry.buff.buffDescription") + level.ToString(),
                duration: (int)TimeSpan.FromSeconds(60 + level * 120).TotalMilliseconds,
                effects: new StardewValley.Buffs.BuffEffects
                {
                    FarmingLevel = { l },
                    FishingLevel = { l },
                    MiningLevel = { l },
                    CombatLevel = { l },
                    ForagingLevel = { l }
                }
                );

            foreach (string customSkills in Skills.GetSkillList())
            {
                baseSkillBuff.customFields.Add($"spacechase.SpaceCore.SkillBuff.{customSkills}", $"{l}");
            }
            player.buffs.Apply(baseSkillBuff);

            player.currentLocation.playSound("powerup", player.Tile);
            Utilities.AddEXP(player, 10);
            return null;
        }
    }
}
