using System;
using System.Linq;
using MagicSkillCode.Framework.Schools;
using SpaceCore;
using MagicSkillCode.Core;
using StardewValley;

namespace MagicSkillCode.Framework.Spells
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
            int farm = l, fish = l, mine = l, luck = l, forage = l, def = 0 /*1*/, atk = 2;
            atk = l switch
            {
                2 => 5,
                3 => 10,
                _ => atk
            };

            player.buffs.Apply(new Buff(
                id: "spacechase0.magic.buff",
                source: "spell:life:buff",
                displaySource: "Buff (spell)",
                duration: (int)TimeSpan.FromSeconds(60 + level * 120).TotalMilliseconds,
                effects: new StardewValley.Buffs.BuffEffects
                {
                    FarmingLevel = { farm },
                    FishingLevel = { fish },
                    MiningLevel = { mine },
                    LuckLevel = { luck },
                    ForagingLevel = { forage },
                    Defense = { def },
                    Attack = { atk },
                }
            ));

            Utilities.AddEXP(player, 10);
            return null;
        }
    }
}
