using MoonShared;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace ScytheToolUpgrades
{
    [XmlType("Mods_moonslime_upgradeablescythe")] // SpaceCore serialisation signature
    public class UpgradeableScythe : Tool
    {
        public new string Name = "Scythe";

        public UpgradeableScythe() : base()
        {
            base.UpgradeLevel = 0;
        }

        public UpgradeableScythe(int upgradeLevel) : base()
        {
            base.UpgradeLevel = upgradeLevel;
            base.InitialParentTileIndex = -1;
            base.IndexOfMenuItemView = -1;
        }

        public override Item getOne()
        {
            UpgradeableScythe result = new()
            {
                UpgradeLevel = base.UpgradeLevel
            };
            this.CopyEnchantments(this, result);
            result._GetOneFrom(this);
            return result;
        }

        protected override string loadDisplayName()
        {
            throw new NotImplementedException();
        }

        protected override string loadDescription()
        {
            throw new NotImplementedException();
        }

        public static bool CanBeUpgraded()
        {
            Tool pail = Game1.player.getToolFromName("Scythe");
            int MaxUpgradeLevel = ModEntry.RadiationTier ? 6 : 4;
            return pail is not null && pail.UpgradeLevel != MaxUpgradeLevel;
        }

        public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
        {
            spriteBatch.Draw(
                texture: ModEntry.Assets.Sprites,
                position: location + new Vector2(32f, 32f),
                sourceRectangle: IconSourceRectangle(this.UpgradeLevel),
                color: color * transparency,
                rotation: 0f,
                origin: new Vector2(8, 8),
                scale: Game1.pixelZoom * scaleSize,
                effects: SpriteEffects.None,
                layerDepth: layerDepth);
        }

        public static Rectangle IconSourceRectangle(int upgradeLevel)
        {
            Rectangle source = new(80, 0, 16, 16);
            source.Y += upgradeLevel * source.Height;
            return source;
        }

        public override int maximumStackSize()
        {
            return 1;
        }

        public override bool canBeTrashed()
        {
            return false;
        }

        public override bool actionWhenPurchased()
        {
            if (this.UpgradeLevel > 0 && Game1.player.toolBeingUpgraded.Value == null)
            {
                Tool t = Game1.player.getToolFromName("Pail");
                Game1.player.removeItemFromInventory(t);
                if (t is not UpgradeableScythe)
                {
                    t = new UpgradeableScythe(upgradeLevel: 1);
                } else
                {
                    t.UpgradeLevel++;
                }
                Game1.player.toolBeingUpgraded.Value = t;
                Game1.player.daysLeftForToolUpgrade.Value = ModEntry.Config.PailUpgradeDays;
                Game1.playSound("parry");
                Game1.exitActiveMenu();
                Game1.drawDialogue(Game1.getCharacterFromName("Clint"), Game1.content.LoadString("Strings\\StringsFromCSFiles:Tool.cs.14317"));
                return true;
            }
            return base.actionWhenPurchased();
        }

        public static void AddToShopStock(Dictionary<ISalable, int[]> itemPriceAndStock, Farmer who)
        {
            if (who == Game1.player && CanBeUpgraded())
            {
                int quantity = 1;
                int upgradeLevel = who.getToolFromName("Pail").UpgradeLevel + 1;
                if (who.getToolFromName("Pail") is not UpgradeableScythe)
                {
                    upgradeLevel = 1;
                }
                int upgradePrice = ModEntry.PriceForToolUpgradeLevel(upgradeLevel); 
                upgradePrice = (int)(upgradePrice * 1);
                int extraMaterialIndex = ModEntry.IndexOfExtraMaterialForToolUpgrade(upgradeLevel);
                itemPriceAndStock.Add(
                    new UpgradeableScythe(upgradeLevel: upgradeLevel),
                    new int[] { upgradePrice, quantity, extraMaterialIndex, 5 });
            }
        }

        public override void DoFunction(GameLocation location, int x, int y, int power, Farmer who)
        {


            base.DoFunction(location, x, y, power, who);
        }


    }
}
