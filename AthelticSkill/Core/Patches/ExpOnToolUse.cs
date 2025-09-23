using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using StardewValley.Tools;
using StardewValley;
using BirbCore.Attributes;

namespace AthleticSkill.Core.Patches
{
    [HarmonyPatch(typeof(Axe), nameof(Axe.DoFunction))]
    public class Axe_exp_patch
    {
        [HarmonyPostfix]
        private static void Postfix(Farmer who)
        {
            Utilities.AddEXP(who, ModEntry.Config.ExpFromStaminaDrain);
        }
    }

    [HarmonyPatch(typeof(FishingRod), nameof(FishingRod.DoFunction))]
    public class FishingRod_exp_patch
    {
        [HarmonyPostfix]
        private static void Postfix(Farmer who)
        {
            Utilities.AddEXP(who, ModEntry.Config.ExpFromStaminaDrain);
        }
    }

    [HarmonyPatch(typeof(Hoe), nameof(Hoe.DoFunction))]
    public class Hoe_exp_patch
    {
        [HarmonyPostfix]
        private static void Postfix(Farmer who)
        {
            Utilities.AddEXP(who, ModEntry.Config.ExpFromStaminaDrain);
        }
    }

    [HarmonyPatch(typeof(MilkPail), nameof(MilkPail.DoFunction))]
    public class MilkPail_exp_patch
    {
        [HarmonyPostfix]
        private static void Postfix(Farmer who)
        {
            Utilities.AddEXP(who, ModEntry.Config.ExpFromStaminaDrain);
        }
    }


    [HarmonyPatch(typeof(Pickaxe), nameof(Pickaxe.DoFunction))]
    public class Pickaxe_exp_patch
    {
        [HarmonyPostfix]
        private static void Postfix(Farmer who)
        {
            Utilities.AddEXP(who, ModEntry.Config.ExpFromStaminaDrain);
        }
    }

    [HarmonyPatch(typeof(Shears), nameof(Shears.DoFunction))]
    public class Shears_exp_patch
    {
        [HarmonyPostfix]
        private static void Postfix(Farmer who)
        {
            Utilities.AddEXP(who, ModEntry.Config.ExpFromStaminaDrain);
        }
    }

    [HarmonyPatch(typeof(WateringCan), nameof(WateringCan.DoFunction))]
    public class WateringCan_exp_patch
    {
        [HarmonyPostfix]
        private static void Postfix(Farmer who)
        {
            Utilities.AddEXP(who, ModEntry.Config.ExpFromStaminaDrain);
        }
    }




}
