using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    // This class defines a "CharmSpell" that increases friendship with nearby NPCs.
    public class CharmSpell : Spell
    {
        /*********
        ** Public methods
        *********/
        public CharmSpell()
            : base(SchoolId.Eldritch, "charm")
        {
            // SchoolId.Eldritch identifies the spell's magical school.
            // "charm" is the internal name used to reference this spell.
        }

        public override SpellSyncMode SyncMode => SpellSyncMode.LocalWorld;

        public override int GetManaCost(Farmer player, int level)
        {
            return 15;
        }

        public override bool CanCast(Farmer player, int level)
        {
            return base.CanCast(player, level) && player.health > 25;
        }

        // Called when the spell is cast.
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player == null || player.currentLocation == null)
                return null;

            // Only the caster's own machine should mutate friendship, health, mana, EXP, and NPC visuals.
            if (!player.IsLocalPlayer)
                return null;

            GameLocation location = player.currentLocation;
            Vector2 playerTile = player.Tile;
            int range = level * 2 + 8;

            List<NPC> npcsInRange = new();

            // Go through all characters in the location.
            foreach (NPC npc in location.characters)
            {
                float distance = Vector2.Distance(npc.Tile, playerTile);

                // Only affect NPCs that meet all these criteria:
                // 1. Is a villager.
                // 2. Is within the spell's range.
                // 3. Can receive gifts.
                // 4. Player has friendship data with them.
                if (npc.IsVillager &&
                    distance <= range &&
                    npc.CanReceiveGifts() &&
                    player.friendshipData.ContainsKey(npc.Name))
                {
                    npcsInRange.Add(npc);
                }
            }

            // If no NPCs are in range, the spell fizzles.
            if (npcsInRange.Count == 0)
                return new SpellFizzle(player, this.GetManaCost(player, level));

            int affectedCount = 0;

            // Apply the charm effect to each NPC in range.
            foreach (NPC npc in npcsInRange)
            {
                // Stop casting if the player runs out of mana or health.
                if (!this.CanContinueCast(player, level) || player.health <= 24)
                    break;

                // Reduce mana for additional NPCs. The first NPC is covered by the initial spell cost.
                if (affectedCount != 0)
                    player.AddMana(-this.GetManaCost(player, level));

                // Increase friendship points.
                player.changeFriendship(20 * (level + 1), npc);

                // Player loses some health as a cost of casting.
                player.takeDamage(25, false, null);

                // Play sound effect for the NPC.
                npc.currentLocation.playSound("jingle1", npc.Tile);

                // Award experience points to the player.
                Utilities.AddEXP(player, 25);

                // Make NPC do a "blush" emote.
                npc.doEmote(Character.blushEmote);

                // Calculate a position above the NPC to display particle effects.
                Point point = npc.StandingPixel;
                point.X -= npc.Sprite.SpriteWidth * 2;
                point.Y -= (int)(npc.Sprite.SpriteHeight * 1.5);

                // Show cyan animated sprite above NPC.
                Game1.Multiplayer.broadcastSprites(location,
                    new TemporaryAnimatedSprite(
                        10,
                        point.ToVector2(),
                        Color.Cyan,
                        10,
                        Game1.random.NextDouble() < 0.5,
                        70f,
                        0,
                        Game1.tileSize,
                        100f));

                // Show another cyan animated sprite slightly higher.
                point.Y -= (int)(npc.Sprite.SpriteHeight * 2.5);
                Game1.Multiplayer.broadcastSprites(location,
                    new TemporaryAnimatedSprite(
                        10,
                        point.ToVector2(),
                        Color.Cyan,
                        10,
                        Game1.random.NextDouble() < 0.5,
                        70f,
                        0,
                        Game1.tileSize,
                        100f));

                affectedCount++;
            }

            return affectedCount > 0
                ? new SpellSuccess(player, "clam_tone", affectedCount * 25)
                : new SpellFizzle(player, this.GetManaCost(player, level));
        }
    }
}
