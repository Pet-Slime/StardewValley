using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    // This class defines a "LuckStealSpell" that increases the daily luck for the player but reduces friendship with a random NPC.
    public class LuckStealSpell : Spell
    {
        /*********
        ** Public methods
        *********/
        public LuckStealSpell()
            : base(SchoolId.Eldritch, "lucksteal")
        {
            // SchoolId.Eldritch identifies the spell's magical school.
            // "lucksteal" is the internal name for this spell.
        }

        public override SpellSyncMode SyncMode => SpellSyncMode.LocalWorld;

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
            // Can cast only if shared daily luck is not already set to maximum.
            return base.CanCast(player, level) && player.team.sharedDailyLuck.Value < 0.12;
        }

        // Called when the spell is cast.
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player == null)
                return null;

            // Only the caster's own machine should mutate friendship and shared daily luck.
            if (!player.IsLocalPlayer)
                return null;

            if (player.friendshipData.Count() == 0)
                return new SpellFizzle(player, this.GetManaCost(player, level));

            // Pick a random NPC from the player's friendship data.
            int index = Game1.random.Next(player.friendshipData.Count());
            string npcName = new List<string>(player.friendshipData.Keys)[index];
            var friendshipData = player.friendshipData[npcName];

            // Reduce friendship points with that NPC by 250, but not below 0.
            friendshipData.Points = Math.Max(0, friendshipData.Points - 250);

            // Set the player's daily luck to maximum.
            player.team.sharedDailyLuck.Value = 0.12;

            // Return a successful spell effect with a sound and grant exp.
            return new SpellSuccess(player, "death", 50);
        }
    }
}
