using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoonShared.Attributes;

namespace PoisonBarAPI.Core
{

    [SConfig]
    public class Config
    {


        [SConfig.Option(0, 255, 1)]
        public int PoisonBarRed { get; set; } = 56;


        [SConfig.Option(0, 255, 1)]
        public int PoisonBarGreen { get; set; } = 36;


        [SConfig.Option(0, 255, 1)]
        public int PoisonBarBlue { get; set; } = 113;

        [SConfig.Option(0, 255, 1)]
        public int PoisonBarTopRed { get; set; } = 0;


        [SConfig.Option(0, 255, 1)]
        public int PoisonBarTopGreen { get; set; } = 170;


        [SConfig.Option(0, 255, 1)]
        public int PoisonBarTopBlue { get; set; } = 119;
    }
}
