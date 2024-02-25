using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley.Network;
using StardewValley;
using System.Xml.Serialization;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using SObject = StardewValley.Object;


namespace ExcavationSkill.Objects
{
    [XmlType("Mods_moonslime_ExcavationSkill_PathTerrain")]
    public class PathsTerrain : Flooring
    {
        private struct NeighborLoc
        {
            public readonly Vector2 Offset;

            public readonly byte Direction;

            public readonly byte InvDirection;

            public NeighborLoc(Vector2 a, byte b, byte c)
            {
                Offset = a;
                Direction = b;
                InvDirection = c;
            }
        }

        private struct Neighbor
        {
            public readonly Flooring feature;

            public readonly byte direction;

            public readonly byte invDirection;

            public Neighbor(Flooring a, byte b, byte c)
            {
                feature = a;
                direction = b;
                invDirection = c;
            }
        }

        public new static Texture2D floorsTexture;

        public new static Texture2D floorsTextureWinter;

        [InstancedStatic]
        public new static Dictionary<byte, int> drawGuide;


        [XmlElement("whichFloor")]
        public new readonly NetInt whichFloor = new NetInt();

        [XmlElement("whichView")]
        public new readonly NetInt whichView = new NetInt();

        [XmlElement("isPathway")]
        public new readonly NetBool isPathway = new NetBool();

        [XmlElement("isSteppingStone")]
        public new readonly NetBool isSteppingStone = new NetBool();

        [XmlElement("drawContouredShadow")]
        public new readonly NetBool drawContouredShadow = new NetBool();

        [XmlElement("cornerDecoratedBorders")]
        public new readonly NetBool cornerDecoratedBorders = new NetBool();

        private byte neighborMask;

        private static readonly NeighborLoc[] _offsets = new NeighborLoc[8]
        {
            new NeighborLoc(N_Offset, 1, 4),
            new NeighborLoc(S_Offset, 4, 1),
            new NeighborLoc(E_Offset, 2, 8),
            new NeighborLoc(W_Offset, 8, 2),
            new NeighborLoc(NE_Offset, 16, 128),
            new NeighborLoc(NW_Offset, 32, 64),
            new NeighborLoc(SE_Offset, 64, 32),
            new NeighborLoc(SW_Offset, 128, 16)
        };

        private List<Neighbor> _neighbors = new List<Neighbor>();

        public PathsTerrain()
            : base()
        {
            base.NetFields.AddFields(whichFloor, whichView, isPathway, isSteppingStone, drawContouredShadow, cornerDecoratedBorders);
            loadSprite();
            if (drawGuide == null)
            {
                populateDrawGuide();
            }
        }

        public PathsTerrain(int which)
            : this()
        {
            whichFloor.Value = which;
            ApplyFlooringFlags();
        }

        public override void ApplyFlooringFlags()
        {
            whichView.Value = Game1.random.Next(16);
            isSteppingStone.Value = true;
            isPathway.Value = true;
        }

        public override Rectangle getBoundingBox(Vector2 tileLocation)
        {
            return new Rectangle((int)(tileLocation.X * 64f), (int)(tileLocation.Y * 64f), 64, 64);
        }

        public static void populateDrawGuide()
        {
            drawGuide = new Dictionary<byte, int>();
            drawGuide.Add(0, 0);
            drawGuide.Add(6, 1);
            drawGuide.Add(14, 2);
            drawGuide.Add(12, 3);
            drawGuide.Add(4, 16);
            drawGuide.Add(7, 17);
            drawGuide.Add(15, 18);
            drawGuide.Add(13, 19);
            drawGuide.Add(5, 32);
            drawGuide.Add(3, 33);
            drawGuide.Add(11, 34);
            drawGuide.Add(9, 35);
            drawGuide.Add(1, 48);
            drawGuide.Add(2, 49);
            drawGuide.Add(10, 50);
            drawGuide.Add(8, 51);
            drawGuideList = new List<int>(drawGuide.Count);
            foreach (KeyValuePair<byte, int> item in drawGuide)
            {
                drawGuideList.Add(item.Value);
            }
        }

        public override void loadSprite()
        {
            if (floorsTexture == null)
            {
                try
                {
                    floorsTexture = ModEntry.Assets.Flooring;
                }
                catch (Exception)
                {
                }
            }

            if (floorsTextureWinter == null)
            {
                try
                {
                    floorsTextureWinter = ModEntry.Assets.FlooringWinter;
                }
                catch (Exception)
                {
                }
            }

            
                isPathway.Value = true;
        }

        public override void doCollisionAction(Rectangle positionOfCollider, int speedOfCollision, Vector2 tileLocation, Character who, GameLocation location)
        {
            base.doCollisionAction(positionOfCollider, speedOfCollision, tileLocation, who, location);
            if (who != null && who is Farmer && location is Farm)
            {
                (who as Farmer).temporarySpeedBuff = 0.1f;
            }
        }

