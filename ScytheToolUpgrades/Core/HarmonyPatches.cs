using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using System;
using System.Collections.Generic;
using MoonShared;
using System.Reflection;
using System.Reflection.Emit;
using StardewValley.Tools;
using StardewValley.Menus;
using StardewValley.Objects;

namespace ScytheToolUpgrades
{
    [HarmonyPatch(typeof(Utility), nameof(Utility.getBlacksmithUpgradeStock))]
    class Utility_GetBlacksmithUpgradeStock
    {
        public static void Postfix(
            Dictionary<ISalable, int[]> __result,
            Farmer who)
        {
            try
            {
                UpgradeableScythe.AddToShopStock(itemPriceAndStock: __result, who: who);
            }
            catch (Exception e)
            {
                Log.Error($"Failed in {MethodBase.GetCurrentMethod().DeclaringType}\n{e}");
            }
        }
    }

    [HarmonyPatch(typeof(Farmer), nameof(Farmer.showHoldingItem))]
    class Farmer_ShowHoldingItem
    {
        public static bool Prefix(
            Farmer who)
        {
            try
            {
                Item mrg = who.mostRecentlyGrabbedItem;
                if (mrg is UpgradeableScythe )
                {
                    Rectangle r = UpgradeableScythe.IconSourceRectangle((who.mostRecentlyGrabbedItem as Tool).UpgradeLevel);
                    switch (mrg)
                    {
                        case UpgradeableScythe:
                            r = UpgradeableScythe.IconSourceRectangle((who.mostRecentlyGrabbedItem as Tool).UpgradeLevel);
                            break;
                    }
                    Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(
                        textureName: ModEntry.Assets.SpritesPath,
                        sourceRect: r,
                        animationInterval: 2500f,
                        animationLength: 1,
                        numberOfLoops: 0,
                        position: who.Position + new Vector2(0f, -124f),
                        flicker: false,
                        flipped: false,
                        layerDepth: 1f,
                        alphaFade: 0f,
                        color: Color.White,
                        scale: 4f,
                        scaleChange: 0f,
                        rotation: 0f,
                        rotationChange: 0f)
                    {
                        motion = new Vector2(0f, -0.1f)
                    });
                    return false;
                }
            }
            catch (Exception e)
            {
                Log.Error($"Failed in {MethodBase.GetCurrentMethod().DeclaringType}\n{e}");
            }
            return true;
        }
    }

    // Allow sending pan to upgrade in the mail with Mail Services
    [HarmonyPatch("MailServicesMod.ToolUpgradeOverrides", "mailbox")]
    class MailServicesMod_ToolUpgradeOverrides_Mailbox_Pan
    {
        public static bool Prepare()
        {
            return ModEntry.Instance.Helper.ModRegistry.IsLoaded("Digus.MailServicesMod");
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);

            for (int i = 0; i < code.Count; i++)
            {
                if (code[i].Is(OpCodes.Isinst, typeof(Axe)))
                {
                    yield return new CodeInstruction(OpCodes.Isinst, typeof(UpgradeableScythe));
                    yield return code[i + 1];
                    yield return code[i + 2];
                    // ILCode of newer versions is shorter for whatever reason
                    if (ModEntry.Instance.Helper.ModRegistry.Get("Digus.MailServicesMod").Manifest.Version.IsOlderThan("1.5"))
                    {
                        yield return code[1 + 3];
                    }
                    yield return code[i];
                }
                else
                {
                    yield return code[i];
                }
            }
        }
    }
}
