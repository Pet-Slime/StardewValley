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

namespace ArchaeologySkill
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
            Log.Trace("Archaeology skill check for buried treasure, general");
            return true;

            
        }
    }
}
