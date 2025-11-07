using System.Collections.Generic;
using MoonShared.Attributes;
using Microsoft.Xna.Framework;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    // This class defines a "CharmSpell" that increases friendship with nearby NPCs
    public class CharmSpell : Spell
    {
        /*********
        ** Public methods
        *********/

        public CharmSpell()
            : base(SchoolId.Eldritch, "charm")
        {
            // SchoolId.Eldritch identifies the spell's magical school
            // "charm" is the internal name used to reference this spell
        }

        public override int GetManaCost(Farmer player, int level)
        {
            return 15;
        }

        public override bool CanCast(Farmer player, int level)
        {
            return base.CanCast(player, level) && player.health > 25;
        }

        // Called when the spell is cast
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            // Only run this for the local player
            if (!player.IsLocalPlayer)
                return null;

            // Get all characters (NPCs and monsters) in the current location
            var mobs = player.currentLocation.characters;

            // Calculate the effective range of the spell based on its level
            int levelAmount = level * 2 + 8;

            GameLocation location = player.currentLocation;
            Vector2 playerTile = player.Tile;

            // Lists to store NPCs and monsters that are affected by the spell
            List<NPC> npcsInRange = new List<NPC>();
            List<NPC> monstersInRange = new List<NPC>();

            // Go through all characters in the location
            foreach (var NPC in location.characters)
            {
                float Distance = Vector2.Distance(NPC.Tile, playerTile); // distance from player
                float profession = levelAmount; // max effective distance

                // Debug logging to trace spell logic (can be ignored by beginners)
                Log.Trace("Wizardry Charm Spell: going to go through the list...");
                Log.Trace("NPC name is: " + NPC.Name);
                Log.Trace("is NPC villager?: " + NPC.IsVillager.ToString());
                Log.Trace("NPC Distance: " + Distance.ToString());
                Log.Trace("distance value: " + profession.ToString());
                Log.Trace("distance check: " + (Distance <= profession).ToString());

                // Only affect NPCs that meet all these criteria:
                // 1. Is a villager
                // 2. Is within the spell's range
                // 3. Can receive gifts
                // 4. Player has friendship data with them
                if (NPC.IsVillager &&
                    Distance <= profession &&
                    NPC.CanReceiveGifts() &&
                    player.friendshipData.ContainsKey(NPC.Name))
                {
                    npcsInRange.Add(NPC); // add to affected list
                    continue;
                }
            }

            // If no NPCs are in range, the spell fizzles (fails)
            if (npcsInRange.Count == 0)
                return new SpellFizzle(player, this.GetManaCost(player, level));

            int num = 0; // counter to track first NPC (used for mana cost logic)

            // Apply the charm effect to each NPC in range
            foreach (var NPC in npcsInRange)
            {
                // Stop casting if the player runs out of mana or health
                if (!this.CanContinueCast(player, level))
                    return null;

                if (player.health <= 24)
                    return null;

                // Reduce mana for additional NPCs (first one is free)
                if (num != 0)
                {
                    player.AddMana(-this.GetManaCost(player, level));
                }

                // Increase friendship points
                player.changeFriendship(20 * (level + 1), NPC);

                // Player loses some health as a cost of casting
                player.takeDamage(25, false, null);

                // Play sound effect for the NPC
                NPC.currentLocation.playSound("jingle1", NPC.Tile);

                // Award experience points to the player
                Utilities.AddEXP(player, 25);

                // Make NPC do a "blush" emote
                NPC.doEmote(Character.blushEmote);

                // Calculate a position above the NPC to display particle effects
                var point = NPC.StandingPixel;
                point.X -= NPC.Sprite.SpriteWidth * 2;
                point.Y -= (int)(NPC.Sprite.SpriteHeight * 1.5);

                // Show cyan animated sprite above NPC (first layer)
                Game1.Multiplayer.broadcastSprites(player.currentLocation,
                    new TemporaryAnimatedSprite(10,
                    point.ToVector2(),
                    Color.Cyan,
                    10,
                    Game1.random.NextDouble() < 0.5,
                    70f,
                    0,
                    Game1.tileSize,
                    100f));

                // Show another cyan animated sprite slightly higher (second layer)
                point.Y -= (int)(NPC.Sprite.SpriteHeight * 2.5);
                Game1.Multiplayer.broadcastSprites(player.currentLocation,
                    new TemporaryAnimatedSprite(10,
                    point.ToVector2(),
                    Color.Cyan,
                    10,
                    Game1.random.NextDouble() < 0.5,
                    70f,
                    0,
                    Game1.tileSize,
                    100f));

                num++; // increment counter
            }

            // Spell successfully cast, play a sound and reward proportional to affected NPCs
            return new SpellSuccess(player, "clam_tone", npcsInRange.Count * 25);
        }
    }
}
