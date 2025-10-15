using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BirbCore.Attributes;

namespace WizardryManaBar.Core
{
    [SConfig]
    public class Config
    {
        [SConfig.Option()]
        public bool RenderManaBar { get; set; } = true;


        [SConfig.Option(-500, 500, 1)]
        public int XManaBarOffset { get; set; } = 0;


        [SConfig.Option(0, 1000, 1)]
        public int YManaBarOffset { get; set; } = 0;


        [SConfig.Option(5f, 30f, 0.5f)]
        public float SizeMultiplier { get; set; } = 15f;


        [SConfig.Option(1f, 20f, 1f)]
        public float MaxOverchargeValue { get; set; } = 13f;


        [SConfig.Option()]
        public bool BarsPosition { get; set; } = true;
    }
}
