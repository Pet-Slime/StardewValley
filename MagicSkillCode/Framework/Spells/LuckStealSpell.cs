using System;
using System.Collections.Generic;
using MagicSkillCode.Framework.Schools;
using SpaceCore;
using StardewValley;
using MagicSkillCode.Core;

namespace MagicSkillCode.Framework.Spells
{
    public class LuckStealSpell : Spell
    {
        /*********
        ** Public methods
        *********/
        public LuckStealSpell()
            : base(SchoolId.Eldritch, "lucksteal") { }

        public override int GetManaCost(Farmer player, int level)
        {
            return 0;
        }

        public override int GetMaxCastingLevel()
        {
            return 1;
        }

        public override bool CanCast(Farmer player, int level)
        {
            return base.CanCast(player, level) && player.team.sharedDailyLuck.Value != 0.12;
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            int num = Game1.random.Next(player.friendshipData.Count());
            var friendshipData = player.friendshipData[new List<string>(player.friendshipData.Keys)[num]];
            friendshipData.Points = Math.Max(0, friendshipData.Points - 250);
            player.team.sharedDailyLuck.Value = 0.12;
            player.LocalSound("death");
            Utilities.AddEXP(player, 50);

            return null;
        }
    }
}
