using System;
using System.Linq;
using System.Reflection.Metadata;
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
            if (player == Game1.player)
            {
                return !player.buffs.AppliedBuffs.Values.Any(u => u.source == "spell:life:buff");
            }

            return base.CanCast(player, level);
        }

        public override int GetManaCost(Farmer player, int level)
        {
            return 25;
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player != Game1.player)
                return null;

            if (player.buffs.AppliedBuffs.Values.Any(u => u.source == "spell:life:buff"))
                return null;

            int l = level + 1;


            var baseSkillBuff = new Buff(
                id: "spacechase0.magic.buff",
                source: "spell:life:buff",
                displaySource: ModEntry.Instance.I18N.Get("moonslime.Wizardry.buff.buffDescription"),
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
