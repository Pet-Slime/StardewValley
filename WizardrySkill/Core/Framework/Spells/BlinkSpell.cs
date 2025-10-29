using System.Linq;
using BirbCore.Attributes;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;
using xTile.Tiles;

namespace WizardrySkill.Core.Framework.Spells
{
    public class BlinkSpell : Spell
    {
        /*********
        ** Public methods
        *********/
        public BlinkSpell()
            : base(SchoolId.Toil, "blink") { }

        public override int GetManaCost(Farmer player, int level)
        {
            // Base cost handled dynamically in OnCast
            return 0;
        }

        public override int GetMaxCastingLevel()
        {
            return 1;
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (!player.IsLocalPlayer)
                return null;

            GameLocation location = player.currentLocation;
            Vector2 targetTile = Game1.currentCursorTile;

            Log.Alert("Test 1");
            // 1. Prevent teleporting out of bounds
            if (!location.isTileOnMap(targetTile))
                return new SpellFizzle(player);

            Log.Alert("Test 2");
            // 2. Compute distance
            int distance = (int)Vector2.Distance(player.Tile, targetTile);
            if (distance == 0)
                return new SpellFizzle(player);

            // 3. Build collision rectangle
            Rectangle boundingBox = BlinkSpot(player, targetX - player.GetBoundingBox().Width / 2, targetY - player.GetBoundingBox().Height / 2);

            Log.Alert("Test 3");
            // 4. Block solid walls or obstacles
            if (location.isCollidingPosition(boundingBox, Game1.viewport, isFarmer: true, 0, glider: false, player))
                return new SpellFizzle(player);

            Log.Alert("Test 4");
            // 5. Terrain validation (Back layer)
            var backLayer = location.map.RequireLayer("Back");
            Tile backTile = backLayer.Tiles[(int)targetTile.X, (int)targetTile.Y];
            if (backTile == null)
                return new SpellFizzle(player);

            Log.Alert("Test 5");
            if (HasCollision(backTile))
                return new SpellFizzle(player);

            Log.Alert("Test 6");
            // 6. Block if there’s an object (chest, crops, etc.)
            if (location.objects.ContainsKey(targetTile))
                return new SpellFizzle(player);

            Log.Alert("Test 7");
            // 7. Block if NPC, monster, or pet occupies that tile
            if (location.isCharacterAtTile(targetTile) != null)
                return new SpellFizzle(player);

            Log.Alert("Test 8");
            // 8. Block if furniture occupies the target area
            if (location.furniture?.Any(f => f.boundingBox.Value.Contains(targetX, targetY)) == true)
                return new SpellFizzle(player);

            Log.Alert("Test 9");
            // 9. Mana cost based on distance
            int manaCost = distance * 2;
            if (player.GetCurrentMana() < manaCost)
                return new SpellFizzle(player);

            // 10. Teleport player
            player.position.X = targetX - player.GetBoundingBox().Width / 2;
            player.position.Y = targetY - player.GetBoundingBox().Height / 2;
            player.AddMana(-manaCost);

            // 11. Success effect
            return new SpellSuccess(player, "powerup", 1 * distance);
        }

        /*********
        ** Private helpers
        *********/
        private Rectangle BlinkSpot(Farmer who, int targetX, int targetY)
        {
            // Roughly aligns with player's hitbox
            return new Rectangle(targetX + 8, targetY + who.Sprite.getHeight() - 32, 48, 32);
        }

        private bool HasCollision(Tile tile)
        {
            // Checks for any terrain property that should block blinking
            return tile.TileIndexProperties.ContainsKey("Passable")
                || tile.Properties.ContainsKey("Passable")
                || tile.TileIndexProperties.ContainsKey("Water")
                || tile.Properties.ContainsKey("Water")
                || tile.TileIndexProperties.ContainsKey("Buildings")
                || tile.Properties.ContainsKey("Buildings");
        }
    }
}
