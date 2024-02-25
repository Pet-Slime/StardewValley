using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Network;
using StardewValley.Tools;
using StardewValley.TerrainFeatures;
using StardewValley;
using StardewValley.Objects;
using SOBject = StardewValley.Object;
using StardewValley.Locations;
using MoonShared;
using SpaceCore;
using System.Reflection;
using System.Text.RegularExpressions;
using StardewValley.Monsters;

namespace ExcavationSkill.Objects
{
    //This code is a mess from trying to just copy the vanilla Crab pot code... but hey it works
    [XmlType("Mods_moonslime_ExcavationSkill_PathObjects")]
    public class PathsObject : SOBject
    {


        public new string Name = ModEntry.Instance.I18n.Get("moonslime.excavation.obj_PathsObject.BaseName");
        
        public int TileIndexToShow;

        public PathsObject()
        {

        }

        public PathsObject(string arg)
        {
            int argValue = int.Parse(arg);

            TileLocation = Vector2.Zero;
            ParentSheetIndex = 20001 + argValue;
            Price = 20;
            if (argValue == 2)
            {
                Price = 12;
                DisplayName = ModEntry.Instance.I18n.Get("moonslime.excavation.obj_bone_path.name");
                name = ModEntry.Instance.I18n.Get("moonslime.excavation.obj_bone_path.name");
            } else
            {
                Price = 20;
                DisplayName = ModEntry.Instance.I18n.Get("moonslime.excavation.obj_glass_path.name");
                name = ModEntry.Instance.I18n.Get("moonslime.excavation.obj_glass_path.name");
            }
            CanBeSetDown = true;
            CanBeGrabbed = false;
            IsSpawnedObject = false;
            this.Type = "interactive";
            this.TileIndexToShow = argValue;

        }


        protected override string loadDisplayName()
        {
            string name = ModEntry.Instance.I18n.Get("moonslime.excavation.obj_PathsObject.name");
            if (this.TileIndexToShow == 2)
            {
                name = ModEntry.Instance.I18n.Get("moonslime.excavation.obj_bone_path.name");
            }
            else
            {
                name = ModEntry.Instance.I18n.Get("moonslime.excavation.obj_glass_path.name");
            }
            return name;
        }

        protected override void initNetFields()
        {
            base.initNetFields();
        }


        public override string getDescription()
        {
            if (this.TileIndexToShow == 2)
            {
                return ModEntry.Instance.I18n.Get("moonslime.excavation.obj_bone_path.description");
            } else
            {
                return ModEntry.Instance.I18n.Get("moonslime.excavation.obj_glass_path.description");
            }
        }



        public string FillObjectString(string objectString)
        {
            return string.Format(objectString, this.Price, this.Edibility);
        }



        public override void actionOnPlayerEntry()
        {
            return;
        }

        //Had to make it place a new instance of the shifter object. Else it would just use the instance data from the one the player was holding
        public override bool placementAction(GameLocation location, int x, int y, Farmer who = null)
        {

            Vector2 placementTile = new Vector2(x / 64, y / 64);
            health = 10;
            if (who != null)
            {
                owner.Value = who.UniqueMultiplayerID;
            }
            else
            {
                owner.Value = Game1.player.UniqueMultiplayerID;
            }
            if (IsSprinkler() && location.doesTileHavePropertyNoNull((int)placementTile.X, (int)placementTile.Y, "NoSprinklers", "Back") == "T")
            {
                Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:NoSprinklers"));
                return false;
            }
            if (location.terrainFeatures.ContainsKey(placementTile))
            {
                return false;
            }

            location.terrainFeatures.Add(placementTile, new PathsTerrain(this.TileIndexToShow));
            location.playSound("stoneStep");
            return true;
        }

        public override SOBject GetDeconstructorOutput(Item item)
        {
            if (this.TileIndexToShow == 2)
            {
                return new SOBject(881, 1);
            } else
            {
                return new SOBject(118, 1);
            }
        }

        public override bool canBePlacedInWater()
        {
            return false;
        }


        public override bool isPlaceable()
        {
            return true;
        }


        //Make sure to call the new shifter object and not a vanilla object
        public override Item getOne()
        {
            PathsObject @object = new PathsObject(this.TileIndexToShow.ToString());
            @object._GetOneFrom(this);
            return @object;
        }

        //Make sure to replace regular objects with the shifter object
        public override void _GetOneFrom(Item source)
        {
            orderData.Value = (source as PathsObject).orderData.Value;
            owner.Value = (source as PathsObject).owner.Value;
            base._GetOneFrom(source);
        }

        public override bool performObjectDropInAction(Item dropInItem, bool probe, Farmer who)
        {
            return false;
        }

        public override bool checkForAction(Farmer who, bool justCheckingForActivity = false)
        {
            return false;
        }

        public override void performRemoveAction(Vector2 tileLocation, GameLocation environment)
        {
            return;
        }

        public override void DayUpdate(GameLocation location)
        {
            return;
        }



        public override void updateWhenCurrentLocation(GameTime time, GameLocation environment)
        {
            return;
        }

