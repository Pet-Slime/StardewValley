using System;
using System.Collections.Generic;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    // This class defines a "LuckStealSpell" that increases the daily luck for the player but reduces friendship with a random NPC
    public class LuckStealSpell : Spell
    {
        /*********
        ** Public methods
        *********/

        public LuckStealSpell()
            : base(SchoolId.Eldritch, "lucksteal")
        {
            // SchoolId.Eldritch identifies the spell's magical school
            // "lucksteal" is the internal name for this spell
        }

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
            // Can cast only if the shared daily luck is not already set to maximum (0.12)
            return base.CanCast(player, level) && player.team.sharedDailyLuck.Value != 0.12;
        }

        // Called when the spell is cast
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            // Only run for the local player
            if (!player.IsLocalPlayer)
                return null;

            // Pick a random NPC from the player's friendship data
            int num = Game1.random.Next(player.friendshipData.Count());
            var friendshipData = player.friendshipData[new List<string>(player.friendshipData.Keys)[num]];

            // Reduce friendship points with that NPC by 250, but not below 0
            friendshipData.Points = Math.Max(0, friendshipData.Points - 250);

            // Set the player's daily luck to maximum
            player.team.sharedDailyLuck.Value = 0.12;

            // Return a successful spell effect with a sound and grant exp
            return new SpellSuccess(player, "death", 50);
        }
    }
}
