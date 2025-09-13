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

        [SConfig.Option(1, 30, 1)]
        public int MinimumEnergyToSprint { get; set; } = 5;
    }
}
