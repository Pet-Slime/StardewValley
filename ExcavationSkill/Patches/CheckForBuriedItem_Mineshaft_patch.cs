using HarmonyLib;
using StardewValley;
using StardewValley.Locations;
using Netcode;
using StardewValley.Tools;
using MoonShared;
using StardewModdingAPI;
using MoonShared.Patching;

namespace ExcavationSkill
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
            bool isQuarryArea = false;
            try
            {
                ///isQuarryArea from the vanilla method is private.
                ///isQuarryArea is equal to the value of netIsQuarryArea
                ///use reflection to get the value of netIsQuarryArea and set our own isQuarryArea equal to it to match vanilla
                var isQuarryAreaNet = ModEntry.Instance.Helper.Reflection.GetField<NetBool>(__instance, "netIsQuarryArea");
                isQuarryArea = isQuarryAreaNet.GetValue();
            }
            catch
            {
                Log.Error("Could not get value of isQuarryArea");
            }


            if (isQuarryArea)
            {
                __result = "";
            }

            if (Game1.random.NextDouble() < 0.15)
            {
                int objectIndex = 330;
                if (Game1.random.NextDouble() < 0.07)
                {
                    if (Game1.random.NextDouble() < 0.75)
                    {
                        switch (Game1.random.Next(5))
                        {
                            case 0:
                                objectIndex = 96;
                                break;
                            case 1:
                                objectIndex = ((!who.hasOrWillReceiveMail("lostBookFound")) ? 770 : (((int)Game1.netWorldState.Value.LostBooksFound.Value < 21) ? 102 : 770));
                                break;
                            case 2:
                                objectIndex = 110;
                                break;
                            case 3:
                                objectIndex = 112;
                                break;
                            case 4:
                                objectIndex = 585;
                                break;
                        }
                    }
                    else if (Game1.random.NextDouble() < 0.75)
                    {
                        switch (__instance.getMineArea())
                        {
                            case 0:
                            case 10:
                                objectIndex = ((Game1.random.NextDouble() < 0.5) ? 121 : 97);
                                break;
                            case 40:
                                objectIndex = ((Game1.random.NextDouble() < 0.5) ? 122 : 336);
                                break;
                            case 80:
                                objectIndex = 99;
                                break;
                        }
                    }
                    else
                    {
                        objectIndex = ((Game1.random.NextDouble() < 0.5) ? 126 : 127);
                    }
                }
                else if (Game1.random.NextDouble() < 0.19)
                {
                    objectIndex = ((Game1.random.NextDouble() < 0.5) ? 390 : __instance.getOreIndexForLevel(__instance.mineLevel, Game1.random));
                }
                else
                {
                    if (Game1.random.NextDouble() < 0.08)
                    {
                        Game1.createRadialDebris(__instance, 8, xLocation, yLocation, Game1.random.Next(1, 5), resource: true);

                        //Custom code
                        double test2 = Utilities.GetLevel() * 0.05;
                        bool bonusLoot2 = false;
                        if (Game1.random.NextDouble() < test2)
                        {
                            bonusLoot2 = true;
                        }
                        ModEntry.AddEXP(Game1.getFarmer(who.UniqueMultiplayerID), ModEntry.Config.ExperienceFromMinesDigging);
                        Utilities.ApplySpeedBoost(Game1.getFarmer(who.UniqueMultiplayerID));
                        if (bonusLoot2)
                        {
                            Game1.createRadialDebris(__instance, 8, xLocation, yLocation, Game1.random.Next(1, 5), resource: true);
                        }
                        ///Custom Code Location


                        __result = "";
                    }

                    if (Game1.random.NextDouble() < 0.45)
                    {
                        objectIndex = 330;
                    }
                    else if (Game1.random.NextDouble() < 0.12)
                    {
                        if (Game1.random.NextDouble() < 0.25)
                        {
                            objectIndex = 749;
                        }
                        else
                        {
                            switch (__instance.getMineArea())
                            {
                                case 0:
                                case 10:
                                    objectIndex = 535;
                                    break;
                                case 40:
                                    objectIndex = 536;
                                    break;
                                case 80:
                                    objectIndex = 537;
                                    break;
                            }
                        }
                    }
                    else
                    {
                        objectIndex = 78;
                    }
                }
                Game1.createObjectDebris(objectIndex, xLocation, yLocation, who.UniqueMultiplayerID, __instance);


                //Custom code
                double test = Utilities.GetLevel() * 0.05;
                bool bonusLoot = false;
                if (Game1.random.NextDouble() < test)
                {
                    bonusLoot = true;
                }

                ModEntry.AddEXP(Game1.getFarmer(who.UniqueMultiplayerID), ModEntry.Config.ExperienceFromMinesDigging);
                Utilities.ApplySpeedBoost(Game1.getFarmer(who.UniqueMultiplayerID));
                if (bonusLoot)
                {
                    Game1.createObjectDebris(objectIndex, xLocation, yLocation, who.UniqueMultiplayerID, __instance);
                }
                //Custom Code Location
                bool num = who != null && who.CurrentTool != null && who.CurrentTool is Hoe && who.CurrentTool.hasEnchantmentOfType<GenerousEnchantment>();
                float num2 = 0.25f;
                if (num && Game1.random.NextDouble() < (double)num2)
                {
                    Game1.createObjectDebris(objectIndex, xLocation, yLocation, who.UniqueMultiplayerID, __instance);
                    //Custom Code Location
                    ModEntry.AddEXP(Game1.getFarmer(who.UniqueMultiplayerID), ModEntry.Config.ExperienceFromMinesDigging);
                    //Custom Code Location
                }

                __result = "";
            }

            __result = "";
            return true;
        }
    }
}
