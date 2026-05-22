using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    // This class defines the "Rewind" spell, which rewinds the in-game time by a portion of the day.
    public class RewindSpell : Spell
    {
        /*********
        ** Public methods
        *********/

        // Constructor: sets the spell's school and ID.
        public RewindSpell()
            : base(SchoolId.Arcane, "rewind") { }

        public override SpellSyncMode SyncMode => SpellSyncMode.LocalWorld;

        // Limits the maximum level this spell can be cast at.
        public override int GetMaxCastingLevel()
        {
            return 1;
        }

        // Returns the mana cost for casting this spell.
        public override int GetManaCost(Farmer player, int level)
        {
            return 25;
        }

        // Returns the item cost for casting this spell.
        public override IDictionary<string, int> GetItemCost(Farmer player, int level)
        {
            return new Dictionary<string, int>
            {
                ["336"] = 1
            };
        }

        // Determines whether the spell can currently be cast.
        public override bool CanCast(Farmer player, int level)
        {
            // Any player can cast this. Stardew's normal time sync handles the shared time update.
            return base.CanCast(player, level)
                && Game1.timeOfDay != 600;
        }

        // Called when the spell is cast.
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player == null || player.currentLocation == null)
                return null;

            // Only the caster's own machine should consume the reagent, mutate time, and broadcast visuals.
            // Remote machines observe the cast packet but do not replay this effect.
            if (!player.IsLocalPlayer)
                return null;

            // If time somehow reached 6:00 AM before the spell executed, fail safely.
            if (Game1.timeOfDay == 600)
                return new SpellFizzle(player, this.GetManaCost(player, level));

            // Consume the item unless this cast came from a scroll.
            if (!this.ConsumeItemCost(player, level))
                return new SpellFizzle(player, this.GetManaCost(player, level));

            this.BroadcastRewindVisuals(player);

            // Rewind the in-game time by 2 hours, without going below 6:00 AM.
            // Stardew handles syncing time to the other clients.
            Game1.timeOfDay = Math.Max(600, Game1.timeOfDay - 200);

            // Return a successful spell effect with a sound and grant exp.
            return new SpellSuccess(player, "ticket_machine_whir", 25);
        }


        /*********
        ** Private helpers
        *********/

        private void BroadcastRewindVisuals(Farmer player)
        {
            // Determine the starting point for the visual effects.
            Point point = player.StandingPixel;

            // Adjust the point to appear above the player sprite.
            point.X -= player.Sprite.SpriteWidth * 2;
            point.Y -= (int)(player.Sprite.SpriteHeight * 1.5);

            // Create a yellow particle effect at the first position.
            Game1.Multiplayer.broadcastSprites(player.currentLocation,
                new TemporaryAnimatedSprite(
                    10,
                    point.ToVector2(),
                    Color.Yellow,
                    10,
                    Game1.random.NextDouble() < 0.5,
                    70f,
                    0,
                    Game1.tileSize,
                    100f));

            // Move the effect higher for a second particle burst.
            point.Y -= (int)(player.Sprite.SpriteHeight * 2.5);

            Game1.Multiplayer.broadcastSprites(player.currentLocation,
                new TemporaryAnimatedSprite(
                    10,
                    point.ToVector2(),
                    Color.Yellow,
                    10,
                    Game1.random.NextDouble() < 0.5,
                    70f,
                    0,
                    Game1.tileSize,
                    100f));
        }
    }
}