        public override bool isPassable(Character c = null)
        {
            return true;
        }

        public string getFootstepSound()
        {
            return "stoneStep";
        }

        private Texture2D getTexture()
        {
            if (Game1.GetSeasonForLocation(currentLocation)[0] == 'w' && (currentLocation == null || !currentLocation.isGreenhouse))
            {
                return floorsTextureWinter;
            }

            return floorsTexture;
        }

        public override bool performToolAction(Tool t, int damage, Vector2 tileLocation, GameLocation location)
        {
            if (location == null)
            {
                location = Game1.currentLocation;
            }

            if ((t != null || damage > 0) && (damage > 0 || t is Pickaxe || t is Axe))
            {
                Game1.createRadialDebris(location, ((int)whichFloor == 0) ? 12 : 14, (int)tileLocation.X, (int)tileLocation.Y, 4, resource: false);
                string parentSheetIndex = "0";
                switch ((int)whichFloor)
                {
                    case 1:
                        location.playSound("hammer");
                        parentSheetIndex = "1";
                        break;
                    case 2:
                        location.playSound("hammer");
                        parentSheetIndex = "2";
                        break;
                }

                location.debris.Add(new Debris(new PathsObject(parentSheetIndex), tileLocation * 64f + new Vector2(32f, 32f)));
                return true;
            }

            return false;
        }

        public override void drawInMenu(SpriteBatch spriteBatch, Vector2 positionOnScreen, Vector2 tileLocation, float scale, float layerDepth)
        {
            int num = 1;
            int num2 = ((int)whichFloor-1) * 4 * 64;
            byte b = 0;
            Vector2 key = tileLocation;
            key.X += 1f;
            GameLocation locationFromName = Game1.getLocationFromName("Farm");
            if (locationFromName.terrainFeatures.ContainsKey(key) && locationFromName.terrainFeatures[key] is Flooring || locationFromName.terrainFeatures[key] is PathsTerrain)
            {
                b = (byte)(b + 2);
            }

            key.X -= 2f;
            if (locationFromName.terrainFeatures.ContainsKey(key) && Game1.currentLocation.terrainFeatures[key] is Flooring || locationFromName.terrainFeatures[key] is PathsTerrain)
            {
                b = (byte)(b + 8);
            }

            key.X += 1f;
            key.Y += 1f;
            if (Game1.currentLocation.terrainFeatures.ContainsKey(key) && locationFromName.terrainFeatures[key] is Flooring || locationFromName.terrainFeatures[key] is PathsTerrain)
            {
                b = (byte)(b + 4);
            }

            key.Y -= 2f;
            if (locationFromName.terrainFeatures.ContainsKey(key) && locationFromName.terrainFeatures[key] is Flooring || locationFromName.terrainFeatures[key] is PathsTerrain)
            {
                b = (byte)(b + 1);
            }

            num = drawGuide[b];
            spriteBatch.Draw(floorsTexture, positionOnScreen, new Rectangle(num % 16 * 16, num / 16 * 16 + num2, 16, 16), Color.White, 0f, Vector2.Zero, scale * 4f, SpriteEffects.None, layerDepth + positionOnScreen.Y / 20000f);
        }

