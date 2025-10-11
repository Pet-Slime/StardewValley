using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BirbCore.Attributes;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace AthleticSkill
{
    [SConfig]
    public class Config
    {
        [SConfig.Option()]
        public KeybindList Key_Cast { get; set; } = KeybindList.Parse("LeftControl");

        [SConfig.Option()]
        public bool ToggleSprint { get; set; } = false;

        [SConfig.Option(6, 100, 1)]
        public int MinimumEnergyToSprint { get; set; } = 20;

        [SConfig.Option(1, 100, 1)]
        public int ExpFromSprinting{ get; set; } = 1;


        [SConfig.Option(0, 100, 1)]
        public int ExpChanceFromTools { get; set; } = 25;

        [SConfig.Option(1, 100, 1)]
        public int ExpFromLightToolUse { get; set; } = 2;

        [SConfig.Option(1, 100, 1)]
        public int ExpFromHeavyToolUse { get; set; } = 3;
    }
}
