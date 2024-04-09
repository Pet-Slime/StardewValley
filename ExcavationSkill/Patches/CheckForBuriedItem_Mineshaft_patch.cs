using HarmonyLib;
using StardewValley;
using StardewValley.Locations;
using Netcode;
using StardewValley.Tools;
using MoonShared;
using StardewModdingAPI;
using MoonShared.Patching;
using StardewValley.Enchantments;
using StardewValley.Extensions;

namespace ArchaeologySkill
{
    internal class CheckForBuriedItem_Mineshaft_patch : BasePatcher
    {
        /*********
        ** Public methods
        *********/
        /// <inheritdoc />
        public override void Apply(Harmony harmony, IMonitor monitor)
        {
            harmony.Patch(
                original: this.RequireMethod<MineShaft>("checkForBuriedItem"),
                prefix: this.GetHarmonyMethod(nameof(Replace_EXP))
            );
        }


        /*********
        ** Private methods
        *********/
        /// Post Fix to make it so the player gets more money with the Antiquary profession

        [HarmonyLib.HarmonyPrefix]
        private static bool Replace_EXP(
        MineShaft __instance, string __result, int xLocation, int yLocation, bool explosion, bool detectOnly, Farmer who)
        {


            if (__instance.isQuarryArea)
            {
                __result = "";
            }

            if (Game1.random.NextDouble() < 0.15)
            {
                string id = "(O)330";
                if (Game1.random.NextDouble() < 0.07)
                {
                    if (Game1.random.NextDouble() < 0.75)
                    {
                        switch (Game1.random.Next(5))
                        {
                            case 0:
                                id = "(O)96";
                                break;
                            case 1:
                                id = ((!who.hasOrWillReceiveMail("lostBookFound")) ? "(O)770" : ((Game1.netWorldState.Value.LostBooksFound < 21) ? "(O)102" : "(O)770"));
                                break;
                            case 2:
                                id = "(O)110";
                                break;
                            case 3:
                                id = "(O)112";
                                break;
                            case 4:
                                id = "(O)585";
                                break;
                        }
                    }
                    else if (Game1.random.NextDouble() < 0.75)
                    {
                        switch (__instance.getMineArea())
                        {
                            case 0:
                            case 10:
                                id = Game1.random.Choose("(O)121", "(O)97");
                                break;
                            case 40:
                                id = Game1.random.Choose("(O)122", "(O)336");
                                break;
                            case 80:
                                id = "(O)99";
                                break;
                        }
                    }
                    else
                    {
                        id = Game1.random.Choose("(O)126", "(O)127");
                    }
                }
                else if (Game1.random.NextDouble() < 0.19)
                {
                    id = (Game1.random.NextBool() ? "(O)390" : __instance.getOreIdForLevel(__instance.mineLevel, Game1.random));
                }
                else if (Game1.random.NextDouble() < 0.45)
                {
                    id = "(O)330";
                }
                else if (Game1.random.NextDouble() < 0.12)
                {
                    if (Game1.random.NextDouble() < 0.25)
                    {
                        id = "(O)749";
                    }
                    else
                    {
                        switch (__instance.getMineArea())
                        {
                            case 0:
                            case 10:
                                id = "(O)535";
                                break;
                            case 40:
                                id = "(O)536";
                                break;
                            case 80:
                                id = "(O)537";
                                break;
                        }
                    }
                }
                else
                {
                    id = "(O)78";
                }

                Game1.createObjectDebris(id, xLocation, yLocation, who.UniqueMultiplayerID, __instance);
                bool num = who?.CurrentTool is Hoe && who.CurrentTool.hasEnchantmentOfType<GenerousEnchantment>();
                float num2 = 0.25f;
                if (num && Game1.random.NextDouble() < (double)num2)
                {
                    Game1.createObjectDebris(id, xLocation, yLocation, who.UniqueMultiplayerID, __instance);
                }

                __result = "";
            }

            __result = "";
            return false;
        }
    }
}
