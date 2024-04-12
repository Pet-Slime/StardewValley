using ArchaeologySkill;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MoonShared;
using Netcode;
using SpaceCore;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using xTile.Dimensions;
using Object = StardewValley.Object;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace ArchaeologySkill.Objects.Water_Shifter
{
    [XmlType("Mods_moonslime_Archaeology_ShifterObject")]
    public class WaterShifter : Object
    {
        private float YBob;
        [XmlElement("ShifterDirectionOffset")]
        public readonly NetVector2 ShifterDirectionOffset = new();


        [XmlElement("ShifterBait")]
        public readonly NetRef<Object> ShifterBait = new();

        public static Texture2D Texture => ModEntry.Assets.Water_shifter;

        public static Microsoft.Xna.Framework.Rectangle SourceRect => new(0, 0, 16, 16);

        [XmlElement("ShifterArtTileToShowNet")]
        public readonly NetInt ShifterArtTileToShowNet = new NetInt();

        [XmlIgnore]
        public virtual int ShifterArtTileToShow
        {
            get
            {
                return ShifterArtTileToShowNet.Value;
            }
            set
            {
                if (ShifterArtTileToShowNet.Value != value)
                {
                    ShifterArtTileToShowNet.Value = value;
                    RecalculateBoundingBox();
                }
            }
        }


        [XmlElement("ShifterLocationNet")]
        public readonly NetVector2 ShifterLocationNet = new NetVector2();

        [XmlIgnore]
        public virtual Vector2 ShifterLocation
        {
            get
            {
                return ShifterLocationNet.Value;
            }
            set
            {
                if (ShifterLocationNet.Value != value)
                {
                    ShifterLocationNet.Value = value;
                    RecalculateBoundingBox();
                }
            }
        }


        [XmlElement("shifterOwner")]
        public readonly NetLong Owner = new NetLong();

        public bool LidFlapping;

        public bool LidClosing;

        public float LidFlapTimer;

        public float ShakeTimer;

        public Vector2 Shake;

        private readonly int[] Qualities = [lowQuality, medQuality, highQuality, bestQuality];

        public WaterShifter() : base(Vector2.Zero, ModEntry.ObjectInfo.Id) { }

        public WaterShifter(Vector2 shifterLocation, int stack = 1) : this()
        {
            ItemId = "moonslime.Archaeology.water_shifter";
            ShifterArtTileToShow = 0;
            ShifterLocation = shifterLocation;
            Type = "interactive";
            Stack = stack;
            Owner = owner;
        }



        protected void AddOverlayTilesIfNecessary(GameLocation location, int x, int y, List<Vector2> tiles)
        {
            if (location != Game1.currentLocation || location.getTileIndexAt(x, y, "Buildings") < 0 || location.doesTileHaveProperty(x, y + 1, "Back", "Water") != null)
                return;
            tiles.Add(new(x, y));
        }

        protected bool CheckLocation(GameLocation location, float x, float y) => location.doesTileHaveProperty((int)x, (int)y, "Water", "Back") == null || location.doesTileHaveProperty((int)x, (int)y, "Passable", "Buildings") != null;


        protected override void initNetFields()
        {
            base.initNetFields();
            NetFields.AddField(ShifterDirectionOffset, "ShifterDirectionOffset").AddField(ShifterBait, "ShifterBait").AddField(Owner, "shifterOwner")
                .AddField(ShifterLocationNet, "ShifterLocationNet").AddField(ShifterArtTileToShowNet, "ShifterArtTileToShowNet");
        }

        public List<Vector2> GetOverlayTiles(GameLocation location)
        {
            List<Vector2> tiles = new();
            if (ShifterDirectionOffset.Y < 0f)
                AddOverlayTilesIfNecessary(location, (int)ShifterLocation.X, (int)ShifterLocation.Y, tiles);
            AddOverlayTilesIfNecessary(location, (int)ShifterLocation.X, (int)ShifterLocation.Y + 1, tiles);
            if (ShifterDirectionOffset.X < 0f)
                AddOverlayTilesIfNecessary(location, (int)ShifterLocation.X - 1, (int)ShifterLocation.Y + 1, tiles);
            if (ShifterDirectionOffset.X > 0f)
                AddOverlayTilesIfNecessary(location, (int)ShifterLocation.X + 1, (int)ShifterLocation.Y + 1, tiles);
            return tiles;
        }

        public void AddOverlayTiles(GameLocation location)
        {
            if (location != Game1.currentLocation)
                return;
            foreach (Vector2 overlayTile in GetOverlayTiles(location))
            {
                if (!Game1.crabPotOverlayTiles.ContainsKey(overlayTile))
                    Game1.crabPotOverlayTiles[overlayTile] = 0;
                Game1.crabPotOverlayTiles[overlayTile]++;
            }
        }

        public void RemoveOverlayTiles(GameLocation location)
        {
            if (location != Game1.currentLocation)
                return;
            foreach (Vector2 overlayTile in GetOverlayTiles(location))
            {
                if (Game1.crabPotOverlayTiles.ContainsKey(overlayTile))
                {
                    Game1.crabPotOverlayTiles[overlayTile]--;
                    if (Game1.crabPotOverlayTiles[overlayTile] <= 0)
                        Game1.crabPotOverlayTiles.Remove(overlayTile);
                }
            }
        }

        public void UpdateOffset(GameLocation location)
        {
            Vector2 zero = Vector2.Zero;
            if ( CheckLocation(location,  ShifterLocation.X - 1f,  ShifterLocation.Y))
                zero += new Vector2(32f, 0f);
            if ( CheckLocation(location,  ShifterLocation.X + 1f,  ShifterLocation.Y))
                zero += new Vector2(-32f, 0f);
            if (zero.X != 0.0f &&  CheckLocation(location,  ShifterLocation.X + Math.Sign(zero.X),  ShifterLocation.Y + 1f))
                zero += new Vector2(0.0f, -42f);
            if ( CheckLocation(location,  ShifterLocation.X,  ShifterLocation.Y - 1f))
                zero += new Vector2(0.0f, 32f);
            if ( CheckLocation(location,  ShifterLocation.X,  ShifterLocation.Y + 1f))
                zero += new Vector2(0.0f, -42f);
             ShifterDirectionOffset.Value = zero;
        }

        public static bool IsValidPlacementLocation(GameLocation location, int x, int y)
        {
            Vector2 tile = new(x, y);
            bool flag = location.isWaterTile(x + 1, y) && location.isWaterTile(x - 1, y) || location.isWaterTile(x, y + 1) && location.isWaterTile(x, y - 1);
            return location is not Caldera && !location.Objects.ContainsKey(tile) && flag && (location.isWaterTile(x, y) && location.doesTileHaveProperty(x, y, "Passable", "Buildings") == null);
        }

        

        protected override Item GetOneNew()
        {
            return new Object("moonslime.Archaeology.water_shifter", 1);
        }

        public override bool isPlaceable() => true;

        public override bool canBeTrashed() => true;

        public override bool canBeDropped() => true;

        public override bool canBeGivenAsGift() => false;

        public override bool canBeShipped() => false;

        public override void actionOnPlayerEntry()
        {
             UpdateOffset( Location);
             AddOverlayTiles( Location);
            base.actionOnPlayerEntry();
        }

        public override bool placementAction(GameLocation location, int x, int y, Farmer who = null)
        {
            if (who != null)
                 Owner.Value = who.UniqueMultiplayerID;
            if (!IsValidPlacementLocation(location, (int)Math.Floor(x / 64f), (int)Math.Floor(y / 64f)))
                return false;
            ShifterLocation = new Vector2((int)Math.Floor(x / 64f), (int)Math.Floor(y / 64f));
            location.Objects.Add( ShifterLocation, this);
            location.playSound("waterSlosh");
            DelayedAction.playSoundAfterDelay("slosh", 150);
             UpdateOffset(location);
             AddOverlayTiles(location);
            return true;
        }

        public override bool performObjectDropInAction(Item dropInItem, bool probe, Farmer who, bool returnFalseIfItemConsumed = false)
        {
            GameLocation location =  Location;
            if (location == null)
            {
                return false;
            }

            if (!(dropInItem is Object @object))
            {
                return false;
            }


            if (@object.name == "Fiber" &&  ShifterBait.Value == null)
            {
                if (!probe)
                {
                    if (who != null)
                    {
                         Owner.Value = who.UniqueMultiplayerID;
                    }

                     ShifterBait.Value = @object.getOne() as Object;
                    location.playSound("Ship");
                     LidFlapping = true;
                     LidFlapTimer = 60f;
                }

                return true;
            }

            return false;
        }

        public override void performRemoveAction()
        {
             RemoveOverlayTiles(Location);
             ShifterBait.Value = null;
            base.performRemoveAction();
        }

        public override bool checkForAction(Farmer who, bool justCheckingForActivity = false)
        {
            GameLocation location = Location;
            if (ShifterArtTileToShow == 4)
            {

                if (justCheckingForActivity)
                {
                    return true;
                }

                if ( heldObject.Value == null)
                {
                    ShifterBait.Value = null;
                    readyForHarvest.Value = false;
                    ShifterArtTileToShow = 0;
                    return true;
                }

                Object value =  heldObject.Value;
                heldObject.Value = null;
                if (who.IsLocalPlayer && !who.addItemToInventoryBool(value))
                {
                    heldObject.Value = value;
                    Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Crop.cs.588"));
                    return false;
                }

                readyForHarvest.Value = false;
                ShifterArtTileToShow = 0;
                LidFlapping = true;
                LidFlapTimer = 60f;
                ShifterBait.Value = null;
                who.animateOnce(279 + who.FacingDirection);
                who.currentLocation.playSound("fishingRodBend");
                DelayedAction.playSoundAfterDelay("coin", 500);
//              ModEntry.AddEXP(Game1.getFarmer(who.UniqueMultiplayerID), ModEntry.Config.ExperienceFromWaterShifter);
                Shake = Vector2.Zero;
                ShakeTimer = 0f;
                return true;
            }

            if ( ShifterBait.Value == null)
            {
                if (justCheckingForActivity)
                {
                    return true;
                }

                if (Game1.didPlayerJustClickAtAll(ignoreNonMouseHeldInput: true))
                {
                    if (who.addItemToInventoryBool(GetOneNew()))
                    {
                        if (who.isMoving())
                        {
                            Game1.haltAfterCheck = false;
                        }

                        Game1.playSound("coin");
                        location.objects.Remove(ShifterLocation);
                        return true;
                    }

                    Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Crop.cs.588"));
                }
            }

            return false;
        }

        public override void dropItem(GameLocation location, Vector2 origin, Vector2 destination)
        {
            if (fragility == 2)
                return;
            string itemID = ModEntry.ObjectInfo.Id;
            location.debris.Add(new(new Object(itemID, 1), origin, destination));
        }

        public override void DayUpdate()
        {
            var player = Game1.getFarmer( Owner.Value);
            //Player Can get artifacts from the shift if they have the Trowler Profession
            bool flag = player != null && player.HasCustomProfession(Archaeology_Skill.Archaeology10b1);

            //If there is no fiber in the shifter, return and don't do anything.
            //If there is already an item in the shifter, return an don't do anything
            if (!( ShifterBait.Value != null) ||  heldObject.Value != null)
            {
                return;
            }

            ShifterArtTileToShow = 4;
             readyForHarvest.Value = true;
            Random random = new Random((int)Game1.stats.DaysPlayed + (int)Game1.uniqueIDForThisGame / 2 + (int) ShifterLocation.X * 1000 + (int) ShifterLocation.Y);

            //Generate the list of loot
            List<string> list =
            [
                //Populate the loot list
                .. ModEntry.BonusLootTable,
            ];

            //If flag is true, add in the artifact loot table to the list
            if (flag)
            {
                foreach (string item in ModEntry.ArtifactLootTable)
                {
                    list.Add(item);
                }
            }


            //Shuffle the list so it's in a random order!
            list.Shuffle<string>(random);

            if ( heldObject.Value == null)
            {
                 heldObject.Value = new Object(list[random.Next(list.Count)], 1);
            }
        }

        public override void updateWhenCurrentLocation(GameTime time)
        {
            if ( LidFlapping)
            {
                 LidFlapTimer -= time.ElapsedGameTime.Milliseconds;
                if ( LidFlapTimer <= 0f)
                {
                    ShifterArtTileToShow += ((! LidClosing) ? 1 : (-1));
                    if (ShifterArtTileToShow >= 3 && ! LidClosing)
                    {
                         LidClosing = true;
                        ShifterArtTileToShow--;

                    }
                    else if (ShifterArtTileToShow <= 1 &&  LidClosing)
                    {
                         LidClosing = false;
                        ShifterArtTileToShow++;
                         LidFlapping = false;
                        if ( ShifterBait.Value != null)
                        {
                            ShifterArtTileToShow = 3;
                        } else
                        {
                            ShifterArtTileToShow = 0;
                        }
                    }

                     LidFlapTimer = 60f;
                }
            }

            if (readyForHarvest.Value &&  heldObject.Value != null)
            {
                 ShakeTimer -= time.ElapsedGameTime.Milliseconds;
                if ( ShakeTimer < 0f)
                {
                     ShakeTimer = Game1.random.Next(2800, 3200);
                }
            }

            if ( ShakeTimer > 2000f)
            {
                 Shake.X = Game1.random.Next(-1, 2);
            }
            else
            {
                 Shake.X = 0f;
            }
        }



        public override void draw(SpriteBatch spriteBatch, int x, int y, float alpha = 1f)
        {
            GameLocation location = Location;
            if (location == null)
            {
                return;
            }

            if (heldObject.Value != null)
            {
                ShifterArtTileToShow = 4;
            }
            else if (ShifterArtTileToShow == 0)
            {
                ShifterArtTileToShow = 0;
            }

            YBob = (float)(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 500.0 + (double)(x * 64)) * 8.0 + 8.0);
            if (YBob <= 0.001f)
            {
                location.temporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 0, 64, 64), 150f, 8, 0, ShifterDirectionOffset.Value + new Vector2(x * 64 + 4, y * 64 + 32), flicker: false, Game1.random.NextBool(), 0.001f, 0.01f, Color.White, 0.75f, 0.003f, 0f, 0f));
            }

            spriteBatch.Draw(ModEntry.Assets.Water_shifter, Game1.GlobalToLocal(Game1.viewport, ShifterDirectionOffset.Value + new Vector2(x * 64, y * 64 + (int)YBob)) + Shake, Game1.getSourceRectForStandardTileSheet(ModEntry.Assets.Water_shifter, ShifterArtTileToShow, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, ((float)(y * 64) + ShifterDirectionOffset.Y + (float)(x % 4)) / 10000f);
            if (location.waterTiles != null && x < location.waterTiles.waterTiles.GetLength(0) && y < location.waterTiles.waterTiles.GetLength(1) && location.waterTiles.waterTiles[x, y].isWater)
            {
                if (location.waterTiles.waterTiles[x, y].isVisible)
                {
                    spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, ShifterDirectionOffset.Value + new Vector2(x * 64 + 4, y * 64 + 48)) + Shake, new Rectangle(location.waterAnimationIndex * 64, 2112 + (((x + y) % 2 != 0) ? ((!location.waterTileFlip) ? 128 : 0) : (location.waterTileFlip ? 128 : 0)), 56, 16 + (int)YBob), location.waterColor.Value, 0f, Vector2.Zero, 1f, SpriteEffects.None, ((float)(y * 64) + ShifterDirectionOffset.Y + (float)(x % 4)) / 9999f);
                }
                else
                {
                    Color a = new Color(135, 135, 135, 215);
                    a = Utility.MultiplyColor(a, location.waterColor.Value);
                    spriteBatch.Draw(Game1.staminaRect, Game1.GlobalToLocal(Game1.viewport, ShifterDirectionOffset.Value + new Vector2(x * 64 + 4, y * 64 + 48)) + Shake, null, a, 0f, Vector2.Zero, new Vector2(56f, 16 + (int)YBob), SpriteEffects.None, ((float)(y * 64) + ShifterDirectionOffset.Y + (float)(x % 4)) / 9999f);
                }
            }

            if (readyForHarvest.Value && heldObject.Value != null)
            {
                float num = 4f * (float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0), 2);
                spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, ShifterDirectionOffset.Value + new Vector2(x * 64 - 8, (float)(y * 64 - 96 - 16) + num)), new Rectangle(141, 465, 20, 24), Color.White * 0.75f, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)((y + 1) * 64) / 10000f + 1E-06f + ShifterLocation.X / 10000f);
                ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(heldObject.Value.QualifiedItemId);
                spriteBatch.Draw(dataOrErrorItem.GetTexture(), Game1.GlobalToLocal(Game1.viewport, ShifterDirectionOffset.Value + new Vector2(x * 64 + 32, (float)(y * 64 - 64 - 8) + num)), dataOrErrorItem.GetSourceRect(), Color.White * 0.75f, 0f, new Vector2(8f, 8f), 4f, SpriteEffects.None, (float)((y + 1) * 64) / 10000f + 1E-05f + ShifterLocation.X / 10000f);
            }
        }
    }
}
