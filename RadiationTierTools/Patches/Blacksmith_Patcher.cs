using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoonShared.Patching;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Tools;

namespace RadiationTierTools.Patches
{
    internal class Blacksmith_Patcher : BasePatcher
    {
        /*********
        ** Public methods
        *********/
        /// <inheritdoc />
        public override void Apply(Harmony harmony, IMonitor monitor)
        {
            harmony.Patch(
                original: this.RequireMethod<StardewValley.Utility>("getBlacksmithUpgradeStock"),
                prefix: this.GetHarmonyMethod(nameof(Before_RadiationTier))
            );
        }


        /*********
        ** Private methods
        *********/
        /// Post Fix to make it so the player gets more money with the Antiquary profession

        [HarmonyLib.HarmonyPrefix]
        private static bool Before_RadiationTier(
        StardewValley.Object __instance, ref Dictionary<ISalable, int[]> __result, Farmer who)
        {
            Dictionary<ISalable, int[]> dictionary = new Dictionary<ISalable, int[]>();
            Tool toolFromName = who.getToolFromName("Axe");
            Tool toolFromName2 = who.getToolFromName("Watering Can");
            Tool toolFromName3 = who.getToolFromName("Pickaxe");
            Tool toolFromName4 = who.getToolFromName("Hoe");
            if (toolFromName != null && (int)toolFromName.upgradeLevel < 6)
            {
                Tool tool = new Axe();
                tool.UpgradeLevel = (int)toolFromName.upgradeLevel + 1;
                dictionary.Add(tool, new int[3]
                {
                    PriceForToolUpgradeLevel(tool.UpgradeLevel),
                    1,
                    IndexOfExtraMaterialForToolUpgrade(tool.upgradeLevel)
                });
            }

            if (toolFromName2 != null && (int)toolFromName2.upgradeLevel < 6)
            {
                Tool tool2 = new WateringCan();
                tool2.UpgradeLevel = (int)toolFromName2.upgradeLevel + 1;
                dictionary.Add(tool2, new int[3]
                {
                    PriceForToolUpgradeLevel(tool2.UpgradeLevel),
                    1,
                    IndexOfExtraMaterialForToolUpgrade(tool2.upgradeLevel)
                });
            }

            if (toolFromName3 != null && (int)toolFromName3.upgradeLevel < 6)
            {
                Tool tool3 = new Pickaxe();
                tool3.UpgradeLevel = (int)toolFromName3.upgradeLevel + 1;
                dictionary.Add(tool3, new int[3]
                {
                    PriceForToolUpgradeLevel(tool3.UpgradeLevel),
                    1,
                    IndexOfExtraMaterialForToolUpgrade(tool3.upgradeLevel)
                });
            }

            if (toolFromName4 != null && (int)toolFromName4.upgradeLevel < 6)
            {
                Tool tool4 = new Hoe();
                tool4.UpgradeLevel = (int)toolFromName4.upgradeLevel + 1;
                dictionary.Add(tool4, new int[3]
                {
                    PriceForToolUpgradeLevel(tool4.UpgradeLevel),
                    1,
                    IndexOfExtraMaterialForToolUpgrade(tool4.upgradeLevel)
                });
            }

            if (who.trashCanLevel < 4)
            {
                string name = "";
                switch (who.trashCanLevel + 1)
                {
                    case 1:
                        name = Game1.content.LoadString("Strings\\StringsFromCSFiles:Tool.cs.14299", Game1.content.LoadString("Strings\\StringsFromCSFiles:TrashCan"));
                        break;
                    case 2:
                        name = Game1.content.LoadString("Strings\\StringsFromCSFiles:Tool.cs.14300", Game1.content.LoadString("Strings\\StringsFromCSFiles:TrashCan"));
                        break;
                    case 3:
                        name = Game1.content.LoadString("Strings\\StringsFromCSFiles:Tool.cs.14301", Game1.content.LoadString("Strings\\StringsFromCSFiles:TrashCan"));
                        break;
                    case 4:
                        name = Game1.content.LoadString("Strings\\StringsFromCSFiles:Tool.cs.14302", Game1.content.LoadString("Strings\\StringsFromCSFiles:TrashCan"));
                        break;
                }

                Tool key = new GenericTool(name, Game1.content.LoadString("Strings\\StringsFromCSFiles:TrashCan_Description", ((who.trashCanLevel + 1) * 15).ToString() ?? ""), who.trashCanLevel + 1, 13 + who.trashCanLevel, 13 + who.trashCanLevel);
                dictionary.Add(key, new int[3]
                {
                    PriceForToolUpgradeLevel(who.trashCanLevel + 1) / 2,
                    1,
                    IndexOfExtraMaterialForToolUpgrade(who.trashCanLevel + 1)
                });
            }


            __result = dictionary;
            return false;
        }

        private static int PriceForToolUpgradeLevel(int level)
        {
            return level switch
            {
                1 => 2500,
                2 => 5000,
                3 => 10000,
                4 => 20000,
                5 => 40000,
                6 => 80000,
                _ => 2000,
            };
        }

        private static int IndexOfExtraMaterialForToolUpgrade(int level)
        {
            return level switch
            {
                1 => 334,
                2 => 335,
                3 => 336,
                4 => 337,
                5 => 910,
                6 => 852,
                _ => 334,
            };
        }
    }
}
