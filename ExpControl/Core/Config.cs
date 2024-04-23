using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BirbCore.Attributes;

namespace ExpControl.Core
{
    [SConfig]
    public class Config
    {
        [SConfig.Option(0.1f, 50.0f, 0.1f)]
        public float FarmingEXP { get; set; } = 1f;
        [SConfig.Option(0.1f, 50.0f, 0.1f)]
        public float MiningEXP { get; set; } = 1f;
        [SConfig.Option(0.1f, 50.0f, 0.1f)]
        public float FishingEXP { get; set; } = 1f;
        [SConfig.Option(0.1f, 50.0f, 0.1f)]
        public float ForagingEXP { get; set; } = 1f;
        [SConfig.Option(0.1f, 50.0f, 0.1f)]
        public float CombatEXP { get; set; } = 1f;
    }
}
