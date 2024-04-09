using HarmonyLib;
using MoonShared.Patching;
using StardewModdingAPI;
using StardewValley;

using Microsoft.Xna.Framework;
using MoonShared;
using Force.DeepCloner;
using System.IO;
using StardewValley.Network;
using Netcode;
using StardewValley.Buildings;
using StardewValley.Locations;
using System;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;

namespace ArchaeologySkill.Patches
{
    internal class VolcanoDungeonLevel_patch : BasePatcher
    {
        /*********
        ** Public methods
        *********/
        /// <inheritdoc />
        public override void Apply(Harmony harmony, IMonitor monitor)
        {
            harmony.Patch(
                original: this.RequireMethod<StardewValley.Locations.VolcanoDungeon>("drawAboveAlwaysFrontLayer"),
                prefix: this.GetHarmonyMethod(nameof(After_GetPriceAfterMultipliers))
            );
        }


        /*********
        ** Private methods
        *********/
        /// Change the floor string to help make the illusion of the volcano dungeon being infinite

        [HarmonyLib.HarmonyPostfix]
        private static bool After_GetPriceAfterMultipliers(
        StardewValley.Locations.VolcanoDungeon __instance, SpriteBatch b)
        {
            if (__instance.level?.Get() > 15)
            {
                Color color_Red = SpriteText.color_Red;
                string s = (__instance.level?.Get() - 30).Value.ToString() ?? "";
                Microsoft.Xna.Framework.Rectangle titleSafeArea = Game1.game1.GraphicsDevice.Viewport.GetTitleSafeArea();
                SpriteText.drawString(b, s, titleSafeArea.Left + 16, titleSafeArea.Top + 16, 999999, -1, 999999, 1f, 1f, junimoText: false, 2, "", color_Red);
                return false; // don't run original code
            }

            return true; // run original code
        }

 
    }
}
