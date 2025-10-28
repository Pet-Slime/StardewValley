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
    public class EnergyChannelEffect : IActiveEffect
    {
        /*********
        ** Fields
        *********/
        private readonly Farmer Summoner;
        private readonly int Level;


        public Vector2 Tile;

        private GameLocation PrevSummonerLoc;

        private int TimeLeft = 300 * 60;
        private int AttackTimer;


        /*********
        ** Public methods
        *********/
        public EnergyChannelEffect(Farmer summoner, int level)
        {
            this.Summoner = summoner;
            this.Level = level+1;

            this.Tile = this.Summoner.Tile;
            this.PrevSummonerLoc = summoner.currentLocation;
            this.AttackTimer = 0;
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

            if (this.Tile != this.Summoner.Tile)
            {
                this.CleanUp();
                this.TimeLeft = 0;
                return false;
            }


            if (this.Summoner.GetCurrentMana() < 3)
            {
                this.CleanUp();
                this.TimeLeft = 0;
                return false;
            }

            if (this.AttackTimer > 0)
                this.AttackTimer--;
            if (this.AttackTimer <= 0) {

                this.AttemptAttack(this.Summoner);
                this.UpdateSprite(this.Summoner);
            }

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
        private void AttemptAttack(Farmer target)
        {

            target.Stamina = Math.Min(target.Stamina + this.Level, target.MaxStamina);
            target.AddMana(-this.Level);
            this.AttackTimer = 60;
        }

        private void UpdateSprite(Farmer target)
        {
            GameLocation location = target.currentLocation;

            var point = target.StandingPixel;

            point.X -= target.Sprite.SpriteWidth * 2;
            point.Y -= (int)(target.Sprite.SpriteHeight * 1.5);

            Game1.Multiplayer.broadcastSprites(target.currentLocation,
                new TemporaryAnimatedSprite(10,
                point.ToVector2(),
                Color.Green,
                10,
                Game1.random.NextDouble() < 0.5,
                70f,
                0,
                Game1.tileSize,
                100f));

            point.Y -= (int)(target.Sprite.SpriteHeight * 2.5);

            Game1.Multiplayer.broadcastSprites(target.currentLocation,
                new TemporaryAnimatedSprite(10,
                point.ToVector2(),
                Color.Green,
                10,
                Game1.random.NextDouble() < 0.5,
                70f,
                0,
                Game1.tileSize,
                100f));
        }
    }
}
