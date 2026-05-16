using System;
using System.Collections.Generic;
using StardewModdingAPI;
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

        public override SpellSyncMode SyncMode => SpellSyncMode.HostWorld;

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
            return base.CanCast(player, level) && player.team.sharedDailyLuck.Value != 0.12 && player.friendshipData.Count() > 0;
        }

        public override string BuildExtraData(Farmer caster, int level, int targetX, int targetY)
        {
            if (caster == null || caster.friendshipData.Count() == 0)
                return "";

            // Pick a random NPC from the player's friendship data
            int num = Game1.random.Next(caster.friendshipData.Count());
            return new List<string>(caster.friendshipData.Keys)[num];
        }

        // Called when the spell is cast
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            string extraData = this.BuildExtraData(player, level, targetX, targetY);
            return this.OnReceiveCast(player, level, targetX, targetY, extraData);
        }

        // Called when the spell is received through the spell sync system
        public override IActiveEffect OnReceiveCast(Farmer caster, int level, int targetX, int targetY, string extraData)
        {
            if (caster == null)
                return null;

            // Only the actual casting player should lose friendship.
            if (caster.IsLocalPlayer)
            {
                string npcName = extraData;

                // Fallback for direct casts or old/empty synced data
                if (string.IsNullOrWhiteSpace(npcName))
                {
                    if (caster.friendshipData.Count() == 0)
                        return new SpellFizzle(caster, this.GetManaCost(caster, level));

                    int num = Game1.random.Next(caster.friendshipData.Count());
                    npcName = new List<string>(caster.friendshipData.Keys)[num];
                }

                if (!caster.friendshipData.TryGetValue(npcName, out var friendshipData))
                    return new SpellFizzle(caster, this.GetManaCost(caster, level));

                // Reduce friendship points with that NPC by 250, but not below 0
                friendshipData.Points = Math.Max(0, friendshipData.Points - 250);
            }

            // Only the host should mutate shared team luck state.
            if (Context.IsMainPlayer)
            {
                // Set the player's daily luck to maximum
                caster.team.sharedDailyLuck.Value = 0.12;
            }

            // Return a successful spell effect with a sound and grant exp
            return caster.IsLocalPlayer
                ? new SpellSuccess(caster, "death", 50)
                : null;
        }
    }
}
