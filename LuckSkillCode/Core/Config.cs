using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BirbCore.Attributes;

namespace LuckSkill
{
    [SConfig]
    public class Config
    {
        [SConfig.Option(0, 4000, 1)]
        public int DailyLuckExpBonus { get; set; } = 750;
    }
}
