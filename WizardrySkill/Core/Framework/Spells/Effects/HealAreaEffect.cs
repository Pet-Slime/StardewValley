using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using BirbCore.Attributes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceCore;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Monsters;
using xTile.Dimensions;
using xTile.Tiles;
using static StardewValley.Minigames.TargetGame;
using Object = StardewValley.Object;

namespace WizardrySkill.Core.Framework.Spells.Effects
{
    public class HealAreaEffect : IActiveEffect
    {
        /*********
        ** Fields
        *********/
        private readonly Farmer Summoner;
        private readonly int Level;


        public Vector2 Tile;

        private GameLocation PrevSummonerLoc;

        private int TimeLeft = 120 * 60;


        /*********
        ** Public methods
        *********/
        public HealAreaEffect(Farmer summoner, int level)
        {
            this.Summoner = summoner;
            this.Level = level+1;

            this.Tile = this.Summoner.Tile;
            this.PrevSummonerLoc = summoner.currentLocation;
        }

        public bool Update(UpdateTickedEventArgs e)
        {
            if (this.Summoner == null)
            {
                this.CleanUp();
                this.TimeLeft = 0;
                return false;
            }

            // Handle Location changed
            if (this.PrevSummonerLoc != this.Summoner.currentLocation)
            {
                this.CleanUp();
                this.TimeLeft = 0;
                return false;
            }


            if (this.TimeLeft % 120 != 0)
            {
                --this.TimeLeft;
                return true;
            }

            // Handle attack or movement
            List<Vector2> target = MakeAList(this.Tile, 2 * (this.Level));
            this.AttemptAttack(target);
            this.UpdateSprite(target);

            if (--this.TimeLeft <= 0)
            {
                this.CleanUp();
                return false;
            }

            return true;
        }

        public void Draw(SpriteBatch b)
        {
            // nothing; drawn Manually via TemporaryAnimatedSprite
        }

        public void CleanUp()
        {

        }


        /*********
        ** Private helpers
        *********/
        private void AttemptAttack(List<Vector2> target)
        {
            foreach (Farmer player in Game1.getOnlineFarmers())
            {
                if (player != null && player.currentLocation == this.PrevSummonerLoc && target.Contains(player.Tile))
                {
                    player.health = Math.Min(player.health + 1, player.maxHealth);
                    player.currentLocation.debris.Add(new Debris(1, new Vector2(player.StandingPixel.X + 8, player.StandingPixel.Y), Color.Green, 1f, player));
                    if (this.TimeLeft % 300 == 0)
                    {
                        player.currentLocation.playSound("healSound", player.Tile);
                    }
                }
            }
        }

        private List<Vector2> MakeAList(Vector2 tileLocation, int level, bool hollow = false)
        {
            GameLocation location = this.Summoner.currentLocation;
            List<Vector2> list = new List<Vector2>();
            int centerX = (int)tileLocation.X;
            int centerY = (int)tileLocation.Y;

            float radius = 1.5f + level;
            float radiusSq = radius * radius;
            float innerRadiusSq = (radius - 0.75f) * (radius - 0.75f);

            for (int x = centerX - (int)radius; x <= centerX + (int)radius; x++)
            {
                for (int y = centerY - (int)radius; y <= centerY + (int)radius; y++)
                {
                    float dx = x - centerX;
                    float dy = y - centerY;
                    float distSq = dx * dx + dy * dy;

                    if (distSq <= radiusSq)
                    {


                        if (!hollow || distSq >= innerRadiusSq)
                            list.Add(new Vector2(x, y));
                    }
                }
            }

            return list;
        }

        private void UpdateSprite(List<Vector2> target)
        {
            GameLocation location = this.Summoner.currentLocation;

            foreach (Vector2 tile in target)
            {

                if (location.IsTileBlockedBy(new Vector2(tile.X, tile.Y), ignorePassables: CollisionMask.Farmers))
                    continue;

                var point = tile * Game1.tileSize;
                Game1.Multiplayer.broadcastSprites(location,
                    new TemporaryAnimatedSprite(10,
                    point,
                    Color.Red,
                    10,
                    Game1.random.NextDouble() < 0.5,
                    70f,
                    0,
                    Game1.tileSize,
                    (tile.Y + 1) * Game1.tileSize / 10000f));
            }
        }
    }
}
