using HarmonyLib;
using System;
using System.Collections.Generic;
using StardewValley;
using StardewValley.Locations;
using Netcode;
using SpaceCore;
using Microsoft.Xna.Framework;
using StardewValley.Tools;
using MoonShared;
using StardewValley.Objects;
using System.Reflection;
using xTile.Dimensions;
using System.Reflection.Emit;
using System.Globalization;
using System.Linq;
using xTile.ObjectModel;
using xTile;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using System.Diagnostics.CodeAnalysis;
using MoonShared.Patching;

namespace ExcavationSkill
{
    internal class CheckForBuriedItem_Base_patch : BasePatcher
    {
        /*********
        ** Public methods
        *********/
        /// <inheritdoc />
        public override void Apply(Harmony harmony, IMonitor monitor)
        {
            harmony.Patch(
                original: this.RequireMethod<GameLocation>("checkForBuriedItem"),
                prefix: this.GetHarmonyMethod(nameof(Replace_EXP))
            );
        }


        /*********
        ** Private methods
        *********/
        /// Prefix since all results are blank. Copy vanilla code and past in the replacements

        [HarmonyLib.HarmonyPrefix]
        private static bool Replace_EXP(
        GameLocation __instance, string __result, int xLocation, int yLocation, bool explosion, bool detectOnly, Farmer who)
        {
            Log.Trace("Excavation skill check for buried treasure, general");

            Random random = new Random(xLocation * 2000 + yLocation * 77 + (int)Game1.uniqueIDForThisGame / 2 + (int)Game1.stats.DaysPlayed + (int)Game1.stats.DirtHoed);
            string text = __instance.doesTileHaveProperty(xLocation, yLocation, "Treasure", "Back");

            //Custom code
            double test = Utilities.GetLevel() * 0.05;
            bool bonusLoot = false;
            if (random.NextDouble() < test)
            {
                bonusLoot = true;
            }
            //end Custom Code

            if (text != null)
            {
                string[] array = text.Split(' ');
                if (detectOnly)
                {
                    __result = array[0];
                }

                switch (array[0])
                {
                    case "Coins":
                        Game1.createObjectDebris(330, xLocation, yLocation);
                        #region Custom Code
                        Utilities.ApplyExcavationSkill(who, bonusLoot, 330, xLocation, yLocation);
                        #endregion
                        break;
                    case "Copper":
                        Game1.createDebris(0, xLocation, yLocation, Convert.ToInt32(array[1]));
                        #region Custom Code
                        Utilities.ApplyExcavationSkill(who, bonusLoot, 0, xLocation, yLocation);
                        #endregion
                        break;
                    case "Coal":
                        Game1.createDebris(4, xLocation, yLocation, Convert.ToInt32(array[1]));
                        #region Custom Code
                        Utilities.ApplyExcavationSkill(who, bonusLoot, 4, xLocation, yLocation);
                        #endregion
                        break;
                    case "Iron":
                        Game1.createDebris(2, xLocation, yLocation, Convert.ToInt32(array[1]));
                        #region Custom Code
                        Utilities.ApplyExcavationSkill(who, bonusLoot, 2, xLocation, yLocation);
                        #endregion
                        break;
                    case "Gold":
                        Game1.createDebris(6, xLocation, yLocation, Convert.ToInt32(array[1]));
                        #region Custom Code
                        Utilities.ApplyExcavationSkill(who, bonusLoot, 6, xLocation, yLocation);
                        #endregion
                        break;
                    case "Iridium":
                        Game1.createDebris(10, xLocation, yLocation, Convert.ToInt32(array[1]));
                        #region Custom Code
                        Utilities.ApplyExcavationSkill(who, bonusLoot, 10, xLocation, yLocation);
                        #endregion
                        break;
                    case "CaveCarrot":
                        Game1.createObjectDebris(78, xLocation, yLocation);
                        #region Custom Code
                        Utilities.ApplyExcavationSkill(who, bonusLoot, 78, xLocation, yLocation);
                        #endregion
                        break;
                    case "Arch":
                        Game1.createObjectDebris(Convert.ToInt32(array[1]), xLocation, yLocation);
                        #region Custom Code
                        Utilities.ApplyExcavationSkill(who, bonusLoot, Convert.ToInt32(array[1]), xLocation, yLocation);
                        #endregion
                        break;
                    case "Object":
                        Game1.createObjectDebris(Convert.ToInt32(array[1]), xLocation, yLocation);
                        if (Convert.ToInt32(array[1]) == 78)
                        {
                            Game1.stats.CaveCarrotsFound++;
                        }
                        #region Custom Code
                        Utilities.ApplyExcavationSkill(who, bonusLoot, Convert.ToInt32(array[1]), xLocation, yLocation);
                        if (bonusLoot && Convert.ToInt32(array[1]) == 78)
                        {
                            Game1.stats.CaveCarrotsFound++;
                        }
                        #endregion
                        break;
                }

                __instance.map.GetLayer("Back").Tiles[xLocation, yLocation].Properties["Treasure"] = null;
            }
            else
            {
                bool flag = who != null && who.CurrentTool != null && who.CurrentTool is Hoe && who.CurrentTool.hasEnchantmentOfType<GenerousEnchantment>();
                float num = 0.5f;
                if (!__instance.IsFarm && (bool)__instance.IsOutdoors && Game1.GetSeasonForLocation(__instance).Equals("winter") && random.NextDouble() < 0.08 && !explosion && !detectOnly && !(__instance is Desert))
                {
                    Game1.createObjectDebris((random.NextDouble() < 0.5) ? 412 : 416, xLocation, yLocation);
                    #region Custom Code
                    Utilities.ApplyExcavationSkill(who, bonusLoot, (random.NextDouble() < 0.5) ? 412 : 416, xLocation, yLocation);
                    #endregion
                    if (flag && random.NextDouble() < (double)num)
                    {
                        Game1.createObjectDebris((random.NextDouble() < 0.5) ? 412 : 416, xLocation, yLocation);
                        #region Custom Code
                        Utilities.ApplyExcavationSkill(who, bonusLoot, (random.NextDouble() < 0.5) ? 412 : 416, xLocation, yLocation);
                        #endregion
                    }

                    __result = "";
                }

                if ((bool)__instance.IsOutdoors && random.NextDouble() < 0.03 && !explosion)
                {
                    if (detectOnly)
                    {
                        __instance.map.GetLayer("Back").Tiles[xLocation, yLocation].Properties.Add("Treasure", new PropertyValue("Object " + 330));
                        __result = "Object";
                    }

                    Game1.createObjectDebris(330, xLocation, yLocation);
                    ///Custom Code Location
                    #region Custom Code
                    Utilities.ApplyExcavationSkill(who, bonusLoot, 330, xLocation, yLocation);
                    #endregion
                    if (flag && random.NextDouble() < (double)num)
                    {
                        Game1.createObjectDebris(330, xLocation, yLocation);
                        #region Custom Code
                        Utilities.ApplyExcavationSkill(who, bonusLoot, 330, xLocation, yLocation);
                        #endregion
                    }

                    __result = "";
                }
            }

            __result = "";
            return true;
        }
    }
}
