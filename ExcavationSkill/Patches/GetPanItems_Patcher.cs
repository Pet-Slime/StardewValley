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
    internal class GetPanItems_Patcher : BasePatcher
    {
        /*********
        ** Public methods
        *********/
        /// <inheritdoc />
        public override void Apply(Harmony harmony, IMonitor monitor)
        {
            harmony.Patch(
                original: this.RequireMethod<Pan>("getPanItems"),
                postfix: this.GetHarmonyMethod(nameof(After_getPanItems))
            );
        }


        /*********
        ** Private methods
        *********/

        [HarmonyLib.HarmonyPostfix]
        private static void After_getPanItems(
        Pan __instance, List<Item> __result, GameLocation location, Farmer who)
        {
            //Add EXP for the player Panning and check for the gold rush profession
            Utilities.ApplyArchaeologySkill(Game1.getFarmer(who.UniqueMultiplayerID));

            //Add Artifacts to the drop list chance if they have the Trowler Profession


            //Add extra loot to the list if they have the Dowser profession

        }
    }
}
