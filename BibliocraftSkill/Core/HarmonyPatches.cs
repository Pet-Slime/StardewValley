using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MoonShared;
using Newtonsoft.Json.Linq;
using SpaceCore;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Characters;
using StardewValley.Enchantments;
using StardewValley.Extensions;
using StardewValley.GameData.Locations;
using StardewValley.Locations;
using StardewValley.Network;
using StardewValley.Tools;
using static BirbCore.Attributes.SMod;

namespace BibliocraftSkill
{

    [HarmonyPatch(typeof(StardewValley.Object), "readBook")]
    class ReadBookPostfix_patch
    {
        [HarmonyLib.HarmonyPostfix]
        public static void Postfix(GameLocation __instance, ref GameLocation location)
        {
            var who = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);

            Utilities.AddEXP(who, 50);
        }
    }
}
