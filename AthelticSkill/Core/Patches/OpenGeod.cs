using HarmonyLib;
using Microsoft.Xna.Framework;
using SpaceCore;
using StardewValley;

namespace AthleticSkill.Core.Patches
{
    [HarmonyPatch(typeof(StardewValley.Object), nameof(StardewValley.Object.performUseAction))]
    class OpenGeode_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(StardewValley.Object __instance, ref bool __result, GameLocation location)
        {
            if (!ModEntry.UseAltProfession || __instance == null || __instance is not Item || !Utility.IsGeode(__instance, true))
                return;

            // Get the farmer, and if the farmer is null or doesnt have the right profession, exist out.
            Farmer farmer = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);
            if (farmer == null || !farmer.HasCustomProfession(Athletic_Skill.Athletic10a1))
                return;

            // Get a random item from the Geode, and if the item is null, then exist out
            Item item = Utility.getTreasureFromGeode(__instance);
            if (item == null)
                return;

            // If the player's inventory is full, exit out
            if (farmer.isInventoryFull())
            {
                Game1.addHUDMessage(HUDMessage.ForCornerTextbox(
                    ModEntry.Instance.I18N.Get("moonslime.Athletics.GeodeOpen.InventoryFull"))
                    );
                return;
            }

            // Update stats
            Game1.stats.GeodesCracked++;

            // Play sound
            Game1.playSound("stoneCrack");

            // Create & broadcast animation
            var animation = CreateGeodeAnimation(item, farmer.Position + new Vector2(0f, -96f));
            Game1.Multiplayer.broadcastSprites(location, animation);

            // Add item to inventory
            farmer.addItemToInventory(item);

            // Return that the item has been used
            __result = true;
        }

        /// <summary>
        /// Creates the floating ghost animation from the geode
        /// </summary>
        private static TemporaryAnimatedSprite CreateGeodeAnimation(Item item, Vector2 position)
        {
            TemporaryAnimatedSprite sprite = new TemporaryAnimatedSprite(0, 9999f, 1, 999, position, flicker: false, flipped: false, verticalFlipped: false, 0f)
            {
                motion = new Vector2(0f, -1f),
                scaleChange = 0.01f,
                alpha = 1f,
                alphaFade = 0.0075f,
                shakeIntensity = 1f,
                initialPosition = position,
                xPeriodic = true,
                xPeriodicLoopTime = 1000f,
                xPeriodicRange = 4f,
                layerDepth = 1f
            };
            sprite.CopyAppearanceFromItemId(item.QualifiedItemId);
            return sprite;
        }
    }
}
