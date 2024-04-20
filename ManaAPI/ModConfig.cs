using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManaBar
{
    public class ModConfig
    {
        public bool RenderManaBar { get; set; }

        public int XManaBarOffset { get; set; }

        public int YManaBarOffset { get; set; }

        public float SizeMultiplier { get; set; }

        public float MaxOverchargeValue { get; set; }

        public bool BarsPosition { get; set; }

        public ModConfig()
        {
            XManaBarOffset = 0;
            YManaBarOffset = 0;

            RenderManaBar = true;

            SizeMultiplier = 15f;
            MaxOverchargeValue = 2;

            BarsPosition = true;
        }
    }
}