        public override void draw(SpriteBatch spriteBatch, Vector2 tileLocation)
        {


            if (cornerDecoratedBorders.Value)
            {
                int num = 6;
                if ((neighborMask & 9) == 9 && (neighborMask & 0x20) == 0)
                {
                    spriteBatch.Draw(getTexture(), Game1.GlobalToLocal(Game1.viewport, new Vector2(tileLocation.X * 64f, tileLocation.Y * 64f)), new Rectangle(64 - num + 64 * ((int)whichFloor % 4), 48 - num + (int)whichFloor / 4 * 64, num, num), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (tileLocation.Y * 64f + 2f + tileLocation.X / 10000f) / 20000f);
                }

                if ((neighborMask & 3) == 3 && (neighborMask & 0x10) == 0)
                {
                    spriteBatch.Draw(getTexture(), Game1.GlobalToLocal(Game1.viewport, new Vector2(tileLocation.X * 64f + 64f - (float)(num * 4), tileLocation.Y * 64f)), new Rectangle(16 + 64 * ((int)whichFloor % 4), 48 - num + (int)whichFloor / 4 * 64, num, num), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (tileLocation.Y * 64f + 2f + tileLocation.X / 10000f + (float)(int)whichFloor) / 20000f);
                }

                if ((neighborMask & 6) == 6 && (neighborMask & 0x40) == 0)
                {
                    spriteBatch.Draw(getTexture(), Game1.GlobalToLocal(Game1.viewport, new Vector2(tileLocation.X * 64f + 64f - (float)(num * 4), tileLocation.Y * 64f + 64f - (float)(num * 4))), new Rectangle(16 + 64 * ((int)whichFloor % 4), (int)whichFloor / 4 * 64, num, num), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (tileLocation.Y * 64f + 2f + tileLocation.X / 10000f) / 20000f);
                }

                if ((neighborMask & 0xC) == 12 && (neighborMask & 0x80) == 0)
                {
                    spriteBatch.Draw(getTexture(), Game1.GlobalToLocal(Game1.viewport, new Vector2(tileLocation.X * 64f, tileLocation.Y * 64f + 64f - (float)(num * 4))), new Rectangle(64 - num + 64 * ((int)whichFloor % 4), (int)whichFloor / 4 * 64, num, num), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (tileLocation.Y * 64f + 2f + tileLocation.X / 10000f) / 20000f);
                }
            }

            byte key = (byte)(neighborMask & 0xFu);
            int num2 = drawGuide[key];
            if ((bool)isSteppingStone)
            {
                num2 = drawGuideList[whichView.Value];
            }

            if ((bool)drawContouredShadow)
            {
                Color black = Color.Black;
                black.A = (byte)((float)(int)black.A * 0.33f);
                spriteBatch.Draw(getTexture(), Game1.GlobalToLocal(Game1.viewport, new Vector2(tileLocation.X * 64f, tileLocation.Y * 64f)) + new Vector2(-4f, 4f), new Rectangle((int)whichFloor % 4 * 64 + num2 * 16 % 256, num2 / 16 * 16 + (int)whichFloor / 4 * 64, 16, 16), black, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-10f);
            }

            spriteBatch.Draw(getTexture(), Game1.GlobalToLocal(Game1.viewport, new Vector2(tileLocation.X * 64f, tileLocation.Y * 64f)), new Rectangle((int)whichFloor % 4 * 64 + num2 * 16 % 256, num2 / 16 * 16 + (int)whichFloor / 4 * 64, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-09f);
        }

        public override bool tickUpdate(GameTime time, Vector2 tileLocation, GameLocation location)
        {
            base.NeedsUpdate = false;
            return false;
        }

        public override void dayUpdate(GameLocation environment, Vector2 tileLocation)
        {
        }

        public override bool seasonUpdate(bool onLoad)
        {
            return false;
        }

        private List<Neighbor> gatherNeighbors(GameLocation loc, Vector2 tilePos)
        {
            List<Neighbor> neighbors = _neighbors;
            neighbors.Clear();
            TerrainFeature value = null;
            Flooring flooring = null;
            PathsTerrain flooring2 = null;
            NetVector2Dictionary<TerrainFeature, NetRef<TerrainFeature>> terrainFeatures = loc.terrainFeatures;
            NeighborLoc[] offsets = _offsets;
            for (int i = 0; i < offsets.Length; i++)
            {
                NeighborLoc neighborLoc = offsets[i];
                Vector2 vector = tilePos + neighborLoc.Offset;
                if (loc.map != null && !loc.isTileOnMap(vector))
                {
                    Neighbor item = new Neighbor(null, neighborLoc.Direction, neighborLoc.InvDirection);
                    neighbors.Add(item);
                }
                else if (terrainFeatures.TryGetValue(vector, out value) && value != null)
                {
                    flooring = value as Flooring;
                    flooring2 = value as PathsTerrain;
                    if (flooring != null && flooring.whichFloor == whichFloor)
                    {
                        Neighbor item2 = new Neighbor(flooring, neighborLoc.Direction, neighborLoc.InvDirection);
                        neighbors.Add(item2);
                    }
                    if (flooring2 != null && flooring2.whichFloor == whichFloor)
                    {
                        Neighbor item3 = new Neighbor(flooring2, neighborLoc.Direction, neighborLoc.InvDirection);
                        neighbors.Add(item3);
                    }
                }
            }

            return neighbors;
        }

        public void OnAdded(GameLocation loc, Vector2 tilePos)
        {
            List<Neighbor> list = gatherNeighbors(loc, tilePos);
            neighborMask = 0;
            foreach (Neighbor item in list)
            {
                neighborMask |= item.direction;
                if (item.feature != null)
                {
                    item.feature.OnNeighborAdded(item.invDirection);
                }
            }
        }

        public void OnRemoved(GameLocation loc, Vector2 tilePos)
        {
            List<Neighbor> list = gatherNeighbors(loc, tilePos);
            neighborMask = 0;
            foreach (Neighbor item in list)
            {
                if (item.feature != null)
                {
                    item.feature.OnNeighborRemoved(item.invDirection);
                }
            }
        }

        public void OnNeighborAdded(byte direction)
        {
            neighborMask |= direction;
        }

        public void OnNeighborRemoved(byte direction)
        {
            neighborMask = (byte)(neighborMask & ~direction);
        }
    }
}
