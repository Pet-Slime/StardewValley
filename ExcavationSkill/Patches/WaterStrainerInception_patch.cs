using HarmonyLib;
using System;
using System.Collections.Generic;
using StardewValley;
using StardewValley.Locations;
using SpaceCore;
using Microsoft.Xna.Framework;
using StardewValley.Tools;
using MoonShared;
using System.Globalization;
using System.Linq;
using StardewModdingAPI;
using MoonShared.Patching;
using ExcavationSkill.Objects;

namespace ExcavationSkill
{
    internal class WaterStrainerInception_patch : BasePatcher
    {
        /*********
        ** Public methods
        *********/
        /// <inheritdoc />
        public override void Apply(Harmony harmony, IMonitor monitor)
        {
            harmony.Patch(
                original: this.RequireMethod<CraftingRecipe>("createItem"),
                postfix: this.GetHarmonyMethod(nameof(After_Intercept_Dummy_Craft))
            );

        }


        /*********
        ** Private methods
        *********/

        
        /// Post Fix to make it so the player can get EXp. Also the extra loot chance when digging.
        [HarmonyLib.HarmonyPostfix]
        private static void After_Intercept_Dummy_Craft(
            ref Item __result)
        {
            
            if (__result.Name.Contains("moonslime.excavation.dummy_water_strainer"))
            {
                __result = new ShifterObject("yarrrr");
            }
            if (__result.Name.Contains("moonslime.excavation.dummy_path_glass"))
            {
                __result = new PathsObject("1");
            }
            if (__result.Name.Contains("moonslime.excavation.dummy_path_bone"))
            {
                __result = new PathsObject("2");
            }
        }
    }
}
