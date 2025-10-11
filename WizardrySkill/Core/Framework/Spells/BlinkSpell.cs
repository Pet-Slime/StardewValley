using System;
using System.Linq;
using BirbCore.Attributes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceCore;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Extensions;
using StardewValley.TerrainFeatures;
using WizardrySkill.Core;
using WizardrySkill.Core.Framework;
using WizardrySkill.Core.Framework.Schools;
using xTile.Tiles;
using static BirbCore.Attributes.SMod;
using static StardewValley.Minigames.CraneGame;

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
            return 0;
        }

        public override int GetMaxCastingLevel()
        {
            return 1;
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            Microsoft.Xna.Framework.Rectangle boundingBox = BlinkSpot(player, targetX - player.GetBoundingBox().Width / 2, targetY - player.GetBoundingBox().Height / 2);
            
            if (!player.currentLocation.isCollidingPosition(boundingBox, Game1.viewport, isFarmer: true, 0, glider: false, player))
            {

                var targetTile = Game1.currentCursorTile;
                int distance = (int)Vector2.Distance(player.Tile, targetTile);
                Tile backTile = player.currentLocation.map.RequireLayer("Back").Tiles[(int)targetTile.X, (int)targetTile.Y];



               if (distance == 0)
                   return null;

                if (backTile == null)
                    return null;

                if (backTile != null && (backTile.TileIndexProperties.ContainsKey("Passable") || backTile.Properties.ContainsKey("Passable")))
                    return null;


                if (player.GetCurrentMana() < distance * 5)
                    return null;

                player.position.X = targetX - player.GetBoundingBox().Width / 2;
                player.position.Y = targetY - player.GetBoundingBox().Height / 2;

                player.AddMana(distance * 5 * -1);

                player.LocalSound("powerup");
                ;
                Utilities.AddEXP(player, 4);

                return null;

            }
            return null;

        }

        public Microsoft.Xna.Framework.Rectangle BlinkSpot(Farmer who, int targetX, int targetY)
        {
            return new Microsoft.Xna.Framework.Rectangle(targetX + 8, targetY + who.Sprite.getHeight() - 32, 48, 32);
        }

    }
}
