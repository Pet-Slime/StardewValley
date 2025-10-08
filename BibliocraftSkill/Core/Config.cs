using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BirbCore.Attributes;

namespace BibliocraftSkill
{
    [SConfig]
    public class Config
    {

        [SConfig.Option(0, 100, 5)]
        public int ExperienceFromReading { get; set; } = 50;


        [SConfig.Option(0, 100, 5)]
        public int ExperienceFromVapiusReading { get; set; } = 25;


        [SConfig.Option(0, 100, 5)]
        public int ExperienceFromBookMachines { get; set; } = 15;

        [SConfig.Option(0, 100, 5)]
        public int ExperienceFromMailing { get; set; } = 5;
    }
}
