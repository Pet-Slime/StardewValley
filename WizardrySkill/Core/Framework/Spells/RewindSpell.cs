using System;
using MoonShared.Attributes;
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

        // Constructor: sets the spell's school and ID
        public RewindSpell()
            : base(SchoolId.Arcane, "rewind") { }

        // Limits the maximum level this spell can be cast at
        public override int GetMaxCastingLevel()
        {
            return 1;
        }

        // Determines whether the spell can currently be cast
        public override bool CanCast(Farmer player, int level)
        {
            // Conditions:
            // 1. Base checks for mana
            // 2. Player must have at least 1 item with ID "336" (a gold bar)
            // 3. Time must be later than 6:00 AM (600 in-game time)
            return base.CanCast(player, level) && player.Items.ContainsId("336", 1) && Game1.timeOfDay != 600;
        }

        // Returns the mana cost for casting this spell
        public override int GetManaCost(Farmer player, int level)
        {
            return 25; 
        }

        // Called when the spell is cast
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            // Only execute for the local player (to avoid duplicating effects in multiplayer)
            if (!player.IsLocalPlayer)
                return null;

            // Consume one ticket item
            player.Items.ReduceId("336", 1);

            // Determine the starting point for the visual effects
            var point = player.StandingPixel;

            // Adjust the point to appear above the player sprite
            point.X -= player.Sprite.SpriteWidth * 2;
            point.Y -= (int)(player.Sprite.SpriteHeight * 1.5);

            // Create a yellow particle effect at the first position
            Game1.Multiplayer.broadcastSprites(player.currentLocation,
                new TemporaryAnimatedSprite(10,
                point.ToVector2(),
                Color.Yellow,
                10,
                Game1.random.NextDouble() < 0.5, // random flipping of the sprite
                70f, // animation interval
                0,
                Game1.tileSize,
                100f));

            // Move the effect higher for a second particle burst
            point.Y -= (int)(player.Sprite.SpriteHeight * 2.5);

            Game1.Multiplayer.broadcastSprites(player.currentLocation,
                new TemporaryAnimatedSprite(10,
                point.ToVector2(),
                Color.Yellow,
                10,
                Game1.random.NextDouble() < 0.5,
                70f,
                0,
                Game1.tileSize,
                100f));

            // Rewind the in-game time by 2 hours (200 in-game units)
            // Ensures the time does not go below 6:00 AM
            Game1.timeOfDay = Math.Max(600, Game1.timeOfDay - 200);

            // Return a successful spell effect with a sound and grant exp
            return new SpellSuccess(player, "ticket_machine_whir", 25);
        }
    }
}