        public override void draw(SpriteBatch b, int x, int y, float alpha = 1f)
        {

            float YBob = 0;
            b.Draw(ModEntry.Assets.tilesheet, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, y * 64 + YBob)), Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, this.TileIndexToShow, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, ((float)(y * 64) + 0 + (float)(x % 4)) / 10000f);
        }

        public override void drawWhenHeld(SpriteBatch spriteBatch, Vector2 objectPosition, Farmer f)
        {

            spriteBatch.Draw(ModEntry.Assets.tilesheet, objectPosition, GameLocation.getSourceRectForObject(this.TileIndexToShow), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, Math.Max(0f, (float)(f.getStandingY() + 3) / 10000f));
            if (f.ActiveObject == null || !f.ActiveObject.Name.Contains("="))
            {
                return;
            }

            spriteBatch.Draw(ModEntry.Assets.tilesheet, objectPosition + new Vector2(32f, 32f), GameLocation.getSourceRectForObject(this.TileIndexToShow), Color.White, 0f, new Vector2(32f, 32f), 4f + Math.Abs(Game1.starCropShimmerPause) / 8f, SpriteEffects.None, Math.Max(0f, (float)(f.getStandingY() + 3) / 10000f));
            if (!(Math.Abs(Game1.starCropShimmerPause) <= 0.05f) || !(Game1.random.NextDouble() < 0.97))
            {
                Game1.starCropShimmerPause += 0.04f;
                if (Game1.starCropShimmerPause >= 0.8f)
                {
                    Game1.starCropShimmerPause = -0.8f;
                }
            }
        }

        public override void drawAsProp(SpriteBatch b)
        {
            if (this.isTemporarilyInvisible)
            {
                return;
            }

            int num = (int)this.TileLocation.X;
            int num2 = (int)this.TileLocation.Y;


            b.Draw(Game1.shadowTexture, getLocalPosition(Game1.viewport) + new Vector2(32f, 53f), Game1.shadowTexture.Bounds, Color.White, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 4f, SpriteEffects.None, (float)getBoundingBox(new Vector2(num, num2)).Bottom / 15000f);

            Texture2D objectSpriteSheet = ModEntry.Assets.tilesheet;
            Vector2 position = Game1.GlobalToLocal(Game1.viewport, new Vector2(num * 64 + 32, num2 * 64 + 32));
            Microsoft.Xna.Framework.Rectangle? sourceRectangle = GameLocation.getSourceRectForObject(this.TileIndexToShow);
            Color white = Color.White;
            Vector2 origin = new Vector2(8f, 8f);
            _ = scale;
            b.Draw(objectSpriteSheet, position, sourceRectangle, white, 0f, origin, (scale.Y > 1f) ? getScale().Y : 4f, flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None, (float)getBoundingBox(new Vector2(num, num2)).Bottom / 10000f);
        }

        public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
        {
            if ((bool)this.IsRecipe)
            {
                transparency = 0.5f;
                scaleSize *= 0.75f;
            }

            bool flag = ((drawStackNumber == StackDrawType.Draw && maximumStackSize() > 1 && Stack > 1) || drawStackNumber == StackDrawType.Draw_OneInclusive) && (double)scaleSize > 0.3 && Stack != int.MaxValue;
            if (this.IsRecipe)
            {
                flag = false;
            }

            if ((int)this.ParentSheetIndex != 590 && drawShadow)
            {
                spriteBatch.Draw(Game1.shadowTexture, location + new Vector2(32f, 48f), Game1.shadowTexture.Bounds, color * 0.5f, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 3f, SpriteEffects.None, layerDepth - 0.0001f);
            }

            spriteBatch.Draw(ModEntry.Assets.tilesheet, location + new Vector2((int)(32f * scaleSize), (int)(32f * scaleSize)), Game1.getSourceRectForStandardTileSheet(ModEntry.Assets.tilesheet, this.TileIndexToShow, 16, 16), color * transparency, 0f, new Vector2(8f, 8f) * scaleSize, 4f * scaleSize, SpriteEffects.None, layerDepth);
            if (flag)
            {
                Utility.drawTinyDigits(stack, spriteBatch, location + new Vector2((float)(64 - Utility.getWidthOfTinyDigitString(stack, 3f * scaleSize)) + 3f * scaleSize, 64f - 18f * scaleSize + 1f), 3f * scaleSize, 1f, color);
            }

            if (drawStackNumber != 0 && (int)this.Quality > 0)
            {
                Microsoft.Xna.Framework.Rectangle value = (((int)this.Quality < 4) ? new Microsoft.Xna.Framework.Rectangle(338 + ((int)this.Quality - 1) * 8, 400, 8, 8) : new Microsoft.Xna.Framework.Rectangle(346, 392, 8, 8));
                Texture2D mouseCursors = Game1.mouseCursors;
                float num = (((int)this.Quality < 4) ? 0f : (((float)Math.Cos((double)Game1.currentGameTime.TotalGameTime.Milliseconds * Math.PI / 512.0) + 1f) * 0.05f));
                spriteBatch.Draw(mouseCursors, location + new Vector2(12f, 52f + num), value, color * transparency, 0f, new Vector2(4f, 4f), 3f * scaleSize * (1f + num), SpriteEffects.None, layerDepth);
            }

            if (base.Category == -22 && uses.Value > 0)
            {
                float num2 = ((float)(FishingRod.maxTackleUses - uses.Value) + 0f) / (float)FishingRod.maxTackleUses;
                spriteBatch.Draw(Game1.staminaRect, new Microsoft.Xna.Framework.Rectangle((int)location.X, (int)(location.Y + 56f * scaleSize), (int)(64f * scaleSize * num2), (int)(8f * scaleSize)), Utility.getRedToGreenLerpColor(num2));
            }

            if ((bool)this.IsRecipe)
            {
                spriteBatch.Draw(ModEntry.Assets.tilesheet, location + new Vector2(16f, 16f), Game1.getSourceRectForStandardTileSheet(ModEntry.Assets.tilesheet, 451, 16, 16), color, 0f, Vector2.Zero, 3f, SpriteEffects.None, layerDepth + 0.0001f);
            }
        }
    }
}
